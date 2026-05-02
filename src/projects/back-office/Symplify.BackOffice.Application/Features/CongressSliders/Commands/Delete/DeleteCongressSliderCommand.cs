using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.CongressSliders.Constants;
using Symplify.BackOffice.Application.Features.CongressSliders.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.CongressSliders.Commands.Delete;
public class DeleteCongressSliderCommand : IRequest<DeletedCongressSliderResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCongressSliders";
    public string[] Roles => new[] { CongressSlidersOperationClaims.Admin, CongressSlidersOperationClaims.Write, CongressSlidersOperationClaims.Delete };
    public class DeleteCongressSliderCommandHandler : IRequestHandler<DeleteCongressSliderCommand, DeletedCongressSliderResponse>
    {
        private readonly ICongressSliderRepository _repository; private readonly IMapper _mapper; private readonly CongressSliderBusinessRules _rules;
        public DeleteCongressSliderCommandHandler(ICongressSliderRepository repository, IMapper mapper, CongressSliderBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<DeletedCongressSliderResponse> Handle(DeleteCongressSliderCommand request, CancellationToken cancellationToken)
        {
            CongressSlider? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.CongressSliderShouldExistWhenSelected(entity);
            CongressSlider deletedEntity = await _repository.DeleteAsync(entity!);
            return _mapper.Map<DeletedCongressSliderResponse>(deletedEntity);
        }
    }
}
