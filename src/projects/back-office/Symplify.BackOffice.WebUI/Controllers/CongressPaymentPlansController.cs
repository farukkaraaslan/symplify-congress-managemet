using System.Globalization;
using Core.Application.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.CongressPaymentPlans.Commands.Create;
using Symplify.BackOffice.Application.Features.CongressPaymentPlans.Commands.Delete;
using Symplify.BackOffice.Application.Features.CongressPaymentPlans.Commands.Update;
using Symplify.BackOffice.Application.Features.CongressPaymentPlans.Constants;
using Symplify.BackOffice.Application.Features.CongressPaymentPlans.Queries.GetForUpdate;
using Symplify.BackOffice.Application.Features.CongressPaymentPlans.Queries.GetList;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.WebUI.Localization;
using Symplify.BackOffice.WebUI.Models.CongressPaymentPlans;
using Symplify.BackOffice.WebUI.Models.Shared.DataTables;

namespace Symplify.BackOffice.WebUI.Controllers;

[Authorize]
[Route("{culture=tr-TR}/[controller]/[action]")]
public sealed class CongressPaymentPlansController : Controller
{
    private const string SafeFallbackCulture = "tr-TR";
    private const string NameField = "Name";
    private const string DescriptionField = "Description";

    private static readonly string[] CurrencyValues = { "TRY", "USD", "EUR" };

    private readonly IMediator _mediator;
    private readonly IApplicationLanguageProvider _applicationLanguageProvider;
    private readonly IBackOfficeViewLocalizer _localizer;

    public CongressPaymentPlansController(
        IMediator mediator,
        IApplicationLanguageProvider applicationLanguageProvider,
        IBackOfficeViewLocalizer localizer)
    {
        _mediator = mediator;
        _applicationLanguageProvider = applicationLanguageProvider;
        _localizer = localizer;
    }


    [HttpGet]
    public IActionResult Index()
    {
        string culture = RouteData.Values["culture"]?.ToString() ?? SafeFallbackCulture;
        return RedirectToAction("Index", "Congresses", new { culture });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GetList(
        [FromForm] DataTableRequest request,
        [FromForm] Guid congressId,
        [FromForm] string? audienceType,
        [FromForm] string? paymentCategory,
        [FromForm] string? publicVisibility,
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
                data = Array.Empty<object>()
            });
        }

        DataTableQueryOptions tableOptions = DataTableQueryOptions.From(
            request,
            defaultSortColumn: "order",
            defaultSortDirection: "asc",
            allowedSortColumns: new[]
            {
                "order", "name", "audienceType", "paymentCategory", "amount", "currency", "validUntil", "isPublicVisible", "isActive"
            });

        string culture = await ResolveCurrentCultureAsync(cancellationToken);

        var response = await _mediator.Send(
            new GetListCongressPaymentPlanQuery
            {
                CongressId = congressId,
                Culture = culture,
                AudienceType = NormalizeEmpty(audienceType),
                PaymentCategory = NormalizeEmpty(paymentCategory),
                IsPublicVisible = ParseNullableBoolean(publicVisibility),
                IsActive = ParseNullableBoolean(status),
                BypassCache = true,
                PageRequest = new PageRequest { Page = 0, PageSize = 10000 }
            },
            cancellationToken);

        List<GetListCongressPaymentPlanListItemDto> items = response.Items.ToList();

        if (!string.IsNullOrWhiteSpace(tableOptions.SearchText))
            items = ApplySearch(items, tableOptions.SearchText!).ToList();

        int filteredCount = items.Count;
        items = ApplySort(items, tableOptions.SortColumn, tableOptions.SortDirection).ToList();

        List<GetListCongressPaymentPlanListItemDto> pageItems = items
            .Skip(tableOptions.Start)
            .Take(tableOptions.PageSize)
            .ToList();

        return Json(new
        {
            draw = request.Draw,
            recordsTotal = response.Count,
            recordsFiltered = filteredCount,
            data = pageItems.Select((item, index) => new
            {
                rowNumber = tableOptions.Start + index + 1,
                id = item.Id,
                congressId = item.CongressId,
                code = item.Code,
                name = item.Name,
                description = item.Description,
                amount = item.Amount,
                amountText = FormatAmount(item.Amount),
                currency = item.Currency,
                audienceType = item.AudienceType,
                audienceTypeText = GetAudienceTypeText(item.AudienceType),
                paymentCategory = item.PaymentCategory,
                paymentCategoryText = GetPaymentCategoryText(item.PaymentCategory),
                dueDate = FormatDate(item.DueDate),
                validFrom = FormatDate(item.ValidFrom),
                validUntil = FormatDate(item.ValidUntil),
                validityText = FormatValidityRange(item.ValidFrom, item.ValidUntil),
                order = item.Order,
                isPublicVisible = item.IsPublicVisible,
                isActive = item.IsActive,
                isFallback = item.IsFallback
            })
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetFilterOptions(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        return Json(new
        {
            audienceTypes = BuildAudienceTypeOptions().Select(option => new { value = option.Value, text = option.Text }),
            paymentCategories = BuildPaymentCategoryOptions().Select(option => new { value = option.Value, text = option.Text }),
            currencies = BuildCurrencyOptions().Select(option => new { value = option.Value, text = option.Text })
        });
    }

    [HttpGet]
    public async Task<IActionResult> CreateModal(Guid congressId, CancellationToken cancellationToken)
    {
        CreateCongressPaymentPlanViewModel model = new()
        {
            CongressId = congressId,
            Amount = 0,
            Currency = "TRY",
            AudienceType = CongressPaymentPlanAudienceTypes.All,
            PaymentCategory = CongressPaymentPlanCategories.Participation,
            IsPublicVisible = true,
            IsActive = true,
            AudienceTypeOptions = BuildAudienceTypeOptions(),
            PaymentCategoryOptions = BuildPaymentCategoryOptions(),
            CurrencyOptions = BuildCurrencyOptions(),
            Translations = await BuildTranslationViewModelsAsync(cancellationToken)
        };

        return PartialView("~/Views/CongressPaymentPlans/_CreatePaymentPlanModal.cshtml", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] CreateCongressPaymentPlanViewModel model, CancellationToken cancellationToken)
    {
        ParsedPaymentPlanDates dates = ValidateCreateModel(model);

        if (!ModelState.IsValid)
            return BadRequest(CreateValidationErrorResponse());

        try
        {
            CreatedCongressPaymentPlanResponse response = await _mediator.Send(
                new CreateCongressPaymentPlanCommand
                {
                    CongressId = model.CongressId,
                    Code = NormalizeText(model.Code),
                    Amount = model.Amount,
                    Currency = model.Currency,
                    AudienceType = model.AudienceType,
                    PaymentCategory = model.PaymentCategory,
                    DueDate = dates.DueDate,
                    ValidFrom = dates.ValidFrom,
                    ValidUntil = dates.ValidUntil,
                    IsPublicVisible = model.IsPublicVisible,
                    IsActive = model.IsActive,
                    Translations = BuildTranslationInputs(model.Translations)
                },
                cancellationToken);

            return Json(new
            {
                success = true,
                id = response.Id,
                message = GetText("BackOffice.CongressPaymentPlans.Messages.Created", "Ödeme planı başarıyla oluşturuldu.")
            });
        }
        catch (Exception exception)
        {
            return BadRequest(new { success = false, message = GetExceptionMessage(exception) });
        }
    }

    [HttpGet]
    public async Task<IActionResult> EditModal(Guid id, Guid congressId, CancellationToken cancellationToken)
    {
        GetCongressPaymentPlanForUpdateResponse response = await _mediator.Send(
            new GetCongressPaymentPlanForUpdateQuery { Id = id },
            cancellationToken);

        if (response.CongressId != congressId)
            return BadRequest(new { success = false, message = GetText("Common.InvalidRequest", "Geçersiz istek.") });

        UpdateCongressPaymentPlanViewModel model = new()
        {
            Id = response.Id,
            CongressId = response.CongressId,
            Code = response.Code,
            Amount = response.Amount,
            Currency = response.Currency,
            AudienceType = response.AudienceType,
            PaymentCategory = response.PaymentCategory,
            DueDateText = FormatDateForInput(response.DueDate),
            ValidFromText = FormatDateForInput(response.ValidFrom),
            ValidUntilText = FormatDateForInput(response.ValidUntil),
            Order = response.Order,
            IsPublicVisible = response.IsPublicVisible,
            IsActive = response.IsActive,
            AudienceTypeOptions = BuildAudienceTypeOptions(),
            PaymentCategoryOptions = BuildPaymentCategoryOptions(),
            CurrencyOptions = BuildCurrencyOptions(),
            Translations = response.Translations.Select(translation => new CongressPaymentPlanTranslationViewModel
            {
                LanguageId = translation.LanguageId,
                Culture = translation.Culture,
                LanguageName = translation.LanguageName,
                IsDefault = translation.IsDefault,
                Exists = translation.Exists,
                Name = GetField(translation.Fields, NameField),
                Description = GetField(translation.Fields, DescriptionField)
            }).ToList()
        };

        return PartialView("~/Views/CongressPaymentPlans/_UpdatePaymentPlanModal.cshtml", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update([FromForm] UpdateCongressPaymentPlanViewModel model, CancellationToken cancellationToken)
    {
        ParsedPaymentPlanDates dates = ValidateUpdateModel(model);

        if (!ModelState.IsValid)
            return BadRequest(CreateValidationErrorResponse());

        try
        {
            await _mediator.Send(
                new UpdateCongressPaymentPlanCommand
                {
                    Id = model.Id,
                    CongressId = model.CongressId,
                    Code = NormalizeText(model.Code),
                    Amount = model.Amount,
                    Currency = model.Currency,
                    AudienceType = model.AudienceType,
                    PaymentCategory = model.PaymentCategory,
                    DueDate = dates.DueDate,
                    ValidFrom = dates.ValidFrom,
                    ValidUntil = dates.ValidUntil,
                    IsPublicVisible = model.IsPublicVisible,
                    IsActive = model.IsActive,
                    Translations = BuildTranslationInputs(model.Translations)
                },
                cancellationToken);

            return Json(new
            {
                success = true,
                message = GetText("BackOffice.CongressPaymentPlans.Messages.Updated", "Ödeme planı başarıyla güncellendi.")
            });
        }
        catch (Exception exception)
        {
            return BadRequest(new { success = false, message = GetExceptionMessage(exception) });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete([FromForm] Guid id, [FromForm] Guid congressId, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty || congressId == Guid.Empty)
            return BadRequest(new { success = false, message = GetText("Common.InvalidRequest", "Geçersiz istek.") });

        try
        {
            await _mediator.Send(new DeleteCongressPaymentPlanCommand { Id = id }, cancellationToken);

            return Json(new
            {
                success = true,
                message = GetText("BackOffice.CongressPaymentPlans.Messages.Deleted", "Ödeme planı başarıyla silindi.")
            });
        }
        catch (Exception exception)
        {
            return BadRequest(new { success = false, message = GetExceptionMessage(exception) });
        }
    }

    private ParsedPaymentPlanDates ValidateCreateModel(CreateCongressPaymentPlanViewModel model)
    {
        ValidateBaseModel(model.CongressId, model.Amount, model.Currency, model.AudienceType, model.PaymentCategory, model.Translations);

        return ParseDates(model.DueDateText, model.ValidFromText, model.ValidUntilText);
    }

    private ParsedPaymentPlanDates ValidateUpdateModel(UpdateCongressPaymentPlanViewModel model)
    {
        if (model.Id == Guid.Empty)
            ModelState.AddModelError(nameof(model.Id), GetText("Common.InvalidRequest", "Geçersiz istek."));

        ValidateBaseModel(model.CongressId, model.Amount, model.Currency, model.AudienceType, model.PaymentCategory, model.Translations);

        return ParseDates(model.DueDateText, model.ValidFromText, model.ValidUntilText);
    }

    private void ValidateBaseModel(
        Guid congressId,
        decimal amount,
        string? currency,
        string? audienceType,
        string? paymentCategory,
        IReadOnlyList<CongressPaymentPlanTranslationViewModel> translations)
    {
        if (congressId == Guid.Empty)
            ModelState.AddModelError(nameof(CreateCongressPaymentPlanViewModel.CongressId), GetText("BackOffice.CongressPaymentPlans.Validation.CongressRequired", "Kongre bilgisi zorunludur."));

        if (amount <= 0)
            ModelState.AddModelError(nameof(CreateCongressPaymentPlanViewModel.Amount), GetText("BackOffice.CongressPaymentPlans.Validation.AmountRequired", "Tutar sıfırdan büyük olmalıdır."));

        if (string.IsNullOrWhiteSpace(currency))
            ModelState.AddModelError(nameof(CreateCongressPaymentPlanViewModel.Currency), GetText("BackOffice.CongressPaymentPlans.Validation.CurrencyRequired", "Para birimi zorunludur."));

        if (!CongressPaymentPlanAudienceTypes.IsValid(audienceType))
            ModelState.AddModelError(nameof(CreateCongressPaymentPlanViewModel.AudienceType), GetText("BackOffice.CongressPaymentPlans.Business.InvalidAudienceType", "Katılımcı tipi geçersiz."));

        if (!CongressPaymentPlanCategories.IsValid(paymentCategory))
            ModelState.AddModelError(nameof(CreateCongressPaymentPlanViewModel.PaymentCategory), GetText("BackOffice.CongressPaymentPlans.Business.InvalidPaymentCategory", "Ödeme kategorisi geçersiz."));

        CongressPaymentPlanTranslationViewModel? defaultTranslation = translations.FirstOrDefault(translation => translation.IsDefault);

        if (defaultTranslation is null || string.IsNullOrWhiteSpace(defaultTranslation.Name))
        {
            int defaultIndex = 0;

            if (defaultTranslation is not null)
            {
                for (int index = 0; index < translations.Count; index++)
                {
                    if (translations[index].LanguageId == defaultTranslation.LanguageId)
                    {
                        defaultIndex = index;
                        break;
                    }
                }
            }

            ModelState.AddModelError($"Translations[{defaultIndex}].Name", GetText("BackOffice.CongressPaymentPlans.Business.DefaultTranslationRequired", "Varsayılan dilde plan adı zorunludur."));
        }
    }

    private ParsedPaymentPlanDates ParseDates(string? dueDateText, string? validFromText, string? validUntilText)
    {
        DateTime? dueDate = ParseOptionalDate(dueDateText, nameof(CreateCongressPaymentPlanViewModel.DueDateText), GetText("BackOffice.CongressPaymentPlans.Validation.DueDateInvalid", "Son tarih geçerli bir tarih olmalıdır."));
        DateTime? validFrom = ParseOptionalDate(validFromText, nameof(CreateCongressPaymentPlanViewModel.ValidFromText), GetText("BackOffice.CongressPaymentPlans.Validation.ValidFromInvalid", "Geçerlilik başlangıcı geçerli bir tarih olmalıdır."));
        DateTime? validUntil = ParseOptionalDate(validUntilText, nameof(CreateCongressPaymentPlanViewModel.ValidUntilText), GetText("BackOffice.CongressPaymentPlans.Validation.ValidUntilInvalid", "Geçerlilik bitişi geçerli bir tarih olmalıdır."));

        if (validFrom.HasValue && validUntil.HasValue && validUntil.Value < validFrom.Value)
            ModelState.AddModelError(nameof(CreateCongressPaymentPlanViewModel.ValidUntilText), GetText("BackOffice.CongressPaymentPlans.Business.InvalidDateRange", "Geçerlilik bitişi başlangıç tarihinden önce olamaz."));

        return new ParsedPaymentPlanDates(dueDate, validFrom, validUntil);
    }

    private DateTime? ParseOptionalDate(string? value, string key, string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        string normalizedValue = value.Trim();
        string[] formats = { "dd.MM.yyyy", "dd.MM.yyyy HH:mm", "yyyy-MM-dd", "yyyy-MM-ddTHH:mm", "yyyy-MM-dd HH:mm" };

        if (DateTime.TryParseExact(normalizedValue, formats, CultureInfo.GetCultureInfo("tr-TR"), DateTimeStyles.AssumeLocal, out DateTime parsedDate))
            return parsedDate;

        if (DateTime.TryParse(normalizedValue, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out parsedDate))
            return parsedDate;

        ModelState.AddModelError(key, errorMessage);
        return null;
    }

    private async Task<List<CongressPaymentPlanTranslationViewModel>> BuildTranslationViewModelsAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<ApplicationLanguageDto> languages = await _applicationLanguageProvider.GetActiveLanguagesAsync(cancellationToken);

        return languages
            .OrderByDescending(language => language.IsDefault)
            .ThenBy(language => language.Order)
            .ThenBy(language => language.Name)
            .Select(language => new CongressPaymentPlanTranslationViewModel
            {
                LanguageId = language.Id,
                Culture = language.Culture,
                LanguageName = language.Name,
                IsDefault = language.IsDefault,
                Exists = false
            })
            .ToList();
    }

    private ICollection<TranslationInputDto> BuildTranslationInputs(IEnumerable<CongressPaymentPlanTranslationViewModel> translations)
    {
        return translations
            .GroupBy(translation => translation.LanguageId)
            .Select(group => group.First())
            .Where(translation => translation.IsDefault || !string.IsNullOrWhiteSpace(translation.Name) || !string.IsNullOrWhiteSpace(translation.Description))
            .Select(translation => new TranslationInputDto
            {
                LanguageId = translation.LanguageId,
                Fields = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    [NameField] = NormalizeText(translation.Name),
                    [DescriptionField] = NormalizeText(translation.Description)
                }
            })
            .ToList();
    }

    private static IEnumerable<GetListCongressPaymentPlanListItemDto> ApplySearch(IEnumerable<GetListCongressPaymentPlanListItemDto> items, string searchText)
    {
        return items.Where(item =>
            Contains(item.Name, searchText) ||
            Contains(item.Description, searchText) ||
            Contains(item.Code, searchText) ||
            Contains(item.Currency, searchText) ||
            Contains(item.AudienceType, searchText) ||
            Contains(item.PaymentCategory, searchText));
    }

    private static IEnumerable<GetListCongressPaymentPlanListItemDto> ApplySort(IEnumerable<GetListCongressPaymentPlanListItemDto> items, string column, string direction)
    {
        bool desc = string.Equals(direction, "desc", StringComparison.OrdinalIgnoreCase);

        return column.ToLowerInvariant() switch
        {
            "name" => desc ? items.OrderByDescending(item => item.Name) : items.OrderBy(item => item.Name),
            "audiencetype" => desc ? items.OrderByDescending(item => item.AudienceType) : items.OrderBy(item => item.AudienceType),
            "paymentcategory" => desc ? items.OrderByDescending(item => item.PaymentCategory) : items.OrderBy(item => item.PaymentCategory),
            "amount" => desc ? items.OrderByDescending(item => item.Amount) : items.OrderBy(item => item.Amount),
            "currency" => desc ? items.OrderByDescending(item => item.Currency) : items.OrderBy(item => item.Currency),
            "validuntil" => desc ? items.OrderByDescending(item => item.ValidUntil) : items.OrderBy(item => item.ValidUntil),
            "ispublicvisible" => desc ? items.OrderByDescending(item => item.IsPublicVisible) : items.OrderBy(item => item.IsPublicVisible),
            "isactive" => desc ? items.OrderByDescending(item => item.IsActive) : items.OrderBy(item => item.IsActive),
            _ => desc ? items.OrderByDescending(item => item.Order) : items.OrderBy(item => item.Order)
        };
    }

    private static bool Contains(string? value, string searchText)
        => !string.IsNullOrWhiteSpace(value) && value.Contains(searchText, StringComparison.OrdinalIgnoreCase);

    private static string? NormalizeText(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeEmpty(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool? ParseNullableBoolean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        string normalizedValue = value.Trim().ToLowerInvariant();

        if (normalizedValue is "true" or "1" or "aktif" or "active" or "visible" or "görünür" or "gorunur")
            return true;

        if (normalizedValue is "false" or "0" or "pasif" or "passive" or "hidden" or "gizli")
            return false;

        return null;
    }

    private List<CongressPaymentPlanSelectOptionViewModel> BuildAudienceTypeOptions()
    {
        return new()
        {
            new() { Value = CongressPaymentPlanAudienceTypes.All, Text = GetAudienceTypeText(CongressPaymentPlanAudienceTypes.All) },
            new() { Value = CongressPaymentPlanAudienceTypes.Domestic, Text = GetAudienceTypeText(CongressPaymentPlanAudienceTypes.Domestic) },
            new() { Value = CongressPaymentPlanAudienceTypes.International, Text = GetAudienceTypeText(CongressPaymentPlanAudienceTypes.International) }
        };
    }

    private List<CongressPaymentPlanSelectOptionViewModel> BuildPaymentCategoryOptions()
    {
        return new()
        {
            new() { Value = CongressPaymentPlanCategories.Participation, Text = GetPaymentCategoryText(CongressPaymentPlanCategories.Participation) },
            new() { Value = CongressPaymentPlanCategories.SecondSubmission, Text = GetPaymentCategoryText(CongressPaymentPlanCategories.SecondSubmission) },
            new() { Value = CongressPaymentPlanCategories.Listener, Text = GetPaymentCategoryText(CongressPaymentPlanCategories.Listener) },
            new() { Value = CongressPaymentPlanCategories.Student, Text = GetPaymentCategoryText(CongressPaymentPlanCategories.Student) },
            new() { Value = CongressPaymentPlanCategories.Other, Text = GetPaymentCategoryText(CongressPaymentPlanCategories.Other) }
        };
    }

    private static List<CongressPaymentPlanSelectOptionViewModel> BuildCurrencyOptions()
        => CurrencyValues.Select(currency => new CongressPaymentPlanSelectOptionViewModel { Value = currency, Text = currency }).ToList();

    private string GetAudienceTypeText(string? audienceType)
    {
        string normalizedValue = CongressPaymentPlanAudienceTypes.Normalize(audienceType);

        return normalizedValue switch
        {
            CongressPaymentPlanAudienceTypes.Domestic => GetText("BackOffice.CongressPaymentPlans.Audience.Domestic", "Yerli Katılımcı"),
            CongressPaymentPlanAudienceTypes.International => GetText("BackOffice.CongressPaymentPlans.Audience.International", "Yabancı Katılımcı"),
            _ => GetText("BackOffice.CongressPaymentPlans.Audience.All", "Tümü")
        };
    }

    private string GetPaymentCategoryText(string? paymentCategory)
    {
        string normalizedValue = CongressPaymentPlanCategories.Normalize(paymentCategory);

        return normalizedValue switch
        {
            CongressPaymentPlanCategories.SecondSubmission => GetText("BackOffice.CongressPaymentPlans.Category.SecondSubmission", "İkinci Bildiri"),
            CongressPaymentPlanCategories.Listener => GetText("BackOffice.CongressPaymentPlans.Category.Listener", "Dinleyici"),
            CongressPaymentPlanCategories.Student => GetText("BackOffice.CongressPaymentPlans.Category.Student", "Öğrenci"),
            CongressPaymentPlanCategories.Other => GetText("BackOffice.CongressPaymentPlans.Category.Other", "Diğer"),
            _ => GetText("BackOffice.CongressPaymentPlans.Category.Participation", "Katılım")
        };
    }

    private static string FormatAmount(decimal amount)
        => amount.ToString("N2", CultureInfo.GetCultureInfo("tr-TR"));

    private static string FormatDate(DateTime? value)
        => value.HasValue ? value.Value.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("tr-TR")) : string.Empty;

    private static string FormatDateForInput(DateTime? value)
        => value.HasValue ? value.Value.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("tr-TR")) : string.Empty;

    private string FormatValidityRange(DateTime? validFrom, DateTime? validUntil)
    {
        if (!validFrom.HasValue && !validUntil.HasValue)
            return GetText("Common.NotSpecified", "Belirtilmedi");

        string from = validFrom.HasValue ? FormatDate(validFrom) : "-";
        string until = validUntil.HasValue ? FormatDate(validUntil) : "-";

        return $"{from} - {until}";
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

    private static string? GetField(IDictionary<string, string?> fields, string key)
        => fields.TryGetValue(key, out string? value) ? value : null;

    private sealed record ParsedPaymentPlanDates(DateTime? DueDate, DateTime? ValidFrom, DateTime? ValidUntil);
}
