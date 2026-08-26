namespace Symplify.BackOffice.Application.Features.ParticipationCertificates.Services;

public interface IParticipationCertificatePdfRenderer
{
    void ValidateTemplate(byte[] templatePdfBytes);

    byte[] Render(ParticipationCertificatePdfRenderRequest request);
}
