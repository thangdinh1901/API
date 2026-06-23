using System;

namespace Plant3DCatalogComposer.Services
{
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

            if (id.StartsWith("WN_", StringComparison.Ordinal))
                return Flange("ASME B16.5", "B16.5", "FLANGE", "FLWN");

            if (id.StartsWith("SO_", StringComparison.Ordinal))
                return Flange("ASME B16.5", "B16.5", "FLANGE", "FLSO");

            if (id.StartsWith("BLD_", StringComparison.Ordinal))
                return new CatalogExcelIsoMetadata
                {
                    CompatibleStandard = "ASME B16.5",
                    DesignStd = "B16.5",
                    IsoType = "FLANGE-BLIND",
                    IsoSkey = "FLBL",
                    ContentIsoSymbolDefinition = "TYPE=FLANGE-BLIND,SKEY=FLBL",
                };

            if (id.StartsWith("LJ_RING_", StringComparison.Ordinal))
                return new CatalogExcelIsoMetadata
                {
                    CompatibleStandard = "ASME B16.5",
                    DesignStd = "B16.5",
                    IsoType = "FLANGE",
                    IsoSkey = "FFLB",
                    ContentIsoSymbolDefinition = "TYPE=FLANGE,SKEY=FFLB",
                };

            if (id.StartsWith("COLLAR_LJ_", StringComparison.Ordinal))
                return new CatalogExcelIsoMetadata
                {
                    CompatibleStandard = "ASME B16.9",
                    DesignStd = "B16.9",
                    IsoType = "LAPJOINT",
                    IsoSkey = "FLSE",
                    ContentIsoSymbolDefinition = "SKEY=FLSE,TYPE=LAPJOINT-STUB-END",
                };

            if (id.StartsWith("STUBEND_", StringComparison.Ordinal))
                return new CatalogExcelIsoMetadata
                {
                    CompatibleStandard = "ASME B16.9",
                    DesignStd = "B16.9",
                    IsoType = "LAPJOINT",
                    IsoSkey = "FLSE",
                    ContentIsoSymbolDefinition = "SKEY=FLSE,TYPE=LAPJOINT-STUBEND",
                };

            if (id.StartsWith("GSK_", StringComparison.Ordinal))
                return new CatalogExcelIsoMetadata
                {
                    CompatibleStandard = "ASME B16.21",
                    DesignStd = "B16.21",
                    IsoType = "GASKET",
                    IsoSkey = "GASK",
                    ContentIsoSymbolDefinition = "TYPE=GASKET",
                };

            if (id.Contains("ELBOW", StringComparison.Ordinal)
                || id.Contains("BEND", StringComparison.Ordinal)
                || part.PnpClassName.Equals("Elbow", StringComparison.OrdinalIgnoreCase))
            {
                return isSw
                    ? Fitting("ASME B16.11", "B16.11", "ELBOW", "ELSW")
                    : Fitting("ASME B16.9", "B16.9", "ELBOW", "ELBW");
            }

            if (id.Contains("TEE", StringComparison.Ordinal))
            {
                return isSw
                    ? Fitting("ASME B16.11", "B16.11", "TEE", "TESW")
                    : Fitting("ASME B16.9", "B16.9", "TEE", "TEBW");
            }

            if (id.Contains("REDUCER_ECC", StringComparison.Ordinal))
                return Fitting("ASME B16.9", "B16.9", "REDUCER-ECCENTRIC", "REBW");

            if (id.Contains("REDUCER", StringComparison.Ordinal))
            {
                return isSw
                    ? Fitting("ASME B16.11", "B16.11", "REDUCER-CONCENTRIC", "RCSW")
                    : Fitting("ASME B16.9", "B16.9", "REDUCER-CONCENTRIC", "RCBW");
            }

            return new CatalogExcelIsoMetadata();
        }

        public static CatalogExcelIsoMetadata PipeSch40() =>
            new()
            {
                CompatibleStandard = "ASME B36.10M",
                DesignStd = "B36.10M",
                IsoType = "PIPE",
                IsoSkey = "PIPE",
                ContentIsoSymbolDefinition = "TYPE=PIPE",
            };

        public static CatalogExcelIsoMetadata StudBoltRf() =>
            new()
            {
                CompatibleStandard = "ASTM A193",
                DesignStd = "ASTM A193",
                IsoType = "BOLT",
                IsoSkey = "BOLT",
                ContentIsoSymbolDefinition = "TYPE=BOLT",
            };

        private static CatalogExcelIsoMetadata Flange(string compatible, string design, string type, string skey) =>
            new()
            {
                CompatibleStandard = compatible,
                DesignStd = design,
                IsoType = type,
                IsoSkey = skey,
                ContentIsoSymbolDefinition = $"TYPE={type},SKEY={skey}",
            };

        private static CatalogExcelIsoMetadata Fitting(string compatible, string design, string type, string skey) =>
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
