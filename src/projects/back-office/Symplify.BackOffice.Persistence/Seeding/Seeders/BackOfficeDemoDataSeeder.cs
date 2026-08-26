using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Enums;
using Symplify.BackOffice.Domain.Lookups;
using Symplify.BackOffice.Domain.Organization;
using Symplify.BackOffice.Domain.Workflow;
using Symplify.BackOffice.Persistence.Contexts;
using Symplify.BackOffice.Persistence.Seeding.Definitions;
using TransactionStatusEntity = Symplify.BackOffice.Domain.Workflow.TransactionStatus;

namespace Symplify.BackOffice.Persistence.Seeding.Seeders;

public sealed class BackOfficeDemoDataSeeder
{
    private readonly BackOfficeDbContext _context;
    private readonly ILogger<BackOfficeDemoDataSeeder> _logger;
    private Guid _turkishLanguageId;
    private Guid _englishLanguageId;

    public BackOfficeDemoDataSeeder(
        BackOfficeDbContext context,
        ILogger<BackOfficeDemoDataSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        DateTime now = DateTime.UtcNow;

        _logger.LogInformation("BackOffice demo data seed started.");

        await ResolveSeedLanguageIdsAsync(cancellationToken);

        await SeedOrganizationsAsync(now, cancellationToken);
        await SeedLookupsAsync(now, cancellationToken);
        await SeedWorkflowAsync(now, cancellationToken);
        await SeedCongressesAsync(now, cancellationToken);
        await SeedCongressBindingsAsync(now, cancellationToken);
        await SeedCongressWorkflowAsync(now, cancellationToken);

        _logger.LogInformation("BackOffice demo data seed completed.");
    }

    public async Task SeedWorkflowOnlyAsync(CancellationToken cancellationToken = default)
    {
        DateTime now = DateTime.UtcNow;

        _logger.LogInformation("BackOffice workflow seed started.");

        await ResolveSeedLanguageIdsAsync(cancellationToken);
        await SeedWorkflowAsync(now, cancellationToken);

        _logger.LogInformation("BackOffice workflow seed completed.");
    }

    private async Task ResolveSeedLanguageIdsAsync(CancellationToken cancellationToken)
    {
        var languages = await _context.Languages
            .IgnoreQueryFilters()
            .Where(language => language.Culture == "tr-TR" || language.Culture == "en-US")
            .Select(language => new { language.Id, language.Culture })
            .ToListAsync(cancellationToken);

        Guid? turkishLanguageId = languages
            .FirstOrDefault(language => language.Culture == "tr-TR")
            ?.Id;

        Guid? englishLanguageId = languages
            .FirstOrDefault(language => language.Culture == "en-US")
            ?.Id;

        if (turkishLanguageId is null || englishLanguageId is null)
            throw new InvalidOperationException("Workflow seed requires tr-TR and en-US languages.");

        _turkishLanguageId = turkishLanguageId.Value;
        _englishLanguageId = englishLanguageId.Value;
    }

    private async Task SeedOrganizationsAsync(DateTime now, CancellationToken cancellationToken)
    {
        foreach (BackOfficeDemoSeedDefinition.OrganizationSeed seed in BackOfficeDemoSeedDefinition.Organizations)
            await UpsertOrganizationAsync(seed, now, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task UpsertOrganizationAsync(
        BackOfficeDemoSeedDefinition.OrganizationSeed seed,
        DateTime now,
        CancellationToken cancellationToken)
    {
        Organization? organization = await _context.Organizations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                entity => entity.Id == seed.Id || entity.Code == seed.Code || entity.Slug == seed.Slug,
                cancellationToken);

        if (organization is null)
        {
            organization = new Organization
            {
                Id = seed.Id,
                CreatedDate = now,
                CreatedBy = BackOfficeDemoSeedDefinition.SystemActor
            };

            await _context.Organizations.AddAsync(organization, cancellationToken);
        }
        else
        {
            organization.UpdatedDate = now;
            organization.UpdatedBy = BackOfficeDemoSeedDefinition.SystemActor;
        }

        organization.Name = seed.Name;
        organization.Code = seed.Code;
        organization.ShortName = seed.ShortName;
        organization.Slug = seed.Slug;
        organization.WebsiteUrl = seed.WebsiteUrl;
        organization.HostUrl = seed.HostUrl;
        organization.Description = seed.Description;
        organization.ContactName = $"{seed.ShortName} Kongre Sekretaryası";
        organization.ContactTitle = "Kongre Sekretaryası";
        organization.ContactEmail = seed.ContactEmail;
        organization.ContactPhone = "+90 000 000 00 00";
        organization.BrandColor = seed.BrandColor;
        organization.IsActive = true;
        organization.DeletedDate = null;
        organization.DeletedBy = null;
    }

    private async Task SeedLookupsAsync(DateTime now, CancellationToken cancellationToken)
    {
        foreach (BackOfficeDemoSeedDefinition.LookupSeed seed in BackOfficeDemoSeedDefinition.Titles)
            await UpsertTitleAsync(seed, now, cancellationToken);

        foreach (BackOfficeDemoSeedDefinition.LookupSeed seed in BackOfficeDemoSeedDefinition.DocumentTypes)
            await UpsertDocumentTypeAsync(seed, now, cancellationToken);

        foreach (BackOfficeDemoSeedDefinition.LookupSeed seed in BackOfficeDemoSeedDefinition.SubmissionTypes)
            await UpsertSubmissionTypeAsync(seed, now, cancellationToken);

        foreach (BackOfficeDemoSeedDefinition.LookupSeed seed in BackOfficeDemoSeedDefinition.Topics)
            await UpsertTopicAsync(seed, now, cancellationToken);

        foreach (BackOfficeDemoSeedDefinition.LookupSeed seed in BackOfficeDemoSeedDefinition.EvaluationCriteria)
            await UpsertEvaluationCriterionAsync(seed, now, cancellationToken);

        foreach (BackOfficeDemoSeedDefinition.LookupSeed seed in BackOfficeDemoSeedDefinition.EventRooms)
            await UpsertEventRoomAsync(seed, now, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedWorkflowAsync(DateTime now, CancellationToken cancellationToken)
    {
        Dictionary<string, int> phaseIdsByCode = new(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, int> statusIdsByCode = new(StringComparer.OrdinalIgnoreCase);
        Dictionary<(string From, string To), int> transitionIdsByCode = new();
        Dictionary<(string From, string To), Guid> workflowTemplateTransitionIdsByCode = new();

        foreach (BackOfficeDemoSeedDefinition.TransactionStatusPhaseSeed seed in BackOfficeDemoSeedDefinition.WorkflowPhases)
        {
            TransactionStatusPhase phase = await UpsertTransactionStatusPhaseAsync(seed, now, cancellationToken);
            phaseIdsByCode[seed.Code] = phase.Id;
        }

        await _context.SaveChangesAsync(cancellationToken);

        foreach (BackOfficeDemoSeedDefinition.TransactionStatusSeed seed in BackOfficeDemoSeedDefinition.WorkflowStatuses)
        {
            TransactionStatusEntity status = await UpsertTransactionStatusAsync(seed, phaseIdsByCode, now, cancellationToken);
            statusIdsByCode[seed.Code] = status.Id;
        }

        await _context.SaveChangesAsync(cancellationToken);

        foreach (BackOfficeDemoSeedDefinition.TransactionStatusTransitionSeed seed in BackOfficeDemoSeedDefinition.WorkflowTransitions)
        {
            TransactionStatusTransition transition = await UpsertTransactionStatusTransitionAsync(seed, statusIdsByCode, now, cancellationToken);
            transitionIdsByCode[(seed.FromStatusCode, seed.ToStatusCode)] = transition.Id;
        }

        await DeactivateObsoleteSeededTransitionsAsync(statusIdsByCode, now, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        WorkflowTemplate template = await UpsertWorkflowTemplateAsync(statusIdsByCode["SUBMITTED"], now, cancellationToken);

        foreach (BackOfficeDemoSeedDefinition.TransactionStatusTransitionSeed seed in BackOfficeDemoSeedDefinition.WorkflowTransitions)
        {
            int transitionId = transitionIdsByCode[(seed.FromStatusCode, seed.ToStatusCode)];
            WorkflowTemplateTransition templateTransition = await UpsertWorkflowTemplateTransitionAsync(template.Id, transitionId, seed.Order, now, cancellationToken);
            workflowTemplateTransitionIdsByCode[(seed.FromStatusCode, seed.ToStatusCode)] = templateTransition.Id;
        }

        foreach (BackOfficeDemoSeedDefinition.WorkflowEffectSeed seed in BackOfficeDemoSeedDefinition.WorkflowEffects)
        {
            if (!transitionIdsByCode.TryGetValue((seed.FromStatusCode, seed.ToStatusCode), out int transitionId))
            {
                _logger.LogWarning(
                    "Workflow effect seed skipped because transition was not found. FromStatusCode: {FromStatusCode}, ToStatusCode: {ToStatusCode}, EffectType: {EffectType}",
                    seed.FromStatusCode,
                    seed.ToStatusCode,
                    seed.EffectType);

                continue;
            }

            await UpsertWorkflowEffectAsync(seed, transitionId, now, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedCongressesAsync(DateTime now, CancellationToken cancellationToken)
    {
        foreach (BackOfficeDemoSeedDefinition.CongressSeed seed in BackOfficeDemoSeedDefinition.Congresses)
            await UpsertCongressAsync(seed, now, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task UpsertCongressAsync(
        BackOfficeDemoSeedDefinition.CongressSeed seed,
        DateTime now,
        CancellationToken cancellationToken)
    {
        Congress? congress = await _context.Congresses
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                entity => entity.Id == seed.Id ||
                          (entity.OrganizationId == seed.OrganizationId && entity.Code == seed.Code),
                cancellationToken);

        if (congress is null)
        {
            congress = new Congress
            {
                Id = seed.Id,
                CreatedDate = now,
                CreatedBy = BackOfficeDemoSeedDefinition.SystemActor
            };

            await _context.Congresses.AddAsync(congress, cancellationToken);
        }
        else
        {
            congress.UpdatedDate = now;
            congress.UpdatedBy = BackOfficeDemoSeedDefinition.SystemActor;
        }

        congress.OrganizationId = seed.OrganizationId;
        congress.Code = seed.Code;
        congress.Name = seed.Name;
        congress.Slug = seed.Slug;
        congress.EditionNumber = seed.EditionNumber;
        congress.StartDate = seed.StartDate;
        congress.EndDate = seed.EndDate;
        congress.Status = CongressStatus.Published;
        congress.ContactName = "Kongre Sekretaryası";
        congress.ContactTitle = "Kongre Sekretaryası";
        congress.ContactEmail = seed.ContactEmail;
        congress.ContactPhone = "+90 000 000 00 00";
        congress.ContactAddress = seed.City + ", Türkiye";
        congress.VenueName = seed.VenueName;
        congress.DeletedDate = null;
        congress.DeletedBy = null;

        await UpsertCongressTranslationAsync(
            congress.Id,
            _turkishLanguageId,
            seed.TurkishTitle,
            seed.TurkishShortDescription,
            seed.TurkishSubtitle,
            seed.TurkishWelcomeTitle,
            seed.TurkishWelcomeContent,
            now,
            cancellationToken);

        await UpsertCongressTranslationAsync(
            congress.Id,
            _englishLanguageId,
            seed.EnglishTitle,
            seed.EnglishShortDescription,
            seed.EnglishSubtitle,
            seed.EnglishWelcomeTitle,
            seed.EnglishWelcomeContent,
            now,
            cancellationToken);
    }

    private async Task SeedCongressBindingsAsync(DateTime now, CancellationToken cancellationToken)
    {
        foreach (BackOfficeDemoSeedDefinition.CongressSeed congress in BackOfficeDemoSeedDefinition.Congresses)
        {
            int order = 1;
            foreach (BackOfficeDemoSeedDefinition.LookupSeed seed in BackOfficeDemoSeedDefinition.Topics)
                await UpsertCongressTopicAsync(congress.Id, seed.Id, order++, now, cancellationToken);

            order = 1;
            foreach (BackOfficeDemoSeedDefinition.LookupSeed seed in BackOfficeDemoSeedDefinition.SubmissionTypes)
                await UpsertCongressSubmissionTypeAsync(congress.Id, seed.Id, order++, now, cancellationToken);

            order = 1;
            foreach (BackOfficeDemoSeedDefinition.LookupSeed seed in BackOfficeDemoSeedDefinition.EvaluationCriteria)
                await UpsertCongressEvaluationCriterionAsync(congress.Id, seed.Id, order++, now, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedCongressWorkflowAsync(DateTime now, CancellationToken cancellationToken)
    {
        WorkflowTemplate? template = await _context.WorkflowTemplates
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(entity => entity.Id == BackOfficeDemoSeedDefinition.DefaultWorkflowTemplateId || entity.Code == "SUBMISSION_DEFAULT", cancellationToken);

        if (template is null)
            throw new InvalidOperationException("Default workflow template seed was not created.");

        List<WorkflowTemplateTransition> templateTransitions = await _context.WorkflowTemplateTransitions
            .IgnoreQueryFilters()
            .Where(entity => entity.WorkflowTemplateId == template.Id)
            .OrderBy(entity => entity.Order)
            .ToListAsync(cancellationToken);

        foreach (BackOfficeDemoSeedDefinition.CongressSeed congress in BackOfficeDemoSeedDefinition.Congresses)
        {
            CongressWorkflowSetting? setting = await _context.CongressWorkflowSettings
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(entity => entity.CongressId == congress.Id, cancellationToken);

            if (setting is null)
            {
                setting = new CongressWorkflowSetting
                {
                    Id = congress.WorkflowSettingId,
                    CongressId = congress.Id,
                    CreatedDate = now,
                    CreatedBy = BackOfficeDemoSeedDefinition.SystemActor
                };

                await _context.CongressWorkflowSettings.AddAsync(setting, cancellationToken);
            }
            else
            {
                setting.UpdatedDate = now;
                setting.UpdatedBy = BackOfficeDemoSeedDefinition.SystemActor;
            }

            setting.SourceWorkflowTemplateId = template.Id;
            setting.InitialTransactionStatusId = template.InitialTransactionStatusId;
            setting.IsActive = true;
            setting.DeletedDate = null;
            setting.DeletedBy = null;

            foreach (WorkflowTemplateTransition templateTransition in templateTransitions)
            {
                CongressTransactionStatusTransition? congressTransition = await _context.CongressTransactionStatusTransitions
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(entity =>
                        entity.CongressId == congress.Id &&
                        entity.TransactionStatusTransitionId == templateTransition.TransactionStatusTransitionId,
                        cancellationToken);

                if (congressTransition is null)
                {
                    congressTransition = new CongressTransactionStatusTransition
                    {
                        Id = Guid.NewGuid(),
                        CongressId = congress.Id,
                        TransactionStatusTransitionId = templateTransition.TransactionStatusTransitionId,
                        CreatedDate = now,
                        CreatedBy = BackOfficeDemoSeedDefinition.SystemActor
                    };

                    await _context.CongressTransactionStatusTransitions.AddAsync(congressTransition, cancellationToken);
                }
                else
                {
                    congressTransition.UpdatedDate = now;
                    congressTransition.UpdatedBy = BackOfficeDemoSeedDefinition.SystemActor;
                }

                congressTransition.SourceWorkflowTemplateTransitionId = templateTransition.Id;
                congressTransition.Order = templateTransition.Order;
                congressTransition.IsActive = templateTransition.IsActive;
                congressTransition.DeletedDate = null;
                congressTransition.DeletedBy = null;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task UpsertTitleAsync(BackOfficeDemoSeedDefinition.LookupSeed seed, DateTime now, CancellationToken cancellationToken)
    {
        Title entity = await UpsertLookupRootAsync(
            _context.Titles.IgnoreQueryFilters(),
            seed.Id,
            seed.Code,
            seed.Order,
            () => new Title { Id = seed.Id },
            entity => _context.Titles.AddAsync(entity, cancellationToken).AsTask(),
            now,
            cancellationToken);

        await UpsertTitleTranslationAsync(entity.Id, _turkishLanguageId, seed.TurkishName, seed.TurkishDescription, now, cancellationToken);
        await UpsertTitleTranslationAsync(entity.Id, _englishLanguageId, seed.EnglishName, seed.EnglishDescription, now, cancellationToken);
    }

    private async Task UpsertDocumentTypeAsync(BackOfficeDemoSeedDefinition.LookupSeed seed, DateTime now, CancellationToken cancellationToken)
    {
        DocumentType entity = await UpsertLookupRootAsync(
            _context.DocumentTypes.IgnoreQueryFilters(),
            seed.Id,
            seed.Code,
            seed.Order,
            () => new DocumentType { Id = seed.Id },
            entity => _context.DocumentTypes.AddAsync(entity, cancellationToken).AsTask(),
            now,
            cancellationToken);

        await UpsertDocumentTypeTranslationAsync(entity.Id, _turkishLanguageId, seed.TurkishName, seed.TurkishDescription, now, cancellationToken);
        await UpsertDocumentTypeTranslationAsync(entity.Id, _englishLanguageId, seed.EnglishName, seed.EnglishDescription, now, cancellationToken);
    }

    private async Task UpsertSubmissionTypeAsync(BackOfficeDemoSeedDefinition.LookupSeed seed, DateTime now, CancellationToken cancellationToken)
    {
        SubmissionType entity = await UpsertLookupRootAsync(
            _context.SubmissionTypes.IgnoreQueryFilters(),
            seed.Id,
            seed.Code,
            seed.Order,
            () => new SubmissionType { Id = seed.Id },
            entity => _context.SubmissionTypes.AddAsync(entity, cancellationToken).AsTask(),
            now,
            cancellationToken);

        entity.FormProfile = seed.FormProfile;

        await UpsertSubmissionTypeTranslationAsync(entity.Id, _turkishLanguageId, seed.TurkishName, seed.TurkishDescription, now, cancellationToken);
        await UpsertSubmissionTypeTranslationAsync(entity.Id, _englishLanguageId, seed.EnglishName, seed.EnglishDescription, now, cancellationToken);
    }

    private async Task UpsertTopicAsync(BackOfficeDemoSeedDefinition.LookupSeed seed, DateTime now, CancellationToken cancellationToken)
    {
        Topic entity = await UpsertLookupRootAsync(
            _context.Categories.IgnoreQueryFilters(),
            seed.Id,
            seed.Code,
            seed.Order,
            () => new Topic { Id = seed.Id },
            entity => _context.Categories.AddAsync(entity, cancellationToken).AsTask(),
            now,
            cancellationToken);

        await UpsertTopicTranslationAsync(entity.Id, _turkishLanguageId, seed.TurkishName, seed.TurkishDescription, now, cancellationToken);
        await UpsertTopicTranslationAsync(entity.Id, _englishLanguageId, seed.EnglishName, seed.EnglishDescription, now, cancellationToken);
    }

    private async Task UpsertEvaluationCriterionAsync(BackOfficeDemoSeedDefinition.LookupSeed seed, DateTime now, CancellationToken cancellationToken)
    {
        EvaluationCriterion entity = await UpsertLookupRootAsync(
            _context.EvaluationCriteria.IgnoreQueryFilters(),
            seed.Id,
            seed.Code,
            seed.Order,
            () => new EvaluationCriterion { Id = seed.Id },
            entity => _context.EvaluationCriteria.AddAsync(entity, cancellationToken).AsTask(),
            now,
            cancellationToken);

        if (entity.Score <= 0)
        {
            entity.Score = 10;
            await _context.SaveChangesAsync(cancellationToken);
        }

        await UpsertEvaluationCriterionTranslationAsync(entity.Id, _turkishLanguageId, seed.TurkishName, seed.TurkishDescription, now, cancellationToken);
        await UpsertEvaluationCriterionTranslationAsync(entity.Id, _englishLanguageId, seed.EnglishName, seed.EnglishDescription, now, cancellationToken);
    }

    private async Task UpsertEventRoomAsync(BackOfficeDemoSeedDefinition.LookupSeed seed, DateTime now, CancellationToken cancellationToken)
    {
        EventRoom entity = await UpsertLookupRootAsync(
            _context.EventRooms.IgnoreQueryFilters(),
            seed.Id,
            seed.Code,
            seed.Order,
            () => new EventRoom { Id = seed.Id },
            entity => _context.EventRooms.AddAsync(entity, cancellationToken).AsTask(),
            now,
            cancellationToken);

        await UpsertEventRoomTranslationAsync(entity.Id, _turkishLanguageId, seed.TurkishName, seed.TurkishDescription, now, cancellationToken);
        await UpsertEventRoomTranslationAsync(entity.Id, _englishLanguageId, seed.EnglishName, seed.EnglishDescription, now, cancellationToken);
    }

    private async Task<T> UpsertLookupRootAsync<T>(
        IQueryable<T> query,
        Guid id,
        string code,
        int order,
        Func<T> factory,
        Func<T, Task> addAsync,
        DateTime now,
        CancellationToken cancellationToken)
        where T : Core.Persistence.Repositories.Entity<Guid>, Core.Persistence.Repositories.IEntityTimestamps, Core.Persistence.Repositories.IAuditable
    {
        T? entity = await query.FirstOrDefaultAsync(item => item.Id == id || EF.Property<string?>(item, "Code") == code, cancellationToken);

        if (entity is null)
        {
            entity = factory();
            entity.CreatedDate = now;
            entity.CreatedBy = BackOfficeDemoSeedDefinition.SystemActor;
            await addAsync(entity);
        }
        else
        {
            entity.UpdatedDate = now;
            entity.UpdatedBy = BackOfficeDemoSeedDefinition.SystemActor;
        }

        typeof(T).GetProperty("Code")?.SetValue(entity, code);
        typeof(T).GetProperty("Order")?.SetValue(entity, order);
        typeof(T).GetProperty("IsActive")?.SetValue(entity, true);
        entity.DeletedDate = null;
        entity.DeletedBy = null;

        return entity;
    }

    private static bool IsSimplifiedWorkflowPhaseActive(string code)
    {
        return string.Equals(code, "SUBMISSION", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(code, "REVIEW", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(code, "POST_ACCEPTANCE", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(code, "FINAL", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSimplifiedWorkflowStatusActive(string code)
    {
        return string.Equals(code, "SUBMITTED", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(code, "REVIEWER_ASSIGNMENT", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(code, "UNDER_REVIEW", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(code, "ACCEPTED", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(code, "COMPLETED", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(code, "REJECTED", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<TransactionStatusPhase> UpsertTransactionStatusPhaseAsync(
        BackOfficeDemoSeedDefinition.TransactionStatusPhaseSeed seed,
        DateTime now,
        CancellationToken cancellationToken)
    {
        TransactionStatusPhase? entity = await _context.TransactionStatusPhases
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.Id == seed.Id || item.Code == seed.Code, cancellationToken);

        if (entity is null)
        {
            entity = new TransactionStatusPhase
            {
                Id = seed.Id,
                CreatedDate = now,
                CreatedBy = BackOfficeDemoSeedDefinition.SystemActor
            };
            await _context.TransactionStatusPhases.AddAsync(entity, cancellationToken);
        }
        else
        {
            entity.UpdatedDate = now;
            entity.UpdatedBy = BackOfficeDemoSeedDefinition.SystemActor;
        }

        entity.Code = seed.Code;
        entity.Order = seed.Order;
        entity.IsActive = IsSimplifiedWorkflowPhaseActive(seed.Code);
        entity.DeletedDate = null;
        entity.DeletedBy = null;

        await UpsertTransactionStatusPhaseTranslationAsync(entity.Id, _turkishLanguageId, seed.TurkishName, seed.TurkishDescription, now, cancellationToken);
        await UpsertTransactionStatusPhaseTranslationAsync(entity.Id, _englishLanguageId, seed.EnglishName, seed.EnglishDescription, now, cancellationToken);

        return entity;
    }

    private async Task<TransactionStatusEntity> UpsertTransactionStatusAsync(
        BackOfficeDemoSeedDefinition.TransactionStatusSeed seed,
        IReadOnlyDictionary<string, int> phaseIdsByCode,
        DateTime now,
        CancellationToken cancellationToken)
    {
        int phaseId = phaseIdsByCode.Values.Contains(seed.PhaseId)
            ? seed.PhaseId
            : seed.PhaseId;

        TransactionStatusEntity? entity = await _context.TransactionStatuses
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.Id == seed.Id || item.Code == seed.Code, cancellationToken);

        if (entity is null)
        {
            entity = new TransactionStatusEntity
            {
                Id = seed.Id,
                CreatedDate = now,
                CreatedBy = BackOfficeDemoSeedDefinition.SystemActor
            };
            await _context.TransactionStatuses.AddAsync(entity, cancellationToken);
        }
        else
        {
            entity.UpdatedDate = now;
            entity.UpdatedBy = BackOfficeDemoSeedDefinition.SystemActor;
        }

        entity.TransactionStatusPhaseId = phaseId;
        entity.Code = seed.Code;
        entity.Order = seed.Order;
        entity.IsEditable = seed.IsEditable;
        entity.IsFinal = seed.IsFinal;
        entity.IsActive = IsSimplifiedWorkflowStatusActive(seed.Code);
        entity.DeletedDate = null;
        entity.DeletedBy = null;

        await UpsertTransactionStatusTranslationAsync(entity.Id, _turkishLanguageId, seed.TurkishName, seed.TurkishDescription, now, cancellationToken);
        await UpsertTransactionStatusTranslationAsync(entity.Id, _englishLanguageId, seed.EnglishName, seed.EnglishDescription, now, cancellationToken);

        return entity;
    }

    private async Task<TransactionStatusTransition> UpsertTransactionStatusTransitionAsync(
        BackOfficeDemoSeedDefinition.TransactionStatusTransitionSeed seed,
        IReadOnlyDictionary<string, int> statusIdsByCode,
        DateTime now,
        CancellationToken cancellationToken)
    {
        int fromStatusId = statusIdsByCode[seed.FromStatusCode];
        int toStatusId = statusIdsByCode[seed.ToStatusCode];

        TransactionStatusTransition? entity = await _context.TransactionStatusTransitions
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.Id == seed.Id ||
                                         (item.FromStatusId == fromStatusId && item.ToStatusId == toStatusId), cancellationToken);

        if (entity is null)
        {
            entity = new TransactionStatusTransition
            {
                Id = seed.Id,
                CreatedDate = now,
                CreatedBy = BackOfficeDemoSeedDefinition.SystemActor
            };
            await _context.TransactionStatusTransitions.AddAsync(entity, cancellationToken);
        }
        else
        {
            entity.UpdatedDate = now;
            entity.UpdatedBy = BackOfficeDemoSeedDefinition.SystemActor;
        }

        entity.FromStatusId = fromStatusId;
        entity.ToStatusId = toStatusId;
        entity.IsAuto = seed.IsAuto;
        entity.IsActive = true;
        entity.DeletedDate = null;
        entity.DeletedBy = null;

        await UpsertTransactionStatusTransitionTranslationAsync(entity.Id, _turkishLanguageId, seed.TurkishName, seed.TurkishDescription, now, cancellationToken);
        await UpsertTransactionStatusTransitionTranslationAsync(entity.Id, _englishLanguageId, seed.EnglishName, seed.EnglishDescription, now, cancellationToken);

        return entity;
    }

    private async Task DeactivateObsoleteSeededTransitionsAsync(
        IReadOnlyDictionary<string, int> statusIdsByCode,
        DateTime now,
        CancellationToken cancellationToken)
    {
        (string From, string To)[] obsoleteTransitions =
        {
            ("DRAFT", "SUBMITTED"),
            ("DRAFT", "WITHDRAWN"),
            ("SUBMITTED", "PRE_CHECK"),
            ("SUBMITTED", "REVISION_REQUESTED"),
            ("SUBMITTED", "WITHDRAWN"),
            ("PRE_CHECK", "REVIEWER_ASSIGNMENT"),
            ("UNDER_REVIEW", "REVIEWS_COMPLETED"),
            ("UNDER_REVIEW", "EDITORIAL_DECISION"),
            ("REVIEWS_COMPLETED", "EDITORIAL_DECISION"),
            ("EDITORIAL_DECISION", "ACCEPTED"),
            ("EDITORIAL_DECISION", "REJECTED"),
            ("EDITORIAL_DECISION", "REVISION_REQUESTED"),
            ("REVISION_REQUESTED", "SUBMITTED"),
            ("ACCEPTED", "PAYMENT_PENDING"),
            ("PAYMENT_PENDING", "COMPLETED")
        };

        foreach ((string fromCode, string toCode) in obsoleteTransitions)
        {
            if (!statusIdsByCode.TryGetValue(fromCode, out int fromStatusId) ||
                !statusIdsByCode.TryGetValue(toCode, out int toStatusId))
                continue;

            TransactionStatusTransition? transition = await _context.TransactionStatusTransitions
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(entity =>
                    entity.FromStatusId == fromStatusId &&
                    entity.ToStatusId == toStatusId &&
                    entity.DeletedDate == null,
                    cancellationToken);

            if (transition is null)
                continue;

            transition.IsActive = false;
            transition.IsAuto = false;
            transition.UpdatedDate = now;
            transition.UpdatedBy = BackOfficeDemoSeedDefinition.SystemActor;

            List<WorkflowTemplateTransition> templateTransitions = await _context.WorkflowTemplateTransitions
                .IgnoreQueryFilters()
                .Where(entity =>
                    entity.TransactionStatusTransitionId == transition.Id &&
                    entity.DeletedDate == null)
                .ToListAsync(cancellationToken);

            foreach (WorkflowTemplateTransition templateTransition in templateTransitions)
            {
                templateTransition.IsActive = false;
                templateTransition.UpdatedDate = now;
                templateTransition.UpdatedBy = BackOfficeDemoSeedDefinition.SystemActor;
            }

            List<CongressTransactionStatusTransition> congressTransitions = await _context.CongressTransactionStatusTransitions
                .IgnoreQueryFilters()
                .Where(entity =>
                    entity.TransactionStatusTransitionId == transition.Id &&
                    entity.DeletedDate == null)
                .ToListAsync(cancellationToken);

            foreach (CongressTransactionStatusTransition congressTransition in congressTransitions)
            {
                congressTransition.IsActive = false;
                congressTransition.UpdatedDate = now;
                congressTransition.UpdatedBy = BackOfficeDemoSeedDefinition.SystemActor;
            }
        }
    }

    private async Task<WorkflowTemplate> UpsertWorkflowTemplateAsync(int initialStatusId, DateTime now, CancellationToken cancellationToken)
    {
        List<WorkflowTemplate> otherDefaultTemplates = await _context.WorkflowTemplates
            .IgnoreQueryFilters()
            .Where(template => template.IsDefault && template.Id != BackOfficeDemoSeedDefinition.DefaultWorkflowTemplateId)
            .ToListAsync(cancellationToken);

        foreach (WorkflowTemplate otherDefaultTemplate in otherDefaultTemplates)
        {
            otherDefaultTemplate.IsDefault = false;
            otherDefaultTemplate.UpdatedDate = now;
            otherDefaultTemplate.UpdatedBy = BackOfficeDemoSeedDefinition.SystemActor;
        }

        if (otherDefaultTemplates.Count > 0)
            await _context.SaveChangesAsync(cancellationToken);

        WorkflowTemplate? entity = await _context.WorkflowTemplates
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.Id == BackOfficeDemoSeedDefinition.DefaultWorkflowTemplateId || item.Code == "SUBMISSION_DEFAULT", cancellationToken);

        if (entity is null)
        {
            entity = new WorkflowTemplate
            {
                Id = BackOfficeDemoSeedDefinition.DefaultWorkflowTemplateId,
                CreatedDate = now,
                CreatedBy = BackOfficeDemoSeedDefinition.SystemActor
            };
            await _context.WorkflowTemplates.AddAsync(entity, cancellationToken);
        }
        else
        {
            entity.UpdatedDate = now;
            entity.UpdatedBy = BackOfficeDemoSeedDefinition.SystemActor;
        }

        entity.Code = "SUBMISSION_DEFAULT";
        entity.InitialTransactionStatusId = initialStatusId;
        entity.IsDefault = true;
        entity.IsActive = true;
        entity.DeletedDate = null;
        entity.DeletedBy = null;

        await UpsertWorkflowTemplateTranslationAsync(entity.Id, _turkishLanguageId, "Sade Bildiri İş Akışı", "Yazarın doğrudan gönderdiği, editörün kabul veya ret kararı verdiği sade süreç.", now, cancellationToken);
        await UpsertWorkflowTemplateTranslationAsync(entity.Id, _englishLanguageId, "Simplified Submission Workflow", "Simplified process where the author submits directly and the editor accepts or rejects.", now, cancellationToken);

        return entity;
    }

    private async Task<WorkflowTemplateTransition> UpsertWorkflowTemplateTransitionAsync(
        Guid workflowTemplateId,
        int transactionStatusTransitionId,
        int order,
        DateTime now,
        CancellationToken cancellationToken)
    {
        WorkflowTemplateTransition? entity = await _context.WorkflowTemplateTransitions
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.WorkflowTemplateId == workflowTemplateId &&
                                         item.TransactionStatusTransitionId == transactionStatusTransitionId, cancellationToken);

        if (entity is null)
        {
            entity = new WorkflowTemplateTransition
            {
                Id = Guid.NewGuid(),
                WorkflowTemplateId = workflowTemplateId,
                TransactionStatusTransitionId = transactionStatusTransitionId,
                CreatedDate = now,
                CreatedBy = BackOfficeDemoSeedDefinition.SystemActor
            };
            await _context.WorkflowTemplateTransitions.AddAsync(entity, cancellationToken);
        }
        else
        {
            entity.UpdatedDate = now;
            entity.UpdatedBy = BackOfficeDemoSeedDefinition.SystemActor;
        }

        entity.Order = order;
        entity.IsActive = true;
        entity.DeletedDate = null;
        entity.DeletedBy = null;
        return entity;
    }

    private async Task UpsertWorkflowEffectAsync(
        BackOfficeDemoSeedDefinition.WorkflowEffectSeed seed,
        int transactionStatusTransitionId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        WorkflowTransitionEffect? entity = await _context.WorkflowTransitionEffects
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.Id == seed.Id ||
                                         (item.TransactionStatusTransitionId == transactionStatusTransitionId &&
                                          item.EffectType == seed.EffectType &&
                                          item.Order == seed.Order), cancellationToken);

        if (entity is null)
        {
            entity = new WorkflowTransitionEffect
            {
                Id = seed.Id,
                TransactionStatusTransitionId = transactionStatusTransitionId,
                CreatedDate = now,
                CreatedBy = BackOfficeDemoSeedDefinition.SystemActor
            };
            await _context.WorkflowTransitionEffects.AddAsync(entity, cancellationToken);
        }
        else
        {
            entity.UpdatedDate = now;
            entity.UpdatedBy = BackOfficeDemoSeedDefinition.SystemActor;
        }

        entity.TransactionStatusTransitionId = transactionStatusTransitionId;
        entity.EffectType = seed.EffectType;
        entity.ParametersJson = seed.ParametersJson;
        entity.Order = seed.Order;
        entity.IsActive = true;
        entity.DeletedDate = null;
        entity.DeletedBy = null;
    }

    private async Task UpsertCongressTopicAsync(Guid congressId, Guid topicId, int order, DateTime now, CancellationToken cancellationToken)
    {
        CongressTopic? entity = await _context.CongressTopics
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.CongressId == congressId && item.TopicId == topicId, cancellationToken);

        if (entity is null)
        {
            entity = new CongressTopic
            {
                Id = Guid.NewGuid(),
                CongressId = congressId,
                TopicId = topicId,
                CreatedDate = now,
                CreatedBy = BackOfficeDemoSeedDefinition.SystemActor
            };
            await _context.CongressTopics.AddAsync(entity, cancellationToken);
        }
        else
        {
            entity.UpdatedDate = now;
            entity.UpdatedBy = BackOfficeDemoSeedDefinition.SystemActor;
        }

        entity.Order = order;
        entity.IsActive = true;
        entity.DeletedDate = null;
        entity.DeletedBy = null;
    }

    private async Task UpsertCongressSubmissionTypeAsync(Guid congressId, Guid submissionTypeId, int order, DateTime now, CancellationToken cancellationToken)
    {
        CongressSubmissionType? entity = await _context.CongressSubmissionTypes
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.CongressId == congressId && item.SubmissionTypeId == submissionTypeId, cancellationToken);

        if (entity is null)
        {
            entity = new CongressSubmissionType
            {
                Id = Guid.NewGuid(),
                CongressId = congressId,
                SubmissionTypeId = submissionTypeId,
                CreatedDate = now,
                CreatedBy = BackOfficeDemoSeedDefinition.SystemActor
            };
            await _context.CongressSubmissionTypes.AddAsync(entity, cancellationToken);
        }
        else
        {
            entity.UpdatedDate = now;
            entity.UpdatedBy = BackOfficeDemoSeedDefinition.SystemActor;
        }

        entity.Order = order;
        entity.IsActive = true;
        entity.DeletedDate = null;
        entity.DeletedBy = null;
    }

    private async Task UpsertCongressEvaluationCriterionAsync(Guid congressId, Guid evaluationCriterionId, int order, DateTime now, CancellationToken cancellationToken)
    {
        CongressEvaluationCriterion? entity = await _context.CongressEvaluationCriteria
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.CongressId == congressId && item.EvaluationCriterionId == evaluationCriterionId, cancellationToken);

        if (entity is null)
        {
            entity = new CongressEvaluationCriterion
            {
                Id = Guid.NewGuid(),
                CongressId = congressId,
                EvaluationCriterionId = evaluationCriterionId,
                CreatedDate = now,
                CreatedBy = BackOfficeDemoSeedDefinition.SystemActor
            };
            await _context.CongressEvaluationCriteria.AddAsync(entity, cancellationToken);
        }
        else
        {
            entity.UpdatedDate = now;
            entity.UpdatedBy = BackOfficeDemoSeedDefinition.SystemActor;
        }

        entity.Order = order;
        entity.IsActive = true;
        entity.DeletedDate = null;
        entity.DeletedBy = null;
    }

    private async Task UpsertTitleTranslationAsync(Guid titleId, Guid languageId, string name, string? description, DateTime now, CancellationToken cancellationToken)
    {
        TitleTranslation? entity = await _context.TitleTranslations.IgnoreQueryFilters().FirstOrDefaultAsync(item => item.TitleId == titleId && item.LanguageId == languageId, cancellationToken);
        if (entity is null)
        {
            entity = new TitleTranslation { Id = Guid.NewGuid(), TitleId = titleId, LanguageId = languageId, CreatedDate = now, CreatedBy = BackOfficeDemoSeedDefinition.SystemActor };
            await _context.TitleTranslations.AddAsync(entity, cancellationToken);
        }
        else { entity.UpdatedDate = now; entity.UpdatedBy = BackOfficeDemoSeedDefinition.SystemActor; }
        entity.Name = name; entity.Description = description; entity.DeletedDate = null; entity.DeletedBy = null;
    }

    private async Task UpsertDocumentTypeTranslationAsync(Guid documentTypeId, Guid languageId, string name, string? description, DateTime now, CancellationToken cancellationToken)
    {
        DocumentTypeTranslation? entity = await _context.DocumentTypeTranslations.IgnoreQueryFilters().FirstOrDefaultAsync(item => item.DocumentTypeId == documentTypeId && item.LanguageId == languageId, cancellationToken);
        if (entity is null) { entity = new DocumentTypeTranslation { Id = Guid.NewGuid(), DocumentTypeId = documentTypeId, LanguageId = languageId, CreatedDate = now, CreatedBy = BackOfficeDemoSeedDefinition.SystemActor }; await _context.DocumentTypeTranslations.AddAsync(entity, cancellationToken); }
        else { entity.UpdatedDate = now; entity.UpdatedBy = BackOfficeDemoSeedDefinition.SystemActor; }
        entity.Name = name; entity.Description = description; entity.DeletedDate = null; entity.DeletedBy = null;
    }

    private async Task UpsertSubmissionTypeTranslationAsync(Guid submissionTypeId, Guid languageId, string name, string? description, DateTime now, CancellationToken cancellationToken)
    {
        SubmissionTypeTranslation? entity = await _context.SubmissionTypeTranslations.IgnoreQueryFilters().FirstOrDefaultAsync(item => item.SubmissionTypeId == submissionTypeId && item.LanguageId == languageId, cancellationToken);
        if (entity is null) { entity = new SubmissionTypeTranslation { Id = Guid.NewGuid(), SubmissionTypeId = submissionTypeId, LanguageId = languageId, CreatedDate = now, CreatedBy = BackOfficeDemoSeedDefinition.SystemActor }; await _context.SubmissionTypeTranslations.AddAsync(entity, cancellationToken); }
        else { entity.UpdatedDate = now; entity.UpdatedBy = BackOfficeDemoSeedDefinition.SystemActor; }
        entity.Name = name; entity.Description = description; entity.DeletedDate = null; entity.DeletedBy = null;
    }

    private async Task UpsertTopicTranslationAsync(Guid topicId, Guid languageId, string name, string? description, DateTime now, CancellationToken cancellationToken)
    {
        TopicTranslation? entity = await _context.CategoryTranslations.IgnoreQueryFilters().FirstOrDefaultAsync(item => item.TopicId == topicId && item.LanguageId == languageId, cancellationToken);
        if (entity is null) { entity = new TopicTranslation { Id = Guid.NewGuid(), TopicId = topicId, LanguageId = languageId, CreatedDate = now, CreatedBy = BackOfficeDemoSeedDefinition.SystemActor }; await _context.CategoryTranslations.AddAsync(entity, cancellationToken); }
        else { entity.UpdatedDate = now; entity.UpdatedBy = BackOfficeDemoSeedDefinition.SystemActor; }
        entity.Name = name; entity.Description = description; entity.DeletedDate = null; entity.DeletedBy = null;
    }

    private async Task UpsertEvaluationCriterionTranslationAsync(Guid evaluationCriterionId, Guid languageId, string name, string? description, DateTime now, CancellationToken cancellationToken)
    {
        EvaluationCriterionTranslation? entity = await _context.EvaluationCriterionTranslations.IgnoreQueryFilters().FirstOrDefaultAsync(item => item.EvaluationCriterionId == evaluationCriterionId && item.LanguageId == languageId, cancellationToken);
        if (entity is null) { entity = new EvaluationCriterionTranslation { Id = Guid.NewGuid(), EvaluationCriterionId = evaluationCriterionId, LanguageId = languageId, CreatedDate = now, CreatedBy = BackOfficeDemoSeedDefinition.SystemActor }; await _context.EvaluationCriterionTranslations.AddAsync(entity, cancellationToken); }
        else { entity.UpdatedDate = now; entity.UpdatedBy = BackOfficeDemoSeedDefinition.SystemActor; }
        entity.Name = name; entity.Description = description; entity.DeletedDate = null; entity.DeletedBy = null;
    }

    private async Task UpsertEventRoomTranslationAsync(Guid eventRoomId, Guid languageId, string name, string? description, DateTime now, CancellationToken cancellationToken)
    {
        EventRoomTranslation? entity = await _context.EventRoomTranslations.IgnoreQueryFilters().FirstOrDefaultAsync(item => item.EventRoomId == eventRoomId && item.LanguageId == languageId, cancellationToken);
        if (entity is null) { entity = new EventRoomTranslation { Id = Guid.NewGuid(), EventRoomId = eventRoomId, LanguageId = languageId, CreatedDate = now, CreatedBy = BackOfficeDemoSeedDefinition.SystemActor }; await _context.EventRoomTranslations.AddAsync(entity, cancellationToken); }
        else { entity.UpdatedDate = now; entity.UpdatedBy = BackOfficeDemoSeedDefinition.SystemActor; }
        entity.Name = name; entity.Description = description; entity.DeletedDate = null; entity.DeletedBy = null;
    }

    private async Task UpsertTransactionStatusPhaseTranslationAsync(int phaseId, Guid languageId, string name, string? description, DateTime now, CancellationToken cancellationToken)
    {
        TransactionStatusPhaseTranslation? entity = await _context.TransactionStatusPhaseTranslations.IgnoreQueryFilters().FirstOrDefaultAsync(item => item.TransactionStatusPhaseId == phaseId && item.LanguageId == languageId, cancellationToken);
        if (entity is null) { entity = new TransactionStatusPhaseTranslation { Id = Guid.NewGuid(), TransactionStatusPhaseId = phaseId, LanguageId = languageId, CreatedDate = now, CreatedBy = BackOfficeDemoSeedDefinition.SystemActor }; await _context.TransactionStatusPhaseTranslations.AddAsync(entity, cancellationToken); }
        else { entity.UpdatedDate = now; entity.UpdatedBy = BackOfficeDemoSeedDefinition.SystemActor; }
        entity.Name = name; entity.Description = description; entity.DeletedDate = null; entity.DeletedBy = null;
    }

    private async Task UpsertTransactionStatusTranslationAsync(int statusId, Guid languageId, string name, string? description, DateTime now, CancellationToken cancellationToken)
    {
        TransactionStatusTranslation? entity = await _context.TransactionStatusTranslations.IgnoreQueryFilters().FirstOrDefaultAsync(item => item.TransactionStatusId == statusId && item.LanguageId == languageId, cancellationToken);
        if (entity is null) { entity = new TransactionStatusTranslation { Id = Guid.NewGuid(), TransactionStatusId = statusId, LanguageId = languageId, CreatedDate = now, CreatedBy = BackOfficeDemoSeedDefinition.SystemActor }; await _context.TransactionStatusTranslations.AddAsync(entity, cancellationToken); }
        else { entity.UpdatedDate = now; entity.UpdatedBy = BackOfficeDemoSeedDefinition.SystemActor; }
        entity.Name = name; entity.Description = description; entity.DeletedDate = null; entity.DeletedBy = null;
    }

    private async Task UpsertTransactionStatusTransitionTranslationAsync(int transitionId, Guid languageId, string name, string? description, DateTime now, CancellationToken cancellationToken)
    {
        TransactionStatusTransitionTranslation? entity = await _context.TransactionStatusTransitionTranslations.IgnoreQueryFilters().FirstOrDefaultAsync(item => item.TransactionStatusTransitionId == transitionId && item.LanguageId == languageId, cancellationToken);
        if (entity is null) { entity = new TransactionStatusTransitionTranslation { Id = Guid.NewGuid(), TransactionStatusTransitionId = transitionId, LanguageId = languageId, CreatedDate = now, CreatedBy = BackOfficeDemoSeedDefinition.SystemActor }; await _context.TransactionStatusTransitionTranslations.AddAsync(entity, cancellationToken); }
        else { entity.UpdatedDate = now; entity.UpdatedBy = BackOfficeDemoSeedDefinition.SystemActor; }
        entity.Name = name; entity.Description = description; entity.DeletedDate = null; entity.DeletedBy = null;
    }

    private async Task UpsertWorkflowTemplateTranslationAsync(Guid workflowTemplateId, Guid languageId, string name, string? description, DateTime now, CancellationToken cancellationToken)
    {
        WorkflowTemplateTranslation? entity = await _context.WorkflowTemplateTranslations.IgnoreQueryFilters().FirstOrDefaultAsync(item => item.WorkflowTemplateId == workflowTemplateId && item.LanguageId == languageId, cancellationToken);
        if (entity is null) { entity = new WorkflowTemplateTranslation { Id = Guid.NewGuid(), WorkflowTemplateId = workflowTemplateId, LanguageId = languageId, CreatedDate = now, CreatedBy = BackOfficeDemoSeedDefinition.SystemActor }; await _context.WorkflowTemplateTranslations.AddAsync(entity, cancellationToken); }
        else { entity.UpdatedDate = now; entity.UpdatedBy = BackOfficeDemoSeedDefinition.SystemActor; }
        entity.Name = name; entity.Description = description; entity.DeletedDate = null; entity.DeletedBy = null;
    }

    private async Task UpsertCongressTranslationAsync(Guid congressId, Guid languageId, string title, string shortDescription, string subtitle, string welcomeTitle, string welcomeContent, DateTime now, CancellationToken cancellationToken)
    {
        CongressTranslation? entity = await _context.CongressTranslations.IgnoreQueryFilters().FirstOrDefaultAsync(item => item.CongressId == congressId && item.LanguageId == languageId, cancellationToken);
        if (entity is null) { entity = new CongressTranslation { Id = Guid.NewGuid(), CongressId = congressId, LanguageId = languageId, CreatedDate = now, CreatedBy = BackOfficeDemoSeedDefinition.SystemActor }; await _context.CongressTranslations.AddAsync(entity, cancellationToken); }
        else { entity.UpdatedDate = now; entity.UpdatedBy = BackOfficeDemoSeedDefinition.SystemActor; }
        entity.Title = title;
        entity.Subtitle = subtitle;
        entity.ShortDescription = shortDescription;
        entity.Description = shortDescription;
        entity.WelcomeTitle = welcomeTitle;
        entity.WelcomeContent = welcomeContent;
        entity.SeoTitle = title;
        entity.SeoDescription = shortDescription;
        entity.DeletedDate = null;
        entity.DeletedBy = null;
    }
}
