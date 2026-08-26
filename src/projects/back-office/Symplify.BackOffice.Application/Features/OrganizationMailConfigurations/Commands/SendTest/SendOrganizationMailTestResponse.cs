namespace Symplify.BackOffice.Application.Features.OrganizationMailConfigurations.Commands.SendTest;

public sealed class SendOrganizationMailTestResponse
{
    public Guid OrganizationId { get; set; }
    public DateTime SentAt { get; set; }
}
