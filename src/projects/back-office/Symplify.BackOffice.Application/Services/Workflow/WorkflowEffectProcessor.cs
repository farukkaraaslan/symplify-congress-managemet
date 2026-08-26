using System.Text.Json;
using Core.Persistence.Paging;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Enums;
using Symplify.BackOffice.Domain.Submission;
using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Application.Services.Workflow;

public sealed class WorkflowEffectProcessor : IWorkflowEffectProcessor
{
    private readonly IAcceptanceLetterService _acceptanceLetterService;
    private readonly IMailOutboxService _mailOutboxService;
    private readonly ISubmissionAcceptanceLetterRepository _acceptanceLetterRepository;

    public WorkflowEffectProcessor(
        IAcceptanceLetterService acceptanceLetterService,
        IMailOutboxService mailOutboxService,
        ISubmissionAcceptanceLetterRepository acceptanceLetterRepository)
    {
        _acceptanceLetterService = acceptanceLetterService;
        _mailOutboxService = mailOutboxService;
        _acceptanceLetterRepository = acceptanceLetterRepository;
    }

    public async Task ProcessAsync(
        WorkflowContext context,
        IReadOnlyCollection<WorkflowTransitionEffect> effects,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<SubmissionAcceptanceLetter>? latestLetters = null;
        foreach (WorkflowTransitionEffect effect in effects.OrderBy(item => item.Order))
        {
            if (effect.EffectType == WorkflowEffectType.GenerateAcceptanceLetter)
            {
                latestLetters = await _acceptanceLetterService.GenerateAsync(context.Submission, cancellationToken);
                continue;
            }

            if (effect.EffectType == WorkflowEffectType.QueueAcceptanceEmail)
            {
                latestLetters ??= await GetLatestAcceptanceLettersAsync(context.Submission.Id, cancellationToken);

                if (latestLetters.Count == 0)
                    continue;

                string? fallbackEmail = ResolveToEmail(effect.ParametersJson, context.Submission);

                foreach (SubmissionAcceptanceLetter letter in latestLetters)
                {
                    await _mailOutboxService.QueueAcceptanceEmailAsync(
                        context.Submission,
                        letter,
                        fallbackEmail,
                        cancellationToken);
                }

                continue;
            }

            if (effect.EffectType == WorkflowEffectType.QueueSubmissionStatusEmail)
            {
                string templateCode = ResolveTemplateCode(effect.ParametersJson);
                string? fallbackEmail = ResolveToEmail(effect.ParametersJson, context.Submission);

                await _mailOutboxService.QueueSubmissionStatusEmailAsync(
                    context.Submission,
                    templateCode,
                    fallbackEmail,
                    cancellationToken);
            }
        }
    }

    private async Task<IReadOnlyList<SubmissionAcceptanceLetter>> GetLatestAcceptanceLettersAsync(Guid submissionId, CancellationToken cancellationToken)
    {
        IPaginate<SubmissionAcceptanceLetter> letters = await _acceptanceLetterRepository.GetListAsync(
            predicate: letter => letter.SubmissionId == submissionId,
            orderBy: query => query.OrderByDescending(letter => letter.GeneratedAt),
            index: 0,
            size: 100,
            cancellationToken: cancellationToken);

        return letters.Items
            .GroupBy(letter => letter.AuthorId ?? Guid.Empty)
            .Select(group => group.OrderByDescending(letter => letter.GeneratedAt).First())
            .ToList();
    }

    private static string ResolveTemplateCode(string parametersJson)
    {
        if (string.IsNullOrWhiteSpace(parametersJson))
            return "SUBMISSION_SENT_TO_REVIEW";

        using JsonDocument document = JsonDocument.Parse(parametersJson);

        if (document.RootElement.TryGetProperty("templateCode", out JsonElement templateCodeElement))
            return templateCodeElement.GetString() ?? "SUBMISSION_SENT_TO_REVIEW";

        return "SUBMISSION_SENT_TO_REVIEW";
    }

    private static string? ResolveToEmail(string parametersJson, Submission submission)
    {
        if (!string.IsNullOrWhiteSpace(parametersJson))
        {
            using JsonDocument document = JsonDocument.Parse(parametersJson);

            if (document.RootElement.TryGetProperty("toEmail", out JsonElement toEmailElement))
                return toEmailElement.GetString();

            if (document.RootElement.TryGetProperty("recipientEmail", out JsonElement recipientEmailElement))
                return recipientEmailElement.GetString();
        }

        return submission.Authors.FirstOrDefault(author => author.IsCorrespondingAuthor)?.Email
            ?? submission.Authors.FirstOrDefault()?.Email;
    }

}