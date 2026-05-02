using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.CongressDocuments.Constants;
using Symplify.BackOffice.Application.Features.CongressDocuments.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Application.Features.CongressDocuments.Constants;
namespace Symplify.BackOffice.Application.Features.CongressDocuments.Commands.Delete;
public class DeleteCongressDocumentCommand : IRequest<DeletedCongressDocumentResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCongressDocuments";
    public string[] Roles => new[] { CongressDocumentsOperationClaims.Admin, CongressDocumentsOperationClaims.Write, CongressDocumentsOperationClaims.Delete };
    public class DeleteCongressDocumentCommandHandler : IRequestHandler<DeleteCongressDocumentCommand, DeletedCongressDocumentResponse>
    {
        private readonly ICongressDocumentRepository _repository; private readonly IMapper _mapper; private readonly CongressDocumentBusinessRules _rules;
        public DeleteCongressDocumentCommandHandler(ICongressDocumentRepository repository, IMapper mapper, CongressDocumentBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<DeletedCongressDocumentResponse> Handle(DeleteCongressDocumentCommand request, CancellationToken cancellationToken)
        {
            CongressDocument? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.CongressDocumentShouldExistWhenSelected(entity);
            CongressDocument deletedEntity = await _repository.DeleteAsync(entity!);
            return _mapper.Map<DeletedCongressDocumentResponse>(deletedEntity);
        }
    }
}
