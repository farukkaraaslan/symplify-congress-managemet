using ClosedXML.Excel;
using Core.Application.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.CongressBoardMembers.Commands;
using Symplify.BackOffice.Application.Features.CongressBoardMembers.Commands.Create;
using Symplify.BackOffice.Application.Features.CongressBoardMembers.Commands.Delete;
using Symplify.BackOffice.Application.Features.CongressBoardMembers.Commands.ImportExcel;
using Symplify.BackOffice.Application.Features.CongressBoardMembers.Commands.Reorder;
using Symplify.BackOffice.Application.Features.CongressBoardMembers.Commands.Update;
using Symplify.BackOffice.Application.Features.CongressBoardMembers.Queries.GetForUpdate;
using Symplify.BackOffice.Application.Features.CongressBoardMembers.Queries.GetList;
using Symplify.BackOffice.Application.Features.CongressBoards.Queries.GetList;
using Symplify.BackOffice.Application.Features.Titles.Queries.GetList;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.WebUI.Localization;
using Symplify.BackOffice.WebUI.Models.CongressBoardMembers;
using Symplify.BackOffice.WebUI.Models.Shared.DataTables;

namespace Symplify.BackOffice.WebUI.Controllers;

[Authorize]
[Route("{culture=tr-TR}/[controller]/[action]")]
public sealed class CongressBoardMembersController : Controller
{
    private const long MaxImageSizeInBytes = 5 * 1024 * 1024;
    private const long MaxExcelSizeInBytes = 5 * 1024 * 1024;

    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    private readonly IMediator _mediator;
    private readonly IApplicationLanguageProvider _applicationLanguageProvider;
    private readonly IBackOfficeViewLocalizer _localizer;

    public CongressBoardMembersController(
        IMediator mediator,
        IApplicationLanguageProvider applicationLanguageProvider,
        IBackOfficeViewLocalizer localizer)
    {
        _mediator = mediator;
        _applicationLanguageProvider = applicationLanguageProvider;
        _localizer = localizer;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GetList(
        [FromForm] DataTableRequest request,
        [FromForm] Guid congressId,
        [FromForm] string? boardName,
        [FromForm] string? academicTitle,
        [FromForm] string? status,
        CancellationToken cancellationToken)
    {
        if (congressId == Guid.Empty)
        {
            return Json(new
            {
                draw = request.Draw,
                recordsTotal = 0,
                recordsFiltered = 0,
                data = Array.Empty<object>(),
                summary = CreateSummary(Array.Empty<GetListCongressBoardMemberListItemDto>())
            });
        }

        DataTableQueryOptions tableOptions = DataTableQueryOptions.From(
            request,
            defaultSortColumn: "order",
            defaultSortDirection: "asc",
            allowedSortColumns: new[]
            {
                "order", "boardName", "academicTitle", "fullName", "institution", "isAcceptanceLetterSigner", "isActive"
            });

        string culture = await ResolveCurrentCultureAsync(cancellationToken);
        bool? isActive = ParseStatusFilter(status);

        var response = await _mediator.Send(
            new GetListCongressBoardMemberQuery
            {
                CongressId = congressId,
                Culture = culture,
                SearchText = tableOptions.SearchText,
                BoardName = boardName,
                AcademicTitle = academicTitle,
                IsActive = isActive,
                SortColumn = tableOptions.SortColumn,
                SortDirection = tableOptions.SortDirection,
                PageRequest = new PageRequest
                {
                    Page = tableOptions.Page,
                    PageSize = tableOptions.PageSize
                }
            },
            cancellationToken);

        var allForSummary = await _mediator.Send(
            new GetListCongressBoardMemberQuery
            {
                CongressId = congressId,
                Culture = culture,
                PageRequest = new PageRequest { Page = 0, PageSize = 10000 }
            },
            cancellationToken);

        return Json(new
        {
            draw = request.Draw,
            recordsTotal = response.Count,
            recordsFiltered = response.Count,
            summary = CreateSummary(allForSummary.Items),
            data = response.Items.Select((item, index) => new
            {
                rowNumber = tableOptions.Start + index + 1,
                id = item.Id,
                congressId = item.CongressId,
                congressBoardId = item.CongressBoardId,
                boardName = item.BoardName,
                academicTitle = string.IsNullOrWhiteSpace(item.AcademicTitle) ? "-" : item.AcademicTitle,
                fullName = item.FullName,
                institution = string.IsNullOrWhiteSpace(item.Institution) ? "-" : item.Institution,
                imagePreviewUrl = BuildBoardMemberPhotoUrl(
                    item.Id,
                    item.CongressId,
                    item.HasImage,
                    culture),
                order = item.Order,
                isAcceptanceLetterSigner = item.IsAcceptanceLetterSigner,
                hasSignature = item.HasSignature,
                isActive = item.IsActive,
                isFallback = item.IsFallback
            })
        });
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reorder(
        [FromBody] DataTableReorderRequest request,
        [FromQuery] Guid congressId,
        CancellationToken cancellationToken)
    {
        if (congressId == Guid.Empty || request.Items.Count == 0)
        {
            return BadRequest(new
            {
                success = false,
                message = GetText("Common.InvalidRequest", "Geçersiz istek.")
            });
        }

        try
        {
            await _mediator.Send(
                new ReorderCongressBoardMemberCommand
                {
                    CongressId = congressId,
                    Items = request.Items
                        .Where(item => item.Id != Guid.Empty)
                        .Select(item => new ReorderCongressBoardMemberItemDto
                        {
                            Id = item.Id,
                            Order = item.Order
                        })
                        .ToList()
                },
                cancellationToken);

            return Json(new
            {
                success = true,
                message = GetText("Common.Updated", "Kayıt güncellendi.")
            });
        }
        catch (Exception exception)
        {
            return BadRequest(new
            {
                success = false,
                message = GetExceptionMessage(exception)
            });
        }
    }

    [HttpGet]
    public async Task<IActionResult> CreateModal(Guid congressId, CancellationToken cancellationToken)
    {
        CreateCongressBoardMemberViewModel model = new()
        {
            CongressId = congressId,
            IsActive = true,
            Order = 0,
            BoardOptions = await GetBoardOptionsAsync(congressId, cancellationToken),
            AcademicTitleOptions = await GetAcademicTitleOptionsAsync(cancellationToken),
            Translations = await BuildTranslationViewModelsAsync(cancellationToken)
        };

        return PartialView("~/Views/CongressBoardMembers/_CreateCommitteeMemberModal.cshtml", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] CreateCongressBoardMemberViewModel model, CancellationToken cancellationToken)
    {
        ValidateCreateModel(model);

        if (!ModelState.IsValid)
            return BadRequest(CreateValidationErrorResponse());

        Stream? imageStream = null;

        try
        {
            CongressBoardMemberImageInputDto? imageInput = CreateImageInput(model.ImageFile, out imageStream);

            CreatedCongressBoardMemberResponse response = await _mediator.Send(
                new CreateCongressBoardMemberCommand
                {
                    CongressId = model.CongressId,
                    CongressBoardId = model.CongressBoardId,
                    BoardName = null,
                    FullName = model.FullName,
                    AcademicTitle = NormalizeText(model.AcademicTitle),
                    Institution = NormalizeText(model.Institution),
                    Image = imageInput,
                    Order = 0,
                    IsActive = model.IsActive,
                    Translations = BuildTranslationInputs(model.Translations)
                },
                cancellationToken);

            return Json(new
            {
                success = true,
                id = response.Id,
                message = GetText("BackOffice.CongressBoardMembers.Messages.Created", "Kurul üyesi başarıyla oluşturuldu.")
            });
        }
        catch (Exception exception)
        {
            return BadRequest(new
            {
                success = false,
                message = GetExceptionMessage(exception)
            });
        }
        finally
        {
            if (imageStream is not null)
                await imageStream.DisposeAsync();
        }
    }

    [HttpGet]
    public async Task<IActionResult> EditModal(Guid id, Guid congressId, CancellationToken cancellationToken)
    {
        GetCongressBoardMemberForUpdateResponse response = await _mediator.Send(
            new GetCongressBoardMemberForUpdateQuery
            {
                Id = id,
                CongressId = congressId
            },
            cancellationToken);

        UpdateCongressBoardMemberViewModel model = new()
        {
            Id = response.Id,
            CongressId = response.CongressId,
            CongressBoardId = response.CongressBoardId,
            BoardName = response.BoardName,
            FullName = response.FullName,
            AcademicTitle = response.AcademicTitle,
            Institution = response.Institution,
            ImagePath = response.ImagePath,
            ImagePreviewUrl = BuildBoardMemberPhotoUrl(
                response.Id,
                response.CongressId,
                !string.IsNullOrWhiteSpace(response.ImageObjectName) ||
                !string.IsNullOrWhiteSpace(response.ImagePath),
                ResolveRouteCulture()),
            Order = response.Order,
            IsActive = response.IsActive,
            BoardOptions = await GetBoardOptionsAsync(congressId, cancellationToken),
            AcademicTitleOptions = await GetAcademicTitleOptionsAsync(cancellationToken),
            Translations = response.Translations.Select(translation => new CongressBoardMemberTranslationViewModel
            {
                LanguageId = translation.LanguageId,
                Culture = translation.Culture,
                LanguageName = translation.LanguageName,
                IsDefault = translation.IsDefault,
                Exists = translation.Exists,
                Biography = GetField(translation.Fields, "Biography")
            }).ToList()
        };

        return PartialView("~/Views/CongressBoardMembers/_UpdateCommitteeMemberModal.cshtml", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update([FromForm] UpdateCongressBoardMemberViewModel model, CancellationToken cancellationToken)
    {
        ValidateUpdateModel(model);

        if (!ModelState.IsValid)
            return BadRequest(CreateValidationErrorResponse());

        Stream? imageStream = null;

        try
        {
            CongressBoardMemberImageInputDto? imageInput = CreateImageInput(model.ImageFile, out imageStream);

            await _mediator.Send(
                new UpdateCongressBoardMemberCommand
                {
                    Id = model.Id,
                    CongressId = model.CongressId,
                    CongressBoardId = model.CongressBoardId,
                    BoardName = null,
                    FullName = model.FullName,
                    AcademicTitle = NormalizeText(model.AcademicTitle),
                    Institution = NormalizeText(model.Institution),
                    Image = imageInput,
                    Order = 0,
                    IsActive = model.IsActive,
                    Translations = BuildTranslationInputs(model.Translations)
                },
                cancellationToken);

            return Json(new
            {
                success = true,
                message = GetText("BackOffice.CongressBoardMembers.Messages.Updated", "Kurul üyesi başarıyla güncellendi.")
            });
        }
        catch (Exception exception)
        {
            return BadRequest(new
            {
                success = false,
                message = GetExceptionMessage(exception)
            });
        }
        finally
        {
            if (imageStream is not null)
                await imageStream.DisposeAsync();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete([FromForm] Guid id, [FromForm] Guid congressId, CancellationToken cancellationToken)
    {
        try
        {
            await _mediator.Send(new DeleteCongressBoardMemberCommand { Id = id }, cancellationToken);

            return Json(new
            {
                success = true,
                message = GetText("BackOffice.CongressBoardMembers.Messages.Deleted", "Kurul üyesi başarıyla silindi.")
            });
        }
        catch (Exception exception)
        {
            return BadRequest(new
            {
                success = false,
                message = GetExceptionMessage(exception)
            });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetFilterOptions(Guid congressId, CancellationToken cancellationToken)
    {
        return Json(new
        {
            boardOptions = (await GetBoardOptionsAsync(congressId, cancellationToken)).Select(item => new { value = item.Name, text = item.Name }),
            academicTitleOptions = (await GetAcademicTitleOptionsAsync(cancellationToken)).Select(title => new
            {
                value = title,
                text = string.Equals(title, "-", StringComparison.OrdinalIgnoreCase)
                    ? GetText("BackOffice.CongressBoardMembers.Fields.NoAcademicTitle", "Ünvansız")
                    : title
            })
        });
    }

    [HttpGet]
    public async Task<IActionResult> DownloadExcelTemplate(Guid congressId, CancellationToken cancellationToken)
    {
        List<CongressBoardSelectItemViewModel> boardOptions = await GetBoardOptionsAsync(congressId, cancellationToken);
        List<string> titleOptions = await GetAcademicTitleOptionsAsync(cancellationToken);

        using XLWorkbook workbook = new();
        IXLWorksheet sheet = workbook.Worksheets.Add("Kurul Üyeleri");
        IXLWorksheet lists = workbook.Worksheets.Add("Listeler");

        string[] headers =
        {
            "Kurul Türü",
            "Akademik Ünvan",
            "Ad Soyad",
            "Kurum",
            "Durum",
            "Açıklama"
        };

        for (int index = 0; index < headers.Length; index++)
            sheet.Cell(1, index + 1).Value = headers[index];

        sheet.Cell(2, 1).Value = boardOptions.FirstOrDefault()?.Name ?? "Bilim Kurulu";
        sheet.Cell(2, 2).Value = titleOptions.FirstOrDefault(title => !string.Equals(title, "-", StringComparison.OrdinalIgnoreCase)) ?? "Prof. Dr.";
        sheet.Cell(2, 3).Value = "Ad Soyad";
        sheet.Cell(2, 4).Value = "Üniversite / Kurum";
        sheet.Cell(2, 5).Value = "Aktif";
        sheet.Cell(2, 6).Value = "İsteğe bağlı görev veya açıklama.";

        for (int index = 0; index < boardOptions.Count; index++)
            lists.Cell(index + 1, 1).Value = boardOptions[index].Name;

        for (int index = 0; index < titleOptions.Count; index++)
            lists.Cell(index + 1, 2).Value = titleOptions[index];

        lists.Cell(1, 3).Value = "Aktif";
        lists.Cell(2, 3).Value = "Pasif";

        if (boardOptions.Count > 0)
        {
            string boardRange = $"Listeler!$A$1:$A${boardOptions.Count}";
            sheet.Range("A2:A1000").SetDataValidation().List(boardRange, true);
        }

        if (titleOptions.Count > 0)
        {
            string titleRange = $"Listeler!$B$1:$B${titleOptions.Count}";
            sheet.Range("B2:B1000").SetDataValidation().List(titleRange, true);
        }

        sheet.Range("E2:E1000").SetDataValidation().List("Listeler!$C$1:$C$2", true);

        sheet.Range("A1:F1").Style.Font.Bold = true;
        sheet.Range("A1:F1").Style.Fill.BackgroundColor = XLColor.FromHtml("#EAF2FF");
        sheet.Range("A1:F1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        sheet.Range("A1:F2").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        sheet.Range("A1:F2").Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        sheet.Columns().AdjustToContents();
        sheet.SheetView.FreezeRows(1);

        lists.Hide();

        using MemoryStream stream = new();
        workbook.SaveAs(stream);

        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "kurul-uyeleri-template.xlsx");
    }

    [HttpGet]
    public IActionResult UploadExcelModal(Guid congressId)
    {
        return PartialView("~/Views/CongressBoardMembers/_ExcelUploadCommitteeMemberModal.cshtml", new UploadCongressBoardMembersExcelViewModel
        {
            CongressId = congressId
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadExcel([FromForm] UploadCongressBoardMembersExcelViewModel model, CancellationToken cancellationToken)
    {
        ValidateExcelModel(model);

        if (!ModelState.IsValid)
            return BadRequest(CreateValidationErrorResponse());

        try
        {
            List<CongressBoardMemberExcelImportRowDto> rows = ReadExcelRows(model.File!);

            ImportCongressBoardMembersFromExcelResponse response = await _mediator.Send(
                new ImportCongressBoardMembersFromExcelCommand
                {
                    CongressId = model.CongressId,
                    Rows = rows
                },
                cancellationToken);

            return Json(new
            {
                success = response.Errors.Count == 0,
                importedCount = response.ImportedCount,
                skippedCount = response.SkippedCount,
                errors = response.Errors,
                message = response.Errors.Count == 0
                    ? GetText("BackOffice.CongressBoardMembers.Messages.Imported", "Excel dosyası başarıyla yüklendi.")
                    : GetText("BackOffice.CongressBoardMembers.Messages.ImportedWithErrors", "Excel dosyası bazı hatalarla işlendi.")
            });
        }
        catch (Exception exception)
        {
            return BadRequest(new
            {
                success = false,
                message = GetExceptionMessage(exception)
            });
        }
    }

    private async Task<List<CongressBoardSelectItemViewModel>> GetBoardOptionsAsync(Guid congressId, CancellationToken cancellationToken)
    {
        string culture = await ResolveCurrentCultureAsync(cancellationToken);

        var response = await _mediator.Send(
            new GetListCongressBoardQuery
            {
                CongressId = congressId,
                Culture = culture,
                IsActive = true,
                PageRequest = new PageRequest { Page = 0, PageSize = 500 }
            },
            cancellationToken);

        List<CongressBoardSelectItemViewModel> options = response.Items
            .Where(item => !string.IsNullOrWhiteSpace(item.Name))
            .OrderBy(item => item.Order <= 0 ? int.MaxValue : item.Order)
            .ThenBy(item => item.Name)
            .Select(item => new CongressBoardSelectItemViewModel
            {
                Id = item.Id,
                Name = item.Name
            })
            .ToList();

        return options;
    }

    private async Task<List<string>> GetAcademicTitleOptionsAsync(CancellationToken cancellationToken)
    {
        string culture = await ResolveCurrentCultureAsync(cancellationToken);

        var response = await _mediator.Send(
            new GetListTitleQuery
            {
                Culture = culture,
                IsActive = true,
                SortColumn = "order",
                SortDirection = "asc",
                PageRequest = new PageRequest { Page = 0, PageSize = 500 }
            },
            cancellationToken);

        List<string> titles = response.Items
            .OrderBy(item => item.Order <= 0 ? int.MaxValue : item.Order)
            .ThenBy(item => item.Name)
            .Select(item => ResolveAcademicTitleOption(item.Description, item.Code, item.Name))
            .Where(title => !string.IsNullOrWhiteSpace(title))
            .Select(title => title.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (titles.All(title => !string.Equals(title, "-", StringComparison.OrdinalIgnoreCase)))
            titles.Insert(0, "-");

        return titles;
    }

    private async Task<List<CongressBoardMemberTranslationViewModel>> BuildTranslationViewModelsAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<ApplicationLanguageDto> languages = await _applicationLanguageProvider.GetActiveLanguagesAsync(cancellationToken);

        return languages
            .OrderByDescending(language => language.IsDefault)
            .ThenBy(language => language.Order)
            .ThenBy(language => language.Name)
            .Select(language => new CongressBoardMemberTranslationViewModel
            {
                LanguageId = language.Id,
                Culture = language.Culture,
                LanguageName = language.Name,
                IsDefault = language.IsDefault,
                Exists = false
            })
            .ToList();
    }

    private void ValidateCreateModel(CreateCongressBoardMemberViewModel model)
    {
        ValidateBaseModel(model.CongressId, model.CongressBoardId, model.FullName, model.ImageFile);
    }

    private void ValidateUpdateModel(UpdateCongressBoardMemberViewModel model)
    {
        if (model.Id == Guid.Empty)
            ModelState.AddModelError(nameof(model.Id), GetText("Common.InvalidRequest", "Geçersiz istek."));

        ValidateBaseModel(model.CongressId, model.CongressBoardId, model.FullName, model.ImageFile);
    }

    private void ValidateBaseModel(Guid congressId, Guid? congressBoardId, string? fullName, IFormFile? imageFile)
    {
        if (congressId == Guid.Empty)
            ModelState.AddModelError(nameof(CreateCongressBoardMemberViewModel.CongressId), GetText("BackOffice.CongressBoardMembers.Validation.CongressRequired", "Kongre bilgisi zorunludur."));

        if (!congressBoardId.HasValue || congressBoardId.Value == Guid.Empty)
            ModelState.AddModelError(nameof(CreateCongressBoardMemberViewModel.CongressBoardId), GetText("BackOffice.CongressBoardMembers.Validation.BoardRequired", "Kurul türü zorunludur."));

        if (string.IsNullOrWhiteSpace(fullName))
            ModelState.AddModelError(nameof(CreateCongressBoardMemberViewModel.FullName), GetText("BackOffice.CongressBoardMembers.Validation.FullNameRequired", "Ad soyad zorunludur."));

        if (imageFile is not null && imageFile.Length > 0)
            ValidateImageFile(imageFile, nameof(CreateCongressBoardMemberViewModel.ImageFile));
    }

    private void ValidateImageFile(IFormFile file, string key)
    {
        string extension = Path.GetExtension(file.FileName);

        if (!AllowedImageExtensions.Contains(extension))
            ModelState.AddModelError(key, GetText("BackOffice.CongressBoardMembers.Validation.ImageExtensionInvalid", "Sadece JPG, PNG veya WEBP görsel yükleyebilirsiniz."));

        if (file.Length > MaxImageSizeInBytes)
            ModelState.AddModelError(key, GetText("BackOffice.CongressBoardMembers.Validation.ImageSizeInvalid", "Fotoğraf en fazla 5 MB olabilir."));
    }

    private void ValidateExcelModel(UploadCongressBoardMembersExcelViewModel model)
    {
        if (model.CongressId == Guid.Empty)
            ModelState.AddModelError(nameof(model.CongressId), GetText("BackOffice.CongressBoardMembers.Validation.CongressRequired", "Kongre bilgisi zorunludur."));

        if (model.File is null || model.File.Length <= 0)
        {
            ModelState.AddModelError(nameof(model.File), GetText("BackOffice.CongressBoardMembers.Validation.ExcelFileRequired", "Excel dosyası seçilmelidir."));
            return;
        }

        string extension = Path.GetExtension(model.File.FileName);

        if (!string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase))
            ModelState.AddModelError(nameof(model.File), GetText("BackOffice.CongressBoardMembers.Validation.ExcelFileInvalid", "Sadece .xlsx dosyası yükleyebilirsiniz."));

        if (model.File.Length > MaxExcelSizeInBytes)
            ModelState.AddModelError(nameof(model.File), GetText("BackOffice.CongressBoardMembers.Validation.ExcelFileInvalid", "Excel dosyası en fazla 5 MB olabilir."));
    }

    private List<CongressBoardMemberExcelImportRowDto> ReadExcelRows(IFormFile file)
    {
        using Stream stream = file.OpenReadStream();
        using XLWorkbook workbook = new(stream);
        IXLWorksheet worksheet = workbook.Worksheet(1);

        List<CongressBoardMemberExcelImportRowDto> rows = new();
        int lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

        for (int rowNumber = 2; rowNumber <= lastRow; rowNumber++)
        {
            string? boardName = ReadCell(worksheet, rowNumber, 1);
            string? academicTitle = ReadCell(worksheet, rowNumber, 2);
            string? fullName = ReadCell(worksheet, rowNumber, 3);
            string? institution = ReadCell(worksheet, rowNumber, 4);
            string? statusText = ReadCell(worksheet, rowNumber, 5);
            string? description = ReadCell(worksheet, rowNumber, 6);

            if (string.IsNullOrWhiteSpace(boardName) &&
                string.IsNullOrWhiteSpace(academicTitle) &&
                string.IsNullOrWhiteSpace(fullName) &&
                string.IsNullOrWhiteSpace(institution))
            {
                continue;
            }

            rows.Add(new CongressBoardMemberExcelImportRowDto
            {
                RowNumber = rowNumber,
                BoardName = boardName,
                AcademicTitle = academicTitle,
                FullName = fullName,
                Institution = institution,
                Order = null,
                IsActive = ParseExcelStatus(statusText),
                Description = description
            });
        }

        return rows;
    }

    private static string? ReadCell(IXLWorksheet worksheet, int row, int column)
    {
        string value = worksheet.Cell(row, column).GetString();
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static bool ParseExcelStatus(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;

        string normalized = value.Trim().ToLowerInvariant();

        return normalized is "aktif" or "active" or "true" or "1" or "evet" or "yes";
    }

    private static bool? ParseStatusFilter(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return null;

        string normalized = status.Trim().ToLowerInvariant();

        if (normalized is "aktif" or "active" or "true" or "1")
            return true;

        if (normalized is "pasif" or "passive" or "false" or "0")
            return false;

        return null;
    }

    private static object CreateSummary(IEnumerable<GetListCongressBoardMemberListItemDto> items)
    {
        List<GetListCongressBoardMemberListItemDto> itemList = items.ToList();

        return new
        {
            total = itemList.Count,
            organizing = itemList.Count(item =>
                string.Equals(item.BoardName, "Düzenleme Kurulu", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(item.BoardName, "Düzenleme Kurulu Başkanı", StringComparison.OrdinalIgnoreCase)),
            scientific = itemList.Count(item => string.Equals(item.BoardName, "Bilim Kurulu", StringComparison.OrdinalIgnoreCase)),
            secretariat = itemList.Count(item => string.Equals(item.BoardName, "Sekreterya", StringComparison.OrdinalIgnoreCase))
        };
    }

    private ICollection<TranslationInputDto> BuildTranslationInputs(IEnumerable<CongressBoardMemberTranslationViewModel> translations)
    {
        return translations
            .GroupBy(translation => translation.LanguageId)
            .Select(group => group.First())
            .Where(translation => translation.IsDefault || !string.IsNullOrWhiteSpace(translation.Biography))
            .Select(translation => new TranslationInputDto
            {
                LanguageId = translation.LanguageId,
                Fields = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Biography"] = NormalizeText(translation.Biography)
                }
            })
            .ToList();
    }

    private static CongressBoardMemberImageInputDto? CreateImageInput(IFormFile? file, out Stream? stream)
    {
        stream = null;

        if (file is null || file.Length <= 0)
            return null;

        stream = file.OpenReadStream();

        return new CongressBoardMemberImageInputDto
        {
            OriginalFileName = file.FileName,
            ContentType = file.ContentType,
            Length = file.Length,
            Content = stream
        };
    }

    private async Task<string> ResolveCurrentCultureAsync(CancellationToken cancellationToken)
    {
        string? routeCulture = RouteData.Values["culture"]?.ToString();

        if (!string.IsNullOrWhiteSpace(routeCulture))
            return await NormalizeCultureFromApplicationLanguagesAsync(routeCulture, cancellationToken);

        string? pathCulture = HttpContext.Request.Path.Value?
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();

        return await NormalizeCultureFromApplicationLanguagesAsync(pathCulture, cancellationToken);
    }

    private async Task<string> NormalizeCultureFromApplicationLanguagesAsync(string? culture, CancellationToken cancellationToken)
    {
        IReadOnlyList<ApplicationLanguageDto> activeLanguages = await _applicationLanguageProvider.GetActiveLanguagesAsync(cancellationToken);
        string? requestedCulture = culture?.Trim();

        if (!string.IsNullOrWhiteSpace(requestedCulture))
        {
            ApplicationLanguageDto? matchedLanguage = activeLanguages
                .OrderByDescending(language => language.IsDefault)
                .ThenBy(language => language.Name)
                .FirstOrDefault(language =>
                    string.Equals(language.Culture, requestedCulture, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(GetTwoLetterIsoCode(language.Culture), requestedCulture, StringComparison.OrdinalIgnoreCase));

            if (matchedLanguage is not null)
                return matchedLanguage.Culture;
        }

        ApplicationLanguageDto defaultLanguage = await _applicationLanguageProvider.GetDefaultLanguageAsync(cancellationToken);

        return !string.IsNullOrWhiteSpace(defaultLanguage.Culture)
            ? defaultLanguage.Culture
            : activeLanguages.OrderByDescending(language => language.IsDefault).ThenBy(language => language.Name).FirstOrDefault()?.Culture ?? "tr-TR";
    }

    private static string GetTwoLetterIsoCode(string? culture)
    {
        if (string.IsNullOrWhiteSpace(culture))
            return string.Empty;

        string normalizedCulture = culture.Trim();
        int separatorIndex = normalizedCulture.IndexOf('-');

        return separatorIndex > 0 ? normalizedCulture[..separatorIndex] : normalizedCulture;
    }


    private string? BuildBoardMemberPhotoUrl(
        Guid id,
        Guid congressId,
        bool hasImage,
        string culture)
    {
        if (!hasImage || id == Guid.Empty || congressId == Guid.Empty)
            return null;

        return Url.Action(
            "Photo",
            "CongressBoardMemberMedia",
            new
            {
                culture,
                congressId,
                id
            });
    }

    private string ResolveRouteCulture()
        => RouteData.Values["culture"]?.ToString() ?? "tr-TR";


    private string GetText(string key, string fallback)
    {
        string value = _localizer.GetStringValue(key);

        return string.IsNullOrWhiteSpace(value) ? key : value;
    }

    private string GetExceptionMessage(Exception exception)
    {
        return !string.IsNullOrWhiteSpace(exception.Message)
            ? GetText(exception.Message, exception.Message)
            : GetText("Common.GenericError", string.Empty);
    }

    private object CreateValidationErrorResponse()
    {
        return new
        {
            success = false,
            message = GetText("Common.InvalidRequest", "Form alanlarını kontrol edin."),
            errors = ModelState
                .Where(item => item.Value?.Errors.Count > 0)
                .ToDictionary(
                    item => item.Key,
                    item => item.Value!.Errors.Select(error =>
                        GetText(string.IsNullOrWhiteSpace(error.ErrorMessage) ? "Common.InvalidRequest" : error.ErrorMessage, error.ErrorMessage)).ToArray())
        };
    }

    private static string? NormalizeText(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string ResolveAcademicTitleOption(string? description, string? code, string? name)
    {
        if (!string.IsNullOrWhiteSpace(description))
            return description.Trim();

        string? codeDisplay = NormalizeAcademicTitleCode(code);
        if (!string.IsNullOrWhiteSpace(codeDisplay))
            return codeDisplay;

        return name?.Trim() ?? string.Empty;
    }

    private static string? NormalizeAcademicTitleCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;

        string normalized = new(code.Trim().ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());

        return normalized switch
        {
            "PROFDR" => "Prof. Dr.",
            "ASSOCPROFDR" => "Doç. Dr.",
            "ASSTPROFDR" => "Dr. Öğr. Üyesi",
            "DR" => "Dr.",
            "LECTURER" => "Öğr. Gör.",
            "RESASST" => "Arş. Gör.",
            "RESEARCHER" => "Arş.",
            "SPEC" or "SPECIALIST" => "Uzm.",
            _ => null
        };
    }

    private static string? GetField(IDictionary<string, string?> fields, string key)
        => fields.TryGetValue(key, out string? value) ? value : null;
}
