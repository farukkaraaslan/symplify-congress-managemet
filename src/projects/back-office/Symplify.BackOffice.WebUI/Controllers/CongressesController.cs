using Core.Application.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.Auth.Queries.GetStatesByCountry;
using Symplify.BackOffice.Application.Features.Cities.Queries.GetList;
using Symplify.BackOffice.Application.Features.Congresses.Cloning;
using Symplify.BackOffice.Application.Features.Congresses.Commands.Create;
using Symplify.BackOffice.Application.Features.Congresses.Commands.Update;
using Symplify.BackOffice.Application.Features.Congresses.Queries.GetById;
using Symplify.BackOffice.Application.Features.Congresses.Queries.GetForUpdate;
using Symplify.BackOffice.Application.Features.Congresses.Queries.GetList;
using Symplify.BackOffice.Application.Features.Congresses.Queries.GetCloneSources;
using Symplify.BackOffice.Application.Features.Countries.Queries.GetList;
using Symplify.BackOffice.Application.Features.Organizations.Queries.GetList;
using Symplify.BackOffice.Application.Features.States.Queries.GetList;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Maintenance;
using Symplify.BackOffice.Domain.Enums;
using Symplify.BackOffice.WebUI.Localization;
using Symplify.BackOffice.WebUI.Extensions;
using Symplify.BackOffice.WebUI.Models.Congresses;
using Symplify.BackOffice.WebUI.Models.Shared.DataTables;

namespace Symplify.BackOffice.WebUI.Controllers;

[Authorize]
[Route("{culture=tr-TR}/[controller]/[action]")]
public sealed class CongressesController : Controller
{
    private const string SafeFallbackCulture = "tr-TR";

    private const string TitleField = "Title";
    private const string ShortTitleStorageField = "Subtitle";
    private const string SubtitleField = "Subtitle";
    private const string ShortDescriptionField = "ShortDescription";
    private const string WelcomeTitleField = "WelcomeTitle";
    private const string WelcomeContentField = "WelcomeContent";
    private const string SeoTitleField = "SeoTitle";
    private const string SeoDescriptionField = "SeoDescription";

    private readonly IMediator _mediator;
    private readonly IWebHostEnvironment _environment;
    private readonly IApplicationLanguageProvider _applicationLanguageProvider;
    private readonly IBackOfficeViewLocalizer _localizer;
    private readonly ICongressCleanupService _congressCleanupService;

    public CongressesController(
        IMediator mediator,
        IWebHostEnvironment environment,
        IApplicationLanguageProvider applicationLanguageProvider,
        IBackOfficeViewLocalizer localizer,
        ICongressCleanupService congressCleanupService)
    {
        _mediator = mediator;
        _environment = environment;
        _applicationLanguageProvider = applicationLanguageProvider;
        _localizer = localizer;
        _congressCleanupService = congressCleanupService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(Guid? organizationId, CancellationToken cancellationToken)
    {
        CongressesIndexViewModel model = new()
        {
            OrganizationId = organizationId,
            StatusFilter = new CongressStatusFilterViewModel
            {
                Value = (int)CongressStatus.Published
            },
            OrganizationOptions = await GetOrganizationOptionsAsync(organizationId, cancellationToken),
            StatusOptions = GetStatusFilterOptions(CongressStatus.Published)
        };

        if (organizationId.HasValue)
        {
            SelectListItem? selectedOrganization = model.OrganizationOptions.FirstOrDefault(item =>
                string.Equals(item.Value, organizationId.Value.ToString(), StringComparison.OrdinalIgnoreCase));

            model.OrganizationName = selectedOrganization?.Text;
        }

        return View(model);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Manage(Guid id, [FromQuery] string? tab, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
            return BadRequest();

        string culture = await ResolveCurrentCultureAsync(cancellationToken);

        GetByIdCongressResponse response = await _mediator.Send(
            new GetByIdCongressQuery
            {
                Id = id,
                Culture = culture
            },
            cancellationToken);

        ManageCongressViewModel model = new()
        {
            Id = response.Id,
            OrganizationId = response.OrganizationId,
            Code = response.Code,
            Name = response.Name,
            Title = response.Title,
            Subtitle = response.Subtitle,
            Description = response.Description,
            ShortDescription = response.ShortDescription,
            WelcomeTitle = response.WelcomeTitle,
            WelcomeContent = response.WelcomeContent,
            LogoLightPath = response.LogoLightPath,
            LogoDarkPath = response.LogoDarkPath,
            LogoLightUrl = response.LogoLightUrl,
            LogoDarkUrl = response.LogoDarkUrl,
            LogoPath = response.LogoLightPath,
            LogoUrl = response.LogoUrl,
            TranslationCultures = response.TranslationCultures,
            StartDate = response.StartDate,
            EndDate = response.EndDate,
            Status = response.Status,
            DisplayLanguageId = response.DisplayLanguageId,
            IsFallback = response.IsFallback,
            ActiveTab = NormalizeManageTab(tab)
        };

        return View("Manage", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GetList(
        [FromForm] DataTableRequest request,
        [FromForm] Guid? organizationId,
        [FromForm] int? status,
        CancellationToken cancellationToken)
    {
        string culture = await ResolveCurrentCultureAsync(cancellationToken);

        DataTableQueryOptions tableOptions = DataTableQueryOptions.From(
            request,
            defaultSortColumn: "startDate",
            defaultSortDirection: "desc",
            allowedSortColumns: new[] { "title", "code", "startDate", "endDate", "status" });

        CongressStatus statusFilter = ResolveStatusFilter(status);

        var response = await _mediator.Send(
            new GetListCongressQuery
            {
                OrganizationId = organizationId,
                Status = statusFilter,
                Culture = culture,
                SearchText = tableOptions.SearchText,
                SortColumn = tableOptions.SortColumn,
                SortDirection = tableOptions.SortDirection,
                PageRequest = new PageRequest
                {
                    Page = tableOptions.Page,
                    PageSize = tableOptions.PageSize
                }
            },
            cancellationToken);

        Dictionary<Guid, string> organizationNames = await GetOrganizationNameMapAsync(cancellationToken);

        return Json(new
        {
            draw = request.Draw,
            recordsTotal = response.Count,
            recordsFiltered = response.Count,
            data = response.Items.Select((item, index) => new
            {
                rowNumber = tableOptions.Start + index + 1,
                id = item.Id,
                organizationId = item.OrganizationId,
                organizationName = organizationNames.TryGetValue(item.OrganizationId, out string? organizationName) ? organizationName : string.Empty,
                code = item.Code,
                title = !string.IsNullOrWhiteSpace(item.Title) ? item.Title : item.Name,
                subtitle = item.Subtitle,
                logoLightPath = item.LogoLightPath,
                logoDarkPath = item.LogoDarkPath,
                logoLightUrl = item.LogoLightUrl,
                logoDarkUrl = item.LogoDarkUrl,
                logoUrl = item.LogoUrl,
                translationCultures = item.TranslationCultures,
                startDate = FormatDate(item.StartDate),
                endDate = FormatDate(item.EndDate),
                dateRange = FormatDateRange(item.StartDate, item.EndDate),
                venueName = item.VenueName,
                location = FormatLocation(item.VenueName),
                displayLanguageId = item.DisplayLanguageId,
                isFallback = item.IsFallback,
                status = item.Status.ToString(),
                statusName = item.Status.ToString(),
                statusValue = (int)item.Status,
                statusText = GetStatusText(item.Status),
                statusBadgeClass = GetStatusBadgeClass(item.Status),
                editUrl = Url.Action(nameof(Edit), "Congresses", new { culture = CurrentCulture(), id = item.Id }),
                manageUrl = Url.Action(nameof(Manage), "Congresses", new { culture = CurrentCulture(), id = item.Id, tab = "slider" })
                    ?? $"/{CurrentCulture()}/Congresses/Manage/{item.Id}?tab=slider",
                deleteInspectionUrl = Url.Action(nameof(DeleteInspection), "Congresses", new { culture = CurrentCulture(), id = item.Id }),
                deleteUrl = Url.Action(nameof(DeleteDocumentOnly), "Congresses", new { culture = CurrentCulture(), id = item.Id }),
                contactEmail = item.ContactEmail,
                contactPhone = item.ContactPhone
            })
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create(Guid? organizationId, CancellationToken cancellationToken)
    {
        CreateCongressViewModel model = await BuildCreateViewModelAsync(organizationId, cancellationToken);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> States(Guid countryId, CancellationToken cancellationToken)
    {
        if (countryId == Guid.Empty)
            return Json(Array.Empty<object>());

        var states = await _mediator.Send(
            new GetStatesByCountryQuery
            {
                CountryId = countryId,
                Culture = CurrentCulture()
            },
            cancellationToken);

        return Json(states);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [FromForm] CreateCongressViewModel model,
        [FromForm] string? submitMode,
        CancellationToken cancellationToken)
    {
        ValidateCreateModel(model);

        if (!ModelState.IsValid)
            return BadRequest(CreateValidationErrorResponse());

        try
        {
            CreateCongressCommand command = new()
            {
                OrganizationId = model.OrganizationId,
                EditionNumber = model.EditionNumber,
                StartDate = NormalizeDate(model.StartDate),
                EndDate = NormalizeDate(model.EndDate),
                Status = string.Equals(submitMode, "draft", StringComparison.OrdinalIgnoreCase)
                    ? CongressStatus.Draft
                    : model.Status,
                ContactName = NormalizeText(model.ContactName),
                ContactTitle = NormalizeText(model.ContactTitle),
                ContactPhone = NormalizeText(model.ContactPhone),
                ContactAddress = NormalizeText(model.ContactAddress),
                VenueName = NormalizeText(model.VenueName),
                CountryId = model.CountryId,
                StateId = model.StateId,
                ContactEmails = BuildContactEmailInputs(model.ContactEmails),
                CopyFromCongressId = model.CopyFromPreviousCongress
                    ? model.SourceCongressId
                    : null,
                ShiftRelativeDates = model.ShiftRelativeDates,
                CloneModules = model.CopyFromPreviousCongress
                    ? model.CloneModules
                    : new List<CongressCloneModule>(),
                Translations = BuildTranslationInputs(model.Translations)
            };

            await _mediator.Send(command, cancellationToken);

            string successMessage = GetText("BackOffice.Congresses.Messages.Created", "Kongre başarıyla oluşturuldu.");
            Guid? redirectOrganizationId = model.OrganizationId == Guid.Empty ? (Guid?)null : model.OrganizationId;
            string redirectUrl = Url.Action(
                nameof(Index),
                "Congresses",
                new { culture = CurrentCulture(), organizationId = redirectOrganizationId }) ?? $"/{CurrentCulture()}/congresses/index";

            return Json(new
            {
                success = true,
                message = successMessage,
                redirectUrl
            });
        }
        catch (Exception exception)
        {
            string message = GetExceptionMessage(exception);

            return BadRequest(new
            {
                success = false,
                message
            });
        }
    }


    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        UpdateCongressViewModel model = await BuildUpdateViewModelAsync(id, cancellationToken);
        return View(model);
    }

    [HttpPost("{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        Guid id,
        [FromForm] UpdateCongressViewModel model,
        [FromForm] string? submitMode,
        CancellationToken cancellationToken)
    {
        if (model.Id == Guid.Empty)
            model.Id = id;

        if (model.Id != id)
        {
            ModelState.AddModelError(
                nameof(model.Id),
                GetText("Common.InvalidRequest", "Geçersiz istek."));
        }

        ValidateUpdateModel(model);

        if (!ModelState.IsValid)
            return BadRequest(CreateValidationErrorResponse());

        try
        {
            UpdateCongressCommand command = new()
            {
                Id = model.Id,
                OrganizationId = model.OrganizationId,
                EditionNumber = model.EditionNumber,
                StartDate = NormalizeDate(model.StartDate),
                EndDate = NormalizeDate(model.EndDate),
                Status = string.Equals(submitMode, "draft", StringComparison.OrdinalIgnoreCase)
                    ? CongressStatus.Draft
                    : model.Status,
                ContactName = NormalizeText(model.ContactName),
                ContactTitle = NormalizeText(model.ContactTitle),
                ContactPhone = NormalizeText(model.ContactPhone),
                ContactAddress = NormalizeText(model.ContactAddress),
                VenueName = NormalizeText(model.VenueName),
                LogoLightPath = model.ExistingLogoLightPath,
                LogoDarkPath = model.ExistingLogoDarkPath,
                LogoLight = model.LogoLightFile.ToCongressLogoInputDto(),
                LogoDark = model.LogoDarkFile.ToCongressLogoInputDto(),
                CountryId = model.CountryId,
                StateId = model.StateId,
                ContactEmails = BuildUpdateContactEmailInputs(model.ContactEmails),
                Translations = BuildUpdateTranslationInputs(model.Translations)
            };

            await _mediator.Send(command, cancellationToken);

            string successMessage = GetText("BackOffice.Congresses.Messages.Updated", "Kongre başarıyla güncellendi.");
            Guid? redirectOrganizationId = model.OrganizationId == Guid.Empty ? (Guid?)null : model.OrganizationId;
            string redirectUrl = Url.Action(
                nameof(Index),
                "Congresses",
                new { culture = CurrentCulture(), organizationId = redirectOrganizationId }) ?? $"/{CurrentCulture()}/congresses/index";

            return Json(new
            {
                success = true,
                message = successMessage,
                redirectUrl
            });
        }
        catch (Exception exception)
        {
            string message = GetExceptionMessage(exception);

            return BadRequest(new
            {
                success = false,
                message
            });
        }
    }


    [HttpGet("{id:guid}")]
    public async Task<IActionResult> DeleteInspection(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
            return BadRequest(new { success = false, message = GetText("Common.InvalidRequest", "Geçersiz istek.") });

        try
        {
            CongressDeleteInspectionResult result = await _congressCleanupService
                .InspectDocumentOnlyCleanupAsync(id, cancellationToken);

            return Json(new
            {
                success = true,
                congressId = result.CongressId,
                code = result.Code,
                title = result.Title,
                status = result.Status,
                documentCount = result.DocumentCount,
                documentTranslationCount = result.DocumentTranslationCount,
                translationCount = result.TranslationCount,
                workflowSettingCount = result.WorkflowSettingCount,
                workflowTransitionCount = result.WorkflowTransitionCount,
                isSafe = result.IsSafeForDocumentOnlyDelete,
                blockingDependencies = result.BlockingDependencies.Select(item => new
                {
                    name = item.Key,
                    count = item.Value
                })
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

    [HttpPost("{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDocumentOnly(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
            return BadRequest(new { success = false, message = GetText("Common.InvalidRequest", "Geçersiz istek.") });

        try
        {
            CongressDeleteCleanupResult result = await _congressCleanupService
                .DeleteDocumentOnlyCongressAsync(id, cancellationToken);

            string message = string.Format(
                GetText(
                    "BackOffice.Congresses.Messages.DocumentOnlyDeleted",
                    "Kongre silindi. {0} doküman ve {1} MinIO objesi temizlendi."),
                result.DeletedDocumentCount,
                result.DeletedStorageObjectCount);

            return Json(new
            {
                success = true,
                message,
                congressId = result.CongressId,
                code = result.Code,
                title = result.Title,
                deletedDocumentCount = result.DeletedDocumentCount,
                deletedTranslationCount = result.DeletedTranslationCount,
                deletedWorkflowRecordCount = result.DeletedWorkflowRecordCount,
                deletedStorageObjectCount = result.DeletedStorageObjectCount
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

    private async Task<UpdateCongressViewModel> BuildUpdateViewModelAsync(Guid id, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new GetCongressForUpdateQuery { Id = id }, cancellationToken);

        UpdateCongressViewModel model = new()
        {
            Id = response.Id,
            OrganizationId = response.OrganizationId,
            Code = response.Code,
            EditionNumber = response.EditionNumber,
            StartDate = response.StartDate,
            EndDate = response.EndDate,
            Status = response.Status,
            ContactName = response.ContactName,
            ContactTitle = response.ContactTitle,
            ContactPhone = response.ContactPhone,
            ContactAddress = response.ContactAddress,
            VenueName = response.VenueName,
            ExistingLogoLightPath = response.LogoLightPath,
            ExistingLogoDarkPath = response.LogoDarkPath,
            ExistingLogoLightUrl = response.LogoLightUrl,
            ExistingLogoDarkUrl = response.LogoDarkUrl,
            CountryId = response.CountryId,
            StateId = response.StateId,
            ContactEmails = response.ContactEmails.Count > 0
                ? response.ContactEmails
                    .OrderByDescending(item => item.IsPrimary)
                    .ThenBy(item => item.Order)
                    .Select(item => new CreateCongressContactEmailViewModel
                    {
                        Label = item.Label,
                        Email = item.Email,
                        IsPrimary = item.IsPrimary,
                        IsVisibleOnPortal = item.IsVisibleOnPortal,
                        ReceivesContactMessages = item.ReceivesContactMessages,
                        Order = item.Order
                    })
                    .ToList()
                : string.IsNullOrWhiteSpace(response.ContactEmail)
                    ? new List<CreateCongressContactEmailViewModel>
                    {
                        new()
                        {
                            Label = "Genel Bilgi",
                            IsPrimary = true,
                            IsVisibleOnPortal = true,
                            ReceivesContactMessages = true,
                            Order = 0
                        }
                    }
                    : new List<CreateCongressContactEmailViewModel>
                    {
                        new()
                        {
                            Label = "Genel Bilgi",
                            Email = response.ContactEmail,
                            IsPrimary = true,
                            IsVisibleOnPortal = true,
                            ReceivesContactMessages = true,
                            Order = 0
                        }
                    },
            Translations = response.Translations
                .OrderByDescending(translation => translation.IsDefault)
                .ThenBy(translation => translation.LanguageName)
                .Select(translation => new UpdateCongressTranslationViewModel
                {
                    LanguageId = translation.LanguageId,
                    Culture = translation.Culture,
                    LanguageName = translation.LanguageName,
                    IsDefault = translation.IsDefault,
                    Exists = translation.Exists,
                    Title = GetField(translation.Fields, TitleField),
                    ShortTitle = GetField(translation.Fields, ShortTitleStorageField),
                    WelcomeContent = GetField(translation.Fields, WelcomeContentField),
                    SeoTitle = GetField(translation.Fields, SeoTitleField),
                    SeoDescription = GetField(translation.Fields, SeoDescriptionField)
                })
                .ToList()
        };

        await PopulateUpdateLookupOptionsAsync(model, cancellationToken);

        return model;
    }

    private async Task PopulateUpdateLookupOptionsAsync(UpdateCongressViewModel model, CancellationToken cancellationToken)
    {
        model.OrganizationOptions = await GetOrganizationOptionsAsync(model.OrganizationId == Guid.Empty ? (Guid?)null : model.OrganizationId, cancellationToken);
        model.CountryOptions = await GetCountryOptionsAsync(model.CountryId, cancellationToken);
        model.StateOptions = await GetStateOptionsAsync(model.StateId, model.CountryId, cancellationToken);
        model.StatusOptions = GetStatusOptions(model.Status);
    }

    private void ValidateUpdateModel(UpdateCongressViewModel model)
    {
        if (model.Id == Guid.Empty)
        {
            ModelState.AddModelError(
                nameof(model.Id),
                GetText("Common.InvalidRequest", "Geçersiz istek."));
        }

        if (model.OrganizationId == Guid.Empty)
        {
            ModelState.AddModelError(
                nameof(model.OrganizationId),
                GetText("BackOffice.Congresses.Validation.OrganizationRequired", "Organizasyon seçimi zorunludur."));
        }

        if (!model.StartDate.HasValue)
        {
            ModelState.AddModelError(
                nameof(model.StartDate),
                GetText("BackOffice.Congresses.Validation.StartDateRequired", "Başlangıç tarihi zorunludur."));
        }

        if (!model.EndDate.HasValue)
        {
            ModelState.AddModelError(
                nameof(model.EndDate),
                GetText("BackOffice.Congresses.Validation.EndDateRequired", "Bitiş tarihi zorunludur."));
        }

        if (model.StartDate.HasValue && model.EndDate.HasValue && model.EndDate.Value.Date < model.StartDate.Value.Date)
        {
            ModelState.AddModelError(
                nameof(model.EndDate),
                GetText("BackOffice.Congresses.Validation.DateRangeInvalid", "Bitiş tarihi başlangıç tarihinden önce olamaz."));
        }

        model.ContactEmails ??= new List<CreateCongressContactEmailViewModel>();

        List<CreateCongressContactEmailViewModel> contactEmails = model.ContactEmails
            .Where(item => !string.IsNullOrWhiteSpace(item.Email))
            .ToList();

        if (contactEmails.Count == 0)
        {
            ModelState.AddModelError(
                nameof(model.ContactEmails),
                "En az bir geçerli kongre iletişim e-posta adresi girilmelidir.");
        }

        if (contactEmails.Count(item => item.IsPrimary) > 1)
        {
            ModelState.AddModelError(
                nameof(model.ContactEmails),
                "Yalnızca bir e-posta adresi ana adres olarak seçilebilir.");
        }

        HashSet<string> uniqueEmails = new(StringComparer.OrdinalIgnoreCase);

        for (int index = 0; index < model.ContactEmails.Count; index++)
        {
            CreateCongressContactEmailViewModel item = model.ContactEmails[index];

            if (string.IsNullOrWhiteSpace(item.Email))
                continue;

            if (!System.Net.Mail.MailAddress.TryCreate(item.Email.Trim(), out _))
            {
                ModelState.AddModelError(
                    $"ContactEmails[{index}].Email",
                    "Geçerli bir e-posta adresi giriniz.");
            }

            if (!uniqueEmails.Add(item.Email.Trim()))
            {
                ModelState.AddModelError(
                    $"ContactEmails[{index}].Email",
                    "Aynı e-posta adresi birden fazla kez eklenemez.");
            }
        }

        int defaultLanguageIndex = model.Translations.FindIndex(translation => translation.IsDefault);

        if (defaultLanguageIndex < 0)
        {
            ModelState.AddModelError(nameof(model.Translations), GetText("Common.InvalidRequest", "Geçersiz istek."));
            return;
        }

        UpdateCongressTranslationViewModel defaultTranslation = model.Translations[defaultLanguageIndex];

        if (string.IsNullOrWhiteSpace(defaultTranslation.Title))
        {
            ModelState.AddModelError(
                $"Translations[{defaultLanguageIndex}].Title",
                GetText("BackOffice.Congresses.Validation.TitleRequired", "Varsayılan dilde kongre başlığı zorunludur."));
        }

        if (string.IsNullOrWhiteSpace(defaultTranslation.WelcomeContent))
        {
            ModelState.AddModelError(
                $"Translations[{defaultLanguageIndex}].WelcomeContent",
                GetText("BackOffice.Congresses.Validation.WelcomeContentRequired", "Varsayılan dilde karşılama yazısı zorunludur."));
        }
    }

    private async Task<CreateCongressViewModel> BuildCreateViewModelAsync(Guid? organizationId, CancellationToken cancellationToken)
    {
        IReadOnlyList<ApplicationLanguageDto> languages = await _applicationLanguageProvider
            .GetActiveLanguagesAsync(cancellationToken);

        List<ApplicationLanguageDto> orderedLanguages = languages
            .OrderByDescending(language => language.IsDefault)
            .ThenBy(language => language.Name)
            .ToList();

        CreateCongressViewModel model = new()
        {
            OrganizationId = organizationId ?? Guid.Empty,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date,
            Status = CongressStatus.Draft,
            ShiftRelativeDates = true,
            CloneModules = Enum.GetValues<CongressCloneModule>().ToList(),
            ContactEmails = new List<CreateCongressContactEmailViewModel>
            {
                new()
                {
                    Label = "Genel Bilgi",
                    IsPrimary = true,
                    IsVisibleOnPortal = true,
                    ReceivesContactMessages = true,
                    Order = 0
                }
            },
            Translations = orderedLanguages.Select(language => new CreateCongressTranslationViewModel
            {
                LanguageId = language.Id,
                Culture = language.Culture,
                LanguageName = language.Name,
                IsDefault = language.IsDefault
            }).ToList()
        };

        await PopulateCreateLookupOptionsAsync(model, cancellationToken);

        return model;
    }

    private async Task PopulateCreateLookupOptionsAsync(CreateCongressViewModel model, CancellationToken cancellationToken)
    {
        model.OrganizationOptions = await GetOrganizationOptionsAsync(model.OrganizationId == Guid.Empty ? (Guid?)null : model.OrganizationId, cancellationToken);
        model.CountryOptions = await GetCountryOptionsAsync(model.CountryId, cancellationToken);
        model.StateOptions = await GetStateOptionsAsync(model.StateId, model.CountryId, cancellationToken);
        model.StatusOptions = GetStatusOptions(model.Status);

        IReadOnlyList<GetCongressCloneSourceListItemDto> cloneSources =
            await _mediator.Send(
                new GetCongressCloneSourceListQuery(),
                cancellationToken);

        model.CloneSourceOptions = cloneSources
            .Select(source => new CongressCloneSourceOptionViewModel
            {
                Id = source.Id,
                OrganizationId = source.OrganizationId,
                Text = BuildCloneSourceText(source)
            })
            .ToList();
    }

    private void ValidateCreateModel(CreateCongressViewModel model)
    {
        if (model.OrganizationId == Guid.Empty)
        {
            ModelState.AddModelError(
                nameof(model.OrganizationId),
                GetText("BackOffice.Congresses.Validation.OrganizationRequired", "Organizasyon seçimi zorunludur."));
        }

        if (model.CopyFromPreviousCongress)
        {
            if (!model.SourceCongressId.HasValue ||
                model.SourceCongressId.Value == Guid.Empty)
            {
                ModelState.AddModelError(
                    nameof(model.SourceCongressId),
                    "Kopyalanacak kaynak kongre seçilmelidir.");
            }

            if (model.CloneModules.Count == 0)
            {
                ModelState.AddModelError(
                    nameof(model.CloneModules),
                    "Kopyalanacak en az bir alan seçilmelidir.");
            }
        }

        if (!model.StartDate.HasValue)
        {
            ModelState.AddModelError(
                nameof(model.StartDate),
                GetText("BackOffice.Congresses.Validation.StartDateRequired", "Başlangıç tarihi zorunludur."));
        }

        if (!model.EndDate.HasValue)
        {
            ModelState.AddModelError(
                nameof(model.EndDate),
                GetText("BackOffice.Congresses.Validation.EndDateRequired", "Bitiş tarihi zorunludur."));
        }

        if (model.StartDate.HasValue && model.EndDate.HasValue && model.EndDate.Value.Date < model.StartDate.Value.Date)
        {
            ModelState.AddModelError(
                nameof(model.EndDate),
                GetText("BackOffice.Congresses.Validation.DateRangeInvalid", "Bitiş tarihi başlangıç tarihinden önce olamaz."));
        }

        bool copiesGeneralInformation =
            model.CopyFromPreviousCongress &&
            model.CloneModules.Contains(CongressCloneModule.GeneralInformation);

        model.ContactEmails ??= new List<CreateCongressContactEmailViewModel>();

        List<CreateCongressContactEmailViewModel> contactEmails = model.ContactEmails
            .Where(item => !string.IsNullOrWhiteSpace(item.Email))
            .ToList();

        if (!copiesGeneralInformation && contactEmails.Count == 0)
        {
            ModelState.AddModelError(
                nameof(model.ContactEmails),
                "En az bir geçerli kongre iletişim e-posta adresi girilmelidir.");
        }

        if (contactEmails.Count(item => item.IsPrimary) > 1)
        {
            ModelState.AddModelError(
                nameof(model.ContactEmails),
                "Yalnızca bir e-posta adresi ana adres olarak seçilebilir.");
        }

        HashSet<string> uniqueEmails = new(StringComparer.OrdinalIgnoreCase);

        for (int index = 0; index < model.ContactEmails.Count; index++)
        {
            CreateCongressContactEmailViewModel item = model.ContactEmails[index];

            if (string.IsNullOrWhiteSpace(item.Email))
                continue;

            if (!System.Net.Mail.MailAddress.TryCreate(item.Email.Trim(), out _))
            {
                ModelState.AddModelError(
                    $"ContactEmails[{index}].Email",
                    "Geçerli bir e-posta adresi giriniz.");
            }

            if (!uniqueEmails.Add(item.Email.Trim()))
            {
                ModelState.AddModelError(
                    $"ContactEmails[{index}].Email",
                    "Aynı e-posta adresi birden fazla kez eklenemez.");
            }
        }

        int defaultLanguageIndex = model.Translations.FindIndex(translation => translation.IsDefault);

        if (defaultLanguageIndex < 0)
        {
            ModelState.AddModelError(nameof(model.Translations), GetText("Common.InvalidRequest", "Geçersiz istek."));
            return;
        }

        CreateCongressTranslationViewModel defaultTranslation = model.Translations[defaultLanguageIndex];

        if (string.IsNullOrWhiteSpace(defaultTranslation.Title))
        {
            ModelState.AddModelError(
                $"Translations[{defaultLanguageIndex}].Title",
                GetText("BackOffice.Congresses.Validation.TitleRequired", "Varsayılan dilde kongre başlığı zorunludur."));
        }

        if (!copiesGeneralInformation &&
            string.IsNullOrWhiteSpace(defaultTranslation.WelcomeContent))
        {
            ModelState.AddModelError(
                $"Translations[{defaultLanguageIndex}].WelcomeContent",
                GetText("BackOffice.Congresses.Validation.WelcomeContentRequired", "Varsayılan dilde karşılama yazısı zorunludur."));
        }
    }

    private ICollection<TranslationInputDto> BuildTranslationInputs(IReadOnlyCollection<CreateCongressTranslationViewModel> translations)
    {
        return translations
            .GroupBy(translation => translation.LanguageId)
            .Select(group => group.First())
            .Where(HasAnyTranslationValue)
            .Select(translation => new TranslationInputDto
            {
                LanguageId = translation.LanguageId,
                Fields = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    [TitleField] = NormalizeText(translation.Title),
                    [ShortTitleStorageField] = NormalizeText(translation.ShortTitle),
                    [WelcomeContentField] = NormalizeText(translation.WelcomeContent),
                    [SeoTitleField] = NormalizeText(translation.SeoTitle),
                    [SeoDescriptionField] = NormalizeText(translation.SeoDescription)
                }
            })
            .ToList();
    }


    private static ICollection<CreateCongressContactEmailInputDto> BuildContactEmailInputs(
        IReadOnlyCollection<CreateCongressContactEmailViewModel> contactEmails)
    {
        return contactEmails
            .Where(item => !string.IsNullOrWhiteSpace(item.Email))
            .Select((item, index) => new CreateCongressContactEmailInputDto
            {
                Label = NormalizeText(item.Label),
                Email = item.Email!.Trim(),
                IsPrimary = item.IsPrimary,
                IsVisibleOnPortal = item.IsVisibleOnPortal,
                ReceivesContactMessages = item.ReceivesContactMessages,
                Order = index
            })
            .ToList();
    }

    private static ICollection<UpdateCongressContactEmailInputDto> BuildUpdateContactEmailInputs(
        IReadOnlyCollection<CreateCongressContactEmailViewModel> contactEmails)
    {
        return contactEmails
            .Where(item => !string.IsNullOrWhiteSpace(item.Email))
            .Select((item, index) => new UpdateCongressContactEmailInputDto
            {
                Label = NormalizeText(item.Label),
                Email = item.Email!.Trim(),
                IsPrimary = item.IsPrimary,
                IsVisibleOnPortal = item.IsVisibleOnPortal,
                ReceivesContactMessages = item.ReceivesContactMessages,
                Order = index
            })
            .ToList();
    }

    private ICollection<TranslationInputDto> BuildUpdateTranslationInputs(IReadOnlyCollection<UpdateCongressTranslationViewModel> translations)
    {
        return translations
            .GroupBy(translation => translation.LanguageId)
            .Select(group => group.First())
            .Where(HasAnyUpdateTranslationValue)
            .Select(translation => new TranslationInputDto
            {
                LanguageId = translation.LanguageId,
                Fields = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    [TitleField] = NormalizeText(translation.Title),
                    [ShortTitleStorageField] = NormalizeText(translation.ShortTitle),
                    [WelcomeContentField] = NormalizeText(translation.WelcomeContent),
                    [SeoTitleField] = NormalizeText(translation.SeoTitle),
                    [SeoDescriptionField] = NormalizeText(translation.SeoDescription)
                }
            })
            .ToList();
    }

    private static bool HasAnyUpdateTranslationValue(UpdateCongressTranslationViewModel translation)
    {
        return !string.IsNullOrWhiteSpace(translation.Title) ||
               !string.IsNullOrWhiteSpace(translation.ShortTitle) ||
               !string.IsNullOrWhiteSpace(translation.WelcomeContent) ||
               !string.IsNullOrWhiteSpace(translation.SeoTitle) ||
               !string.IsNullOrWhiteSpace(translation.SeoDescription);
    }

    private static string? GetField(IReadOnlyDictionary<string, string?> fields, string key)
    {
        return fields.TryGetValue(key, out string? value) ? value : null;
    }

    private static bool HasAnyTranslationValue(CreateCongressTranslationViewModel translation)
    {
        return !string.IsNullOrWhiteSpace(translation.Title) ||
               !string.IsNullOrWhiteSpace(translation.ShortTitle) ||
               !string.IsNullOrWhiteSpace(translation.WelcomeContent) ||
               !string.IsNullOrWhiteSpace(translation.SeoTitle) ||
               !string.IsNullOrWhiteSpace(translation.SeoDescription);
    }

    private static string BuildCloneSourceText(
        GetCongressCloneSourceListItemDto source)
    {
        string dateText = source.StartDate.HasValue
            ? source.StartDate.Value.ToString("yyyy")
            : "-";

        string editionText = source.EditionNumber.HasValue
            ? $"{source.EditionNumber.Value}. Kongre"
            : "Kongre";

        return $"{source.Name} ({source.Code}) · {editionText} · {dateText}";
    }

    private async Task<List<SelectListItem>> GetOrganizationOptionsAsync(Guid? selectedOrganizationId, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _mediator.Send(
                new GetListOrganizationQuery
                {
                    PageRequest = new PageRequest { Page = 0, PageSize = 500 },
                    SortColumn = "name",
                    SortDirection = "asc"
                },
                cancellationToken);

            return response.Items
                .Where(item => item.IsActive)
                .OrderBy(item => item.Name)
                .Select(item => new SelectListItem
                {
                    Value = item.Id.ToString(),
                    Text = string.IsNullOrWhiteSpace(item.ShortName)
                        ? $"{item.Name} ({item.Code})"
                        : $"{item.ShortName} - {item.Name}",
                    Selected = selectedOrganizationId.HasValue && item.Id == selectedOrganizationId.Value
                })
                .ToList();
        }
        catch
        {
            return new List<SelectListItem>();
        }
    }

    private async Task<Dictionary<Guid, string>> GetOrganizationNameMapAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await _mediator.Send(
                new GetListOrganizationQuery
                {
                    PageRequest = new PageRequest { Page = 0, PageSize = 500 },
                    SortColumn = "name",
                    SortDirection = "asc"
                },
                cancellationToken);

            return response.Items.ToDictionary(
                item => item.Id,
                item => string.IsNullOrWhiteSpace(item.ShortName) ? item.Name : item.ShortName);
        }
        catch
        {
            return new Dictionary<Guid, string>();
        }
    }

    private async Task<List<SelectListItem>> GetCountryOptionsAsync(Guid? selectedId, CancellationToken cancellationToken)
    {
        try
        {
            string culture = await ResolveCurrentCultureAsync(cancellationToken);
            var response = await _mediator.Send(new GetListCountryQuery
            {
                Culture = culture,
                PageRequest = new PageRequest { Page = 0, PageSize = 500 }
            }, cancellationToken);

            return response.Items.Select(item => new SelectListItem
            {
                Value = item.Id.ToString(),
                Text = item.Name,
                Selected = selectedId.HasValue && item.Id == selectedId.Value
            }).ToList();
        }
        catch
        {
            return new List<SelectListItem>();
        }
    }

    private async Task<List<SelectListItem>> GetStateOptionsAsync(Guid? selectedId, CancellationToken cancellationToken)
    {
        try
        {
            string culture = await ResolveCurrentCultureAsync(cancellationToken);
            var response = await _mediator.Send(new GetListStateQuery
            {
                Culture = culture,
                PageRequest = new PageRequest { Page = 0, PageSize = 500 }
            }, cancellationToken);

            return response.Items.Select(item => new SelectListItem
            {
                Value = item.Id.ToString(),
                Text = item.Name,
                Selected = selectedId.HasValue && item.Id == selectedId.Value
            }).ToList();
        }
        catch
        {
            return new List<SelectListItem>();
        }
    }

    private async Task<List<SelectListItem>> GetStateOptionsAsync(
        Guid? selectedId,
        Guid? countryId,
        CancellationToken cancellationToken)
    {
        if (!countryId.HasValue || countryId.Value == Guid.Empty)
            return new List<SelectListItem>();

        try
        {
            var states = await _mediator.Send(
                new GetStatesByCountryQuery
                {
                    CountryId = countryId.Value,
                    Culture = CurrentCulture()
                },
                cancellationToken);

            return states.Select(item => new SelectListItem
            {
                Value = item.Value,
                Text = item.Text,
                Selected = selectedId.HasValue &&
                           Guid.TryParse(item.Value, out Guid stateId) &&
                           stateId == selectedId.Value
            }).ToList();
        }
        catch
        {
            return new List<SelectListItem>();
        }
    }

    private async Task<List<SelectListItem>> GetCityOptionsAsync(Guid? selectedId, CancellationToken cancellationToken)
    {
        try
        {
            string culture = await ResolveCurrentCultureAsync(cancellationToken);
            var response = await _mediator.Send(new GetListCityQuery
            {
                Culture = culture,
                PageRequest = new PageRequest { Page = 0, PageSize = 500 }
            }, cancellationToken);

            return response.Items.Select(item => new SelectListItem
            {
                Value = item.Id.ToString(),
                Text = item.Name,
                Selected = selectedId.HasValue && item.Id == selectedId.Value
            }).ToList();
        }
        catch
        {
            return new List<SelectListItem>();
        }
    }

    private static CongressStatus ResolveStatusFilter(int? rawStatus)
    {
        if (rawStatus.HasValue && Enum.IsDefined(typeof(CongressStatus), rawStatus.Value))
            return (CongressStatus)rawStatus.Value;

        return CongressStatus.Published;
    }

    private List<SelectListItem> GetStatusFilterOptions(CongressStatus selectedStatus)
    {
        return Enum.GetValues<CongressStatus>()
            .Select(status => new SelectListItem
            {
                Value = ((int)status).ToString(),
                Text = GetStatusText(status),
                Selected = status == selectedStatus
            })
            .ToList();
    }

    private List<SelectListItem> GetStatusOptions(CongressStatus selectedStatus)
    {
        return Enum.GetValues<CongressStatus>()
            .Select(status => new SelectListItem
            {
                Value = ((int)status).ToString(),
                Text = GetStatusText(status),
                Selected = status == selectedStatus
            })
            .ToList();
    }

    private string GetStatusText(CongressStatus status)
    {
        return status switch
        {
            CongressStatus.Draft => GetText("BackOffice.Congresses.Status.Draft", "Taslak"),
            CongressStatus.Published => GetText("BackOffice.Congresses.Status.Published", "Yayında"),
            CongressStatus.Archived => GetText("BackOffice.Congresses.Status.Archived", "Arşivde"),
            CongressStatus.Cancelled => GetText("BackOffice.Congresses.Status.Cancelled", "İptal"),
            _ => status.ToString()
        };
    }

    private static string GetStatusBadgeClass(CongressStatus status)
    {
        return status switch
        {
            CongressStatus.Draft => "bg-warning-light text-warning",
            CongressStatus.Published => "bg-success-light text-success",
            CongressStatus.Archived => "bg-neutral-200 text-neutral-700",
            CongressStatus.Cancelled => "bg-danger-light text-danger",
            _ => "bg-neutral-200 text-neutral-700"
        };
    }

    private static string NormalizeManageTab(string? tab)
    {
        if (string.IsNullOrWhiteSpace(tab))
            return "congress";

        return tab.Trim().ToLowerInvariant() switch
        {
            "slider" or "sliders" => "slider",
            "sections" or "section" => "sections",
            "announcements" or "announcement" => "announcements",
            "boards" or "board" or "committees" or "committee" => "boards",
            "important-dates" or "dates" => "important-dates",
            "documents" or "document" => "documents",
            "workflow" or "workflows" or "congress-workflow" or "congress-workflows" => "workflow",
            "congress" => "congress",
            "topics" or "topic" => "topics",
            "submission-types" or "submissiontype" or "submissiontypes" or "submission_types" => "submission-types",
            _ => "congress"
        };
    }

    private async Task<string> ResolveCurrentCultureAsync(CancellationToken cancellationToken)
    {
        string? formCulture = Request.HasFormContentType ? Request.Form["culture"].FirstOrDefault() : null;

        if (!string.IsNullOrWhiteSpace(formCulture))
            return await NormalizeCultureFromApplicationLanguagesAsync(formCulture, cancellationToken);

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
            : activeLanguages.OrderByDescending(language => language.IsDefault).ThenBy(language => language.Name).FirstOrDefault()?.Culture ?? SafeFallbackCulture;
    }

    private static string GetTwoLetterIsoCode(string? culture)
    {
        if (string.IsNullOrWhiteSpace(culture))
            return string.Empty;

        string normalizedCulture = culture.Trim();
        int separatorIndex = normalizedCulture.IndexOf('-');

        return separatorIndex > 0 ? normalizedCulture[..separatorIndex] : normalizedCulture;
    }


    private async Task<string?> SaveLogoAsync(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return null;

        string extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!new[] { ".png", ".jpg", ".jpeg", ".svg", ".webp" }.Contains(extension))
            throw new InvalidOperationException(GetText("BackOffice.Congresses.Validation.InvalidLogo", "Logo PNG, JPG, WEBP veya SVG olmalıdır."));

        string directory = Path.Combine(_environment.WebRootPath, "uploads", "congresses", "logos");
        Directory.CreateDirectory(directory);

        string fileName = $"{Guid.NewGuid():N}{extension}";
        string physicalPath = Path.Combine(directory, fileName);

        await using FileStream stream = System.IO.File.Create(physicalPath);
        await file.CopyToAsync(stream, cancellationToken);

        return $"/uploads/congresses/logos/{fileName}";
    }

    private string CurrentCulture()
    {
        return RouteData.Values["culture"]?.ToString()
               ?? HttpContext.Request.Path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()
               ?? SafeFallbackCulture;
    }

    private string GetText(string key, string fallback)
    {
        string value = _localizer.GetStringValue(key);

        return string.IsNullOrWhiteSpace(value) || string.Equals(value, key, StringComparison.OrdinalIgnoreCase)
            ? fallback
            : value;
    }

    private string GetExceptionMessage(Exception exception)
    {
        if (string.IsNullOrWhiteSpace(exception.Message))
            return GetText("Common.GenericError", "İşlem sırasında bir hata oluştu.");

        return GetText(exception.Message, exception.Message);
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
                    item => item.Value!.Errors.Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage) ? GetText("Common.InvalidRequest", "Geçersiz değer.") : error.ErrorMessage).ToArray())
        };
    }


    private static string? NormalizeText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static DateTime? NormalizeDate(DateTime? value)
    {
        if (!value.HasValue)
            return null;

        return DateTime.SpecifyKind(value.Value.Date, DateTimeKind.Utc);
    }

    private static string FormatDate(DateTime? value)
    {
        return value.HasValue ? value.Value.ToString("dd.MM.yyyy") : "-";
    }

    private static string FormatDateRange(DateTime? startDate, DateTime? endDate)
    {
        return $"{FormatDate(startDate)} - {FormatDate(endDate)}";
    }

    private static string FormatLocation(string? venueName)
    {
        return string.IsNullOrWhiteSpace(venueName) ? "-" : venueName.Trim();
    }
}
