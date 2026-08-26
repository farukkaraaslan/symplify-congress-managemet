using Core.Application.Pipelines.Authorization;
using FluentValidation;
using MediatR;
using Symplify.BackOffice.Application.Features.ProgramManagement.Constants;
using Symplify.BackOffice.Application.Features.ProgramManagement.Services;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.Application.Features.ProgramManagement.Commands.UpdateItemDuration;

public sealed class UpdateProgramItemDurationCommand : IRequest, ISecuredRequest
{
    public Guid CongressId { get; set; }
    public Guid ItemId { get; set; }
    public int DurationMinutes { get; set; }
    public string[] Roles => ProgramManagementOperationClaims.AdminOnly;

    public sealed class Handler : IRequestHandler<UpdateProgramItemDurationCommand>
    {
        private readonly IProgramManagementRepository _repository;
        public Handler(IProgramManagementRepository repository) => _repository = repository;

        public async Task Handle(UpdateProgramItemDurationCommand request, CancellationToken cancellationToken)
        {
            CongressProgramPlan plan = await _repository.GetPlanForUpdateAsync(request.CongressId, cancellationToken)
                ?? throw new InvalidOperationException("Program taslağı bulunamadı.");
            CongressProgramSession session = plan.Days.SelectMany(x => x.Sessions)
                .FirstOrDefault(x => x.Items.Any(i => i.Id == request.ItemId))
                ?? throw new InvalidOperationException("Program bildirisi bulunamadı.");
            CongressProgramItem item = session.Items.First(x => x.Id == request.ItemId);
            if (item.IsLocked)
                throw new InvalidOperationException("Kilitli bildirinin süresi değiştirilemez.");

            item.DurationMinutes = request.DurationMinutes;
            item.Source = CongressProgramItemSource.Manual;
            item.UpdatedDate = DateTime.UtcNow;

            // Oturum süresi sabit kalır. Süre artışı mevcut oturum kapasitesini aşarsa
            // taşan bildiriler aynı salonun sonraki oturumlarına otomatik aktarılır.
            // Süre azalırsa sonraki oturumlardan uygun bildiriler öne çekilir.
            ProgramScheduleRebalancer.RebalanceFromSessions(plan, session.Id);

            plan.UpdatedDate = item.UpdatedDate;
            await _repository.SaveChangesAsync(cancellationToken);
        }
    }
}

public sealed class UpdateProgramItemDurationCommandValidator : AbstractValidator<UpdateProgramItemDurationCommand>
{
    public UpdateProgramItemDurationCommandValidator()
    {
        RuleFor(x => x.CongressId).NotEmpty();
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.DurationMinutes).InclusiveBetween(5, 120);
    }
}
