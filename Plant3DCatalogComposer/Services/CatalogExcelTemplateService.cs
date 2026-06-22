using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;

namespace Plant3DCatalogComposer.Services
{
    internal static class CatalogExcelTemplateService
    {
        private const int MaxSheetNameLength = 31;

        public static string ResolveTemplatePath() => CatalogExcelExportService.ResolveTemplatePath();

        public static IReadOnlyList<string> ListTemplatePartIds()
        {
            using var workbook = new XLWorkbook(ResolveTemplatePath());
            return workbook.Worksheets
                .Select(w => w.Name)
                .Where(n => n.Contains(',', StringComparison.Ordinal))
                .Select(n => n.Split(',')[0].Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static string? InferCloneSourcePartId(
            string partId,
            string pnpClassName,
            string standardSet,
            string group)
        {
            string pnp = pnpClassName.Trim();
            string set = standardSet.Trim();

            if (set.Equals(BwSch40StandardCatalog.SetId, StringComparison.OrdinalIgnoreCase))
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

            if (set.Equals(SwCl3000StandardCatalog.SetId, StringComparison.OrdinalIgnoreCase))
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
                if (partId.StartsWith("SO_", StringComparison.OrdinalIgnoreCase))
                    return "SO_FLRF_CL150";
                if (partId.StartsWith("BLD_", StringComparison.OrdinalIgnoreCase))
                    return "BLD_FLRF_CL150";
                if (partId.StartsWith("LJ_RING_", StringComparison.OrdinalIgnoreCase))
                    return "LJ_RING_CL150_RF";
                return "WN_FLRF_CL150";
            }

            if (group.Equals("Gasket", StringComparison.OrdinalIgnoreCase))
            {
                return partId.Contains("FF", StringComparison.OrdinalIgnoreCase)
                    ? "GSK_FF_CL150"
                    : "GSK_RF_CL150";
            }

            return "ELBOW_90_LR_BW_SCH40";
        }

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
            if (source == null)
            {
                error = $"No template sheet for clone source '{cloneSourcePartId}'.";
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
            workbook.SaveAs(templatePath);
            sheetName = newName;
            return true;
        }

        internal static IXLWorksheet? FindSheetByPartPrefix(XLWorkbook workbook, string partId)
        {
            string prefix = partId + ",";
            return workbook.Worksheets
                .FirstOrDefault(w => w.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
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
