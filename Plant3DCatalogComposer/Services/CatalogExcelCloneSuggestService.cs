using System;
using System.Collections.Generic;
using System.Linq;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer.Services
{
    internal static class CatalogExcelCloneSuggestService
    {
        public static string InferCloneSourcePartId(
            string? catalogCategory,
            string? pipingComponent,
            string? primaryEndType,
            string? pressureClass,
            string? pipeSchedule,
            string standardSet = "",
            string activateGroup = "")
        {
            IReadOnlyList<string> ranked = ListForPartFamily(
                catalogCategory,
                pipingComponent,
                primaryEndType,
                pressureClass,
                pipeSchedule);
            if (ranked.Count > 0)
                return ranked[0];

            string category = CatalogCategories.NormalizeCategoryId(catalogCategory);
            string group = string.IsNullOrWhiteSpace(activateGroup)
                ? CatalogPartFamilyOptions.ResolveActivateGroup(category, pipingComponent ?? "")
                : activateGroup;

            return CatalogExcelTemplateService.InferLegacyCloneSourcePartId(
                "",
                pipingComponent ?? "",
                standardSet,
                group,
                primaryEndType,
                pressureClass,
                pipeSchedule);
        }

        /// <summary>Excel from list — matched by Part Family fields only (not catalog script name).</summary>
        public static IReadOnlyList<string> ListForPartFamily(
            string? catalogCategory,
            string? pipingComponent,
            string? primaryEndType,
            string? pressureClass,
            string? pipeSchedule)
        {
            string category = CatalogCategories.NormalizeCategoryId(catalogCategory);
            string component = pipingComponent?.Trim() ?? "";
            string end = Plant3DEndTypes.NormalizeCode(primaryEndType);

            // Valves: always offer the canonical valve clone templates (best end/class match first).
            // The physical sheet is imported on demand by EnsurePartSheet, so the dropdown does not
            // depend on whether the workbook already contains VALVE_* sheets.
            if (IsValveFamily(category, null))
                return RankAllValveTemplates(end, pressureClass);

            IEnumerable<string> candidates = SafeListAllTemplates()
                .Where(id => IsKnownCloneSource(id, category));

            // Exact match: family + end + class/schedule.
            var matched = candidates
                .Where(id => CatalogPartFamilySuggestService.TemplateMatchesPartFamily(id, category, component, end))
                .Where(id => CatalogPartFamilySuggestService.TemplateMatchesClassSchedule(id, end, pressureClass, pipeSchedule))
                .ToList();

            IReadOnlyList<string> ranked = RankMatches(category, component, end, pressureClass, pipeSchedule, matched);
            if (ranked.Count > 0)
                return ranked;

            // Relax class/schedule — still family + end matched.
            matched = candidates
                .Where(id => CatalogPartFamilySuggestService.TemplateMatchesPartFamily(id, category, component, end))
                .ToList();

            return RankMatches(category, component, end, pressureClass, pipeSchedule, matched);
        }

        /// <summary>Best-matching valve clone first, then the remaining valve templates.</summary>
        private static IReadOnlyList<string> RankAllValveTemplates(string end, string? pressureClass)
        {
            var ordered = new List<string>(RankValveTemplates(end, pressureClass));
            foreach (string id in CatalogValveExcelTemplates.All)
            {
                if (!ordered.Any(x => x.Equals(id, StringComparison.OrdinalIgnoreCase)))
                    ordered.Add(id);
            }

            return ordered;
        }

        private static IReadOnlyList<string> RankMatches(
            string category,
            string component,
            string end,
            string? pressureClass,
            string? pipeSchedule,
            IReadOnlyList<string> matched)
        {
            if (matched.Count == 0)
                return Array.Empty<string>();

            string group = CatalogPartFamilyOptions.ResolveActivateGroup(category, component);
            string inferred = CatalogExcelTemplateService.InferLegacyCloneSourcePartId(
                "",
                component,
                "",
                group,
                end,
                pressureClass,
                pipeSchedule);

            var ordered = new List<string>();
            AddIfPresent(ordered, inferred, matched);
            foreach (string id in matched.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                AddIfPresent(ordered, id, matched);

            return ordered;
        }

        /// <summary>Rank generic valve clone sheets by primary end type and pressure class.</summary>
        private static IReadOnlyList<string> RankValveTemplates(string? primaryEndType, string? pressureClass)
        {
            string end = Plant3DEndTypes.NormalizeCode(primaryEndType);
            string pc = NormalizePressureClass(pressureClass);

            if (end.Equals("BV", StringComparison.OrdinalIgnoreCase)
                || end.Equals("PL", StringComparison.OrdinalIgnoreCase))
            {
                return PreferPressureClass(
                    [CatalogValveExcelTemplates.BvCl150, CatalogValveExcelTemplates.Angle],
                    pc);
            }

            if (end.Equals("SW", StringComparison.OrdinalIgnoreCase)
                || end.Equals("THDF", StringComparison.OrdinalIgnoreCase)
                || end.Equals("THDM", StringComparison.OrdinalIgnoreCase))
            {
                return PreferPressureClass(
                    [CatalogValveExcelTemplates.SwCl3000],
                    pc,
                    fallbackClass: "3000");
            }

            if (end.Equals("WF", StringComparison.OrdinalIgnoreCase)
                || end.Equals("LUG", StringComparison.OrdinalIgnoreCase))
            {
                return PreferPressureClass(
                    [CatalogValveExcelTemplates.FlRich, CatalogValveExcelTemplates.FlSimple],
                    pc);
            }

            if (end.Equals("FL", StringComparison.OrdinalIgnoreCase)
                || end.Equals("SO", StringComparison.OrdinalIgnoreCase)
                || end.Equals("Undefined_ET", StringComparison.OrdinalIgnoreCase))
            {
                return PreferPressureClass(
                    [CatalogValveExcelTemplates.FlSimple, CatalogValveExcelTemplates.FlRich],
                    pc);
            }

            return PreferPressureClass([CatalogValveExcelTemplates.FlSimple], pc);
        }

        private static IReadOnlyList<string> PreferPressureClass(
            IReadOnlyList<string> templates,
            string pressureClass,
            string? fallbackClass = null)
        {
            string pc = pressureClass.Length > 0 ? pressureClass : fallbackClass ?? "150";
            string token = $"CL{pc}";

            var exact = templates
                .Where(id => id.Contains(token, StringComparison.OrdinalIgnoreCase))
                .ToList();
            return exact.Count > 0 ? exact : templates.ToList();
        }

        private static string NormalizePressureClass(string? pressureClass)
        {
            string pc = (pressureClass ?? "150").Trim();
            if (pc.StartsWith("CL", StringComparison.OrdinalIgnoreCase))
                pc = pc[2..];
            return pc;
        }

        private static bool IsKnownCloneSource(string templatePartId, string category)
        {
            if (string.IsNullOrWhiteSpace(templatePartId))
                return false;

            if (IsValveFamily(category, null))
            {
                return CatalogValveExcelTemplates.All.Any(x =>
                    x.Equals(templatePartId, StringComparison.OrdinalIgnoreCase));
            }

            if (templatePartId.StartsWith("VALVE_", StringComparison.OrdinalIgnoreCase))
                return false;

            if (BwSch40StandardCatalog.IsStandardPart(templatePartId)
                || SwCl3000StandardCatalog.IsStandardPart(templatePartId))
            {
                return true;
            }

            if (templatePartId.StartsWith("WN_", StringComparison.OrdinalIgnoreCase)
                || templatePartId.StartsWith("SO_", StringComparison.OrdinalIgnoreCase)
                || templatePartId.StartsWith("BLD_", StringComparison.OrdinalIgnoreCase)
                || templatePartId.StartsWith("GSK_", StringComparison.OrdinalIgnoreCase)
                || templatePartId.StartsWith("STUBEND_", StringComparison.OrdinalIgnoreCase)
                || templatePartId.StartsWith("LJ_RING_", StringComparison.OrdinalIgnoreCase)
                || templatePartId.StartsWith("PIPE_", StringComparison.OrdinalIgnoreCase)
                || templatePartId.StartsWith("STUD_", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return StandardCatalogGuard.IsProtectedStandardPart(templatePartId);
        }

        private static bool IsValveFamily(string? catalogCategory, string? activateGroup) =>
            CatalogCategories.NormalizeCategoryId(catalogCategory)
                .Equals(CatalogCategories.Valves, StringComparison.OrdinalIgnoreCase)
            || string.Equals(activateGroup, "Valve", StringComparison.OrdinalIgnoreCase);

        private static IReadOnlyList<string> SafeListAllTemplates()
        {
            try
            {
                return CatalogExcelTemplateService.ListTemplatePartIds();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        private static void AddIfPresent(List<string> target, string id, IReadOnlyList<string> pool)
        {
            if (string.IsNullOrWhiteSpace(id))
                return;

            if (!pool.Any(x => x.Equals(id, StringComparison.OrdinalIgnoreCase)))
                return;

            if (target.Any(x => x.Equals(id, StringComparison.OrdinalIgnoreCase)))
                return;

            target.Add(id);
        }
    }
}
