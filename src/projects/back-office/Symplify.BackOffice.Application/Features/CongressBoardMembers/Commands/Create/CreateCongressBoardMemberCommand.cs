using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.CongressBoardMembers.Constants;
using Symplify.BackOffice.Application.Features.CongressBoardMembers.Rules;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.CongressBoardMembers.Commands.Create;
public class CreateCongressBoardMemberCommand : IRequest<CreatedCongressBoardMemberResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid CongressBoardId { get; set; }
    public string? ImagePath { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
    public ICollection<TranslationInputDto> Translations { get; set; } = new List<TranslationInputDto>();
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCongressBoardMembers";
    public string[] Roles => new[] { CongressBoardMembersOperationClaims.Admin, CongressBoardMembersOperationClaims.Write, CongressBoardMembersOperationClaims.Add };
    public class CreateCongressBoardMemberCommandHandler : IRequestHandler<CreateCongressBoardMemberCommand, CreatedCongressBoardMemberResponse>
    {
        private static readonly string[] TranslationFieldNames = new[] { "FullName", "Title", "Institution", "Biography" };
        private readonly ICongressBoardMemberRepository _repository; private readonly ICongressBoardMemberTranslationRepository _translationRepository; private readonly IApplicationLanguageProvider _languageProvider; private readonly IMapper _mapper; private readonly CongressBoardMemberBusinessRules _rules;
        public CreateCongressBoardMemberCommandHandler(ICongressBoardMemberRepository repository, ICongressBoardMemberTranslationRepository translationRepository, IApplicationLanguageProvider languageProvider, IMapper mapper, CongressBoardMemberBusinessRules rules) { _repository = repository; _translationRepository = translationRepository; _languageProvider = languageProvider; _mapper = mapper; _rules = rules; }
        public async Task<CreatedCongressBoardMemberResponse> Handle(CreateCongressBoardMemberCommand request, CancellationToken cancellationToken)
        {
            await _rules.DefaultTranslationShouldExist(request.Translations, cancellationToken);
            CongressBoardMember entity = new()
            {
                Id = Guid.NewGuid(),
                CongressBoardId = request.CongressBoardId,
                ImagePath = request.ImagePath,
                Order = request.Order,
                IsActive = request.IsActive,
            };
            CongressBoardMember createdEntity = await _repository.AddAsync(entity);
            IReadOnlyList<ApplicationLanguageDto> activeLanguages = await _languageProvider.GetActiveLanguagesAsync(cancellationToken);
            HashSet<Guid> activeLanguageIds = activeLanguages.Select(x => x.Id).ToHashSet();
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            foreach (TranslationInputDto input in request.Translations)
            {
                if (!activeLanguageIds.Contains(input.LanguageId)) continue;
                bool isDefaultLanguage = input.LanguageId == defaultLanguage.Id;
                bool hasAnyValue = LocalizedEntityRuntimeHelper.HasAnyValue(input.Fields, TranslationFieldNames);
                if (!isDefaultLanguage && !hasAnyValue) continue;
                CongressBoardMemberTranslation translation = new();
                LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "Id", Guid.NewGuid());
                LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "CongressBoardMemberId", createdEntity.Id);
                LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "LanguageId", input.LanguageId);
                LocalizedEntityRuntimeHelper.ApplyFieldDictionary(translation, TranslationFieldNames, input.Fields);
                await _translationRepository.AddAsync(translation);
            }
            return _mapper.Map<CreatedCongressBoardMemberResponse>(createdEntity);
        }
    }
}
