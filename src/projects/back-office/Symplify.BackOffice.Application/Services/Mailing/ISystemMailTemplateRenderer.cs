namespace Symplify.BackOffice.Application.Services.Mailing;

public interface ISystemMailTemplateRenderer
{
    Task<RenderedSystemMailTemplate> RenderAsync(
        SystemMailTemplateRenderRequest request,
        CancellationToken cancellationToken = default);

    Task<RenderedSystemMailTemplate> RenderCustomAsync(
        CustomMailTemplateRenderRequest request,
        CancellationToken cancellationToken = default);
}
