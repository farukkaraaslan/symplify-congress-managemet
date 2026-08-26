using System.Globalization;
using System.Text;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.CongressBoardMembers.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Lookups;

namespace Symplify.BackOffice.Application.Features.CongressBoardMembers.Commands.ImportExcel;

public sealed class ImportCongressBoardMembersFromExcelCommand : IRequest<ImportCongressBoardMembersFromExcelResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid CongressId { get; set; }

    public List<CongressBoardMemberExcelImportRowDto> Rows { get; set; } = new();

    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCongressBoardMembers";
    public string[] Roles => new[] { CongressBoardMembersOperationClaims.Admin, CongressBoardMembersOperationClaims.Write, CongressBoardMembersOperationClaims.Add };

    public sealed class ImportCongressBoardMembersFromExcelCommandHandler : IRequestHandler<ImportCongressBoardMembersFromExcelCommand, ImportCongressBoardMembersFromExcelResponse>
    {
        private readonly ICongressBoardRepository _boardRepository;
        private readonly ICongressBoardTranslationRepository _boardTranslationRepository;
        private readonly ICongressBoardMemberRepository _memberRepository;
        private readonly ICongressBoardMemberTranslationRepository _memberTranslationRepository;
        private readonly ITitleRepository _titleRepository;
        private readonly ITitleTranslationRepository _titleTranslationRepository;
        private readonly IApplicationLanguageProvider _languageProvider;

        public ImportCongressBoardMembersFromExcelCommandHandler(
            ICongressBoardRepository boardRepository,
            ICongressBoardTranslationRepository boardTranslationRepository,
            ICongressBoardMemberRepository memberRepository,
            ICongressBoardMemberTranslationRepository memberTranslationRepository,
            ITitleRepository titleRepository,
            ITitleTranslationRepository titleTranslationRepository,
            IApplicationLanguageProvider languageProvider)
        {
            _boardRepository = boardRepository;
            _boardTranslationRepository = boardTranslationRepository;
            _memberRepository = memberRepository;
            _memberTranslationRepository = memberTranslationRepository;
            _titleRepository = titleRepository;
            _titleTranslationRepository = titleTranslationRepository;
            _languageProvider = languageProvider;
        }

        public async Task<ImportCongressBoardMembersFromExcelResponse> Handle(
            ImportCongressBoardMembersFromExcelCommand request,
            CancellationToken cancellationToken)
        {
            ImportCongressBoardMembersFromExcelResponse response = new();

            if (request.CongressId == Guid.Empty)
            {
                response.Errors.Add(CongressBoardMembersMessages.CongressRequired);
                return response;
            }

            if (request.Rows.Count == 0)
            {
                response.Errors.Add(CongressBoardMembersMessages.ImportFileEmpty);
                return response;
            }

            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            Dictionary<string, string?> academicTitleLookup = BuildAcademicTitleLookup(defaultLanguage.Id);

            foreach (CongressBoardMemberExcelImportRowDto row in request.Rows)
            {
                string? boardName = Normalize(row.BoardName);
                string? fullName = Normalize(row.FullName);

                if (string.IsNullOrWhiteSpace(boardName))
                {
                    response.SkippedCount++;
                    response.Errors.Add($"{row.RowNumber}. satır: Kurul Türü zorunludur.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(fullName))
                {
                    response.SkippedCount++;
                    response.Errors.Add($"{row.RowNumber}. satır: Ad Soyad zorunludur.");
                    continue;
                }

                CongressBoard? board = await FindBoardAsync(request.CongressId, boardName, defaultLanguage, cancellationToken);

                if (board is null)
                {
                    response.SkippedCount++;
                    response.Errors.Add($"{row.RowNumber}. satır: Kurul Türü bulunamadı: {boardName}");
                    continue;
                }

                string? academicTitle = ResolveAcademicTitle(row.AcademicTitle, academicTitleLookup);

                if (!string.IsNullOrWhiteSpace(row.AcademicTitle) && academicTitle is null)
                {
                    response.SkippedCount++;
                    response.Errors.Add($"{row.RowNumber}. satır: Akademik Ünvan bulunamadı: {row.AcademicTitle}");
                    continue;
                }

                int order = GetNextMemberOrder(board.Id);

                CongressBoardMember member = new()
                {
                    Id = Guid.NewGuid(),
                    CongressBoardId = board.Id,
                    FullName = fullName,
                    AcademicTitle = academicTitle,
                    Institution = Normalize(row.Institution),
                    ImagePath = null,
                    Order = order,
                    IsActive = row.IsActive
                };

                CongressBoardMember createdMember = await _memberRepository.AddAsync(member);

                await _memberTranslationRepository.AddAsync(new CongressBoardMemberTranslation
                {
                    Id = Guid.NewGuid(),
                    CongressBoardMemberId = createdMember.Id,
                    LanguageId = defaultLanguage.Id,
                    FullName = createdMember.FullName,
                    Title = createdMember.AcademicTitle,
                    Institution = createdMember.Institution,
                    Biography = Normalize(row.Description)
                });

                response.ImportedCount++;
            }

            return response;
        }

        private async Task<CongressBoard?> FindBoardAsync(
            Guid congressId,
            string boardName,
            ApplicationLanguageDto defaultLanguage,
            CancellationToken cancellationToken)
        {
            List<CongressBoard> boards = _boardRepository
                .Query()
                .ToList()
                .Where(board => board.CongressId == congressId && !IsDeleted(board))
                .ToList();

            HashSet<Guid> boardIds = boards.Select(board => board.Id).ToHashSet();

            List<CongressBoardTranslation> translations = _boardTranslationRepository
                .Query()
                .ToList()
                .Where(translation => boardIds.Contains(translation.CongressBoardId) && !IsDeleted(translation))
                .ToList();

            string normalizedBoardName = NormalizeKey(boardName);

            CongressBoardTranslation? matchedTranslation = translations
                .FirstOrDefault(translation =>
                    translation.LanguageId == defaultLanguage.Id &&
                    string.Equals(NormalizeKey(translation.Name), normalizedBoardName, StringComparison.OrdinalIgnoreCase))
                ?? translations.FirstOrDefault(translation =>
                    string.Equals(NormalizeKey(translation.Name), normalizedBoardName, StringComparison.OrdinalIgnoreCase));

            return matchedTranslation is null
                ? null
                : boards.FirstOrDefault(board => board.Id == matchedTranslation.CongressBoardId);
        }

        private Dictionary<string, string?> BuildAcademicTitleLookup(Guid defaultLanguageId)
        {
            Dictionary<string, string?> lookup = new(StringComparer.OrdinalIgnoreCase)
            {
                [NormalizeKey("-")] = null,
                [NormalizeKey("Ünvansız")] = null,
                [NormalizeKey("Unvansız")] = null,
                [NormalizeKey("No Title")] = null
            };

            List<Title> titles = _titleRepository
                .Query()
                .ToList()
                .Where(title => title.IsActive && !IsDeleted(title))
                .ToList();

            HashSet<Guid> titleIds = titles.Select(title => title.Id).ToHashSet();

            List<TitleTranslation> translations = _titleTranslationRepository
                .Query()
                .ToList()
                .Where(translation => titleIds.Contains(translation.TitleId) && !IsDeleted(translation))
                .ToList();

            foreach (Title title in titles)
            {
                TitleTranslation? defaultTranslation = translations
                    .FirstOrDefault(translation => translation.TitleId == title.Id && translation.LanguageId == defaultLanguageId)
                    ?? translations.FirstOrDefault(translation => translation.TitleId == title.Id);

                string? displayName = Normalize(defaultTranslation?.Name);

                if (string.IsNullOrWhiteSpace(displayName))
                    continue;

                lookup[NormalizeKey(displayName)] = displayName;

                if (!string.IsNullOrWhiteSpace(title.Code))
                    lookup[NormalizeKey(title.Code)] = displayName;
            }

            return lookup;
        }

        private static string? ResolveAcademicTitle(string? value, Dictionary<string, string?> lookup)
        {
            string? normalized = Normalize(value);

            if (string.IsNullOrWhiteSpace(normalized))
                return null;

            return lookup.TryGetValue(NormalizeKey(normalized), out string? title)
                ? title
                : null;
        }

        private static string NormalizeKey(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            string normalized = value.Trim().Replace('\u00A0', ' ');
            normalized = ReplaceTurkishCharacters(normalized);
            normalized = RemoveDiacritics(normalized).ToLowerInvariant();
            normalized = string.Join(' ', normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries));

            return normalized;
        }

        private static string ReplaceTurkishCharacters(string value)
        {
            return value
                .Replace('Ç', 'C')
                .Replace('ç', 'c')
                .Replace('Ğ', 'G')
                .Replace('ğ', 'g')
                .Replace('İ', 'I')
                .Replace('ı', 'i')
                .Replace('Ö', 'O')
                .Replace('ö', 'o')
                .Replace('Ş', 'S')
                .Replace('ş', 's')
                .Replace('Ü', 'U')
                .Replace('ü', 'u');
        }

        private static string RemoveDiacritics(string value)
        {
            string normalized = value.Normalize(NormalizationForm.FormD);
            StringBuilder builder = new();

            foreach (char character in normalized)
            {
                UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(character);

                if (category != UnicodeCategory.NonSpacingMark)
                    builder.Append(character);
            }

            return builder.ToString().Normalize(NormalizationForm.FormC);
        }

        private int GetNextMemberOrder(Guid boardId)
        {
            List<CongressBoardMember> members = _memberRepository
                .Query()
                .ToList()
                .Where(member => member.CongressBoardId == boardId && !IsDeleted(member))
                .ToList();

            return members.Count == 0 ? 1 : members.Max(member => member.Order <= 0 ? 0 : member.Order) + 1;
        }

        private static string? Normalize(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static bool IsDeleted(object entity)
            => LocalizedEntityRuntimeHelper.GetPropertyValue(entity, "DeletedDate") is not null;
    }
}
