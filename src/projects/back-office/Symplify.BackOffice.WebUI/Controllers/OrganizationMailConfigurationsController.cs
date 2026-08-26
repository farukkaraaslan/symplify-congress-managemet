using Core.Application.Requests;
using Core.Application.Storage;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Symplify.BackOffice.Application.Features.OrganizationMailConfigurations.Commands.Delete;
using Symplify.BackOffice.Application.Features.OrganizationMailConfigurations.Commands.Save;
using Symplify.BackOffice.Application.Features.OrganizationMailConfigurations.Commands.SendTest;
using Symplify.BackOffice.Application.Features.OrganizationMailConfigurations.Queries.GetByOrganizationId;
using Symplify.BackOffice.Application.Features.Organizations.Queries.GetList;
using Symplify.BackOffice.WebUI.Extensions;
using Symplify.BackOffice.WebUI.Models.OrganizationMailConfigurations;

namespace Symplify.BackOffice.WebUI.Controllers;

[Authorize]
[Route("{culture=tr-TR}/[controller]/[action]")]
public sealed class OrganizationMailConfigurationsController : Controller
{
    private readonly IMediator _mediator;
    private readonly IObjectStorageService _objectStorageService;

    public OrganizationMailConfigurationsController(
        IMediator mediator,
        IObjectStorageService objectStorageService)
    {
        _mediator = mediator;
        _objectStorageService = objectStorageService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(Guid? organizationId, CancellationToken cancellationToken)
    {
        var organizationsResponse = await _mediator.Send(
            new GetListOrganizationQuery
            {
                SortColumn = "name",
                SortDirection = "asc",
                PageRequest = new PageRequest { Page = 0, PageSize = 500 }
            },
            cancellationToken);

        var organizations = organizationsResponse.Items
            .Where(item => item.IsActive)
            .OrderBy(item => item.Name)
            .ToList();

        Guid? selectedId = organizationId.HasValue && organizations.Any(item => item.Id == organizationId.Value)
            ? organizationId
            : organizations.FirstOrDefault()?.Id;

        OrganizationMailConfigurationsIndexViewModel model = new()
        {
            SelectedOrganizationId = selectedId,
            Organizations = organizations
                .Select(item => new SelectListItem
                {
                    Value = item.Id.ToString("D"),
                    Text = string.IsNullOrWhiteSpace(item.ShortName)
                        ? item.Name
                        : $"{item.Name} ({item.ShortName})",
                    Selected = item.Id == selectedId
                })
                .ToList()
        };

        if (!selectedId.HasValue)
            return View(model);

        var selectedOrganization = organizations.First(item => item.Id == selectedId.Value);
        GetOrganizationMailConfigurationByOrganizationIdResponse configuration = await _mediator.Send(
            new GetOrganizationMailConfigurationByOrganizationIdQuery
            {
                OrganizationId = selectedId.Value
            },
            cancellationToken);

        model.SelectedOrganizationName = selectedOrganization.Name;
        model.SelectedOrganizationCode = selectedOrganization.ShortName ?? selectedOrganization.Code;
        model.Exists = configuration.Exists;
        model.HasStoredPassword = configuration.HasStoredPassword;
        model.HasMailLogo = configuration.HasMailLogo;
        model.MailLogoFileName = configuration.MailLogoFileName;
        model.LastTestedAt = configuration.LastTestedAt;
        model.LastTestSucceeded = configuration.LastTestSucceeded;
        model.LastTestError = configuration.LastTestError;
        model.Configuration = new OrganizationMailConfigurationViewModel
        {
            OrganizationId = selectedId.Value,
            Host = configuration.Host,
            Port = configuration.Port,
            EnableSsl = configuration.EnableSsl,
            Username = configuration.Username,
            FromEmail = configuration.FromEmail,
            FromName = configuration.FromName,
            ReplyToEmail = configuration.ReplyToEmail,
            ReplyToName = configuration.ReplyToName,
            IsActive = configuration.IsActive
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Logo(Guid organizationId, CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty)
            return NotFound();

        GetOrganizationMailConfigurationByOrganizationIdResponse configuration = await _mediator.Send(
            new GetOrganizationMailConfigurationByOrganizationIdQuery
            {
                OrganizationId = organizationId
            },
            cancellationToken);

        if (!configuration.HasMailLogo ||
            string.IsNullOrWhiteSpace(configuration.MailLogoBucketName) ||
            string.IsNullOrWhiteSpace(configuration.MailLogoObjectName))
        {
            return NotFound();
        }

        try
        {
            Stream stream = await _objectStorageService.OpenReadAsync(
                configuration.MailLogoBucketName,
                configuration.MailLogoObjectName,
                cancellationToken);

            string contentType = configuration.MailLogoContentType?.Trim().ToLowerInvariant() switch
            {
                "image/jpeg" => "image/jpeg",
                "image/jpg" => "image/jpeg",
                _ => "image/png"
            };

            Response.Headers.CacheControl = "private,max-age=300";
            return File(stream, contentType);
        }
        catch
        {
            return NotFound();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(
        [FromForm] OrganizationMailConfigurationViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = FirstModelError() });

        var mailLogo = model.MailLogo.ToOrganizationLogoInputDto();

        try
        {
            SaveOrganizationMailConfigurationResponse response = await _mediator.Send(
                new SaveOrganizationMailConfigurationCommand
                {
                    OrganizationId = model.OrganizationId,
                    Host = model.Host,
                    Port = model.Port,
                    EnableSsl = model.EnableSsl,
                    Username = model.Username,
                    Password = model.Password,
                    FromEmail = model.FromEmail,
                    FromName = model.FromName,
                    ReplyToEmail = model.ReplyToEmail,
                    ReplyToName = model.ReplyToName,
                    MailLogo = mailLogo,
                    RemoveMailLogo = model.RemoveMailLogo,
                    IsActive = model.IsActive
                },
                cancellationToken);

            return Json(new
            {
                success = true,
                response.Id,
                response.Created,
                message = response.Created
                    ? "Organizasyon mail ayarları oluşturuldu."
                    : "Organizasyon mail ayarları güncellendi."
            });
        }
        catch (BusinessException exception)
        {
            return BadRequest(new { success = false, message = MapBusinessMessage(exception.Message) });
        }
        finally
        {
            if (mailLogo is not null)
                await mailLogo.Content.DisposeAsync();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendTest(
        [FromForm] SendOrganizationMailTestViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = FirstModelError() });

        try
        {
            SendOrganizationMailTestResponse response = await _mediator.Send(
                new SendOrganizationMailTestCommand
                {
                    OrganizationId = model.OrganizationId,
                    ToEmail = model.ToEmail,
                    ToName = model.ToName
                },
                cancellationToken);

            return Json(new
            {
                success = true,
                response.SentAt,
                message = "Test maili başarıyla gönderildi."
            });
        }
        catch (BusinessException)
        {
            return BadRequest(new
            {
                success = false,
                message = "Test maili gönderilemedi. SMTP ayarlarını ve private MinIO mail logosunu kontrol edin."
            });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid organizationId, CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty)
            return BadRequest(new { success = false, message = "Organizasyon bilgisi geçersiz." });

        try
        {
            await _mediator.Send(
                new DeleteOrganizationMailConfigurationCommand { OrganizationId = organizationId },
                cancellationToken);

            return Json(new { success = true, message = "Organizasyon mail ayarları kaldırıldı." });
        }
        catch (BusinessException exception)
        {
            return BadRequest(new { success = false, message = MapBusinessMessage(exception.Message) });
        }
    }

    private static string MapBusinessMessage(string messageKey) => messageKey switch
    {
        "BackOffice.OrganizationMailConfigurations.Validation.PasswordRequired" => "İlk kayıtta SMTP parolası zorunludur.",
        "BackOffice.OrganizationMailConfigurations.Validation.OrganizationNotFound" => "Organizasyon bulunamadı.",
        "BackOffice.OrganizationMailConfigurations.Validation.ConfigurationNotFound" => "Organizasyon mail ayarları bulunamadı.",
        "BackOffice.OrganizationMailConfigurations.Validation.ActiveConfigurationNotFound" => "Organizasyonun aktif mail ayarı bulunamadı.",
        "BackOffice.OrganizationMailConfigurations.Validation.InvalidMailLogo" => "Mail logosu PNG veya JPEG olmalı ve 300 KB boyutunu geçmemelidir.",
        "BackOffice.OrganizationMailConfigurations.Validation.ObjectStorageBucketMissing" => "Mail logosunun kaydedileceği MinIO bucket ayarı bulunamadı.",
        _ => "Organizasyon mail ayarları işlemi tamamlanamadı."
    };

    private string FirstModelError() =>
        ModelState.Values
            .SelectMany(value => value.Errors)
            .Select(error => error.ErrorMessage)
            .FirstOrDefault(message => !string.IsNullOrWhiteSpace(message))
        ?? "Form alanlarını kontrol edin.";
}
