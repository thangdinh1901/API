using System;
using System.Collections.Generic;
using System.Linq;
using ClosedXML.Excel;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer.Services
{
    /// <summary>
    /// Maps Excel geometry columns (L, D1, …) to catalog script params and clone-template CGPD strings.
    /// </summary>
    internal static class CatalogExcelGeometryParams
    {
        private const int TemplateSeedRow = 3;
        private const int HeaderRow = 1;

        public static string ResolveParamDefinition(ValveProject project, string? partId = null)
        {
            foreach (string candidate in EnumerateTemplatePartIds(project, partId))
            {
                string? cgp = TryReadTemplateCgp(candidate);
                if (!string.IsNullOrWhiteSpace(cgp))
                    return NormalizeParamDefinition(cgp);
            }

            return "DN";
        }

        private static IEnumerable<string> EnumerateTemplatePartIds(ValveProject project, string? partId)
        {
            string clone = ResolveCloneSourcePartId(project);
            if (!string.IsNullOrWhiteSpace(clone))
                yield return clone;

            if (!string.IsNullOrWhiteSpace(partId))
                yield return partId.Trim();

            if (!string.IsNullOrWhiteSpace(project.CustomPartId))
                yield return project.CustomPartId.Trim();
        }

        private static string? TryReadTemplateCgp(string templatePartId)
        {
            try
            {
                using var workbook = new XLWorkbook(CatalogExcelTemplateService.ResolveTemplatePath());
                IXLWorksheet? sheet = CatalogExcelTemplateService.FindSheetByPartPrefix(workbook, templatePartId);
                if (sheet == null)
                    return null;

                var header = ReadHeaderMap(sheet, HeaderRow);
                return GetString(sheet, header, TemplateSeedRow, "ContentGeometryParamDefinition");
            }
            catch
            {
                return null;
            }
        }

        public static string NormalizeParamDefinition(string? cgp)
        {
            if (string.IsNullOrWhiteSpace(cgp))
                return "DN";

            return string.Join(
                ",",
                ParseParamNames(cgp));
        }

        public static IReadOnlyList<string> ParseParamNames(string? cgp)
        {
            if (string.IsNullOrWhiteSpace(cgp))
                return ["DN"];

            return cgp
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static IReadOnlyList<(string ExcelName, double Value, string XmlType)> CollectScriptParams(
            ValveProject project,
            string? paramDefinition = null)
        {
            string cgp = NormalizeParamDefinition(paramDefinition ?? ResolveParamDefinition(project));
            var rows = new List<(string, double, string)>();

            foreach (string name in ParseParamNames(cgp))
            {
                if (name.Equals("DN", StringComparison.OrdinalIgnoreCase))
                {
                    if (project.Parameters.DN > 0)
                        rows.Add((name, project.Parameters.DN, "INT"));
                    continue;
                }

                if (TryGetValue(project, name, out double value))
                {
                    rows.Add((name, value, ResolveXmlType(name)));
                    continue;
                }

                if (TryGetDefaultValue(project, name, out double fallback))
                    rows.Add((name, fallback, ResolveXmlType(name)));
            }

            return rows;
        }

        private static string ResolveXmlType(string name) =>
            name.Equals("DN", StringComparison.OrdinalIgnoreCase)
            || name.Equals("DN2", StringComparison.OrdinalIgnoreCase)
                ? "INT"
                : "LENGTH0";

        private static bool TryGetDefaultValue(ValveProject project, string excelName, out double value)
        {
            value = 0;
            if (excelName.Equals("L", StringComparison.OrdinalIgnoreCase))
            {
                value = project.Parameters.FaceToFace > 0
                    ? project.Parameters.FaceToFace
                    : Math.Max(150, project.Parameters.DN > 0 ? project.Parameters.DN * 2 : 200);
                return true;
            }

            if (excelName.Equals("D1", StringComparison.OrdinalIgnoreCase))
            {
                int dn = project.Parameters.DN > 0 ? (int)Math.Round(project.Parameters.DN) : 50;
                value = project.Parameters.BodyOD > 0 ? project.Parameters.BodyOD : PipeSizeCatalog.OdSch40Mm(dn);
                return true;
            }

            return false;
        }

        public static bool TryGetValue(ValveProject project, string excelName, out double value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(excelName))
                return false;

            string? composerName = MapExcelToComposerName(excelName);
            if (composerName != null)
            {
                value = ProjectDimensionService.GetValue(project, composerName);
                return value > 0;
            }

            if (project.Parameters.CustomDimensions.TryGetValue(excelName, out value))
                return value > 0;

            return false;
        }

        public static string? MapExcelToComposerName(string excelName) =>
            excelName.ToUpperInvariant() switch
            {
                "L" => "FaceToFace",
                "D1" => "BodyOD",
                _ => null,
            };

        private static string ResolveCloneSourcePartId(ValveProject project)
        {
            if (!string.IsNullOrWhiteSpace(project.ExcelCloneSourcePartId))
                return project.ExcelCloneSourcePartId.Trim();

            string partId = !string.IsNullOrWhiteSpace(project.CustomPartId)
                ? project.CustomPartId.Trim()
                : string.IsNullOrWhiteSpace(project.ValveName)
                    ? "COMPOSER_PART"
                    : CatalogProjectService.SanitizeCatalogName(project.ValveName);

            return CatalogExcelTemplateService.InferCloneSourcePartId(
                partId,
                project.CatalogCategory,
                project.PnpClassName ?? "",
                project.StandardSet ?? "",
                project.CatalogGroup ?? "",
                CatalogStandardSetInference.ResolvePrimaryEndType(project),
                project.Parameters.PressureClass,
                project.Parameters.PipeSchedule);
        }

        private static Dictionary<string, int> ReadHeaderMap(IXLWorksheet sheet, int headerRow)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (IXLCell cell in sheet.Row(1).CellsUsed())
            {
                string? text = cell.GetString()?.Trim();
                if (!string.IsNullOrEmpty(text))
                    map.TryAdd(text, cell.Address.ColumnNumber);
            }

            if (headerRow != 1)
            {
                foreach (IXLCell cell in sheet.Row(headerRow).CellsUsed())
                {
                    string? text = cell.GetString()?.Trim();
                    if (!string.IsNullOrEmpty(text))
                        map.TryAdd(text, cell.Address.ColumnNumber);
                }
            }

            return map;
        }

        private static string GetString(
            IXLWorksheet sheet,
            Dictionary<string, int> header,
            int rowIndex,
            string key)
        {
            if (!header.TryGetValue(key, out int col))
                return string.Empty;

            return sheet.Cell(rowIndex, col).GetString()?.Trim() ?? string.Empty;
        }
    }
}
