namespace Symplify.BackOffice.Application.Features.CongressBoardMembers.Constants;

public static class CongressBoardMembersMessages
{
    public const string EntityNotFound = "BackOffice.CongressBoardMembers.Business.EntityNotFound";
    public const string TranslationNotFound = "BackOffice.CongressBoardMembers.Business.TranslationNotFound";
    public const string DefaultTranslationRequired = "BackOffice.CongressBoardMembers.Business.DefaultTranslationRequired";
    public const string DefaultTranslationCannotBeDeleted = "BackOffice.CongressBoardMembers.Business.DefaultTranslationCannotBeDeleted";

    public const string CongressRequired = "BackOffice.CongressBoardMembers.Validation.CongressRequired";
    public const string BoardRequired = "BackOffice.CongressBoardMembers.Validation.BoardRequired";
    public const string FullNameRequired = "BackOffice.CongressBoardMembers.Validation.FullNameRequired";
    public const string FullNameMaxLength = "BackOffice.CongressBoardMembers.Validation.FullNameMaxLength";
    public const string AcademicTitleMaxLength = "BackOffice.CongressBoardMembers.Validation.AcademicTitleMaxLength";
    public const string InstitutionMaxLength = "BackOffice.CongressBoardMembers.Validation.InstitutionMaxLength";
    public const string OrderMustBePositive = "BackOffice.CongressBoardMembers.Validation.OrderMustBePositive";
    public const string ReorderRequired = "BackOffice.CongressBoardMembers.Validation.ReorderRequired";
    public const string InvalidReorderList = "BackOffice.CongressBoardMembers.Validation.InvalidReorderList";
    public const string ReorderSingleBoardRequired = "BackOffice.CongressBoardMembers.Validation.ReorderSingleBoardRequired";

    public const string ImageExtensionInvalid = "BackOffice.CongressBoardMembers.Validation.ImageExtensionInvalid";
    public const string ImageSizeInvalid = "BackOffice.CongressBoardMembers.Validation.ImageSizeInvalid";
    public const string ObjectStorageBucketMissing = "BackOffice.CongressBoardMembers.Storage.BucketMissing";
    public const string SignatureRequiredForSigner = "BackOffice.CongressBoardMembers.Validation.SignatureRequiredForSigner";

    public const string ExcelFileRequired = "BackOffice.CongressBoardMembers.Validation.ExcelFileRequired";
    public const string ExcelFileInvalid = "BackOffice.CongressBoardMembers.Validation.ExcelFileInvalid";
    public const string ImportFileEmpty = "BackOffice.CongressBoardMembers.Validation.ImportFileEmpty";
}
