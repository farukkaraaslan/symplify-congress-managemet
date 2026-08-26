namespace Symplify.BackOffice.Application.Features.BulkEmails.Queries.GetComposePage;

public sealed class GetBulkEmailComposePageResponse
{
    public Guid? SelectedCongressId { get; set; }

    public IReadOnlyList<BulkEmailCongressOptionDto> Congresses { get; set; } = Array.Empty<BulkEmailCongressOptionDto>();
}

public sealed class BulkEmailCongressOptionDto
{
    public Guid Id { get; set; }

    public string Text { get; set; } = string.Empty;
}
