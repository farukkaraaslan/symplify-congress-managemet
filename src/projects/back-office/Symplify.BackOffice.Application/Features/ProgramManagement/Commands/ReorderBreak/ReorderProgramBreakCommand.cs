using Core.Application.Pipelines.Authorization;
using FluentValidation;
using MediatR;
using Symplify.BackOffice.Application.Features.ProgramManagement.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.Application.Features.ProgramManagement.Commands.ReorderBreak;

/// <summary>
/// Moves an automatically generated break either on the room timeline or between presentations
/// of a non-empty session. Opening and lunch blocks remain fixed anchors.
/// </summary>
public sealed class ReorderProgramBreakCommand : IRequest, ISecuredRequest
{
    public Guid CongressId { get; set; }
    public Guid ProgramDayId { get; set; }
    public Guid EventRoomId { get; set; }
    public Guid BreakId { get; set; }
    public Guid? TargetSessionId { get; set; }
    public int? TargetItemIndex { get; set; }
    public List<string> OrderedBlockKeys { get; set; } = new();
    public string[] Roles => ProgramManagementOperationClaims.AdminOnly;

    public sealed class Handler : IRequestHandler<ReorderProgramBreakCommand>
    {
        private readonly IProgramManagementRepository _repository;

        public Handler(IProgramManagementRepository repository)
        {
            _repository = repository;
        }

        public async Task Handle(ReorderProgramBreakCommand request, CancellationToken cancellationToken)
        {
            CongressProgramPlan plan = await _repository.GetPlanForUpdateAsync(request.CongressId, cancellationToken)
                ?? throw new InvalidOperationException("Program taslağı bulunamadı.");

            CongressProgramDay day = plan.Days.FirstOrDefault(x => x.FixedBlocks.Any(block => block.Id == request.BreakId))
                ?? throw new InvalidOperationException("Program günü bulunamadı.");

            CongressProgramFixedBlock movingBreak = day.FixedBlocks.FirstOrDefault(x => x.Id == request.BreakId)
                ?? throw new InvalidOperationException("Taşınacak ara bloğu bulunamadı.");

            if (movingBreak.BlockType != CongressProgramFixedBlockType.Break)
                throw new InvalidOperationException("Yalnızca ara blokları sürüklenebilir.");
            if (!movingBreak.EventRoomId.HasValue)
                throw new InvalidOperationException("Salon bağı olmayan ortak bloklar taşınamaz.");

            bool normalizedInsideOperation = false;

            if (request.TargetSessionId.HasValue)
            {
                CongressProgramSession targetSession = plan.Days
                    .SelectMany(x => x.Sessions)
                    .FirstOrDefault(x => x.Id == request.TargetSessionId.Value)
                    ?? throw new InvalidOperationException("Hedef oturum bulunamadı.");

                if (targetSession.ProgramDayId != day.Id || targetSession.EventRoomId != movingBreak.EventRoomId.Value)
                {
                    MoveBreakAcrossSession(
                        plan,
                        day,
                        movingBreak,
                        targetSession,
                        request.TargetItemIndex ?? 0);
                    normalizedInsideOperation = true;
                }
                else
                {
                    MoveBreakBetweenPresentations(
                        day,
                        targetSession.EventRoomId,
                        movingBreak,
                        targetSession.Id,
                        request.TargetItemIndex ?? 0);
                }
            }
            else
            {
                if (movingBreak.EventRoomId != request.EventRoomId)
                    throw new InvalidOperationException("Ara bloğu yalnızca kendi salon zaman çizelgesinde taşınabilir.");

                MoveBreakOnRoomTimeline(day, request.EventRoomId, movingBreak, request.OrderedBlockKeys);
            }

            DateTime now = DateTime.UtcNow;
            if (!normalizedInsideOperation)
                NormalizeOrders(day, request.EventRoomId, now);
            movingBreak.IsLocked = false;
            movingBreak.UpdatedDate = now;
            plan.UpdatedDate = now;
            await _repository.SaveChangesAsync(cancellationToken);
        }

        private static void MoveBreakAcrossSession(
            CongressProgramPlan plan,
            CongressProgramDay sourceDay,
            CongressProgramFixedBlock movingBreak,
            CongressProgramSession targetSession,
            int targetItemIndex)
        {
            if (!movingBreak.EventRoomId.HasValue)
                throw new InvalidOperationException("Ara için kaynak salon bilgisi bulunamadı.");

            CongressProgramDay targetDay = plan.Days.First(x => x.Id == targetSession.ProgramDayId);
            int duration = MinutesBetween(movingBreak.StartTime, movingBreak.EndTime);
            if (duration <= 0)
                throw new InvalidOperationException("Ara süresi geçerli değil.");

            DateTime now = DateTime.UtcNow;
            Guid sourceRoomId = movingBreak.EventRoomId.Value;
            RemoveBreakFromSourceTimeline(sourceDay, sourceRoomId, movingBreak, duration, now);

            if (sourceDay.Id != targetDay.Id)
            {
                sourceDay.FixedBlocks.Remove(movingBreak);
                targetDay.FixedBlocks.Add(movingBreak);
                movingBreak.ProgramDayId = targetDay.Id;
                movingBreak.ProgramDay = targetDay;
            }

            movingBreak.EventRoomId = targetSession.EventRoomId;

            List<CongressProgramItem> targetItems = targetSession.Items
                .OrderBy(x => x.Order)
                .ThenBy(x => x.Id)
                .ToList();
            if (targetItems.Count == 0)
                throw new InvalidOperationException("Ara, bildirisi olmayan bir oturuma yerleştirilemez.");

            int insertionIndex = Math.Clamp(targetItemIndex, 0, targetItems.Count);
            int minutesBeforeBreak = targetItems
                .Take(insertionIndex)
                .Sum(x => Math.Max(1, x.DurationMinutes));

            TimeOnly originalTargetEnd = targetSession.EndTime;
            movingBreak.StartTime = AddMinutes(targetSession.StartTime, minutesBeforeBreak);
            movingBreak.EndTime = AddMinutes(movingBreak.StartTime, duration);
            movingBreak.IsLocked = false;
            movingBreak.UpdatedDate = now;

            targetSession.EndTime = AddMinutes(targetSession.EndTime, duration);
            targetSession.UpdatedDate = now;
            ShiftFollowingEntities(targetDay, targetSession.EventRoomId, originalTargetEnd, duration, movingBreak.Id, targetSession.Id, now);

            NormalizeOrders(sourceDay, sourceRoomId, now);
            NormalizeOrders(targetDay, targetSession.EventRoomId, now);
        }

        private static void RemoveBreakFromSourceTimeline(
            CongressProgramDay sourceDay,
            Guid sourceRoomId,
            CongressProgramFixedBlock movingBreak,
            int duration,
            DateTime now)
        {
            TimeOnly originalBreakEnd = movingBreak.EndTime;
            CongressProgramSession? hostSession = sourceDay.Sessions
                .Where(x => x.EventRoomId == sourceRoomId)
                .Where(x => movingBreak.StartTime >= x.StartTime && movingBreak.EndTime <= x.EndTime)
                .OrderBy(x => MinutesBetween(x.StartTime, x.EndTime))
                .ThenBy(x => x.StartTime)
                .FirstOrDefault();

            if (hostSession is not null)
            {
                TimeOnly originalSessionEnd = hostSession.EndTime;
                TimeOnly newSessionEnd = AddMinutes(hostSession.EndTime, -duration);
                if (newSessionEnd <= hostSession.StartTime)
                    throw new InvalidOperationException("Ara taşındığında kaynak oturum süresi geçersiz hale geliyor.");

                hostSession.EndTime = newSessionEnd;
                hostSession.UpdatedDate = now;
                ShiftFollowingEntities(sourceDay, sourceRoomId, originalSessionEnd, -duration, movingBreak.Id, hostSession.Id, now);
                return;
            }

            ShiftFollowingEntities(sourceDay, sourceRoomId, originalBreakEnd, -duration, movingBreak.Id, null, now);
        }

        private static void ShiftFollowingEntities(
            CongressProgramDay day,
            Guid roomId,
            TimeOnly from,
            int deltaMinutes,
            Guid excludedBreakId,
            Guid? excludedSessionId,
            DateTime now)
        {
            foreach (CongressProgramSession session in day.Sessions
                         .Where(x => x.EventRoomId == roomId)
                         .Where(x => !excludedSessionId.HasValue || x.Id != excludedSessionId.Value)
                         .Where(x => x.StartTime >= from)
                         .OrderBy(x => x.StartTime))
            {
                session.StartTime = AddMinutes(session.StartTime, deltaMinutes);
                session.EndTime = AddMinutes(session.EndTime, deltaMinutes);
                session.UpdatedDate = now;
            }

            foreach (CongressProgramFixedBlock block in day.FixedBlocks
                         .Where(x => x.Id != excludedBreakId)
                         .Where(x => x.EventRoomId == roomId)
                         .Where(x => x.BlockType == CongressProgramFixedBlockType.Break)
                         .Where(x => x.StartTime >= from)
                         .OrderBy(x => x.StartTime))
            {
                block.StartTime = AddMinutes(block.StartTime, deltaMinutes);
                block.EndTime = AddMinutes(block.EndTime, deltaMinutes);
                block.UpdatedDate = now;
            }
        }

        private static void MoveBreakBetweenPresentations(
            CongressProgramDay day,
            Guid roomId,
            CongressProgramFixedBlock movingBreak,
            Guid targetSessionId,
            int targetItemIndex)
        {
            CongressProgramSession targetSession = day.Sessions.FirstOrDefault(x => x.Id == targetSessionId)
                ?? throw new InvalidOperationException("Hedef oturum bulunamadı.");

            if (targetSession.EventRoomId != roomId)
                throw new InvalidOperationException("Ara yalnızca aynı salon içindeki bir oturuma taşınabilir.");

            List<CongressProgramItem> targetItems = targetSession.Items
                .OrderBy(x => x.Order)
                .ThenBy(x => x.Id)
                .ToList();

            if (targetItems.Count == 0)
                throw new InvalidOperationException("Ara, bildirisi olmayan bir oturuma yerleştirilemez.");
            if (targetItemIndex < 0 || targetItemIndex > targetItems.Count)
                throw new InvalidOperationException("Ara için seçilen bildiri konumu geçerli değil.");

            SegmentBounds sourceSegment = ResolveSegment(day, roomId, movingBreak.StartTime, movingBreak.EndTime);
            SegmentBounds targetSegment = ResolveSegment(day, roomId, targetSession.StartTime, targetSession.EndTime);
            if (sourceSegment.PreviousAnchorId != targetSegment.PreviousAnchorId
                || sourceSegment.NextAnchorId != targetSegment.NextAnchorId)
            {
                throw new InvalidOperationException(
                    "Ara açılış veya öğle arası sınırının dışına taşınamaz. Aynı zaman bölümündeki bir bildirinin arasına bırakın.");
            }

            List<CongressProgramSession> segmentSessions = day.Sessions
                .Where(x => x.EventRoomId == roomId
                            && x.StartTime >= sourceSegment.Start
                            && x.EndTime <= sourceSegment.End)
                .OrderBy(x => x.StartTime)
                .ThenBy(x => x.EndTime)
                .ThenBy(x => x.Id)
                .ToList();

            List<CongressProgramFixedBlock> segmentBreaks = day.FixedBlocks
                .Where(x => x.EventRoomId == roomId
                            && x.BlockType == CongressProgramFixedBlockType.Break
                            && x.StartTime >= sourceSegment.Start
                            && x.EndTime <= sourceSegment.End)
                .OrderBy(x => x.StartTime)
                .ThenBy(x => x.EndTime)
                .ThenBy(x => x.Id)
                .ToList();

            Dictionary<Guid, EmbeddedPlacement> currentPlacements = CaptureEmbeddedPlacements(
                segmentSessions,
                segmentBreaks);

            Dictionary<Guid, int> baseSessionDurations = segmentSessions.ToDictionary(
                session => session.Id,
                session =>
                {
                    int embeddedMinutes = currentPlacements.Values
                        .Where(x => x.SessionId == session.Id)
                        .Sum(x => x.DurationMinutes);
                    return MinutesBetween(session.StartTime, session.EndTime) - embeddedMinutes;
                });

            if (baseSessionDurations.Values.Any(x => x <= 0))
                throw new InvalidOperationException("Oturum süreleri ara taşıma işlemi için geçerli değil.");

            Dictionary<Guid, EmbeddedPlacement> nextPlacements = currentPlacements
                .Where(x => x.Key != movingBreak.Id)
                .ToDictionary(x => x.Key, x => x.Value);

            nextPlacements[movingBreak.Id] = new EmbeddedPlacement(
                movingBreak.Id,
                targetSession.Id,
                targetItemIndex,
                MinutesBetween(movingBreak.StartTime, movingBreak.EndTime));

            HashSet<Guid> embeddedBreakIds = nextPlacements.Keys.ToHashSet();
            List<TopLevelEntity> topLevelEntities = new();

            topLevelEntities.AddRange(segmentSessions.Select(session =>
                TopLevelEntity.ForSession(
                    session,
                    baseSessionDurations[session.Id]
                    + nextPlacements.Values.Where(x => x.SessionId == session.Id).Sum(x => x.DurationMinutes))));

            topLevelEntities.AddRange(segmentBreaks
                .Where(x => !embeddedBreakIds.Contains(x.Id))
                .Select(TopLevelEntity.ForBreak));

            topLevelEntities = topLevelEntities
                .OrderBy(x => x.OriginalStartTime)
                .ThenBy(x => x.OriginalEndTime)
                .ThenBy(x => x.SortOrder)
                .ThenBy(x => x.Id)
                .ToList();

            int segmentMinutes = MinutesBetween(sourceSegment.Start, sourceSegment.End);
            int contentMinutes = topLevelEntities.Sum(x => x.DurationMinutes);
            if (contentMinutes != segmentMinutes)
            {
                throw new InvalidOperationException(
                    $"Zaman bölümü toplamı tutarsız. Beklenen {segmentMinutes} dakika, bulunan {contentMinutes} dakika.");
            }

            DateTime now = DateTime.UtcNow;
            TimeOnly cursor = sourceSegment.Start;
            foreach (TopLevelEntity entity in topLevelEntities)
            {
                TimeOnly end = AddMinutes(cursor, entity.DurationMinutes);
                entity.ApplyTimes(cursor, end, now);
                cursor = end;
            }

            if (cursor != sourceSegment.End)
                throw new InvalidOperationException("Ara taşıma işlemi zaman bölümünün sınırlarıyla eşleşmedi.");

            ApplyEmbeddedBreakTimes(segmentSessions, segmentBreaks, nextPlacements, now);
        }

        private static Dictionary<Guid, EmbeddedPlacement> CaptureEmbeddedPlacements(
            IReadOnlyCollection<CongressProgramSession> sessions,
            IReadOnlyCollection<CongressProgramFixedBlock> breaks)
        {
            Dictionary<Guid, EmbeddedPlacement> result = new();

            foreach (CongressProgramSession session in sessions)
            {
                List<CongressProgramFixedBlock> embeddedBreaks = breaks
                    .Where(x => x.StartTime >= session.StartTime && x.EndTime <= session.EndTime)
                    .OrderBy(x => x.StartTime)
                    .ThenBy(x => x.Order)
                    .ThenBy(x => x.Id)
                    .ToList();

                List<CongressProgramItem> items = session.Items
                    .OrderBy(x => x.Order)
                    .ThenBy(x => x.Id)
                    .ToList();

                int precedingBreakMinutes = 0;
                foreach (CongressProgramFixedBlock breakBlock in embeddedBreaks)
                {
                    int elapsed = Math.Max(0, MinutesBetween(session.StartTime, breakBlock.StartTime));
                    int presentationMinutes = Math.Max(0, elapsed - precedingBreakMinutes);
                    int cumulative = 0;
                    int itemIndex = 0;

                    while (itemIndex < items.Count
                           && cumulative + items[itemIndex].DurationMinutes <= presentationMinutes)
                    {
                        cumulative += items[itemIndex].DurationMinutes;
                        itemIndex++;
                    }

                    int duration = MinutesBetween(breakBlock.StartTime, breakBlock.EndTime);
                    result[breakBlock.Id] = new EmbeddedPlacement(
                        breakBlock.Id,
                        session.Id,
                        itemIndex,
                        duration);
                    precedingBreakMinutes += duration;
                }
            }

            return result;
        }

        private static void ApplyEmbeddedBreakTimes(
            IReadOnlyCollection<CongressProgramSession> sessions,
            IReadOnlyCollection<CongressProgramFixedBlock> breaks,
            IReadOnlyDictionary<Guid, EmbeddedPlacement> placements,
            DateTime now)
        {
            Dictionary<Guid, CongressProgramFixedBlock> breaksById = breaks.ToDictionary(x => x.Id);

            foreach (CongressProgramSession session in sessions)
            {
                List<CongressProgramItem> items = session.Items
                    .OrderBy(x => x.Order)
                    .ThenBy(x => x.Id)
                    .ToList();

                Dictionary<int, List<EmbeddedPlacement>> placementsByIndex = placements.Values
                    .Where(x => x.SessionId == session.Id)
                    .GroupBy(x => x.ItemIndex)
                    .ToDictionary(
                        x => x.Key,
                        x => x.OrderBy(y => breaksById[y.BreakId].Order)
                            .ThenBy(y => y.BreakId)
                            .ToList());

                TimeOnly cursor = session.StartTime;
                for (int itemIndex = 0; itemIndex <= items.Count; itemIndex++)
                {
                    if (placementsByIndex.TryGetValue(itemIndex, out List<EmbeddedPlacement>? atIndex))
                    {
                        foreach (EmbeddedPlacement placement in atIndex)
                        {
                            CongressProgramFixedBlock breakBlock = breaksById[placement.BreakId];
                            TimeOnly end = AddMinutes(cursor, placement.DurationMinutes);
                            breakBlock.StartTime = cursor;
                            breakBlock.EndTime = end;
                            breakBlock.UpdatedDate = now;
                            cursor = end;
                        }
                    }

                    if (itemIndex < items.Count)
                        cursor = AddMinutes(cursor, items[itemIndex].DurationMinutes);
                }

                TimeOnly contentEnd = AddMinutes(cursor,
                    items.Count == 0 ? 0 : session.QuestionAnswerDurationMinutes);
                if (contentEnd > session.EndTime)
                {
                    throw new InvalidOperationException(
                        $"{session.Title} için bildiri, ara ve soru-cevap süreleri oturum sınırını aşıyor.");
                }
            }
        }

        private static SegmentBounds ResolveSegment(
            CongressProgramDay day,
            Guid roomId,
            TimeOnly start,
            TimeOnly end)
        {
            List<CongressProgramFixedBlock> anchors = day.FixedBlocks
                .Where(x => (!x.EventRoomId.HasValue || x.EventRoomId == roomId)
                            && x.BlockType != CongressProgramFixedBlockType.Break)
                .OrderBy(x => x.StartTime)
                .ThenBy(x => x.EndTime)
                .ToList();

            CongressProgramFixedBlock? previous = anchors
                .Where(x => x.EndTime <= start)
                .OrderByDescending(x => x.EndTime)
                .FirstOrDefault();
            CongressProgramFixedBlock? next = anchors
                .Where(x => x.StartTime >= end)
                .OrderBy(x => x.StartTime)
                .FirstOrDefault();

            return new SegmentBounds(
                previous?.EndTime ?? day.StartTime,
                next?.StartTime ?? day.EndTime,
                previous?.Id,
                next?.Id);
        }

        private static void MoveBreakOnRoomTimeline(
            CongressProgramDay day,
            Guid roomId,
            CongressProgramFixedBlock movingBreak,
            IReadOnlyCollection<string> requestedOrderedKeys)
        {
            List<CongressProgramFixedBlock> anchors = day.FixedBlocks
                .Where(x => (!x.EventRoomId.HasValue || x.EventRoomId == roomId)
                            && x.BlockType != CongressProgramFixedBlockType.Break)
                .OrderBy(x => x.StartTime)
                .ThenBy(x => x.EndTime)
                .ToList();

            CongressProgramFixedBlock? originalPreviousAnchor = anchors
                .Where(x => x.EndTime <= movingBreak.StartTime)
                .OrderByDescending(x => x.EndTime)
                .FirstOrDefault();
            CongressProgramFixedBlock? originalNextAnchor = anchors
                .Where(x => x.StartTime >= movingBreak.EndTime)
                .OrderBy(x => x.StartTime)
                .FirstOrDefault();

            Dictionary<string, TimelineEntity> entities = BuildTimelineEntities(day, roomId);
            List<string> orderedKeys = requestedOrderedKeys
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToList();

            if (orderedKeys.Count != orderedKeys.Distinct(StringComparer.OrdinalIgnoreCase).Count())
                throw new InvalidOperationException("Zaman çizelgesi sıralamasında tekrar eden kayıt bulunuyor.");
            if (orderedKeys.Any(x => !entities.ContainsKey(x)))
                throw new InvalidOperationException("Zaman çizelgesi sıralaması mevcut program kayıtlarıyla eşleşmiyor.");

            string breakKey = FixedKey(movingBreak.Id);
            int breakIndex = orderedKeys.FindIndex(x => string.Equals(x, breakKey, StringComparison.OrdinalIgnoreCase));
            if (breakIndex < 0)
                throw new InvalidOperationException("Taşınan ara bloğu sıralama listesinde bulunamadı.");

            CongressProgramFixedBlock? newPreviousAnchor = FindNearestAnchorBefore(orderedKeys, breakIndex, entities);
            CongressProgramFixedBlock? newNextAnchor = FindNearestAnchorAfter(orderedKeys, breakIndex, entities);
            if (newPreviousAnchor?.Id != originalPreviousAnchor?.Id || newNextAnchor?.Id != originalNextAnchor?.Id)
            {
                throw new InvalidOperationException(
                    "Ara bloğu açılış veya öğle arası sınırının dışına taşınamaz. Aynı zaman bölümünde farklı bir konuma bırakın.");
            }

            TimeOnly segmentStart = originalPreviousAnchor?.EndTime ?? day.StartTime;
            TimeOnly segmentEnd = originalNextAnchor?.StartTime ?? day.EndTime;

            List<TimelineEntity> segmentEntities = entities.Values
                .Where(x => x.IsMovableTimelineEntity)
                .Where(x => x.StartTime >= segmentStart && x.EndTime <= segmentEnd)
                .ToList();

            HashSet<string> expectedKeys = segmentEntities
                .Select(x => x.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            List<string> orderedSegmentKeys = orderedKeys
                .Where(expectedKeys.Contains)
                .ToList();

            if (orderedSegmentKeys.Count != expectedKeys.Count || orderedSegmentKeys.Any(x => !expectedKeys.Contains(x)))
                throw new InvalidOperationException("Ara bloğunun bulunduğu zaman bölümünün sıralaması eksik veya geçersiz.");

            int segmentMinutes = MinutesBetween(segmentStart, segmentEnd);
            int contentMinutes = orderedSegmentKeys.Sum(key => entities[key].DurationMinutes);
            if (contentMinutes != segmentMinutes)
            {
                throw new InvalidOperationException(
                    $"Zaman bölümü toplamı tutarsız. Beklenen {segmentMinutes} dakika, bulunan {contentMinutes} dakika.");
            }

            TimeOnly cursor = segmentStart;
            DateTime now = DateTime.UtcNow;
            foreach (string key in orderedSegmentKeys)
            {
                TimelineEntity entity = entities[key];
                TimeOnly end = AddMinutes(cursor, entity.DurationMinutes);
                entity.ApplyTimes(cursor, end, now);
                cursor = end;
            }

            if (cursor != segmentEnd)
                throw new InvalidOperationException("Ara taşıma işlemi zaman bölümünün sınırlarıyla eşleşmedi.");
        }

        private static Dictionary<string, TimelineEntity> BuildTimelineEntities(
            CongressProgramDay day,
            Guid roomId)
        {
            Dictionary<string, TimelineEntity> result = new(StringComparer.OrdinalIgnoreCase);
            List<CongressProgramSession> roomSessions = day.Sessions
                .Where(x => x.EventRoomId == roomId)
                .ToList();

            foreach (CongressProgramSession session in roomSessions)
            {
                TimelineEntity entity = TimelineEntity.FromSession(session);
                result.Add(entity.Key, entity);
            }

            HashSet<Guid> embeddedBreakIds = day.FixedBlocks
                .Where(x => x.EventRoomId == roomId
                            && x.BlockType == CongressProgramFixedBlockType.Break
                            && roomSessions.Any(session =>
                                x.StartTime >= session.StartTime && x.EndTime <= session.EndTime))
                .Select(x => x.Id)
                .ToHashSet();

            foreach (CongressProgramFixedBlock block in day.FixedBlocks
                         .Where(x => (!x.EventRoomId.HasValue || x.EventRoomId == roomId)
                                     && !embeddedBreakIds.Contains(x.Id)))
            {
                TimelineEntity entity = TimelineEntity.FromFixedBlock(block);
                result.Add(entity.Key, entity);
            }

            return result;
        }

        private static CongressProgramFixedBlock? FindNearestAnchorBefore(
            IReadOnlyList<string> keys,
            int startIndex,
            IReadOnlyDictionary<string, TimelineEntity> entities)
        {
            for (int index = startIndex - 1; index >= 0; index--)
            {
                CongressProgramFixedBlock? block = entities[keys[index]].FixedBlock;
                if (block is not null && block.BlockType != CongressProgramFixedBlockType.Break)
                    return block;
            }

            return null;
        }

        private static CongressProgramFixedBlock? FindNearestAnchorAfter(
            IReadOnlyList<string> keys,
            int startIndex,
            IReadOnlyDictionary<string, TimelineEntity> entities)
        {
            for (int index = startIndex + 1; index < keys.Count; index++)
            {
                CongressProgramFixedBlock? block = entities[keys[index]].FixedBlock;
                if (block is not null && block.BlockType != CongressProgramFixedBlockType.Break)
                    return block;
            }

            return null;
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
                session.Title = $"{session.Order}. Oturum";
                session.UpdatedDate = now;
            }

            int fixedOrder = 1;
            foreach (CongressProgramFixedBlock block in day.FixedBlocks
                         .Where(x => !x.EventRoomId.HasValue || x.EventRoomId == roomId)
                         .OrderBy(x => x.StartTime)
                         .ThenBy(x => x.EndTime)
                         .ThenBy(x => x.Id))
            {
                block.Order = fixedOrder++;
                block.UpdatedDate = now;
            }

            day.UpdatedDate = now;
        }

        private static string SessionKey(Guid id) => $"session:{id:D}";
        private static string FixedKey(Guid id) => $"fixed:{id:D}";

        private static int MinutesBetween(TimeOnly start, TimeOnly end)
            => (int)(end.ToTimeSpan() - start.ToTimeSpan()).TotalMinutes;

        private static TimeOnly AddMinutes(TimeOnly value, int minutes)
            => TimeOnly.FromTimeSpan(value.ToTimeSpan().Add(TimeSpan.FromMinutes(minutes)));

        private sealed record SegmentBounds(
            TimeOnly Start,
            TimeOnly End,
            Guid? PreviousAnchorId,
            Guid? NextAnchorId);

        private sealed record EmbeddedPlacement(
            Guid BreakId,
            Guid SessionId,
            int ItemIndex,
            int DurationMinutes);

        private sealed class TopLevelEntity
        {
            private TopLevelEntity(
                Guid id,
                CongressProgramSession? session,
                CongressProgramFixedBlock? breakBlock,
                int durationMinutes)
            {
                Id = id;
                Session = session;
                BreakBlock = breakBlock;
                DurationMinutes = durationMinutes;
                OriginalStartTime = session?.StartTime ?? breakBlock!.StartTime;
                OriginalEndTime = session?.EndTime ?? breakBlock!.EndTime;
            }

            public Guid Id { get; }
            public CongressProgramSession? Session { get; }
            public CongressProgramFixedBlock? BreakBlock { get; }
            public int DurationMinutes { get; }
            public TimeOnly OriginalStartTime { get; }
            public TimeOnly OriginalEndTime { get; }
            public int SortOrder => Session is not null ? 0 : 1;

            public static TopLevelEntity ForSession(CongressProgramSession session, int durationMinutes)
                => new(session.Id, session, null, durationMinutes);

            public static TopLevelEntity ForBreak(CongressProgramFixedBlock breakBlock)
                => new(
                    breakBlock.Id,
                    null,
                    breakBlock,
                    MinutesBetween(breakBlock.StartTime, breakBlock.EndTime));

            public void ApplyTimes(TimeOnly start, TimeOnly end, DateTime now)
            {
                if (Session is not null)
                {
                    Session.StartTime = start;
                    Session.EndTime = end;
                    Session.UpdatedDate = now;
                    return;
                }

                BreakBlock!.StartTime = start;
                BreakBlock.EndTime = end;
                BreakBlock.UpdatedDate = now;
            }
        }

        private sealed class TimelineEntity
        {
            private TimelineEntity(
                string key,
                CongressProgramSession? session,
                CongressProgramFixedBlock? fixedBlock)
            {
                Key = key;
                Session = session;
                FixedBlock = fixedBlock;
            }

            public string Key { get; }
            public CongressProgramSession? Session { get; }
            public CongressProgramFixedBlock? FixedBlock { get; }
            public TimeOnly StartTime => Session?.StartTime ?? FixedBlock!.StartTime;
            public TimeOnly EndTime => Session?.EndTime ?? FixedBlock!.EndTime;
            public int DurationMinutes => MinutesBetween(StartTime, EndTime);
            public bool IsMovableTimelineEntity => Session is not null
                                                   || FixedBlock?.BlockType == CongressProgramFixedBlockType.Break;

            public static TimelineEntity FromSession(CongressProgramSession session)
                => new(SessionKey(session.Id), session, null);

            public static TimelineEntity FromFixedBlock(CongressProgramFixedBlock block)
                => new(FixedKey(block.Id), null, block);

            public void ApplyTimes(TimeOnly start, TimeOnly end, DateTime now)
            {
                if (Session is not null)
                {
                    Session.StartTime = start;
                    Session.EndTime = end;
                    Session.UpdatedDate = now;
                    return;
                }

                FixedBlock!.StartTime = start;
                FixedBlock.EndTime = end;
                FixedBlock.UpdatedDate = now;
            }
        }
    }
}

public sealed class ReorderProgramBreakCommandValidator : AbstractValidator<ReorderProgramBreakCommand>
{
    public ReorderProgramBreakCommandValidator()
    {
        RuleFor(x => x.CongressId).NotEmpty();
        RuleFor(x => x.ProgramDayId).NotEmpty();
        RuleFor(x => x.EventRoomId).NotEmpty();
        RuleFor(x => x.BreakId).NotEmpty();

        When(x => x.TargetSessionId.HasValue, () =>
        {
            RuleFor(x => x.TargetSessionId).NotEmpty();
            RuleFor(x => x.TargetItemIndex)
                .Must(x => x.HasValue && x.Value >= 0)
                .WithMessage("Hedef bildiri konumu geçerli olmalıdır.");
        });

        When(x => !x.TargetSessionId.HasValue, () =>
        {
            RuleFor(x => x.OrderedBlockKeys).NotEmpty();
        });
    }
}
