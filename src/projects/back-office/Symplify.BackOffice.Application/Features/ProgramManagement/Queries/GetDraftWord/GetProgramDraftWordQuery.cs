using System.Text.Json;
using Core.Application.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Core.Application.Pipelines.Authorization;
using MediatR;
using Symplify.BackOffice.Application.Features.AbstractBook.Models;
using Symplify.BackOffice.Application.Features.ProgramManagement.Constants;
using Symplify.BackOffice.Application.Features.ProgramManagement.Models;
using Symplify.BackOffice.Application.Features.ProgramManagement.Services;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.ProgramManagement.Queries.GetDraftWord;

public sealed class GetProgramDraftWordQuery : IRequest<ProgramDraftWordResponse>, ISecuredRequest
{
    public Guid CongressId { get; set; }
    public string? Culture { get; set; }
    public ProgramBookCoverDto Cover { get; set; } = new();
    public ProgramBookRenderOptionsDto Options { get; set; } = new();
    public string? PublicBaseUrl { get; set; }
    public string[] Roles => ProgramManagementOperationClaims.AdminOnly;

    public sealed class Handler : IRequestHandler<GetProgramDraftWordQuery, ProgramDraftWordResponse>
    {
        private const int MaxHeaderLogoSizeInBytes = 5 * 1024 * 1024;

        private readonly IProgramManagementRepository _repository;
        private readonly IAbstractBookRepository _abstractBookRepository;
        private readonly IProgramDraftWordRenderer _renderer;
        private readonly IObjectStorageService _objectStorageService;
        private readonly ObjectStorageOptions _storageOptions;
        private readonly ILogger<Handler> _logger;

        public Handler(
            IProgramManagementRepository repository,
            IAbstractBookRepository abstractBookRepository,
            IProgramDraftWordRenderer renderer,
            IObjectStorageService objectStorageService,
            IOptions<ObjectStorageOptions> storageOptions,
            ILogger<Handler> logger)
        {
            _repository = repository;
            _abstractBookRepository = abstractBookRepository;
            _renderer = renderer;
            _objectStorageService = objectStorageService;
            _storageOptions = storageOptions.Value;
            _logger = logger;
        }

        public async Task<ProgramDraftWordResponse> Handle(
            GetProgramDraftWordQuery request,
            CancellationToken cancellationToken)
        {
            CongressProgramPlan plan = await _repository.GetPlanForDisplayAsync(
                request.CongressId,
                cancellationToken)
                ?? throw new InvalidOperationException("Word oluşturmak için program taslağı bulunamadı.");

            ProgramGenerationSourceDto source = await _repository.GetGenerationSourceAsync(
                request.CongressId,
                null,
                request.Culture,
                cancellationToken,
                BuildDisplayFilter(plan))
                ?? throw new InvalidOperationException("Aktif kongre bulunamadı.");

            ProgramPlanDto dto = ProgramPlanMapper.Map(plan, source);
            ProgramBookPageHeaderDto pageHeader = await BuildPageHeaderAsync(
                request,
                source,
                cancellationToken);

            byte[] content = _renderer.Render(
                source.CongressName,
                dto,
                request.Culture,
                request.Cover,
                request.Options,
                request.PublicBaseUrl,
                pageHeader);
            string fileName = BuildFileName(source.CongressName);

            return new ProgramDraftWordResponse(content, fileName);
        }

        private async Task<ProgramBookPageHeaderDto> BuildPageHeaderAsync(
            GetProgramDraftWordQuery request,
            ProgramGenerationSourceDto source,
            CancellationToken cancellationToken)
        {
            AbstractBookDocumentSourceDto? metadata = await _abstractBookRepository.GetDocumentSourceAsync(
                request.CongressId,
                Array.Empty<Guid>(),
                request.Culture,
                cancellationToken);

            (byte[]? logoBytes, string? logoContentType) = await TryLoadHeaderLogoAsync(
                request.CongressId,
                request.Culture,
                cancellationToken);

            return new ProgramBookPageHeaderDto
            {
                CongressName = FirstNonEmpty(metadata?.CongressName, source.CongressName),
                CongressEnglishName = FirstNonEmpty(
                    metadata?.CongressEnglishName,
                    metadata?.CongressName,
                    source.CongressName),
                StartDate = metadata?.StartDate ?? source.StartDate,
                EndDate = metadata?.EndDate ?? source.EndDate,
                City = metadata?.City?.Trim() ?? string.Empty,
                Venue = metadata?.Venue?.Trim() ?? string.Empty,
                LogoBytes = logoBytes,
                LogoContentType = logoContentType
            };
        }

        private async Task<(byte[]? Bytes, string? ContentType)> TryLoadHeaderLogoAsync(
            Guid congressId,
            string? culture,
            CancellationToken cancellationToken)
        {
            string bucketName = _storageOptions.Buckets.CongressImages?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(bucketName))
                return (null, null);

            string? logoPath = await _abstractBookRepository.GetCongressLogoUrlAsync(
                congressId,
                culture,
                cancellationToken);

            if (!TryResolveObjectName(logoPath, bucketName, out string objectName))
                return (null, null);

            try
            {
                await using Stream source = await _objectStorageService.OpenReadAsync(
                    bucketName,
                    objectName,
                    cancellationToken);
                using MemoryStream buffer = new();
                await source.CopyToAsync(buffer, cancellationToken);

                if (buffer.Length <= 0 || buffer.Length > MaxHeaderLogoSizeInBytes)
                {
                    _logger.LogWarning(
                        "Program book header logo ignored because its size is invalid. CongressId: {CongressId}, ObjectName: {ObjectName}, Size: {Size}",
                        congressId,
                        objectName,
                        buffer.Length);
                    return (null, null);
                }

                byte[] bytes = buffer.ToArray();
                string? contentType = DetectSupportedImageContentType(bytes);
                if (contentType is null)
                {
                    _logger.LogWarning(
                        "Program book header logo ignored because its format is unsupported. CongressId: {CongressId}, ObjectName: {ObjectName}",
                        congressId,
                        objectName);
                    return (null, null);
                }

                return (bytes, contentType);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Congress logo could not be loaded for program book header. CongressId: {CongressId}, ObjectName: {ObjectName}",
                    congressId,
                    objectName);
                return (null, null);
            }
        }

        private static bool TryResolveObjectName(
            string? value,
            string bucketName,
            out string objectName)
        {
            objectName = string.Empty;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            string normalized;
            bool isAbsoluteUrl;
            try
            {
                normalized = value.Trim().Replace('\\', '/');
                isAbsoluteUrl = Uri.TryCreate(normalized, UriKind.Absolute, out Uri? uri);
                normalized = isAbsoluteUrl
                    ? Uri.UnescapeDataString(uri!.AbsolutePath).Trim('/')
                    : Uri.UnescapeDataString(normalized).Trim();
            }
            catch (UriFormatException)
            {
                return false;
            }

            normalized = normalized.TrimStart('~').Trim('/');
            string normalizedBucket = bucketName.Trim().Trim('/');
            string publicPrefix = $"public-assets/{normalizedBucket}/";
            string bucketPrefix = normalizedBucket + "/";

            if (normalized.StartsWith(publicPrefix, StringComparison.OrdinalIgnoreCase))
                normalized = normalized[publicPrefix.Length..];
            else if (normalized.StartsWith(bucketPrefix, StringComparison.OrdinalIgnoreCase))
                normalized = normalized[bucketPrefix.Length..];
            else if (isAbsoluteUrl || normalized.StartsWith("public-assets/", StringComparison.OrdinalIgnoreCase))
                return false;

            normalized = normalized.Trim('/');
            if (string.IsNullOrWhiteSpace(normalized) || normalized.Contains("..", StringComparison.Ordinal))
                return false;

            objectName = normalized;
            return true;
        }

        private static string? DetectSupportedImageContentType(byte[] bytes)
        {
            if (bytes.Length >= 8
                && bytes[0] == 0x89
                && bytes[1] == 0x50
                && bytes[2] == 0x4E
                && bytes[3] == 0x47
                && bytes[4] == 0x0D
                && bytes[5] == 0x0A
                && bytes[6] == 0x1A
                && bytes[7] == 0x0A)
            {
                return "image/png";
            }

            if (bytes.Length >= 3
                && bytes[0] == 0xFF
                && bytes[1] == 0xD8
                && bytes[2] == 0xFF)
            {
                return "image/jpeg";
            }

            return null;
        }

        private static string FirstNonEmpty(params string?[] values)
            => values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))?.Trim() ?? string.Empty;

        private static ProgramSubmissionFilterDto BuildDisplayFilter(CongressProgramPlan plan)
        {
            ProgramSubmissionFilterDto savedFilter = DeserializeSavedFilter(plan.SubmissionFilterJson);
            IReadOnlyCollection<Guid> includedSubmissionIds = DeserializeEligibleSubmissionIds(
                plan.EligibleSubmissionIdsJson);

            return new ProgramSubmissionFilterDto
            {
                Preset = savedFilter.Preset,
                WorkflowStatusCodes = savedFilter.WorkflowStatusCodes,
                PaymentStatusIds = savedFilter.PaymentStatusIds,
                SubmissionTypeIds = savedFilter.SubmissionTypeIds,
                TopicIds = savedFilter.TopicIds,
                IncludedSubmissionIds = includedSubmissionIds,
                SearchText = savedFilter.SearchText
            };
        }

        private static ProgramSubmissionFilterDto DeserializeSavedFilter(string? json)
        {
            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    return JsonSerializer.Deserialize<ProgramSubmissionFilterDto>(json)
                           ?? new ProgramSubmissionFilterDto
                           {
                               Preset = ProgramSubmissionScopePreset.AcceptedOnly
                           };
                }
                catch (JsonException)
                {
                }
            }

            return new ProgramSubmissionFilterDto
            {
                Preset = ProgramSubmissionScopePreset.AcceptedOnly
            };
        }

        private static IReadOnlyCollection<Guid> DeserializeEligibleSubmissionIds(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return Array.Empty<Guid>();

            try
            {
                return (JsonSerializer.Deserialize<Guid[]>(json) ?? Array.Empty<Guid>())
                    .Where(id => id != Guid.Empty)
                    .Distinct()
                    .ToArray();
            }
            catch (JsonException)
            {
                return Array.Empty<Guid>();
            }
        }

        private static string BuildFileName(string congressName)
        {
            string safe = new string((congressName ?? "kongre-programi")
                .Trim()
                .Select(character => char.IsLetterOrDigit(character) || character is '-' or '_'
                    ? character
                    : '-')
                .ToArray());

            while (safe.Contains("--", StringComparison.Ordinal))
                safe = safe.Replace("--", "-", StringComparison.Ordinal);

            safe = safe.Trim('-');
            if (string.IsNullOrWhiteSpace(safe))
                safe = "kongre-programi";
            if (safe.Length > 80)
                safe = safe[..80].TrimEnd('-');

            return $"{safe}-taslak-program.docx";
        }
    }
}
