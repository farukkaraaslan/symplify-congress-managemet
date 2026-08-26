namespace Symplify.BackOffice.Application.Features.Congresses.Commands.Update;

public sealed class UpdateCongressContactEmailInputDto
{
    public string? Label { get; set; }

    public string? Email { get; set; }

    public bool IsPrimary { get; set; }

    public bool IsVisibleOnPortal { get; set; } = true;

    public bool ReceivesContactMessages { get; set; } = true;

    public int Order { get; set; }
}
