using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer.Services
{
    internal static class CatalogExcelTemplateService
    {
        private const int MaxSheetNameLength = 31;

        private static readonly object TemplateCacheLock = new();
        private static string? _cachedTemplatePath;
        private static DateTime _cachedTemplateWriteUtc;
        private static IReadOnlyList<string>? _cachedTemplatePartIds;

        public static string ResolveTemplatePath() => CatalogExcelExportService.ResolveTemplatePath();

        /// <summary>Preload sheet ids (background-friendly) so the palette opens faster.</summary>
        public static void WarmTemplateCache()
        {
            try
            {
                _ = ListTemplatePartIds();
            }
            catch
            {
                // optional warm-up
            }
        }

        public static void InvalidateTemplateCache()
        {
            lock (TemplateCacheLock)
            {
                _cachedTemplatePartIds = null;
                _cachedTemplatePath = null;
            }
        }

        public static IReadOnlyList<string> ListTemplatePartIds()
        {
            string path = ResolveTemplatePath();
            if (!File.Exists(path))
                return Array.Empty<string>();

            DateTime writeUtc = File.GetLastWriteTimeUtc(path);
            lock (TemplateCacheLock)
            {
                if (_cachedTemplatePartIds != null
                    && _cachedTemplatePath != null
                    && _cachedTemplatePath.Equals(path, StringComparison.OrdinalIgnoreCase)
                    && _cachedTemplateWriteUtc == writeUtc)
                {
                    return _cachedTemplatePartIds;
                }
            }

            using var workbook = new XLWorkbook(path);
            var ids = workbook.Worksheets
                .Select(w => w.Name)
                .Where(n => n.Contains(',', StringComparison.Ordinal))
                .Select(n => n.Split(',')[0].Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();

            lock (TemplateCacheLock)
            {
                _cachedTemplatePath = path;
                _cachedTemplateWriteUtc = writeUtc;
                _cachedTemplatePartIds = ids;
            }

            return ids;
        }

        public static string? InferCloneSourcePartId(
            string partId,
            string pnpClassName,
            string standardSet,
            string group)
            => InferCloneSourcePartId(partId, null, pnpClassName, standardSet, group, null, null, null);

        public static string InferCloneSourcePartId(
            string partId,
            string? catalogCategory,
            string pnpClassName,
            string standardSet,
            string group,
            string? primaryEndType,
            string? pressureClass = null,
            string? pipeSchedule = null)
            => CatalogExcelCloneSuggestService.InferCloneSourcePartId(
                catalogCategory,
                pnpClassName,
                primaryEndType,
                pressureClass,
                pipeSchedule,
                standardSet,
                group);

        internal static string InferLegacyCloneSourcePartId(
            string partId,
            string pnpClassName,
            string standardSet,
            string group,
            string? primaryEndType = null,
            string? pressureClass = null,
            string? pipeSchedule = null)
        {
            string pnp = pnpClassName.Trim();
            string set = standardSet.Trim();
            string end = Plant3DEndTypes.NormalizeCode(primaryEndType);

            if (set.Equals(BwSch40StandardCatalog.SetId, StringComparison.OrdinalIgnoreCase)
                || IsScheduleEnd(end))
            {
                return pnp switch
                {
                    "Elbow" => "ELBOW_90_LR_BW_SCH40",
                    "Tee" => "TEE_EQ_BW_SCH40",
                    "Reducer" => "REDUCER_CONC_BW_SCH40",
                    "StubEnd" => "STUBEND_LJ_A_BW_SCH40",
                    _ => "ELBOW_90_LR_BW_SCH40",
                };
            }

            if (set.Equals(SwCl3000StandardCatalog.SetId, StringComparison.OrdinalIgnoreCase)
                || IsSwEnd(end))
            {
                return pnp switch
                {
                    "Elbow" => "ELBOW_90_SW_CL3000",
                    "Tee" => "TEE_EQ_SW_CL3000",
                    _ => "ELBOW_90_SW_CL3000",
                };
            }

            if (group.Equals("Flange", StringComparison.OrdinalIgnoreCase))
            {
                if (pnp.Equals("BlindFlange", StringComparison.OrdinalIgnoreCase))
                    return "BLD_FLRF_CL150";
                if (end.Equals("LAP", StringComparison.OrdinalIgnoreCase))
                    return "LJ_RING_CL150_RF";
                if (end.Equals("SO", StringComparison.OrdinalIgnoreCase))
                    return "SO_FLRF_CL150";
                return "WN_FLRF_CL150";
            }

            if (group.Equals("Gasket", StringComparison.OrdinalIgnoreCase))
            {
                return end.Equals("FF", StringComparison.OrdinalIgnoreCase)
                    ? "GSK_FF_CL150"
                    : "GSK_RF_CL150";
            }

            if (group.Equals("Valve", StringComparison.OrdinalIgnoreCase))
            {
                IReadOnlyList<string> valves = CatalogExcelCloneSuggestService.ListForPartFamily(
                    CatalogCategories.Valves,
                    pnp,
                    end,
                    pressureClass,
                    pipeSchedule);
                if (valves.Count > 0)
                    return valves[0];
                return CatalogValveExcelTemplates.FlSimple;
            }

            return "ELBOW_90_LR_BW_SCH40";
        }

        private static bool IsScheduleEnd(string end) =>
            end.Equals("BV", StringComparison.OrdinalIgnoreCase)
            || end.Equals("PL", StringComparison.OrdinalIgnoreCase)
            || end.Equals("PPL", StringComparison.OrdinalIgnoreCase)
            || end.Equals("BW", StringComparison.OrdinalIgnoreCase);

        private static bool IsSwEnd(string end) =>
            end.Equals("SW", StringComparison.OrdinalIgnoreCase)
            || end.Equals("PSW", StringComparison.OrdinalIgnoreCase);

        public static IReadOnlyList<string> ListForPartFamily(
            string? catalogCategory,
            string? pipingComponent,
            string? primaryEndType,
            string? pressureClass,
            string? pipeSchedule = null)
            => CatalogExcelCloneSuggestService.ListForPartFamily(
                catalogCategory,
                pipingComponent,
                primaryEndType,
                pressureClass,
                pipeSchedule);

        public static bool EnsurePartSheet(
            string partId,
            string cloneSourcePartId,
            out string? sheetName,
            out string? error)
        {
            sheetName = null;
            error = null;
            partId = partId.Trim();
            cloneSourcePartId = cloneSourcePartId.Trim();

            if (string.IsNullOrEmpty(partId))
            {
                error = "Part id is required.";
                return false;
            }

            if (string.IsNullOrEmpty(cloneSourcePartId))
            {
                error = "Select an Excel clone source part.";
                return false;
            }

            string templatePath = ResolveTemplatePath();
            using var workbook = new XLWorkbook(templatePath);

            IXLWorksheet? existing = FindSheetByPartPrefix(workbook, partId);
            if (existing != null)
            {
                sheetName = existing.Name;
                return true;
            }

            IXLWorksheet? source = FindSheetByPartPrefix(workbook, cloneSourcePartId);
            if (source == null && TryImportCloneSourceSheet(workbook, cloneSourcePartId, templatePath))
            {
                SaveTemplateWorkbook(workbook, templatePath);
                source = FindSheetByPartPrefix(workbook, cloneSourcePartId);
            }

            if (source == null)
            {
                error =
                    $"No template sheet for clone source '{cloneSourcePartId}' in:\n{templatePath}\n" +
                    "Rebuild/redeploy the plugin so Resources/CatalogBuilderTemplate.xlsx includes valve sheets, " +
                    "or set deploy.json CatalogGenerator to your API repo.";
                return false;
            }

            string newName = BuildSheetName(partId, source.Name, cloneSourcePartId);
            if (workbook.Worksheets.Any(w => w.Name.Equals(newName, StringComparison.OrdinalIgnoreCase)))
            {
                sheetName = newName;
                return true;
            }

            source.CopyTo(newName);
            IXLWorksheet cloned = workbook.Worksheet(newName);
            ReplacePartTokens(cloned, cloneSourcePartId, partId);
            SaveTemplateWorkbook(workbook, templatePath);
            sheetName = newName;
            return true;
        }

        private static void SaveTemplateWorkbook(XLWorkbook workbook, string templatePath)
        {
            workbook.SaveAs(templatePath);
            InvalidateTemplateCache();
        }

        /// <summary>Remove template sheets for parts no longer present under catalog_generator/parts.</summary>
        public static int RemoveOrphanedPartSheets(IReadOnlySet<string> activePartIds)
        {
            string templatePath = ResolveTemplatePath();
            using var workbook = new XLWorkbook(templatePath);
            var toRemove = new List<IXLWorksheet>();

            foreach (IXLWorksheet sheet in workbook.Worksheets)
            {
                if (!TryParseSheetPartId(sheet.Name, out string partId))
                    continue;

                if (!activePartIds.Contains(partId))
                    toRemove.Add(sheet);
            }

            if (toRemove.Count == 0)
                return 0;

            foreach (IXLWorksheet sheet in toRemove)
                sheet.Delete();

            SaveTemplateWorkbook(workbook, templatePath);
            return toRemove.Count;
        }

        private static bool TryParseSheetPartId(string sheetName, out string partId)
        {
            partId = "";
            int comma = sheetName.IndexOf(',');
            if (comma <= 0)
                return false;

            partId = sheetName[..comma].Trim();
            return !string.IsNullOrEmpty(partId);
        }

        internal static IXLWorksheet? FindSheetByPartPrefix(XLWorkbook workbook, string partId)
        {
            string prefix = partId + ",";
            return workbook.Worksheets
                .FirstOrDefault(w => w.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        }

        private static bool TryImportCloneSourceSheet(
            XLWorkbook targetWorkbook,
            string cloneSourcePartId,
            string activeTemplatePath)
        {
            foreach (string path in EnumerateTemplateSearchPaths(activeTemplatePath))
            {
                using var altWorkbook = new XLWorkbook(path);
                IXLWorksheet? source = FindSheetByPartPrefix(altWorkbook, cloneSourcePartId);
                if (source == null)
                    continue;

                string importName = source.Name;
                if (FindSheetByPartPrefix(targetWorkbook, cloneSourcePartId) != null)
                    return true;

                if (targetWorkbook.Worksheets.Any(w =>
                        w.Name.Equals(importName, StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }

                source.CopyTo(targetWorkbook, importName);
                return true;
            }

            return false;
        }

        private static IEnumerable<string> EnumerateTemplateSearchPaths(string activeTemplatePath)
        {
            var paths = new List<string>();
            void Add(string? path)
            {
                if (string.IsNullOrWhiteSpace(path))
                    return;
                if (!File.Exists(path))
                    return;
                if (paths.Any(p => p.Equals(path, StringComparison.OrdinalIgnoreCase)))
                    return;
                paths.Add(path);
            }

            Add(CatalogExcelExportService.DevTemplatePath());
            Add(CatalogExcelExportService.PluginTemplatePath());
            Add(activeTemplatePath);
            return paths;
        }

        private static string BuildSheetName(string partId, string sourceSheetName, string cloneSourcePartId)
        {
            string suffix = sourceSheetName.Length > cloneSourcePartId.Length
                ? sourceSheetName.Substring(cloneSourcePartId.Length)
                : ",FL,40";

            string candidate = partId + suffix;
            if (candidate.Length <= MaxSheetNameLength)
                return candidate;

            int keepSuffix = Math.Max(0, MaxSheetNameLength - partId.Length);
            if (keepSuffix == 0)
                return partId.Length <= MaxSheetNameLength ? partId : partId[..MaxSheetNameLength];

            return partId + suffix[..Math.Min(suffix.Length, keepSuffix)];
        }

        private static void ReplacePartTokens(IXLWorksheet sheet, string oldId, string newId)
        {
            string oldCust = "CUST_" + oldId;
            string newCust = "CUST_" + newId;

            foreach (IXLCell cell in sheet.CellsUsed())
            {
                if (cell.Value.IsText)
                {
                    string text = cell.GetString();
                    if (text.Contains(oldId, StringComparison.Ordinal) ||
                        text.Contains(oldCust, StringComparison.Ordinal))
                    {
                        cell.Value = text
                            .Replace(oldCust, newCust, StringComparison.Ordinal)
                            .Replace(oldId, newId, StringComparison.Ordinal);
                    }
                }
            }
        }
    }
}
