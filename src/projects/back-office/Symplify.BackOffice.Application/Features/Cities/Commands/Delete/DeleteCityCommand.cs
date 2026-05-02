using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.Cities.Constants;
using Symplify.BackOffice.Application.Features.Cities.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Reference;
using Symplify.BackOffice.Application.Features.Cities.Commands.Delete;
namespace Symplify.BackOffice.Application.Features.Cities.Commands.Delete;
public class DeleteCityCommand : IRequest<DeletedCityResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCities";
    public string[] Roles => new[] { CitiesOperationClaims.Admin, CitiesOperationClaims.Write, CitiesOperationClaims.Delete };
    public class DeleteCityCommandHandler : IRequestHandler<DeleteCityCommand, DeletedCityResponse>
    {
        private readonly ICityRepository _repository; private readonly IMapper _mapper; private readonly CityBusinessRules _rules;
        public DeleteCityCommandHandler(ICityRepository repository, IMapper mapper, CityBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<DeletedCityResponse> Handle(DeleteCityCommand request, CancellationToken cancellationToken)
        {
            City? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.CityShouldExistWhenSelected(entity);
            City deletedEntity = await _repository.DeleteAsync(entity!);
            return _mapper.Map<DeletedCityResponse>(deletedEntity);
        }
    }
}
