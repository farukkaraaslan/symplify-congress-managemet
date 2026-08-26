using System.Collections.Generic;

namespace Symplify.BackOffice.Persistence.Seeding.Definitions.Localization;

public static class CongressDocumentResourceSeedDefinitions
{
    public static IReadOnlyCollection<ResourceSeedDefinition> All { get; } = new List<ResourceSeedDefinition>
    {
        new("BackOffice", "BackOffice.CongressDocuments.ListTitle", "Doküman Listesi", "Document List"),
        new("BackOffice", "BackOffice.CongressDocuments.ListDescription", "Kongreye ait dokümanları MinIO üzerinde güvenli şekilde yönetin.", "Manage congress documents securely on MinIO."),
        new("BackOffice", "BackOffice.CongressDocuments.Reorder.Help", "Sıralamayı değiştirmek için satırları sürükleyip bırakabilirsiniz.", "Drag and drop rows to change the order."),

        new("BackOffice", "BackOffice.CongressDocuments.Create.Title", "Yeni Doküman", "New Document"),
        new("BackOffice", "BackOffice.CongressDocuments.Create.Description", "Doküman bilgilerini girin ve dosyayı yükleyin.", "Enter document information and upload the file."),
        new("BackOffice", "BackOffice.CongressDocuments.Update.Title", "Doküman Düzenle", "Edit Document"),
        new("BackOffice", "BackOffice.CongressDocuments.Update.Description", "Seçili dokümanın bilgilerini güncelleyin. Yeni dosya seçmezseniz mevcut dosya korunur.", "Update the selected document. The existing file is preserved if no new file is selected."),

        new("BackOffice", "BackOffice.CongressDocuments.Fields.Order", "Sıra No", "Order"),
        new("BackOffice", "BackOffice.CongressDocuments.Fields.DocumentType", "Doküman Tipi", "Document Type"),
        new("BackOffice", "BackOffice.CongressDocuments.Fields.Description", "Açıklama", "Description"),
        new("BackOffice", "BackOffice.CongressDocuments.Fields.Category", "Kategori", "Category"),
        new("BackOffice", "BackOffice.CongressDocuments.Fields.File", "Dosya", "File"),
        new("BackOffice", "BackOffice.CongressDocuments.Fields.CoverImage", "Kapak Görseli", "Cover Image"),
        new("BackOffice", "BackOffice.CongressDocuments.Fields.CurrentCoverImage", "Mevcut Kapak Görseli", "Current Cover Image"),
        new("BackOffice", "BackOffice.CongressDocuments.Fields.RemoveCoverImage", "Kapak görselini kaldır", "Remove cover image"),
        new("BackOffice", "BackOffice.CongressDocuments.Fields.FileName", "Dosya Adı", "File Name"),
        new("BackOffice", "BackOffice.CongressDocuments.Fields.FileSize", "Boyut", "Size"),
        new("BackOffice", "BackOffice.CongressDocuments.Fields.ContentType", "İçerik Tipi", "Content Type"),
        new("BackOffice", "BackOffice.CongressDocuments.Fields.Status", "Durum", "Status"),
        new("BackOffice", "BackOffice.CongressDocuments.Fields.IsActive", "Aktif mi?", "Is Active?"),
        new("BackOffice", "BackOffice.CongressDocuments.Fields.Storage", "Depolama", "Storage"),
        new("BackOffice", "BackOffice.CongressDocuments.Label.Optional", "Opsiyonel", "Optional"),

        new("BackOffice", "BackOffice.CongressDocuments.Buttons.New", "Yeni Doküman", "New Document"),
        new("BackOffice", "BackOffice.CongressDocuments.Buttons.Save", "Dokümanı Kaydet", "Save Document"),
        new("BackOffice", "BackOffice.CongressDocuments.Buttons.Update", "Dokümanı Güncelle", "Update Document"),
        new("BackOffice", "BackOffice.CongressDocuments.Buttons.Download", "İndir", "Download"),

        new("BackOffice", "BackOffice.CongressDocuments.Help.File", "PDF, Office dosyaları veya görsel yükleyebilirsiniz. Maksimum dosya boyutu 50 MB'dır.", "You can upload PDF, Office documents, or images. Maximum file size is 50 MB."),
        new("BackOffice", "BackOffice.CongressDocuments.Help.UpdateFile", "Yeni dosya seçmezseniz mevcut dosya korunur.", "The current file is preserved if no new file is selected."),
        new("BackOffice", "BackOffice.CongressDocuments.Help.Description", "Portalda doküman kartında gösterilir. Boş bırakırsanız açıklama gösterilmez.", "Shown on the document card in the portal. If empty, no description is shown."),
        new("BackOffice", "BackOffice.CongressDocuments.Help.CoverImage", "JPG, PNG veya WEBP formatında kapak görseli yükleyebilirsiniz. Maksimum 5 MB.", "You can upload a cover image in JPG, PNG, or WEBP format. Maximum 5 MB."),
        new("BackOffice", "BackOffice.CongressDocuments.Help.UpdateCoverImage", "Yeni kapak görseli seçmezseniz mevcut kapak korunur.", "The current cover is preserved if no new cover image is selected."),
        new("BackOffice", "BackOffice.CongressDocuments.Placeholder.Description", "Örn. Fen ve Mühendislik alanı için geçerlidir.", "E.g. Applies to Science and Engineering category."),
        new("BackOffice", "BackOffice.CongressDocuments.Dropzone.Select", "Dosya seçin veya buraya sürükleyin", "Choose a file or drag it here"),
        new("BackOffice", "BackOffice.CongressDocuments.Dropzone.SelectCoverImage", "Kapak görseli seçin veya buraya sürükleyin", "Choose a cover image or drag it here"),
        new("BackOffice", "BackOffice.CongressDocuments.Dropzone.Selected", "Seçilen dosya", "Selected file"),

        new("BackOffice", "BackOffice.CongressDocuments.Messages.Created", "Doküman başarıyla oluşturuldu.", "Document created successfully."),
        new("BackOffice", "BackOffice.CongressDocuments.Messages.Updated", "Doküman başarıyla güncellendi.", "Document updated successfully."),
        new("BackOffice", "BackOffice.CongressDocuments.Messages.Deleted", "Doküman başarıyla silindi.", "Document deleted successfully."),
        new("BackOffice", "BackOffice.CongressDocuments.Messages.Reordered", "Doküman sıralaması güncellendi.", "Document order updated successfully."),

        new("BackOffice", "BackOffice.CongressDocuments.Validation.CongressRequired", "Kongre bilgisi zorunludur.", "Congress is required."),
        new("BackOffice", "BackOffice.CongressDocuments.Validation.CongressNotFound", "Kongre bulunamadı.", "Congress was not found."),
        new("BackOffice", "BackOffice.CongressDocuments.Validation.EntityNotFound", "Doküman bulunamadı.", "Document was not found."),
        new("BackOffice", "BackOffice.CongressDocuments.Validation.DocumentTypeRequired", "Doküman tipi seçimi zorunludur.", "Document type is required."),
        new("BackOffice", "BackOffice.CongressDocuments.Validation.DocumentTypeNotFound", "Doküman tipi bulunamadı.", "Document type was not found."),
        new("BackOffice", "BackOffice.CongressDocuments.Validation.DescriptionMaxLength", "Açıklama en fazla 1000 karakter olabilir.", "Description can be at most 1000 characters."),
        new("BackOffice", "BackOffice.CongressDocuments.Validation.FileRequired", "Dosya yüklenmesi zorunludur.", "File upload is required."),
        new("BackOffice", "BackOffice.CongressDocuments.Validation.FileTooLarge", "Dosya boyutu en fazla 50 MB olabilir.", "File size can be at most 50 MB."),
        new("BackOffice", "BackOffice.CongressDocuments.Validation.FileInvalid", "Dosya geçersiz veya dosya türüne izin verilmiyor.", "The file is invalid or the file type is not allowed."),
        new("BackOffice", "BackOffice.CongressDocuments.Validation.CoverImageTooLarge", "Kapak görseli en fazla 5 MB olabilir.", "Cover image size can be at most 5 MB."),
        new("BackOffice", "BackOffice.CongressDocuments.Validation.CoverImageInvalid", "Kapak görseli JPG, PNG veya WEBP formatında olmalıdır.", "Cover image must be in JPG, PNG, or WEBP format."),
        new("BackOffice", "BackOffice.CongressDocuments.Validation.FileExtensionNotAllowed", "Bu dosya türüne izin verilmiyor.", "This file type is not allowed."),
        new("BackOffice", "BackOffice.CongressDocuments.Validation.OrderInvalid", "Sıralama değeri sıfırdan küçük olamaz.", "Order cannot be lower than zero."),
        new("BackOffice", "BackOffice.CongressDocuments.Validation.ReorderRequired", "Sıralanacak doküman bulunamadı.", "No document was found to reorder."),
        new("BackOffice", "BackOffice.CongressDocuments.Validation.InvalidReorderList", "Sıralama listesi geçersiz.", "Reorder list is invalid."),
        new("BackOffice", "BackOffice.CongressDocuments.Validation.ObjectStorageBucketMissing", "Doküman depolama bucket bilgisi bulunamadı.", "Document storage bucket configuration was not found."),
        new("BackOffice", "BackOffice.CongressDocuments.Validation.ObjectStorageObjectMissing", "Doküman depolama nesne bilgisi bulunamadı.", "Document storage object information was not found."),

        new("BackOffice", "BackOffice.CongressDocuments.Js.active", "Aktif", "Active"),
        new("BackOffice", "BackOffice.CongressDocuments.Js.passive", "Pasif", "Passive"),
        new("BackOffice", "BackOffice.CongressDocuments.Js.edit", "Düzenle", "Edit"),
        new("BackOffice", "BackOffice.CongressDocuments.Js.delete", "Sil", "Delete"),
        new("BackOffice", "BackOffice.CongressDocuments.Js.download", "İndir", "Download"),
        new("BackOffice", "BackOffice.CongressDocuments.Js.coverImage", "Kapak görseli", "Cover image"),
        new("BackOffice", "BackOffice.CongressDocuments.Js.hasCoverImage", "Kapak görseli var", "Has cover image"),
        new("BackOffice", "BackOffice.CongressDocuments.Js.saved", "Kayıt kaydedildi.", "Record saved."),
        new("BackOffice", "BackOffice.CongressDocuments.Js.deleted", "Kayıt silindi.", "Record deleted."),
        new("BackOffice", "BackOffice.CongressDocuments.Js.reordered", "Sıralama güncellendi.", "Order updated."),
        new("BackOffice", "BackOffice.CongressDocuments.Js.genericError", "İşlem sırasında bir hata oluştu.", "An error occurred during the operation."),
        new("BackOffice", "BackOffice.CongressDocuments.Js.deleteConfirmTitle", "Emin misiniz?", "Are you sure?"),
        new("BackOffice", "BackOffice.CongressDocuments.Js.deleteConfirmText", "Bu doküman silinecek.", "This document will be deleted."),
        new("BackOffice", "BackOffice.CongressDocuments.Js.deleteConfirmButton", "Sil", "Delete"),
        new("BackOffice", "BackOffice.CongressDocuments.Js.dragHandle", "Sırayı değiştirmek için sürükleyin", "Drag to reorder"),
        new("BackOffice", "BackOffice.CongressDocuments.Js.reorderNotAllowed", "Sıralama yapmak için arama boş olmalı ve tablo Sıra No kolonuna göre artan sıralanmalıdır.", "To reorder, search must be empty and the table must be sorted by Order ascending.")
    };
}
