namespace Symplify.BackOffice.Application.Features.WorkflowTemplateTransitions.Constants;

public static class WorkflowTemplateTransitionResourceKeys
{
    private const string Prefix = "BackOffice.WorkflowTemplateTransitions";

    public static class Validation
    {
        public const string EntityNotFound = $"{Prefix}.Validation.EntityNotFound";
        public const string TemplateNotFound = $"{Prefix}.Validation.TemplateNotFound";
        public const string TransitionNotFound = $"{Prefix}.Validation.TransitionNotFound";
        public const string TransitionInactive = $"{Prefix}.Validation.TransitionInactive";
        public const string TransitionAlreadyExists = $"{Prefix}.Validation.TransitionAlreadyExists";
        public const string EntityUsedByCongressWorkflow = $"{Prefix}.Validation.EntityUsedByCongressWorkflow";
        public const string ReorderInvalid = $"{Prefix}.Validation.ReorderInvalid";
    }

    public static class Messages
    {
        public const string Created = $"{Prefix}.Messages.Created";
        public const string Updated = $"{Prefix}.Messages.Updated";
        public const string Deleted = $"{Prefix}.Messages.Deleted";
        public const string Reordered = $"{Prefix}.Messages.Reordered";
    }
}
