using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.Application.Features.ProgramManagement.Services;

/// <summary>
/// Keeps program sessions inside their persisted time slots.
/// When a presentation is extended, moved or reordered, overflow presentations are cascaded
/// to the next chronological session of the same room. When a duration is shortened, later
/// presentations are pulled forward into the freed capacity.
/// </summary>
public static class ProgramScheduleRebalancer
{
    public static void RebalanceFromSessions(CongressProgramPlan plan, params Guid[] affectedSessionIds)
    {
        ArgumentNullException.ThrowIfNull(plan);

        Guid[] sessionIds = affectedSessionIds
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToArray();

        if (sessionIds.Length == 0)
            return;

        Dictionary<Guid, CongressProgramDay> dayById = plan.Days.ToDictionary(x => x.Id);
        List<CongressProgramSession> allSessions = plan.Days
            .OrderBy(x => x.Order)
            .ThenBy(x => x.Date)
            .SelectMany(day => day.Sessions
                .OrderBy(session => session.StartTime)
                .ThenBy(session => session.EndTime)
                .ThenBy(session => session.Order)
                .ThenBy(session => session.Id))
            .ToList();

        Dictionary<Guid, int> startIndexByRoom = new();
        foreach (Guid sessionId in sessionIds)
        {
            CongressProgramSession? affected = allSessions.FirstOrDefault(x => x.Id == sessionId);
            if (affected is null)
                continue;

            List<CongressProgramSession> roomSessions = allSessions
                .Where(x => x.EventRoomId == affected.EventRoomId)
                .ToList();
            int index = roomSessions.FindIndex(x => x.Id == affected.Id);
            if (index < 0)
                continue;

            if (!startIndexByRoom.TryGetValue(affected.EventRoomId, out int existingIndex)
                || index < existingIndex)
            {
                startIndexByRoom[affected.EventRoomId] = index;
            }
        }

        DateTime now = DateTime.UtcNow;
        foreach ((Guid roomId, int startIndex) in startIndexByRoom)
        {
            List<CongressProgramSession> roomSessions = allSessions
                .Where(x => x.EventRoomId == roomId)
                .ToList();

            RebalanceRoomStream(roomSessions, startIndex, dayById, now);
        }
    }

    private static void RebalanceRoomStream(
        IReadOnlyList<CongressProgramSession> roomSessions,
        int startIndex,
        IReadOnlyDictionary<Guid, CongressProgramDay> dayById,
        DateTime now)
    {
        if (startIndex < 0 || startIndex >= roomSessions.Count)
            return;

        List<CongressProgramItem> pending = new();
        for (int index = startIndex; index < roomSessions.Count; index++)
        {
            CongressProgramSession session = roomSessions[index];
            List<CongressProgramItem> movableItems = session.Items
                .Where(x => !x.IsLocked)
                .OrderBy(x => x.Order)
                .ThenBy(x => x.Id)
                .ToList();

            pending.AddRange(movableItems);
            foreach (CongressProgramItem item in movableItems)
                session.Items.Remove(item);
        }

        for (int index = startIndex; index < roomSessions.Count; index++)
        {
            CongressProgramSession session = roomSessions[index];
            CongressProgramDay day = ResolveDay(session, dayById);
            int capacity = GetAvailablePresentationMinutes(day, session);
            int lockedMinutes = session.Items
                .Where(x => x.IsLocked)
                .Sum(x => Math.Max(1, x.DurationMinutes));
            int remaining = Math.Max(0, capacity - lockedMinutes);

            while (pending.Count > 0)
            {
                CongressProgramItem next = pending[0];
                int duration = Math.Max(1, next.DurationMinutes);

                if (duration > capacity)
                {
                    throw new InvalidOperationException(
                        $"{next.Submission?.SubmissionNumber ?? next.SubmissionId.ToString("N")[..8]} bildirisi tek oturum kapasitesinden uzun. Süreyi azaltın veya oturum kapasitesini artırın.");
                }

                if (duration > remaining)
                    break;

                pending.RemoveAt(0);
                next.ProgramSessionId = session.Id;
                next.ProgramSession = session;
                next.UpdatedDate = now;
                session.Items.Add(next);
                remaining -= duration;
            }

            NormalizeItemOrders(session);
            session.UpdatedDate = now;
        }

        if (pending.Count > 0)
        {
            throw new InvalidOperationException(
                "Bu süre değişikliği/taşıma sonraki oturumlara sığmıyor. Aynı salonda sonraki oturum kapasitesi yok. Süreyi azaltın, yeni oturum oluşturun veya bazı bildirileri başka salona/güne taşıyın.");
        }
    }

    private static CongressProgramDay ResolveDay(
        CongressProgramSession session,
        IReadOnlyDictionary<Guid, CongressProgramDay> dayById)
    {
        if (session.ProgramDay is not null)
            return session.ProgramDay;

        if (dayById.TryGetValue(session.ProgramDayId, out CongressProgramDay? day))
            return day;

        throw new InvalidOperationException("Oturumun program günü bulunamadı.");
    }

    private static int GetAvailablePresentationMinutes(
        CongressProgramDay day,
        CongressProgramSession session)
    {
        int total = MinutesBetween(session.StartTime, session.EndTime);
        int embeddedBreakMinutes = day.FixedBlocks
            .Where(x => x.EventRoomId == session.EventRoomId
                        && x.BlockType == CongressProgramFixedBlockType.Break
                        && x.StartTime >= session.StartTime
                        && x.EndTime <= session.EndTime)
            .Sum(x => Math.Max(0, MinutesBetween(x.StartTime, x.EndTime)));

        return Math.Max(0, total - session.QuestionAnswerDurationMinutes - embeddedBreakMinutes);
    }

    private static void NormalizeItemOrders(CongressProgramSession session)
    {
        int order = 1;
        foreach (CongressProgramItem item in session.Items
                     .OrderBy(x => x.IsLocked ? 0 : 1)
                     .ThenBy(x => x.Order)
                     .ThenBy(x => x.Id)
                     .ToList())
        {
            item.Order = order++;
        }
    }

    private static int MinutesBetween(TimeOnly start, TimeOnly end)
        => (int)(end.ToTimeSpan() - start.ToTimeSpan()).TotalMinutes;
}
