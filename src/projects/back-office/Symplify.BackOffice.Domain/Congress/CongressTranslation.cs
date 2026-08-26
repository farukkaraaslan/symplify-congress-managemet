using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Localization;

namespace Symplify.BackOffice.Domain.Congress;

public class CongressTranslation : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid CongressId { get; set; }

    public Guid LanguageId { get; set; }

    public string Title { get; set; } = null!;

    public string? Subtitle { get; set; }

    // Liste/SEO/public kısa açıklama için kullanılmalı.
    public string? ShortDescription { get; set; }

    // Eski alan geriye uyumluluk için korunur. Uzun ana sayfa karşılama yazısı için WelcomeContent kullanılmalı.
    public string? Description { get; set; }

    // Public ana sayfadaki “Değerli Araştırmacılar...” alanı.
    public string? WelcomeTitle { get; set; }

    public string? WelcomeContent { get; set; }

    public string? SeoTitle { get; set; }

    public string? SeoDescription { get; set; }

    // Eski tasarımdan kalma alan. Yeni akışta marka/logo Organization üzerinden gelmeli.
    public string? LogoPath { get; set; }

    public virtual Congress Congress { get; set; } = null!;

    public virtual Language Language { get; set; } = null!;
}
