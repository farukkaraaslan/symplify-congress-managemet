namespace Symplify.BackOffice.Application.Features.CongressWorkflows.Constants;

public static class CongressWorkflowResourceKeys
{
    private const string Prefix = "BackOffice.CongressWorkflows";

    public static class Validation
    {
        public const string CongressNotFound = $"{Prefix}.Validation.CongressNotFound";
        public const string TemplateNotFound = $"{Prefix}.Validation.TemplateNotFound";
        public const string TemplateHasNoTransitions = $"{Prefix}.Validation.TemplateHasNoTransitions";
        public const string InitialStatusRequired = $"{Prefix}.Validation.InitialStatusRequired";
        public const string TransitionNotFound = $"{Prefix}.Validation.TransitionNotFound";
        public const string TransitionAlreadyExists = $"{Prefix}.Validation.TransitionAlreadyExists";
        public const string EntityNotFound = $"{Prefix}.Validation.EntityNotFound";
        public const string ReorderInvalid = $"{Prefix}.Validation.ReorderInvalid";
    }

    public static class Messages
    {
        public const string TemplateApplied = $"{Prefix}.Messages.TemplateApplied";
        public const string TransitionAdded = $"{Prefix}.Messages.TransitionAdded";
        public const string TransitionUpdated = $"{Prefix}.Messages.TransitionUpdated";
        public const string TransitionDeleted = $"{Prefix}.Messages.TransitionDeleted";
        public const string Reordered = $"{Prefix}.Messages.Reordered";
    }
}
