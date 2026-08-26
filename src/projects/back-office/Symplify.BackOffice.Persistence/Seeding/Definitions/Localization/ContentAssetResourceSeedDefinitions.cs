using System.Collections.Generic;

namespace Symplify.BackOffice.Persistence.Seeding.Definitions.Localization;

public static class ContentAssetResourceSeedDefinitions
{
    public static IReadOnlyCollection<ResourceSeedDefinition> All { get; } = new List<ResourceSeedDefinition>
    {
        new("BackOffice", "BackOffice.ContentAssets.Buttons.UploadAndInsert", "Dosya Yükle", "Upload File"),
        new("BackOffice", "BackOffice.ContentAssets.Messages.Uploading", "Dosya yükleniyor...", "Uploading file..."),
        new("BackOffice", "BackOffice.ContentAssets.Messages.Uploaded", "Dosya yüklendi ve bağlantı hazırlandı.", "File uploaded and link prepared."),
        new("BackOffice", "BackOffice.ContentAssets.Messages.UploadFailed", "Dosya yüklenemedi.", "File could not be uploaded."),
        new("BackOffice", "BackOffice.ContentAssets.Messages.StorageConfigurationInvalid", "Dosya yükleme servisi yapılandırması hatalı. MinIO erişim bilgilerini kontrol edin.", "File upload service configuration is invalid. Please check MinIO access credentials."),
        new("BackOffice", "BackOffice.ContentAssets.Validation.FileRequired", "Dosya seçimi zorunludur.", "File selection is required."),
        new("BackOffice", "BackOffice.ContentAssets.Validation.FileTooLarge", "Dosya boyutu en fazla 25 MB olabilir.", "File size can be at most 25 MB."),
        new("BackOffice", "BackOffice.ContentAssets.Validation.FileInvalid", "Bu dosya türüne izin verilmiyor. PDF, Word, Excel, PowerPoint, TXT, JPG, PNG veya WEBP yükleyebilirsiniz.", "This file type is not allowed. You can upload PDF, Word, Excel, PowerPoint, TXT, JPG, PNG or WEBP files."),
        new("BackOffice", "BackOffice.ContentAssets.Validation.ObjectStorageBucketMissing", "İçerik dosyası depolama bucket bilgisi bulunamadı.", "Content asset storage bucket configuration was not found."),
        new("BackOffice", "BackOffice.ContentAssets.Validation.CongressNotFound", "Kongre kaydı bulunamadı.", "Congress record was not found.")
    };
}
