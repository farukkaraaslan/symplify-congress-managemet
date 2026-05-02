using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.CongressDocuments.Constants;
using Symplify.BackOffice.Application.Features.CongressDocuments.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.CongressDocuments.Commands.Update;
public class UpdateCongressDocumentCommand : IRequest<UpdatedCongressDocumentResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public Guid CongressId { get; set; }
    public Guid? DocumentTypeId { get; set; }
    public Symplify.BackOffice.Domain.Enums.CongressDocumentType? Type { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string? OriginalFileName { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCongressDocuments";
    public string[] Roles => new[] { CongressDocumentsOperationClaims.Admin, CongressDocumentsOperationClaims.Write, CongressDocumentsOperationClaims.Update };
    public class UpdateCongressDocumentCommandHandler : IRequestHandler<UpdateCongressDocumentCommand, UpdatedCongressDocumentResponse>
    {
        private readonly ICongressDocumentRepository _repository; private readonly IMapper _mapper; private readonly CongressDocumentBusinessRules _rules;
        public UpdateCongressDocumentCommandHandler(ICongressDocumentRepository repository, IMapper mapper, CongressDocumentBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<UpdatedCongressDocumentResponse> Handle(UpdateCongressDocumentCommand request, CancellationToken cancellationToken)
        {
            CongressDocument? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.CongressDocumentShouldExistWhenSelected(entity);
            entity!.CongressId = request.CongressId;
            entity!.DocumentTypeId = request.DocumentTypeId;
            entity!.Type = request.Type;
            entity!.FilePath = request.FilePath;
            entity!.OriginalFileName = request.OriginalFileName;
            entity!.Order = request.Order;
            entity!.IsActive = request.IsActive;
            CongressDocument updatedEntity = await _repository.UpdateAsync(entity!);
            return _mapper.Map<UpdatedCongressDocumentResponse>(updatedEntity);
        }
    }
}
