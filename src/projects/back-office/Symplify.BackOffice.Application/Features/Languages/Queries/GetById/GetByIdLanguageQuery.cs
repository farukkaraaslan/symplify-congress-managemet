using AutoMapper;
using Core.Application.Pipelines.Authorization;
using MediatR;
using Symplify.BackOffice.Application.Features.Languages.Constants;
using Symplify.BackOffice.Application.Features.Languages.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Localization;
namespace Symplify.BackOffice.Application.Features.Languages.Queries.GetById;
public class GetByIdLanguageQuery : IRequest<GetByIdLanguageResponse>, ISecuredRequest
{
    public Guid Id { get; set; }
    public string[] Roles => new[] { LanguagesOperationClaims.Admin, LanguagesOperationClaims.Read };
    public class GetByIdLanguageQueryHandler : IRequestHandler<GetByIdLanguageQuery, GetByIdLanguageResponse>
    {
        private readonly ILanguageRepository _repository; private readonly IMapper _mapper; private readonly LanguageBusinessRules _rules;
        public GetByIdLanguageQueryHandler(ILanguageRepository repository, IMapper mapper, LanguageBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<GetByIdLanguageResponse> Handle(GetByIdLanguageQuery request, CancellationToken cancellationToken)
        {
            Language? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.LanguageShouldExistWhenSelected(entity);
            return _mapper.Map<GetByIdLanguageResponse>(entity);
        }
    }
}
