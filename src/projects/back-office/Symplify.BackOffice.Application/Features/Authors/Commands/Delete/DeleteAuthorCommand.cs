using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.Authors.Constants;
using Symplify.BackOffice.Application.Features.Authors.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Submission;
using Symplify.BackOffice.Application.Features.Authors.Commands.Delete;
namespace Symplify.BackOffice.Application.Features.Authors.Commands.Delete;
public class DeleteAuthorCommand : IRequest<DeletedAuthorResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetAuthors";
    public string[] Roles => new[] { AuthorsOperationClaims.Admin, AuthorsOperationClaims.Write, AuthorsOperationClaims.Delete };
    public class DeleteAuthorCommandHandler : IRequestHandler<DeleteAuthorCommand, DeletedAuthorResponse>
    {
        private readonly IAuthorRepository _repository; private readonly IMapper _mapper; private readonly AuthorBusinessRules _rules;
        public DeleteAuthorCommandHandler(IAuthorRepository repository, IMapper mapper, AuthorBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<DeletedAuthorResponse> Handle(DeleteAuthorCommand request, CancellationToken cancellationToken)
        {
            Author? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.AuthorShouldExistWhenSelected(entity);
            Author deletedEntity = await _repository.DeleteAsync(entity!);
            return _mapper.Map<DeletedAuthorResponse>(deletedEntity);
        }
    }
}
