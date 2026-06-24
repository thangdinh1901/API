using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ClosedXML.Excel;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer.Services
{
    /// <summary>
    /// Seeds Design Dimensions from the Part Family "Excel from" clone source at the current DN.
    /// </summary>
    internal static class CatalogDimensionSuggestService
    {
        private const int HeaderRow = 1;
        private const int DataStartRow = 2;

        private static readonly HashSet<string> CatalogTabParams = new(StringComparer.OrdinalIgnoreCase)
        {
            "DN", "DN2",
        };

        public static void ApplySuggestions(ValveProject project)
        {
            IReadOnlyList<(string Name, double ValueMm)> suggestions = Suggest(project);
            if (suggestions.Count == 0)
                return;

            if (CatalogProjectService.HasUserDesignDimensions(project))
            {
                MergeSuggestions(project, suggestions);
                return;
            }

            ProjectDimensionService.ReplaceAll(project, suggestions);
        }

        private static void MergeSuggestions(
            ValveProject project,
            IReadOnlyList<(string Name, double ValueMm)> suggestions)
        {
            foreach ((string name, double valueMm) in suggestions)
            {
                if (CatalogProjectService.HasDimensionBinding(project, name))
                    continue;

                if (ProjectDimensionService.GetValue(project, name) > 0)
                    continue;

                ProjectDimensionService.SetValue(project, name, valueMm);
            }
        }

        public static IReadOnlyList<(string Name, double ValueMm)> Suggest(ValveProject project)
        {
            string cloneId = ResolveCloneSourcePartId(project);
            if (string.IsNullOrEmpty(cloneId))
                return Array.Empty<(string, double)>();

            CustomPartDefinition? clone = CustomPartCatalog.FindById(cloneId);
            if (clone == null)
                return Array.Empty<(string, double)>();

            var suggestions = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

            MergeExcelGeometryRow(cloneId, project, suggestions);
            MergeSizeVariant(clone, project, suggestions);
            MergePartTypeTables(clone, project, suggestions);

            if (clone.Skeleton != null)
            {
                foreach (KeyValuePair<string, double> pair in clone.Skeleton)
                {
                    if (pair.Value > 0 && !CatalogTabParams.Contains(pair.Key))
                        suggestions[pair.Key] = pair.Value;
                }
            }

            foreach (CatalogPartParam param in clone.CatalogParams)
            {
                if (CatalogTabParams.Contains(param.Name) || param.UseSkeletonDN || param.UseSkeletonDN2)
                    continue;

                if (!suggestions.ContainsKey(param.Name) && param.Default > 0)
                    suggestions[param.Name] = param.Default;
            }

            return suggestions
                .Where(pair => pair.Value > 0)
                .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .Select(pair => (pair.Key, pair.Value))
                .ToList();
        }

        private static string ResolveCloneSourcePartId(ValveProject project)
        {
            if (!string.IsNullOrWhiteSpace(project.ExcelCloneSourcePartId))
                return project.ExcelCloneSourcePartId.Trim();

            string partId = string.IsNullOrWhiteSpace(project.ValveName)
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

        private static void MergeExcelGeometryRow(
            string cloneSourcePartId,
            ValveProject project,
            Dictionary<string, double> suggestions)
        {
            try
            {
                using var workbook = new XLWorkbook(CatalogExcelTemplateService.ResolveTemplatePath());
                IXLWorksheet? sheet = CatalogExcelTemplateService.FindSheetByPartPrefix(workbook, cloneSourcePartId);
                if (sheet == null)
                    return;

                Dictionary<string, int> header = ReadHeaderMap(sheet, HeaderRow);
                if (!header.ContainsKey("DN"))
                    return;

                int dn = (int)Math.Round(project.Parameters.DN);
                int? dn2 = project.Parameters.DN2 > 0
                    ? (int)Math.Round(project.Parameters.DN2)
                    : null;

                int lastRow = sheet.LastRowUsed()?.RowNumber() ?? DataStartRow;
                for (int rowIndex = DataStartRow; rowIndex <= lastRow; rowIndex++)
                {
                    if (!TryReadInt(sheet, header, rowIndex, "DN", out int rowDn) || rowDn != dn)
                        continue;

                    if (dn2.HasValue && header.ContainsKey("DN2"))
                    {
                        if (!TryReadInt(sheet, header, rowIndex, "DN2", out int rowDn2) || rowDn2 != dn2.Value)
                            continue;
                    }

                    string paramDef = GetString(sheet, header, rowIndex, "ContentGeometryParamDefinition");
                    foreach (string paramName in ParseGeometryParamNames(paramDef))
                    {
                        if (CatalogTabParams.Contains(paramName))
                            continue;

                        if (TryReadDouble(sheet, header, rowIndex, paramName, out double value) && value > 0)
                            suggestions[paramName] = value;
                    }

                    return;
                }
            }
            catch
            {
                // Template read is best-effort; size-catalog tables still apply.
            }
        }

        private static void MergeSizeVariant(
            CustomPartDefinition clone,
            ValveProject project,
            Dictionary<string, double> suggestions)
        {
            int dn = (int)Math.Round(project.Parameters.DN);
            int? dn2 = project.Parameters.DN2 > 0 ? (int)Math.Round(project.Parameters.DN2) : null;

            CatalogExcelSizeVariant? variant = CatalogExcelSizeCatalog.BuildSizes(clone)
                .FirstOrDefault(v =>
                    v.Dn == dn
                    && (!dn2.HasValue || v.Dn2 == dn2));

            if (variant == null)
                return;

            if (variant.Cel is > 0)
                suggestions["CEL"] = variant.Cel.Value;
            if (variant.T is > 0)
                suggestions["T"] = variant.T.Value;
        }

        private static void MergePartTypeTables(
            CustomPartDefinition clone,
            ValveProject project,
            Dictionary<string, double> suggestions)
        {
            int dn = (int)Math.Round(project.Parameters.DN);
            if (dn <= 0)
                return;

            string id = clone.Id;

            string catalogGroup = string.IsNullOrWhiteSpace(project.CatalogGroup)
                ? clone.Group
                : project.CatalogGroup;

            if (FittingDimensionService.UsesPipeRunDimensions(catalogGroup))
            {
                FittingDimensionService.ConnectionStyle style = InferConnectionStyle(project, clone);
                suggestions["BodyOD"] = FittingDimensionService.RunDiameterMm(dn);
                suggestions["ElbowCenterToFace"] = ElbowCenterToFaceMm(dn, style, id);
                return;
            }

            if (id.StartsWith("STUBEND_", StringComparison.OrdinalIgnoreCase)
                || id.StartsWith("COLLAR_LJ_", StringComparison.OrdinalIgnoreCase))
            {
                CatalogStubEndTable.Pattern pattern = CatalogStubEndTable.ResolvePattern(id);
                if (CatalogStubEndTable.TryGet(dn, pattern, out CatalogStubEndTable.StubEndDims stub))
                {
                    suggestions["FaceToFace"] = stub.L;
                    suggestions["BodyOD"] = stub.D2;
                    suggestions["L"] = stub.L;
                    suggestions["B"] = stub.B;
                    suggestions["D1"] = stub.D1;
                    suggestions["D2"] = stub.D2;
                }

                return;
            }

            if (id.StartsWith("LJ_RING_", StringComparison.OrdinalIgnoreCase))
            {
                if (CatalogLjRingCl150Table.TryGet(dn, out CatalogLjRingCl150Table.LjRingDims ring))
                {
                    suggestions["FaceToFace"] = ring.L;
                    suggestions["L"] = ring.L;
                    suggestions["B"] = ring.Tf;
                    suggestions["D1"] = ring.D1;
                    suggestions["D2"] = ring.D2;
                    suggestions["Tf"] = ring.Tf;
                }

                return;
            }

            if (clone.Group.Equals("Flange", StringComparison.OrdinalIgnoreCase)
                || clone.Group.Equals("Gasket", StringComparison.OrdinalIgnoreCase))
            {
                suggestions["BodyOD"] = PipeSizeCatalog.OdSch40Mm(dn);

                if (CatalogFlangeCl150RfTable.TryGetTf(dn, out double tf))
                    suggestions["Tf"] = tf;
            }
        }

        private static FittingDimensionService.ConnectionStyle InferConnectionStyle(
            ValveProject project,
            CustomPartDefinition clone)
        {
            if (project.Ports.Count > 0)
                return FittingDimensionService.InferConnectionStyle(project);

            string set = project.StandardSet ?? clone.StandardSet ?? "";
            if (set.Contains("SW", StringComparison.OrdinalIgnoreCase)
                || clone.Id.Contains("_SW_", StringComparison.OrdinalIgnoreCase))
                return FittingDimensionService.ConnectionStyle.SocketWeld;

            return FittingDimensionService.ConnectionStyle.ButtWeld;
        }

        private static double ElbowCenterToFaceMm(
            int dn,
            FittingDimensionService.ConnectionStyle style,
            string clonePartId)
        {
            double center = FittingDimensionService.ElbowCenterToFaceMm(dn, style);
            if (clonePartId.Contains("ELBOW_45", StringComparison.OrdinalIgnoreCase)
                || clonePartId.Contains("45_LR", StringComparison.OrdinalIgnoreCase)
                || clonePartId.Contains("45_SW", StringComparison.OrdinalIgnoreCase))
            {
                // B16.9 LR 45° ≈ half of LR 90° center-to-face at same DN.
                return Math.Round(center * 0.5, 3);
            }

            return center;
        }

        private static IEnumerable<string> ParseGeometryParamNames(string paramDef)
        {
            if (string.IsNullOrWhiteSpace(paramDef))
                yield break;

            foreach (string token in paramDef.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (token.Length > 0)
                    yield return token;
            }
        }

        private static Dictionary<string, int> ReadHeaderMap(IXLWorksheet sheet, int headerRow)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (IXLCell cell in sheet.Row(headerRow).CellsUsed())
            {
                string? name = cell.GetString()?.Trim();
                if (!string.IsNullOrEmpty(name))
                    map[name] = cell.Address.ColumnNumber;
            }

            return map;
        }

        private static string GetString(IXLWorksheet sheet, Dictionary<string, int> header, int row, string column)
        {
            if (!header.TryGetValue(column, out int col))
                return string.Empty;

            return sheet.Cell(row, col).GetString()?.Trim() ?? string.Empty;
        }

        private static bool TryReadInt(IXLWorksheet sheet, Dictionary<string, int> header, int row, string column, out int value)
        {
            value = 0;
            if (!header.TryGetValue(column, out int col))
                return false;

            IXLCell cell = sheet.Cell(row, col);
            if (cell.TryGetValue(out int i))
            {
                value = i;
                return true;
            }

            string text = cell.GetString()?.Trim() ?? "";
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryReadDouble(IXLWorksheet sheet, Dictionary<string, int> header, int row, string column, out double value)
        {
            value = 0;
            if (!header.TryGetValue(column, out int col))
                return false;

            IXLCell cell = sheet.Cell(row, col);
            if (cell.TryGetValue(out double d))
            {
                value = d;
                return true;
            }

            if (cell.TryGetValue(out int i))
            {
                value = i;
                return true;
            }

            string text = cell.GetString()?.Trim() ?? "";
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }
    }
}
