using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Plant3DCatalogComposer.Services
{
    /// <summary>
    /// Rebuilds catalog_generator ScriptGroup.xml, variants.xml, and variants.map
    /// from every parts/*/catalog_entry.py (Spec Editor reads these at CustomScripts root).
    /// </summary>
    internal static class CatalogMetadataSyncService
    {
        private static readonly Regex ActivateDefRegex = new(
            @"@activate\s*\(\s*(?<attrs>.*?)\)\s*def\s+(?<name>CUST_\w+)\s*\(",
            RegexOptions.Singleline | RegexOptions.Compiled);

        private static readonly string[] PreservedVariantsMapLines =
        {
            "P3D_COMPOSER_REBUILD=P3D_COMPOSER_REBUILD",
            "wrapper=WRAPPER",
        };

        public static void SyncFromParts(string catalogGeneratorDir)
        {
            if (string.IsNullOrWhiteSpace(catalogGeneratorDir))
                return;

            string partsDir = Path.Combine(catalogGeneratorDir, "parts");
            if (!Directory.Exists(partsDir))
                return;

            var entries = new List<CatalogPartMetadata>();
            foreach (string partDir in Directory.EnumerateDirectories(partsDir))
            {
                string partId = Path.GetFileName(partDir);
                if (StandardCatalogGuard.IsSandboxDirectory(partId))
                    continue;

                string entryPy = Path.Combine(partDir, "catalog_entry.py");
                if (!File.Exists(entryPy))
                    continue;

                CatalogPartMetadata? meta = TryParseCatalogEntry(entryPy, partId);
                if (meta != null)
                    entries.Add(meta);
            }

            entries.Sort((a, b) => string.Compare(a.ScriptName, b.ScriptName, StringComparison.Ordinal));
            WriteScriptGroup(catalogGeneratorDir, entries);
            WriteVariants(catalogGeneratorDir, entries);
            WriteVariantsMap(catalogGeneratorDir, entries);
        }

        private sealed class CatalogPartMetadata
        {
            public required string ScriptName { get; init; }
            public required string Group { get; init; }
            public required string FirstPortEndtypes { get; init; }
            public required string TooltipShort { get; init; }
            public required string TooltipLong { get; init; }
        }

        private static CatalogPartMetadata? TryParseCatalogEntry(string entryPyPath, string partId)
        {
            string text = File.ReadAllText(entryPyPath);
            Match match = ActivateDefRegex.Match(text);
            if (!match.Success)
                return null;

            string attrs = match.Groups["attrs"].Value;
            string scriptName = match.Groups["name"].Value;
            string group = ExtractStringAttr(attrs, "Group") ?? "Custom";
            string endtypes = ExtractStringAttr(attrs, "FirstPortEndtypes") ?? "FL";
            string shortTip = ExtractStringAttr(attrs, "TooltipShort") ?? partId;
            string longTip = ExtractStringAttr(attrs, "TooltipLong") ?? shortTip;

            return new CatalogPartMetadata
            {
                ScriptName = scriptName,
                Group = group,
                FirstPortEndtypes = endtypes,
                TooltipShort = shortTip,
                TooltipLong = longTip,
            };
        }

        private static string? ExtractStringAttr(string attrsBlock, string name)
        {
            Match m = Regex.Match(attrsBlock, $@"{Regex.Escape(name)}\s*=\s*""([^""]*)""");
            return m.Success ? m.Groups[1].Value : null;
        }

        private static void WriteScriptGroup(string catalogGeneratorDir, IReadOnlyList<CatalogPartMetadata> entries)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            sb.AppendLine("<ScriptInfo>");
            foreach (CatalogPartMetadata entry in entries)
            {
                sb.AppendLine("\t<ScriptGroup>");
                sb.AppendLine($"\t\t<ScriptName>{entry.ScriptName}</ScriptName>");
                sb.AppendLine($"\t\t<Group>{EscapeXml(entry.Group)}</Group>");
                sb.AppendLine($"\t\t<FirstPortEndtypes>{EscapeXml(entry.FirstPortEndtypes)}</FirstPortEndtypes>");
                sb.AppendLine("\t</ScriptGroup>");
            }

            sb.AppendLine("</ScriptInfo>");
            File.WriteAllText(Path.Combine(catalogGeneratorDir, "ScriptGroup.xml"), sb.ToString().TrimEnd() + Environment.NewLine);
        }

        private static void WriteVariants(string catalogGeneratorDir, IReadOnlyList<CatalogPartMetadata> entries)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            sb.AppendLine("<MsgList xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">");
            sb.AppendLine("\t<ToolTipLongColl>");
            foreach (CatalogPartMetadata entry in entries)
            {
                sb.AppendLine(
                    $"\t\t<Data id=\"{entry.ScriptName}_L\">{EscapeXml(entry.TooltipLong)}</Data>");
            }

            sb.AppendLine("\t</ToolTipLongColl>");
            sb.AppendLine("\t<ToolTipShortColl>");
            foreach (CatalogPartMetadata entry in entries)
            {
                sb.AppendLine(
                    $"\t\t<Data id=\"{entry.ScriptName}\">{EscapeXml(entry.TooltipShort)}</Data>");
            }

            sb.AppendLine("\t</ToolTipShortColl>");
            sb.AppendLine("\t<ParamGroups>");
            sb.AppendLine("\t</ParamGroups>");
            sb.AppendLine("\t<EnumColl>");
            sb.AppendLine("\t</EnumColl>");
            sb.AppendLine("</MsgList>");
            File.WriteAllText(Path.Combine(catalogGeneratorDir, "variants.xml"), sb.ToString().TrimEnd() + Environment.NewLine);
        }

        private static void WriteVariantsMap(string catalogGeneratorDir, IReadOnlyList<CatalogPartMetadata> entries)
        {
            var lines = entries
                .Select(e => $"{e.ScriptName}={e.ScriptName}")
                .ToList();
            lines.AddRange(PreservedVariantsMapLines);
            File.WriteAllText(
                Path.Combine(catalogGeneratorDir, "variants.map"),
                string.Join(Environment.NewLine, lines) + Environment.NewLine);
        }

        private static string EscapeXml(string value) =>
            value
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;");
    }
}
