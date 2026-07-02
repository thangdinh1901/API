using System;
using System.Collections.Generic;
using System.Linq;
using ClosedXML.Excel;

namespace Plant3DCatalogComposer.Services
{
    /// <summary>Per-sheet metadata read from a configured catalog workbook (e.g. CATA_NUI.xlsx).</summary>
    internal sealed class TemplatePartInfo
    {
        public required string SheetName { get; init; }
        public required string PartId { get; init; }
        public required string DisplayName { get; init; }
        public string Component { get; init; } = "";
        public string Category { get; init; } = "";
        public string EndType { get; init; } = "";
        public string Schedule { get; init; } = "";
        public string PressureClass { get; init; } = "";
        public string ShapeName { get; init; } = "";
    }

    /// <summary>
    /// Reads part metadata from the actual cells of a real catalog workbook. Real catalogs extracted
    /// from the Spec Editor (CATA_NUI.xlsx) carry the End Type / Schedule / Pressure Class in data
    /// cells (not in the sheet name), and use two title rows before the first data row.
    /// </summary>
    internal static class CatalogTemplatePartReader
    {
        private static readonly HashSet<string> KnownComponents = new(StringComparer.OrdinalIgnoreCase)
        {
            "Elbow", "Tee", "Reducer", "Cross", "Cap", "Flange", "BlindFlange", "Gasket",
            "Olet", "Pipe", "Coupling", "Union", "Nipple", "Valve", "Bend",
        };

        public static IReadOnlyList<TemplatePartInfo> Read()
        {
            string? path = CatalogTemplateSettings.ResolveConfiguredTemplatePath();
            if (path == null)
                return Array.Empty<TemplatePartInfo>();

            try
            {
                return ReadFrom(path);
            }
            catch
            {
                return Array.Empty<TemplatePartInfo>();
            }
        }

        private static IReadOnlyList<TemplatePartInfo> ReadFrom(string path)
        {
            var list = new List<TemplatePartInfo>();
            using var workbook = new XLWorkbook(path);

            foreach (IXLWorksheet ws in workbook.Worksheets)
            {
                string? partId = CatalogExcelTemplateService.DeriveSheetPartId(ws.Name);
                if (partId == null)
                    continue;

                Dictionary<string, int> header = BuildHeader(ws);
                int dataRow = FindDataRow(ws, header);
                if (dataRow < 0)
                    continue;

                string longDesc = Cell(ws, header, dataRow, "PartFamilyLongDesc");
                list.Add(new TemplatePartInfo
                {
                    SheetName = ws.Name,
                    PartId = partId,
                    DisplayName = string.IsNullOrWhiteSpace(longDesc) ? ws.Name.Trim() : longDesc.Trim(),
                    Component = Cell(ws, header, dataRow, "PnPClassName"),
                    Category = Cell(ws, header, dataRow, "PartCategory"),
                    EndType = FirstByPrefix(ws, header, dataRow, "EndType"),
                    Schedule = FirstByPrefix(ws, header, dataRow, "Schedule"),
                    PressureClass = FirstByPrefix(ws, header, dataRow, "PressureClass"),
                    ShapeName = Cell(ws, header, dataRow, "ShapeName"),
                });
            }

            return list;
        }

        private static Dictionary<string, int> BuildHeader(IXLWorksheet ws)
        {
            var header = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            int lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 0;
            for (int c = 1; c <= lastCol; c++)
            {
                string name = ws.Cell(1, c).GetString().Trim();
                if (name.Length > 0 && !header.ContainsKey(name))
                    header[name] = c;
            }

            return header;
        }

        /// <summary>First row (>=2) whose PnPClassName cell holds a real component (skips title rows).</summary>
        private static int FindDataRow(IXLWorksheet ws, Dictionary<string, int> header)
        {
            int lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
            int scanTo = Math.Min(lastRow, 15);

            if (header.TryGetValue("PnPClassName", out int pnpCol))
            {
                for (int r = 2; r <= scanTo; r++)
                {
                    if (KnownComponents.Contains(ws.Cell(r, pnpCol).GetString().Trim()))
                        return r;
                }
            }

            // Fallback: first row with a non-label ShapeName value.
            if (header.TryGetValue("ShapeName", out int shapeCol))
            {
                for (int r = 2; r <= scanTo; r++)
                {
                    string v = ws.Cell(r, shapeCol).GetString().Trim();
                    if (v.Length > 0 && !v.Equals("Shape Name", StringComparison.OrdinalIgnoreCase))
                        return r;
                }
            }

            return -1;
        }

        private static string Cell(IXLWorksheet ws, Dictionary<string, int> header, int row, string name) =>
            header.TryGetValue(name, out int col) ? ws.Cell(row, col).GetString().Trim() : "";

        private static string FirstByPrefix(
            IXLWorksheet ws,
            Dictionary<string, int> header,
            int row,
            string prefix)
        {
            foreach (KeyValuePair<string, int> kv in header
                         .Where(h => h.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                         .OrderBy(h => h.Value))
            {
                string v = ws.Cell(row, kv.Value).GetString().Trim();
                if (v.Length > 0)
                    return v;
            }

            return "";
        }
    }
}
