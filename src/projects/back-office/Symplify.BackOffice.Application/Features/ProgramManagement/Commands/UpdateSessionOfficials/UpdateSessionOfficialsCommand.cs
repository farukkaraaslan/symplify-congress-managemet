using Core.Application.Pipelines.Authorization;
using FluentValidation;
using MediatR;
using Symplify.BackOffice.Application.Features.ProgramManagement.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.ProgramManagement.Commands.UpdateSessionOfficials;

public sealed class UpdateSessionOfficialsCommand : IRequest, ISecuredRequest
{
    public Guid CongressId { get; set; }
    public Guid SessionId { get; set; }

    public Guid? ChairAuthorId { get; set; }
    public Guid? ChairBoardMemberId { get; set; }
    public Guid? ViceChairAuthorId { get; set; }
    public Guid? ViceChairBoardMemberId { get; set; }

    public string[] Roles => ProgramManagementOperationClaims.AdminOnly;

    public sealed class Handler : IRequestHandler<UpdateSessionOfficialsCommand>
    {
        private readonly IProgramManagementRepository _repository;

        public Handler(IProgramManagementRepository repository)
        {
            _repository = repository;
        }

        public async Task Handle(
            UpdateSessionOfficialsCommand request,
            CancellationToken cancellationToken)
        {
            EnsureSingleSourcePerRole(request);
            EnsureDifferentOfficials(request);

            CongressProgramPlan plan = await _repository.GetPlanForUpdateAsync(
                    request.CongressId,
                    cancellationToken)
                ?? throw new InvalidOperationException("Program taslağı bulunamadı.");

            CongressProgramSession session = plan.Days
                .SelectMany(day => day.Sessions)
                .FirstOrDefault(x => x.Id == request.SessionId)
                ?? throw new InvalidOperationException("Oturum bulunamadı.");

            Guid[] selectedAuthorIds = new[]
                {
                    request.ChairAuthorId,
                    request.ViceChairAuthorId
                }
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .Distinct()
                .ToArray();

            Guid[] selectedBoardMemberIds = new[]
                {
                    request.ChairBoardMemberId,
                    request.ViceChairBoardMemberId
                }
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .Distinct()
                .ToArray();

            bool authorsAreEligible = await _repository.AreAuthorsEligibleForCongressAsync(
                request.CongressId,
                selectedAuthorIds,
                cancellationToken);

            if (!authorsAreEligible)
            {
                throw new InvalidOperationException(
                    "Seçilen görevlilerden biri bu kongrenin uygun katılımcı listesinde bulunmuyor.");
            }

            bool boardMembersAreEligible = await _repository.AreBoardMembersEligibleForCongressAsync(
                request.CongressId,
                selectedBoardMemberIds,
                cancellationToken);

            if (!boardMembersAreEligible)
            {
                throw new InvalidOperationException(
                    "Seçilen görevlilerden biri bu kongrenin aktif kurul üyeleri arasında bulunmuyor.");
            }

            CongressProgramDay targetDay = plan.Days
                .First(day => day.Id == session.ProgramDayId);

            ValidateAuthorSchedule(
                plan,
                targetDay,
                session,
                request.ChairAuthorId,
                "Oturum başkanı");
            ValidateBoardMemberSchedule(
                plan,
                targetDay,
                session,
                request.ChairBoardMemberId,
                "Oturum başkanı");
            ValidateAuthorSchedule(
                plan,
                targetDay,
                session,
                request.ViceChairAuthorId,
                "Oturum başkan yardımcısı");
            ValidateBoardMemberSchedule(
                plan,
                targetDay,
                session,
                request.ViceChairBoardMemberId,
                "Oturum başkan yardımcısı");

            DateTime now = DateTime.UtcNow;
            session.ChairAuthorId = request.ChairAuthorId;
            session.ChairBoardMemberId = request.ChairBoardMemberId;
            session.ViceChairAuthorId = request.ViceChairAuthorId;
            session.ViceChairBoardMemberId = request.ViceChairBoardMemberId;
            session.UpdatedDate = now;
            plan.UpdatedDate = now;

            await _repository.SaveChangesAsync(cancellationToken);
        }

        private static void EnsureSingleSourcePerRole(UpdateSessionOfficialsCommand request)
        {
            if (request.ChairAuthorId.HasValue && request.ChairBoardMemberId.HasValue)
            {
                throw new InvalidOperationException(
                    "Oturum başkanı aynı anda hem katılımcı hem kurul üyesi olarak seçilemez.");
            }

            if (request.ViceChairAuthorId.HasValue && request.ViceChairBoardMemberId.HasValue)
            {
                throw new InvalidOperationException(
                    "Oturum başkan yardımcısı aynı anda hem katılımcı hem kurul üyesi olarak seçilemez.");
            }
        }

        private static void EnsureDifferentOfficials(UpdateSessionOfficialsCommand request)
        {
            bool sameAuthor = request.ChairAuthorId.HasValue
                              && request.ViceChairAuthorId.HasValue
                              && request.ChairAuthorId.Value == request.ViceChairAuthorId.Value;

            bool sameBoardMember = request.ChairBoardMemberId.HasValue
                                   && request.ViceChairBoardMemberId.HasValue
                                   && request.ChairBoardMemberId.Value == request.ViceChairBoardMemberId.Value;

            if (sameAuthor || sameBoardMember)
            {
                throw new InvalidOperationException(
                    "Oturum başkanı ile başkan yardımcısı aynı kişi olamaz.");
            }
        }

        private static void ValidateAuthorSchedule(
            CongressProgramPlan plan,
            CongressProgramDay targetDay,
            CongressProgramSession targetSession,
            Guid? authorId,
            string roleName)
        {
            if (!authorId.HasValue)
                return;

            IEnumerable<CongressProgramSession> overlappingSessions = plan.Days
                .Where(day => day.Date == targetDay.Date)
                .SelectMany(day => day.Sessions)
                .Where(session => session.Id != targetSession.Id
                                  && Overlaps(
                                      targetSession.StartTime,
                                      targetSession.EndTime,
                                      session.StartTime,
                                      session.EndTime));

            bool hasOfficialConflict = overlappingSessions.Any(session =>
                session.ChairAuthorId == authorId.Value
                || session.ViceChairAuthorId == authorId.Value);

            if (hasOfficialConflict)
            {
                throw new InvalidOperationException(
                    $"{roleName} aynı saat aralığında başka bir oturumda görevli.");
            }

            bool hasPresentationConflict = overlappingSessions.Any(session =>
                session.Items.Any(item =>
                    item.Submission.Authors.Any(author => author.Id == authorId.Value)));

            if (hasPresentationConflict)
            {
                throw new InvalidOperationException(
                    $"{roleName} aynı saat aralığında başka bir oturumda bildiri sahibidir.");
            }
        }

        private static void ValidateBoardMemberSchedule(
            CongressProgramPlan plan,
            CongressProgramDay targetDay,
            CongressProgramSession targetSession,
            Guid? boardMemberId,
            string roleName)
        {
            if (!boardMemberId.HasValue)
                return;

            bool hasConflict = plan.Days
                .Where(day => day.Date == targetDay.Date)
                .SelectMany(day => day.Sessions)
                .Any(session =>
                    session.Id != targetSession.Id
                    && Overlaps(
                        targetSession.StartTime,
                        targetSession.EndTime,
                        session.StartTime,
                        session.EndTime)
                    && (session.ChairBoardMemberId == boardMemberId.Value
                        || session.ViceChairBoardMemberId == boardMemberId.Value));

            if (hasConflict)
            {
                throw new InvalidOperationException(
                    $"{roleName} aynı saat aralığında başka bir oturumda görevli.");
            }
        }

        private static bool Overlaps(
            TimeOnly start1,
            TimeOnly end1,
            TimeOnly start2,
            TimeOnly end2)
            => start1 < end2 && start2 < end1;
    }
}

public sealed class UpdateSessionOfficialsCommandValidator
    : AbstractValidator<UpdateSessionOfficialsCommand>
{
    public UpdateSessionOfficialsCommandValidator()
    {
        RuleFor(x => x.CongressId).NotEmpty();
        RuleFor(x => x.SessionId).NotEmpty();

        RuleFor(x => x)
            .Must(x => !(x.ChairAuthorId.HasValue && x.ChairBoardMemberId.HasValue))
            .WithMessage("Oturum başkanı için yalnızca bir kişi kaynağı seçilebilir.");

        RuleFor(x => x)
            .Must(x => !(x.ViceChairAuthorId.HasValue && x.ViceChairBoardMemberId.HasValue))
            .WithMessage("Oturum başkan yardımcısı için yalnızca bir kişi kaynağı seçilebilir.");

        RuleFor(x => x)
            .Must(x => !(
                x.ChairAuthorId.HasValue
                && x.ViceChairAuthorId.HasValue
                && x.ChairAuthorId.Value == x.ViceChairAuthorId.Value))
            .WithMessage("Oturum başkanı ile başkan yardımcısı aynı kişi olamaz.");

        RuleFor(x => x)
            .Must(x => !(
                x.ChairBoardMemberId.HasValue
                && x.ViceChairBoardMemberId.HasValue
                && x.ChairBoardMemberId.Value == x.ViceChairBoardMemberId.Value))
            .WithMessage("Oturum başkanı ile başkan yardımcısı aynı kişi olamaz.");
    }
}
