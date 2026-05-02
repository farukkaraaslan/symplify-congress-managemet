using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.PaymentDocuments.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;
namespace Symplify.BackOffice.Application.Features.PaymentDocuments.Commands.Create;
public class CreatePaymentDocumentCommand : IRequest<CreatedPaymentDocumentResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid? SubmissionId { get; set; }
    public Guid? CongressId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string? OriginalFileName { get; set; }
    public string? ContentType { get; set; }
    public long? Size { get; set; }
    public bool IsApproved { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetPaymentDocuments";
    public string[] Roles => new[] { PaymentDocumentsOperationClaims.Admin, PaymentDocumentsOperationClaims.Write, PaymentDocumentsOperationClaims.Add };
    public class CreatePaymentDocumentCommandHandler : IRequestHandler<CreatePaymentDocumentCommand, CreatedPaymentDocumentResponse>
    {
        private readonly IPaymentDocumentRepository _repository;
        private readonly IMapper _mapper;
        public CreatePaymentDocumentCommandHandler(IPaymentDocumentRepository repository, IMapper mapper) { _repository = repository; _mapper = mapper; }
        public async Task<CreatedPaymentDocumentResponse> Handle(CreatePaymentDocumentCommand request, CancellationToken cancellationToken)
        {
            PaymentDocument entity = new()
            {
                Id = Guid.NewGuid(),
                SubmissionId = request.SubmissionId,
                CongressId = request.CongressId,
                FilePath = request.FilePath,
                OriginalFileName = request.OriginalFileName,
                ContentType = request.ContentType,
                Size = request.Size,
                IsApproved = request.IsApproved,
            };
            PaymentDocument createdEntity = await _repository.AddAsync(entity);
            return _mapper.Map<CreatedPaymentDocumentResponse>(createdEntity);
        }
    }
}
