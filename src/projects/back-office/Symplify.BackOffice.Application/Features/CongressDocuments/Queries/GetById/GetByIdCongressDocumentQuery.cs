using AutoMapper;
using Core.Application.Pipelines.Authorization;
using MediatR;
using Symplify.BackOffice.Application.Features.CongressDocuments.Constants;
using Symplify.BackOffice.Application.Features.CongressDocuments.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.CongressDocuments.Queries.GetById;
public class GetByIdCongressDocumentQuery : IRequest<GetByIdCongressDocumentResponse>, ISecuredRequest
{
    public Guid Id { get; set; }
    public string[] Roles => new[] { CongressDocumentsOperationClaims.Admin, CongressDocumentsOperationClaims.Read };
    public class GetByIdCongressDocumentQueryHandler : IRequestHandler<GetByIdCongressDocumentQuery, GetByIdCongressDocumentResponse>
    {
        private readonly ICongressDocumentRepository _repository; private readonly IMapper _mapper; private readonly CongressDocumentBusinessRules _rules;
        public GetByIdCongressDocumentQueryHandler(ICongressDocumentRepository repository, IMapper mapper, CongressDocumentBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<GetByIdCongressDocumentResponse> Handle(GetByIdCongressDocumentQuery request, CancellationToken cancellationToken)
        {
            CongressDocument? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.CongressDocumentShouldExistWhenSelected(entity);
            return _mapper.Map<GetByIdCongressDocumentResponse>(entity);
        }
    }
}
