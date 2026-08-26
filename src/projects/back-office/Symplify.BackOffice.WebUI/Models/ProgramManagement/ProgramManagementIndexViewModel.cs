using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Symplify.BackOffice.Application.Features.ProgramManagement.Models;
using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.WebUI.Models.ProgramManagement;

public sealed class ProgramManagementIndexViewModel
{
    public ProgramManagementPageResponse Page { get; set; } = new();
    public GenerateProgramViewModel Generate { get; set; } = new();
    public ProgramBookExportViewModel Export { get; set; } = new();
}

public sealed class ProgramBookExportViewModel
{
    [Required]
    public Guid CongressId { get; set; }

    public IFormFile? CoverImageFile { get; set; }

    public bool IncludeTableOfContents { get; set; } = true;

    public bool IncludeScheduleTimes { get; set; } = true;

    public bool IncludeBoards { get; set; } = true;
}

public sealed class GenerateProgramViewModel : IValidatableObject
{
    [Required]
    public Guid CongressId { get; set; }

    [MinLength(1, ErrorMessage = "En az bir salon seçilmelidir.")]
    public List<Guid> RoomIds { get; set; } = new();

    [Required]
    [DataType(DataType.Time)]
    public TimeOnly DayStartTime { get; set; } = new(9, 0);

    [Required]
    [DataType(DataType.Time)]
    public TimeOnly DayEndTime { get; set; } = new(19, 30);

    [Range(30, 360)]
    public int SessionDurationMinutes { get; set; } = 120;

    [Range(5, 120)]
    public int PresentationDurationMinutes { get; set; } = 10;

    public bool IncludeQuestionAnswer { get; set; } = true;

    [Range(0, 180)]
    public int QuestionAnswerDurationMinutes { get; set; } = 10;

    [Range(0, 180)]
    public int BreakDurationMinutes { get; set; } = 30;

    public bool IncludeSessionBreaks { get; set; } = false;

    [Range(0, 60)]
    public int SessionBreakDurationMinutes { get; set; } = 10;

    public bool IncludeOpening { get; set; } = true;

    [Range(0, 360)]
    public int OpeningDurationMinutes { get; set; } = 60;

    [MaxLength(200)]
    public string OpeningTitle { get; set; } = "Açılış Konuşması";

    public Guid? OpeningRoomId { get; set; }

    public bool IncludeLunch { get; set; } = true;

    [DataType(DataType.Time)]
    public TimeOnly LunchStartTime { get; set; } = new(12, 30);

    [Range(0, 180)]
    public int LunchDurationMinutes { get; set; } = 30;

    [MaxLength(200)]
    public string LunchTitle { get; set; } = "Öğle Arası";

    public CongressProgramGenerationMode Mode { get; set; } = CongressProgramGenerationMode.ReplaceAll;

    public ProgramSubmissionScopePreset SubmissionScopePreset { get; set; } = ProgramSubmissionScopePreset.AcceptedOnly;

    public List<string> WorkflowStatusCodes { get; set; } = new();

    public List<int> PaymentStatusIds { get; set; } = new();

    public List<Guid> SubmissionTypeIds { get; set; } = new();

    public List<Guid> TopicIds { get; set; } = new();

    [MaxLength(250)]
    public string? SubmissionSearchText { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CongressId == Guid.Empty)
        {
            yield return new ValidationResult(
                "Kongre seçimi zorunludur.",
                new[] { nameof(CongressId) });
        }

        if (RoomIds.Count == 0)
        {
            yield return new ValidationResult(
                "En az bir salon seçilmelidir.",
                new[] { nameof(RoomIds) });
        }

        if (DayEndTime <= DayStartTime)
        {
            yield return new ValidationResult(
                "Gün bitiş saati başlangıç saatinden sonra olmalıdır.",
                new[] { nameof(DayStartTime), nameof(DayEndTime) });
            yield break;
        }

        int effectiveQuestionAnswerDurationMinutes = IncludeQuestionAnswer
            ? QuestionAnswerDurationMinutes
            : 0;

        if (IncludeQuestionAnswer && QuestionAnswerDurationMinutes <= 0)
        {
            yield return new ValidationResult(
                "Soru-cevap bölümü etkinse süre sıfırdan büyük olmalıdır.",
                new[] { nameof(QuestionAnswerDurationMinutes) });
        }

        int effectiveSessionBreakDurationMinutes = IncludeSessionBreaks
            ? SessionBreakDurationMinutes
            : 0;

        if (IncludeSessionBreaks && SessionBreakDurationMinutes <= 0)
        {
            yield return new ValidationResult(
                "Oturum içi ara etkinse süre sıfırdan büyük olmalıdır.",
                new[] { nameof(SessionBreakDurationMinutes) });
        }

        if (PresentationDurationMinutes + effectiveQuestionAnswerDurationMinutes + effectiveSessionBreakDurationMinutes > SessionDurationMinutes)
        {
            yield return new ValidationResult(
                "Oturum süresi, en az bir bildiri, varsa oturum içi ara ve soru-cevap süresini karşılamalıdır.",
                new[]
                {
                    nameof(SessionDurationMinutes),
                    nameof(PresentationDurationMinutes),
                    nameof(QuestionAnswerDurationMinutes),
                    nameof(SessionBreakDurationMinutes)
                });
        }

        TimeOnly? openingEnd = null;
        if (IncludeOpening)
        {
            if (OpeningDurationMinutes <= 0)
            {
                yield return new ValidationResult(
                    "Açılış bloğu etkinse süresi sıfırdan büyük olmalıdır.",
                    new[] { nameof(OpeningDurationMinutes) });
            }
            else
            {
                openingEnd = AddMinutes(DayStartTime, OpeningDurationMinutes);
                if (openingEnd > DayEndTime)
                {
                    yield return new ValidationResult(
                        "Açılış bloğu günlük çalışma saatlerinin dışına taşıyor.",
                        new[] { nameof(OpeningDurationMinutes), nameof(DayEndTime) });
                }
            }

            if (OpeningRoomId.HasValue && !RoomIds.Contains(OpeningRoomId.Value))
            {
                yield return new ValidationResult(
                    "Açılış salonu programa dahil edilen salonlardan biri olmalıdır.",
                    new[] { nameof(OpeningRoomId), nameof(RoomIds) });
            }
        }

        if (IncludeLunch)
        {
            if (LunchDurationMinutes <= 0)
            {
                yield return new ValidationResult(
                    "Öğle arası etkinse süresi sıfırdan büyük olmalıdır.",
                    new[] { nameof(LunchDurationMinutes) });
            }
            else
            {
                TimeOnly lunchEnd = AddMinutes(LunchStartTime, LunchDurationMinutes);
                if (LunchStartTime < DayStartTime || lunchEnd > DayEndTime)
                {
                    yield return new ValidationResult(
                        "Öğle arası günlük çalışma saatlerinin içinde olmalıdır.",
                        new[] { nameof(LunchStartTime), nameof(LunchDurationMinutes) });
                }

                if (openingEnd.HasValue
                    && Overlaps(DayStartTime, openingEnd.Value, LunchStartTime, lunchEnd))
                {
                    yield return new ValidationResult(
                        "Açılış bloğu ile öğle arası çakışamaz.",
                        new[]
                        {
                            nameof(OpeningDurationMinutes),
                            nameof(LunchStartTime),
                            nameof(LunchDurationMinutes)
                        });
                }
            }
        }
    }

    private static bool Overlaps(TimeOnly start1, TimeOnly end1, TimeOnly start2, TimeOnly end2)
        => start1 < end2 && start2 < end1;

    private static TimeOnly AddMinutes(TimeOnly time, int minutes)
        => TimeOnly.FromTimeSpan(time.ToTimeSpan().Add(TimeSpan.FromMinutes(minutes)));
}
