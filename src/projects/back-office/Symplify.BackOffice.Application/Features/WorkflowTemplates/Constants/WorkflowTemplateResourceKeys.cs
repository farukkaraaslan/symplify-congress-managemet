namespace Symplify.BackOffice.Application.Features.WorkflowTemplates.Constants;

public static class WorkflowTemplateResourceKeys
{
    private const string Prefix = "BackOffice.WorkflowTemplates";

    public static class Validation
    {
        public const string EntityNotFound = $"{Prefix}.Validation.EntityNotFound";
        public const string TranslationNotFound = $"{Prefix}.Validation.TranslationNotFound";
        public const string DefaultTranslationRequired = $"{Prefix}.Validation.DefaultTranslationRequired";
        public const string CodeRequired = $"{Prefix}.Validation.CodeRequired";
        public const string CodeMaxLength = $"{Prefix}.Validation.CodeMaxLength";
        public const string CodeAlreadyExists = $"{Prefix}.Validation.CodeAlreadyExists";
        public const string NameRequired = $"{Prefix}.Validation.NameRequired";
        public const string NameMaxLength = $"{Prefix}.Validation.NameMaxLength";
        public const string DescriptionMaxLength = $"{Prefix}.Validation.DescriptionMaxLength";
        public const string DuplicateTranslationNameInRequest = $"{Prefix}.Validation.DuplicateTranslationNameInRequest";
        public const string NameAlreadyExists = $"{Prefix}.Validation.NameAlreadyExists";
        public const string InitialStatusNotFound = $"{Prefix}.Validation.InitialStatusNotFound";
        public const string InitialStatusInactive = $"{Prefix}.Validation.InitialStatusInactive";
        public const string InitialStatusIsFinal = $"{Prefix}.Validation.InitialStatusIsFinal";
        public const string EntityUsedByCongressWorkflow = $"{Prefix}.Validation.EntityUsedByCongressWorkflow";
    }

    public static class Messages
    {
        public const string Created = $"{Prefix}.Messages.Created";
        public const string Updated = $"{Prefix}.Messages.Updated";
        public const string Deleted = $"{Prefix}.Messages.Deleted";
        public const string TranslationDeleted = $"{Prefix}.Messages.TranslationDeleted";
    }
}
