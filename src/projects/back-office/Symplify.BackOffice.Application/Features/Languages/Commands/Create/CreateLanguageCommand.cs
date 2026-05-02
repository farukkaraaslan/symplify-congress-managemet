using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.Languages.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Localization;
namespace Symplify.BackOffice.Application.Features.Languages.Commands.Create;
public class CreateLanguageCommand : IRequest<CreatedLanguageResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public string Name { get; set; } = string.Empty;
    public string Culture { get; set; } = string.Empty;
    public string? TwoLetterIsoCode { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public int Order { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetLanguages";
    public string[] Roles => new[] { LanguagesOperationClaims.Admin, LanguagesOperationClaims.Write, LanguagesOperationClaims.Add };
    public class CreateLanguageCommandHandler : IRequestHandler<CreateLanguageCommand, CreatedLanguageResponse>
    {
        private readonly ILanguageRepository _repository;
        private readonly IMapper _mapper;
        public CreateLanguageCommandHandler(ILanguageRepository repository, IMapper mapper) { _repository = repository; _mapper = mapper; }
        public async Task<CreatedLanguageResponse> Handle(CreateLanguageCommand request, CancellationToken cancellationToken)
        {
            Language entity = new()
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Culture = request.Culture,
                TwoLetterIsoCode = request.TwoLetterIsoCode,
                IsDefault = request.IsDefault,
                IsActive = request.IsActive,
                Order = request.Order,
            };
            Language createdEntity = await _repository.AddAsync(entity);
            return _mapper.Map<CreatedLanguageResponse>(createdEntity);
        }
    }
}
