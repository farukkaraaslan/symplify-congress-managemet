namespace Symplify.BackOffice.Application.Services.Workflow;

public sealed class ChangeWorkflowStatusResult
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public int? NewStatusId { get; init; }

    public static ChangeWorkflowStatusResult Ok(int? newStatusId) => new()
    {
        Success = true,
        NewStatusId = newStatusId
    };

    public static ChangeWorkflowStatusResult Failed(string message) => new()
    {
        Success = false,
        Message = message
    };
}
