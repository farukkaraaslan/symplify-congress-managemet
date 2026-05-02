using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.Languages.Constants;
using Symplify.BackOffice.Application.Features.Languages.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Localization;
namespace Symplify.BackOffice.Application.Features.Languages.Commands.Delete;
public class DeleteLanguageCommand : IRequest<DeletedLanguageResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetLanguages";
    public string[] Roles => new[] { LanguagesOperationClaims.Admin, LanguagesOperationClaims.Write, LanguagesOperationClaims.Delete };
    public class DeleteLanguageCommandHandler : IRequestHandler<DeleteLanguageCommand, DeletedLanguageResponse>
    {
        private readonly ILanguageRepository _repository; private readonly IMapper _mapper; private readonly LanguageBusinessRules _rules;
        public DeleteLanguageCommandHandler(ILanguageRepository repository, IMapper mapper, LanguageBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<DeletedLanguageResponse> Handle(DeleteLanguageCommand request, CancellationToken cancellationToken)
        {
            Language? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.LanguageShouldExistWhenSelected(entity);
            Language deletedEntity = await _repository.DeleteAsync(entity!);
            return _mapper.Map<DeletedLanguageResponse>(deletedEntity);
        }
    }
}
