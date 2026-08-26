using Core.Application.Pipelines.Authorization;
using MediatR;
using Symplify.BackOffice.Application.Features.ProgramManagement.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.Application.Features.ProgramManagement.Commands.DeleteBreak;

/// <summary>
/// Permanently removes a generated break from the draft programme.
/// The following sessions/breaks in the same fixed-anchor segment are shifted
/// earlier so the removed interval is not recreated as another persisted break.
/// Opening, lunch and other non-break fixed blocks are never deleted here.
/// </summary>
public sealed class DeleteProgramBreakCommand : IRequest, ISecuredRequest
{
    public Guid CongressId { get; set; }
    public Guid BreakId { get; set; }
    public string[] Roles => ProgramManagementOperationClaims.AdminOnly;

    public sealed class Handler : IRequestHandler<DeleteProgramBreakCommand>
    {
        private readonly IProgramManagementRepository _repository;

        public Handler(IProgramManagementRepository repository)
        {
            _repository = repository;
        }

        public async Task Handle(DeleteProgramBreakCommand request, CancellationToken cancellationToken)
        {
            CongressProgramPlan plan = await _repository.GetPlanForUpdateAsync(
                    request.CongressId,
                    cancellationToken)
                ?? throw new InvalidOperationException("Program taslağı bulunamadı.");

            CongressProgramDay? day = plan.Days
                .FirstOrDefault(x => x.FixedBlocks.Any(block => block.Id == request.BreakId));
            CongressProgramFixedBlock? breakBlock = day?.FixedBlocks
                .FirstOrDefault(x => x.Id == request.BreakId);

            if (day is null || breakBlock is null)
                throw new InvalidOperationException("Kaldırılacak ara bloğu bulunamadı.");

            if (breakBlock.BlockType != CongressProgramFixedBlockType.Break)
                throw new InvalidOperationException("Yalnızca ara blokları kaldırılabilir.");

            if (!breakBlock.EventRoomId.HasValue)
                throw new InvalidOperationException("Salon bağı olmayan ortak bloklar bu işlemle kaldırılamaz.");

            int durationMinutes = MinutesBetween(breakBlock.StartTime, breakBlock.EndTime);
            if (durationMinutes <= 0)
                throw new InvalidOperationException("Ara süresi geçerli değil.");

            Guid roomId = breakBlock.EventRoomId.Value;
            DateTime now = DateTime.UtcNow;
            TimeOnly originalBreakStart = breakBlock.StartTime;
            TimeOnly originalBreakEnd = breakBlock.EndTime;

            CongressProgramSession? hostSession = day.Sessions
                .Where(x => x.EventRoomId == roomId)
                .Where(x => originalBreakStart >= x.StartTime && originalBreakEnd <= x.EndTime)
                .OrderBy(x => MinutesBetween(x.StartTime, x.EndTime))
                .ThenBy(x => x.StartTime)
                .FirstOrDefault();

            TimeOnly shiftBoundary = ResolveNextFixedAnchorStart(
                day,
                roomId,
                originalBreakEnd,
                breakBlock.Id);

            if (hostSession is not null)
            {
                TimeOnly originalSessionEnd = hostSession.EndTime;
                TimeOnly newSessionEnd = hostSession.EndTime.AddMinutes(-durationMinutes);
                if (newSessionEnd <= hostSession.StartTime)
                    throw new InvalidOperationException("Ara kaldırıldığında oturum süresi geçersiz hale geliyor.");

                hostSession.EndTime = newSessionEnd;
                hostSession.UpdatedDate = now;

                ShiftLaterEmbeddedBreaks(
                    day,
                    roomId,
                    breakBlock.Id,
                    originalBreakEnd,
                    originalSessionEnd,
                    durationMinutes,
                    now);

                ShiftFollowingTimelineEntities(
                    day,
                    roomId,
                    originalSessionEnd,
                    shiftBoundary,
                    durationMinutes,
                    breakBlock.Id,
                    hostSession.Id,
                    now);
            }
            else
            {
                ShiftFollowingTimelineEntities(
                    day,
                    roomId,
                    originalBreakEnd,
                    shiftBoundary,
                    durationMinutes,
                    breakBlock.Id,
                    null,
                    now);
            }

            day.FixedBlocks.Remove(breakBlock);
            _repository.RemoveFixedBlock(breakBlock);

            NormalizeOrders(day, roomId, now);
            plan.UpdatedDate = now;
            await _repository.SaveChangesAsync(cancellationToken);
        }

        private static TimeOnly ResolveNextFixedAnchorStart(
            CongressProgramDay day,
            Guid roomId,
            TimeOnly after,
            Guid excludedBreakId)
        {
            CongressProgramFixedBlock? nextAnchor = day.FixedBlocks
                .Where(x => x.Id != excludedBreakId)
                .Where(x => x.BlockType != CongressProgramFixedBlockType.Break)
                .Where(x => !x.EventRoomId.HasValue || x.EventRoomId == roomId)
                .Where(x => x.StartTime >= after)
                .OrderBy(x => x.StartTime)
                .ThenBy(x => x.Order)
                .FirstOrDefault();

            return nextAnchor?.StartTime ?? day.EndTime;
        }

        private static void ShiftLaterEmbeddedBreaks(
            CongressProgramDay day,
            Guid roomId,
            Guid removedBreakId,
            TimeOnly removedBreakEnd,
            TimeOnly hostSessionOriginalEnd,
            int durationMinutes,
            DateTime now)
        {
            foreach (CongressProgramFixedBlock block in day.FixedBlocks
                         .Where(x => x.Id != removedBreakId)
                         .Where(x => x.EventRoomId == roomId)
                         .Where(x => x.BlockType == CongressProgramFixedBlockType.Break)
                         .Where(x => x.StartTime >= removedBreakEnd && x.EndTime <= hostSessionOriginalEnd))
            {
                block.StartTime = block.StartTime.AddMinutes(-durationMinutes);
                block.EndTime = block.EndTime.AddMinutes(-durationMinutes);
                block.UpdatedDate = now;
            }
        }

        private static void ShiftFollowingTimelineEntities(
            CongressProgramDay day,
            Guid roomId,
            TimeOnly from,
            TimeOnly boundary,
            int durationMinutes,
            Guid removedBreakId,
            Guid? excludedSessionId,
            DateTime now)
        {
            foreach (CongressProgramSession session in day.Sessions
                         .Where(x => x.EventRoomId == roomId)
                         .Where(x => !excludedSessionId.HasValue || x.Id != excludedSessionId.Value)
                         .Where(x => x.StartTime >= from && x.EndTime <= boundary)
                         .OrderBy(x => x.StartTime))
            {
                session.StartTime = session.StartTime.AddMinutes(-durationMinutes);
                session.EndTime = session.EndTime.AddMinutes(-durationMinutes);
                session.UpdatedDate = now;
            }

            foreach (CongressProgramFixedBlock block in day.FixedBlocks
                         .Where(x => x.Id != removedBreakId)
                         .Where(x => x.EventRoomId == roomId)
                         .Where(x => x.BlockType == CongressProgramFixedBlockType.Break)
                         .Where(x => x.StartTime >= from && x.EndTime <= boundary)
                         .OrderBy(x => x.StartTime))
            {
                block.StartTime = block.StartTime.AddMinutes(-durationMinutes);
                block.EndTime = block.EndTime.AddMinutes(-durationMinutes);
                block.UpdatedDate = now;
            }
        }

        private static void NormalizeOrders(CongressProgramDay day, Guid roomId, DateTime now)
        {
            int sessionOrder = 1;
            foreach (CongressProgramSession session in day.Sessions
                         .Where(x => x.EventRoomId == roomId)
                         .OrderBy(x => x.StartTime)
                         .ThenBy(x => x.EndTime)
                         .ThenBy(x => x.Id))
            {
                session.Order = sessionOrder++;
                session.UpdatedDate = now;
            }

            int blockOrder = 1;
            foreach (CongressProgramFixedBlock block in day.FixedBlocks
                         .Where(x => !x.EventRoomId.HasValue || x.EventRoomId == roomId)
                         .OrderBy(x => x.StartTime)
                         .ThenBy(x => x.EndTime)
                         .ThenBy(x => x.Id))
            {
                block.Order = blockOrder++;
                block.UpdatedDate = now;
            }
        }

        private static int MinutesBetween(TimeOnly start, TimeOnly end)
            => (int)(end.ToTimeSpan() - start.ToTimeSpan()).TotalMinutes;
    }
}
