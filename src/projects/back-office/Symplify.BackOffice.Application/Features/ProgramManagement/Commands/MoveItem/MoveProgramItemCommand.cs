using Core.Application.Pipelines.Authorization;
using FluentValidation;
using MediatR;
using Symplify.BackOffice.Application.Features.ProgramManagement.Constants;
using Symplify.BackOffice.Application.Features.ProgramManagement.Services;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Enums;
using Symplify.BackOffice.Domain.Submission;

namespace Symplify.BackOffice.Application.Features.ProgramManagement.Commands.MoveItem;

public sealed class MoveProgramItemCommand : IRequest, ISecuredRequest
{
    public Guid CongressId { get; set; }
    public Guid ItemId { get; set; }
    public Guid TargetSessionId { get; set; }
    public string[] Roles => ProgramManagementOperationClaims.AdminOnly;

    public sealed class Handler : IRequestHandler<MoveProgramItemCommand>
    {
        private readonly IProgramManagementRepository _repository;

        public Handler(IProgramManagementRepository repository)
        {
            _repository = repository;
        }

        public async Task Handle(MoveProgramItemCommand request, CancellationToken cancellationToken)
        {
            CongressProgramPlan plan = await _repository.GetPlanForUpdateAsync(request.CongressId, cancellationToken)
                ?? throw new InvalidOperationException("Program taslağı bulunamadı.");

            List<CongressProgramSession> sessions = plan.Days
                .SelectMany(x => x.Sessions)
                .ToList();

            CongressProgramSession sourceSession = sessions
                .FirstOrDefault(x => x.Items.Any(i => i.Id == request.ItemId))
                ?? throw new InvalidOperationException("Taşınacak bildiri programda bulunamadı.");
            CongressProgramSession targetSession = sessions
                .FirstOrDefault(x => x.Id == request.TargetSessionId)
                ?? throw new InvalidOperationException("Hedef oturum bulunamadı.");
            CongressProgramItem item = sourceSession.Items.First(x => x.Id == request.ItemId);

            if (item.IsLocked)
                throw new InvalidOperationException("Kilitli bildiri taşınamaz.");
            if (sourceSession.Id == targetSession.Id)
                return;

            if (HasAuthorConflict(item, targetSession, sessions))
            {
                throw new InvalidOperationException(
                    "Bu bildirinin yazarlarından en az biri hedef oturumla aynı saatte başka bir oturumda yer alıyor.");
            }

            sourceSession.Items.Remove(item);
            item.ProgramSessionId = targetSession.Id;
            item.ProgramSession = targetSession;
            item.Order = targetSession.Items.Count + 1;
            item.Source = CongressProgramItemSource.Manual;
            targetSession.Items.Add(item);

            NormalizeOrder(sourceSession);
            NormalizeOrder(targetSession);
            ProgramScheduleRebalancer.RebalanceFromSessions(plan, sourceSession.Id, targetSession.Id);

            DateTime now = DateTime.UtcNow;
            item.UpdatedDate = now;
            sourceSession.UpdatedDate = now;
            targetSession.UpdatedDate = now;
            plan.UpdatedDate = now;

            await _repository.SaveChangesAsync(cancellationToken);
        }

        private static bool HasAuthorConflict(
            CongressProgramItem movingItem,
            CongressProgramSession targetSession,
            IReadOnlyCollection<CongressProgramSession> allSessions)
        {
            HashSet<string> movingAuthorKeys = movingItem.Submission.Authors
                .Select(BuildAuthorKey)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (movingAuthorKeys.Count == 0)
                return false;

            foreach (CongressProgramSession session in allSessions)
            {
                if (session.Id == targetSession.Id || session.ProgramDayId != targetSession.ProgramDayId)
                    continue;
                if (!Overlaps(targetSession.StartTime, targetSession.EndTime, session.StartTime, session.EndTime))
                    continue;

                foreach (CongressProgramItem item in session.Items)
                {
                    if (item.Id == movingItem.Id)
                        continue;

                    if (item.Submission.Authors
                        .Select(BuildAuthorKey)
                        .Any(movingAuthorKeys.Contains))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static string BuildAuthorKey(Author author)
        {
            if (!string.IsNullOrWhiteSpace(author.Orcid))
                return $"orcid:{NormalizeIdentityPart(author.Orcid)}";
            if (!string.IsNullOrWhiteSpace(author.Email))
                return $"email:{NormalizeIdentityPart(author.Email)}";

            return $"name:{NormalizeIdentityPart(author.FirstName)}|{NormalizeIdentityPart(author.LastName)}|{NormalizeIdentityPart(author.Institution)}";
        }

        private static string NormalizeIdentityPart(string? value)
            => string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToUpperInvariant();

        private static void NormalizeOrder(CongressProgramSession session)
        {
            int order = 1;
            foreach (CongressProgramItem item in session.Items.OrderBy(x => x.Order).ThenBy(x => x.Id))
                item.Order = order++;
        }


        private static int GetEmbeddedBreakMinutes(
            CongressProgramDay day,
            CongressProgramSession session)
            => day.FixedBlocks
                .Where(x => x.EventRoomId == session.EventRoomId
                            && x.BlockType == CongressProgramFixedBlockType.Break
                            && x.StartTime >= session.StartTime
                            && x.EndTime <= session.EndTime)
                .Sum(x => MinutesBetween(x.StartTime, x.EndTime));

        private static bool Overlaps(TimeOnly start1, TimeOnly end1, TimeOnly start2, TimeOnly end2)
            => start1 < end2 && start2 < end1;

        private static int MinutesBetween(TimeOnly start, TimeOnly end)
            => (int)(end.ToTimeSpan() - start.ToTimeSpan()).TotalMinutes;
    }
}

public sealed class MoveProgramItemCommandValidator : AbstractValidator<MoveProgramItemCommand>
{
    public MoveProgramItemCommandValidator()
    {
        RuleFor(x => x.CongressId).NotEmpty();
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.TargetSessionId).NotEmpty();
    }
}
