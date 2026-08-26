using Symplify.BackOffice.Application.Features.MailDeliveries.Dtos;

namespace Symplify.BackOffice.Application.Features.MailDeliveries.Queries.GetList;

public sealed class GetMailDeliveryListResponse
{
    /// <summary>
    /// Authorization scope içerisindeki toplam mail kaydı. DataTables recordsTotal için kullanılır.
    /// </summary>
    public int RecordsTotalCount { get; set; }

    /// <summary>
    /// Uygulanan ekran filtreleri ve arama sonrasındaki toplam kayıt.
    /// DataTables recordsFiltered ve ekrandaki Toplam kartı için kullanılır.
    /// </summary>
    public int TotalCount { get; set; }

    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }

    public int PendingTransportCount { get; set; }
    public int FailedTransportCount { get; set; }
    public int DeliveredCount { get; set; }
    public int BouncedCount { get; set; }
    public int DelayedCount { get; set; }

    public IReadOnlyList<MailDeliveryListItemDto> Items { get; set; } = Array.Empty<MailDeliveryListItemDto>();
    public IReadOnlyList<MailDeliveryFilterOptionDto> Organizations { get; set; } = Array.Empty<MailDeliveryFilterOptionDto>();
    public IReadOnlyList<MailDeliveryFilterOptionDto> Congresses { get; set; } = Array.Empty<MailDeliveryFilterOptionDto>();
}
