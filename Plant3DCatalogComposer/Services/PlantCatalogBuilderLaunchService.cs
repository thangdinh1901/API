using System;
using System.Diagnostics;
using System.IO;

namespace Plant3DCatalogComposer.Services
{
    /// <summary>Launch Autodesk AutoCADPlant3DCatalogBuilder.exe with an exported workbook.</summary>
    internal static class PlantCatalogBuilderLaunchService
    {
        private const string CatalogBuilderExeName = "AutoCADPlant3DCatalogBuilder.exe";

        public static bool TryLaunch(string excelPath, out string message)
        {
            message = string.Empty;
            if (string.IsNullOrWhiteSpace(excelPath) || !File.Exists(excelPath))
            {
                message = "Excel file not found — open Catalog Builder manually.";
                return false;
            }

            string? exe = ResolveCatalogBuilderExe();
            if (exe == null)
            {
                message =
                    "AutoCADPlant3DCatalogBuilder.exe not found under Program Files\\Autodesk\\AutoCAD *\\PLNT3D.\n"
                    + "Open Spec Editor → Tools → Catalog Builder, then load:\n"
                    + excelPath;
                return false;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = Quote(excelPath),
                    UseShellExecute = true,
                });

                message = "Catalog Builder opened with exported workbook.";
                return true;
            }
            catch (Exception ex)
            {
                message = "Could not start Catalog Builder: " + ex.Message;
                return false;
            }
        }

        public static string? ResolveCatalogBuilderExe()
        {
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string autodeskRoot = Path.Combine(programFiles, "Autodesk");
            if (!Directory.Exists(autodeskRoot))
                return null;

            string? newest = null;
            foreach (string acadDir in Directory.EnumerateDirectories(autodeskRoot, "AutoCAD *"))
            {
                string candidate = Path.Combine(acadDir, "PLNT3D", CatalogBuilderExeName);
                if (!File.Exists(candidate))
                    continue;

                if (newest == null
                    || string.Compare(candidate, newest, StringComparison.OrdinalIgnoreCase) > 0)
                {
                    newest = candidate;
                }
            }

            return newest;
        }

        private static string Quote(string path) => "\"" + path.Replace("\"", "\\\"") + "\"";
    }
}
