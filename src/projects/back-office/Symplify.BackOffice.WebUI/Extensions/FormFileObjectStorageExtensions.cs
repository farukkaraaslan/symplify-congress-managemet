using Microsoft.AspNetCore.Http;
using Symplify.BackOffice.Application.Features.Congresses.Commands;
using Symplify.BackOffice.Application.Features.CongressSliders.Commands;
using Symplify.BackOffice.Application.Features.Organizations.Commands;

namespace Symplify.BackOffice.WebUI.Extensions;

public static class FormFileObjectStorageExtensions
{
    public static OrganizationLogoInputDto? ToOrganizationLogoInputDto(this IFormFile? file)
    {
        if (file is null || file.Length <= 0)
            return null;

        return new OrganizationLogoInputDto
        {
            OriginalFileName = file.FileName,
            ContentType = file.ContentType,
            Length = file.Length,
            Content = file.OpenReadStream()
        };
    }

    public static CongressLogoInputDto? ToCongressLogoInputDto(this IFormFile? file)
    {
        if (file is null || file.Length <= 0)
            return null;

        return new CongressLogoInputDto
        {
            OriginalFileName = file.FileName,
            ContentType = file.ContentType,
            Length = file.Length,
            Content = file.OpenReadStream()
        };
    }

    public static CongressSliderImageInputDto? ToCongressSliderImageInputDto(this IFormFile? file)
    {
        if (file is null || file.Length <= 0)
            return null;

        return new CongressSliderImageInputDto
        {
            OriginalFileName = file.FileName,
            ContentType = file.ContentType,
            Length = file.Length,
            Content = file.OpenReadStream()
        };
    }
}
