using System;

namespace Plant3DCatalogComposer.Services
{
    /// <summary>
    /// ISOGEN symbol metadata (SKEY + PCF type) per component. SKEYs match the project iso config
    /// NUI/Isometric/IsoSkeyAcadBlockMap.xml (SKEY → AutoCAD block): first two chars = component
    /// (EL elbow, TE tee, RC/RE reducer, FL flange, VT gate, VG globe, VB ball, VY butterfly,
    /// VC check, VD diaphragm, VN needle, VP plug, AV angle, VV basic), last two = end connection
    /// (BW butt weld, SW socket weld, SC screwed, FL flanged).
    /// </summary>
    internal sealed class CatalogExcelIsoMetadata
    {
        public string CompatibleStandard { get; init; } = "";

        public string DesignStd { get; init; } = "";

        public string IsoType { get; init; } = "";

        public string IsoSkey { get; init; } = "";

        public string ContentIsoSymbolDefinition { get; init; } = "";

        public static CatalogExcelIsoMetadata Resolve(CustomPartDefinition part)
        {
            string id = part.Id.ToUpperInvariant();
            bool isSw = part.StandardSet.Equals(SwCl3000StandardCatalog.SetId, StringComparison.OrdinalIgnoreCase)
                        || id.Contains("_SW_", StringComparison.Ordinal);

            // --- Flanges (SKEY encodes the flange type; no separate end suffix). ---
            if (id.StartsWith("WN_", StringComparison.Ordinal))
                return Make("ASME B16.5", "B16.5", "FLANGE", "FLWN");

            if (id.StartsWith("SO_", StringComparison.Ordinal))
                return Make("ASME B16.5", "B16.5", "FLANGE", "FLSO");

            if (id.StartsWith("BLD_", StringComparison.Ordinal))
                return Make("ASME B16.5", "B16.5", "FLANGE-BLIND", "FLBL");

            // Lap-joint backing ring = loose backing flange (FLLB → FittingFlange block).
            if (id.StartsWith("LJ_RING_", StringComparison.Ordinal))
                return Make("ASME B16.5", "B16.5", "FLANGE", "FLLB");

            // Stub end / collar = FLSE (→ Stub-End block in IsoSkeyAcadBlockMap.xml).
            if (id.StartsWith("STUBEND_", StringComparison.Ordinal)
                || id.StartsWith("COLLAR_LJ_", StringComparison.Ordinal))
                return Make("ASME B16.9", "B16.9", "FLANGE", "FLSE");

            if (id.StartsWith("GSK_", StringComparison.Ordinal))
                return Make("ASME B16.21", "B16.21", "GASKET", "GASK");

            // --- Fittings (component prefix + end connection suffix). ---
            if (id.Contains("ELBOW", StringComparison.Ordinal)
                || id.Contains("BEND", StringComparison.Ordinal)
                || part.PnpClassName.Equals("Elbow", StringComparison.OrdinalIgnoreCase))
            {
                string design = isSw ? "B16.11" : "B16.9";
                return Make($"ASME {design}", design, "ELBOW", "EL" + Conn(part, "BW"));
            }

            if (id.Contains("TEE", StringComparison.Ordinal)
                || part.PnpClassName.Equals("Tee", StringComparison.OrdinalIgnoreCase))
            {
                string design = isSw ? "B16.11" : "B16.9";
                return Make($"ASME {design}", design, "TEE", "TE" + Conn(part, "BW"));
            }

            if (id.Contains("REDUCER_ECC", StringComparison.Ordinal))
            {
                string design = isSw ? "B16.11" : "B16.9";
                return Make($"ASME {design}", design, "REDUCER-ECCENTRIC", "RE" + Conn(part, "BW"));
            }

            if (id.Contains("REDUCER", StringComparison.Ordinal)
                || part.PnpClassName.Equals("Reducer", StringComparison.OrdinalIgnoreCase))
            {
                string design = isSw ? "B16.11" : "B16.9";
                return Make($"ASME {design}", design, "REDUCER-CONCENTRIC", "RC" + Conn(part, "BW"));
            }

            // --- Valves (valve-type prefix + end connection suffix). ---
            if (id.StartsWith("VALVE_", StringComparison.Ordinal)
                || part.Group.Equals("Valve", StringComparison.OrdinalIgnoreCase)
                || part.PnpClassName.Equals("Valve", StringComparison.OrdinalIgnoreCase))
            {
                (string prefix, string type) = ValveSymbol(part);
                return Make("ASME B16.10", "Custom", type, prefix + Conn(part, "FL"));
            }

            return new CatalogExcelIsoMetadata();
        }

        public static CatalogExcelIsoMetadata PipeSch40() =>
            Make("ASME B36.10M", "B36.10M", "PIPE", "PIPE");

        public static CatalogExcelIsoMetadata StudBoltRf() =>
            Make("ASTM A193", "ASTM A193", "BOLT", "BOLT");

        /// <summary>ISOGEN end-connection suffix (BW/SW/SC/FL) from the part's primary end type.</summary>
        private static string Conn(CustomPartDefinition part, string fallback)
        {
            string id = part.Id.ToUpperInvariant();
            string end = (part.PrimaryEndType ?? "").Trim().ToUpperInvariant();

            if (end is "SW" or "PSW"
                || id.Contains("_SW_", StringComparison.Ordinal)
                || part.StandardSet.Equals(SwCl3000StandardCatalog.SetId, StringComparison.OrdinalIgnoreCase))
                return "SW";

            if (end is "THDM" or "THDF" or "TAP" or "SCRD" or "NPT")
                return "SC";

            if (end is "FL" or "SO" or "WF" or "LUG" or "RF" or "FF")
                return "FL";

            if (end is "BV" or "BW" or "PL" or "PPL"
                || id.Contains("_BW_", StringComparison.Ordinal))
                return "BW";

            return fallback;
        }

        /// <summary>Valve SKEY prefix (VT gate, VG globe, …) + PCF type from id / name / short desc.</summary>
        private static (string Prefix, string Type) ValveSymbol(CustomPartDefinition part)
        {
            string text = (part.Id + " " + part.DisplayName + " " + part.ShortDescription).ToUpperInvariant();

            if (text.Contains("GATE", StringComparison.Ordinal)) return ("VT", "VALVE");
            if (text.Contains("GLOBE", StringComparison.Ordinal)) return ("VG", "VALVE");
            if (text.Contains("BALL", StringComparison.Ordinal)) return ("VB", "VALVE");
            if (text.Contains("BUTTERFLY", StringComparison.Ordinal)
                || text.Contains("WAFER", StringComparison.Ordinal)) return ("VY", "VALVE");
            if (text.Contains("CHECK", StringComparison.Ordinal)
                || text.Contains("NRV", StringComparison.Ordinal)) return ("VC", "VALVE");
            if (text.Contains("PLUG", StringComparison.Ordinal)
                || text.Contains("COCK", StringComparison.Ordinal)) return ("VP", "VALVE");
            if (text.Contains("DIAPHRAGM", StringComparison.Ordinal)) return ("VD", "VALVE");
            if (text.Contains("NEEDLE", StringComparison.Ordinal)) return ("VN", "VALVE");
            if (text.Contains("3WAY", StringComparison.Ordinal)
                || text.Contains("3-WAY", StringComparison.Ordinal)
                || text.Contains("THREE WAY", StringComparison.Ordinal)) return ("V3", "VALVE");
            if (text.Contains("4WAY", StringComparison.Ordinal)
                || text.Contains("4-WAY", StringComparison.Ordinal)) return ("V4", "VALVE");
            if (text.Contains("RELIEF", StringComparison.Ordinal)
                || text.Contains("SAFETY", StringComparison.Ordinal)
                || text.Contains("PSV", StringComparison.Ordinal)
                || text.Contains("ANGLE", StringComparison.Ordinal)) return ("AV", "VALVE-ANGLE");

            return ("VV", "VALVE"); // basic / unspecified valve (VV?? → GateValve block)
        }

        private static CatalogExcelIsoMetadata Make(string compatible, string design, string type, string skey) =>
            new()
            {
                CompatibleStandard = compatible,
                DesignStd = design,
                IsoType = type,
                IsoSkey = skey,
                ContentIsoSymbolDefinition = $"TYPE={type},SKEY={skey}",
            };
    }
}
