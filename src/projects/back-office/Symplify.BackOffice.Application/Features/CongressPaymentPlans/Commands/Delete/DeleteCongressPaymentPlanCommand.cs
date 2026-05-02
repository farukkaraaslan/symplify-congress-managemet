using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.CongressPaymentPlans.Constants;
using Symplify.BackOffice.Application.Features.CongressPaymentPlans.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.CongressPaymentPlans.Commands.Delete;
public class DeleteCongressPaymentPlanCommand : IRequest<DeletedCongressPaymentPlanResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCongressPaymentPlans";
    public string[] Roles => new[] { CongressPaymentPlansOperationClaims.Admin, CongressPaymentPlansOperationClaims.Write, CongressPaymentPlansOperationClaims.Delete };
    public class DeleteCongressPaymentPlanCommandHandler : IRequestHandler<DeleteCongressPaymentPlanCommand, DeletedCongressPaymentPlanResponse>
    {
        private readonly ICongressPaymentPlanRepository _repository; private readonly IMapper _mapper; private readonly CongressPaymentPlanBusinessRules _rules;
        public DeleteCongressPaymentPlanCommandHandler(ICongressPaymentPlanRepository repository, IMapper mapper, CongressPaymentPlanBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<DeletedCongressPaymentPlanResponse> Handle(DeleteCongressPaymentPlanCommand request, CancellationToken cancellationToken)
        {
            CongressPaymentPlan? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.CongressPaymentPlanShouldExistWhenSelected(entity);
            CongressPaymentPlan deletedEntity = await _repository.DeleteAsync(entity!);
            return _mapper.Map<DeletedCongressPaymentPlanResponse>(deletedEntity);
        }
    }
}
