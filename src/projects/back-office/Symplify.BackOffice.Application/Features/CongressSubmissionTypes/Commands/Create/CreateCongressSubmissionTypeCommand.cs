using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.CongressSubmissionTypes.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.CongressSubmissionTypes.Commands.Create;
public class CreateCongressSubmissionTypeCommand : IRequest<CreatedCongressSubmissionTypeResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid CongressId { get; set; }
    public Guid SubmissionTypeId { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCongressSubmissionTypes";
    public string[] Roles => new[] { CongressSubmissionTypesOperationClaims.Admin, CongressSubmissionTypesOperationClaims.Write, CongressSubmissionTypesOperationClaims.Add };
    public class CreateCongressSubmissionTypeCommandHandler : IRequestHandler<CreateCongressSubmissionTypeCommand, CreatedCongressSubmissionTypeResponse>
    {
        private readonly ICongressSubmissionTypeRepository _repository;
        private readonly IMapper _mapper;
        public CreateCongressSubmissionTypeCommandHandler(ICongressSubmissionTypeRepository repository, IMapper mapper) { _repository = repository; _mapper = mapper; }
        public async Task<CreatedCongressSubmissionTypeResponse> Handle(CreateCongressSubmissionTypeCommand request, CancellationToken cancellationToken)
        {
            CongressSubmissionType entity = new()
            {
                Id = Guid.NewGuid(),
                CongressId = request.CongressId,
                SubmissionTypeId = request.SubmissionTypeId,
                Order = request.Order,
                IsActive = request.IsActive,
            };
            CongressSubmissionType createdEntity = await _repository.AddAsync(entity);
            return _mapper.Map<CreatedCongressSubmissionTypeResponse>(createdEntity);
        }
    }
}
