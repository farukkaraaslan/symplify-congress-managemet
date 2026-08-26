using Core.Application.Pipelines.Authorization;
using FluentValidation;
using MediatR;
using Symplify.BackOffice.Application.Features.ProgramManagement.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.ProgramManagement.Commands.Reset;

public sealed class ResetCongressProgramCommand : IRequest, ISecuredRequest
{
    public Guid CongressId { get; set; }
    public string[] Roles => ProgramManagementOperationClaims.AdminOnly;

    public sealed class Handler : IRequestHandler<ResetCongressProgramCommand>
    {
        private readonly IProgramManagementRepository _repository;
        public Handler(IProgramManagementRepository repository) => _repository = repository;

        public async Task Handle(ResetCongressProgramCommand request, CancellationToken cancellationToken)
        {
            CongressProgramPlan? plan = await _repository.GetPlanForUpdateAsync(request.CongressId, cancellationToken);
            if (plan is null)
                return;
            _repository.RemovePlan(plan);
            await _repository.SaveChangesAsync(cancellationToken);
        }
    }
}

public sealed class ResetCongressProgramCommandValidator : AbstractValidator<ResetCongressProgramCommand>
{
    public ResetCongressProgramCommandValidator() => RuleFor(x => x.CongressId).NotEmpty();
}
