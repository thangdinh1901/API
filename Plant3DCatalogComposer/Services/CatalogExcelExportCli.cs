using System;

namespace Plant3DCatalogComposer.Services
{
    /// <summary>Headless Excel export for scripts/Export-CatalogExcel.ps1 (no AutoCAD UI).</summary>
    public static class CatalogExcelExportCli
    {
        public static int ExportAll(string outputPath)
        {
            CatalogExcelExportResult result = CatalogExcelExportService.Export(outputPath);
            Console.WriteLine(result.Message);
            if (result.Warnings.Count > 0)
            {
                foreach (string w in result.Warnings)
                    Console.WriteLine("WARN: " + w);
            }

            return result.Success ? 0 : 1;
        }
    }
}
