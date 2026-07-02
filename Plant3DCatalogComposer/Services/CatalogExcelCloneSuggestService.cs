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

            // A user-configured catalog (e.g. CATA_NUI.xlsx) is a curated workbook whose sheet names
            // do not follow the legacy "{PartId},{End},{Class}" convention. Trust every curated sheet
            // and surface it filtered only by Component / Class-Sch (Primary End is intentionally ignored).
            if (CatalogTemplateSettings.ResolveConfiguredTemplatePath() != null)
                return ListFromConfiguredTemplate(category, component, pressureClass, pipeSchedule);

            // Valves: always offer the canonical valve clone templates (best end/class match first).
            // The physical sheet is imported on demand by EnsurePartSheet, so the dropdown does not
            // depend on whether the workbook already contains VALVE_* sheets.
            if (IsValveFamily(category, null))
                return RankAllValveTemplates(end, pressureClass);

            IEnumerable<string> candidates = SafeListAllTemplates()
                .Where(id => IsKnownCloneSource(id, category));

            // Primary End type is intentionally NOT used to filter the "Excel from:" list — the
            // dropdown is constrained only by Category / Component and the explicit Class/Sch field.
            // (end is still used below for ranking the default selection.)
            var matched = candidates
                .Where(id => CatalogPartFamilySuggestService.TemplateMatchesPartFamily(id, category, component, ""))
                .Where(id => CatalogPartFamilySuggestService.TemplateMatchesClassScheduleNoEnd(id, pressureClass, pipeSchedule))
                .ToList();

            IReadOnlyList<string> ranked = RankMatches(category, component, end, pressureClass, pipeSchedule, matched);
            if (ranked.Count > 0)
                return ranked;

            // Relax class/schedule — still family matched (end-neutral).
            matched = candidates
                .Where(id => CatalogPartFamilySuggestService.TemplateMatchesPartFamily(id, category, component, ""))
                .ToList();

            return RankMatches(category, component, end, pressureClass, pipeSchedule, matched);
        }

        /// <summary>
        /// Suggestion list for a user-configured catalog workbook. Every non-meta sheet is a valid
        /// clone source; the list is narrowed by a loose Component/Category keyword and the explicit
        /// Class/Sch, never by Primary End type. Falls back to the full list if a filter empties it.
        /// </summary>
        private static IReadOnlyList<string> ListFromConfiguredTemplate(
            string category,
            string component,
            string? pressureClass,
            string? pipeSchedule)
        {
            IReadOnlyList<string> all = SafeListAllTemplates();
            if (all.Count == 0)
                return all;

            var byComponent = all
                .Where(id => MatchesLooseComponent(id, category, component))
                .ToList();
            List<string> pool = byComponent.Count > 0 ? byComponent : all.ToList();

            var byClass = pool
                .Where(id => CatalogPartFamilySuggestService.TemplateMatchesClassScheduleNoEnd(
                    id, pressureClass, pipeSchedule))
                .ToList();
            List<string> final = byClass.Count > 0 ? byClass : pool;

            return final
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>Keyword match tolerant of real catalog sheet names ("FLANGE WN_RF_CL150", "ELBOW 90_BV_SCH40").</summary>
        private static bool MatchesLooseComponent(string templatePartId, string category, string component)
        {
            string id = (templatePartId ?? "").ToUpperInvariant();
            string comp = component?.Trim() ?? "";

            string[] componentKeywords = comp.ToUpperInvariant() switch
            {
                "ELBOW" => ["ELBOW", "BEND"],
                "TEE" => ["TEE"],
                "REDUCER" => ["REDUCER", "RED "],
                "CROSS" => ["CROSS"],
                "CAP" => ["CAP"],
                "FLANGE" => ["FLANGE", "WN_", "WN "],
                "BLINDFLANGE" => ["BLIND", "BLD"],
                "GASKET" => ["GSK", "GASKET"],
                "STUBEND" => ["STUBEND", "STUB"],
                _ => [],
            };
            if (componentKeywords.Length > 0)
                return componentKeywords.Any(k => id.Contains(k, StringComparison.Ordinal));

            string[] categoryKeywords = category switch
            {
                _ when category.Equals(CatalogCategories.Flanges, StringComparison.OrdinalIgnoreCase)
                    => ["FLANGE", "WN", "SO_", "BLD", "BLIND"],
                _ when category.Equals(CatalogCategories.Fasteners, StringComparison.OrdinalIgnoreCase)
                    => ["GSK", "GASKET", "STUD", "BOLT", "NUT", "STUBEND"],
                _ when category.Equals(CatalogCategories.Pipe, StringComparison.OrdinalIgnoreCase)
                    => ["PIPE"],
                _ when category.Equals(CatalogCategories.Fittings, StringComparison.OrdinalIgnoreCase)
                    => ["ELBOW", "TEE", "REDUCER", "CROSS", "CAP", "BEND"],
                _ => [],
            };
            if (categoryKeywords.Length > 0)
                return categoryKeywords.Any(k => id.Contains(k, StringComparison.Ordinal));

            return true;
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
