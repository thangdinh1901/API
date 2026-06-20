using System.Text;

namespace Plant3DCatalogComposer.Services
{
    /// <summary>Deployable Plant 3D catalog files produced from the scene graph.</summary>
    public sealed class CatalogPackage
    {
        public required string ScriptName { get; init; }
        /// <summary>Logical part id (e.g. ELBOW_45_LR_BW_SCH40).</summary>
        public required string FolderName { get; init; }
        /// <summary>Relative path under parts/ for file export.</summary>
        public required string ExportFolderName { get; init; }
        public bool IsStandardPortReference { get; init; }
        public string? StandardPartId { get; init; }
        public required int PortManagerPortCount { get; init; }
        public required string CatalogEntryXml { get; init; }
        public required string CatalogEntryPy { get; init; }
        public required string ScriptGroupXml { get; init; }
        public required string GeometryPy { get; init; }

        public string ToDisplayText()
        {
            var sb = new StringBuilder();
            sb.AppendLine("\"\"\"Plant 3D catalog package — Plant 3D Catalog Composer.\"\"\"");
            if (IsStandardPortReference && !string.IsNullOrEmpty(StandardPartId))
            {
                sb.AppendLine("# PORT REFERENCE ONLY — standard library is NOT modified.");
                sb.AppendLine($"# Standard part: catalog_generator/parts/{StandardPartId}/");
                sb.AppendLine($"# Sandbox export: catalog_generator/parts/{ExportFolderName}/");
                sb.AppendLine("# Share this output so port fixes can be applied to the standard source files.");
            }
            else
            {
                sb.AppendLine($"# Part folder: {ExportFolderName}/");
            }
            sb.AppendLine(
                PortManagerPortCount > 0
                    ? $"# Port Manager: {PortManagerPortCount} port(s) — prim.set_port in {ScriptName}.py → add_ports()"
                    : "# Port Manager: 0 ports — connection points from library geometry or TODO defaults");
            sb.AppendLine("# Deploy files (also written on Generate Code):");
            sb.AppendLine("#   catalog_entry.xml | catalog_entry.py | ScriptGroup.xml");
            sb.AppendLine($"#   {ExportFolderName}/{ScriptName}.py");
            sb.AppendLine($"#   {ExportFolderName}/{ScriptName}.xml | {ExportFolderName}/__INIT__.xml (identical stubs, auto-written)");
            sb.AppendLine("# Note: catalog_entry.py only has @activate metadata (Ports, FirstPortEndtypes).");
            sb.AppendLine("# Preview rebuild uses composer_live.py (not included here).");
            sb.AppendLine();
            AppendSection(sb, "catalog_entry.xml", CatalogEntryXml);
            AppendSection(sb, "catalog_entry.py", CatalogEntryPy);
            AppendSection(sb, "ScriptGroup.xml", ScriptGroupXml);
            AppendSection(sb, $"{ScriptName}.py", GeometryPy);
            AppendSection(sb, $"{ScriptName}.xml", CatalogPartBoilerplate.EmptyArrayOfScriptXml);
            AppendSection(sb, "__INIT__.xml", CatalogPartBoilerplate.EmptyArrayOfScriptXml);
            return sb.ToString();
        }

        private static void AppendSection(StringBuilder sb, string title, string content)
        {
            sb.AppendLine("# " + new string('=', 78));
            sb.AppendLine($"# {title}");
            sb.AppendLine("# " + new string('=', 78));
            sb.AppendLine(content.TrimEnd());
            sb.AppendLine();
        }
    }
}
