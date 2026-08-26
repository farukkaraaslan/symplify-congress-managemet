namespace Symplify.BackOffice.WebUI.Models.Organizations;

public sealed class OrganizationDataTableRequest
{
    public int Draw { get; set; } = 1;
    public int Start { get; set; }
    public int Length { get; set; } = 10;

    public int Page => Length <= 0 ? 0 : Start / Length;
    public int PageSize => Length <= 0 ? 10 : Length;
}
