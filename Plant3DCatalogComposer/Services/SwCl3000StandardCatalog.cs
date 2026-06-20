using System;
using System.Collections.Generic;
using System.Linq;

namespace Plant3DCatalogComposer.Services
{
    /// <summary>ASME B16.11 Class 3000 socket-weld fitting library.</summary>
    public static class SwCl3000StandardCatalog
    {
        public const string SetId = "SW_CL3000";

        public const string PressureClass = "3000";

        public static IReadOnlyList<string> PartIds { get; } =
        [
            "ELBOW_90_SW_CL3000",
            "ELBOW_45_SW_CL3000",
            "TEE_EQ_SW_CL3000",
            "TEE_REDUCE_SW_CL3000",
        ];

        public static bool IsStandardPart(string? partId) =>
            !string.IsNullOrWhiteSpace(partId)
            && PartIds.Any(id => id.Equals(partId, StringComparison.OrdinalIgnoreCase));
    }
}
