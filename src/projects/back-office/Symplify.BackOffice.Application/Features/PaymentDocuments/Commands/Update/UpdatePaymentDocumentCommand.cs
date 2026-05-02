using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.PaymentDocuments.Constants;
using Symplify.BackOffice.Application.Features.PaymentDocuments.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;
namespace Symplify.BackOffice.Application.Features.PaymentDocuments.Commands.Update;
public class UpdatePaymentDocumentCommand : IRequest<UpdatedPaymentDocumentResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
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
    public string[] Roles => new[] { PaymentDocumentsOperationClaims.Admin, PaymentDocumentsOperationClaims.Write, PaymentDocumentsOperationClaims.Update };
    public class UpdatePaymentDocumentCommandHandler : IRequestHandler<UpdatePaymentDocumentCommand, UpdatedPaymentDocumentResponse>
    {
        private readonly IPaymentDocumentRepository _repository; private readonly IMapper _mapper; private readonly PaymentDocumentBusinessRules _rules;
        public UpdatePaymentDocumentCommandHandler(IPaymentDocumentRepository repository, IMapper mapper, PaymentDocumentBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<UpdatedPaymentDocumentResponse> Handle(UpdatePaymentDocumentCommand request, CancellationToken cancellationToken)
        {
            PaymentDocument? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.PaymentDocumentShouldExistWhenSelected(entity);
            entity!.SubmissionId = request.SubmissionId;
            entity!.CongressId = request.CongressId;
            entity!.FilePath = request.FilePath;
            entity!.OriginalFileName = request.OriginalFileName;
            entity!.ContentType = request.ContentType;
            entity!.Size = request.Size;
            entity!.IsApproved = request.IsApproved;
            PaymentDocument updatedEntity = await _repository.UpdateAsync(entity!);
            return _mapper.Map<UpdatedPaymentDocumentResponse>(updatedEntity);
        }
    }
}
