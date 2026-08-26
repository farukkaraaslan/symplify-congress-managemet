namespace Symplify.BackOffice.WebUI.Models.Congresses;

public sealed class CongressCloneSourceOptionViewModel
{
    public Guid Id { get; init; }

    public Guid OrganizationId { get; init; }

    public string Text { get; init; } = string.Empty;
}
