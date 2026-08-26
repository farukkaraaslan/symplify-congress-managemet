using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Workflow;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Seeding.Seeders;

public sealed class CongressWorkflowBackfillSeeder
{
    private readonly BackOfficeDbContext _context;
    private readonly ILogger<CongressWorkflowBackfillSeeder> _logger;

    public CongressWorkflowBackfillSeeder(
        BackOfficeDbContext context,
        ILogger<CongressWorkflowBackfillSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        WorkflowTemplate? defaultTemplate = await _context.WorkflowTemplates
            .AsNoTracking()
            .Where(template => template.IsActive && template.IsDefault)
            .OrderBy(template => template.Code)
            .ThenBy(template => template.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (defaultTemplate is null)
        {
            _logger.LogInformation("Congress workflow backfill skipped. Active default workflow template was not found.");
            return;
        }

        List<WorkflowTemplateTransition> templateTransitions = await _context.WorkflowTemplateTransitions
            .AsNoTracking()
            .Where(transition => transition.WorkflowTemplateId == defaultTemplate.Id && transition.IsActive)
            .OrderBy(transition => transition.Order)
            .ThenBy(transition => transition.Id)
            .ToListAsync(cancellationToken);

        if (templateTransitions.Count == 0)
        {
            _logger.LogInformation("Congress workflow backfill skipped. Default workflow template has no active transition.");
            return;
        }

        HashSet<Guid> congressIdsWithWorkflow = await _context.CongressWorkflowSettings
            .AsNoTracking()
            .Select(setting => setting.CongressId)
            .ToHashSetAsync(cancellationToken);

        List<Guid> congressIdsWithoutWorkflow = await _context.Congresses
            .AsNoTracking()
            .Where(congress => !congressIdsWithWorkflow.Contains(congress.Id))
            .OrderBy(congress => congress.CreatedDate)
            .Select(congress => congress.Id)
            .ToListAsync(cancellationToken);

        if (congressIdsWithoutWorkflow.Count == 0)
        {
            _logger.LogInformation("Congress workflow backfill skipped. All congresses already have workflow settings.");
            return;
        }

        DateTime utcNow = DateTime.UtcNow;

        foreach (Guid congressId in congressIdsWithoutWorkflow)
        {
            await _context.CongressWorkflowSettings.AddAsync(new CongressWorkflowSetting
            {
                Id = Guid.NewGuid(),
                CongressId = congressId,
                SourceWorkflowTemplateId = defaultTemplate.Id,
                InitialTransactionStatusId = defaultTemplate.InitialTransactionStatusId,
                IsActive = true,
                CreatedDate = utcNow,
                CreatedBy = "System"
            }, cancellationToken);

            foreach (WorkflowTemplateTransition templateTransition in templateTransitions)
            {
                await _context.CongressTransactionStatusTransitions.AddAsync(new CongressTransactionStatusTransition
                {
                    Id = Guid.NewGuid(),
                    CongressId = congressId,
                    TransactionStatusTransitionId = templateTransition.TransactionStatusTransitionId,
                    SourceWorkflowTemplateTransitionId = templateTransition.Id,
                    Order = templateTransition.Order,
                    IsActive = templateTransition.IsActive,
                    CreatedDate = utcNow,
                    CreatedBy = "System"
                }, cancellationToken);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Congress workflow backfill completed. Default workflow template {WorkflowTemplateId} applied to {CongressCount} congresses.",
            defaultTemplate.Id,
            congressIdsWithoutWorkflow.Count);
    }
}
