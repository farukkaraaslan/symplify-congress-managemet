namespace Symplify.BackOffice.Application.Features.PaymentDocuments.Commands.Delete;
public class DeletedPaymentDocumentResponse
{
    public Guid Id { get; set; }
    public Guid? SubmissionId { get; set; }
    public Guid? CongressId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string? OriginalFileName { get; set; }
    public string? ContentType { get; set; }
    public long? Size { get; set; }
    public bool IsApproved { get; set; }
}
