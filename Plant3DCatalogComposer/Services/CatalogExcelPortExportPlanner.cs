using System;
using System.Collections.Generic;
using System.Linq;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer.Services
{
    /// <summary>How port columns are laid out on a Catalog Builder worksheet.</summary>
    internal enum CatalogExcelSheetPortSchema
    {
        SingleAll,
        Dual,
        Triple,
    }

    internal readonly struct CatalogExcelLogicalPort
    {
        public CatalogExcelLogicalPort(string endType, string portName)
        {
            EndType = endType;
            PortName = portName;
        }

        public string EndType { get; }

        public string PortName { get; }
    }

    internal sealed class CatalogExcelPortExportPlan
    {
        public required CatalogExcelPortLayout EffectiveLayout { get; init; }

        public required IReadOnlyList<(CatalogExcelLogicalPort Port, string Suffix)> SheetPorts { get; init; }

        public int ConnectionPortCount { get; init; }

        public string PressureClass { get; init; } = "150";

        public string? FlangeFacing { get; init; }

        public string? Warning { get; init; }
    }

    /// <summary>
    /// Maps Part Family + Port Manager ports to Excel column suffixes (S-ALL / S1 / S2 / S3),
    /// respecting the clone-template sheet schema (valve templates use S-ALL).
    /// </summary>
    internal static class CatalogExcelPortExportPlanner
    {
        public static CatalogExcelPortExportPlan Build(
            IReadOnlyDictionary<string, int> header,
            CatalogExcelPartRow row,
            CatalogExcelSizeVariant size,
            ValveProject? project)
        {
            CatalogExcelSheetPortSchema schema = DetectSheetSchema(header);
            IReadOnlyList<CatalogExcelLogicalPort> logical = ResolveLogicalPorts(row, project);
            string pressureClass = ResolvePressureClass(row, project);
            string? facing = ResolveFlangeFacing(row, project);

            var warnings = new List<string>();
            IReadOnlyList<(CatalogExcelLogicalPort Port, string Suffix)> mapped =
                MapToSheet(schema, logical, warnings);

            CatalogExcelPortLayout layout = ResolveEffectiveLayout(schema, mapped);
            int connectionCount = Math.Max(logical.Count, mapped.Count > 0 ? logical.Count : 1);

            ApplySizeVariantDns(row, size, mapped, layout, out IReadOnlyList<(CatalogExcelLogicalPort Port, string Suffix, int Dn)> sizedPorts);

            return new CatalogExcelPortExportPlan
            {
                EffectiveLayout = layout,
                SheetPorts = sizedPorts.Select(p => (p.Port, p.Suffix)).ToList(),
                ConnectionPortCount = connectionCount,
                PressureClass = pressureClass,
                FlangeFacing = facing,
                Warning = warnings.Count > 0 ? string.Join(" ", warnings) : null,
            };
        }

        public static int ResolvePortDn(
            CatalogExcelPortExportPlan plan,
            string suffix,
            CatalogExcelPartRow row,
            CatalogExcelSizeVariant size,
            int runDn,
            double runOd)
        {
            int index = IndexOfSuffix(plan, suffix);
            if (index < 0)
                return runDn;

            int? dn2 = size.Dn2;
            int smallDn = dn2 ?? runDn;

            if (plan.EffectiveLayout == CatalogExcelPortLayout.DualPortBv && suffix.Equals("S2", StringComparison.OrdinalIgnoreCase))
                return smallDn;

            if (plan.EffectiveLayout == CatalogExcelPortLayout.TriplePortBv
                && (suffix.Equals("S2", StringComparison.OrdinalIgnoreCase) || suffix.Equals("S3", StringComparison.OrdinalIgnoreCase)))
            {
                return suffix.Equals("S3", StringComparison.OrdinalIgnoreCase) ? smallDn : runDn;
            }

            return runDn;
        }

        private static int IndexOfSuffix(CatalogExcelPortExportPlan plan, string suffix)
        {
            for (int i = 0; i < plan.SheetPorts.Count; i++)
            {
                if (plan.SheetPorts[i].Suffix.Equals(suffix, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return -1;
        }

        private static void ApplySizeVariantDns(
            CatalogExcelPartRow row,
            CatalogExcelSizeVariant size,
            IReadOnlyList<(CatalogExcelLogicalPort Port, string Suffix)> mapped,
            CatalogExcelPortLayout layout,
            out IReadOnlyList<(CatalogExcelLogicalPort Port, string Suffix, int Dn)> result)
        {
            // DN per suffix is resolved at write time via ResolvePortDn; keep mapped list as-is.
            result = mapped.Select(m => (m.Port, m.Suffix, size.Dn)).ToList();
        }

        internal static CatalogExcelSheetPortSchema DetectSheetSchema(IReadOnlyDictionary<string, int> header)
        {
            bool hasSAll = header.ContainsKey("EndType_S-ALL");
            bool hasS1 = header.ContainsKey("EndType_S1");
            bool hasS2 = header.ContainsKey("EndType_S2");
            bool hasS3 = header.ContainsKey("EndType_S3");

            if (hasS1 && hasS2 && hasS3)
                return CatalogExcelSheetPortSchema.Triple;

            if (hasS1 && hasS2)
                return CatalogExcelSheetPortSchema.Dual;

            if (hasSAll)
                return CatalogExcelSheetPortSchema.SingleAll;

            if (hasS1)
                return CatalogExcelSheetPortSchema.Dual;

            return CatalogExcelSheetPortSchema.SingleAll;
        }

        private static IReadOnlyList<CatalogExcelLogicalPort> ResolveLogicalPorts(
            CatalogExcelPartRow row,
            ValveProject? project)
        {
            if (TryResolveProjectPorts(row, project, out IReadOnlyList<CatalogExcelLogicalPort>? fromProject))
                return fromProject;

            return ResolveCatalogRowPorts(row);
        }

        private static bool TryResolveProjectPorts(
            CatalogExcelPartRow row,
            ValveProject? project,
            out IReadOnlyList<CatalogExcelLogicalPort> ports)
        {
            ports = Array.Empty<CatalogExcelLogicalPort>();
            if (project == null || project.Ports.Count == 0)
                return false;

            if (!PartMatchesProject(row.Part.Id, project))
                return false;

            ports = project.Ports
                .OrderBy(p => p.Number)
                .Select(p => new CatalogExcelLogicalPort(
                    PortConnectionTypeHelper.ToEndTypeCode(p.Type),
                    PortSuffixName(p.Number)))
                .ToList();
            return true;
        }

        internal static bool PartMatchesProject(string partId, ValveProject project)
        {
            string sanitized = CatalogProjectService.SanitizeCatalogName(project.ValveName ?? "");
            return !string.IsNullOrEmpty(sanitized)
                && sanitized.Equals(partId, StringComparison.OrdinalIgnoreCase);
        }

        private static string PortSuffixName(int portNumber) =>
            portNumber switch
            {
                1 => "S1",
                2 => "S2",
                3 => "S3",
                _ => $"S{portNumber}",
            };

        private static IReadOnlyList<CatalogExcelLogicalPort> ResolveCatalogRowPorts(CatalogExcelPartRow row)
        {
            var ports = new List<CatalogExcelLogicalPort>();

            void Add((string EndType, string PortName) spec)
            {
                if (string.IsNullOrWhiteSpace(spec.EndType))
                    return;
                ports.Add(new CatalogExcelLogicalPort(spec.EndType, spec.PortName));
            }

            switch (row.PortLayout)
            {
                case CatalogExcelPortLayout.DualFlange:
                case CatalogExcelPortLayout.DualPortBv:
                    Add(row.Port1);
                    if (row.HasSecondPort)
                        Add(row.Port2);
                    break;

                case CatalogExcelPortLayout.TriplePortBv:
                    Add(row.Port1);
                    Add(row.Port2);
                    ports.Add(new CatalogExcelLogicalPort(row.FittingEndType, "S3"));
                    break;

                default:
                    Add(row.Port1);
                    break;
            }

            if (ports.Count == 0)
                ports.Add(new CatalogExcelLogicalPort("FL", "ALL"));

            return ports;
        }

        private static IReadOnlyList<(CatalogExcelLogicalPort Port, string Suffix)> MapToSheet(
            CatalogExcelSheetPortSchema schema,
            IReadOnlyList<CatalogExcelLogicalPort> logical,
            List<string> warnings)
        {
            if (logical.Count == 0)
                logical = new[] { new CatalogExcelLogicalPort("FL", "ALL") };

            switch (schema)
            {
                case CatalogExcelSheetPortSchema.SingleAll:
                    return MapSingleAll(logical, warnings);

                case CatalogExcelSheetPortSchema.Dual:
                    return MapDual(logical, warnings);

                case CatalogExcelSheetPortSchema.Triple:
                    return MapTriple(logical, warnings);

                default:
                    return MapSingleAll(logical, warnings);
            }
        }

        private static IReadOnlyList<(CatalogExcelLogicalPort, string)> MapSingleAll(
            IReadOnlyList<CatalogExcelLogicalPort> logical,
            List<string> warnings)
        {
            string endType = logical[0].EndType;
            if (logical.Select(p => p.EndType).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
            {
                warnings.Add(
                    "Sheet uses port columns S-ALL but Port Manager / part has mixed end types — "
                    + $"using '{endType}' for EndType_S-ALL; use a dual-port template (S1/S2) for mixed ends.");
            }

            var port = new CatalogExcelLogicalPort(endType, "ALL");
            return new[] { (port, "S-ALL") };
        }

        private static IReadOnlyList<(CatalogExcelLogicalPort, string)> MapDual(
            IReadOnlyList<CatalogExcelLogicalPort> logical,
            List<string> warnings)
        {
            if (logical.Count > 2)
                warnings.Add($"Sheet has S1/S2 only — exporting first 2 of {logical.Count} Port Manager port(s).");

            var result = new List<(CatalogExcelLogicalPort, string)>();
            if (logical.Count >= 1)
                result.Add((logical[0], "S1"));
            if (logical.Count >= 2)
                result.Add((logical[1], "S2"));

            return result;
        }

        private static IReadOnlyList<(CatalogExcelLogicalPort, string)> MapTriple(
            IReadOnlyList<CatalogExcelLogicalPort> logical,
            List<string> warnings)
        {
            if (logical.Count > 3)
                warnings.Add($"Sheet has S1/S2/S3 only — exporting first 3 of {logical.Count} Port Manager port(s).");

            var suffixes = new[] { "S1", "S2", "S3" };
            var result = new List<(CatalogExcelLogicalPort, string)>();
            for (int i = 0; i < suffixes.Length; i++)
            {
                CatalogExcelLogicalPort port = i < logical.Count
                    ? logical[i]
                    : logical[^1];
                result.Add((port, suffixes[i]));
            }

            return result;
        }

        private static CatalogExcelPortLayout ResolveEffectiveLayout(
            CatalogExcelSheetPortSchema schema,
            IReadOnlyList<(CatalogExcelLogicalPort Port, string Suffix)> mapped)
        {
            return schema switch
            {
                CatalogExcelSheetPortSchema.SingleAll => CatalogExcelPortLayout.SingleAll,
                CatalogExcelSheetPortSchema.Triple => CatalogExcelPortLayout.TriplePortBv,
                CatalogExcelSheetPortSchema.Dual when mapped.Count >= 2
                    && mapped.Any(p => p.Port.EndType.Equals("BV", StringComparison.OrdinalIgnoreCase))
                    && mapped.Any(p => !p.Port.EndType.Equals("BV", StringComparison.OrdinalIgnoreCase))
                    => CatalogExcelPortLayout.DualPortBv,
                CatalogExcelSheetPortSchema.Dual => CatalogExcelPortLayout.DualFlange,
                _ => CatalogExcelPortLayout.SingleAll,
            };
        }

        private static string ResolvePressureClass(CatalogExcelPartRow row, ValveProject? project)
        {
            if (project != null
                && PartMatchesProject(row.Part.Id, project)
                && !string.IsNullOrWhiteSpace(project.Parameters.PressureClass))
            {
                return project.Parameters.PressureClass.Trim();
            }

            return row.PressureClass;
        }

        private static string? ResolveFlangeFacing(CatalogExcelPartRow row, ValveProject? project)
        {
            if (project != null
                && PartMatchesProject(row.Part.Id, project)
                && !string.IsNullOrWhiteSpace(project.FlangeFacing))
            {
                return CatalogFlangeFacing.Normalize(project.FlangeFacing);
            }

            if (!string.IsNullOrWhiteSpace(row.Part.FlangeFacing))
                return CatalogFlangeFacing.Normalize(row.Part.FlangeFacing);

            return null;
        }
    }
}
