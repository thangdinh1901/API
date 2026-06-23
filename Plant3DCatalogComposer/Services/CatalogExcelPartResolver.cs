using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Plant3DCatalogComposer.Services
{
    internal enum CatalogExcelPortLayout
    {
        DualFlange,
        DualPortBv,
        TriplePortBv,
        SingleAll,
    }

    internal sealed class CatalogExcelPartRow
    {
        public required CustomPartDefinition Part { get; init; }

        public string FamilyLongDesc { get; init; } = "";

        public string ShortDescription { get; init; } = "";

        public string PartCategory { get; init; } = "";

        public string PnPClassName { get; init; } = "";

        public string PressureClass { get; init; } = "150";

        public string FittingEndType { get; init; } = "BV";

        public string? PipeSchedule { get; init; }

        public string Material { get; init; } = "CS";

        public int PortCount { get; init; } = 2;

        public CatalogExcelPortLayout PortLayout { get; init; }

        public bool HasSecondPort { get; init; } = true;

        public (string EndType, string PortName) Port1 { get; init; }

        public (string EndType, string PortName) Port2 { get; init; }
    }

    internal static class CatalogExcelPartResolver
    {
        private static readonly Guid FamilyNamespace = new("6f3b2c4a-8e91-4d5f-a1b2-c3d4e5f60718");
        private static readonly Guid SizeRecordNamespace = new("7c4d3e5b-9f02-4e6a-b2c1-d4e5f6071829");

        public static Guid StableFamilyId(string partId) =>
            CreateStableGuid(FamilyNamespace, partId.ToUpperInvariant());

        public static Guid StableSizeRecordId(string partId, int dn, int? dn2 = null, int portIndex = 1)
        {
            string key = dn2.HasValue
                ? $"{partId.ToUpperInvariant()}|{dn}|{dn2.Value}|P{portIndex}"
                : $"{partId.ToUpperInvariant()}|{dn}|P{portIndex}";
            return CreateStableGuid(SizeRecordNamespace, key);
        }

        private static Guid CreateStableGuid(Guid namespaceId, string name)
        {
            byte[] namespaceBytes = namespaceId.ToByteArray();
            SwapGuidByteOrder(namespaceBytes);
            byte[] nameBytes = Encoding.UTF8.GetBytes(name);
            byte[] hash;
            using (var sha1 = SHA1.Create())
            {
                byte[] combined = new byte[namespaceBytes.Length + nameBytes.Length];
                Buffer.BlockCopy(namespaceBytes, 0, combined, 0, namespaceBytes.Length);
                Buffer.BlockCopy(nameBytes, 0, combined, namespaceBytes.Length, nameBytes.Length);
                hash = sha1.ComputeHash(combined);
            }

            byte[] guidBytes = new byte[16];
            Array.Copy(hash, 0, guidBytes, 0, 16);
            guidBytes[6] = (byte)((guidBytes[6] & 0x0F) | 0x50);
            guidBytes[8] = (byte)((guidBytes[8] & 0x3F) | 0x80);
            return new Guid(guidBytes);
        }

        private static void SwapGuidByteOrder(byte[] guidBytes)
        {
            Array.Reverse(guidBytes, 0, 4);
            Array.Reverse(guidBytes, 4, 2);
            Array.Reverse(guidBytes, 6, 2);
        }

        public static IReadOnlyList<CatalogExcelPartRow> DiscoverExportParts()
        {
            var rows = new List<CatalogExcelPartRow>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (CustomPartDefinition part in CustomPartCatalog.InsertableParts)
            {
                if (seen.Add(part.Id))
                    TryAddPart(rows, part);

                // CollarLapped joint resolves Fasteners/Collar from spec — mirror each LJ stub export.
                string? collarId = CatalogLapJointIds.CollarExportIdFromStub(part.Id);
                if (collarId != null && seen.Add(collarId))
                    TryAddPart(rows, CloneAsCollarExport(part, collarId));
            }

            return OrderRows(rows);
        }

        private static CustomPartDefinition CloneAsCollarExport(CustomPartDefinition stub, string collarId) =>
            new()
            {
                Role = stub.Role,
                Id = collarId,
                DisplayName = "Lap-joint collar",
                Group = stub.Group,
                Category = stub.Category,
                DefaultDN = stub.DefaultDN,
                PressureClass = stub.PressureClass,
                PipeSchedule = stub.PipeSchedule,
                StandardSet = stub.StandardSet,
                ParametricDN = stub.ParametricDN,
                Skeleton = stub.Skeleton,
                CatalogParams = stub.CatalogParams,
                CatalogFrameRotation = stub.CatalogFrameRotation,
            };

        private static IReadOnlyList<CatalogExcelPartRow> OrderRows(List<CatalogExcelPartRow> rows) =>
            rows
                .OrderBy(r => r.Part.Group, StringComparer.OrdinalIgnoreCase)
                .ThenBy(r => r.Part.Id, StringComparer.OrdinalIgnoreCase)
                .ToList();

        private static void TryAddPart(List<CatalogExcelPartRow> rows, CustomPartDefinition part)
        {
            CatalogExcelPartRow? row = BuildRow(part);
            if (row != null)
                rows.Add(row);
        }

        private static CatalogExcelPartRow? BuildRow(CustomPartDefinition part)
        {
            if (!IsExportablePart(part))
                return null;

            ParseCatalogEntryMetadata(part.Id, out _, out int portCount);

            TryResolveFlangePortEndTypes(part.Id, out string? flangePort1, out string? flangePort2);
            (CatalogExcelPortLayout layout, portCount, bool hasSecondPort) =
                ResolvePortLayout(part, portCount, flangePort2 != null);

            return new CatalogExcelPartRow
            {
                Part = part,
                FamilyLongDesc = BuildFamilyLongDesc(part),
                ShortDescription = CatalogExcelShortDescription.Resolve(part),
                PartCategory = ResolvePartCategory(part),
                PnPClassName = ResolvePnPClassName(part),
                PressureClass = NormalizePressureClass(part),
                FittingEndType = ResolveFittingEndType(part),
                PipeSchedule = string.IsNullOrWhiteSpace(part.PipeSchedule) ? null : part.PipeSchedule.Trim(),
                Material = part.Group.Equals("Gasket", StringComparison.OrdinalIgnoreCase) ? "CNAF" : "CS",
                PortCount = portCount,
                PortLayout = layout,
                HasSecondPort = hasSecondPort,
                Port1 = ResolvePort1(part, layout, flangePort1),
                Port2 = ResolvePort2(part, layout, flangePort2),
            };
        }

        private static (CatalogExcelPortLayout Layout, int PortCount, bool HasSecondPort) ResolvePortLayout(
            CustomPartDefinition part,
            int catalogEntryPortCount,
            bool hasFlangeSecondPort)
        {
            if (hasFlangeSecondPort || part.Id.StartsWith("BLD_", StringComparison.OrdinalIgnoreCase))
            {
                bool dual = hasFlangeSecondPort;
                return (dual ? CatalogExcelPortLayout.DualFlange : CatalogExcelPortLayout.SingleAll,
                    dual ? 2 : 1,
                    dual);
            }

            if (part.Id.Contains("TEE_REDUCE", StringComparison.OrdinalIgnoreCase))
                return (CatalogExcelPortLayout.TriplePortBv, 3, true);

            if (part.Id.Contains("TEE_EQ", StringComparison.OrdinalIgnoreCase))
                return (CatalogExcelPortLayout.TriplePortBv, 3, true);

            if (IsDualPortFlangedFitting(part, catalogEntryPortCount))
                return (CatalogExcelPortLayout.DualFlange, 2, true);

            if (IsDualPortBvFitting(part, catalogEntryPortCount))
                return (CatalogExcelPortLayout.DualPortBv, 2, true);

            return (CatalogExcelPortLayout.SingleAll, catalogEntryPortCount, catalogEntryPortCount > 1);
        }

        private static bool IsDualPortFlangedFitting(CustomPartDefinition part, int catalogEntryPortCount)
        {
            if (catalogEntryPortCount < 2)
                return false;

            if (!part.Group.Equals("Fitting", StringComparison.OrdinalIgnoreCase))
                return false;

            return CatalogFlangeFacing.IsFlangedEndType(ResolveFittingEndType(part));
        }

        private static bool IsDualPortBvFitting(CustomPartDefinition part, int catalogEntryPortCount)
        {
            if (catalogEntryPortCount < 2)
                return false;

            if (!part.Group.Equals("Fitting", StringComparison.OrdinalIgnoreCase))
                return false;

            if (part.Id.Contains("TEE", StringComparison.OrdinalIgnoreCase))
                return false;

            if (part.Id.Contains("ELBOW", StringComparison.OrdinalIgnoreCase)
                || part.Id.Contains("REDUCER", StringComparison.OrdinalIgnoreCase)
                || part.Id.Contains("BEND", StringComparison.OrdinalIgnoreCase))
                return true;

            if (part.PnpClassName.Equals("Elbow", StringComparison.OrdinalIgnoreCase)
                || part.PnpClassName.Equals("Reducer", StringComparison.OrdinalIgnoreCase))
                return true;

            return true;
        }

        private static bool IsExportablePart(CustomPartDefinition part)
        {
            if (part.IsCompositeTemplate)
                return false;

            if (part.Group.Equals("Gasket", StringComparison.OrdinalIgnoreCase))
                return true;

            if (part.Group.Equals("Fastener", StringComparison.OrdinalIgnoreCase))
                return true;

            if (part.Group.Equals("Flange", StringComparison.OrdinalIgnoreCase))
                return true;

            if (part.Group.Equals("Fitting", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        private static void ParseCatalogEntryMetadata(string partId, out string firstPortEndtypes, out int portCount)
        {
            firstPortEndtypes = "FL";
            portCount = 2;

            string? entry = CatalogPortTemplates.TryLoadCatalogEntryPy(partId);
            if (string.IsNullOrEmpty(entry))
                return;

            Match endtypesMatch = Regex.Match(entry, @"FirstPortEndtypes=""([^""]+)""");
            if (endtypesMatch.Success)
                firstPortEndtypes = endtypesMatch.Groups[1].Value.Trim();

            Match portsMatch = Regex.Match(entry, @"Ports=""(\d+)""");
            if (portsMatch.Success && int.TryParse(portsMatch.Groups[1].Value, out int ports))
                portCount = ports;
        }

        private static string BuildFamilyLongDesc(CustomPartDefinition part)
        {
            string id = part.Id.ToUpperInvariant();
            return id switch
            {
                _ when id.StartsWith("WN_", StringComparison.Ordinal) =>
                    "Flange. WN CL150 RF smooth (3.2-6.3 um) CS ASTM A105N Dims to ASME B16.5",
                _ when id.StartsWith("SO_", StringComparison.Ordinal) =>
                    "Flange. SO CL150 RF smooth (3.2-6.3 um) CS ASTM A105N Dims to ASME B16.5",
                _ when id.StartsWith("BLD_", StringComparison.Ordinal) =>
                    "Flange. Blind CL150 RF smooth (3.2-6.3 um) CS ASTM A105N Dims to ASME B16.5",
                _ when id.StartsWith("GSK_", StringComparison.Ordinal) =>
                    "Gasket CNAF Ring-Type CL150 Klingersil C4500",
                _ when id.Contains("ELBOW_90_LR", StringComparison.Ordinal) =>
                    "Elbow 90 deg LR BW. Welded CS ASTM A234-WPB. Dims to ASME B16.9",
                _ when id.Contains("ELBOW_90_SR", StringComparison.Ordinal) =>
                    "Elbow 90 deg SR BW. Welded CS ASTM A234-WPB. Dims to ASME B16.9",
                _ when id.Contains("ELBOW_45_LR", StringComparison.Ordinal) =>
                    "Elbow 45 deg LR BW. Welded CS ASTM A234-WPB. Dims to ASME B16.9",
                _ when id.Contains("ELBOW_90_SW", StringComparison.Ordinal) =>
                    "Elbow 90 deg SW CL3000 CS ASTM A105N Dims to ASME B16.11",
                _ when id.Contains("ELBOW_45_SW", StringComparison.Ordinal) =>
                    "Elbow 45 deg SW CL3000 CS ASTM A105N Dims to ASME B16.11",
                _ when id.Contains("TEE_EQ_BW", StringComparison.Ordinal) =>
                    "Tee Equal BW. Welded CS ASTM A234-WPB. Dims to ASME B16.9",
                _ when id.Contains("TEE_REDUCE_BW", StringComparison.Ordinal) =>
                    "Tee Red BW Welded CS ASTM A234-WPB. Dims to ASME B16.9",
                _ when id.Contains("TEE_EQ_SW", StringComparison.Ordinal) =>
                    "Tee Equal SW CL3000 CS ASTM A105N Dims to ASME B16.11",
                _ when id.Contains("TEE_REDUCE_SW", StringComparison.Ordinal) =>
                    "Tee Red SW CL3000 CS ASTM A105N Dims to ASME B16.11",
                _ when id.Contains("REDUCER_ECC", StringComparison.Ordinal) =>
                    "Reducer Ecc. BW Welded CS ASTM A234-WPB. Dims to ASME B16.9",
                _ when id.Contains("REDUCER_CONC", StringComparison.Ordinal) =>
                    "Reducer Conc. BW Welded CS ASTM A234-WPB. Dims to ASME B16.9",
                _ when id.StartsWith("COLLAR_LJ_", StringComparison.Ordinal) && id.Contains("_SH_", StringComparison.Ordinal) =>
                    "Lap-joint collar (stub end), SCH 40, Short Pattern, ASME B16.9",
                _ when id.StartsWith("COLLAR_LJ_", StringComparison.Ordinal) =>
                    "Lap-joint collar (stub end), SCH 40, Long Pattern (Standard), ASME B16.9",
                _ when id.StartsWith("STUBEND_", StringComparison.Ordinal) && id.Contains("_SH_", StringComparison.Ordinal) =>
                    "STUB-END FOR LAP FLANGE, SCH 40, Short Pattern, ASME B16.9",
                _ when id.StartsWith("STUBEND_", StringComparison.Ordinal) =>
                    "STUB-END FOR LAP FLANGE, SCH 40, Long Pattern (Standard), ASME B16.9",
                _ when id.StartsWith("LJ_RING_", StringComparison.Ordinal) =>
                    "FLANGE LJ, 150 LB, FF, ASME B16.5",
                _ => part.DisplayName,
            };
        }

        private static string ResolvePartCategory(CustomPartDefinition part)
        {
            string id = part.Id.ToUpperInvariant();
            if (id.StartsWith("STUBEND_", StringComparison.Ordinal)
                || id.StartsWith("COLLAR_LJ_", StringComparison.Ordinal))
                return "Fasteners";

            return part.Group switch
            {
                _ when part.Group.Equals("Gasket", StringComparison.OrdinalIgnoreCase) => "Fasteners",
                _ when part.Group.Equals("Fastener", StringComparison.OrdinalIgnoreCase) => "Fasteners",
                _ when part.Group.Equals("Flange", StringComparison.OrdinalIgnoreCase) => "Flanges",
                _ when part.Group.Equals("Fitting", StringComparison.OrdinalIgnoreCase) => "Fittings",
                _ when part.Group.Equals("Valve", StringComparison.OrdinalIgnoreCase) => "Valves",
                _ => part.Category,
            };
        }

        private static string ResolvePnPClassName(CustomPartDefinition part)
        {
            if (!string.IsNullOrWhiteSpace(part.PnpClassName))
                return part.PnpClassName.Trim();

            string id = part.Id.ToUpperInvariant();
            if (id.StartsWith("GSK_", StringComparison.Ordinal))
                return "Gasket";
            if (id.StartsWith("BLD_", StringComparison.Ordinal))
                return "BlindFlange";
            if (id.StartsWith("COLLAR_LJ_", StringComparison.Ordinal))
                return "Collar";
            if (id.StartsWith("STUBEND_", StringComparison.Ordinal))
                return "StubEnd";
            if (id.StartsWith("LJ_RING_", StringComparison.Ordinal))
                return "Flange";
            if (id.StartsWith("WN_", StringComparison.Ordinal) || id.StartsWith("SO_", StringComparison.Ordinal))
                return "Flange";
            if (id.Contains("ELBOW", StringComparison.Ordinal))
                return "Elbow";
            if (id.Contains("TEE", StringComparison.Ordinal))
                return "Tee";
            if (id.Contains("REDUCER", StringComparison.Ordinal))
                return "Reducer";

            return "EngineeringItems";
        }

        private static string NormalizePressureClass(CustomPartDefinition part) =>
            string.IsNullOrWhiteSpace(part.PressureClass) ? "150" : part.PressureClass.Trim();

        private static string ResolveFittingEndType(CustomPartDefinition part)
        {
            if (!string.IsNullOrWhiteSpace(part.PrimaryEndType)
                && !part.PrimaryEndType.Equals("Undefined_ET", StringComparison.OrdinalIgnoreCase))
            {
                return CatalogPortTemplates.MapPrimaryEndToPortEndTypePublic(part.PrimaryEndType);
            }

            if (part.StandardSet.Equals(SwCl3000StandardCatalog.SetId, StringComparison.OrdinalIgnoreCase)
                || part.Id.Contains("_SW_", StringComparison.OrdinalIgnoreCase))
                return "SW";

            return "BV";
        }

        private static (string EndType, string PortName) ResolvePort1(
            CustomPartDefinition part,
            CatalogExcelPortLayout layout,
            string? flangePort1)
        {
            if (TryResolveElbowPortEndTypes(part, out string? elbowP1, out _))
                return (elbowP1!, "S1");

            if (layout == CatalogExcelPortLayout.DualFlange)
                return (flangePort1 ?? "FL", "S1");

            if (layout is CatalogExcelPortLayout.DualPortBv or CatalogExcelPortLayout.TriplePortBv)
                return (ResolveFittingEndType(part), "S1");

            if (flangePort1 != null)
                return (flangePort1, "ALL");

            if (part.Group.Equals("Gasket", StringComparison.OrdinalIgnoreCase))
                return ("FL", "ALL");

            if (part.Group.Equals("Fitting", StringComparison.OrdinalIgnoreCase))
            {
                string endType = ResolveFittingEndType(part);
                return (endType is "SW" or "BV" ? endType : "FL", "ALL");
            }

            if (part.Group.Equals("Valve", StringComparison.OrdinalIgnoreCase))
                return ("FL", "ALL");

            return ("FL", "ALL");
        }

        private static (string EndType, string PortName) ResolvePort2(
            CustomPartDefinition part,
            CatalogExcelPortLayout layout,
            string? flangePort2)
        {
            if (TryResolveElbowPortEndTypes(part, out _, out string? elbowP2))
                return (elbowP2!, "S2");

            if (layout == CatalogExcelPortLayout.DualFlange)
            {
                if (flangePort2 != null)
                    return (flangePort2, "S2");
                return (ResolveFittingEndType(part), "S2");
            }

            if (layout is CatalogExcelPortLayout.DualPortBv or CatalogExcelPortLayout.TriplePortBv)
                return (ResolveFittingEndType(part), "S2");

            return ("FL", "S2");
        }

        private static bool TryResolveFlangePortEndTypes(
            string partId,
            out string? port1EndType,
            out string? port2EndType)
        {
            port1EndType = null;
            port2EndType = null;
            string id = partId.ToUpperInvariant();

            if (id.StartsWith("WN_", StringComparison.Ordinal))
            {
                port1EndType = "FL";
                port2EndType = "BV";
                return true;
            }

            if (id.StartsWith("SO_", StringComparison.Ordinal))
            {
                port1EndType = "FL";
                port2EndType = "SO";
                return true;
            }

            if (id.StartsWith("BLD_", StringComparison.Ordinal))
            {
                port1EndType = "FL";
                port2EndType = null;
                return true;
            }

            if (id.StartsWith("LJ_RING_", StringComparison.Ordinal))
            {
                port1EndType = "FL";
                port2EndType = "LAP";
                return true;
            }

            if (id.StartsWith("STUBEND_", StringComparison.Ordinal)
                || id.StartsWith("COLLAR_LJ_", StringComparison.Ordinal))
            {
                port1EndType = "LAP";
                port2EndType = "BV";
                return true;
            }

            if (id.StartsWith("GSK_", StringComparison.Ordinal))
            {
                port1EndType = "FL";
                port2EndType = "FL";
                return true;
            }

            return false;
        }

        /// <summary>
        /// Both elbow ports are fitting ends (BV or SW). Pipe stock uses PL (plain) in the catalog;
        /// Plant 3D joint tables connect PL pipe to BV/SW fittings — do not mark fitting port 2 as PL.
        /// </summary>
        private static bool TryResolveElbowPortEndTypes(
            CustomPartDefinition part,
            out string? port1EndType,
            out string? port2EndType)
        {
            port1EndType = null;
            port2EndType = null;
            string partId = part.Id;

            bool isElbowLike = partId.Contains("ELBOW", StringComparison.OrdinalIgnoreCase)
                || partId.Contains("BEND", StringComparison.OrdinalIgnoreCase)
                || part.PnpClassName.Equals("Elbow", StringComparison.OrdinalIgnoreCase);
            if (!isElbowLike)
                return false;

            if (!string.IsNullOrWhiteSpace(part.PrimaryEndType)
                && !part.PrimaryEndType.Equals("Undefined_ET", StringComparison.OrdinalIgnoreCase))
            {
                string mapped = CatalogPortTemplates.MapPrimaryEndToPortEndTypePublic(part.PrimaryEndType);
                if (CatalogFlangeFacing.IsFlangedEndType(mapped))
                    return false;

                port1EndType = mapped;
                port2EndType = mapped;
                return true;
            }

            if (partId.Contains("_SW_", StringComparison.OrdinalIgnoreCase))
            {
                port1EndType = "SW";
                port2EndType = "SW";
                return true;
            }

            port1EndType = "BV";
            port2EndType = "BV";
            return true;
        }
    }
}
