using System.Collections.Generic;

namespace Plant3DCatalogComposer.Services
{
    /// <summary>Part library deployed from catalog_generator/parts/.</summary>
    public sealed class CustomPartDefinition
    {
        /// <summary>standard = insert catalog part; composite = valve skeleton template on Setup.</summary>
        public required string Role { get; init; }

        public required string Id { get; init; }

        public required string DisplayName { get; init; }

        public required string Group { get; init; }

        /// <summary>Plant 3D Component Category (Fittings, Flanges, Fasteners, …).</summary>
        public required string Category { get; init; }

        public double DefaultDN { get; init; }

        public string PressureClass { get; init; } = "150";

        /// <summary>40, 80, 40S, … — empty = all schedules (e.g. flanges).</summary>
        public string PipeSchedule { get; init; } = "";

        /// <summary>Named library set (e.g. BW_SCH40 standard butt-weld fittings).</summary>
        public string StandardSet { get; init; } = "";

        /// <summary>Excel ShortDescription override from part.json.</summary>
        public string ShortDescription { get; init; } = "";

        /// <summary>Plant Spec Editor PnP class from part.json.</summary>
        public string PnpClassName { get; init; } = "";

        public bool ParametricDN { get; init; } = true;

        public IReadOnlyDictionary<string, double>? Skeleton { get; init; }

        public IReadOnlyList<CatalogPartParam> CatalogParams { get; init; } = [];

        /// <summary>Rotation baked in CUST_*.py (row-major 3x3), e.g. rotateY(90) on flanges.</summary>
        public double[]? CatalogFrameRotation { get; init; }

        public string CatalogFunctionName => $"CUST_{Id}";

        public string CatalogEntryFileName => $"CUST_{Id}.py";

        public bool IsStandardCatalog => Role.Equals("standard", System.StringComparison.OrdinalIgnoreCase)
            || Role.Equals("catalog", System.StringComparison.OrdinalIgnoreCase);

        public bool IsCompositeTemplate => Role.Equals("composite", System.StringComparison.OrdinalIgnoreCase);
    }
}
