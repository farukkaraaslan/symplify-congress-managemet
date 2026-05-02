using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.Languages.Constants;
using Symplify.BackOffice.Application.Features.Languages.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Localization;
namespace Symplify.BackOffice.Application.Features.Languages.Commands.Update;
public class UpdateLanguageCommand : IRequest<UpdatedLanguageResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Culture { get; set; } = string.Empty;
    public string? TwoLetterIsoCode { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public int Order { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetLanguages";
    public string[] Roles => new[] { LanguagesOperationClaims.Admin, LanguagesOperationClaims.Write, LanguagesOperationClaims.Update };
    public class UpdateLanguageCommandHandler : IRequestHandler<UpdateLanguageCommand, UpdatedLanguageResponse>
    {
        private readonly ILanguageRepository _repository; private readonly IMapper _mapper; private readonly LanguageBusinessRules _rules;
        public UpdateLanguageCommandHandler(ILanguageRepository repository, IMapper mapper, LanguageBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<UpdatedLanguageResponse> Handle(UpdateLanguageCommand request, CancellationToken cancellationToken)
        {
            Language? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.LanguageShouldExistWhenSelected(entity);
            entity!.Name = request.Name;
            entity!.Culture = request.Culture;
            entity!.TwoLetterIsoCode = request.TwoLetterIsoCode;
            entity!.IsDefault = request.IsDefault;
            entity!.IsActive = request.IsActive;
            entity!.Order = request.Order;
            Language updatedEntity = await _repository.UpdateAsync(entity!);
            return _mapper.Map<UpdatedLanguageResponse>(updatedEntity);
        }
    }
}
