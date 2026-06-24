using System;
using System.Collections.Generic;
using System.Linq;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer.Services
{
    internal static class CatalogPartFamilySuggestService
    {
        public static IReadOnlyList<string> ListPrimaryEndTypes(
            string? catalogCategory,
            string? pipingComponent,
            string? scriptName = null)
        {
            string category = CatalogCategories.NormalizeCategoryId(catalogCategory);
            string component = pipingComponent?.Trim() ?? "";
            string id = (scriptName ?? "").ToUpperInvariant();

            if (category.Equals(CatalogCategories.Flanges, StringComparison.OrdinalIgnoreCase))
                return ["FL", "LAP"];

            if (category.Equals(CatalogCategories.Valves, StringComparison.OrdinalIgnoreCase))
            {
                if (ContainsAny(id, "PSV", "RELIEF", "PRV", "SAFETY"))
                    return ["FL"];
                if (ContainsAny(id, "3WAY", "3_WAY", "THREE_WAY", "3-WAY"))
                    return ["FL", "BV", "SW"];
                if (id.Contains("ANGLE", StringComparison.Ordinal))
                    return ["BV", "FL"];
                if (ContainsAny(id, "BUTTERFLY", "WAFER"))
                    return ["WF", "LUG", "FL"];
                return ["FL", "BV", "SW", "WF", "LUG", "THDF", "GRV"];
            }

            if (category.Equals(CatalogCategories.Pipe, StringComparison.OrdinalIgnoreCase))
            {
                if (component.Equals("HDPE", StringComparison.OrdinalIgnoreCase))
                    return ["P", "SL", "BELL", "SPIG"];
                return ["BV", "PL", "P", "SW", "THDM"];
            }

            if (category.Equals(CatalogCategories.Fasteners, StringComparison.OrdinalIgnoreCase))
            {
                if (component.Equals("Gasket", StringComparison.OrdinalIgnoreCase))
                    return ["FL"];
                if (component.Equals("StubEnd", StringComparison.OrdinalIgnoreCase)
                    || component.Equals("Collar", StringComparison.OrdinalIgnoreCase)
                    || component.Equals("BackingRing", StringComparison.OrdinalIgnoreCase))
                {
                    return ["BV", "LAP"];
                }

                return ["FL", "BV"];
            }

            if (category.Equals(CatalogCategories.Olet, StringComparison.OrdinalIgnoreCase))
                return ["BV", "SW", "FL", "THDM"];

            if (category.Equals(CatalogCategories.Instruments, StringComparison.OrdinalIgnoreCase)
                || category.Equals(CatalogCategories.Actuators, StringComparison.OrdinalIgnoreCase))
            {
                return ["FL", "SW", "THDF", "THDM"];
            }

            if (category.Equals(CatalogCategories.Miscellaneous, StringComparison.OrdinalIgnoreCase))
                return ["BV", "FL", "THDM"];

            // Fittings (default)
            return ["BV", "SW", "THDM", "THDF", "FL"];
        }

        public static string InferPrimaryEndType(
            string? catalogCategory,
            string? pipingComponent,
            string? scriptName = null)
        {
            IReadOnlyList<string> options = ListPrimaryEndTypes(catalogCategory, pipingComponent, scriptName);
            if (options.Count == 0)
                return "Undefined_ET";

            string category = CatalogCategories.NormalizeCategoryId(catalogCategory);
            string component = pipingComponent?.Trim() ?? "";

            if (category.Equals(CatalogCategories.Flanges, StringComparison.OrdinalIgnoreCase))
                return "FL";

            if (category.Equals(CatalogCategories.Valves, StringComparison.OrdinalIgnoreCase))
            {
                string id = (scriptName ?? "").ToUpperInvariant();
                if (ContainsAny(id, "BUTTERFLY", "WAFER"))
                    return options.Contains("WF", StringComparer.OrdinalIgnoreCase) ? "WF" : options[0];
                if (ContainsAny(id, "SOCKET", "_SW_"))
                    return "SW";
                if (ContainsAny(id, "BW", "BUTT", "_BV_"))
                    return "BV";
                return "FL";
            }

            if (category.Equals(CatalogCategories.Fasteners, StringComparison.OrdinalIgnoreCase)
                && component.Equals("Gasket", StringComparison.OrdinalIgnoreCase))
            {
                return "FL";
            }

            if (category.Equals(CatalogCategories.Fasteners, StringComparison.OrdinalIgnoreCase)
                && (component.Equals("StubEnd", StringComparison.OrdinalIgnoreCase)
                    || component.Equals("Collar", StringComparison.OrdinalIgnoreCase)))
            {
                return "BV";
            }

            if (category.Equals(CatalogCategories.Pipe, StringComparison.OrdinalIgnoreCase)
                && component.Equals("HDPE", StringComparison.OrdinalIgnoreCase))
            {
                return "P";
            }

            return options[0];
        }

        public static IReadOnlyList<string> ListFacingOptions(
            string? catalogCategory,
            string? pipingComponent,
            string? primaryEndType,
            string? scriptName = null,
            string? excelCloneSourcePartId = null)
        {
            if (!CatalogFlangeFacing.PrimaryEndUsesFacing(primaryEndType))
                return Array.Empty<string>();

            string clone = (excelCloneSourcePartId ?? "").ToUpperInvariant();
            string id = (scriptName ?? "").ToUpperInvariant();

            if (clone.Contains("GSK_FF", StringComparison.Ordinal) || clone.Contains("_FF_CL", StringComparison.Ordinal))
                return ["FF"];

            if (clone.Contains("GSK_RF", StringComparison.Ordinal)
                || clone.Contains("_RF_CL", StringComparison.Ordinal)
                || clone.Contains("_FLRF_", StringComparison.Ordinal))
            {
                return ["RF"];
            }

            if (id.Contains("FF", StringComparison.Ordinal) && !id.Contains("FLRF", StringComparison.Ordinal))
                return ["FF"];

            if (id.Contains("RF", StringComparison.Ordinal))
                return ["RF"];

            string category = CatalogCategories.NormalizeCategoryId(catalogCategory);
            string component = pipingComponent?.Trim() ?? "";
            if (category.Equals(CatalogCategories.Fasteners, StringComparison.OrdinalIgnoreCase)
                && component.Equals("Gasket", StringComparison.OrdinalIgnoreCase))
            {
                return ["RF", "FF"];
            }

            return ["RF"];
        }

        public static string InferFacing(
            string? catalogCategory,
            string? pipingComponent,
            string? primaryEndType,
            string? scriptName = null,
            string? excelCloneSourcePartId = null,
            string? savedFacing = null)
        {
            IReadOnlyList<string> options = ListFacingOptions(
                catalogCategory,
                pipingComponent,
                primaryEndType,
                scriptName,
                excelCloneSourcePartId);

            if (options.Count == 0)
                return "RF";

            string normalized = CatalogFlangeFacing.Normalize(savedFacing);
            if (options.Any(x => x.Equals(normalized, StringComparison.OrdinalIgnoreCase)))
                return normalized;

            return options[0];
        }

        public static bool TemplateMatchesPartFamily(
            string templatePartId,
            string? catalogCategory,
            string? pipingComponent,
            string? primaryEndType)
        {
            if (string.IsNullOrWhiteSpace(templatePartId))
                return false;

            string category = CatalogCategories.NormalizeCategoryId(catalogCategory);
            string component = pipingComponent?.Trim() ?? "";
            string end = Plant3DEndTypes.NormalizeCode(primaryEndType);
            string id = templatePartId.ToUpperInvariant();

            if (CatalogValveExcelTemplates.IsValveTemplate(id))
            {
                return category.Equals(CatalogCategories.Valves, StringComparison.OrdinalIgnoreCase)
                    && CatalogValveExcelTemplates.MatchesPrimaryEnd(templatePartId, primaryEndType);
            }

            if (category.Equals(CatalogCategories.Valves, StringComparison.OrdinalIgnoreCase))
                return false;

            if (category.Equals(CatalogCategories.Flanges, StringComparison.OrdinalIgnoreCase))
            {
                return id.StartsWith("WN_", StringComparison.Ordinal)
                    || id.StartsWith("SO_", StringComparison.Ordinal)
                    || id.StartsWith("BLD_", StringComparison.Ordinal);
            }

            if (category.Equals(CatalogCategories.Fasteners, StringComparison.OrdinalIgnoreCase))
            {
                if (component.Equals("Gasket", StringComparison.OrdinalIgnoreCase))
                    return id.StartsWith("GSK_", StringComparison.Ordinal);
                if (component.Equals("StubEnd", StringComparison.OrdinalIgnoreCase))
                    return id.StartsWith("STUBEND_", StringComparison.Ordinal);
                if (component.Equals("Collar", StringComparison.OrdinalIgnoreCase)
                    || component.Equals("BackingRing", StringComparison.OrdinalIgnoreCase))
                {
                    return id.StartsWith("LJ_RING_", StringComparison.Ordinal)
                        || id.StartsWith("COLLAR_", StringComparison.Ordinal);
                }
            }

            if (!MatchesEndType(id, end))
                return false;

            return MatchesComponent(id, component);
        }

        /// <summary>Match template part id to Class/Sch from Part Family (not script name).</summary>
        public static bool TemplateMatchesClassSchedule(
            string templatePartId,
            string? primaryEndType,
            string? pressureClass,
            string? pipeSchedule)
        {
            if (string.IsNullOrWhiteSpace(templatePartId))
                return false;

            string id = templatePartId.ToUpperInvariant();
            string end = Plant3DEndTypes.NormalizeCode(primaryEndType);
            string pc = NormalizePressureClassToken(pressureClass);
            string sch = PipeScheduleCatalog.Normalize(pipeSchedule ?? "");

            // Valve clone sheets use CL150 / CL3000 — not pipe SCH rows.
            if (CatalogValveExcelTemplates.IsValveTemplate(id))
            {
                if (string.IsNullOrEmpty(pc))
                    pc = "150";
                if (id.Contains($"CL{pc}", StringComparison.Ordinal))
                    return true;
                return !id.Contains("CL150", StringComparison.Ordinal)
                    && !id.Contains("CL3000", StringComparison.Ordinal);
            }

            if (IsScheduleEnd(end))
            {
                if (string.IsNullOrEmpty(sch))
                    sch = "40";
                if (id.Contains($"SCH{sch}", StringComparison.Ordinal))
                    return true;
                return !id.Contains("SCH", StringComparison.Ordinal);
            }

            if (IsSwEnd(end))
            {
                if (pc == "3000")
                    return id.Contains("CL3000", StringComparison.Ordinal) || id.Contains("_SW_", StringComparison.Ordinal);
                return id.Contains("_SW_", StringComparison.Ordinal) || id.Contains("CL3000", StringComparison.Ordinal);
            }

            if (!string.IsNullOrEmpty(pc) && id.Contains($"CL{pc}", StringComparison.Ordinal))
                return true;

            return !id.Contains("CL150", StringComparison.Ordinal)
                && !id.Contains("CL3000", StringComparison.Ordinal)
                && !id.Contains("SCH", StringComparison.Ordinal);
        }

        private static string NormalizePressureClassToken(string? pressureClass)
        {
            string pc = (pressureClass ?? "150").Trim();
            if (pc.StartsWith("CL", StringComparison.OrdinalIgnoreCase))
                pc = pc[2..];
            return pc;
        }

        private static bool MatchesEndType(string templateId, string end)
        {
            if (IsSwEnd(end))
                return templateId.Contains("_SW_", StringComparison.Ordinal);

            if (IsScheduleEnd(end))
            {
                return templateId.Contains("_BW_", StringComparison.Ordinal)
                    || templateId.Contains("SCH40", StringComparison.Ordinal);
            }

            if (IsThreadedEnd(end))
            {
                return templateId.Contains("THD", StringComparison.Ordinal)
                    || templateId.Contains("THREAD", StringComparison.Ordinal);
            }

            if (IsFlangedEnd(end))
            {
                return templateId.StartsWith("WN_", StringComparison.Ordinal)
                    || templateId.StartsWith("SO_", StringComparison.Ordinal)
                    || templateId.StartsWith("BLD_", StringComparison.Ordinal)
                    || templateId.StartsWith("GSK_", StringComparison.Ordinal);
            }

            if (IsPeEnd(end))
                return templateId.Contains("PIPE", StringComparison.Ordinal);

            return true;
        }

        private static bool MatchesComponent(string templateId, string component)
        {
            if (string.IsNullOrWhiteSpace(component))
                return true;

            if (component.Equals("Elbow", StringComparison.OrdinalIgnoreCase))
                return templateId.Contains("ELBOW", StringComparison.Ordinal);

            if (component.Equals("Tee", StringComparison.OrdinalIgnoreCase))
                return templateId.Contains("TEE", StringComparison.Ordinal);

            if (component.Equals("Reducer", StringComparison.OrdinalIgnoreCase))
                return templateId.Contains("REDUCER", StringComparison.Ordinal);

            if (component.Equals("Cross", StringComparison.OrdinalIgnoreCase))
                return templateId.Contains("CROSS", StringComparison.Ordinal);

            if (component.Equals("Cap", StringComparison.OrdinalIgnoreCase))
                return templateId.Contains("CAP", StringComparison.Ordinal);

            if (component.Equals("Flange", StringComparison.OrdinalIgnoreCase))
                return templateId.StartsWith("WN_", StringComparison.Ordinal);

            if (component.Equals("BlindFlange", StringComparison.OrdinalIgnoreCase))
                return templateId.StartsWith("BLD_", StringComparison.Ordinal);

            if (component.Equals("Gasket", StringComparison.OrdinalIgnoreCase))
                return templateId.StartsWith("GSK_", StringComparison.Ordinal);

            if (component.Equals("StubEnd", StringComparison.OrdinalIgnoreCase))
                return templateId.StartsWith("STUBEND_", StringComparison.Ordinal);

            return true;
        }

        private static bool IsScheduleEnd(string end) =>
            end.Equals("BV", StringComparison.OrdinalIgnoreCase)
            || end.Equals("PL", StringComparison.OrdinalIgnoreCase)
            || end.Equals("PPL", StringComparison.OrdinalIgnoreCase)
            || end.Equals("BW", StringComparison.OrdinalIgnoreCase);

        private static bool IsSwEnd(string end) =>
            end.Equals("SW", StringComparison.OrdinalIgnoreCase)
            || end.Equals("PSW", StringComparison.OrdinalIgnoreCase);

        private static bool IsThreadedEnd(string end) =>
            end.Equals("THDM", StringComparison.OrdinalIgnoreCase)
            || end.Equals("THDF", StringComparison.OrdinalIgnoreCase)
            || end.Equals("TAP", StringComparison.OrdinalIgnoreCase);

        private static bool IsFlangedEnd(string end) =>
            end.Equals("FL", StringComparison.OrdinalIgnoreCase)
            || end.Equals("SO", StringComparison.OrdinalIgnoreCase)
            || end.Equals("WF", StringComparison.OrdinalIgnoreCase)
            || end.Equals("LFL", StringComparison.OrdinalIgnoreCase)
            || end.Equals("LLP", StringComparison.OrdinalIgnoreCase)
            || end.Equals("LAP", StringComparison.OrdinalIgnoreCase)
            || end.Equals("LUG", StringComparison.OrdinalIgnoreCase);

        private static bool IsPeEnd(string end) =>
            end.Equals("P", StringComparison.OrdinalIgnoreCase)
            || end.Equals("SL", StringComparison.OrdinalIgnoreCase);

        private static bool ContainsAny(string haystack, params string[] needles)
        {
            foreach (string needle in needles)
            {
                if (haystack.Contains(needle, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }
    }
}
