using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.Authors.Constants;
using Symplify.BackOffice.Application.Features.Authors.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Submission;
using Symplify.BackOffice.Application.Features.Authors.Commands.Update;
namespace Symplify.BackOffice.Application.Features.Authors.Commands.Update;
public class UpdateAuthorCommand : IRequest<UpdatedAuthorResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Institution { get; set; }
    public string? Orcid { get; set; }
    public bool IsCorrespondingAuthor { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetAuthors";
    public string[] Roles => new[] { AuthorsOperationClaims.Admin, AuthorsOperationClaims.Write, AuthorsOperationClaims.Update };
    public class UpdateAuthorCommandHandler : IRequestHandler<UpdateAuthorCommand, UpdatedAuthorResponse>
    {
        private readonly IAuthorRepository _repository; private readonly IMapper _mapper; private readonly AuthorBusinessRules _rules;
        public UpdateAuthorCommandHandler(IAuthorRepository repository, IMapper mapper, AuthorBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<UpdatedAuthorResponse> Handle(UpdateAuthorCommand request, CancellationToken cancellationToken)
        {
            Author? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.AuthorShouldExistWhenSelected(entity);
            entity!.FirstName = request.FirstName;
            entity!.LastName = request.LastName;
            entity!.Email = request.Email;
            entity!.Institution = request.Institution;
            entity!.Orcid = request.Orcid;
            entity!.IsCorrespondingAuthor = request.IsCorrespondingAuthor;
            Author updatedEntity = await _repository.UpdateAsync(entity!);
            return _mapper.Map<UpdatedAuthorResponse>(updatedEntity);
        }
    }
}
