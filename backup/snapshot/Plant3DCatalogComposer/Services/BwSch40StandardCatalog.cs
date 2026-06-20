using System;
using System.Collections.Generic;
using System.Linq;

namespace Plant3DCatalogComposer.Services
{
    /// <summary>Official butt-weld fitting library — Schedule 40 per ASME B16.9 / B36.10M.</summary>
    public static class BwSch40StandardCatalog
    {
        public const string SetId = "BW_SCH40";

        public const string PipeSchedule = "40";

        public static IReadOnlyList<string> PartIds { get; } =
        [
            "ELBOW_90_LR_BW_SCH40",
            "ELBOW_45_LR_BW_SCH40",
            "ELBOW_90_SR_BW_SCH40",
            "REDUCER_CONC_BW_SCH40",
            "REDUCER_ECC_BW_SCH40",
            "TEE_EQ_BW_SCH40",
            "TEE_REDUCE_BW_SCH40",
        ];

        public static bool IsStandardPart(string? partId) =>
            !string.IsNullOrWhiteSpace(partId)
            && PartIds.Any(id => id.Equals(partId, StringComparison.OrdinalIgnoreCase));

        public static bool MatchesSet(CustomPartDefinition part) =>
            SetId.Equals(part.StandardSet, StringComparison.OrdinalIgnoreCase)
            || IsStandardPart(part.Id);
    }
}
