using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer.Services
{
    internal static class CatalogPortTemplates
    {
        public static string? TryLoadAddPortsMethod(string? catalogPartId)
        {
            if (string.IsNullOrWhiteSpace(catalogPartId))
                return null;

            string? pyPath = FindGeometryScript(catalogPartId);
            if (pyPath == null || !File.Exists(pyPath))
                return null;

            string text = File.ReadAllText(pyPath);
            Match match = Regex.Match(
                text,
                @"(?ms)^    def add_ports\(self, s\):.*?^        return self",
                RegexOptions.Multiline);
            return match.Success ? match.Value : null;
        }

        private static string PartsRoot => ProjectPaths.ResolvePartsDir();

        public static string? TryLoadCatalogEntryPy(string? catalogPartId)
        {
            if (string.IsNullOrWhiteSpace(catalogPartId))
                return null;

            string path = Path.Combine(PartsRoot, catalogPartId, "catalog_entry.py");
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }

        /// <summary>Python class in flat CUST_*.py (e.g. SOFLRFCL150), from catalog_entry import line.</summary>
        public static string? TryResolveLibraryClassName(string? catalogPartId)
        {
            string? entryPy = TryLoadCatalogEntryPy(catalogPartId);
            if (string.IsNullOrEmpty(entryPy))
                return null;

            Match match = Regex.Match(
                entryPy,
                @"from\s+[A-Z0-9_]+\.CUST_[A-Z0-9_]+ import\s+(\w+)",
                RegexOptions.Multiline);
            return match.Success ? match.Groups[1].Value : null;
        }

        /// <summary>from CUST_PART import Class,... for flat CustomScripts modules.</summary>
        public static string? TryBuildFlatCatalogImport(string? catalogPartId)
        {
            string? entryPy = TryLoadCatalogEntryPy(catalogPartId);
            if (string.IsNullOrEmpty(catalogPartId) || string.IsNullOrEmpty(entryPy))
                return null;

            Match match = Regex.Match(
                entryPy,
                @"from\s+[A-Z0-9_]+\.CUST_[A-Z0-9_]+ import (.+)");
            if (!match.Success)
                return null;

            return $"from CUST_{catalogPartId} import {match.Groups[1].Value.Trim()}";
        }

        public static string? TryLoadCatalogEntryXml(string? catalogPartId)
        {
            if (string.IsNullOrWhiteSpace(catalogPartId))
                return null;

            string path = Path.Combine(PartsRoot, catalogPartId, "catalog_entry.xml");
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }

        public static string? TryLoadGeometryScriptPath(string? catalogPartId)
        {
            if (string.IsNullOrWhiteSpace(catalogPartId))
                return null;

            string? pyPath = FindGeometryScript(catalogPartId);
            return pyPath != null && File.Exists(pyPath) ? pyPath : null;
        }

        public static int CountPortsInAddPorts(string? addPortsMethod)
        {
            if (string.IsNullOrEmpty(addPortsMethod))
                return 0;

            return Regex.Matches(addPortsMethod, @"prim\.set_port\s*\(").Count;
        }

        public static string InferFirstPortEndtypes(string? catalogPartId, int portCount)
        {
            if (portCount <= 0)
                return "FL";

            if (!string.IsNullOrWhiteSpace(catalogPartId))
            {
                string id = catalogPartId.ToUpperInvariant();
                if (id.StartsWith("WN_", StringComparison.Ordinal))
                    return "FL,BV";
                if (id.StartsWith("SO_", StringComparison.Ordinal))
                    return "FL,SO";
                if (id.StartsWith("BLD_", StringComparison.Ordinal))
                    return "FL";
            }

            string? fromEntry = TryLoadFirstPortEndtypesFromEntry(catalogPartId);
            if (!string.IsNullOrWhiteSpace(fromEntry))
                return fromEntry;

            CustomPartDefinition? def = CustomPartCatalog.FindById(catalogPartId);
            if (def != null)
            {
                if (def.Group.Equals("Gasket", StringComparison.OrdinalIgnoreCase))
                    return portCount > 1 ? "FL,FL" : "FL";
            }

            return "FL";
        }

        private static string? TryLoadFirstPortEndtypesFromEntry(string? catalogPartId)
        {
            string? entry = TryLoadCatalogEntryPy(catalogPartId);
            if (string.IsNullOrEmpty(entry))
                return null;

            Match match = Regex.Match(entry, @"FirstPortEndtypes=""([^""]+)""");
            return match.Success ? match.Groups[1].Value.Trim() : null;
        }

        public static string GenerateDefaultAddPorts(double spanMm, string comment)
        {
            string span = spanMm.ToString("0.######", CultureInfo.InvariantCulture);
            return $@"    def add_ports(self, s):
        """"""{comment}""""""
        prim.set_port(
            s,
            prim.Point3d(0.0, 0.0, 0.0),
            prim.Point3d(-1.0, 0.0, 0.0),
        )
        prim.set_port(
            s,
            prim.Point3d({span}, 0.0, 0.0),
            prim.Point3d(1.0, 0.0, 0.0),
        )
        return self";
        }

        public static string GenerateAddPortsFromProject(ValveProject project)
        {
            var sb = new StringBuilder();
            sb.AppendLine("    def add_ports(self, s):");
            sb.AppendLine("        \"\"\"Auto-generated from Port Manager (scene graph).\"\"\"");

            foreach (ConnectionPort port in project.Ports.OrderBy(p => p.Number))
            {
                double[] pos = PortTransformMath.GetWorldPosition(project, port);
                double[] dir = PortTransformMath.GetWorldDirection(project, port);
                sb.AppendLine(
                    $"        # Port {port.Number} ({PortConnectionTypeHelper.ToEndTypeCode(port.Type)})");
                sb.AppendLine("        prim.set_port(");
                sb.AppendLine("            s,");
                sb.AppendLine(
                    $"            prim.Point3d({Fmt(pos[0])}, {Fmt(pos[1])}, {Fmt(pos[2])}),");
                sb.AppendLine(
                    $"            prim.Point3d({Fmt(dir[0])}, {Fmt(dir[1])}, {Fmt(dir[2])}),");
                sb.AppendLine("        )");
            }

            sb.AppendLine("        return self");
            return sb.ToString();
        }

        public static string BuildPortEndtypesFromProject(ValveProject project)
        {
            if (project.Ports.Count == 0)
                return "FL";

            return PortConnectionTypeHelper.BuildEndtypesCsv(project.Ports);
        }

        public static string InferFirstPortEndtypesFromProject(ValveProject project) =>
            BuildPortEndtypesFromProject(project);

        /// <summary>When Port Manager is empty, derive port end types from Part Family primary end.</summary>
        public static string InferFirstPortEndtypesFromPrimaryEnd(ValveProject project, int portCount)
        {
            if (portCount <= 0)
                return "FL";

            string primary = CatalogStandardSetInference.ResolvePrimaryEndType(project);
            string portEnd = MapPrimaryEndToPortEndType(primary);
            return portCount <= 1
                ? portEnd
                : string.Join(",", Enumerable.Repeat(portEnd, portCount));
        }

        public static string MapPrimaryEndToPortEndTypePublic(string primaryEnd) =>
            MapPrimaryEndToPortEndType(primaryEnd);

        private static string MapPrimaryEndToPortEndType(string primaryEnd)
        {
            return Plant3DEndTypes.NormalizeCode(primaryEnd) switch
            {
                "BV" or "PL" or "PPL" or "P" => "BV",
                "SW" or "PSW" => "SW",
                "LAP" or "LLP" => "LAP",
                "SO" => "SO",
                _ => Plant3DEndTypes.NormalizeCode(primaryEnd),
            };
        }

        private static string Fmt(double value) =>
            value.ToString("0.######", CultureInfo.InvariantCulture);

        /// <summary>Rewrite nested catalog imports/calls for flat CustomScripts layout.</summary>
        public static string SanitizeNestedCatalogGeometry(string geometry)
        {
            geometry = Regex.Replace(
                geometry,
                @"^from (CUST_[A-Z0-9_]+) import \1\s*$",
                m =>
                {
                    string partId = m.Groups[1].Value[5..];
                    return TryBuildFlatCatalogImport(partId) ?? m.Value;
                },
                RegexOptions.Multiline);

            geometry = Regex.Replace(
                geometry,
                @"CUST_([A-Z0-9_]+)\(s,\s*DN=(\d+)(?:,\s*CEL=([\d.]+))?(?:,\s*preview=True)?\)",
                m =>
                {
                    string? cls = TryResolveLibraryClassName(m.Groups[1].Value);
                    if (cls == null)
                        return m.Value;

                    string args = $"s, {m.Groups[2].Value}";
                    if (m.Groups[3].Success &&
                        double.TryParse(m.Groups[3].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double cel) &&
                        Math.Abs(cel) > 1e-9)
                    {
                        args += $", cel_mm={cel.ToString(CultureInfo.InvariantCulture)}";
                    }

                    return $"{cls}({args}, add_ports=False)";
                });

            return geometry;
        }

        private static string? FindGeometryScript(string catalogPartId)
        {
            string nested = Path.Combine(
                PartsRoot,
                catalogPartId,
                catalogPartId,
                $"CUST_{catalogPartId}.py");
            if (File.Exists(nested))
                return nested;

            string flat = Path.Combine(PartsRoot, catalogPartId, $"CUST_{catalogPartId}.py");
            return File.Exists(flat) ? flat : null;
        }
    }
}
