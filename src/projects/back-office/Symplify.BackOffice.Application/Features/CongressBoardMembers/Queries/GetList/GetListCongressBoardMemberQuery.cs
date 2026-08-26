using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.Application.Requests;
using Core.Application.Responses;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.CongressBoardMembers.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.CongressBoardMembers.Queries.GetList;

public class GetListCongressBoardMemberQuery : IRequest<GetListResponse<GetListCongressBoardMemberListItemDto>>, ISecuredRequest, ICachableRequest
{
    public PageRequest PageRequest { get; set; } = new();

    public Guid? CongressId { get; set; }

    public Guid? CongressBoardId { get; set; }

    public Guid? LanguageId { get; set; }

    public string? Culture { get; set; }

    public string? SearchText { get; set; }

    public string? BoardName { get; set; }

    public string? AcademicTitle { get; set; }

    public bool? IsActive { get; set; }

    public string? SortColumn { get; set; }

    public string? SortDirection { get; set; }

    public string[] Roles => new[] { CongressBoardMembersOperationClaims.Admin, CongressBoardMembersOperationClaims.Read };

    public bool BypassCache { get; }

    public string CacheKey => $"GetListCongressBoardMembers({PageRequest.Page},{PageRequest.PageSize},{CongressId},{CongressBoardId},{LanguageId},{Culture},{SearchText},{BoardName},{AcademicTitle},{IsActive},{SortColumn},{SortDirection})";

    public string CacheGroupKey => "GetCongressBoardMembers";

    public TimeSpan? SlidingExpiration { get; }

    public class GetListCongressBoardMemberQueryHandler : IRequestHandler<GetListCongressBoardMemberQuery, GetListResponse<GetListCongressBoardMemberListItemDto>>
    {
        private readonly ICongressBoardMemberRepository _repository;
        private readonly ICongressBoardRepository _boardRepository;
        private readonly ICongressBoardTranslationRepository _boardTranslationRepository;
        private readonly ICongressBoardMemberTranslationRepository _translationRepository;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly ICurrentLanguageProvider _currentLanguageProvider;
        private readonly ITranslationFallbackResolver _fallbackResolver;

        public GetListCongressBoardMemberQueryHandler(
            ICongressBoardMemberRepository repository,
            ICongressBoardRepository boardRepository,
            ICongressBoardTranslationRepository boardTranslationRepository,
            ICongressBoardMemberTranslationRepository translationRepository,
            IApplicationLanguageProvider languageProvider,
            ICurrentLanguageProvider currentLanguageProvider,
            ITranslationFallbackResolver fallbackResolver)
        {
            _repository = repository;
            _boardRepository = boardRepository;
            _boardTranslationRepository = boardTranslationRepository;
            _translationRepository = translationRepository;
            _languageProvider = languageProvider;
            _currentLanguageProvider = currentLanguageProvider;
            _fallbackResolver = fallbackResolver;
        }

        public async Task<GetListResponse<GetListCongressBoardMemberListItemDto>> Handle(
            GetListCongressBoardMemberQuery request,
            CancellationToken cancellationToken)
        {
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            ApplicationLanguageDto requestedLanguage = await ResolveRequestedLanguageAsync(request.LanguageId, request.Culture, defaultLanguage, cancellationToken);

            List<CongressBoard> boards = _boardRepository.Query().ToList().Where(board => !IsDeleted(board)).ToList();

            if (request.CongressId.HasValue && request.CongressId.Value != Guid.Empty)
                boards = boards.Where(board => board.CongressId == request.CongressId.Value).ToList();

            if (request.CongressBoardId.HasValue && request.CongressBoardId.Value != Guid.Empty)
                boards = boards.Where(board => board.Id == request.CongressBoardId.Value).ToList();

            HashSet<Guid> boardIds = boards.Select(board => board.Id).ToHashSet();

            List<CongressBoardTranslation> boardTranslations = _boardTranslationRepository
                .Query()
                .ToList()
                .Where(translation => boardIds.Contains(translation.CongressBoardId) && !IsDeleted(translation))
                .ToList();

            Dictionary<Guid, string> boardNameMap = boards.ToDictionary(
                board => board.Id,
                board => ResolveBoardName(board.Id, boardTranslations, requestedLanguage.Id, defaultLanguage.Id));

            List<CongressBoardMember> roots = _repository
                .Query()
                .ToList()
                .Where(member => boardIds.Contains(member.CongressBoardId) && !IsDeleted(member))
                .ToList();

            if (request.IsActive.HasValue)
                roots = roots.Where(member => member.IsActive == request.IsActive.Value).ToList();

            if (!string.IsNullOrWhiteSpace(request.BoardName))
            {
                string requestedBoardName = request.BoardName.Trim();
                HashSet<Guid> filteredBoardIds = boardNameMap
                    .Where(item => string.Equals(item.Value, requestedBoardName, StringComparison.OrdinalIgnoreCase))
                    .Select(item => item.Key)
                    .ToHashSet();

                roots = roots.Where(member => filteredBoardIds.Contains(member.CongressBoardId)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(request.AcademicTitle))
            {
                string requestedTitle = request.AcademicTitle.Trim();
                roots = roots.Where(member => string.Equals(member.AcademicTitle?.Trim() ?? "-", requestedTitle, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(request.SearchText))
            {
                string searchText = request.SearchText.Trim();
                roots = roots.Where(member =>
                    Contains(member.FullName, searchText) ||
                    Contains(member.AcademicTitle, searchText) ||
                    Contains(member.Institution, searchText) ||
                    Contains(boardNameMap.TryGetValue(member.CongressBoardId, out string? boardName) ? boardName : null, searchText))
                    .ToList();
            }

            HashSet<Guid> memberIds = roots.Select(member => member.Id).ToHashSet();

            List<CongressBoardMemberTranslation> memberTranslations = _translationRepository
                .Query()
                .ToList()
                .Where(translation => memberIds.Contains(translation.CongressBoardMemberId) && !IsDeleted(translation))
                .ToList();

            List<GetListCongressBoardMemberListItemDto> projected = roots.Select(member =>
            {
                List<CongressBoardMemberTranslation> rootTranslations = memberTranslations
                    .Where(translation => translation.CongressBoardMemberId == member.Id)
                    .ToList();

                CongressBoardMemberTranslation? requestedTranslation = rootTranslations.FirstOrDefault(translation => translation.LanguageId == requestedLanguage.Id);
                CongressBoardMemberTranslation? displayTranslation = _fallbackResolver.Resolve(rootTranslations, requestedLanguage.Id, defaultLanguage.Id);

                return new GetListCongressBoardMemberListItemDto
                {
                    Id = member.Id,
                    CongressBoardId = member.CongressBoardId,
                    CongressId = boards.FirstOrDefault(board => board.Id == member.CongressBoardId)?.CongressId ?? Guid.Empty,
                    BoardName = boardNameMap.TryGetValue(member.CongressBoardId, out string? boardName) ? boardName : string.Empty,
                    ImagePath = member.ImagePath,
                    HasImage =
                        !string.IsNullOrWhiteSpace(member.ImageObjectName) ||
                        !string.IsNullOrWhiteSpace(member.ImagePath),
                    Order = member.Order,
                    IsActive = member.IsActive,
                    IsAcceptanceLetterSigner = member.IsAcceptanceLetterSigner,
                    HasSignature = !string.IsNullOrWhiteSpace(member.SignatureObjectName) || !string.IsNullOrWhiteSpace(member.SignaturePath),
                    FullName = member.FullName,
                    AcademicTitle = member.AcademicTitle,
                    Institution = member.Institution,
                    Description = displayTranslation?.Biography,
                    DisplayLanguageId = displayTranslation?.LanguageId ?? default,
                    IsFallback = requestedTranslation is null && displayTranslation is not null
                };
            }).ToList();

            projected = ApplySorting(projected, request.SortColumn, request.SortDirection);

            int page = request.PageRequest.Page < 0 ? 0 : request.PageRequest.Page;
            int pageSize = request.PageRequest.PageSize <= 0 ? 20 : request.PageRequest.PageSize;
            int total = projected.Count;
            int pages = (int)Math.Ceiling(total / (double)pageSize);

            List<GetListCongressBoardMemberListItemDto> items = projected
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToList();

            return new GetListResponse<GetListCongressBoardMemberListItemDto>
            {
                Index = page,
                Size = pageSize,
                Count = total,
                Pages = pages,
                HasPrevious = page > 0,
                HasNext = page + 1 < pages,
                Items = items
            };
        }

        private string ResolveBoardName(
            Guid boardId,
            List<CongressBoardTranslation> translations,
            Guid requestedLanguageId,
            Guid defaultLanguageId)
        {
            List<CongressBoardTranslation> boardTranslations = translations
                .Where(translation => translation.CongressBoardId == boardId)
                .ToList();

            CongressBoardTranslation? translation = _fallbackResolver.Resolve(boardTranslations, requestedLanguageId, defaultLanguageId);

            return translation?.Name ?? string.Empty;
        }

        private static List<GetListCongressBoardMemberListItemDto> ApplySorting(
            List<GetListCongressBoardMemberListItemDto> items,
            string? sortColumn,
            string? sortDirection)
        {
            bool descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
            string normalizedColumn = string.IsNullOrWhiteSpace(sortColumn) ? "order" : sortColumn.Trim().ToLowerInvariant();

            IOrderedEnumerable<GetListCongressBoardMemberListItemDto> ordered = normalizedColumn switch
            {
                "boardname" or "board" => descending
                    ? items.OrderByDescending(item => item.BoardName)
                    : items.OrderBy(item => item.BoardName),
                "academictitle" or "title" => descending
                    ? items.OrderByDescending(item => item.AcademicTitle)
                    : items.OrderBy(item => item.AcademicTitle),
                "fullname" or "name" => descending
                    ? items.OrderByDescending(item => item.FullName)
                    : items.OrderBy(item => item.FullName),
                "institution" => descending
                    ? items.OrderByDescending(item => item.Institution)
                    : items.OrderBy(item => item.Institution),
                "isacceptancelettersigner" or "signer" or "signatureauthority" => descending
                    ? items.OrderByDescending(item => item.IsAcceptanceLetterSigner)
                    : items.OrderBy(item => item.IsAcceptanceLetterSigner),
                "isactive" or "status" => descending
                    ? items.OrderByDescending(item => item.IsActive)
                    : items.OrderBy(item => item.IsActive),
                _ => descending
                    ? items.OrderByDescending(item => item.Order <= 0 ? int.MaxValue : item.Order)
                    : items.OrderBy(item => item.Order <= 0 ? int.MaxValue : item.Order)
            };

            return ordered
                .ThenBy(item => item.BoardName)
                .ThenBy(item => item.Order <= 0 ? int.MaxValue : item.Order)
                .ThenBy(item => item.FullName)
                .ToList();
        }

        private async Task<ApplicationLanguageDto> ResolveRequestedLanguageAsync(
            Guid? languageId,
            string? culture,
            ApplicationLanguageDto defaultLanguage,
            CancellationToken cancellationToken)
        {
            if (languageId.HasValue)
                return await _languageProvider.GetByIdAsync(languageId.Value, cancellationToken) ?? defaultLanguage;

            if (!string.IsNullOrWhiteSpace(culture))
                return await _languageProvider.GetByCultureAsync(culture, cancellationToken) ?? defaultLanguage;

            return await _currentLanguageProvider.GetCurrentLanguageAsync(cancellationToken);
        }

        private static bool Contains(string? source, string value)
            => !string.IsNullOrWhiteSpace(source) && source.Contains(value, StringComparison.OrdinalIgnoreCase);

        private static bool IsDeleted(object entity)
            => LocalizedEntityRuntimeHelper.GetPropertyValue(entity, "DeletedDate") is not null;
    }
}
