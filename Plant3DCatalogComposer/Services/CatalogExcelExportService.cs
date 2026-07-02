using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer.Services
{
    internal sealed class CatalogExcelExportResult
    {
        public bool Success { get; init; }

        public string Message { get; init; } = string.Empty;

        public string OutputPath { get; init; } = string.Empty;

        public int SheetCount { get; init; }

        public int SizeRowCount { get; init; }

        public IReadOnlyList<string> SkippedPartIds { get; init; } = [];

        public IReadOnlyList<string> Warnings { get; init; } = [];
    }

    internal sealed class CatalogExcelTemplateMetadata
    {
        public string FamilyLongDesc { get; init; } = "";

        public string FamilyId { get; init; } = "";

        public string Material { get; init; } = "CS";

        public string CompatibleStandard { get; init; } = "";

        public string DesignStd { get; init; } = "";

        public string IsoSkey { get; init; } = "";

        public string IsoType { get; init; } = "";

        public string ContentIsoSymbolDefinition { get; init; } = "";

        /// <summary>Clone-template seed row CGPD (e.g. DN,L for VALVE_FL_CL150).</summary>
        public string ContentGeometryParamDefinition { get; init; } = "";
    }

    internal static class CatalogExcelExportService
    {
        private const int TemplateSeedRow = 3;
        /// <summary>Published workbook: one machine-name header row, data from row 2.</summary>
        private const int PublishedDataStartRow = 2;
        /// <summary>Template workbook layout (seed + static sheets) still uses row 3+.</summary>
        private const int DataStartRow = TemplateSeedRow;
        private const int HeaderRow = 1;

        private readonly record struct PortWriteSpec(
            (string EndType, string PortName) Port,
            string Suffix,
            int Dn,
            double Od,
            Guid SizeRecordId);

        private static readonly object TemplatePathCacheLock = new();
        private static string? _cachedBestPath;
        private static string _cachedStamp = "";

        /// <summary>
        /// Pick the best Catalog Builder template (most valve sheets). The chosen path is cached and
        /// only re-scored when a candidate file's size/write-time changes, so UI refreshes do not
        /// repeatedly open the 30-sheet workbook (the main palette lag source).
        /// </summary>
        /// <summary>Drop the cached best-template choice (call after the configured path changes).</summary>
        public static void InvalidateTemplateCache()
        {
            lock (TemplatePathCacheLock)
            {
                _cachedBestPath = null;
                _cachedStamp = "";
            }
        }

        public static string ResolveTemplatePath()
        {
            // A user-configured template (e.g. CATA_NUI.xlsx) takes precedence over the bundled
            // Resources workbook so a real validated catalog can act as the standard template.
            string? configured = CatalogTemplateSettings.ResolveConfiguredTemplatePath();
            if (configured != null)
                return configured;

            var candidates = new List<string>();
            if (ProjectPaths.TryResolveApiRoot() != null)
                candidates.Add(DevTemplatePath());
            candidates.Add(PluginTemplatePath());

            var existing = candidates
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(File.Exists)
                .ToList();

            string stamp = string.Join("|", existing.Select(p =>
            {
                var fi = new FileInfo(p);
                return $"{p};{fi.Length};{fi.LastWriteTimeUtc.Ticks}";
            }));

            lock (TemplatePathCacheLock)
            {
                if (_cachedBestPath != null && _cachedStamp == stamp)
                    return _cachedBestPath;
            }

            string? best = null;
            int bestScore = -1;
            foreach (string path in existing)
            {
                int score = ScoreTemplate(path);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = path;
                }
            }

            if (best == null)
            {
                throw new FileNotFoundException(
                    "Catalog Builder Excel template not found. Rebuild the plugin to deploy Resources/CatalogBuilderTemplate.xlsx.");
            }

            lock (TemplatePathCacheLock)
            {
                _cachedBestPath = best;
                _cachedStamp = stamp;
            }

            return best;
        }

        private static int ScoreTemplate(string path)
        {
            try
            {
                using var workbook = new XLWorkbook(path);
                int valveSheets = workbook.Worksheets.Count(w =>
                    w.Name.StartsWith("VALVE_", StringComparison.OrdinalIgnoreCase));
                return workbook.Worksheets.Count + valveSheets * 100;
            }
            catch
            {
                return -1;
            }
        }

        internal static string PluginTemplatePath() =>
            Path.Combine(ProjectPaths.PluginDirectory, "Resources", "CatalogBuilderTemplate.xlsx");

        internal static string DevTemplatePath()
        {
            string root = ProjectPaths.TryResolveApiRoot() ?? ProjectPaths.PluginDirectory;
            return Path.Combine(root, "Plant3DCatalogComposer", "Resources", "CatalogBuilderTemplate.xlsx");
        }

        public static CatalogExcelExportResult Export(
            string outputPath,
            ValveProject? project = null,
            IReadOnlyList<string>? partIdFilter = null)
        {
            string templatePath = ResolveTemplatePath();
            // External template = a user-chosen workbook (Browse → template.json), e.g. CATA_NUI.xlsx.
            // Treated as READ-ONLY structure: only fill size rows into sheets that already exist —
            // no sheet cloning, pruning, or static valve/stud/pipe injection.
            bool externalTemplate = CatalogTemplateSettings.ResolveConfiguredTemplatePath() != null;
            IReadOnlyList<CatalogExcelPartRow> parts = CatalogExcelPartResolver.DiscoverExportParts();
            if (partIdFilter is { Count: > 0 })
            {
                var filter = new HashSet<string>(partIdFilter, StringComparer.OrdinalIgnoreCase);
                parts = parts.Where(p => filter.Contains(p.Part.Id)).ToList();
            }

            var warnings = new List<string>();
            var skipped = new List<string>();
            var filledSheets = new List<string>();

            if (parts.Count == 0)
            {
                return new CatalogExcelExportResult
                {
                    Success = false,
                    Message = "No exportable parts found in catalog_generator/parts.",
                    SkippedPartIds = skipped,
                };
            }

            using var workbook = new XLWorkbook(templatePath);
            int totalRows = 0;

            foreach (CatalogExcelPartRow row in parts)
            {
                IXLWorksheet? sheet = null;

                // "Excel from" (ExcelCloneSourcePartId) drives template selection when set: the part
                // publishes onto a clone of that source sheet — the part name need NOT match any
                // sheet. Clone is in-memory only (Export SaveAs to a separate output); the source
                // template file is never modified. Parts with no "Excel from" (the 18 native
                // fittings) fall back to matching a sheet by their own id.
                if (!string.IsNullOrWhiteSpace(row.Part.ExcelCloneSourcePartId))
                {
                    sheet = CatalogExcelTemplateService.TryClonePartSheetInWorkbook(
                        workbook,
                        row.Part.Id,
                        row.Part.ExcelCloneSourcePartId);
                }

                sheet ??= FindTemplateSheet(workbook, row.Part.Id);

                if (sheet == null)
                {
                    skipped.Add(row.Part.Id);
                    warnings.Add($"{row.Part.Id}: no matching sheet in CatalogBuilderTemplate.xlsx — skipped.");
                    continue;
                }

                CatalogExcelTemplateMetadata metadata = ReadTemplateMetadata(sheet);
                string scriptPath = ResolveScriptPath(row.Part.Id, warnings);
                Guid familyId = ResolveFamilyId(metadata, row.Part.Id);
                IReadOnlyList<CatalogExcelSizeVariant> sizes = CatalogExcelSizeCatalog.BuildSizes(row.Part);

                int written = WriteSizeRows(sheet, row, metadata, familyId, scriptPath, sizes, project, warnings);
                totalRows += written;
                filledSheets.Add(sheet.Name);
            }

            foreach (CustomPartDefinition part in CustomPartCatalog.ExportableParts)
            {
                if (parts.All(p => !p.Part.Id.Equals(part.Id, StringComparison.OrdinalIgnoreCase))
                    && !skipped.Contains(part.Id, StringComparer.OrdinalIgnoreCase))
                {
                    skipped.Add(part.Id);
                }
            }

            if (filledSheets.Count == 0)
            {
                return new CatalogExcelExportResult
                {
                    Success = false,
                    Message = "No template sheets matched exportable parts.",
                    SkippedPartIds = skipped.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                    Warnings = warnings,
                };
            }

            // Prune always runs — it only trims the in-memory EXPORT workbook (never the source
            // template file), so "only this part" publishes just the filled sheet(s). External
            // template only skips the static valve/stud/pipe INJECTION below (that would add sheets
            // the CATA_NUI-style workbook doesn't own).
            bool singlePart = partIdFilter is { Count: 1 };
            int removedSheets = PruneUnexportedPartSheets(
                workbook,
                filledSheets,
                keepStaticSupportSheets: !singlePart && !externalTemplate);
            if (removedSheets > 0)
            {
                warnings.Add(
                    singlePart
                        ? $"Workbook trimmed to {filledSheets.Count} part sheet(s) only."
                        : $"Removed {removedSheets} unused sheet(s) from the export workbook.");
            }

            if (singlePart)
                RemoveCatalogDataFlagSheet(workbook);

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");
            if (!externalTemplate && partIdFilter is not { Count: 1 })
                FillStaticTemplateSheets(workbook, warnings);
            workbook.SaveAs(outputPath);

            string skippedNote = skipped.Count > 0
                ? $"{skipped.Count} part(s) skipped (no template sheet)."
                : "";

            return new CatalogExcelExportResult
            {
                Success = true,
                OutputPath = outputPath,
                SheetCount = filledSheets.Count,
                SizeRowCount = totalRows,
                SkippedPartIds = skipped.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                Warnings = warnings,
                Message =
                    $"Published {filledSheets.Count} sheet(s), {totalRows} size row(s)."
                    + (skippedNote.Length > 0 ? " " + skippedNote.Trim() : ""),
            };
        }

        private static IXLWorksheet? FindTemplateSheet(XLWorkbook workbook, string partId) =>
            CatalogExcelTemplateService.FindSheetByPartPrefixOrNormalizedKey(workbook, partId);

        /// <summary>
        /// Single-part publish loads the full template — drop unfilled part sheets so Spec Editor
        /// does not ingest placeholder families for parts that were not exported.
        /// </summary>
        private static int PruneUnexportedPartSheets(
            XLWorkbook workbook,
            IReadOnlyList<string> filledSheets,
            bool keepStaticSupportSheets)
        {
            var keep = new HashSet<string>(filledSheets, StringComparer.OrdinalIgnoreCase);
            if (keepStaticSupportSheets)
            {
                foreach (IXLWorksheet sheet in workbook.Worksheets)
                {
                    if (IsRequiredCatalogSheet(sheet.Name))
                        keep.Add(sheet.Name);
                }
            }

            int removed = 0;
            foreach (IXLWorksheet sheet in workbook.Worksheets.ToList())
            {
                if (keep.Contains(sheet.Name))
                    continue;

                workbook.Worksheets.Delete(sheet.Name);
                removed++;
            }

            return removed;
        }

        private static bool IsRequiredCatalogSheet(string sheetName) =>
            sheetName.Equals("Catalog Data Flag", StringComparison.OrdinalIgnoreCase)
            || sheetName.StartsWith("PIPE_SCH40", StringComparison.OrdinalIgnoreCase)
            || sheetName.StartsWith("STUD_RF", StringComparison.OrdinalIgnoreCase)
            || sheetName.StartsWith("STUD_LJ", StringComparison.OrdinalIgnoreCase);

        private static Guid ResolveFamilyId(CatalogExcelTemplateMetadata metadata, string partId) =>
            CatalogExcelPartResolver.StableFamilyId(partId);

        private static CatalogExcelTemplateMetadata ReadTemplateMetadata(IXLWorksheet sheet)
        {
            var header = ReadHeaderMap(sheet, HeaderRow);
            return new CatalogExcelTemplateMetadata
            {
                FamilyLongDesc = GetString(sheet, header, DataStartRow, "PartFamilyLongDesc"),
                FamilyId = GetString(sheet, header, DataStartRow, "PartFamilyId"),
                Material = GetString(sheet, header, DataStartRow, "Material"),
                CompatibleStandard = GetString(sheet, header, DataStartRow, "CompatibleStandard"),
                DesignStd = GetString(sheet, header, DataStartRow, "DesignStd"),
                IsoSkey = GetString(sheet, header, DataStartRow, "SKEY"),
                IsoType = GetString(sheet, header, DataStartRow, "TYPE"),
                ContentIsoSymbolDefinition = GetString(sheet, header, DataStartRow, "ContentIsoSymbolDefinition"),
                ContentGeometryParamDefinition = CatalogExcelGeometryParams.NormalizeParamDefinition(
                    GetString(sheet, header, DataStartRow, "ContentGeometryParamDefinition")),
            };
        }

        private static IXLWorksheet? FindStaticSheet(XLWorkbook workbook, string sheetPrefix) =>
            workbook.Worksheets.FirstOrDefault(w =>
                w.Name.StartsWith(sheetPrefix, StringComparison.OrdinalIgnoreCase));

        private static void FillStaticTemplateSheets(XLWorkbook workbook, List<string> warnings)
        {
            // Pipe (native CPP) and stud-bolt sheets are static support families. If the template is
            // missing them, surface a warning (rather than silently dropping Pipe / Bolt stud) so the
            // user knows to restore CatalogBuilderTemplate.xlsx.
            WarnIfStaticSheetMissing(workbook, "PIPE_SCH40", "Pipe (native CPP)", warnings);
            WarnIfStaticSheetMissing(workbook, "STUD_RF", "Stud bolts (RF)", warnings);
            WarnIfStaticSheetMissing(workbook, "STUD_LJ", "Stud bolts (LJ)", warnings);

            FillPipeSheet(workbook);
            FillStudRfSheet(workbook);
            FillStudLjSheet(workbook);
        }

        private static void WarnIfStaticSheetMissing(
            XLWorkbook workbook,
            string sheetPrefix,
            string label,
            List<string> warnings)
        {
            if (FindStaticSheet(workbook, sheetPrefix) == null)
            {
                warnings.Add(
                    $"{label}: template sheet '{sheetPrefix}*' missing — not exported. "
                    + "Run scripts/add_static_support_template_sheets.py to restore CatalogBuilderTemplate.xlsx.");
            }
        }

        private static void FillPipeSheet(XLWorkbook workbook)
        {
            // Pipe stock = plain ends (PL / PE). Butt-weld and socket-weld joints use PL↔BV / PL↔SW rules.
            FillStaticSheet(
                workbook,
                "PIPE_SCH40",
                CatalogExcelIsoMetadata.PipeSch40(),
                BuildPipeSizeLongDesc,
                endType: "PL");
        }

        private static void FillStudRfSheet(XLWorkbook workbook)
        {
            IXLWorksheet? sheet = FindStaticSheet(workbook, "STUD_RF");
            if (sheet == null)
                return;

            var header = ReadHeaderMap(sheet, HeaderRow);
            const string familyDesc = "Studbolt ASTM A193-B7-Nuts ASTM A194-2H";
            const string shortDesc = "Bolt set";
            Guid familyId = CatalogExcelPartResolver.StableFamilyId("STUD_RF_CL150");
            CatalogExcelIsoMetadata iso = CatalogExcelIsoMetadata.StudBoltRf();

            int lastRow = sheet.LastRowUsed()?.RowNumber() ?? DataStartRow;

            for (int rowIndex = DataStartRow; rowIndex <= lastRow; rowIndex++)
            {
                string sizes = GetString(sheet, header, rowIndex, "Sizes");
                if (string.IsNullOrWhiteSpace(sizes) || !int.TryParse(sizes, out int dn))
                    continue;

                if (!CatalogFlangeBoltingCatalog.TryGetRfCl150(dn, out FlangeBoltingSpec bolting))
                    continue;

                Guid sizeRecordId = CatalogExcelPartResolver.StableSizeRecordId("STUD_RF_CL150", dn);

                ApplyIsoMetadata(sheet, header, rowIndex, iso, new CatalogExcelTemplateMetadata());
                Set(sheet, header, rowIndex, "PartFamilyLongDesc", familyDesc);
                Set(sheet, header, rowIndex, "PartSizeLongDesc", BuildStudSizeLongDesc(dn, familyDesc));
                Set(sheet, header, rowIndex, "ShortDescription", shortDesc);
                Set(sheet, header, rowIndex, "PartFamilyId", familyId.ToString("D"));
                Set(sheet, header, rowIndex, "CatalogPartFamilyId", familyId.ToString("D"));
                Set(sheet, header, rowIndex, "PnPClassName", "BoltSet");
                Set(sheet, header, rowIndex, "PartCategory", "Fasteners");
                Set(sheet, header, rowIndex, "ConnectionPortCount", "1");
                Set(sheet, header, rowIndex, "SizeRecordId_S-ALL", sizeRecordId.ToString("D"));
                Set(sheet, header, rowIndex, "PortName_S-ALL", "ALL");
                Set(sheet, header, rowIndex, "NominalDiameter_S-ALL", dn.ToString(CultureInfo.InvariantCulture));
                Set(sheet, header, rowIndex, "NominalUnit_S-ALL", "mm");
                Set(sheet, header, rowIndex, "MatchingPipeOd_S-ALL", FormatOd(PipeSizeCatalog.OdSch40Mm(dn)));
                Clear(sheet, header, rowIndex, "EndType_S-ALL");
                Set(sheet, header, rowIndex, "Facing_S-ALL", "RF");
                Set(sheet, header, rowIndex, "PressureClass_S-ALL", "150");
                Set(sheet, header, rowIndex, "WallThickness_S-ALL", "0");
                Set(sheet, header, rowIndex, "EngagementLength_S-ALL", "0");
                Set(sheet, header, rowIndex, "LengthUnit_S-ALL", "mm");
                Set(sheet, header, rowIndex, "BoltSize", bolting.BoltSize);
                Set(sheet, header, rowIndex, "NumberInSet", bolting.NumberInSet);
                Set(sheet, header, rowIndex, "Length", bolting.LengthInches);
                Set(sheet, header, rowIndex, "IsLugSet", "0");
                Set(sheet, header, rowIndex, "StudTypeDescription", "Stud Bolt");
                Set(sheet, header, rowIndex, "StudDescription", "Lg, ASTM A193, B7");
                Set(sheet, header, rowIndex, "BoltCompatibleStd", "ASTM A193");
            }

            PromoteStaticSheetToPublishedLayout(sheet);
        }

        private static void FillStudLjSheet(XLWorkbook workbook)
        {
            IXLWorksheet? sheet = FindStaticSheet(workbook, "STUD_LJ");
            if (sheet == null)
                return;

            var header = ReadHeaderMap(sheet, HeaderRow);
            const string familyDesc = "Studbolt lap joint CL150 FF ASTM A193-B7";
            const string shortDesc = "Bolt set LJ";
            Guid familyId = CatalogExcelPartResolver.StableFamilyId("STUD_LJ_CL150");
            CatalogExcelIsoMetadata iso = CatalogExcelIsoMetadata.StudBoltRf();

            int lastRow = sheet.LastRowUsed()?.RowNumber() ?? DataStartRow;

            for (int rowIndex = DataStartRow; rowIndex <= lastRow; rowIndex++)
            {
                string sizes = GetString(sheet, header, rowIndex, "Sizes");
                if (string.IsNullOrWhiteSpace(sizes) || !int.TryParse(sizes, out int dn))
                    continue;

                if (!CatalogFlangeBoltingCatalog.TryGetLjFfCl150(dn, out FlangeBoltingSpec bolting))
                    continue;

                Guid sizeRecordId = CatalogExcelPartResolver.StableSizeRecordId("STUD_LJ_CL150", dn);

                ApplyIsoMetadata(sheet, header, rowIndex, iso, new CatalogExcelTemplateMetadata());
                Set(sheet, header, rowIndex, "PartFamilyLongDesc", familyDesc);
                Set(sheet, header, rowIndex, "PartSizeLongDesc", $"Studbolt LJ DN{dn} CL150 FF ASTM A193-B7");
                Set(sheet, header, rowIndex, "ShortDescription", shortDesc);
                Set(sheet, header, rowIndex, "PartFamilyId", familyId.ToString("D"));
                Set(sheet, header, rowIndex, "CatalogPartFamilyId", familyId.ToString("D"));
                Set(sheet, header, rowIndex, "PnPClassName", "BoltSet");
                Set(sheet, header, rowIndex, "PartCategory", "Fasteners");
                Set(sheet, header, rowIndex, "ConnectionPortCount", "1");
                Set(sheet, header, rowIndex, "SizeRecordId_S-ALL", sizeRecordId.ToString("D"));
                Set(sheet, header, rowIndex, "PortName_S-ALL", "ALL");
                Set(sheet, header, rowIndex, "NominalDiameter_S-ALL", dn.ToString(CultureInfo.InvariantCulture));
                Set(sheet, header, rowIndex, "NominalUnit_S-ALL", "mm");
                Set(sheet, header, rowIndex, "MatchingPipeOd_S-ALL", FormatOd(PipeSizeCatalog.OdSch40Mm(dn)));
                Clear(sheet, header, rowIndex, "EndType_S-ALL");
                Set(sheet, header, rowIndex, "Facing_S-ALL", "FF");
                Set(sheet, header, rowIndex, "PressureClass_S-ALL", "150");
                Set(sheet, header, rowIndex, "WallThickness_S-ALL", "0");
                Set(sheet, header, rowIndex, "EngagementLength_S-ALL", "0");
                Set(sheet, header, rowIndex, "LengthUnit_S-ALL", "mm");
                Set(sheet, header, rowIndex, "BoltSize", bolting.BoltSize);
                Set(sheet, header, rowIndex, "NumberInSet", bolting.NumberInSet);
                Set(sheet, header, rowIndex, "Length", bolting.LengthInches);
                Set(sheet, header, rowIndex, "IsLugSet", "0");
                Set(sheet, header, rowIndex, "StudTypeDescription", "Stud Bolt");
                Set(sheet, header, rowIndex, "StudDescription", "Lg, ASTM A193, B7, LJ");
                Set(sheet, header, rowIndex, "BoltCompatibleStd", "ASTM A193");
            }

            PromoteStaticSheetToPublishedLayout(sheet);
        }

        private static void FillStaticSheet(
            XLWorkbook workbook,
            string sheetPrefix,
            CatalogExcelIsoMetadata iso,
            Func<int, string, string> buildSizeDesc,
            string? defaultFamilyLongDesc = null,
            string? endType = null)
        {
            IXLWorksheet? sheet = FindStaticSheet(workbook, sheetPrefix);
            if (sheet == null)
                return;

            var header = ReadHeaderMap(sheet, HeaderRow);
            string familyDesc = GetString(sheet, header, DataStartRow, "PartFamilyLongDesc");
            if (string.IsNullOrWhiteSpace(familyDesc) && !string.IsNullOrWhiteSpace(defaultFamilyLongDesc))
                familyDesc = defaultFamilyLongDesc;

            int lastRow = sheet.LastRowUsed()?.RowNumber() ?? DataStartRow;

            for (int rowIndex = DataStartRow; rowIndex <= lastRow; rowIndex++)
            {
                string sizes = GetString(sheet, header, rowIndex, "Sizes");
                if (string.IsNullOrWhiteSpace(sizes) || !int.TryParse(sizes, out int dn))
                    continue;

                ApplyIsoMetadata(sheet, header, rowIndex, iso, new CatalogExcelTemplateMetadata());
                Set(sheet, header, rowIndex, "PartFamilyLongDesc", familyDesc);
                Set(sheet, header, rowIndex, "PartSizeLongDesc", buildSizeDesc(dn, familyDesc));
                if (sheetPrefix.StartsWith("PIPE", StringComparison.OrdinalIgnoreCase))
                    ApplyNativePipeShapeRow(sheet, header, rowIndex, dn);
                if (!string.IsNullOrEmpty(endType))
                    Set(sheet, header, rowIndex, "EndType_S-ALL", endType);
                if (header.ContainsKey("PressureClass_S-ALL"))
                    Set(sheet, header, rowIndex, "PressureClass_S-ALL", "150");
                if (header.ContainsKey("Schedule_S-ALL") && sheetPrefix.StartsWith("PIPE", StringComparison.OrdinalIgnoreCase))
                    Set(sheet, header, rowIndex, "Schedule_S-ALL", "40");
                if (header.ContainsKey("WallThickness_S-ALL") && sheetPrefix.StartsWith("PIPE", StringComparison.OrdinalIgnoreCase))
                    Set(sheet, header, rowIndex, "WallThickness_S-ALL", FormatOd(ResolvePipeWallThicknessMm(dn)));
                if (header.ContainsKey("MatchingPipeOd_S-ALL") && sheetPrefix.StartsWith("PIPE", StringComparison.OrdinalIgnoreCase))
                    Set(sheet, header, rowIndex, "MatchingPipeOd_S-ALL", FormatOd(PipeSizeCatalog.OdSch40Mm(dn)));
                if (header.ContainsKey("NominalDiameter_S-ALL") && sheetPrefix.StartsWith("PIPE", StringComparison.OrdinalIgnoreCase))
                    Set(sheet, header, rowIndex, "NominalDiameter_S-ALL", dn.ToString(CultureInfo.InvariantCulture));
            }

            PromoteStaticSheetToPublishedLayout(sheet);
        }

        /// <summary>Plant 3D native pipe parametric (CPP) — not a custom CUST_*.py script.</summary>
        private static void ApplyNativePipeShapeRow(
            IXLWorksheet sheet,
            Dictionary<string, int> header,
            int rowIndex,
            int dn)
        {
            Set(sheet, header, rowIndex, "ShapeName", "CPP");
            Set(sheet, header, rowIndex, "ContentGeometryParamDefinition", "D,L,OF,");
            if (header.ContainsKey("D"))
                Set(sheet, header, rowIndex, "D", FormatOd(PipeSizeCatalog.OdSch40Mm(dn)));
            Clear(sheet, header, rowIndex, "ScriptPath");
        }

        /// <summary>Drop template display-label row 2 and show machine headers on row 1.</summary>
        private static void PromoteStaticSheetToPublishedLayout(IXLWorksheet sheet)
        {
            HeaderRowSnapshot headerSnapshot = SnapshotMachineHeaderRow(sheet);
            if (sheet.LastRowUsed()?.RowNumber() >= 2
                && LooksLikeDisplayHeaderRow(sheet, 2))
            {
                sheet.Row(2).Delete();
            }

            FinalizePublishedHeaderRow(sheet, headerSnapshot);
        }

        private static bool LooksLikeDisplayHeaderRow(IXLWorksheet sheet, int row)
        {
            int maxCol = sheet.LastColumnUsed()?.ColumnNumber() ?? 20;
            for (int col = 1; col <= maxCol; col++)
            {
                string? text = sheet.Cell(row, col).GetString()?.Trim();
                if (string.IsNullOrEmpty(text))
                    continue;

                if (string.Equals(text, "Sizes", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(text, "Shape Name", StringComparison.OrdinalIgnoreCase)
                    || text.Contains("Size Record Id", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static string BuildPipeSizeLongDesc(int dn, string familyDesc)
        {
            if (familyDesc.Contains("Plain Ends", StringComparison.OrdinalIgnoreCase))
                return familyDesc.Replace("Plain Ends", $"DN{dn} Plain Ends", StringComparison.OrdinalIgnoreCase);

            return $"Pipe Smls DN{dn} Plain Ends CS ASTM A106-B Dims to ASME B36.10";
        }

        private static string BuildStudSizeLongDesc(int dn, string familyDesc) =>
            $"Studbolt DN{dn} ASTM A193-B7-Nuts ASTM A194-2H";

        private static int WriteSizeRows(
            IXLWorksheet sheet,
            CatalogExcelPartRow row,
            CatalogExcelTemplateMetadata metadata,
            Guid familyId,
            string scriptPath,
            IReadOnlyList<CatalogExcelSizeVariant> sizes,
            ValveProject? project,
            List<string> warnings)
        {
            HeaderRowSnapshot headerSnapshot = SnapshotMachineHeaderRow(sheet);
            ClearPublishedDataRows(sheet);

            var header = ReadHeaderMap(sheet, HeaderRow);
            bool hasDn2 = header.ContainsKey("DN2");
            bool hasCel = header.ContainsKey("CEL");
            bool hasT = header.ContainsKey("T");
            bool hasL = header.ContainsKey("L");
            bool hasD1 = header.ContainsKey("D1");

            string familyDesc = !string.IsNullOrWhiteSpace(row.FamilyLongDesc)
                ? row.FamilyLongDesc
                : IsUsableFamilyLongDesc(metadata.FamilyLongDesc)
                    ? metadata.FamilyLongDesc
                    : row.ShortDescription;
            string material = string.IsNullOrWhiteSpace(metadata.Material) ? row.Material : metadata.Material;
            CatalogExcelIsoMetadata iso = CatalogExcelIsoMetadata.Resolve(row.Part);
            string paramDef = !string.IsNullOrWhiteSpace(metadata.ContentGeometryParamDefinition)
                ? metadata.ContentGeometryParamDefinition
                : BuildContentGeometryParamDefinition(row.Part.Id, hasDn2, hasCel, hasT, hasL, hasD1);

            int written = 0;
            int rowIndex = PublishedDataStartRow;

            foreach (CatalogExcelSizeVariant size in sizes)
            {
                CatalogExcelPortExportPlan portPlan = CatalogExcelPortExportPlanner.Build(header, row, size, project);
                if (!string.IsNullOrWhiteSpace(portPlan.Warning))
                    warnings.Add($"{row.Part.Id}: {portPlan.Warning}");

                WriteSizeRow(
                    sheet,
                    header,
                    rowIndex,
                    row,
                    metadata,
                    iso,
                    familyId,
                    scriptPath,
                    size,
                    paramDef,
                    familyDesc,
                    material,
                    project,
                    portPlan);
                rowIndex++;
                written++;
            }

            FinalizePublishedHeaderRow(sheet, headerSnapshot);
            return written;
        }

        private sealed class HeaderRowSnapshot
        {
            public Dictionary<int, string> Row1 { get; } = [];
        }

        /// <summary>Preserve row 1 machine field names; row 2 display labels are dropped on publish.</summary>
        private static HeaderRowSnapshot SnapshotMachineHeaderRow(IXLWorksheet sheet)
        {
            var snapshot = new HeaderRowSnapshot();
            int maxCol = sheet.LastColumnUsed()?.ColumnNumber() ?? 1;
            for (int col = 1; col <= maxCol; col++)
                snapshot.Row1[col] = sheet.Cell(HeaderRow, col).GetString();

            return snapshot;
        }

        /// <summary>Restore machine headers and unhide row 1 (template hides row 1 in favor of row 2 labels).</summary>
        private static void FinalizePublishedHeaderRow(IXLWorksheet sheet, HeaderRowSnapshot snapshot)
        {
            foreach (KeyValuePair<int, string> entry in snapshot.Row1)
            {
                if (!string.IsNullOrEmpty(entry.Value))
                    sheet.Cell(HeaderRow, entry.Key).SetValue(entry.Value);
            }

            sheet.Row(HeaderRow).Unhide();
        }

        private static void RemoveCatalogDataFlagSheet(XLWorkbook workbook)
        {
            IXLWorksheet? flag = workbook.Worksheets.FirstOrDefault(w =>
                w.Name.Equals("Catalog Data Flag", StringComparison.OrdinalIgnoreCase));
            if (flag != null)
                workbook.Worksheets.Delete(flag.Name);
        }

        private static void WriteSizeRow(
            IXLWorksheet sheet,
            Dictionary<string, int> header,
            int rowIndex,
            CatalogExcelPartRow row,
            CatalogExcelTemplateMetadata metadata,
            CatalogExcelIsoMetadata iso,
            Guid familyId,
            string scriptPath,
            CatalogExcelSizeVariant size,
            string paramDef,
            string familyDesc,
            string material,
            ValveProject? project,
            CatalogExcelPortExportPlan portPlan)
        {
            int dn = size.Dn;
            double od = PipeSizeCatalog.OdSch40Mm(dn);

            Set(sheet, header, rowIndex, "Sizes", BuildCatalogSizesLabel(row.Part.Id, size));
            Set(sheet, header, rowIndex, "DN", dn);

            if (header.ContainsKey("Previews"))
                Set(sheet, header, rowIndex, "Previews", "Preview");

            Set(sheet, header, rowIndex, "ShapeName", row.Part.CatalogFunctionName);
            Set(sheet, header, rowIndex, "ScriptPath", scriptPath);
            if (size.Dn2.HasValue)
                Set(sheet, header, rowIndex, "DN2", size.Dn2.Value);
            if (size.Cel.HasValue)
                Set(sheet, header, rowIndex, "CEL", size.Cel.Value);
            if (size.T.HasValue)
                Set(sheet, header, rowIndex, "T", size.T.Value);
            Set(sheet, header, rowIndex, "ContentGeometryParamDefinition", paramDef);

            if (CatalogLapJointIds.IsLjStubOrCollar(row.Part.Id)
                && CatalogStubEndTable.TryGet(
                    dn,
                    CatalogStubEndTable.ResolvePattern(row.Part.Id),
                    out CatalogStubEndTable.StubEndDims stubMeta))
            {
                // Plant CollarLapped reads L,B,D1,D2 from catalog — not just script ports.
                Set(sheet, header, rowIndex, "L", FormatOd(stubMeta.L));
                Set(sheet, header, rowIndex, "B", FormatOd(stubMeta.B));
                Set(sheet, header, rowIndex, "D1", FormatOd(stubMeta.D1));
                Set(sheet, header, rowIndex, "D2", FormatOd(PipeSizeCatalog.OdSch40Mm(dn)));
                Set(sheet, header, rowIndex, "OF", "-1");
                Set(sheet, header, rowIndex, "FlangeOffset", FormatOd(stubMeta.B));
            }
            else if (row.Part.Id.StartsWith("LJ_RING_", StringComparison.OrdinalIgnoreCase))
            {
                if (CatalogLjRingCl150Table.TryGet(dn, out CatalogLjRingCl150Table.LjRingDims ljRing))
                {
                    Set(sheet, header, rowIndex, "L", FormatOd(ljRing.L));
                    // CollarLapped native path: B = lap-joint flange plate thickness (ASME Table 8 col 4 tf).
                    Set(sheet, header, rowIndex, "B", FormatOd(ljRing.Tf));
                    Set(sheet, header, rowIndex, "D1", FormatOd(ljRing.D1));
                    Set(sheet, header, rowIndex, "D2", FormatOd(ljRing.D2));
                }
            }
            else
            {
                WriteValveGeometryFromProject(sheet, header, rowIndex, row, dn, paramDef, project);
            }

            WritePorts(sheet, header, rowIndex, row, size, dn, od, portPlan);

            WriteElbowRoutingGeometry(sheet, header, rowIndex, row, dn);

            Set(sheet, header, rowIndex, "ShortDescription", row.ShortDescription);
            Set(sheet, header, rowIndex, "PartFamilyLongDesc", familyDesc);
            Set(sheet, header, rowIndex, "PartSizeLongDesc", BuildPartSizeLongDesc(row.ShortDescription, size));
            Set(sheet, header, rowIndex, "PartFamilyId", familyId.ToString("D"));
            Set(sheet, header, rowIndex, "CatalogPartFamilyId", familyId.ToString("D"));
            Set(sheet, header, rowIndex, "ConnectionPortCount", portPlan.ConnectionPortCount.ToString(CultureInfo.InvariantCulture));
            Set(sheet, header, rowIndex, "PartCategory", row.PartCategory);
            Set(sheet, header, rowIndex, "PnPClassName", row.PnPClassName);
            Set(sheet, header, rowIndex, "Material", material);
            ApplyIsoMetadata(sheet, header, rowIndex, iso, metadata);

            WritePressureAndSchedule(sheet, header, rowIndex, row, dn, portPlan);
        }

        private static void WritePorts(
            IXLWorksheet sheet,
            Dictionary<string, int> header,
            int rowIndex,
            CatalogExcelPartRow row,
            CatalogExcelSizeVariant size,
            int dn,
            double od,
            CatalogExcelPortExportPlan plan)
        {
            string partId = row.Part.Id;
            int? dn2 = size.Dn2;
            int portIndex = 0;

            foreach ((CatalogExcelLogicalPort logical, string suffix) in plan.SheetPorts)
            {
                portIndex++;
                int portDn = CatalogExcelPortExportPlanner.ResolvePortDn(plan, suffix, row, size, dn, od);
                double portOd = PipeSizeCatalog.OdSch40Mm(portDn);
                Guid recordId = CatalogExcelPartResolver.StableSizeRecordId(partId, dn, dn2, portIndex);
                var spec = new PortWriteSpec((logical.EndType, logical.PortName), suffix, portDn, portOd, recordId);
                WritePortColumns(sheet, header, rowIndex, spec, row, plan);
            }
        }

        private static void ApplyIsoMetadata(
            IXLWorksheet sheet,
            Dictionary<string, int> header,
            int rowIndex,
            CatalogExcelIsoMetadata iso,
            CatalogExcelTemplateMetadata template)
        {
            string compatible = FirstNonEmpty(iso.CompatibleStandard, template.CompatibleStandard);
            string designStd = FirstNonEmpty(iso.DesignStd, template.DesignStd);
            string isoType = FirstNonEmpty(iso.IsoType, template.IsoType);
            string isoSkey = FirstNonEmpty(iso.IsoSkey, template.IsoSkey);
            string contentIso = FirstNonEmpty(iso.ContentIsoSymbolDefinition, template.ContentIsoSymbolDefinition);

            Set(sheet, header, rowIndex, "CompatibleStandard", compatible);
            Set(sheet, header, rowIndex, "DesignStd", designStd);
            Set(sheet, header, rowIndex, "SKEY", isoSkey);
            Set(sheet, header, rowIndex, "TYPE", isoType);
            Set(sheet, header, rowIndex, "ContentIsoSymbolDefinition", contentIso);
        }

        private static bool IsUsableFamilyLongDesc(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            string trimmed = value.Trim();
            if (trimmed.Length < 12)
                return false;

            if (int.TryParse(trimmed, out _))
                return false;

            return true;
        }

        private static string FirstNonEmpty(params string?[] values)
        {
            foreach (string? value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    return value.Trim();
            }

            return string.Empty;
        }

        private static void WritePressureAndSchedule(
            IXLWorksheet sheet,
            Dictionary<string, int> header,
            int rowIndex,
            CatalogExcelPartRow row,
            int dn,
            CatalogExcelPortExportPlan plan)
        {
            string pressureClass = plan.PressureClass;
            CatalogExcelPortLayout layout = plan.EffectiveLayout;

            if (CatalogLapJointIds.IsLjStubOrCollar(row.Part.Id))
            {
                WriteStubEndPortExtras(sheet, header, rowIndex, row, dn, pressureClass);
                return;
            }

            if (row.Part.Id.StartsWith("LJ_RING_", StringComparison.OrdinalIgnoreCase))
            {
                WriteLjRingPortExtras(sheet, header, rowIndex, row, dn, pressureClass);
                return;
            }

            switch (layout)
            {
                case CatalogExcelPortLayout.DualFlange:
                    SetFlangePortExtras(sheet, header, rowIndex, pressureClass, "S1", "S2");
                    return;

                case CatalogExcelPortLayout.DualPortBv:
                    WriteBvPortExtras(sheet, header, rowIndex, row, pressureClass, "S1", "S2");
                    return;

                case CatalogExcelPortLayout.TriplePortBv:
                    WriteBvPortExtras(sheet, header, rowIndex, row, pressureClass, "S1", "S2", "S3");
                    return;
            }

            bool isBwFitting = IsBwFitting(row);
            if (header.ContainsKey("PressureClass_S-ALL") && !isBwFitting)
                Set(sheet, header, rowIndex, "PressureClass_S-ALL", pressureClass);

            if (isBwFitting && !string.IsNullOrEmpty(row.PipeSchedule))
                Set(sheet, header, rowIndex, "Schedule_S-ALL", row.PipeSchedule);

            if (row.Part.Id.StartsWith("BLD_", StringComparison.OrdinalIgnoreCase)
                && CatalogFlangeCl150RfTable.TryGetTf(dn, out double blindTf))
            {
                Set(sheet, header, rowIndex, "FlangeThickness_S-ALL", FormatOd(blindTf));
            }

            SetPortLengthExtras(sheet, header, rowIndex, "S-ALL");
        }

        private static void WriteLjRingPortExtras(
            IXLWorksheet sheet,
            Dictionary<string, int> header,
            int rowIndex,
            CatalogExcelPartRow row,
            int dn,
            string pressureClass)
        {
            SetFlangePortExtras(sheet, header, rowIndex, pressureClass, "S1");
            foreach (string suffix in new[] { "S1", "S2" })
            {
                if (CatalogLjRingCl150Table.TryGet(dn, out CatalogLjRingCl150Table.LjRingDims lj))
                    Set(sheet, header, rowIndex, $"FlangeThickness_{suffix}", FormatOd(lj.Tf));
            }

            Set(sheet, header, rowIndex, "Facing_S1", "FF");
            Clear(sheet, header, rowIndex, "Facing_S2");

            // Native Plant FLANGE LJ — no EngagementLength; axial offset is catalog L + LAP port.
            Set(sheet, header, rowIndex, "EngagementLength_S1", "0");
            Set(sheet, header, rowIndex, "EngagementLength_S2", "0");
            Set(sheet, header, rowIndex, "WallThickness_S2", "0");
            Set(sheet, header, rowIndex, "LengthUnit_S1", "mm");
            Set(sheet, header, rowIndex, "LengthUnit_S2", "mm");
        }

        private static void WriteStubEndPortExtras(
            IXLWorksheet sheet,
            Dictionary<string, int> header,
            int rowIndex,
            CatalogExcelPartRow row,
            int dn,
            string pressureClass)
        {
            string lapWall = "0";
            if (CatalogStubEndTable.TryGet(
                dn,
                CatalogStubEndTable.ResolvePattern(row.Part.Id),
                out CatalogStubEndTable.StubEndDims stub))
                lapWall = FormatOd(stub.B);

            foreach (string suffix in new[] { "S1", "S2" })
            {
                if (header.ContainsKey($"PressureClass_{suffix}"))
                    Set(sheet, header, rowIndex, $"PressureClass_{suffix}", pressureClass);
                if (!string.IsNullOrEmpty(row.PipeSchedule))
                    Set(sheet, header, rowIndex, $"Schedule_{suffix}", row.PipeSchedule);
                // S1 LAP = lap thickness; S2 BV = pipe end (no collar wall on weld port).
                Set(sheet, header, rowIndex, $"WallThickness_{suffix}",
                    suffix.Equals("S1", StringComparison.OrdinalIgnoreCase) ? lapWall : "0");
                Set(sheet, header, rowIndex, $"EngagementLength_{suffix}", "0");
                Set(sheet, header, rowIndex, $"LengthUnit_{suffix}", "mm");
            }
        }

        private static void SetFlangePortExtras(
            IXLWorksheet sheet,
            Dictionary<string, int> header,
            int rowIndex,
            string pressureClass,
            params string[] suffixes)
        {
            foreach (string suffix in suffixes)
            {
                Set(sheet, header, rowIndex, $"PressureClass_{suffix}", pressureClass);
                SetPortLengthExtras(sheet, header, rowIndex, suffix);
            }
        }

        private static void WriteBvPortExtras(
            IXLWorksheet sheet,
            Dictionary<string, int> header,
            int rowIndex,
            CatalogExcelPartRow row,
            string pressureClass,
            params string[] suffixes)
        {
            bool isBwFitting = IsBwFitting(row);
            foreach (string suffix in suffixes)
            {
                if (header.ContainsKey($"PressureClass_{suffix}"))
                    Set(sheet, header, rowIndex, $"PressureClass_{suffix}", pressureClass);
                if (isBwFitting && !string.IsNullOrEmpty(row.PipeSchedule))
                    Set(sheet, header, rowIndex, $"Schedule_{suffix}", row.PipeSchedule);
                SetPortLengthExtras(sheet, header, rowIndex, suffix);
            }
        }

        private static void SetPortLengthExtras(
            IXLWorksheet sheet,
            Dictionary<string, int> header,
            int rowIndex,
            string suffix)
        {
            Set(sheet, header, rowIndex, $"WallThickness_{suffix}", "0");
            Set(sheet, header, rowIndex, $"EngagementLength_{suffix}", "0");
            Set(sheet, header, rowIndex, $"LengthUnit_{suffix}", "mm");
        }

        private static bool IsBwFitting(CatalogExcelPartRow row) =>
            row.Part.Group.Equals("Fitting", StringComparison.OrdinalIgnoreCase)
            && CatalogFlangeFacing.IsButtWeldEndType(row.FittingEndType);

        /// <summary>Long Description = Short Description + Size (e.g. "FLANGE WN DN50", reducers "DN80x50").</summary>
        private static string BuildPartSizeLongDesc(string shortDescription, CatalogExcelSizeVariant size)
        {
            string dnTag = size.Dn2.HasValue
                ? $"DN{size.Dn}x{size.Dn2.Value}"
                : $"DN{size.Dn}";

            string shortDesc = (shortDescription ?? string.Empty).Trim();
            return shortDesc.Length > 0 ? $"{shortDesc} {dnTag}" : dnTag;
        }

        /// <summary>
        /// Native Plant 3D Elbow class requires PathAngle + CurveRadius (ISREMOVABLE=FALSE).
        /// Cloning an ELBOW template sheet carries these columns; if they stay blank the catalog
        /// part has no routing geometry and insert fails with "Can't find symbol for specified part"
        /// (even though the custom script runs fine in Test Catalog).
        /// </summary>
        private static void WriteElbowRoutingGeometry(
            IXLWorksheet sheet,
            Dictionary<string, int> header,
            int rowIndex,
            CatalogExcelPartRow row,
            int dn)
        {
            if (!header.ContainsKey("PathAngle") || !header.ContainsKey("CurveRadius"))
                return;

            double angle = ResolveElbowPathAngleDeg(row.Part);
            double curveRadius = FittingDimensionService.ElbowCenterToFaceMm(
                dn, FittingDimensionService.ConnectionStyle.ButtWeld);
            if (curveRadius <= 0)
                curveRadius = PipeSizeCatalog.OdSch40Mm(dn) * 1.5;

            Set(sheet, header, rowIndex, "PathAngle", FormatOd(angle));
            Set(sheet, header, rowIndex, "CurveRadius", FormatOd(curveRadius));
            if (header.ContainsKey("SegmentCount"))
                Set(sheet, header, rowIndex, "SegmentCount", "0");
        }

        /// <summary>Elbow turn angle (deg) inferred from part id / clone source / display name; default 90.</summary>
        private static double ResolveElbowPathAngleDeg(CustomPartDefinition part)
        {
            string text = (part.Id + " " + part.ExcelCloneSourcePartId + " " + part.DisplayName)
                .ToUpperInvariant();

            if (System.Text.RegularExpressions.Regex.IsMatch(text, @"\b180\b") || text.Contains("_180_"))
                return 180;
            if (System.Text.RegularExpressions.Regex.IsMatch(text, @"\b45\b") || text.Contains("_45_"))
                return 45;
            return 90;
        }

        private static void WriteValveGeometryFromProject(
            IXLWorksheet sheet,
            Dictionary<string, int> header,
            int rowIndex,
            CatalogExcelPartRow row,
            int dn,
            string paramDef,
            ValveProject? project)
        {
            if (!row.Part.Group.Equals("Valve", StringComparison.OrdinalIgnoreCase))
                return;

            IReadOnlyList<string> paramNames = CatalogExcelGeometryParams.ParseParamNames(paramDef);

            if (paramNames.Any(n => n.Equals("L", StringComparison.OrdinalIgnoreCase))
                && header.ContainsKey("L"))
            {
                double l = ResolveValveFaceToFaceMm(dn, project);
                Set(sheet, header, rowIndex, "L", FormatOd(l));
            }

            if (paramNames.Any(n => n.Equals("D1", StringComparison.OrdinalIgnoreCase))
                && header.ContainsKey("D1"))
            {
                double d1 = ResolveValveBodyOdMm(dn, project);
                Set(sheet, header, rowIndex, "D1", FormatOd(d1));
            }
        }

        private static double ResolveValveFaceToFaceMm(int dn, ValveProject? project)
        {
            if (project != null && project.Parameters.FaceToFace > 0)
                return project.Parameters.FaceToFace;

            // Placeholder face-to-face (mm) so Catalog Builder accepts CGPD=DN,L rows.
            return Math.Max(150, dn * 2);
        }

        private static double ResolveValveBodyOdMm(int dn, ValveProject? project)
        {
            if (project != null
                && project.Parameters.BodyOD > 0
                && (int)Math.Round(project.Parameters.DN) == dn)
            {
                return project.Parameters.BodyOD;
            }

            return PipeSizeCatalog.OdSch40Mm(dn);
        }

        private static string BuildContentGeometryParamDefinition(
            string partId,
            bool hasDn2,
            bool hasCel,
            bool hasT,
            bool hasL = false,
            bool hasD1 = false)
        {
            // Catalog Builder template: parameter NAMES in ContentGeometryParamDefinition;
            // per-size VALUES go in DN / DN2 / CEL / T / L / D1 columns.
            if (hasL || hasD1)
            {
                var names = new List<string> { "DN" };
                if (hasL)
                    names.Add("L");
                if (hasD1)
                    names.Add("D1");
                if (hasDn2)
                    names.Add("DN2");
                return string.Join(",", names);
            }

            if (partId.StartsWith("SO_", StringComparison.OrdinalIgnoreCase) && hasCel)
                return "DN,CEL";

            if (partId.StartsWith("GSK_", StringComparison.OrdinalIgnoreCase))
                return hasT ? "DN,T" : "DN";

            if (hasDn2)
                return "DN,DN2";

            if (hasCel)
                return "DN,CEL";

            if (hasT)
                return "DN,T";

            return "DN";
        }

        private static void WritePortColumns(
            IXLWorksheet sheet,
            Dictionary<string, int> header,
            int rowIndex,
            PortWriteSpec port,
            CatalogExcelPartRow row,
            CatalogExcelPortExportPlan plan)
        {
            Set(sheet, header, rowIndex, $"SizeRecordId_{port.Suffix}", port.SizeRecordId.ToString("D"));
            Set(sheet, header, rowIndex, $"PortName_{port.Suffix}", port.Port.PortName);

            Set(sheet, header, rowIndex, $"NominalDiameter_{port.Suffix}", port.Dn.ToString(CultureInfo.InvariantCulture));
            Set(sheet, header, rowIndex, $"NominalUnit_{port.Suffix}", "mm");
            Set(sheet, header, rowIndex, $"MatchingPipeOd_{port.Suffix}", FormatOd(
                ResolveMatchingPipeOdMm(row.Part.Id, port.Dn, port.Port.EndType)));

            Set(sheet, header, rowIndex, $"EndType_{port.Suffix}", port.Port.EndType);

            // LAP/BV collar ports — no pressure class (Plant CollarLapped matches on ND + Collar class).
            if (port.Port.EndType.Equals("FL", StringComparison.OrdinalIgnoreCase)
                || port.Port.EndType.Equals("SO", StringComparison.OrdinalIgnoreCase))
            {
                Set(sheet, header, rowIndex, $"PressureClass_{port.Suffix}", plan.PressureClass);
            }

            // RF on WN/BLD/gasket FL ports; LJ backing ring is FF; LAP/BV have no facing.
            string? facing = ResolvePortFacing(row.Part, port.Port.EndType, plan.FlangeFacing);
            if (facing != null)
                Set(sheet, header, rowIndex, $"Facing_{port.Suffix}", facing);
        }

        private static string? ResolvePortFacing(CustomPartDefinition part, string endType, string? familyFacingOverride = null)
        {
            if (endType.Equals("FL", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(familyFacingOverride))
                return CatalogFlangeFacing.Normalize(familyFacingOverride);

            if (endType.Equals("FL", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(part.FlangeFacing))
                return CatalogFlangeFacing.Normalize(part.FlangeFacing);

            string partId = part.Id;
            if (partId.StartsWith("GSK_FF_", StringComparison.OrdinalIgnoreCase))
                return "FF";

            if (partId.StartsWith("GSK_", StringComparison.OrdinalIgnoreCase))
                return "RF";

            if (partId.StartsWith("LJ_RING_", StringComparison.OrdinalIgnoreCase)
                && endType.Equals("FL", StringComparison.OrdinalIgnoreCase))
                return "FF";

            if (endType.Equals("FL", StringComparison.OrdinalIgnoreCase))
                return "RF";

            return null;
        }

        private static string ResolveScriptPath(string partId, List<string> warnings)
        {
            string scriptPartId = CatalogLapJointIds.IsCollarExport(partId)
                ? CatalogLapJointIds.StubExportIdFromCollar(partId)
                : partId;

            // Deploy writes flat CustomScripts/CUST_{partId}.py (no parts/<id>/ subfolders).
            string flat = Path.Combine(ProjectPaths.CustomScriptsDir, $"CUST_{scriptPartId}.py");
            if (File.Exists(flat))
                return flat;

            string? geometry = CatalogPortTemplates.TryLoadGeometryScriptPath(scriptPartId);
            if (!string.IsNullOrEmpty(geometry) && File.Exists(geometry))
            {
                warnings.Add(
                    $"{partId}: script not deployed — using dev path. Run Deploy Catalog before Publish.");
                return geometry;
            }

            warnings.Add($"{partId}: CUST_{scriptPartId}.py not found in CustomScripts.");
            return flat;
        }

        private static Dictionary<string, int> ReadHeaderMap(IXLWorksheet sheet, int headerRow)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            MergeHeaderRow(sheet, 1, map);
            if (headerRow != 1)
                MergeHeaderRow(sheet, headerRow, map);
            return map;
        }

        private static void MergeHeaderRow(IXLWorksheet sheet, int row, Dictionary<string, int> map)
        {
            foreach (IXLCell cell in sheet.Row(row).CellsUsed())
            {
                string? text = cell.GetString()?.Trim();
                if (string.IsNullOrEmpty(text))
                    continue;

                map.TryAdd(text, cell.Address.ColumnNumber);
            }
        }

        /// <summary>
        /// Catalog Builder reducing size key (ASME export uses NPS combo e.g. 4"x3").
        /// </summary>
        private static string BuildCatalogSizesLabel(string partId, CatalogExcelSizeVariant size)
        {
            if (!size.Dn2.HasValue)
                return size.Dn.ToString(CultureInfo.InvariantCulture);

            PipeSizeOption? large = PipeSizeCatalog.FindByDn(size.Dn);
            PipeSizeOption? small = PipeSizeCatalog.FindByDn(size.Dn2.Value);
            if (large != null && small != null)
                return $"{large.NpsLabel}\"x{small.NpsLabel}\"";

            return $"{size.Dn}x{size.Dn2.Value}";
        }

        private static double ResolveMatchingPipeOdMm(string partId, int dn, string endType)
        {
            if (CatalogLapJointIds.IsLjStubOrCollar(partId)
                || partId.StartsWith("LJ_RING_", StringComparison.OrdinalIgnoreCase))
                return PipeSizeCatalog.OdSch40Mm(dn);

            return PipeSizeCatalog.OdSch40Mm(dn);
        }

        private static void ClearNativeParametricColumns(
            IXLWorksheet sheet,
            Dictionary<string, int> header,
            int rowIndex,
            string partId)
        {
            foreach (string column in CatalogLapJointIds.IsLjStubOrCollar(partId)
                ? new[] { "L", "B", "D1", "D2" }
                : new[] { "L", "D1", "D2" })
                Clear(sheet, header, rowIndex, column);
        }

        private static string GetString(
            IXLWorksheet sheet,
            Dictionary<string, int> header,
            int row,
            string column)
        {
            if (!header.TryGetValue(column, out int col))
                return string.Empty;

            return sheet.Cell(row, col).GetString()?.Trim() ?? string.Empty;
        }

        private static void Clear(IXLWorksheet sheet, Dictionary<string, int> header, int row, string column)
        {
            if (!header.TryGetValue(column, out int col))
                return;

            sheet.Cell(row, col).Clear();
        }

        private static void Set(IXLWorksheet sheet, Dictionary<string, int> header, int row, string column, object value)
        {
            if (!header.TryGetValue(column, out int col))
                return;

            IXLCell cell = sheet.Cell(row, col);
            switch (value)
            {
                case string text:
                    cell.SetValue(text);
                    break;
                case int i:
                    cell.SetValue(i);
                    break;
                case double d:
                    cell.SetValue(d);
                    break;
                default:
                    cell.SetValue(value.ToString() ?? string.Empty);
                    break;
            }
        }

        private static void ClearPublishedDataRows(IXLWorksheet sheet)
        {
            if (sheet.LastRowUsed()?.RowNumber() >= PublishedDataStartRow)
                sheet.Rows(PublishedDataStartRow, sheet.LastRowUsed()!.RowNumber()).Delete();
        }

        private static void ClearDataRows(IXLWorksheet sheet)
        {
            if (sheet.LastRowUsed()?.RowNumber() >= DataStartRow)
                sheet.Rows(DataStartRow, sheet.LastRowUsed()!.RowNumber()).Delete();
        }

        private static string FormatOd(double od) =>
            od.ToString("0.##", CultureInfo.InvariantCulture);

        /// <summary>ASME B36.10M Sch-40 nominal wall (mm) — mirrors catalog_generator/pipe_sizes.py.</summary>
        private static double ResolvePipeWallThicknessMm(int dn) => dn switch
        {
            15 => 2.77,
            20 => 2.87,
            25 => 3.38,
            32 => 3.56,
            40 => 3.68,
            50 => 3.91,
            65 => 4.78,
            80 => 5.49,
            90 => 5.74,
            100 => 6.02,
            125 => 6.55,
            150 => 7.11,
            200 => 8.18,
            250 => 9.27,
            300 => 10.31,
            350 => 11.13,
            400 => 12.70,
            450 => 14.27,
            _ => 0,
        };

    }
}
