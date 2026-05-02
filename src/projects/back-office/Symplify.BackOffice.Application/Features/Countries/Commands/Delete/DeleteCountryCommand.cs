using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.Countries.Constants;
using Symplify.BackOffice.Application.Features.Countries.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Reference;
namespace Symplify.BackOffice.Application.Features.Countries.Commands.Delete;
public class DeleteCountryCommand : IRequest<DeletedCountryResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCountries";
    public string[] Roles => new[] { CountriesOperationClaims.Admin, CountriesOperationClaims.Write, CountriesOperationClaims.Delete };
    public class DeleteCountryCommandHandler : IRequestHandler<DeleteCountryCommand, DeletedCountryResponse>
    {
        private readonly ICountryRepository _repository; private readonly IMapper _mapper; private readonly CountryBusinessRules _rules;
        public DeleteCountryCommandHandler(ICountryRepository repository, IMapper mapper, CountryBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<DeletedCountryResponse> Handle(DeleteCountryCommand request, CancellationToken cancellationToken)
        {
            Country? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.CountryShouldExistWhenSelected(entity);
            Country deletedEntity = await _repository.DeleteAsync(entity!);
            return _mapper.Map<DeletedCountryResponse>(deletedEntity);
        }
    }
}
