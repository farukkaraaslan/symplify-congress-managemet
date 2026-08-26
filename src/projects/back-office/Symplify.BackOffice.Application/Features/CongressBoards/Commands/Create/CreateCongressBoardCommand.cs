using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.CongressBoards.Constants;
using Symplify.BackOffice.Application.Features.CongressBoards.Rules;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.CongressBoards.Commands.Create;

public class CreateCongressBoardCommand : IRequest<CreatedCongressBoardResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid CongressId { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<TranslationInputDto> Translations { get; set; } = new List<TranslationInputDto>();

    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCongressBoards";
    public string[] Roles => new[] { CongressBoardsOperationClaims.Admin, CongressBoardsOperationClaims.Write, CongressBoardsOperationClaims.Add };

    public class CreateCongressBoardCommandHandler : IRequestHandler<CreateCongressBoardCommand, CreatedCongressBoardResponse>
    {
        private static readonly string[] TranslationFieldNames = new[] { "Name", "Description" };

        private readonly ICongressBoardRepository _repository;
        private readonly ICongressBoardTranslationRepository _translationRepository;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly IMapper _mapper;
        private readonly CongressBoardBusinessRules _rules;

        public CreateCongressBoardCommandHandler(
            ICongressBoardRepository repository,
            ICongressBoardTranslationRepository translationRepository,
            IApplicationLanguageProvider languageProvider,
            IMapper mapper,
            CongressBoardBusinessRules rules)
        {
            _repository = repository;
            _translationRepository = translationRepository;
            _languageProvider = languageProvider;
            _mapper = mapper;
            _rules = rules;
        }

        public async Task<CreatedCongressBoardResponse> Handle(
            CreateCongressBoardCommand request,
            CancellationToken cancellationToken)
        {
            await _rules.CongressShouldBeSelected(request.CongressId);
            await _rules.DefaultTranslationShouldExist(request.Translations, cancellationToken);

            int order = request.Order > 0
                ? request.Order
                : GetNextOrder(request.CongressId);

            CongressBoard entity = new()
            {
                Id = Guid.NewGuid(),
                CongressId = request.CongressId,
                Order = order,
                IsActive = request.IsActive
            };

            CongressBoard createdEntity = await _repository.AddAsync(entity);
            await AddTranslationsAsync(createdEntity.Id, request.Translations, cancellationToken);

            return _mapper.Map<CreatedCongressBoardResponse>(createdEntity);
        }

        private int GetNextOrder(Guid congressId)
        {
            List<CongressBoard> boards = _repository
                .Query()
                .ToList()
                .Where(board => board.CongressId == congressId && !IsDeleted(board))
                .ToList();

            return boards.Count == 0
                ? 1
                : boards.Max(board => board.Order <= 0 ? 0 : board.Order) + 1;
        }

        private async Task AddTranslationsAsync(
            Guid congressBoardId,
            IEnumerable<TranslationInputDto> translations,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<ApplicationLanguageDto> activeLanguages = await _languageProvider.GetActiveLanguagesAsync(cancellationToken);
            HashSet<Guid> activeLanguageIds = activeLanguages.Select(language => language.Id).ToHashSet();
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);

            foreach (TranslationInputDto input in translations.GroupBy(translation => translation.LanguageId).Select(group => group.First()))
            {
                if (!activeLanguageIds.Contains(input.LanguageId))
                    continue;

                bool isDefaultLanguage = input.LanguageId == defaultLanguage.Id;
                bool hasAnyValue = LocalizedEntityRuntimeHelper.HasAnyValue(input.Fields, TranslationFieldNames);

                if (!isDefaultLanguage && !hasAnyValue)
                    continue;

                CongressBoardTranslation translation = new();
                LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "Id", Guid.NewGuid());
                LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "CongressBoardId", congressBoardId);
                LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "LanguageId", input.LanguageId);
                LocalizedEntityRuntimeHelper.ApplyFieldDictionary(translation, TranslationFieldNames, input.Fields);

                await _translationRepository.AddAsync(translation);
            }
        }

        private static bool IsDeleted(object entity)
            => LocalizedEntityRuntimeHelper.GetPropertyValue(entity, "DeletedDate") is not null;
    }
}
