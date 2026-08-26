namespace Symplify.BackOffice.Application.Features.FullTextBook.Services;

public interface IFullTextBookDocumentFinalizer
{
    Task<byte[]> FinalizeAsync(
        byte[] documentContent,
        CancellationToken cancellationToken = default);
}
