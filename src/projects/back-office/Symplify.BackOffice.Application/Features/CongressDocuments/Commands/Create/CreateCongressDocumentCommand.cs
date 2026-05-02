using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.CongressDocuments.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.CongressDocuments.Commands.Create;
public class CreateCongressDocumentCommand : IRequest<CreatedCongressDocumentResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid CongressId { get; set; }
    public Guid? DocumentTypeId { get; set; }
    public Symplify.BackOffice.Domain.Enums.CongressDocumentType? Type { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string? OriginalFileName { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCongressDocuments";
    public string[] Roles => new[] { CongressDocumentsOperationClaims.Admin, CongressDocumentsOperationClaims.Write, CongressDocumentsOperationClaims.Add };
    public class CreateCongressDocumentCommandHandler : IRequestHandler<CreateCongressDocumentCommand, CreatedCongressDocumentResponse>
    {
        private readonly ICongressDocumentRepository _repository;
        private readonly IMapper _mapper;
        public CreateCongressDocumentCommandHandler(ICongressDocumentRepository repository, IMapper mapper) { _repository = repository; _mapper = mapper; }
        public async Task<CreatedCongressDocumentResponse> Handle(CreateCongressDocumentCommand request, CancellationToken cancellationToken)
        {
            CongressDocument entity = new()
            {
                Id = Guid.NewGuid(),
                CongressId = request.CongressId,
                DocumentTypeId = request.DocumentTypeId,
                Type = request.Type,
                FilePath = request.FilePath,
                OriginalFileName = request.OriginalFileName,
                Order = request.Order,
                IsActive = request.IsActive,
            };
            CongressDocument createdEntity = await _repository.AddAsync(entity);
            return _mapper.Map<CreatedCongressDocumentResponse>(createdEntity);
        }
    }
}
