using System;
using System.Collections.Generic;
using System.IO;

namespace Plant3DCatalogComposer.Services
{
    /// <summary>Writes catalog package files to a user-selected folder (Plant 3D CustomScripts layout).</summary>
    internal static class CatalogExportService
    {
        public static IReadOnlyList<string> Export(CatalogPackage package, string destinationRoot)
        {
            if (string.IsNullOrWhiteSpace(destinationRoot))
                throw new ArgumentException("Destination folder is required.", nameof(destinationRoot));

            string partDir = Path.Combine(destinationRoot, package.ExportFolderName);
            string moduleDir = Path.Combine(partDir, package.FolderName);
            Directory.CreateDirectory(moduleDir);

            var written = new List<string>(6);
            Write(Path.Combine(partDir, "catalog_entry.xml"), package.CatalogEntryXml, written);
            Write(Path.Combine(partDir, "catalog_entry.py"), package.CatalogEntryPy, written);
            Write(Path.Combine(partDir, "ScriptGroup.xml"), package.ScriptGroupXml, written);
            Write(Path.Combine(moduleDir, $"{package.ScriptName}.py"), package.GeometryPy, written);
            Write(Path.Combine(moduleDir, $"{package.ScriptName}.xml"), CatalogPartBoilerplate.EmptyArrayOfScriptXml, written);
            Write(Path.Combine(moduleDir, "__INIT__.xml"), CatalogPartBoilerplate.EmptyArrayOfScriptXml, written);

            string? catalogGeneratorDir = Directory.GetParent(destinationRoot)?.FullName;
            if (!string.IsNullOrEmpty(catalogGeneratorDir) && !package.IsStandardPortReference)
                CatalogMetadataSyncService.SyncFromParts(catalogGeneratorDir);

            return written;
        }

        private static void Write(string path, string content, List<string> written)
        {
            File.WriteAllText(path, content.TrimEnd() + Environment.NewLine);
            written.Add(path);
        }
    }
}
