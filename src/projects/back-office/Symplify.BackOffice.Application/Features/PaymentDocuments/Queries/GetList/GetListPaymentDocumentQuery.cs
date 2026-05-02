using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.Application.Requests;
using Core.Application.Responses;
using Core.Persistence.Paging;
using MediatR;
using Symplify.BackOffice.Application.Features.PaymentDocuments.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;
namespace Symplify.BackOffice.Application.Features.PaymentDocuments.Queries.GetList;
public class GetListPaymentDocumentQuery : IRequest<GetListResponse<GetListPaymentDocumentListItemDto>>, ISecuredRequest, ICachableRequest
{
    public PageRequest PageRequest { get; set; } = new();
    public string[] Roles => new[] { PaymentDocumentsOperationClaims.Admin, PaymentDocumentsOperationClaims.Read };
    public bool BypassCache { get; }
    public string CacheKey => $"GetListPaymentDocuments({PageRequest.Page},{PageRequest.PageSize})";
    public string CacheGroupKey => "GetPaymentDocuments";
    public TimeSpan? SlidingExpiration { get; }
    public class GetListPaymentDocumentQueryHandler : IRequestHandler<GetListPaymentDocumentQuery, GetListResponse<GetListPaymentDocumentListItemDto>>
    {
        private readonly IPaymentDocumentRepository _repository; private readonly IMapper _mapper;
        public GetListPaymentDocumentQueryHandler(IPaymentDocumentRepository repository, IMapper mapper) { _repository = repository; _mapper = mapper; }
        public async Task<GetListResponse<GetListPaymentDocumentListItemDto>> Handle(GetListPaymentDocumentQuery request, CancellationToken cancellationToken)
        {
            IPaginate<PaymentDocument> entities = await _repository.GetListAsync(index: request.PageRequest.Page, size: request.PageRequest.PageSize, cancellationToken: cancellationToken);
            return _mapper.Map<GetListResponse<GetListPaymentDocumentListItemDto>>(entities);
        }
    }
}
