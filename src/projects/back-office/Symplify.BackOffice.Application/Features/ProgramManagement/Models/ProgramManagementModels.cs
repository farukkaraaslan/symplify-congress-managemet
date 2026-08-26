using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.Application.Features.ProgramManagement.Models;

public sealed record ProgramCongressOptionDto(Guid Id, string Name, DateTime? StartDate, DateTime? EndDate);

public enum ProgramSubmissionScopePreset
{
    AllActive = 0,
    AcceptedOnly = 1,
    PaidOnly = 2,
    AcceptedAndPaid = 3
}

public sealed record ProgramStringFilterOptionDto(string Value, string Name, int Order);

public sealed record ProgramIntFilterOptionDto(int Id, string Code, string Name, int Order);

public sealed record ProgramGuidFilterOptionDto(Guid Id, string Code, string Name, int Order);

public sealed class ProgramSubmissionFilterOptionsDto
{
    public IReadOnlyList<ProgramStringFilterOptionDto> WorkflowStatuses { get; init; } = Array.Empty<ProgramStringFilterOptionDto>();
    public IReadOnlyList<ProgramIntFilterOptionDto> PaymentStatuses { get; init; } = Array.Empty<ProgramIntFilterOptionDto>();
    public IReadOnlyList<ProgramGuidFilterOptionDto> SubmissionTypes { get; init; } = Array.Empty<ProgramGuidFilterOptionDto>();
    public IReadOnlyList<ProgramGuidFilterOptionDto> Topics { get; init; } = Array.Empty<ProgramGuidFilterOptionDto>();
}

public sealed class ProgramSubmissionFilterDto
{
    public ProgramSubmissionScopePreset Preset { get; init; } = ProgramSubmissionScopePreset.AcceptedOnly;
    public IReadOnlyCollection<string> WorkflowStatusCodes { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<int> PaymentStatusIds { get; init; } = Array.Empty<int>();
    public IReadOnlyCollection<Guid> SubmissionTypeIds { get; init; } = Array.Empty<Guid>();
    public IReadOnlyCollection<Guid> TopicIds { get; init; } = Array.Empty<Guid>();
    public IReadOnlyCollection<Guid> IncludedSubmissionIds { get; init; } = Array.Empty<Guid>();
    public string? SearchText { get; init; }
}

public sealed record ProgramRoomOptionDto(Guid Id, string Name, int Order);

public sealed record ProgramAuthorOptionDto(
    Guid Id,
    string DisplayName,
    string Institution,
    string? Email,
    int TitleOrder,
    string IdentityKey);

public sealed record ProgramBoardMemberOptionDto(
    Guid Id,
    string DisplayName,
    string Institution,
    int TitleOrder);

public sealed record ProgramBoardMemberPdfDto(
    Guid Id,
    string DisplayName,
    string Institution,
    int Order);

public sealed record ProgramBoardSectionDto(
    Guid Id,
    string Name,
    int Order,
    IReadOnlyList<ProgramBoardMemberPdfDto> Members);

public sealed record ProgramParticipantDto(
    Guid Id,
    string DisplayName,
    string Institution,
    int TitleOrder);

public sealed record ProgramVideoPresentationDto(
    Guid SubmissionId,
    string SubmissionNumber,
    string Title,
    string Authors,
    string? ShortLinkCode);

public sealed record ProgramSubmissionSourceDto(
    Guid Id,
    string SubmissionNumber,
    string Title,
    Guid? TopicId,
    string TopicName,
    Guid? SubmissionTypeId,
    string SubmissionTypeName,
    string WorkflowStatusCode,
    string WorkflowStatusName,
    int? PaymentStatusId,
    string PaymentStatusCode,
    string PaymentStatusName,
    bool IsAccepted,
    bool IsPaid,
    string Authors,
    IReadOnlyCollection<string> AuthorKeys);

public sealed class ProgramGenerationSourceDto
{
    public Guid CongressId { get; init; }
    public string CongressName { get; init; } = string.Empty;
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public IReadOnlyList<ProgramRoomOptionDto> Rooms { get; init; } = Array.Empty<ProgramRoomOptionDto>();
    public IReadOnlyList<ProgramAuthorOptionDto> AuthorOptions { get; init; } = Array.Empty<ProgramAuthorOptionDto>();
    public IReadOnlyList<ProgramBoardMemberOptionDto> BoardMemberOptions { get; init; } = Array.Empty<ProgramBoardMemberOptionDto>();
    public IReadOnlyList<ProgramBoardSectionDto> BoardSections { get; init; } = Array.Empty<ProgramBoardSectionDto>();
    public ProgramSubmissionFilterOptionsDto FilterOptions { get; init; } = new();
    public IReadOnlyList<ProgramSubmissionSourceDto> AllSubmissions { get; init; } = Array.Empty<ProgramSubmissionSourceDto>();
    /// <summary>
    /// The complete submission set matching the selected workflow/payment/type/topic/search filters.
    /// Unlike <see cref="Submissions"/>, this list is not reduced by presentation mode
    /// (timed programme item versus video presentation).
    /// </summary>
    public IReadOnlyList<ProgramSubmissionSourceDto> FilteredSubmissions { get; init; } = Array.Empty<ProgramSubmissionSourceDto>();
    public IReadOnlyList<ProgramSubmissionSourceDto> Submissions { get; init; } = Array.Empty<ProgramSubmissionSourceDto>();
    public IReadOnlyList<ProgramVideoPresentationDto> VideoPresentations { get; init; } = Array.Empty<ProgramVideoPresentationDto>();
}

public sealed class ProgramGenerationSettings
{
    public Guid CongressId { get; init; }
    public IReadOnlyCollection<Guid> RoomIds { get; init; } = Array.Empty<Guid>();
    public TimeOnly DayStartTime { get; init; }
    public TimeOnly DayEndTime { get; init; }
    public int SessionDurationMinutes { get; init; }
    public int PresentationDurationMinutes { get; init; }
    public int QuestionAnswerDurationMinutes { get; init; }
    public int BreakDurationMinutes { get; init; }
    public bool IncludeSessionBreaks { get; init; }
    public int SessionBreakDurationMinutes { get; init; }
    public bool IncludeOpening { get; init; }
    public int OpeningDurationMinutes { get; init; }
    public string OpeningTitle { get; init; } = "Açılış Konuşması";
    public Guid? OpeningRoomId { get; init; }
    public bool IncludeLunch { get; init; }
    public TimeOnly LunchStartTime { get; init; }
    public int LunchDurationMinutes { get; init; }
    public string LunchTitle { get; init; } = "Öğle Arası";
    public CongressProgramGenerationMode Mode { get; init; }
    public Guid? PerformedByUserId { get; init; }
}

public sealed class ProgramGenerationResult
{
    public int EligibleSubmissionCount { get; init; }
    public int AssignedSubmissionCount { get; init; }
    public int UnassignedSubmissionCount { get; init; }
    public int SessionCount { get; init; }
    public int DayCount { get; init; }
}

public sealed class ProgramManagementPageResponse
{
    public IReadOnlyList<ProgramCongressOptionDto> Congresses { get; init; } = Array.Empty<ProgramCongressOptionDto>();
    public Guid? SelectedCongressId { get; init; }
    public ProgramGenerationSourceDto? Source { get; init; }
    public ProgramPlanDto? Plan { get; init; }
}

public sealed class ProgramPlanDto
{
    public Guid Id { get; init; }
    public Guid CongressId { get; init; }
    public string Name { get; init; } = string.Empty;
    public CongressProgramPlanStatus Status { get; init; }
    public DateTime? LastGeneratedAt { get; init; }
    public int DefaultPresentationDurationMinutes { get; init; }
    public int DefaultSessionDurationMinutes { get; init; }
    public int DefaultQuestionAnswerDurationMinutes { get; init; }
    public int DefaultBreakDurationMinutes { get; init; }
    public bool HasSessionBreaks { get; init; }
    public int DefaultSessionBreakDurationMinutes { get; init; }
    public int AssignedCount { get; init; }
    public int EligibleCount { get; init; }
    public int UnassignedCount { get; init; }
    public ProgramSubmissionFilterDto SubmissionFilter { get; init; } = new();
    public IReadOnlyList<ProgramDayDto> Days { get; init; } = Array.Empty<ProgramDayDto>();
    public IReadOnlyList<ProgramSubmissionSourceDto> UnassignedSubmissions { get; init; } = Array.Empty<ProgramSubmissionSourceDto>();
    public IReadOnlyList<ProgramBoardSectionDto> BoardSections { get; init; } = Array.Empty<ProgramBoardSectionDto>();
    public IReadOnlyList<ProgramParticipantDto> Participants { get; init; } = Array.Empty<ProgramParticipantDto>();
    public IReadOnlyList<ProgramVideoPresentationDto> VideoPresentations { get; init; } = Array.Empty<ProgramVideoPresentationDto>();
    public IReadOnlyList<string> TimelineIssues { get; init; } = Array.Empty<string>();
    public bool IsTimelineConsistent => TimelineIssues.Count == 0;
}

public sealed class ProgramDayDto
{
    public Guid Id { get; init; }
    public DateOnly Date { get; init; }
    public TimeOnly StartTime { get; init; }
    public TimeOnly EndTime { get; init; }
    public int Order { get; init; }
    public IReadOnlyList<ProgramRoomScheduleDto> Rooms { get; init; } = Array.Empty<ProgramRoomScheduleDto>();
}

public sealed class ProgramRoomScheduleDto
{
    public Guid RoomId { get; init; }
    public string RoomName { get; init; } = string.Empty;
    public int RoomOrder { get; init; }
    public IReadOnlyList<ProgramScheduleBlockDto> Blocks { get; init; } = Array.Empty<ProgramScheduleBlockDto>();
}

public sealed class ProgramScheduleBlockDto
{
    public string Kind { get; init; } = string.Empty;
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public TimeOnly StartTime { get; init; }
    public TimeOnly EndTime { get; init; }
    public CongressProgramFixedBlockType? FixedBlockType { get; init; }
    public bool IsPersisted { get; init; }
    public bool IsMovable { get; init; }
    public ProgramSessionDto? Session { get; init; }
}

public sealed class ProgramSessionDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public TimeOnly StartTime { get; init; }

    // Actual end is calculated from presentations, embedded breaks and Q&A.
    public TimeOnly EndTime { get; init; }

    // Planned end keeps the persisted slot boundary and is used for capacity validation.
    public TimeOnly PlannedEndTime { get; init; }
    public TimeOnly? QuestionAnswerStartTime { get; init; }
    public TimeOnly? QuestionAnswerEndTime { get; init; }
    public int QuestionAnswerDurationMinutes { get; init; }
    public int AvailablePresentationMinutes { get; init; }
    public int UsedPresentationMinutes { get; init; }
    public int EmbeddedBreakMinutes { get; init; }
    public Guid? ChairAuthorId { get; init; }
    public Guid? ChairBoardMemberId { get; init; }
    public string ChairName { get; init; } = string.Empty;
    public Guid? ViceChairAuthorId { get; init; }
    public Guid? ViceChairBoardMemberId { get; init; }
    public string ViceChairName { get; init; } = string.Empty;
    public IReadOnlyList<Guid> ParticipantAuthorIds { get; init; } = Array.Empty<Guid>();
    public bool IsEmpty => Items.Count == 0;
    public bool IsOverCapacity => UsedPresentationMinutes > AvailablePresentationMinutes;
    public IReadOnlyList<ProgramItemDto> Items { get; init; } = Array.Empty<ProgramItemDto>();
    public IReadOnlyList<ProgramSessionEntryDto> Entries { get; init; } = Array.Empty<ProgramSessionEntryDto>();
}

public sealed class ProgramSessionEntryDto
{
    public string Kind { get; init; } = string.Empty;
    public ProgramItemDto? Item { get; init; }
    public ProgramEmbeddedBreakDto? Break { get; init; }
}

public sealed class ProgramEmbeddedBreakDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public TimeOnly StartTime { get; init; }
    public TimeOnly EndTime { get; init; }
    public int DurationMinutes { get; init; }
    public bool IsMovable { get; init; }
}

public sealed class ProgramItemDto
{
    public Guid Id { get; init; }
    public Guid SubmissionId { get; init; }
    public string SubmissionNumber { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string TopicName { get; init; } = string.Empty;
    public string Authors { get; init; } = string.Empty;
    public int DurationMinutes { get; init; }
    public int Order { get; init; }
    public bool IsLocked { get; init; }
    public CongressProgramItemSource Source { get; init; }
    public TimeOnly StartTime { get; init; }
    public TimeOnly EndTime { get; init; }
}

public sealed class ProgramBookCoverDto
{
    public byte[]? ImageBytes { get; init; }
    public string? ContentType { get; init; }
    public bool HasImage => ImageBytes is { Length: > 0 };
}

public sealed class ProgramBookPageHeaderDto
{
    public string CongressName { get; init; } = string.Empty;
    public string CongressEnglishName { get; init; } = string.Empty;
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public string City { get; init; } = string.Empty;
    public string Venue { get; init; } = string.Empty;
    public byte[]? LogoBytes { get; init; }
    public string? LogoContentType { get; init; }
}

public sealed class ProgramBookRenderOptionsDto
{
    public bool IncludeTableOfContents { get; init; } = true;
    public bool IncludeScheduleTimes { get; init; } = true;
    public bool IncludeBoards { get; init; } = true;
}

public sealed record ProgramDraftPdfResponse(byte[] Content, string FileName);
public sealed record ProgramDraftWordResponse(byte[] Content, string FileName);
