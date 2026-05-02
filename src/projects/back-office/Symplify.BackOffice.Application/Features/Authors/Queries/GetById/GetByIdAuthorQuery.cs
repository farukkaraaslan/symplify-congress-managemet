using AutoMapper;
using Core.Application.Pipelines.Authorization;
using MediatR;
using Symplify.BackOffice.Application.Features.Authors.Constants;
using Symplify.BackOffice.Application.Features.Authors.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Submission;
namespace Symplify.BackOffice.Application.Features.Authors.Queries.GetById;
public class GetByIdAuthorQuery : IRequest<GetByIdAuthorResponse>, ISecuredRequest
{
    public Guid Id { get; set; }
    public string[] Roles => new[] { AuthorsOperationClaims.Admin, AuthorsOperationClaims.Read };
    public class GetByIdAuthorQueryHandler : IRequestHandler<GetByIdAuthorQuery, GetByIdAuthorResponse>
    {
        private readonly IAuthorRepository _repository; private readonly IMapper _mapper; private readonly AuthorBusinessRules _rules;
        public GetByIdAuthorQueryHandler(IAuthorRepository repository, IMapper mapper, AuthorBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<GetByIdAuthorResponse> Handle(GetByIdAuthorQuery request, CancellationToken cancellationToken)
        {
            Author? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.AuthorShouldExistWhenSelected(entity);
            return _mapper.Map<GetByIdAuthorResponse>(entity);
        }
    }
}
