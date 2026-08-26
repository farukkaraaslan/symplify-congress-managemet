using Symplify.BackOffice.Application.Features.ProgramManagement.Models;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.Application.Features.ProgramManagement.Services;

public sealed class CongressProgramScheduler : ICongressProgramScheduler
{
    private const int MaximumProgramDayCount = 14;

    public (CongressProgramPlan Plan, ProgramGenerationResult Result) CreatePlan(
        ProgramGenerationSourceDto source,
        ProgramGenerationSettings settings)
    {
        ValidateSourceAndSettings(source, settings);

        DateTime now = DateTime.UtcNow;
        string? actor = settings.PerformedByUserId?.ToString("D");
        DateOnly firstDate = DateOnly.FromDateTime(source.StartDate!.Value.Date);
        DateOnly lastDate = DateOnly.FromDateTime(source.EndDate!.Value.Date);
        int dayCount = lastDate.DayNumber - firstDate.DayNumber + 1;
        if (dayCount > MaximumProgramDayCount)
            throw new InvalidOperationException($"Program en fazla {MaximumProgramDayCount} gün için otomatik oluşturulabilir.");

        CongressProgramPlan plan = new()
        {
            Id = Guid.NewGuid(),
            CongressId = source.CongressId,
            Name = $"{source.CongressName} Program Taslağı",
            Status = CongressProgramPlanStatus.Draft,
            VersionNo = 1,
            DefaultPresentationDurationMinutes = settings.PresentationDurationMinutes,
            DefaultSessionDurationMinutes = settings.SessionDurationMinutes,
            DefaultQuestionAnswerDurationMinutes = settings.QuestionAnswerDurationMinutes,
            DefaultBreakDurationMinutes = settings.BreakDurationMinutes,
            LastGeneratedAt = now,
            LastGeneratedByUserId = settings.PerformedByUserId,
            CreatedDate = now,
            CreatedBy = actor
        };

        List<CongressProgramSession> sessions = new();
        for (int index = 0; index < dayCount; index++)
        {
            CongressProgramDay day = new()
            {
                Id = Guid.NewGuid(),
                ProgramPlanId = plan.Id,
                Date = firstDate.AddDays(index),
                StartTime = settings.DayStartTime,
                EndTime = settings.DayEndTime,
                Order = index + 1,
                CreatedDate = now,
                CreatedBy = actor,
                ProgramPlan = plan
            };
            plan.Days.Add(day);

            AddConfiguredFixedBlocks(day, settings, index == 0, now, actor);
            CreateSessionsForDay(day, source.Rooms, settings, sessions, now, actor);
        }

        IReadOnlyDictionary<Guid, int> roomOrderById = source.Rooms
            .ToDictionary(x => x.Id, x => x.Order);

        int assigned = AssignSubmissions(
            sessions,
            source.Submissions,
            source.Submissions,
            settings.PresentationDurationMinutes,
            new HashSet<Guid>(),
            roomOrderById,
            now,
            actor);

        return (plan, new ProgramGenerationResult
        {
            EligibleSubmissionCount = source.Submissions.Count,
            AssignedSubmissionCount = assigned,
            UnassignedSubmissionCount = source.Submissions.Count - assigned,
            SessionCount = sessions.Count,
            DayCount = plan.Days.Count
        });
    }

    public ProgramGenerationResult FillUnassigned(
        CongressProgramPlan plan,
        ProgramGenerationSourceDto source,
        ProgramGenerationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ValidateSourceAndSettings(source, settings, validateDates: false);

        HashSet<Guid> assignedIds = plan.Days
            .SelectMany(x => x.Sessions)
            .SelectMany(x => x.Items)
            .Select(x => x.SubmissionId)
            .ToHashSet();

        List<ProgramSubmissionSourceDto> unassigned = source.Submissions
            .Where(x => !assignedIds.Contains(x.Id))
            .ToList();

        List<CongressProgramSession> sessions = plan.Days
            .OrderBy(x => x.Order)
            .SelectMany(x => x.Sessions.OrderBy(s => s.StartTime).ThenBy(s => s.Order))
            .ToList();

        DateTime now = DateTime.UtcNow;
        string? actor = settings.PerformedByUserId?.ToString("D");
        IReadOnlyDictionary<Guid, int> roomOrderById = source.Rooms
            .ToDictionary(x => x.Id, x => x.Order);

        _ = AssignSubmissions(
            sessions,
            unassigned,
            source.Submissions,
            plan.DefaultPresentationDurationMinutes,
            assignedIds,
            roomOrderById,
            now,
            actor);

        plan.LastGeneratedAt = now;
        plan.LastGeneratedByUserId = settings.PerformedByUserId;
        plan.UpdatedDate = now;
        plan.UpdatedBy = actor;

        return new ProgramGenerationResult
        {
            EligibleSubmissionCount = source.Submissions.Count,
            AssignedSubmissionCount = assignedIds.Count,
            UnassignedSubmissionCount = Math.Max(0, source.Submissions.Count - assignedIds.Count),
            SessionCount = sessions.Count,
            DayCount = plan.Days.Count
        };
    }

    private static void ValidateSourceAndSettings(
        ProgramGenerationSourceDto source,
        ProgramGenerationSettings settings,
        bool validateDates = true)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(settings);

        if (validateDates && (!source.StartDate.HasValue || !source.EndDate.HasValue))
            throw new InvalidOperationException("Kongre başlangıç ve bitiş tarihleri tanımlanmadan program oluşturulamaz.");
        if (validateDates && source.EndDate!.Value.Date < source.StartDate!.Value.Date)
            throw new InvalidOperationException("Kongre bitiş tarihi başlangıç tarihinden önce olamaz.");
        if (source.Rooms.Count == 0)
            throw new InvalidOperationException("Program oluşturmak için en az bir etkinlik salonu seçilmelidir.");
        if (settings.OpeningRoomId.HasValue && source.Rooms.All(x => x.Id != settings.OpeningRoomId.Value))
            throw new InvalidOperationException("Açılış salonu programa dahil edilen salonlardan biri olmalıdır.");
        if (settings.DayEndTime <= settings.DayStartTime)
            throw new InvalidOperationException("Gün bitiş saati başlangıç saatinden sonra olmalıdır.");
        if (settings.SessionDurationMinutes is < 30 or > 360)
            throw new InvalidOperationException("Oturum süresi 30 ile 360 dakika arasında olmalıdır.");
        if (settings.PresentationDurationMinutes is < 5 or > 120)
            throw new InvalidOperationException("Bildiri süresi 5 ile 120 dakika arasında olmalıdır.");
        if (settings.QuestionAnswerDurationMinutes < 0 || settings.QuestionAnswerDurationMinutes >= settings.SessionDurationMinutes)
            throw new InvalidOperationException("Soru-cevap süresi oturum süresinden kısa olmalıdır.");
        if (settings.PresentationDurationMinutes + settings.QuestionAnswerDurationMinutes > settings.SessionDurationMinutes)
            throw new InvalidOperationException("Oturum süresi, en az bir bildiri ve soru-cevap süresini karşılamalıdır.");
        if (settings.BreakDurationMinutes < 0 || settings.BreakDurationMinutes > 180)
            throw new InvalidOperationException("Oturum arası süresi 0 ile 180 dakika arasında olmalıdır.");
        if (settings.IncludeSessionBreaks && settings.SessionBreakDurationMinutes <= 0)
            throw new InvalidOperationException("Oturum içi ara etkinse süresi sıfırdan büyük olmalıdır.");
        if (settings.SessionBreakDurationMinutes < 0 || settings.SessionBreakDurationMinutes > 60)
            throw new InvalidOperationException("Oturum içi ara süresi 0 ile 60 dakika arasında olmalıdır.");

        int totalDayMinutes = MinutesBetween(settings.DayStartTime, settings.DayEndTime);
        if (totalDayMinutes < MinimumSessionMinutes(settings))
            throw new InvalidOperationException("Günlük çalışma aralığı en az bir oturum oluşturmak için yetersiz.");

        TimeOnly? openingEnd = null;
        if (settings.IncludeOpening)
        {
            if (settings.OpeningDurationMinutes <= 0)
                throw new InvalidOperationException("Açılış bloğu etkinse süresi sıfırdan büyük olmalıdır.");

            openingEnd = AddMinutes(settings.DayStartTime, settings.OpeningDurationMinutes);
            if (openingEnd > settings.DayEndTime)
                throw new InvalidOperationException("Açılış bloğu günlük çalışma saatlerinin dışına taşıyor.");
        }

        if (settings.IncludeLunch)
        {
            if (settings.LunchDurationMinutes <= 0)
                throw new InvalidOperationException("Öğle arası etkinse süresi sıfırdan büyük olmalıdır.");

            TimeOnly lunchEnd = AddMinutes(settings.LunchStartTime, settings.LunchDurationMinutes);
            if (settings.LunchStartTime < settings.DayStartTime || lunchEnd > settings.DayEndTime)
                throw new InvalidOperationException("Öğle arası günlük çalışma saatlerinin içinde olmalıdır.");

            if (openingEnd.HasValue
                && Overlaps(settings.DayStartTime, openingEnd.Value, settings.LunchStartTime, lunchEnd))
            {
                throw new InvalidOperationException("Açılış bloğu ile öğle arası çakışamaz.");
            }
        }
    }

    private static void AddConfiguredFixedBlocks(
        CongressProgramDay day,
        ProgramGenerationSettings settings,
        bool isFirstDay,
        DateTime now,
        string? actor)
    {
        int order = 1;
        if (isFirstDay && settings.IncludeOpening)
        {
            TimeOnly openingEnd = AddMinutes(settings.DayStartTime, settings.OpeningDurationMinutes);
            day.FixedBlocks.Add(new CongressProgramFixedBlock
            {
                Id = Guid.NewGuid(),
                ProgramDayId = day.Id,
                EventRoomId = settings.OpeningRoomId,
                BlockType = CongressProgramFixedBlockType.Opening,
                Title = string.IsNullOrWhiteSpace(settings.OpeningTitle) ? "Açılış Konuşması" : settings.OpeningTitle.Trim(),
                StartTime = settings.DayStartTime,
                EndTime = openingEnd,
                Order = order++,
                IsLocked = true,
                ProgramDay = day,
                CreatedDate = now,
                CreatedBy = actor
            });
        }

        if (settings.IncludeLunch)
        {
            TimeOnly lunchEnd = AddMinutes(settings.LunchStartTime, settings.LunchDurationMinutes);
            day.FixedBlocks.Add(new CongressProgramFixedBlock
            {
                Id = Guid.NewGuid(),
                ProgramDayId = day.Id,
                EventRoomId = null,
                BlockType = CongressProgramFixedBlockType.Lunch,
                Title = string.IsNullOrWhiteSpace(settings.LunchTitle) ? "Öğle Arası" : settings.LunchTitle.Trim(),
                StartTime = settings.LunchStartTime,
                EndTime = lunchEnd,
                Order = order,
                IsLocked = true,
                ProgramDay = day,
                CreatedDate = now,
                CreatedBy = actor
            });
        }
    }

    private static void CreateSessionsForDay(
        CongressProgramDay day,
        IReadOnlyList<ProgramRoomOptionDto> rooms,
        ProgramGenerationSettings settings,
        ICollection<CongressProgramSession> allSessions,
        DateTime now,
        string? actor)
    {
        foreach (ProgramRoomOptionDto room in rooms.OrderBy(x => x.Order).ThenBy(x => x.Name))
        {
            List<(TimeOnly Start, TimeOnly End)> windows = BuildAvailabilityWindows(
                day.StartTime,
                day.EndTime,
                day.FixedBlocks.Where(x => !x.EventRoomId.HasValue || x.EventRoomId == room.Id));

            int sessionOrder = 1;
            int blockOrder = day.FixedBlocks.Count + 1;
            foreach ((TimeOnly windowStart, TimeOnly windowEnd) in windows)
            {
                FillAvailabilityWindow(
                    day,
                    room,
                    windowStart,
                    windowEnd,
                    settings,
                    allSessions,
                    ref sessionOrder,
                    ref blockOrder,
                    now,
                    actor);
            }
        }
    }

    private static void FillAvailabilityWindow(
        CongressProgramDay day,
        ProgramRoomOptionDto room,
        TimeOnly windowStart,
        TimeOnly windowEnd,
        ProgramGenerationSettings settings,
        ICollection<CongressProgramSession> allSessions,
        ref int sessionOrder,
        ref int blockOrder,
        DateTime now,
        string? actor)
    {
        TimeOnly cursor = windowStart;
        int minimumSessionMinutes = MinimumSessionMinutes(settings);

        while (cursor < windowEnd)
        {
            int remaining = MinutesBetween(cursor, windowEnd);
            if (remaining < minimumSessionMinutes)
            {
                AddBreak(day, room.Id, cursor, windowEnd, ref blockOrder, now, actor);
                break;
            }

            int sessionMinutes = Math.Min(settings.SessionDurationMinutes, remaining);
            TimeOnly sessionEnd = AddMinutes(cursor, sessionMinutes);
            CongressProgramSession session = new()
            {
                Id = Guid.NewGuid(),
                ProgramDayId = day.Id,
                EventRoomId = room.Id,
                Title = $"{sessionOrder}. Oturum",
                StartTime = cursor,
                EndTime = sessionEnd,
                QuestionAnswerDurationMinutes = settings.QuestionAnswerDurationMinutes,
                Order = sessionOrder,
                IsLocked = false,
                ProgramDay = day,
                CreatedDate = now,
                CreatedBy = actor
            };
            day.Sessions.Add(session);
            allSessions.Add(session);
            AddSessionBreakIfEnabled(day, room.Id, session, settings, ref blockOrder, now, actor);
            sessionOrder++;
            cursor = sessionEnd;

            remaining = MinutesBetween(cursor, windowEnd);
            if (remaining <= 0)
                break;

            if (settings.BreakDurationMinutes <= 0)
            {
                if (remaining < minimumSessionMinutes)
                {
                    session.EndTime = windowEnd;
                    cursor = windowEnd;
                }
                continue;
            }

            int breakMinutes = Math.Min(settings.BreakDurationMinutes, remaining);
            int remainingAfterBreak = remaining - breakMinutes;
            if (remainingAfterBreak > 0 && remainingAfterBreak < minimumSessionMinutes)
                breakMinutes = remaining;

            TimeOnly breakEnd = AddMinutes(cursor, breakMinutes);
            AddBreak(day, room.Id, cursor, breakEnd, ref blockOrder, now, actor);
            cursor = breakEnd;
        }
    }

    private static void AddSessionBreakIfEnabled(
        CongressProgramDay day,
        Guid roomId,
        CongressProgramSession session,
        ProgramGenerationSettings settings,
        ref int order,
        DateTime now,
        string? actor)
    {
        if (!settings.IncludeSessionBreaks || settings.SessionBreakDurationMinutes <= 0)
            return;

        int sessionMinutes = MinutesBetween(session.StartTime, session.EndTime);
        int availableBeforeQuestionAnswer = sessionMinutes - session.QuestionAnswerDurationMinutes;
        if (availableBeforeQuestionAnswer <= settings.SessionBreakDurationMinutes + settings.PresentationDurationMinutes)
            return;

        int minutesBeforeBreak = Math.Max(
            settings.PresentationDurationMinutes,
            (availableBeforeQuestionAnswer - settings.SessionBreakDurationMinutes) / 2);
        TimeOnly breakStart = AddMinutes(session.StartTime, minutesBeforeBreak);
        TimeOnly breakEnd = AddMinutes(breakStart, settings.SessionBreakDurationMinutes);

        if (breakEnd >= session.EndTime)
            return;

        AddBreak(day, roomId, breakStart, breakEnd, ref order, now, actor);
    }

    private static void AddBreak(
        CongressProgramDay day,
        Guid roomId,
        TimeOnly start,
        TimeOnly end,
        ref int order,
        DateTime now,
        string? actor)
    {
        if (end <= start)
            return;

        day.FixedBlocks.Add(new CongressProgramFixedBlock
        {
            Id = Guid.NewGuid(),
            ProgramDayId = day.Id,
            EventRoomId = roomId,
            BlockType = CongressProgramFixedBlockType.Break,
            Title = "Ara",
            StartTime = start,
            EndTime = end,
            Order = order++,
            IsLocked = false,
            ProgramDay = day,
            CreatedDate = now,
            CreatedBy = actor
        });
    }

    private static List<(TimeOnly Start, TimeOnly End)> BuildAvailabilityWindows(
        TimeOnly start,
        TimeOnly end,
        IEnumerable<CongressProgramFixedBlock> blocks)
    {
        List<(TimeOnly Start, TimeOnly End)> windows = new() { (start, end) };
        foreach (CongressProgramFixedBlock block in blocks.OrderBy(x => x.StartTime).ThenBy(x => x.EndTime))
        {
            List<(TimeOnly Start, TimeOnly End)> next = new();
            foreach ((TimeOnly windowStart, TimeOnly windowEnd) in windows)
            {
                if (block.EndTime <= windowStart || block.StartTime >= windowEnd)
                {
                    next.Add((windowStart, windowEnd));
                    continue;
                }

                if (block.StartTime > windowStart)
                    next.Add((windowStart, block.StartTime));
                if (block.EndTime < windowEnd)
                    next.Add((block.EndTime, windowEnd));
            }
            windows = next;
        }
        return windows;
    }

    private static int AssignSubmissions(
        IReadOnlyCollection<CongressProgramSession> sessions,
        IReadOnlyCollection<ProgramSubmissionSourceDto> submissions,
        IReadOnlyCollection<ProgramSubmissionSourceDto> allSubmissions,
        int defaultDurationMinutes,
        ISet<Guid> alreadyAssignedIds,
        IReadOnlyDictionary<Guid, int> roomOrderById,
        DateTime now,
        string? actor)
    {
        if (sessions.Count == 0 || submissions.Count == 0)
            return 0;

        Dictionary<Guid, ProgramSubmissionSourceDto> sourceById = allSubmissions.ToDictionary(x => x.Id);
        List<ProgramSubmissionSourceDto> orderedSubmissions = submissions
            .GroupBy(x => x.TopicId)
            .OrderByDescending(x => x.Count())
            .ThenBy(x => x.Key)
            .SelectMany(x => x.OrderBy(s => s.SubmissionNumber))
            .ToList();

        int assigned = 0;
        foreach (ProgramSubmissionSourceDto submission in orderedSubmissions)
        {
            if (alreadyAssignedIds.Contains(submission.Id))
                continue;

            CongressProgramSession? bestSession = sessions
                .Where(session => RemainingMinutes(session) >= defaultDurationMinutes)
                .Where(session => !HasAuthorConflict(session, submission, sessions, sourceById))
                .Select(session => new
                {
                    Session = session,
                    HasSameTopic = ContainsTopic(session, submission.TopicId, sourceById),
                    HasItems = session.Items.Count > 0,
                    RemainingAfterAssignment = RemainingMinutes(session) - defaultDurationMinutes
                })
                // Önce kronolojik olarak en erken gün ve saat doldurulur.
                // Aynı saat diliminde aynı konu devam ediyorsa o oturum tercih edilir;
                // aksi halde dolmakta olan oturum tamamlanır. Böylece 4. oturum gibi
                // daha kısa/geç slotlar sırf boşluğu az diye ilk sırada seçilmez.
                .OrderBy(x => x.Session.ProgramDay.Order)
                .ThenBy(x => x.Session.StartTime)
                .ThenByDescending(x => x.HasSameTopic)
                .ThenByDescending(x => x.HasItems)
                .ThenBy(x => x.RemainingAfterAssignment)
                .ThenBy(x => roomOrderById.TryGetValue(x.Session.EventRoomId, out int roomOrder)
                    ? roomOrder
                    : int.MaxValue)
                .ThenBy(x => x.Session.Order)
                .ThenBy(x => x.Session.Id)
                .Select(x => x.Session)
                .FirstOrDefault();

            if (bestSession is null)
                continue;

            bestSession.Items.Add(new CongressProgramItem
            {
                Id = Guid.NewGuid(),
                ProgramSessionId = bestSession.Id,
                SubmissionId = submission.Id,
                DurationMinutes = defaultDurationMinutes,
                Order = bestSession.Items.Count + 1,
                IsLocked = false,
                Source = CongressProgramItemSource.Automatic,
                ProgramSession = bestSession,
                CreatedDate = now,
                CreatedBy = actor
            });
            alreadyAssignedIds.Add(submission.Id);
            assigned++;
        }

        return assigned;
    }

    private static bool ContainsTopic(
        CongressProgramSession session,
        Guid? topicId,
        IReadOnlyDictionary<Guid, ProgramSubmissionSourceDto> sourceById)
    {
        if (!topicId.HasValue)
            return false;

        return session.Items.Any(item =>
            sourceById.TryGetValue(item.SubmissionId, out ProgramSubmissionSourceDto? source)
            && source.TopicId == topicId);
    }

    private static bool HasAuthorConflict(
        CongressProgramSession target,
        ProgramSubmissionSourceDto submission,
        IReadOnlyCollection<CongressProgramSession> sessions,
        IReadOnlyDictionary<Guid, ProgramSubmissionSourceDto> sourceById)
    {
        if (submission.AuthorKeys.Count == 0)
            return false;

        HashSet<string> authorKeys = submission.AuthorKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (CongressProgramSession other in sessions)
        {
            if (other.Id == target.Id || other.ProgramDayId != target.ProgramDayId)
                continue;
            if (!Overlaps(target.StartTime, target.EndTime, other.StartTime, other.EndTime))
                continue;

            foreach (CongressProgramItem item in other.Items)
            {
                if (!sourceById.TryGetValue(item.SubmissionId, out ProgramSubmissionSourceDto? source))
                    continue;
                if (source.AuthorKeys.Any(authorKeys.Contains))
                    return true;
            }
        }

        return false;
    }

    private static int RemainingMinutes(CongressProgramSession session)
    {
        int total = MinutesBetween(session.StartTime, session.EndTime);
        int embeddedBreakMinutes = session.ProgramDay?.FixedBlocks
            .Where(x => x.EventRoomId == session.EventRoomId
                        && x.BlockType == CongressProgramFixedBlockType.Break
                        && x.StartTime >= session.StartTime
                        && x.EndTime <= session.EndTime)
            .Sum(x => MinutesBetween(x.StartTime, x.EndTime))
            ?? 0;
        int available = Math.Max(0, total - session.QuestionAnswerDurationMinutes - embeddedBreakMinutes);
        return available - session.Items.Sum(x => x.DurationMinutes);
    }

    private static int MinimumSessionMinutes(ProgramGenerationSettings settings)
        => settings.QuestionAnswerDurationMinutes
           + settings.PresentationDurationMinutes
           + (settings.IncludeSessionBreaks ? settings.SessionBreakDurationMinutes : 0);

    private static bool Overlaps(TimeOnly start1, TimeOnly end1, TimeOnly start2, TimeOnly end2)
        => start1 < end2 && start2 < end1;

    private static int MinutesBetween(TimeOnly start, TimeOnly end)
        => (int)(end.ToTimeSpan() - start.ToTimeSpan()).TotalMinutes;

    private static TimeOnly AddMinutes(TimeOnly time, int minutes)
        => TimeOnly.FromTimeSpan(time.ToTimeSpan().Add(TimeSpan.FromMinutes(minutes)));
}
