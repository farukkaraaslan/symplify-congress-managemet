using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.Authors.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Submission;
namespace Symplify.BackOffice.Application.Features.Authors.Commands.Create;
public class CreateAuthorCommand : IRequest<CreatedAuthorResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Institution { get; set; }
    public string? Orcid { get; set; }
    public bool IsCorrespondingAuthor { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetAuthors";
    public string[] Roles => new[] { AuthorsOperationClaims.Admin, AuthorsOperationClaims.Write, AuthorsOperationClaims.Add };
    public class CreateAuthorCommandHandler : IRequestHandler<CreateAuthorCommand, CreatedAuthorResponse>
    {
        private readonly IAuthorRepository _repository;
        private readonly IMapper _mapper;
        public CreateAuthorCommandHandler(IAuthorRepository repository, IMapper mapper) { _repository = repository; _mapper = mapper; }
        public async Task<CreatedAuthorResponse> Handle(CreateAuthorCommand request, CancellationToken cancellationToken)
        {
            Author entity = new()
            {
                Id = Guid.NewGuid(),
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                Institution = request.Institution,
                Orcid = request.Orcid,
                IsCorrespondingAuthor = request.IsCorrespondingAuthor,
            };
            Author createdEntity = await _repository.AddAsync(entity);
            return _mapper.Map<CreatedAuthorResponse>(createdEntity);
        }
    }
}
