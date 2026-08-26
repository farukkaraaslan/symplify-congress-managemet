using Core.Application.Rules;
using Core.CrossCuttingConcerns.Exceptions.Types;
using Symplify.BackOffice.Application.Features.CongressDocuments.Commands;
using Symplify.BackOffice.Application.Features.CongressDocuments.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Lookups;

namespace Symplify.BackOffice.Application.Features.CongressDocuments.Rules;

public class CongressDocumentBusinessRules : BaseBusinessRules
{
    public const long MaxFileSizeBytes = 50 * 1024 * 1024;
    public const long MaxCoverImageSizeBytes = 5 * 1024 * 1024;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".jpg", ".jpeg", ".png", ".webp" };

    private static readonly HashSet<string> AllowedCoverImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    { ".jpg", ".jpeg", ".png", ".webp" };

    private readonly ICongressRepository _congressRepository;
    private readonly IDocumentTypeRepository _documentTypeRepository;

    public CongressDocumentBusinessRules(ICongressRepository congressRepository, IDocumentTypeRepository documentTypeRepository)
    {
        _congressRepository = congressRepository;
        _documentTypeRepository = documentTypeRepository;
    }

    public async Task<Congress> CongressShouldExist(Guid congressId, CancellationToken cancellationToken)
    {
        if (congressId == Guid.Empty) throw new BusinessException(CongressDocumentsMessages.CongressRequired);
        Congress? congress = await _congressRepository.GetAsync(predicate: entity => entity.Id == congressId, cancellationToken: cancellationToken);
        if (congress is null) throw new BusinessException(CongressDocumentsMessages.CongressNotFound);
        return congress;
    }

    public Task CongressDocumentShouldExistWhenSelected(CongressDocument? entity)
    {
        if (entity is null) throw new BusinessException(CongressDocumentsMessages.EntityNotFound);
        return Task.CompletedTask;
    }

    public Task DocumentShouldBelongToCongress(CongressDocument entity, Guid congressId)
    {
        if (congressId == Guid.Empty || entity.CongressId != congressId) throw new BusinessException(CongressDocumentsMessages.EntityNotFound);
        return Task.CompletedTask;
    }

    public async Task<DocumentType> DocumentTypeShouldExist(Guid? documentTypeId, CancellationToken cancellationToken)
    {
        if (!documentTypeId.HasValue || documentTypeId.Value == Guid.Empty)
            throw new BusinessException(CongressDocumentsMessages.DocumentTypeRequired);

        DocumentType? documentType = await _documentTypeRepository.GetAsync(
            predicate: entity => entity.Id == documentTypeId.Value && entity.IsActive,
            cancellationToken: cancellationToken);

        if (documentType is null)
            throw new BusinessException(CongressDocumentsMessages.DocumentTypeNotFound);

        return documentType;
    }

    public Task FileShouldBeValid(CongressDocumentFileInputDto? file, bool isRequired)
    {
        if (file is null || file.Content == Stream.Null || string.IsNullOrWhiteSpace(file.OriginalFileName))
        {
            if (isRequired) throw new BusinessException(CongressDocumentsMessages.FileRequired);
            return Task.CompletedTask;
        }
        if (file.Length <= 0) throw new BusinessException(CongressDocumentsMessages.FileInvalid);
        if (file.Length > MaxFileSizeBytes) throw new BusinessException(CongressDocumentsMessages.FileTooLarge);
        string extension = Path.GetExtension(file.OriginalFileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension)) throw new BusinessException(CongressDocumentsMessages.FileInvalid);
        return Task.CompletedTask;
    }

    public Task CoverImageShouldBeValid(CongressDocumentFileInputDto? coverImage, bool isRequired)
    {
        if (coverImage is null || coverImage.Content == Stream.Null || string.IsNullOrWhiteSpace(coverImage.OriginalFileName))
        {
            if (isRequired) throw new BusinessException(CongressDocumentsMessages.CoverImageInvalid);
            return Task.CompletedTask;
        }

        if (coverImage.Length <= 0) throw new BusinessException(CongressDocumentsMessages.CoverImageInvalid);
        if (coverImage.Length > MaxCoverImageSizeBytes) throw new BusinessException(CongressDocumentsMessages.CoverImageTooLarge);

        string extension = Path.GetExtension(coverImage.OriginalFileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedCoverImageExtensions.Contains(extension))
            throw new BusinessException(CongressDocumentsMessages.CoverImageInvalid);

        return Task.CompletedTask;
    }

    public Task OrderShouldBeValid(int order)
    {
        if (order < 0) throw new BusinessException(CongressDocumentsMessages.OrderInvalid);
        return Task.CompletedTask;
    }

    public Task ReorderItemsShouldBeValid(IReadOnlyCollection<Guid> ids)
    {
        if (ids.Count == 0) throw new BusinessException(CongressDocumentsMessages.ReorderRequired);
        if (ids.Any(id => id == Guid.Empty)) throw new BusinessException(CongressDocumentsMessages.InvalidReorderList);
        return Task.CompletedTask;
    }

    public Task ReorderItemsShouldBelongToCongress(IReadOnlyCollection<Guid> requestedIds, IReadOnlyDictionary<Guid, CongressDocument> entityById)
    {
        if (requestedIds.Any(id => !entityById.ContainsKey(id))) throw new BusinessException(CongressDocumentsMessages.InvalidReorderList);
        return Task.CompletedTask;
    }
}
