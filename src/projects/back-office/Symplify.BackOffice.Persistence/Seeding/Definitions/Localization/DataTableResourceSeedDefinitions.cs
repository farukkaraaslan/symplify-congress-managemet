namespace Symplify.BackOffice.Persistence.Seeding.Definitions.Localization;

public static class DataTableResourceSeedDefinitions
{
    public static IReadOnlyCollection<ResourceSeedDefinition> All { get; } = new List<ResourceSeedDefinition>
    {
        new("Common.DataTable", "Common.DataTable.Search", "Ara:", "Search:"),
        new("Common.DataTable", "Common.DataTable.LengthMenu", "_MENU_ kayıt göster", "Show _MENU_ entries"),
        new("Common.DataTable", "Common.DataTable.Info", "_TOTAL_ kayıttan _START_ - _END_ arası gösteriliyor", "Showing _START_ to _END_ of _TOTAL_ entries"),
        new("Common.DataTable", "Common.DataTable.InfoEmpty", "Kayıt bulunamadı", "Showing 0 to 0 of 0 entries"),
        new("Common.DataTable", "Common.DataTable.InfoFiltered", "(_MAX_ kayıt içerisinden filtrelendi)", "filtered from _MAX_ total entries"),
        new("Common.DataTable", "Common.DataTable.ZeroRecords", "Eşleşen kayıt bulunamadı", "No matching records found"),
        new("Common.DataTable", "Common.DataTable.Processing", "Yükleniyor...", "Processing..."),
        new("Common.DataTable", "Common.DataTable.EmptyTable", "Tabloda kayıt bulunamadı", "No data available in table"),
        new("Common.DataTable", "Common.DataTable.LoadingRecords", "Kayıtlar yükleniyor...", "Loading..."),
        new("Common.DataTable", "Common.DataTable.First", "İlk", "First"),
        new("Common.DataTable", "Common.DataTable.Last", "Son", "Last"),
        new("Common.DataTable", "Common.DataTable.Next", "Sonraki", "Next"),
        new("Common.DataTable", "Common.DataTable.Previous", "Önceki", "Previous"),
        new("Common.DataTable", "Common.DataTable.SortAscending", ": artan sıralama için aktifleştir", ": activate to sort column ascending"),
        new("Common.DataTable", "Common.DataTable.SortDescending", ": azalan sıralama için aktifleştir", ": activate to sort column descending")
    };
}
