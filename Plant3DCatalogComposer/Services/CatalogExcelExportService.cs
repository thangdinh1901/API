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
    }

    internal static class CatalogExcelExportService
    {
        private const int DataStartRow = 3;
        private const int HeaderRow = 2;

        private readonly record struct PortWriteSpec(
            (string EndType, string PortName) Port,
            string Suffix,
            int Dn,
            double Od,
            Guid SizeRecordId);

        public static string ResolveTemplatePath()
        {
            string pluginPath = Path.Combine(ProjectPaths.PluginDirectory, "Resources", "CatalogBuilderTemplate.xlsx");
            if (File.Exists(pluginPath))
                return pluginPath;

            string devPath = Path.Combine(
                ProjectPaths.TryResolveApiRoot() ?? ProjectPaths.PluginDirectory,
                "Plant3DCatalogComposer",
                "Resources",
                "CatalogBuilderTemplate.xlsx");
            if (File.Exists(devPath))
                return devPath;

            throw new FileNotFoundException(
                "Catalog Builder Excel template not found. Rebuild the plugin to deploy Resources/CatalogBuilderTemplate.xlsx.");
        }

        public static CatalogExcelExportResult Export(
            string outputPath,
            ValveProject? project = null,
            IReadOnlyList<string>? partIdFilter = null)
        {
            string templatePath = ResolveTemplatePath();
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
                IXLWorksheet? sheet = FindTemplateSheet(workbook, row.Part.Id);
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

                int written = WriteSizeRows(sheet, row, metadata, familyId, scriptPath, sizes);
                totalRows += written;
                filledSheets.Add(sheet.Name);
            }

            foreach (CustomPartDefinition part in CustomPartCatalog.InsertableParts)
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

            if (partIdFilter is { Count: > 0 })
            {
                int removedSheets = PruneUnexportedPartSheets(
                    workbook,
                    filledSheets,
                    keepStaticSupportSheets: partIdFilter.Count > 1);
                if (removedSheets > 0)
                {
                    warnings.Add(
                        partIdFilter.Count == 1
                            ? $"Workbook trimmed to {filledSheets.Count} part sheet(s) only."
                            : $"Removed {removedSheets} unused part sheet(s) from workbook.");
                }
            }

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");
            if (partIdFilter is not { Count: 1 })
                FillStaticTemplateSheets(workbook);
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

        private static IXLWorksheet? FindTemplateSheet(XLWorkbook workbook, string partId)
        {
            string prefix = partId + ",";
            return workbook.Worksheets
                .FirstOrDefault(w => w.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        }

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
            };
        }

        private static IXLWorksheet? FindStaticSheet(XLWorkbook workbook, string sheetPrefix) =>
            workbook.Worksheets.FirstOrDefault(w =>
                w.Name.StartsWith(sheetPrefix, StringComparison.OrdinalIgnoreCase));

        private static void FillStaticTemplateSheets(XLWorkbook workbook)
        {
            FillPipeSheet(workbook);
            FillStudRfSheet(workbook);
            FillStudLjSheet(workbook);
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
                Set(sheet, header, rowIndex, "NominalDiameter_S-ALL", dn.ToString(System.Globalization.CultureInfo.InvariantCulture));
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
                if (!string.IsNullOrEmpty(endType))
                    Set(sheet, header, rowIndex, "EndType_S-ALL", endType);
                if (header.ContainsKey("PressureClass_S-ALL"))
                    Set(sheet, header, rowIndex, "PressureClass_S-ALL", "150");
                if (header.ContainsKey("Schedule_S-ALL") && sheetPrefix.StartsWith("PIPE", StringComparison.OrdinalIgnoreCase))
                    Set(sheet, header, rowIndex, "Schedule_S-ALL", "40");
            }
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
            IReadOnlyList<CatalogExcelSizeVariant> sizes)
        {
            ClearDataRows(sheet);

            var header = ReadHeaderMap(sheet, HeaderRow);
            bool hasDn2 = header.ContainsKey("DN2");
            bool hasCel = header.ContainsKey("CEL");
            bool hasT = header.ContainsKey("T");

            string familyDesc = IsUsableFamilyLongDesc(metadata.FamilyLongDesc)
                ? metadata.FamilyLongDesc
                : row.FamilyLongDesc;
            string material = string.IsNullOrWhiteSpace(metadata.Material) ? row.Material : metadata.Material;
            CatalogExcelIsoMetadata iso = CatalogExcelIsoMetadata.Resolve(row.Part);
            string paramDef = BuildContentGeometryParamDefinition(row.Part.Id, hasDn2, hasCel, hasT);

            int written = 0;
            int rowIndex = DataStartRow;

            foreach (CatalogExcelSizeVariant size in sizes)
            {
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
                    material);
                rowIndex++;
                written++;
            }

            return written;
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
            string material)
        {
            int dn = size.Dn;
            double od = PipeSizeCatalog.OdSch40Mm(dn);

            Set(sheet, header, rowIndex, "Sizes", BuildCatalogSizesLabel(row.Part.Id, size));
            Set(sheet, header, rowIndex, "DN", dn);

            Set(sheet, header, rowIndex, "Shape Name", row.Part.CatalogFunctionName);
            Set(sheet, header, rowIndex, "ShapeName", row.Part.CatalogFunctionName);
            Set(sheet, header, rowIndex, "Script Path", scriptPath);
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
                    Set(sheet, header, rowIndex, "D1", FormatOd(ljRing.D1));
                    Set(sheet, header, rowIndex, "D2", FormatOd(ljRing.D2));
                }
            }

            WritePorts(sheet, header, rowIndex, row, size, dn, od);

            Set(sheet, header, rowIndex, "ShortDescription", row.ShortDescription);
            Set(sheet, header, rowIndex, "PartFamilyLongDesc", familyDesc);
            Set(sheet, header, rowIndex, "PartSizeLongDesc", BuildPartSizeLongDesc(row.Part, familyDesc, size));
            Set(sheet, header, rowIndex, "PartFamilyId", familyId.ToString("D"));
            Set(sheet, header, rowIndex, "CatalogPartFamilyId", familyId.ToString("D"));
            Set(sheet, header, rowIndex, "ConnectionPortCount", row.PortCount.ToString(CultureInfo.InvariantCulture));
            Set(sheet, header, rowIndex, "PartCategory", row.PartCategory);
            Set(sheet, header, rowIndex, "PnPClassName", row.PnPClassName);
            Set(sheet, header, rowIndex, "Material", material);
            ApplyIsoMetadata(sheet, header, rowIndex, iso, metadata);

            WritePressureAndSchedule(sheet, header, rowIndex, row, dn);
        }

        private static void WritePorts(
            IXLWorksheet sheet,
            Dictionary<string, int> header,
            int rowIndex,
            CatalogExcelPartRow row,
            CatalogExcelSizeVariant size,
            int dn,
            double od)
        {
            foreach (PortWriteSpec port in EnumeratePorts(row, size, dn, od))
                WritePortColumns(sheet, header, rowIndex, port, row);
        }

        private static IEnumerable<PortWriteSpec> EnumeratePorts(
            CatalogExcelPartRow row,
            CatalogExcelSizeVariant size,
            int dn,
            double od)
        {
            string partId = row.Part.Id;
            int? dn2 = size.Dn2;
            int smallDn = dn2 ?? dn;
            double smallOd = PipeSizeCatalog.OdSch40Mm(smallDn);

            Guid RecordId(int portIndex) =>
                CatalogExcelPartResolver.StableSizeRecordId(partId, dn, dn2, portIndex);

            switch (row.PortLayout)
            {
                case CatalogExcelPortLayout.DualFlange:
                    yield return new PortWriteSpec(row.Port1, "S1", dn, od, RecordId(1));
                    if (row.HasSecondPort)
                        yield return new PortWriteSpec(row.Port2, "S2", dn, od, RecordId(2));
                    yield break;

                case CatalogExcelPortLayout.DualPortBv:
                    yield return new PortWriteSpec(row.Port1, "S1", dn, od, RecordId(1));
                    yield return new PortWriteSpec(row.Port2, "S2", smallDn, smallOd, RecordId(2));
                    yield break;

                case CatalogExcelPortLayout.TriplePortBv:
                    yield return new PortWriteSpec(row.Port1, "S1", dn, od, RecordId(1));
                    yield return new PortWriteSpec(row.Port2, "S2", dn, od, RecordId(2));
                    yield return new PortWriteSpec((row.FittingEndType, "S3"), "S3", smallDn, smallOd, RecordId(3));
                    yield break;

                default:
                    yield return new PortWriteSpec(row.Port1, "S-ALL", dn, od, RecordId(1));
                    yield break;
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
            int dn)
        {
            if (CatalogLapJointIds.IsLjStubOrCollar(row.Part.Id))
            {
                WriteStubEndPortExtras(sheet, header, rowIndex, row, dn);
                return;
            }

            if (row.Part.Id.StartsWith("LJ_RING_", StringComparison.OrdinalIgnoreCase))
            {
                WriteLjRingPortExtras(sheet, header, rowIndex, row, dn);
                return;
            }

            switch (row.PortLayout)
            {
                case CatalogExcelPortLayout.DualFlange:
                    SetFlangePortExtras(sheet, header, rowIndex, row.PressureClass, "S1", "S2");
                    return;

                case CatalogExcelPortLayout.DualPortBv:
                    WriteBvPortExtras(sheet, header, rowIndex, row, "S1", "S2");
                    return;

                case CatalogExcelPortLayout.TriplePortBv:
                    WriteBvPortExtras(sheet, header, rowIndex, row, "S1", "S2", "S3");
                    return;
            }

            bool isBwFitting = IsBwFitting(row);
            if (header.ContainsKey("PressureClass_S-ALL") && !isBwFitting)
                Set(sheet, header, rowIndex, "PressureClass_S-ALL", row.PressureClass);

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
            int dn)
        {
            SetFlangePortExtras(sheet, header, rowIndex, row.PressureClass, "S1");
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
            int dn)
        {
            string lapWall = "0";
            if (CatalogStubEndTable.TryGet(
                dn,
                CatalogStubEndTable.ResolvePattern(row.Part.Id),
                out CatalogStubEndTable.StubEndDims stub))
                lapWall = FormatOd(stub.B);

            foreach (string suffix in new[] { "S1", "S2" })
            {
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
            params string[] suffixes)
        {
            bool isBwFitting = IsBwFitting(row);
            foreach (string suffix in suffixes)
            {
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

        private static string BuildPartSizeLongDesc(
            CustomPartDefinition part,
            string familyDesc,
            CatalogExcelSizeVariant size)
        {
            string dnTag = size.Dn2.HasValue
                ? $"DN{size.Dn}x{size.Dn2.Value}"
                : $"DN{size.Dn}";

            string id = part.Id.ToUpperInvariant();
            string trimmed = familyDesc.Trim();

            if (id.Contains("ELBOW", StringComparison.Ordinal) && trimmed.Contains(" deg ", StringComparison.OrdinalIgnoreCase))
                return trimmed.Replace(" deg ", $" deg {dnTag} ", StringComparison.OrdinalIgnoreCase);

            if (id.StartsWith("BLD_", StringComparison.Ordinal) && trimmed.StartsWith("Flange. Blind ", StringComparison.OrdinalIgnoreCase))
                return $"Flange. Blind {dnTag} {trimmed["Flange. Blind ".Length..]}".Trim();

            if (id.StartsWith("WN_", StringComparison.Ordinal) && trimmed.Contains("WN", StringComparison.Ordinal))
                return InsertAfterFlangeKeyword(trimmed, "WN", dnTag);

            if (id.StartsWith("SO_", StringComparison.Ordinal) && trimmed.Contains("SO", StringComparison.Ordinal))
                return InsertAfterFlangeKeyword(trimmed, "SO", dnTag);

            if (CatalogLapJointIds.IsCollarExport(id))
            {
                var pat = CatalogStubEndTable.ResolvePattern(part.Id);
                string patLabel = pat == CatalogStubEndTable.Pattern.Short
                    ? "Short Pattern"
                    : "Long Pattern (Standard)";
                string lg = CatalogStubEndTable.TryGet(
                    size.Dn,
                    pat,
                    out CatalogStubEndTable.StubEndDims stub)
                    ? $"{stub.L:0.#}mm LG"
                    : "LG";
                return $"Collar, {dnTag}, SCH 40, {patLabel}, {lg}, ASME B16.9";
            }

            if (id.StartsWith("STUBEND_", StringComparison.Ordinal))
            {
                var pat = CatalogStubEndTable.ResolvePattern(part.Id);
                string patLabel = pat == CatalogStubEndTable.Pattern.Short
                    ? "Short Pattern"
                    : "Long Pattern (Standard)";
                string lg = CatalogStubEndTable.TryGet(
                    size.Dn,
                    pat,
                    out CatalogStubEndTable.StubEndDims stub)
                    ? $"{stub.L:0.#}mm LG"
                    : "LG";
                return $"STUB-END FOR LAP FLANGE, {dnTag}, SCH 40, {patLabel}, {lg}, ASME B16.9";
            }

            if (id.StartsWith("LJ_RING_", StringComparison.Ordinal))
            {
                if (CatalogLjRingCl150Table.TryGet(size.Dn, out CatalogLjRingCl150Table.LjRingDims lj))
                    return $"FLANGE LJ, {dnTag}, 150 LB, FF, ASME B16.5";
                return $"FLANGE LJ, {dnTag}, 150 LB, FF, ASME B16.5";
            }

            if (id.StartsWith("GSK_", StringComparison.Ordinal))
            {
                if (trimmed.StartsWith("Gasket CNAF ", StringComparison.OrdinalIgnoreCase))
                    return $"Gasket CNAF {dnTag} {trimmed["Gasket CNAF ".Length..]}".Trim();
                return $"Gasket CNAF {dnTag} Ring-Type CL150 Klingersil C4500";
            }

            return $"{trimmed} {dnTag}".Trim();
        }

        private static string InsertAfterFlangeKeyword(string familyDesc, string keyword, string dnTag)
        {
            string marker = $"Flange. {keyword} ";
            if (familyDesc.StartsWith(marker, StringComparison.OrdinalIgnoreCase))
                return $"{marker}{dnTag} {familyDesc[marker.Length..]}".Trim();

            int idx = familyDesc.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
                return $"{familyDesc} {dnTag}".Trim();

            int insertAt = idx + keyword.Length;
            return $"{familyDesc[..insertAt]} {dnTag}{familyDesc[insertAt..]}".Trim();
        }

        private static string BuildContentGeometryParamDefinition(
            string partId,
            bool hasDn2,
            bool hasCel,
            bool hasT)
        {
            // Catalog Builder template: parameter NAMES in ContentGeometryParamDefinition;
            // per-size VALUES go in DN / DN2 / CEL / T columns (PreviewLisp builds DN=15 at preview time).
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
            CatalogExcelPartRow row)
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
                Set(sheet, header, rowIndex, $"PressureClass_{port.Suffix}", row.PressureClass);
            }

            // RF on WN/BLD/gasket FL ports; LJ backing ring is FF; LAP/BV have no facing.
            string? facing = ResolvePortFacing(row.Part, port.Port.EndType);
            if (facing != null)
                Set(sheet, header, rowIndex, $"Facing_{port.Suffix}", facing);
        }

        private static string? ResolvePortFacing(CustomPartDefinition part, string endType)
        {
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

        private static void ClearDataRows(IXLWorksheet sheet)
        {
            if (sheet.LastRowUsed()?.RowNumber() >= DataStartRow)
                sheet.Rows(DataStartRow, sheet.LastRowUsed()!.RowNumber()).Delete();
        }

        private static string FormatOd(double od) =>
            od.ToString("0.##", CultureInfo.InvariantCulture);

    }
}
