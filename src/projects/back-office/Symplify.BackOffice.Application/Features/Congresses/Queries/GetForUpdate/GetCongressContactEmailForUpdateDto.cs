namespace Symplify.BackOffice.Application.Features.Congresses.Queries.GetForUpdate;

public sealed class GetCongressContactEmailForUpdateDto
{
    public string Email { get; set; } = string.Empty;

    public string? Label { get; set; }

    public bool IsPrimary { get; set; }

    public bool IsVisibleOnPortal { get; set; }

    public bool ReceivesContactMessages { get; set; }

    public int Order { get; set; }
}
