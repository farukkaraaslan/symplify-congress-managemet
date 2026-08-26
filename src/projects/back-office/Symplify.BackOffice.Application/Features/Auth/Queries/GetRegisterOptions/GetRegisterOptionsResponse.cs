namespace Symplify.BackOffice.Application.Features.Auth.Queries.GetRegisterOptions;

public sealed class GetRegisterOptionsResponse
{
    public List<AuthSelectOptionDto> Titles { get; set; } = new();

    public List<AuthSelectOptionDto> Congresses { get; set; } = new();

    public List<AuthSelectOptionDto> Countries { get; set; } = new();
}
