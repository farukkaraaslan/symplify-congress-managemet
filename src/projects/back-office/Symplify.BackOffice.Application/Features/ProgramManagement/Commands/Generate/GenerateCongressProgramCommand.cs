using System.Text.Json;
using Core.Application.Pipelines.Authorization;
using FluentValidation;
using MediatR;
using Symplify.BackOffice.Application.Features.ProgramManagement.Constants;
using Symplify.BackOffice.Application.Features.ProgramManagement.Models;
using Symplify.BackOffice.Application.Features.ProgramManagement.Services;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.Application.Features.ProgramManagement.Commands.Generate;

public sealed class GenerateCongressProgramCommand : IRequest<ProgramGenerationResult>, ISecuredRequest
{
    public Guid CongressId { get; set; }
    public List<Guid> RoomIds { get; set; } = new();
    public TimeOnly DayStartTime { get; set; } = new(9, 0);
    public TimeOnly DayEndTime { get; set; } = new(18, 0);
    public int SessionDurationMinutes { get; set; } = 120;
    public int PresentationDurationMinutes { get; set; } = 10;
    public int QuestionAnswerDurationMinutes { get; set; } = 10;
    public int BreakDurationMinutes { get; set; } = 30;
    public bool IncludeSessionBreaks { get; set; }
    public int SessionBreakDurationMinutes { get; set; } = 10;
    public bool IncludeOpening { get; set; } = true;
    public int OpeningDurationMinutes { get; set; } = 60;
    public string OpeningTitle { get; set; } = "Açılış Konuşması";
    public Guid? OpeningRoomId { get; set; }
    public bool IncludeLunch { get; set; } = true;
    public TimeOnly LunchStartTime { get; set; } = new(12, 30);
    public int LunchDurationMinutes { get; set; } = 30;
    public string LunchTitle { get; set; } = "Öğle Arası";
    public CongressProgramGenerationMode Mode { get; set; } = CongressProgramGenerationMode.ReplaceAll;
    public ProgramSubmissionScopePreset SubmissionScopePreset { get; set; } = ProgramSubmissionScopePreset.AcceptedOnly;
    public List<string> WorkflowStatusCodes { get; set; } = new();
    public List<int> PaymentStatusIds { get; set; } = new();
    public List<Guid> SubmissionTypeIds { get; set; } = new();
    public List<Guid> TopicIds { get; set; } = new();
    public string? SubmissionSearchText { get; set; }
    public Guid? PerformedByUserId { get; set; }
    public string? Culture { get; set; }

    public string[] Roles => ProgramManagementOperationClaims.AdminOnly;

    public sealed class Handler : IRequestHandler<GenerateCongressProgramCommand, ProgramGenerationResult>
    {
        private readonly IProgramManagementRepository _repository;
        private readonly ICongressProgramScheduler _scheduler;

        public Handler(IProgramManagementRepository repository, ICongressProgramScheduler scheduler)
        {
            _repository = repository;
            _scheduler = scheduler;
        }

        public async Task<ProgramGenerationResult> Handle(
            GenerateCongressProgramCommand request,
            CancellationToken cancellationToken)
        {
            ProgramSubmissionFilterDto submissionFilter = new()
            {
                Preset = request.SubmissionScopePreset,
                WorkflowStatusCodes = request.WorkflowStatusCodes,
                PaymentStatusIds = request.PaymentStatusIds,
                SubmissionTypeIds = request.SubmissionTypeIds,
                TopicIds = request.TopicIds,
                SearchText = request.SubmissionSearchText
            };

            ProgramGenerationSourceDto source = await _repository.GetGenerationSourceAsync(
                request.CongressId,
                request.RoomIds,
                request.Culture,
                cancellationToken,
                submissionFilter)
                ?? throw new InvalidOperationException("Kongre bulunamadı.");

            if (source.Submissions.Count == 0 && source.VideoPresentations.Count == 0)
                throw new InvalidOperationException("Seçilen filtrelere uygun bildiri bulunamadı.");

            ProgramGenerationSettings settings = new()
            {
                CongressId = request.CongressId,
                RoomIds = request.RoomIds,
                DayStartTime = request.DayStartTime,
                DayEndTime = request.DayEndTime,
                SessionDurationMinutes = request.SessionDurationMinutes,
                PresentationDurationMinutes = request.PresentationDurationMinutes,
                QuestionAnswerDurationMinutes = request.QuestionAnswerDurationMinutes,
                BreakDurationMinutes = request.BreakDurationMinutes,
                IncludeSessionBreaks = request.IncludeSessionBreaks,
                SessionBreakDurationMinutes = request.SessionBreakDurationMinutes,
                IncludeOpening = request.IncludeOpening,
                OpeningDurationMinutes = request.OpeningDurationMinutes,
                OpeningTitle = request.OpeningTitle,
                OpeningRoomId = request.OpeningRoomId,
                IncludeLunch = request.IncludeLunch,
                LunchStartTime = request.LunchStartTime,
                LunchDurationMinutes = request.LunchDurationMinutes,
                LunchTitle = request.LunchTitle,
                Mode = request.Mode,
                PerformedByUserId = request.PerformedByUserId
            };

            CongressProgramPlan? existingPlan = await _repository.GetPlanForUpdateAsync(request.CongressId, cancellationToken);
            if (request.Mode == CongressProgramGenerationMode.FillUnassigned && existingPlan is not null)
            {
                _ = _scheduler.FillUnassigned(existingPlan, source, settings);

                HashSet<Guid> eligibleSubmissionIds = DeserializeSubmissionIds(existingPlan.EligibleSubmissionIdsJson);
                eligibleSubmissionIds.UnionWith(source.Submissions.Select(x => x.Id));
                eligibleSubmissionIds.UnionWith(source.VideoPresentations.Select(x => x.SubmissionId));
                eligibleSubmissionIds.UnionWith(existingPlan.Days
                    .SelectMany(x => x.Sessions)
                    .SelectMany(x => x.Items)
                    .Select(x => x.SubmissionId));

                existingPlan.SubmissionFilterJson = JsonSerializer.Serialize(submissionFilter);
                existingPlan.EligibleSubmissionIdsJson = JsonSerializer.Serialize(eligibleSubmissionIds.OrderBy(x => x));

                await _repository.SaveChangesAsync(cancellationToken);

                int assignedCount = existingPlan.Days
                    .SelectMany(x => x.Sessions)
                    .SelectMany(x => x.Items)
                    .Select(x => x.SubmissionId)
                    .Distinct()
                    .Count();

                return new ProgramGenerationResult
                {
                    EligibleSubmissionCount = source.Submissions.Count,
                    AssignedSubmissionCount = assignedCount,
                    UnassignedSubmissionCount = Math.Max(0, source.Submissions.Count - assignedCount),
                    SessionCount = existingPlan.Days.SelectMany(x => x.Sessions).Count(),
                    DayCount = existingPlan.Days.Count
                };
            }

            (CongressProgramPlan plan, ProgramGenerationResult result) = _scheduler.CreatePlan(source, settings);
            plan.SubmissionFilterJson = JsonSerializer.Serialize(submissionFilter);
            plan.EligibleSubmissionIdsJson = JsonSerializer.Serialize(
                source.Submissions
                    .Select(x => x.Id)
                    .Concat(source.VideoPresentations.Select(x => x.SubmissionId))
                    .Where(x => x != Guid.Empty)
                    .Distinct()
                    .OrderBy(x => x));
            if (existingPlan is not null)
            {
                _repository.RemovePlan(existingPlan);
                await _repository.SaveChangesAsync(cancellationToken);
            }

            await _repository.AddPlanAsync(plan, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
            return result;
        }
    }

    private static HashSet<Guid> DeserializeSubmissionIds(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new HashSet<Guid>();

        try
        {
            return JsonSerializer.Deserialize<Guid[]>(json)?
                .Where(x => x != Guid.Empty)
                .ToHashSet()
                ?? new HashSet<Guid>();
        }
        catch (JsonException)
        {
            return new HashSet<Guid>();
        }
    }
}

public sealed class GenerateCongressProgramCommandValidator : AbstractValidator<GenerateCongressProgramCommand>
{
    public GenerateCongressProgramCommandValidator()
    {
        RuleFor(x => x.CongressId).NotEmpty();
        RuleFor(x => x.RoomIds).NotEmpty();
        RuleFor(x => x.SessionDurationMinutes).InclusiveBetween(30, 360);
        RuleFor(x => x.PresentationDurationMinutes).InclusiveBetween(5, 120);
        RuleFor(x => x.QuestionAnswerDurationMinutes).InclusiveBetween(0, 180);
        RuleFor(x => x.BreakDurationMinutes).InclusiveBetween(0, 180);
        RuleFor(x => x.SessionBreakDurationMinutes).InclusiveBetween(0, 60);
        RuleFor(x => x.OpeningDurationMinutes).InclusiveBetween(0, 360);
        RuleFor(x => x.LunchDurationMinutes).InclusiveBetween(0, 180);
        RuleFor(x => x.SubmissionSearchText).MaximumLength(250);
        RuleForEach(x => x.WorkflowStatusCodes).MaximumLength(100);

        RuleFor(x => x).Custom((command, context) =>
        {
            if (command.DayEndTime <= command.DayStartTime)
                context.AddFailure(nameof(command.DayEndTime), "Gün bitiş saati başlangıç saatinden sonra olmalıdır.");

            int effectiveSessionBreakMinutes = command.IncludeSessionBreaks
                ? command.SessionBreakDurationMinutes
                : 0;

            if (command.IncludeSessionBreaks && command.SessionBreakDurationMinutes <= 0)
            {
                context.AddFailure(
                    nameof(command.SessionBreakDurationMinutes),
                    "Oturum içi ara etkinse süre sıfırdan büyük olmalıdır.");
            }

            if (command.PresentationDurationMinutes + command.QuestionAnswerDurationMinutes + effectiveSessionBreakMinutes > command.SessionDurationMinutes)
            {
                context.AddFailure(
                    nameof(command.SessionDurationMinutes),
                    "Oturum süresi, en az bir bildiri, varsa oturum içi ara ve soru-cevap süresini karşılamalıdır.");
            }

            TimeOnly? openingEnd = null;
            if (command.IncludeOpening)
            {
                if (command.OpeningDurationMinutes <= 0)
                    context.AddFailure(nameof(command.OpeningDurationMinutes), "Açılış bloğu etkinse süresi sıfırdan büyük olmalıdır.");
                else
                    openingEnd = AddMinutes(command.DayStartTime, command.OpeningDurationMinutes);

                if (openingEnd.HasValue && openingEnd.Value > command.DayEndTime)
                    context.AddFailure(nameof(command.OpeningDurationMinutes), "Açılış bloğu günlük çalışma saatlerinin dışına taşıyor.");

                if (command.OpeningRoomId.HasValue && !command.RoomIds.Contains(command.OpeningRoomId.Value))
                    context.AddFailure(nameof(command.OpeningRoomId), "Açılış salonu programa dahil edilen salonlardan biri olmalıdır.");
            }

            if (command.IncludeLunch)
            {
                if (command.LunchDurationMinutes <= 0)
                {
                    context.AddFailure(nameof(command.LunchDurationMinutes), "Öğle arası etkinse süresi sıfırdan büyük olmalıdır.");
                }
                else
                {
                    TimeOnly lunchEnd = AddMinutes(command.LunchStartTime, command.LunchDurationMinutes);
                    if (command.LunchStartTime < command.DayStartTime || lunchEnd > command.DayEndTime)
                        context.AddFailure(nameof(command.LunchStartTime), "Öğle arası günlük çalışma saatlerinin içinde olmalıdır.");

                    if (openingEnd.HasValue
                        && command.DayStartTime < lunchEnd
                        && command.LunchStartTime < openingEnd.Value)
                    {
                        context.AddFailure(nameof(command.LunchStartTime), "Açılış bloğu ile öğle arası çakışamaz.");
                    }
                }
            }
        });
    }

    private static TimeOnly AddMinutes(TimeOnly time, int minutes)
        => TimeOnly.FromTimeSpan(time.ToTimeSpan().Add(TimeSpan.FromMinutes(minutes)));
}
