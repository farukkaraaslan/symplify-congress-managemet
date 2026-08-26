using System.Text.Json;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.Application.Features.ProgramManagement.Models;

public static class ProgramPlanMapper
{
    public static ProgramPlanDto Map(CongressProgramPlan plan, ProgramGenerationSourceDto source)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(source);

        Dictionary<Guid, ProgramRoomOptionDto> rooms = source.Rooms.ToDictionary(x => x.Id);
        Dictionary<Guid, ProgramSubmissionSourceDto> submissions = source.Submissions.ToDictionary(x => x.Id);
        Dictionary<Guid, ProgramAuthorOptionDto> authors = source.AuthorOptions.ToDictionary(x => x.Id);
        Dictionary<Guid, ProgramBoardMemberOptionDto> boardMembers = source.BoardMemberOptions.ToDictionary(x => x.Id);
        HashSet<Guid> videoSubmissionIds = source.VideoPresentations
            .Select(x => x.SubmissionId)
            .ToHashSet();
        List<CongressProgramItem> assignedItems = plan.Days
            .SelectMany(x => x.Sessions)
            .SelectMany(x => x.Items)
            .Where(x => !videoSubmissionIds.Contains(x.SubmissionId))
            .ToList();
        HashSet<Guid> assignedIds = assignedItems
            .Select(x => x.SubmissionId)
            .ToHashSet();
        // Participant index follows the programme's saved submission filters.
        // Presentation file type/review state must not decide whether an author is listed.
        HashSet<string> participantAuthorKeys = source.FilteredSubmissions
            .SelectMany(submission => submission.AuthorKeys)
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        List<ProgramDayDto> days = new();
        foreach (CongressProgramDay day in plan.Days.OrderBy(x => x.Order).ThenBy(x => x.Date))
        {
            List<Guid> roomIds = day.Sessions.Select(x => x.EventRoomId)
                .Concat(day.FixedBlocks.Where(x => x.EventRoomId.HasValue).Select(x => x.EventRoomId!.Value))
                .Distinct()
                .OrderBy(x => rooms.TryGetValue(x, out ProgramRoomOptionDto? room) ? room.Order : int.MaxValue)
                .ThenBy(x => x)
                .ToList();

            List<ProgramRoomScheduleDto> roomSchedules = new();
            foreach (Guid roomId in roomIds)
            {
                string roomName = rooms.TryGetValue(roomId, out ProgramRoomOptionDto? room)
                    ? room.Name
                    : day.Sessions.FirstOrDefault(x => x.EventRoomId == roomId)?.EventRoom?.Code ?? "Salon";
                int roomOrder = room?.Order ?? 0;

                List<CongressProgramSession> roomSessions = day.Sessions
                    .Where(x => x.EventRoomId == roomId)
                    .OrderBy(x => x.StartTime)
                    .ThenBy(x => x.EndTime)
                    .ThenBy(x => x.Order)
                    .ToList();

                List<CongressProgramFixedBlock> roomFixedBlocks = day.FixedBlocks
                    .Where(x => !x.EventRoomId.HasValue || x.EventRoomId == roomId)
                    .OrderBy(x => x.StartTime)
                    .ThenBy(x => x.EndTime)
                    .ThenBy(x => x.Order)
                    .ToList();

                Dictionary<Guid, List<CongressProgramFixedBlock>> embeddedBreaksBySession = roomSessions
                    .ToDictionary(x => x.Id, _ => new List<CongressProgramFixedBlock>());
                HashSet<Guid> embeddedBreakIds = new();

                foreach (CongressProgramFixedBlock breakBlock in roomFixedBlocks
                             .Where(x => x.BlockType == CongressProgramFixedBlockType.Break
                                         && x.EventRoomId == roomId))
                {
                    CongressProgramSession? hostSession = roomSessions
                        .Where(x => breakBlock.StartTime >= x.StartTime
                                    && breakBlock.EndTime <= x.EndTime
                                    && breakBlock.EndTime > breakBlock.StartTime)
                        .OrderBy(x => MinutesBetween(x.StartTime, x.EndTime))
                        .ThenBy(x => x.StartTime)
                        .FirstOrDefault();

                    if (hostSession is null)
                        continue;

                    embeddedBreaksBySession[hostSession.Id].Add(breakBlock);
                    embeddedBreakIds.Add(breakBlock.Id);
                }

                List<ProgramScheduleBlockDto> rawBlocks = new();
                rawBlocks.AddRange(roomFixedBlocks
                    .Where(x => !embeddedBreakIds.Contains(x.Id))
                    .Select(x => new ProgramScheduleBlockDto
                    {
                        Kind = "fixed",
                        Id = x.Id,
                        Title = x.Title,
                        StartTime = x.StartTime,
                        EndTime = x.EndTime,
                        FixedBlockType = x.BlockType,
                        IsPersisted = true,
                        IsMovable = x.BlockType == CongressProgramFixedBlockType.Break
                    }));

                rawBlocks.AddRange(roomSessions.Select(session =>
                    MapSessionBlock(
                        session,
                        submissions,
                        authors,
                        boardMembers,
                        embeddedBreaksBySession[session.Id],
                        videoSubmissionIds)));

                IReadOnlyList<ProgramScheduleBlockDto> normalizedBlocks = NormalizeDisplayTimeline(
                    day.StartTime,
                    day.EndTime,
                    rawBlocks);

                roomSchedules.Add(new ProgramRoomScheduleDto
                {
                    RoomId = roomId,
                    RoomName = roomName,
                    RoomOrder = roomOrder,
                    Blocks = normalizedBlocks
                });
            }

            days.Add(new ProgramDayDto
            {
                Id = day.Id,
                Date = day.Date,
                StartTime = day.StartTime,
                EndTime = day.EndTime,
                Order = day.Order,
                Rooms = roomSchedules
            });
        }

        IReadOnlyList<int> sessionBreakDurations = plan.Days
            .SelectMany(day => day.Sessions.SelectMany(session => day.FixedBlocks
                .Where(block => block.EventRoomId == session.EventRoomId
                                && block.BlockType == CongressProgramFixedBlockType.Break
                                && block.StartTime >= session.StartTime
                                && block.EndTime <= session.EndTime)
                .Select(block => MinutesBetween(block.StartTime, block.EndTime))))
            .Where(duration => duration > 0)
            .ToList();

        IReadOnlyList<string> timelineIssues = ValidateTimeline(days);

        return new ProgramPlanDto
        {
            Id = plan.Id,
            CongressId = plan.CongressId,
            Name = plan.Name,
            Status = plan.Status,
            LastGeneratedAt = plan.LastGeneratedAt,
            DefaultPresentationDurationMinutes = plan.DefaultPresentationDurationMinutes,
            DefaultSessionDurationMinutes = plan.DefaultSessionDurationMinutes,
            DefaultQuestionAnswerDurationMinutes = plan.DefaultQuestionAnswerDurationMinutes,
            DefaultBreakDurationMinutes = plan.DefaultBreakDurationMinutes,
            HasSessionBreaks = sessionBreakDurations.Count > 0,
            DefaultSessionBreakDurationMinutes = sessionBreakDurations.Count > 0
                ? (int)Math.Round(sessionBreakDurations.Average())
                : 10,
            AssignedCount = assignedIds.Count,
            EligibleCount = source.Submissions.Count,
            UnassignedCount = Math.Max(0, source.Submissions.Count - assignedIds.Count),
            SubmissionFilter = DeserializeSubmissionFilter(plan.SubmissionFilterJson),
            Days = days,
            UnassignedSubmissions = source.Submissions.Where(x => !assignedIds.Contains(x.Id)).ToList(),
            BoardSections = source.BoardSections
                .OrderBy(x => x.Order <= 0 ? int.MaxValue : x.Order)
                .ThenBy(x => x.Name)
                .ToList(),
            Participants = source.AuthorOptions
                .Where(x => participantAuthorKeys.Contains(x.IdentityKey))
                .GroupBy(
                    x => !string.IsNullOrWhiteSpace(x.Email)
                        ? $"email:{x.Email.Trim().ToUpperInvariant()}"
                        : $"name:{x.DisplayName.Trim().ToUpperInvariant()}|{x.Institution.Trim().ToUpperInvariant()}",
                    StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First())
                .OrderBy(x => x.TitleOrder)
                .ThenBy(x => x.DisplayName)
                .ThenBy(x => x.Institution)
                .Select(x => new ProgramParticipantDto(x.Id, x.DisplayName, x.Institution, x.TitleOrder))
                .ToList(),
            VideoPresentations = source.VideoPresentations
                .OrderBy(x => x.SubmissionNumber)
                .ThenBy(x => x.Title)
                .ToList(),
            TimelineIssues = timelineIssues
        };
    }

    private static ProgramScheduleBlockDto MapSessionBlock(
        CongressProgramSession session,
        IReadOnlyDictionary<Guid, ProgramSubmissionSourceDto> submissions,
        IReadOnlyDictionary<Guid, ProgramAuthorOptionDto> authors,
        IReadOnlyDictionary<Guid, ProgramBoardMemberOptionDto> boardMembers,
        IReadOnlyCollection<CongressProgramFixedBlock> embeddedBreaks,
        IReadOnlySet<Guid> videoSubmissionIds)
    {
        List<CongressProgramItem> orderedItems = session.Items
            .Where(x => !videoSubmissionIds.Contains(x.SubmissionId))
            .OrderBy(x => x.Order)
            .ThenBy(x => x.Id)
            .ToList();

        Dictionary<int, List<CongressProgramFixedBlock>> breaksByItemIndex =
            ResolveBreakInsertionIndexes(session, orderedItems, embeddedBreaks);

        TimeOnly cursor = session.StartTime;
        List<ProgramItemDto> items = new();
        List<ProgramSessionEntryDto> entries = new();

        for (int itemIndex = 0; itemIndex <= orderedItems.Count; itemIndex++)
        {
            if (breaksByItemIndex.TryGetValue(itemIndex, out List<CongressProgramFixedBlock>? breaksAtIndex))
            {
                foreach (CongressProgramFixedBlock breakBlock in breaksAtIndex
                             .OrderBy(x => x.StartTime)
                             .ThenBy(x => x.Order)
                             .ThenBy(x => x.Id))
                {
                    int duration = Math.Max(1, MinutesBetween(breakBlock.StartTime, breakBlock.EndTime));
                    TimeOnly breakEnd = AddMinutes(cursor, duration);
                    entries.Add(new ProgramSessionEntryDto
                    {
                        Kind = "break",
                        Break = new ProgramEmbeddedBreakDto
                        {
                            Id = breakBlock.Id,
                            Title = breakBlock.Title,
                            StartTime = cursor,
                            EndTime = breakEnd,
                            DurationMinutes = duration,
                            IsMovable = true
                        }
                    });
                    cursor = breakEnd;
                }
            }

            if (itemIndex == orderedItems.Count)
                continue;

            CongressProgramItem item = orderedItems[itemIndex];
            TimeOnly end = AddMinutes(cursor, item.DurationMinutes);
            ProgramSubmissionSourceDto? source = submissions.GetValueOrDefault(item.SubmissionId);
            string authorDisplayNames = !string.IsNullOrWhiteSpace(source?.Authors)
                ? source.Authors
                : string.Join(" - ", item.Submission.Authors
                    .OrderByDescending(x => x.IsCorrespondingAuthor)
                    .ThenBy(x => x.FirstName)
                    .ThenBy(x => x.LastName)
                    .Select(x => $"{x.FirstName} {x.LastName}".Trim()));

            ProgramItemDto itemDto = new()
            {
                Id = item.Id,
                SubmissionId = item.SubmissionId,
                SubmissionNumber = item.Submission.SubmissionNumber,
                Title = item.Submission.Title,
                TopicName = source?.TopicName ?? "Konu belirtilmemiş",
                Authors = authorDisplayNames,
                DurationMinutes = item.DurationMinutes,
                Order = item.Order,
                IsLocked = item.IsLocked,
                Source = item.Source,
                StartTime = cursor,
                EndTime = end
            };

            items.Add(itemDto);
            entries.Add(new ProgramSessionEntryDto
            {
                Kind = "item",
                Item = itemDto
            });
            cursor = end;
        }

        int totalMinutes = MinutesBetween(session.StartTime, session.EndTime);
        int embeddedBreakMinutes = embeddedBreaks.Sum(x => Math.Max(0, MinutesBetween(x.StartTime, x.EndTime)));
        int available = Math.Max(0,
            totalMinutes - session.QuestionAnswerDurationMinutes - embeddedBreakMinutes);
        int used = items.Sum(x => x.DurationMinutes);

        int effectiveQuestionAnswerMinutes = items.Count == 0 ? 0 : session.QuestionAnswerDurationMinutes;
        TimeOnly? questionStart = items.Count == 0 ? null : cursor;
        TimeOnly? questionEnd = questionStart.HasValue
            ? AddMinutes(questionStart.Value, effectiveQuestionAnswerMinutes)
            : null;
        TimeOnly actualEnd = questionEnd ?? session.EndTime;

        return new ProgramScheduleBlockDto
        {
            Kind = "session",
            Id = session.Id,
            Title = session.Title,
            StartTime = session.StartTime,
            EndTime = actualEnd,
            IsPersisted = true,
            IsMovable = false,
            Session = new ProgramSessionDto
            {
                Id = session.Id,
                Title = session.Title,
                StartTime = session.StartTime,
                EndTime = actualEnd,
                PlannedEndTime = session.EndTime,
                QuestionAnswerStartTime = questionStart,
                QuestionAnswerEndTime = questionEnd,
                QuestionAnswerDurationMinutes = effectiveQuestionAnswerMinutes,
                AvailablePresentationMinutes = available,
                UsedPresentationMinutes = used,
                EmbeddedBreakMinutes = embeddedBreakMinutes,
                ChairAuthorId = session.ChairAuthorId,
                ChairBoardMemberId = session.ChairBoardMemberId,
                ChairName = ResolveOfficialName(
                    session.ChairAuthorId,
                    session.ChairBoardMemberId,
                    authors,
                    boardMembers),
                ViceChairAuthorId = session.ViceChairAuthorId,
                ViceChairBoardMemberId = session.ViceChairBoardMemberId,
                ViceChairName = ResolveOfficialName(
                    session.ViceChairAuthorId,
                    session.ViceChairBoardMemberId,
                    authors,
                    boardMembers),
                ParticipantAuthorIds = session.Items
                    .Where(item => !videoSubmissionIds.Contains(item.SubmissionId))
                    .SelectMany(item => item.Submission.Authors)
                    .Select(author => author.Id)
                    .Distinct()
                    .OrderBy(id => id)
                    .ToArray(),
                Items = items,
                Entries = entries
            }
        };
    }


    private static ProgramSubmissionFilterDto DeserializeSubmissionFilter(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new ProgramSubmissionFilterDto
            {
                Preset = ProgramSubmissionScopePreset.AcceptedOnly
            };
        }

        try
        {
            return JsonSerializer.Deserialize<ProgramSubmissionFilterDto>(json)
                   ?? new ProgramSubmissionFilterDto
                   {
                       Preset = ProgramSubmissionScopePreset.AcceptedOnly
                   };
        }
        catch (JsonException)
        {
            return new ProgramSubmissionFilterDto
            {
                Preset = ProgramSubmissionScopePreset.AcceptedOnly
            };
        }
    }

    private static string ResolveOfficialName(
        Guid? authorId,
        Guid? boardMemberId,
        IReadOnlyDictionary<Guid, ProgramAuthorOptionDto> authors,
        IReadOnlyDictionary<Guid, ProgramBoardMemberOptionDto> boardMembers)
    {
        if (authorId.HasValue
            && authors.TryGetValue(authorId.Value, out ProgramAuthorOptionDto? author))
        {
            return author.DisplayName;
        }

        if (boardMemberId.HasValue
            && boardMembers.TryGetValue(boardMemberId.Value, out ProgramBoardMemberOptionDto? boardMember))
        {
            return boardMember.DisplayName;
        }

        return string.Empty;
    }

    private static Dictionary<int, List<CongressProgramFixedBlock>> ResolveBreakInsertionIndexes(
        CongressProgramSession session,
        IReadOnlyList<CongressProgramItem> items,
        IReadOnlyCollection<CongressProgramFixedBlock> embeddedBreaks)
    {
        Dictionary<int, List<CongressProgramFixedBlock>> result = new();
        int precedingBreakMinutes = 0;

        foreach (CongressProgramFixedBlock breakBlock in embeddedBreaks
                     .OrderBy(x => x.StartTime)
                     .ThenBy(x => x.Order)
                     .ThenBy(x => x.Id))
        {
            int elapsedFromSessionStart = Math.Max(0, MinutesBetween(session.StartTime, breakBlock.StartTime));
            int presentationMinutesBeforeBreak = Math.Max(0, elapsedFromSessionStart - precedingBreakMinutes);

            int cumulativePresentationMinutes = 0;
            int insertionIndex = 0;
            while (insertionIndex < items.Count
                   && cumulativePresentationMinutes + items[insertionIndex].DurationMinutes
                   <= presentationMinutesBeforeBreak)
            {
                cumulativePresentationMinutes += items[insertionIndex].DurationMinutes;
                insertionIndex++;
            }

            if (!result.TryGetValue(insertionIndex, out List<CongressProgramFixedBlock>? list))
            {
                list = new List<CongressProgramFixedBlock>();
                result[insertionIndex] = list;
            }

            list.Add(breakBlock);
            precedingBreakMinutes += Math.Max(0, MinutesBetween(breakBlock.StartTime, breakBlock.EndTime));
        }

        return result;
    }

    private static IReadOnlyList<ProgramScheduleBlockDto> NormalizeDisplayTimeline(
        TimeOnly dayStart,
        TimeOnly dayEnd,
        IEnumerable<ProgramScheduleBlockDto> blocks)
    {
        List<ProgramScheduleBlockDto> ordered = blocks
            .OrderBy(x => x.StartTime)
            .ThenBy(x => x.EndTime)
            .ThenBy(x => x.Kind)
            .ToList();

        List<ProgramScheduleBlockDto> result = new();
        TimeOnly cursor = dayStart;

        foreach (ProgramScheduleBlockDto block in ordered)
        {
            if (block.EndTime <= block.StartTime)
            {
                result.Add(block);
                continue;
            }

            if (block.Kind == "fixed" && block.FixedBlockType == CongressProgramFixedBlockType.Break)
            {
                TimeOnly adjustedStart = block.StartTime;

                if (cursor < block.EndTime)
                    adjustedStart = cursor;

                if (adjustedStart < block.EndTime)
                {
                    ProgramScheduleBlockDto adjustedBreak = CopyBlockWithTimes(block, adjustedStart, block.EndTime);
                    result.Add(adjustedBreak);
                    if (adjustedBreak.EndTime > cursor)
                        cursor = adjustedBreak.EndTime;
                }

                continue;
            }

            // An explicitly deleted break must not be recreated as a synthetic block.
            // Keep the real start times visible, but only render persisted break records.
            if (block.StartTime > cursor)
                cursor = block.StartTime;

            result.Add(block);
            if (block.EndTime > cursor)
                cursor = block.EndTime;
        }

        return result
            .Where(x => x.StartTime < dayEnd && x.EndTime > dayStart)
            .OrderBy(x => x.StartTime)
            .ThenBy(x => x.EndTime)
            .ThenBy(x => x.Kind)
            .ToList();
    }

    private static ProgramScheduleBlockDto CopyBlockWithTimes(
        ProgramScheduleBlockDto source,
        TimeOnly start,
        TimeOnly end)
        => new()
        {
            Kind = source.Kind,
            Id = source.Id,
            Title = source.Title,
            StartTime = start,
            EndTime = end,
            FixedBlockType = source.FixedBlockType,
            IsPersisted = source.IsPersisted,
            IsMovable = source.IsMovable,
            Session = source.Session
        };

    private static ProgramScheduleBlockDto CreateSyntheticBreak(TimeOnly start, TimeOnly end)
        => new()
        {
            Kind = "fixed",
            Id = Guid.NewGuid(),
            Title = "Ara",
            StartTime = start,
            EndTime = end,
            FixedBlockType = CongressProgramFixedBlockType.Break,
            IsPersisted = false,
            IsMovable = false
        };

    private static IReadOnlyList<string> ValidateTimeline(IEnumerable<ProgramDayDto> days)
    {
        List<string> issues = new();

        foreach (ProgramDayDto day in days)
        {
            foreach (ProgramRoomScheduleDto room in day.Rooms)
            {
                List<ProgramScheduleBlockDto> blocks = room.Blocks
                    .OrderBy(x => x.StartTime)
                    .ThenBy(x => x.EndTime)
                    .ToList();

                if (blocks.Count == 0)
                {
                    issues.Add($"{day.Date:dd.MM.yyyy} / {room.RoomName}: program bloğu bulunmuyor.");
                    continue;
                }

                TimeOnly cursor = day.StartTime;
                foreach (ProgramScheduleBlockDto block in blocks)
                {
                    if (block.StartTime < day.StartTime || block.EndTime > day.EndTime || block.EndTime <= block.StartTime)
                    {
                        issues.Add($"{day.Date:dd.MM.yyyy} / {room.RoomName}: {block.Title} çalışma saatlerinin dışında veya geçersiz.");
                        continue;
                    }

                    if (block.StartTime > cursor)
                    {
                        issues.Add($"{day.Date:dd.MM.yyyy} / {room.RoomName}: {cursor:HH:mm}-{block.StartTime:HH:mm} arasında plansız süre var.");
                    }
                    else if (block.StartTime < cursor)
                    {
                        issues.Add($"{day.Date:dd.MM.yyyy} / {room.RoomName}: {block.Title} önceki blokla çakışıyor.");
                    }

                    if (block.Session?.IsOverCapacity == true)
                    {
                        issues.Add($"{day.Date:dd.MM.yyyy} / {room.RoomName}: {block.Title} kapasitesi aşılıyor.");
                    }

                    if (block.EndTime > cursor)
                        cursor = block.EndTime;
                }
            }
        }

        return issues.Distinct(StringComparer.Ordinal).Take(20).ToList();
    }

    private static int MinutesBetween(TimeOnly start, TimeOnly end)
        => (int)(end.ToTimeSpan() - start.ToTimeSpan()).TotalMinutes;

    private static TimeOnly AddMinutes(TimeOnly time, int minutes)
        => TimeOnly.FromTimeSpan(time.ToTimeSpan().Add(TimeSpan.FromMinutes(minutes)));
}
