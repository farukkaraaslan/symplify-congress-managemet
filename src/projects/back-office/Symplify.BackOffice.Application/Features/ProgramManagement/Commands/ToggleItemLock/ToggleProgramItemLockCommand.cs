using Core.Application.Pipelines.Authorization;
using FluentValidation;
using MediatR;
using Symplify.BackOffice.Application.Features.ProgramManagement.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.ProgramManagement.Commands.ToggleItemLock;

public sealed class ToggleProgramItemLockCommand : IRequest<bool>, ISecuredRequest
{
    public Guid CongressId { get; set; }
    public Guid ItemId { get; set; }
    public string[] Roles => ProgramManagementOperationClaims.AdminOnly;

    public sealed class Handler : IRequestHandler<ToggleProgramItemLockCommand, bool>
    {
        private readonly IProgramManagementRepository _repository;
        public Handler(IProgramManagementRepository repository) => _repository = repository;

        public async Task<bool> Handle(ToggleProgramItemLockCommand request, CancellationToken cancellationToken)
        {
            CongressProgramPlan plan = await _repository.GetPlanForUpdateAsync(request.CongressId, cancellationToken)
                ?? throw new InvalidOperationException("Program taslağı bulunamadı.");
            CongressProgramItem item = plan.Days.SelectMany(x => x.Sessions).SelectMany(x => x.Items)
                .FirstOrDefault(x => x.Id == request.ItemId)
                ?? throw new InvalidOperationException("Program bildirisi bulunamadı.");
            item.IsLocked = !item.IsLocked;
            item.UpdatedDate = DateTime.UtcNow;
            plan.UpdatedDate = item.UpdatedDate;
            await _repository.SaveChangesAsync(cancellationToken);
            return item.IsLocked;
        }
    }
}

public sealed class ToggleProgramItemLockCommandValidator : AbstractValidator<ToggleProgramItemLockCommand>
{
    public ToggleProgramItemLockCommandValidator()
    {
        RuleFor(x => x.CongressId).NotEmpty();
        RuleFor(x => x.ItemId).NotEmpty();
    }
}
