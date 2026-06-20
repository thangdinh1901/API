using System;

namespace Plant3DCatalogComposer.Services
{
    /// <summary>Palette / catalog short descriptions — one label per family (all sizes share).</summary>
    internal static class CatalogExcelShortDescription
    {
        public static string Resolve(CustomPartDefinition part)
        {
            string id = part.Id.ToUpperInvariant();
            return id switch
            {
                "BLD_FLRF_CL150" => "FLANGE BLIND",
                "WN_FLRF_CL150" => "FLANGE WN",
                "SO_FLRF_CL150" => "FLANGE SO",
                "GSK_RF_CL150" => "GASKET RF",
                "GSK_FF_CL150" => "GASKET FF",
                "ELBOW_45_LR_BW_SCH40" => "ELL 45 LR",
                "ELBOW_90_LR_BW_SCH40" => "ELL 90 LR",
                "ELBOW_90_SR_BW_SCH40" => "ELL 90 SR",
                "ELBOW_45_SW_CL3000" => "ELL 45",
                "ELBOW_90_SW_CL3000" => "ELL 90",
                "TEE_EQ_BW_SCH40" => "TEE",
                "TEE_REDUCE_BW_SCH40" => "TEE (RED)",
                "TEE_EQ_SW_CL3000" => "TEE",
                "TEE_REDUCE_SW_CL3000" => "TEE (RED)",
                "REDUCER_CONC_BW_SCH40" => "REDUCER (CONC)",
                "REDUCER_ECC_BW_SCH40" => "REDUCER (ECC)",
                "STUBEND_LJ_A_BW_SCH40" => "STUB-END FOR LAP FLANGE",
                "STUBEND_LJ_A_SH_BW_SCH40" => "STUB-END SH LAP",
                "LJ_RING_CL150_RF" => "FLANGE LJ",
                _ => part.Id,
            };
        }
    }
}
