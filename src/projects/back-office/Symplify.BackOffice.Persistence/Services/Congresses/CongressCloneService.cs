using Core.Application.Storage;
using Core.CrossCuttingConcerns.Exceptions.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;
using Symplify.BackOffice.Application.Features.Congresses.Cloning;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Submission;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Services.Congresses;

public sealed class CongressCloneService : ICongressCloneService
{
    private readonly BackOfficeDbContext _context;
    private readonly IObjectStorageService _objectStorageService;
    private readonly ObjectStorageOptions _storageOptions;

    public CongressCloneService(
        BackOfficeDbContext context,
        IObjectStorageService objectStorageService,
        IOptions<ObjectStorageOptions> storageOptions)
    {
        _context = context;
        _objectStorageService = objectStorageService;
        _storageOptions = storageOptions.Value;
    }

    public async Task<CongressCloneResult> CloneAsync(
        CongressCloneRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        HashSet<CongressCloneModule> modules = request.Modules
            .Distinct()
            .ToHashSet();

        List<StoredObjectReference> copiedObjects = new();
        Dictionary<CongressCloneModule, int> copiedCounts = new();

        await using IDbContextTransaction transaction =
            await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            Congress source = await _context.Congresses
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    congress =>
                        congress.Id == request.SourceCongressId &&
                        congress.DeletedDate == null,
                    cancellationToken)
                ?? throw new BusinessException(
                    "Kopyalanacak kaynak kongre bulunamadı.");

            Congress target = await _context.Congresses
                .FirstOrDefaultAsync(
                    congress =>
                        congress.Id == request.TargetCongressId &&
                        congress.DeletedDate == null,
                    cancellationToken)
                ?? throw new BusinessException(
                    "Yeni oluşturulan hedef kongre bulunamadı.");

            if (source.OrganizationId != target.OrganizationId)
            {
                throw new BusinessException(
                    "Yalnızca aynı organizasyona bağlı kongreler arasında kopyalama yapılabilir.");
            }

            if (source.Id == target.Id)
            {
                throw new BusinessException(
                    "Kaynak ve hedef kongre aynı olamaz.");
            }

            TimeSpan dateOffset = ResolveDateOffset(
                source,
                target,
                request.ShiftRelativeDates);

            if (modules.Contains(CongressCloneModule.GeneralInformation))
            {
                copiedCounts[CongressCloneModule.GeneralInformation] =
                    await CloneGeneralInformationAsync(
                        source,
                        target,
                        cancellationToken);
            }

            if (modules.Contains(CongressCloneModule.Sliders))
            {
                copiedCounts[CongressCloneModule.Sliders] =
                    await CloneSlidersAsync(
                        source,
                        target,
                        copiedObjects,
                        cancellationToken);
            }

            if (modules.Contains(CongressCloneModule.Sections))
            {
                copiedCounts[CongressCloneModule.Sections] =
                    await CloneSectionsAsync(
                        source,
                        target,
                        cancellationToken);
            }

            if (modules.Contains(CongressCloneModule.Announcements))
            {
                copiedCounts[CongressCloneModule.Announcements] =
                    await CloneAnnouncementsAsync(
                        source,
                        target,
                        dateOffset,
                        copiedObjects,
                        cancellationToken);
            }

            if (modules.Contains(CongressCloneModule.Boards))
            {
                copiedCounts[CongressCloneModule.Boards] =
                    await CloneBoardsAsync(
                        source,
                        target,
                        copiedObjects,
                        cancellationToken);
            }

            if (modules.Contains(CongressCloneModule.ImportantDates))
            {
                copiedCounts[CongressCloneModule.ImportantDates] =
                    await CloneImportantDatesAsync(
                        source,
                        target,
                        dateOffset,
                        cancellationToken);
            }

            if (modules.Contains(CongressCloneModule.PaymentPlans))
            {
                copiedCounts[CongressCloneModule.PaymentPlans] =
                    await ClonePaymentPlansAsync(
                        source,
                        target,
                        dateOffset,
                        cancellationToken);
            }

            if (modules.Contains(CongressCloneModule.Documents))
            {
                copiedCounts[CongressCloneModule.Documents] =
                    await CloneDocumentsAsync(
                        source,
                        target,
                        copiedObjects,
                        cancellationToken);
            }

            if (modules.Contains(CongressCloneModule.Workflow))
            {
                copiedCounts[CongressCloneModule.Workflow] =
                    await CloneWorkflowAsync(
                        source,
                        target,
                        cancellationToken);
            }

            if (modules.Contains(CongressCloneModule.Topics))
            {
                copiedCounts[CongressCloneModule.Topics] =
                    await CloneTopicsAsync(
                        source,
                        target,
                        cancellationToken);
            }

            if (modules.Contains(CongressCloneModule.SubmissionTypes))
            {
                copiedCounts[CongressCloneModule.SubmissionTypes] =
                    await CloneSubmissionTypesAsync(
                        source,
                        target,
                        cancellationToken);
            }

            if (modules.Contains(CongressCloneModule.EvaluationCriteria))
            {
                copiedCounts[CongressCloneModule.EvaluationCriteria] =
                    await CloneEvaluationCriteriaAsync(
                        source,
                        target,
                        cancellationToken);
            }

            if (modules.Contains(CongressCloneModule.ParticipationCertificateTemplates))
            {
                copiedCounts[CongressCloneModule.ParticipationCertificateTemplates] =
                    await CloneParticipationCertificateTemplatesAsync(
                        source,
                        target,
                        copiedObjects,
                        cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new CongressCloneResult
            {
                SourceCongressId = source.Id,
                TargetCongressId = target.Id,
                CopiedRecordCounts = copiedCounts
            };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            await DeleteCopiedObjectsSafelyAsync(copiedObjects, cancellationToken);

            try
            {
                await DeleteCreatedCongressAsync(
                    request.TargetCongressId,
                    cancellationToken);
            }
            catch
            {
                // Asıl kopyalama hatasını maskelememek için cleanup hatası yutulur.
            }

            throw;
        }
    }

    public async Task DeleteCreatedCongressAsync(
        Guid congressId,
        CancellationToken cancellationToken = default)
    {
        if (congressId == Guid.Empty)
            return;

        _context.ChangeTracker.Clear();

        Congress? congress = await _context.Congresses
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                entity => entity.Id == congressId,
                cancellationToken);

        if (congress is null)
            return;

        List<CongressTransactionStatusTransition> transitions =
            await _context.CongressTransactionStatusTransitions
                .IgnoreQueryFilters()
                .Where(item => item.CongressId == congressId)
                .ToListAsync(cancellationToken);

        List<CongressWorkflowSetting> workflowSettings =
            await _context.CongressWorkflowSettings
                .IgnoreQueryFilters()
                .Where(item => item.CongressId == congressId)
                .ToListAsync(cancellationToken);

        List<CongressTranslation> translations =
            await _context.CongressTranslations
                .IgnoreQueryFilters()
                .Where(item => item.CongressId == congressId)
                .ToListAsync(cancellationToken);

        _context.CongressTransactionStatusTransitions.RemoveRange(transitions);
        _context.CongressWorkflowSettings.RemoveRange(workflowSettings);
        _context.CongressTranslations.RemoveRange(translations);
        _context.Congresses.Remove(congress);

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<int> CloneGeneralInformationAsync(
        Congress source,
        Congress target,
        CancellationToken cancellationToken)
    {
        target.ContactName = PreferTarget(target.ContactName, source.ContactName);
        target.ContactTitle = PreferTarget(target.ContactTitle, source.ContactTitle);
        target.ContactEmail = PreferTarget(target.ContactEmail, source.ContactEmail);
        target.ContactPhone = PreferTarget(target.ContactPhone, source.ContactPhone);
        target.ContactAddress = PreferTarget(target.ContactAddress, source.ContactAddress);
        target.VenueName = PreferTarget(target.VenueName, source.VenueName);
        target.CountryId ??= source.CountryId;
        target.StateId ??= source.StateId;

        List<CongressContactEmail> sourceContactEmails =
            await _context.CongressContactEmails
                .AsNoTracking()
                .Where(item =>
                    item.CongressId == source.Id &&
                    item.DeletedDate == null)
                .OrderBy(item => item.Order)
                .ToListAsync(cancellationToken);

        bool targetHasContactEmails = await _context.CongressContactEmails
            .AnyAsync(item =>
                item.CongressId == target.Id &&
                item.DeletedDate == null,
                cancellationToken);

        int copiedContactEmailCount = 0;

        if (!targetHasContactEmails)
        {
            if (sourceContactEmails.Count == 0 &&
                !string.IsNullOrWhiteSpace(source.ContactEmail))
            {
                sourceContactEmails.Add(new CongressContactEmail
                {
                    Email = source.ContactEmail,
                    Label = "Genel Bilgi",
                    IsPrimary = true,
                    IsVisibleOnPortal = true,
                    ReceivesContactMessages = true,
                    Order = 0
                });
            }

            foreach (CongressContactEmail sourceContactEmail in sourceContactEmails)
            {
                _context.CongressContactEmails.Add(new CongressContactEmail
                {
                    Id = Guid.NewGuid(),
                    CongressId = target.Id,
                    Email = sourceContactEmail.Email.Trim().ToLowerInvariant(),
                    Label = sourceContactEmail.Label,
                    IsPrimary = sourceContactEmail.IsPrimary,
                    IsVisibleOnPortal = sourceContactEmail.IsVisibleOnPortal,
                    ReceivesContactMessages = sourceContactEmail.ReceivesContactMessages,
                    Order = sourceContactEmail.Order
                });

                copiedContactEmailCount++;
            }
        }

        List<CongressTranslation> sourceTranslations =
            await _context.CongressTranslations
                .AsNoTracking()
                .Where(translation =>
                    translation.CongressId == source.Id &&
                    translation.DeletedDate == null)
                .ToListAsync(cancellationToken);

        List<CongressTranslation> targetTranslations =
            await _context.CongressTranslations
                .Where(translation =>
                    translation.CongressId == target.Id &&
                    translation.DeletedDate == null)
                .ToListAsync(cancellationToken);

        int copiedTranslationCount = 0;

        foreach (CongressTranslation sourceTranslation in sourceTranslations)
        {
            CongressTranslation? targetTranslation = targetTranslations
                .FirstOrDefault(item =>
                    item.LanguageId == sourceTranslation.LanguageId);

            if (targetTranslation is null)
            {
                targetTranslation = new CongressTranslation
                {
                    Id = Guid.NewGuid(),
                    CongressId = target.Id,
                    LanguageId = sourceTranslation.LanguageId,
                    Title = BuildClonedTitle(
                        sourceTranslation.Title,
                        source.EditionNumber,
                        target.EditionNumber),
                    Subtitle = sourceTranslation.Subtitle,
                    ShortDescription = sourceTranslation.ShortDescription,
                    Description = sourceTranslation.Description,
                    WelcomeTitle = sourceTranslation.WelcomeTitle,
                    WelcomeContent = sourceTranslation.WelcomeContent,
                    SeoTitle = sourceTranslation.SeoTitle,
                    SeoDescription = sourceTranslation.SeoDescription,
                    LogoPath = sourceTranslation.LogoPath
                };

                _context.CongressTranslations.Add(targetTranslation);
                copiedTranslationCount++;
                continue;
            }

            // Yeni kongrede kullanıcı tarafından girilen başlık korunur.
            // Diğer içerik alanları yalnızca hedefte boşsa kaynaktan tamamlanır.
            targetTranslation.Subtitle = PreferTarget(
                targetTranslation.Subtitle,
                sourceTranslation.Subtitle);

            targetTranslation.ShortDescription = PreferTarget(
                targetTranslation.ShortDescription,
                sourceTranslation.ShortDescription);

            targetTranslation.Description = PreferTarget(
                targetTranslation.Description,
                sourceTranslation.Description);

            targetTranslation.WelcomeTitle = PreferTarget(
                targetTranslation.WelcomeTitle,
                sourceTranslation.WelcomeTitle);

            targetTranslation.WelcomeContent = PreferTarget(
                targetTranslation.WelcomeContent,
                sourceTranslation.WelcomeContent);

            targetTranslation.SeoTitle = PreferTarget(
                targetTranslation.SeoTitle,
                sourceTranslation.SeoTitle);

            targetTranslation.SeoDescription = PreferTarget(
                targetTranslation.SeoDescription,
                sourceTranslation.SeoDescription);

            copiedTranslationCount++;
        }

        return 1 + copiedTranslationCount + copiedContactEmailCount;
    }

    private async Task<int> CloneSlidersAsync(
        Congress source,
        Congress target,
        ICollection<StoredObjectReference> copiedObjects,
        CancellationToken cancellationToken)
    {
        List<CongressSlider> sourceEntities = await _context.CongressSliders
            .AsNoTracking()
            .Include(slider => slider.Translations)
            .Where(slider =>
                slider.CongressId == source.Id &&
                slider.DeletedDate == null)
            .OrderBy(slider => slider.Order)
            .ThenBy(slider => slider.Id)
            .ToListAsync(cancellationToken);

        string bucketName = GetRequiredImagesBucket();

        foreach (CongressSlider sourceEntity in sourceEntities)
        {
            Guid newId = Guid.NewGuid();
            string imagePath = sourceEntity.ImagePath;

            if (!string.IsNullOrWhiteSpace(imagePath) &&
                !IsExternalAddress(imagePath))
            {
                string fileName = BuildCopiedFileName(
                    "congress-slider",
                    newId,
                    imagePath);

                string targetObjectName = BuildObjectName(
                    "backoffice",
                    "organizations",
                    target.OrganizationId.ToString("D"),
                    "congresses",
                    target.Id.ToString("D"),
                    "sliders",
                    newId.ToString("D"),
                    fileName);

                StoredObjectCopyResult copied = await CopyObjectAsync(
                    bucketName,
                    imagePath,
                    bucketName,
                    targetObjectName,
                    fileName,
                    "congress-clone-slider",
                    target,
                    copiedObjects,
                    cancellationToken);

                imagePath = copied.ObjectName;
            }

            CongressSlider clone = new()
            {
                Id = newId,
                CongressId = target.Id,
                ImagePath = imagePath,
                Order = sourceEntity.Order,
                IsActive = sourceEntity.IsActive,
                Translations = sourceEntity.Translations
                    .Where(translation => translation.DeletedDate == null)
                    .Select(translation => new CongressSliderTranslation
                    {
                        Id = Guid.NewGuid(),
                        CongressSliderId = newId,
                        LanguageId = translation.LanguageId,
                        Title = translation.Title,
                        Subtitle = translation.Subtitle,
                        ButtonText = translation.ButtonText,
                        ButtonUrl = translation.ButtonUrl
                    })
                    .ToList()
            };

            _context.CongressSliders.Add(clone);
        }

        return sourceEntities.Count;
    }

    private async Task<int> CloneSectionsAsync(
        Congress source,
        Congress target,
        CancellationToken cancellationToken)
    {
        List<CongressSection> sourceEntities = await _context.CongressSections
            .AsNoTracking()
            .Include(section => section.Translations)
            .Where(section =>
                section.CongressId == source.Id &&
                section.DeletedDate == null)
            .OrderBy(section => section.Order)
            .ThenBy(section => section.Id)
            .ToListAsync(cancellationToken);

        foreach (CongressSection sourceEntity in sourceEntities)
        {
            Guid newId = Guid.NewGuid();

            CongressSection clone = new()
            {
                Id = newId,
                CongressId = target.Id,
                BindingKey = sourceEntity.BindingKey,
                Order = sourceEntity.Order,
                IsActive = sourceEntity.IsActive,
                Translations = sourceEntity.Translations
                    .Where(translation => translation.DeletedDate == null)
                    .Select(translation => new CongressSectionTranslation
                    {
                        Id = Guid.NewGuid(),
                        CongressSectionId = newId,
                        LanguageId = translation.LanguageId,
                        Title = translation.Title,
                        Content = translation.Content
                    })
                    .ToList()
            };

            _context.CongressSections.Add(clone);
        }

        return sourceEntities.Count;
    }

    private async Task<int> CloneAnnouncementsAsync(
        Congress source,
        Congress target,
        TimeSpan dateOffset,
        ICollection<StoredObjectReference> copiedObjects,
        CancellationToken cancellationToken)
    {
        List<CongressAnnouncement> sourceEntities =
            await _context.CongressAnnouncements
                .AsNoTracking()
                .Include(announcement => announcement.Translations)
                .Where(announcement =>
                    announcement.CongressId == source.Id &&
                    announcement.DeletedDate == null)
                .OrderBy(announcement => announcement.Order)
                .ThenBy(announcement => announcement.Id)
                .ToListAsync(cancellationToken);

        string documentsBucket = GetRequiredDocumentsBucket();

        foreach (CongressAnnouncement sourceEntity in sourceEntities)
        {
            Guid newId = Guid.NewGuid();
            string? attachmentPath = sourceEntity.AttachmentPath;

            if (!string.IsNullOrWhiteSpace(attachmentPath) &&
                !IsExternalAddress(attachmentPath))
            {
                ObjectStorageFileInfo? fileInfo =
                    await _objectStorageService.GetFileInfoAsync(
                        documentsBucket,
                        attachmentPath,
                        cancellationToken);

                if (fileInfo is not null)
                {
                    string fileName = BuildCopiedFileName(
                        "announcement-attachment",
                        newId,
                        attachmentPath);

                    string targetObjectName = BuildObjectName(
                        "backoffice",
                        "organizations",
                        target.OrganizationId.ToString("D"),
                        "congresses",
                        target.Id.ToString("D"),
                        "announcements",
                        newId.ToString("D"),
                        "attachments",
                        fileName);

                    StoredObjectCopyResult copied = await CopyObjectAsync(
                        documentsBucket,
                        attachmentPath,
                        documentsBucket,
                        targetObjectName,
                        fileName,
                        "congress-clone-announcement",
                        target,
                        copiedObjects,
                        cancellationToken);

                    attachmentPath = copied.ObjectName;
                }
            }

            CongressAnnouncement clone = new()
            {
                Id = newId,
                CongressId = target.Id,
                Type = sourceEntity.Type,
                // Önceki kongrede yayımlanmış duyurular yeni kongrede taslak başlar.
                Status = Symplify.BackOffice.Domain.Enums.CongressAnnouncementStatus.Draft,
                PublishStartDate = ShiftDate(sourceEntity.PublishStartDate, dateOffset),
                PublishEndDate = ShiftDate(sourceEntity.PublishEndDate, dateOffset),
                IsPinned = sourceEntity.IsPinned,
                ShowOnHomePage = sourceEntity.ShowOnHomePage,
                ShowInTicker = sourceEntity.ShowInTicker,
                ExternalUrl = sourceEntity.ExternalUrl,
                AttachmentPath = attachmentPath,
                Order = sourceEntity.Order,
                IsActive = sourceEntity.IsActive,
                Translations = sourceEntity.Translations
                    .Where(translation => translation.DeletedDate == null)
                    .Select(translation => new CongressAnnouncementTranslation
                    {
                        Id = Guid.NewGuid(),
                        CongressAnnouncementId = newId,
                        LanguageId = translation.LanguageId,
                        Title = translation.Title,
                        Summary = translation.Summary,
                        Content = translation.Content,
                        SeoTitle = translation.SeoTitle,
                        SeoDescription = translation.SeoDescription
                    })
                    .ToList()
            };

            _context.CongressAnnouncements.Add(clone);
        }

        return sourceEntities.Count;
    }

    private async Task<int> CloneBoardsAsync(
        Congress source,
        Congress target,
        ICollection<StoredObjectReference> copiedObjects,
        CancellationToken cancellationToken)
    {
        List<CongressBoard> sourceBoards = await _context.CongressBoards
            .AsNoTracking()
            .Include(board => board.Translations)
            .Include(board => board.Members)
                .ThenInclude(member => member.Translations)
            .Where(board =>
                board.CongressId == source.Id &&
                board.DeletedDate == null)
            .OrderBy(board => board.Order)
            .ThenBy(board => board.Id)
            .ToListAsync(cancellationToken);

        int copiedMembers = 0;

        foreach (CongressBoard sourceBoard in sourceBoards)
        {
            Guid newBoardId = Guid.NewGuid();

            CongressBoard cloneBoard = new()
            {
                Id = newBoardId,
                CongressId = target.Id,
                Order = sourceBoard.Order,
                IsActive = sourceBoard.IsActive,
                Translations = sourceBoard.Translations
                    .Where(translation => translation.DeletedDate == null)
                    .Select(translation => new CongressBoardTranslation
                    {
                        Id = Guid.NewGuid(),
                        CongressBoardId = newBoardId,
                        LanguageId = translation.LanguageId,
                        Name = translation.Name,
                        Description = translation.Description
                    })
                    .ToList()
            };

            foreach (CongressBoardMember sourceMember in sourceBoard.Members
                         .Where(member => member.DeletedDate == null)
                         .OrderBy(member => member.Order)
                         .ThenBy(member => member.Id))
            {
                Guid newMemberId = Guid.NewGuid();

                StoredObjectCopyResult? image = await CopyOptionalAssetAsync(
                    sourceMember.ImageBucketName,
                    sourceMember.ImageObjectName ?? sourceMember.ImagePath,
                    GetRequiredImagesBucket(),
                    BuildObjectName(
                        "backoffice",
                        "congresses",
                        target.Id.ToString("D"),
                        "board-members",
                        newMemberId.ToString("D"),
                        BuildCopiedFileName(
                            "board-member",
                            newMemberId,
                            sourceMember.ImageFileName
                                ?? sourceMember.ImageObjectName
                                ?? sourceMember.ImagePath)),
                    sourceMember.ImageFileName,
                    "congress-clone-board-member-image",
                    target,
                    copiedObjects,
                    cancellationToken);

                StoredObjectCopyResult? signature = await CopyOptionalAssetAsync(
                    sourceMember.SignatureBucketName,
                    sourceMember.SignatureObjectName ?? sourceMember.SignaturePath,
                    GetRequiredImagesBucket(),
                    BuildObjectName(
                        "backoffice",
                        "congresses",
                        target.Id.ToString("D"),
                        "board-members",
                        newMemberId.ToString("D"),
                        "signature",
                        BuildCopiedFileName(
                            "board-member-signature",
                            newMemberId,
                            sourceMember.SignatureFileName
                                ?? sourceMember.SignatureObjectName
                                ?? sourceMember.SignaturePath)),
                    sourceMember.SignatureFileName,
                    "congress-clone-board-member-signature",
                    target,
                    copiedObjects,
                    cancellationToken);

                CongressBoardMember cloneMember = new()
                {
                    Id = newMemberId,
                    CongressBoardId = newBoardId,
                    FullName = sourceMember.FullName,
                    AcademicTitle = sourceMember.AcademicTitle,
                    Institution = sourceMember.Institution,

                    ImagePath = image?.ObjectName ??
                        (IsExternalAddress(sourceMember.ImagePath ?? string.Empty)
                            ? sourceMember.ImagePath
                            : null),
                    ImageStorageProvider = image is null ? null : _storageOptions.Provider,
                    ImageBucketName = image?.BucketName,
                    ImageObjectName = image?.ObjectName,
                    ImageFileName = image?.FileName,
                    ImageContentType = image?.ContentType,
                    ImageFileSize = image?.Size,
                    ImageETag = image?.ETag,

                    IsAcceptanceLetterSigner = sourceMember.IsAcceptanceLetterSigner,

                    SignaturePath = signature?.ObjectName ??
                        (IsExternalAddress(sourceMember.SignaturePath ?? string.Empty)
                            ? sourceMember.SignaturePath
                            : null),
                    SignatureStorageProvider = signature is null ? null : _storageOptions.Provider,
                    SignatureBucketName = signature?.BucketName,
                    SignatureObjectName = signature?.ObjectName,
                    SignatureFileName = signature?.FileName,
                    SignatureContentType = signature?.ContentType,
                    SignatureFileSize = signature?.Size,
                    SignatureETag = signature?.ETag,

                    Order = sourceMember.Order,
                    IsActive = sourceMember.IsActive,
                    Translations = sourceMember.Translations
                        .Where(translation => translation.DeletedDate == null)
                        .Select(translation => new CongressBoardMemberTranslation
                        {
                            Id = Guid.NewGuid(),
                            CongressBoardMemberId = newMemberId,
                            LanguageId = translation.LanguageId,
                            FullName = translation.FullName,
                            Title = translation.Title,
                            Institution = translation.Institution,
                            Biography = translation.Biography
                        })
                        .ToList()
                };

                cloneBoard.Members.Add(cloneMember);
                copiedMembers++;
            }

            _context.CongressBoards.Add(cloneBoard);
        }

        return sourceBoards.Count + copiedMembers;
    }

    private async Task<int> CloneImportantDatesAsync(
        Congress source,
        Congress target,
        TimeSpan dateOffset,
        CancellationToken cancellationToken)
    {
        List<CongressImportantDate> sourceEntities =
            await _context.CongressImportantDates
                .AsNoTracking()
                .Include(item => item.Translations)
                .Where(item =>
                    item.CongressId == source.Id &&
                    item.DeletedDate == null)
                .OrderBy(item => item.Order)
                .ThenBy(item => item.Id)
                .ToListAsync(cancellationToken);

        foreach (CongressImportantDate sourceEntity in sourceEntities)
        {
            Guid newId = Guid.NewGuid();

            CongressImportantDate clone = new()
            {
                Id = newId,
                CongressId = target.Id,
                StartDate = ShiftDate(sourceEntity.StartDate, dateOffset),
                EndDate = ShiftDate(sourceEntity.EndDate, dateOffset),
                Order = sourceEntity.Order,
                IsActive = sourceEntity.IsActive,
                Translations = sourceEntity.Translations
                    .Where(translation => translation.DeletedDate == null)
                    .Select(translation => new CongressImportantDateTranslation
                    {
                        Id = Guid.NewGuid(),
                        CongressImportantDateId = newId,
                        LanguageId = translation.LanguageId,
                        Title = translation.Title,
                        Description = translation.Description
                    })
                    .ToList()
            };

            _context.CongressImportantDates.Add(clone);
        }

        return sourceEntities.Count;
    }

    private async Task<int> ClonePaymentPlansAsync(
        Congress source,
        Congress target,
        TimeSpan dateOffset,
        CancellationToken cancellationToken)
    {
        List<CongressPaymentPlan> sourceEntities =
            await _context.CongressPaymentPlans
                .AsNoTracking()
                .Include(item => item.Translations)
                .Where(item =>
                    item.CongressId == source.Id &&
                    item.DeletedDate == null)
                .OrderBy(item => item.Order)
                .ThenBy(item => item.Id)
                .ToListAsync(cancellationToken);

        foreach (CongressPaymentPlan sourceEntity in sourceEntities)
        {
            Guid newId = Guid.NewGuid();

            CongressPaymentPlan clone = new()
            {
                Id = newId,
                CongressId = target.Id,
                Code = sourceEntity.Code,
                Amount = sourceEntity.Amount,
                Currency = sourceEntity.Currency,
                AudienceType = sourceEntity.AudienceType,
                PaymentCategory = sourceEntity.PaymentCategory,
                DueDate = ShiftDate(sourceEntity.DueDate, dateOffset),
                ValidFrom = ShiftDate(sourceEntity.ValidFrom, dateOffset),
                ValidUntil = ShiftDate(sourceEntity.ValidUntil, dateOffset),
                Order = sourceEntity.Order,
                IsPublicVisible = sourceEntity.IsPublicVisible,
                IsActive = sourceEntity.IsActive,
                Translations = sourceEntity.Translations
                    .Where(translation => translation.DeletedDate == null)
                    .Select(translation => new CongressPaymentPlanTranslation
                    {
                        Id = Guid.NewGuid(),
                        CongressPaymentPlanId = newId,
                        LanguageId = translation.LanguageId,
                        Name = translation.Name,
                        Description = translation.Description
                    })
                    .ToList()
            };

            _context.CongressPaymentPlans.Add(clone);
        }

        return sourceEntities.Count;
    }

    private async Task<int> CloneDocumentsAsync(
        Congress source,
        Congress target,
        ICollection<StoredObjectReference> copiedObjects,
        CancellationToken cancellationToken)
    {
        List<CongressDocument> sourceEntities =
            await _context.CongressDocuments
                .AsNoTracking()
                .Include(document => document.Translations)
                .Where(document =>
                    document.CongressId == source.Id &&
                    document.DeletedDate == null)
                .OrderBy(document => document.Order)
                .ThenBy(document => document.Id)
                .ToListAsync(cancellationToken);

        foreach (CongressDocument sourceEntity in sourceEntities)
        {
            Guid newId = Guid.NewGuid();
            string sourceBucket = NormalizeBucket(
                sourceEntity.BucketName,
                GetRequiredDocumentsBucket());

            string sourceObjectName =
                sourceEntity.ObjectName ?? sourceEntity.FilePath;

            string fileName = BuildCopiedFileName(
                "congress-document",
                newId,
                sourceEntity.OriginalFileName ?? sourceObjectName);

            string targetObjectName = BuildObjectName(
                "backoffice",
                "organizations",
                target.OrganizationId.ToString("D"),
                "congresses",
                target.Id.ToString("D"),
                "documents",
                newId.ToString("D"),
                fileName);

            StoredObjectCopyResult file = await CopyObjectAsync(
                sourceBucket,
                sourceObjectName,
                sourceBucket,
                targetObjectName,
                fileName,
                "congress-clone-document",
                target,
                copiedObjects,
                cancellationToken);

            StoredObjectCopyResult? cover = await CopyOptionalAssetAsync(
                sourceEntity.CoverImageBucketName,
                sourceEntity.CoverImageObjectName ?? sourceEntity.CoverImagePath,
                GetRequiredImagesBucket(),
                BuildObjectName(
                    "backoffice",
                    "organizations",
                    target.OrganizationId.ToString("D"),
                    "congresses",
                    target.Id.ToString("D"),
                    "documents",
                    newId.ToString("D"),
                    "cover",
                    BuildCopiedFileName(
                        "congress-document-cover",
                        newId,
                        sourceEntity.CoverImageFileName
                            ?? sourceEntity.CoverImageObjectName
                            ?? sourceEntity.CoverImagePath)),
                sourceEntity.CoverImageFileName,
                "congress-clone-document-cover",
                target,
                copiedObjects,
                cancellationToken);

            CongressDocument clone = new()
            {
                Id = newId,
                CongressId = target.Id,
                DocumentTypeId = sourceEntity.DocumentTypeId,
                FilePath = file.ObjectName,
                OriginalFileName = sourceEntity.OriginalFileName ?? file.FileName,
                StorageProvider = _storageOptions.Provider,
                BucketName = file.BucketName,
                ObjectName = file.ObjectName,
                ContentType = file.ContentType,
                FileExtension = Path.GetExtension(file.FileName)?.ToLowerInvariant(),
                FileSize = file.Size,
                ETag = file.ETag,

                CoverImagePath = cover?.ObjectName ??
                    (IsExternalAddress(sourceEntity.CoverImagePath ?? string.Empty)
                        ? sourceEntity.CoverImagePath
                        : null),
                CoverImageStorageProvider = cover is null ? null : _storageOptions.Provider,
                CoverImageBucketName = cover?.BucketName,
                CoverImageObjectName = cover?.ObjectName,
                CoverImageFileName = cover?.FileName,
                CoverImageContentType = cover?.ContentType,
                CoverImageFileSize = cover?.Size,
                CoverImageETag = cover?.ETag,

                Order = sourceEntity.Order,
                IsActive = sourceEntity.IsActive,
                Translations = sourceEntity.Translations
                    .Where(translation => translation.DeletedDate == null)
                    .Select(translation => new CongressDocumentTranslation
                    {
                        Id = Guid.NewGuid(),
                        CongressDocumentId = newId,
                        LanguageId = translation.LanguageId,
                        Description = translation.Description
                    })
                    .ToList()
            };

            _context.CongressDocuments.Add(clone);
        }

        return sourceEntities.Count;
    }

    private async Task<int> CloneWorkflowAsync(
        Congress source,
        Congress target,
        CancellationToken cancellationToken)
    {
        List<CongressWorkflowSetting> existingSettings =
            await _context.CongressWorkflowSettings
                .Where(setting => setting.CongressId == target.Id)
                .ToListAsync(cancellationToken);

        List<CongressTransactionStatusTransition> existingTransitions =
            await _context.CongressTransactionStatusTransitions
                .Where(transition => transition.CongressId == target.Id)
                .ToListAsync(cancellationToken);

        _context.CongressWorkflowSettings.RemoveRange(existingSettings);
        _context.CongressTransactionStatusTransitions.RemoveRange(existingTransitions);

        CongressWorkflowSetting? sourceSetting =
            await _context.CongressWorkflowSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    setting =>
                        setting.CongressId == source.Id &&
                        setting.DeletedDate == null,
                    cancellationToken);

        List<CongressTransactionStatusTransition> sourceTransitions =
            await _context.CongressTransactionStatusTransitions
                .AsNoTracking()
                .Where(transition =>
                    transition.CongressId == source.Id &&
                    transition.DeletedDate == null)
                .OrderBy(transition => transition.Order)
                .ThenBy(transition => transition.Id)
                .ToListAsync(cancellationToken);

        if (sourceSetting is not null)
        {
            _context.CongressWorkflowSettings.Add(
                new CongressWorkflowSetting
                {
                    Id = Guid.NewGuid(),
                    CongressId = target.Id,
                    SourceWorkflowTemplateId =
                        sourceSetting.SourceWorkflowTemplateId,
                    InitialTransactionStatusId =
                        sourceSetting.InitialTransactionStatusId,
                    IsActive = sourceSetting.IsActive
                });
        }

        foreach (CongressTransactionStatusTransition sourceTransition
                 in sourceTransitions)
        {
            _context.CongressTransactionStatusTransitions.Add(
                new CongressTransactionStatusTransition
                {
                    Id = Guid.NewGuid(),
                    CongressId = target.Id,
                    TransactionStatusTransitionId =
                        sourceTransition.TransactionStatusTransitionId,
                    SourceWorkflowTemplateTransitionId =
                        sourceTransition.SourceWorkflowTemplateTransitionId,
                    Order = sourceTransition.Order,
                    IsActive = sourceTransition.IsActive
                });
        }

        return (sourceSetting is null ? 0 : 1) + sourceTransitions.Count;
    }

    private async Task<int> CloneTopicsAsync(
        Congress source,
        Congress target,
        CancellationToken cancellationToken)
    {
        List<CongressTopic> sourceEntities = await _context.CongressTopics
            .AsNoTracking()
            .Where(item =>
                item.CongressId == source.Id &&
                item.DeletedDate == null)
            .OrderBy(item => item.Order)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);

        foreach (CongressTopic sourceEntity in sourceEntities)
        {
            _context.CongressTopics.Add(
                new CongressTopic
                {
                    Id = Guid.NewGuid(),
                    CongressId = target.Id,
                    TopicId = sourceEntity.TopicId,
                    Order = sourceEntity.Order,
                    IsActive = sourceEntity.IsActive
                });
        }

        return sourceEntities.Count;
    }

    private async Task<int> CloneSubmissionTypesAsync(
        Congress source,
        Congress target,
        CancellationToken cancellationToken)
    {
        List<CongressSubmissionType> sourceEntities =
            await _context.CongressSubmissionTypes
                .AsNoTracking()
                .Where(item =>
                    item.CongressId == source.Id &&
                    item.DeletedDate == null)
                .OrderBy(item => item.Order)
                .ThenBy(item => item.Id)
                .ToListAsync(cancellationToken);

        foreach (CongressSubmissionType sourceEntity in sourceEntities)
        {
            _context.CongressSubmissionTypes.Add(
                new CongressSubmissionType
                {
                    Id = Guid.NewGuid(),
                    CongressId = target.Id,
                    SubmissionTypeId = sourceEntity.SubmissionTypeId,
                    Order = sourceEntity.Order,
                    IsActive = sourceEntity.IsActive
                });
        }

        return sourceEntities.Count;
    }

    private async Task<int> CloneEvaluationCriteriaAsync(
        Congress source,
        Congress target,
        CancellationToken cancellationToken)
    {
        List<CongressEvaluationCriterion> sourceEntities =
            await _context.CongressEvaluationCriteria
                .AsNoTracking()
                .Where(item =>
                    item.CongressId == source.Id &&
                    item.DeletedDate == null)
                .OrderBy(item => item.Order)
                .ThenBy(item => item.Id)
                .ToListAsync(cancellationToken);

        foreach (CongressEvaluationCriterion sourceEntity in sourceEntities)
        {
            _context.CongressEvaluationCriteria.Add(
                new CongressEvaluationCriterion
                {
                    Id = Guid.NewGuid(),
                    CongressId = target.Id,
                    EvaluationCriterionId =
                        sourceEntity.EvaluationCriterionId,
                    Order = sourceEntity.Order,
                    IsActive = sourceEntity.IsActive
                });
        }

        return sourceEntities.Count;
    }

    private async Task<int> CloneParticipationCertificateTemplatesAsync(
        Congress source,
        Congress target,
        ICollection<StoredObjectReference> copiedObjects,
        CancellationToken cancellationToken)
    {
        List<ParticipationCertificateTemplate> sourceTemplates =
            await _context.ParticipationCertificateTemplates
                .AsNoTracking()
                .Where(template =>
                    template.CongressId == source.Id &&
                    template.DeletedDate == null &&
                    template.IsActive)
                .OrderByDescending(template => template.IsDefault)
                .ThenBy(template => template.Culture)
                .ToListAsync(cancellationToken);

        foreach (ParticipationCertificateTemplate sourceTemplate
                 in sourceTemplates)
        {
            Guid newId = Guid.NewGuid();
            string sourceBucket = NormalizeBucket(
                sourceTemplate.BucketName,
                GetRequiredSubmissionsBucket());

            string fileName = BuildCopiedFileName(
                "participation-certificate-template",
                newId,
                sourceTemplate.FileName);

            string targetObjectName = BuildObjectName(
                "participation-certificates",
                "templates",
                target.Id.ToString("N"),
                sourceTemplate.Culture,
                fileName);

            StoredObjectCopyResult copied = await CopyObjectAsync(
                sourceBucket,
                sourceTemplate.ObjectName,
                sourceBucket,
                targetObjectName,
                fileName,
                "congress-clone-participation-certificate-template",
                target,
                copiedObjects,
                cancellationToken);

            _context.ParticipationCertificateTemplates.Add(
                new ParticipationCertificateTemplate
                {
                    Id = newId,
                    CongressId = target.Id,
                    Name = sourceTemplate.Name,
                    Culture = sourceTemplate.Culture,
                    IsDefault = sourceTemplate.IsDefault,
                    BodyText = sourceTemplate.BodyText,
                    MailSubject = sourceTemplate.MailSubject,
                    MailTitle = sourceTemplate.MailTitle,
                    MailBodyHtml = sourceTemplate.MailBodyHtml,
                    IsActive = sourceTemplate.IsActive,
                    StorageProvider = _storageOptions.Provider,
                    BucketName = copied.BucketName,
                    ObjectName = copied.ObjectName,
                    FileName = copied.FileName,
                    ContentType = copied.ContentType,
                    FileSize = copied.Size,
                    ETag = copied.ETag,
                    NameBoxX = sourceTemplate.NameBoxX,
                    NameBoxY = sourceTemplate.NameBoxY,
                    NameBoxWidth = sourceTemplate.NameBoxWidth,
                    NameBoxHeight = sourceTemplate.NameBoxHeight,
                    NameFontSize = sourceTemplate.NameFontSize,
                    NameFontColorHex = sourceTemplate.NameFontColorHex,
                    CoverPlaceholderBackground =
                        sourceTemplate.CoverPlaceholderBackground,
                    PlaceholderBackgroundColorHex =
                        sourceTemplate.PlaceholderBackgroundColorHex,
                    RenderCommitteeSignature =
                        sourceTemplate.RenderCommitteeSignature,
                    CommitteeSignatureBoxX =
                        sourceTemplate.CommitteeSignatureBoxX,
                    CommitteeSignatureBoxY =
                        sourceTemplate.CommitteeSignatureBoxY,
                    CommitteeSignatureBoxWidth =
                        sourceTemplate.CommitteeSignatureBoxWidth,
                    CommitteeSignatureBoxHeight =
                        sourceTemplate.CommitteeSignatureBoxHeight,
                    UploadedAt = DateTime.UtcNow
                });
        }

        return sourceTemplates.Count;
    }

    private async Task<StoredObjectCopyResult?> CopyOptionalAssetAsync(
        string? sourceBucketName,
        string? sourceObjectName,
        string fallbackBucketName,
        string targetObjectName,
        string? preferredFileName,
        string module,
        Congress target,
        ICollection<StoredObjectReference> copiedObjects,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sourceObjectName) ||
            IsExternalAddress(sourceObjectName))
        {
            return null;
        }

        string sourceBucket = NormalizeBucket(
            sourceBucketName,
            fallbackBucketName);

        ObjectStorageFileInfo? sourceInfo =
            await _objectStorageService.GetFileInfoAsync(
                sourceBucket,
                sourceObjectName,
                cancellationToken);

        if (sourceInfo is null)
            return null;

        string fileName = string.IsNullOrWhiteSpace(preferredFileName)
            ? Path.GetFileName(targetObjectName)
            : preferredFileName.Trim();

        return await CopyObjectAsync(
            sourceBucket,
            sourceObjectName,
            fallbackBucketName,
            targetObjectName,
            fileName,
            module,
            target,
            copiedObjects,
            cancellationToken);
    }

    private async Task<StoredObjectCopyResult> CopyObjectAsync(
        string sourceBucketName,
        string sourceObjectName,
        string targetBucketName,
        string targetObjectName,
        string targetFileName,
        string module,
        Congress target,
        ICollection<StoredObjectReference> copiedObjects,
        CancellationToken cancellationToken)
    {
        ObjectStorageFileInfo sourceInfo =
            await _objectStorageService.GetFileInfoAsync(
                sourceBucketName,
                sourceObjectName,
                cancellationToken)
            ?? throw new InvalidOperationException(
                $"Kopyalanacak MinIO nesnesi bulunamadı: {sourceObjectName}");

        await using Stream sourceStream =
            await _objectStorageService.OpenReadAsync(
                sourceBucketName,
                sourceObjectName,
                cancellationToken);

        ObjectStorageUploadResult uploadResult =
            await _objectStorageService.UploadAsync(
                new ObjectStorageUploadRequest
                {
                    BucketName = targetBucketName,
                    ObjectName = targetObjectName,
                    OriginalFileName = targetFileName,
                    ContentType = string.IsNullOrWhiteSpace(sourceInfo.ContentType)
                        ? "application/octet-stream"
                        : sourceInfo.ContentType,
                    Size = sourceInfo.Size,
                    Content = sourceStream,
                    Metadata = new Dictionary<string, string>
                    {
                        ["module"] = module,
                        ["organization-id"] =
                            target.OrganizationId.ToString("D"),
                        ["congress-id"] = target.Id.ToString("D"),
                        ["source-object-name"] = sourceObjectName
                    }
                },
                cancellationToken);

        copiedObjects.Add(
            new StoredObjectReference(
                uploadResult.BucketName,
                uploadResult.ObjectName));

        return new StoredObjectCopyResult(
            uploadResult.BucketName,
            uploadResult.ObjectName,
            uploadResult.OriginalFileName,
            uploadResult.ContentType,
            uploadResult.Size,
            uploadResult.ETag);
    }

    private async Task DeleteCopiedObjectsSafelyAsync(
        IEnumerable<StoredObjectReference> copiedObjects,
        CancellationToken cancellationToken)
    {
        foreach (StoredObjectReference item in copiedObjects.Reverse())
        {
            try
            {
                await _objectStorageService.DeleteAsync(
                    new ObjectStorageDeleteRequest
                    {
                        BucketName = item.BucketName,
                        ObjectName = item.ObjectName
                    },
                    cancellationToken);
            }
            catch
            {
                // Asıl kopyalama hatasını maskelememek için cleanup hatası yutulur.
            }
        }
    }

    private string GetRequiredImagesBucket()
    {
        return GetRequiredBucket(
            _storageOptions.Buckets.CongressImages,
            "ObjectStorage:Buckets:CongressImages");
    }

    private string GetRequiredDocumentsBucket()
    {
        return GetRequiredBucket(
            _storageOptions.Buckets.CongressDocuments,
            "ObjectStorage:Buckets:CongressDocuments");
    }

    private string GetRequiredSubmissionsBucket()
    {
        return GetRequiredBucket(
            _storageOptions.Buckets.Submissions,
            "ObjectStorage:Buckets:Submissions");
    }

    private static string GetRequiredBucket(
        string? value,
        string settingName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"{settingName} ayarı zorunludur.");
        }

        return value.Trim();
    }

    private static string NormalizeBucket(
        string? value,
        string fallback)
    {
        return string.IsNullOrWhiteSpace(value)
            ? fallback
            : value.Trim();
    }

    private static TimeSpan ResolveDateOffset(
        Congress source,
        Congress target,
        bool shiftRelativeDates)
    {
        if (!shiftRelativeDates ||
            !source.StartDate.HasValue ||
            !target.StartDate.HasValue)
        {
            return TimeSpan.Zero;
        }

        return target.StartDate.Value.Date -
               source.StartDate.Value.Date;
    }

    private static DateTime ShiftDate(
        DateTime value,
        TimeSpan offset)
    {
        return value.Add(offset);
    }

    private static DateTime? ShiftDate(
        DateTime? value,
        TimeSpan offset)
    {
        return value.HasValue
            ? value.Value.Add(offset)
            : null;
    }

    private static string BuildClonedTitle(
        string sourceTitle,
        int? sourceEditionNumber,
        int? targetEditionNumber)
    {
        if (string.IsNullOrWhiteSpace(sourceTitle))
            return "Yeni Kongre";

        if (!sourceEditionNumber.HasValue ||
            !targetEditionNumber.HasValue ||
            sourceEditionNumber.Value == targetEditionNumber.Value)
        {
            return sourceTitle.Trim();
        }

        string pattern =
            $@"(?<!\d){Regex.Escape(sourceEditionNumber.Value.ToString())}(?!\d)";

        return Regex.Replace(
            sourceTitle.Trim(),
            pattern,
            targetEditionNumber.Value.ToString(),
            RegexOptions.CultureInvariant);
    }

    private static string? PreferTarget(
        string? targetValue,
        string? sourceValue)
    {
        return string.IsNullOrWhiteSpace(targetValue)
            ? Normalize(sourceValue)
            : targetValue;
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static bool IsExternalAddress(string value)
    {
        return Uri.TryCreate(
                   value,
                   UriKind.Absolute,
                   out Uri? uri) &&
               (uri.Scheme == Uri.UriSchemeHttp ||
                uri.Scheme == Uri.UriSchemeHttps);
    }

    private static string BuildCopiedFileName(
        string prefix,
        Guid id,
        string? sourceName)
    {
        string extension = Path.GetExtension(sourceName);

        if (string.IsNullOrWhiteSpace(extension))
            extension = ".bin";

        return $"{prefix}-{id:N}{extension.ToLowerInvariant()}";
    }

    private static string BuildObjectName(
        params string[] segments)
    {
        return string.Join(
            "/",
            segments
                .Where(segment => !string.IsNullOrWhiteSpace(segment))
                .Select(segment =>
                    segment.Trim().Trim('/').Replace('\\', '/')));
    }

    private static void ValidateRequest(CongressCloneRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.SourceCongressId == Guid.Empty)
        {
            throw new BusinessException(
                "Kopyalanacak kaynak kongre seçilmelidir.");
        }

        if (request.TargetCongressId == Guid.Empty)
        {
            throw new BusinessException(
                "Hedef kongre bilgisi geçersiz.");
        }

        if (request.Modules.Count == 0)
        {
            throw new BusinessException(
                "Kopyalanacak en az bir alan seçilmelidir.");
        }

        if (request.Modules.Any(module => !Enum.IsDefined(module)))
        {
            throw new BusinessException(
                "Geçersiz kongre kopyalama alanı seçildi.");
        }
    }

    private sealed record StoredObjectReference(
        string BucketName,
        string ObjectName);

    private sealed record StoredObjectCopyResult(
        string BucketName,
        string ObjectName,
        string FileName,
        string ContentType,
        long Size,
        string? ETag);
}
