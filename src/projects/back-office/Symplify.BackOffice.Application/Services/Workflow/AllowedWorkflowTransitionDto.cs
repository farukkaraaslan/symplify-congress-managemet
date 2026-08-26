namespace Symplify.BackOffice.Application.Services.Workflow;

public sealed class AllowedWorkflowTransitionDto
{
    public int TransitionId { get; set; }
    public int FromStatusId { get; set; }
    public int ToStatusId { get; set; }
    public string ToStatusCode { get; set; } = string.Empty;
    public string DisplayText { get; set; } = string.Empty;
    public string BadgeClass { get; set; } = string.Empty;
    public bool IsAllowed { get; set; }
    public string? DisabledReason { get; set; }
    public bool RequiresComment { get; set; }
    public bool IsAuto { get; set; }
}
