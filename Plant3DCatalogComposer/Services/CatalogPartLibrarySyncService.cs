using System;

namespace Plant3DCatalogComposer.Services
{
    internal sealed class CatalogPartLibrarySyncResult
    {
        public int ExcelSheetsRemoved { get; init; }
    }

    /// <summary>Reload catalog_generator/parts into UI lists after manual folder changes.</summary>
    internal static class CatalogPartLibrarySyncService
    {
        public static CatalogPartLibrarySyncResult SyncFromDisk()
        {
            var activePartIds = CatalogPartsIndex.CollectCatalogPartIds();
            int removed = 0;
            try
            {
                removed = CatalogExcelTemplateService.RemoveOrphanedPartSheets(activePartIds);
            }
            catch
            {
                // template may be locked or unavailable outside deployed plugin
            }

            CustomPartCatalog.Reload();
            return new CatalogPartLibrarySyncResult { ExcelSheetsRemoved = removed };
        }
    }
}
