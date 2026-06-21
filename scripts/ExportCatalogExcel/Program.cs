using System;
using Plant3DCatalogComposer.Services;

if (args.Length < 1)
{
    Console.Error.WriteLine("Usage: ExportCatalogExcel <output.xlsx>");
    return 1;
}

return CatalogExcelExportCli.ExportAll(args[0]);
