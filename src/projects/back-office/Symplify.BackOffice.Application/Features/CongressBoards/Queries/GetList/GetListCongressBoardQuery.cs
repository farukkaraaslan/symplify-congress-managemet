using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.Application.Requests;
using Core.Application.Responses;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.CongressBoards.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.CongressBoards.Queries.GetList;

public class GetListCongressBoardQuery : IRequest<GetListResponse<GetListCongressBoardListItemDto>>, ISecuredRequest, ICachableRequest
{
    public PageRequest PageRequest { get; set; } = new();
    public Guid? CongressId { get; set; }
    public Guid? LanguageId { get; set; }
    public string? Culture { get; set; }
    public bool? IsActive { get; set; }
    public string? SearchText { get; set; }
    public string[] Roles => new[] { CongressBoardsOperationClaims.Admin, CongressBoardsOperationClaims.Read };
    public bool BypassCache { get; set; }
    public string CacheKey => $"GetListCongressBoards({PageRequest.Page},{PageRequest.PageSize},{CongressId},{LanguageId},{Culture},{IsActive},{SearchText})";
    public string CacheGroupKey => "GetCongressBoards";
    public TimeSpan? SlidingExpiration { get; }

    public class GetListCongressBoardQueryHandler : IRequestHandler<GetListCongressBoardQuery, GetListResponse<GetListCongressBoardListItemDto>>
    {
        private readonly ICongressBoardRepository _repository;
        private readonly ICongressBoardTranslationRepository _translationRepository;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly ICurrentLanguageProvider _currentLanguageProvider;
        private readonly ITranslationFallbackResolver _fallbackResolver;

        public GetListCongressBoardQueryHandler(
            ICongressBoardRepository repository,
            ICongressBoardTranslationRepository translationRepository,
            IApplicationLanguageProvider languageProvider,
            ICurrentLanguageProvider currentLanguageProvider,
            ITranslationFallbackResolver fallbackResolver)
        {
            _repository = repository;
            _translationRepository = translationRepository;
            _languageProvider = languageProvider;
            _currentLanguageProvider = currentLanguageProvider;
            _fallbackResolver = fallbackResolver;
        }

        public async Task<GetListResponse<GetListCongressBoardListItemDto>> Handle(GetListCongressBoardQuery request, CancellationToken cancellationToken)
        {
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            ApplicationLanguageDto requestedLanguage = await ResolveRequestedLanguageAsync(request.LanguageId, request.Culture, defaultLanguage, cancellationToken);

            List<CongressBoard> roots = _repository.Query().ToList().Where(board => !IsDeleted(board)).ToList();

            if (request.CongressId.HasValue && request.CongressId.Value != Guid.Empty)
                roots = roots.Where(board => board.CongressId == request.CongressId.Value).ToList();

            if (request.IsActive.HasValue)
                roots = roots.Where(board => board.IsActive == request.IsActive.Value).ToList();

            roots = roots.OrderBy(board => board.Order <= 0 ? int.MaxValue : board.Order).ThenBy(board => board.Id).ToList();

            HashSet<Guid> rootIds = roots.Select(board => board.Id).ToHashSet();
            List<CongressBoardTranslation> translations = _translationRepository.Query().ToList().Where(translation => rootIds.Contains(translation.CongressBoardId) && !IsDeleted(translation)).ToList();

            List<GetListCongressBoardListItemDto> projected = roots.Select(entity =>
            {
                List<CongressBoardTranslation> rootTranslations = translations.Where(translation => translation.CongressBoardId == entity.Id).ToList();
                CongressBoardTranslation? requestedTranslation = rootTranslations.FirstOrDefault(translation => translation.LanguageId == requestedLanguage.Id);
                CongressBoardTranslation? displayTranslation = _fallbackResolver.Resolve(rootTranslations, requestedLanguage.Id, defaultLanguage.Id);

                return new GetListCongressBoardListItemDto
                {
                    Id = entity.Id,
                    CongressId = entity.CongressId,
                    Order = entity.Order,
                    IsActive = entity.IsActive,
                    Name = displayTranslation?.Name ?? string.Empty,
                    Description = displayTranslation?.Description,
                    DisplayLanguageId = displayTranslation?.LanguageId ?? default,
                    IsFallback = requestedTranslation is null && displayTranslation is not null
                };
            }).ToList();

            if (!string.IsNullOrWhiteSpace(request.SearchText))
            {
                string searchText = request.SearchText.Trim();
                projected = projected.Where(item => item.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            int page = request.PageRequest.Page < 0 ? 0 : request.PageRequest.Page;
            int pageSize = request.PageRequest.PageSize <= 0 ? 20 : request.PageRequest.PageSize;
            int total = projected.Count;
            int pages = (int)Math.Ceiling(total / (double)pageSize);

            return new GetListResponse<GetListCongressBoardListItemDto>
            {
                Index = page,
                Size = pageSize,
                Count = total,
                Pages = pages,
                HasPrevious = page > 0,
                HasNext = page + 1 < pages,
                Items = projected.Skip(page * pageSize).Take(pageSize).ToList()
            };
        }

        private async Task<ApplicationLanguageDto> ResolveRequestedLanguageAsync(Guid? languageId, string? culture, ApplicationLanguageDto defaultLanguage, CancellationToken cancellationToken)
        {
            if (languageId.HasValue)
                return await _languageProvider.GetByIdAsync(languageId.Value, cancellationToken) ?? defaultLanguage;

            if (!string.IsNullOrWhiteSpace(culture))
                return await _languageProvider.GetByCultureAsync(culture, cancellationToken) ?? defaultLanguage;

            return await _currentLanguageProvider.GetCurrentLanguageAsync(cancellationToken);
        }

        private static bool IsDeleted(object entity)
            => LocalizedEntityRuntimeHelper.GetPropertyValue(entity, "DeletedDate") is not null;
    }
}
