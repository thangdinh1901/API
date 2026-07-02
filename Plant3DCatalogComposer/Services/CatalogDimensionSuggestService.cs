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

        /// <summary>Suggest geometry dims for a single inserted native (Catalog-kind) node, using
        /// that node's own CatalogPartId and DN — independent of the composed project's DN/clone.
        /// Returns ONLY original Excel column symbols (D, R, A, L1, …) — no internal aliases
        /// (BodyOD, ElbowCenterToFace, …) — since these seed rows are re-exported under their
        /// original template column names.</summary>
        public static IReadOnlyList<(string Name, double ValueMm)> SuggestForNode(
            ValveProject project,
            PrimitiveNode node)
        {
            if (node.Kind != SceneNodeKind.Catalog || string.IsNullOrEmpty(node.CatalogPartId))
                return Array.Empty<(string, double)>();

            if (!node.Parameters.TryGetValue("DN", out ParamValue? dnParam) || dnParam.Value <= 0)
                return Array.Empty<(string, double)>();

            int dn = (int)Math.Round(dnParam.Value);
            int? dn2 = node.Parameters.TryGetValue("DN2", out ParamValue? dn2Param) && dn2Param.Value > 0
                ? (int)Math.Round(dn2Param.Value)
                : null;

            return SuggestCore(
                node.CatalogPartId!, project, dn, dn2, useProjectPorts: false, originalSymbolsOnly: true);
        }

        private static IReadOnlyList<(string Name, double ValueMm)> SuggestCore(
            string cloneId,
            ValveProject project,
            int dn,
            int? dn2,
            bool useProjectPorts,
            bool originalSymbolsOnly = false)
        {
            CustomPartDefinition? clone = CustomPartCatalog.FindById(cloneId);
            if (clone == null)
                return Array.Empty<(string, double)>();

            var suggestions = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

            MergeExcelGeometryRow(cloneId, dn, dn2, suggestions);
            MergeSizeVariant(clone, dn, dn2, suggestions);

            if (!originalSymbolsOnly)
            {
                MergePartTypeTables(clone, project, dn, useProjectPorts, suggestions);

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
            }

            return suggestions
                .Where(pair => pair.Value > 0)
                .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .Select(pair => (pair.Key, pair.Value))
                .ToList();
        }

        private static void MergeExcelGeometryRow(
            string cloneSourcePartId,
            int dn,
            int? dn2,
            Dictionary<string, double> suggestions)
        {
            try
            {
                using var workbook = new XLWorkbook(CatalogExcelTemplateService.ResolveTemplatePath());
                IXLWorksheet? sheet = CatalogExcelTemplateService.FindSheetByPartPrefixOrNormalizedKey(
                    workbook, cloneSourcePartId);
                if (sheet == null)
                    return;

                Dictionary<string, int> header = ReadHeaderMap(sheet, HeaderRow);
                string? sizeCol = ResolveSizeColumn(header);
                if (sizeCol == null)
                    return;

                int lastRow = sheet.LastRowUsed()?.RowNumber() ?? DataStartRow;
                for (int rowIndex = DataStartRow; rowIndex <= lastRow; rowIndex++)
                {
                    if (!TryReadInt(sheet, header, rowIndex, sizeCol, out int rowDn) || rowDn != dn)
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
            int dn,
            int? dn2,
            Dictionary<string, double> suggestions)
        {
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
            int dn,
            bool useProjectPorts,
            Dictionary<string, double> suggestions)
        {
            if (dn <= 0)
                return;

            string id = clone.Id;

            string catalogGroup = string.IsNullOrWhiteSpace(project.CatalogGroup)
                ? clone.Group
                : project.CatalogGroup;

            if (FittingDimensionService.UsesPipeRunDimensions(catalogGroup))
            {
                FittingDimensionService.ConnectionStyle style = InferConnectionStyle(project, clone, useProjectPorts);
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
            CustomPartDefinition clone,
            bool useProjectPorts)
        {
            if (useProjectPorts && project.Ports.Count > 0)
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

        /// <summary>The column that carries the nominal size per row. Legacy templates use "DN";
        /// CATA_NUI-style sheets have no DN column and identify size via "Sizes" (e.g. "50") or
        /// "NominalDiameter_S-ALL".</summary>
        private static string? ResolveSizeColumn(Dictionary<string, int> header)
        {
            foreach (string candidate in new[] { "DN", "Sizes", "NominalDiameter_S-ALL" })
            {
                if (header.ContainsKey(candidate))
                    return candidate;
            }

            return null;
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
