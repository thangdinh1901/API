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

        /// <summary>Plant 3D Primary End Type from part.json (BV, FL, …).</summary>
        public string PrimaryEndType { get; init; } = "";

        /// <summary>Catalog Builder clone sheet id from part.json (e.g. VALVE_FL_CL150).</summary>
        public string ExcelCloneSourcePartId { get; init; } = "";

        /// <summary>Flange facing (RF / FF) from part.json.</summary>
        public string FlangeFacing { get; init; } = "RF";

        public bool ParametricDN { get; init; } = true;

        /// <summary>DN sizes present in the catalog sheet (part.json "sizes"). Drives the
        /// insert DN combo so only real sizes can be picked (empty = all pipe sizes).</summary>
        public IReadOnlyList<int> Sizes { get; init; } = [];

        public IReadOnlyDictionary<string, double>? Skeleton { get; init; }

        public IReadOnlyList<CatalogPartParam> CatalogParams { get; init; } = [];

        /// <summary>Rotation baked in CUST_*.py (row-major 3x3), e.g. rotateY(90) on flanges.</summary>
        public double[]? CatalogFrameRotation { get; init; }

        public string CatalogFunctionName => $"CUST_{Id}";

        public string CatalogEntryFileName => $"CUST_{Id}.py";

        public bool IsStandardCatalog => Role.Equals("standard", System.StringComparison.OrdinalIgnoreCase)
            || Role.Equals("catalog", System.StringComparison.OrdinalIgnoreCase);

        public bool IsCompositeTemplate => Role.Equals("composite", System.StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Copy with an overridden display name. Used when a configured catalog (e.g. CATA_NUI.xlsx)
        /// drives the insert list: the entry shows the catalog part's name while inserting the
        /// mapped local geometry for preview.
        /// </summary>
        public CustomPartDefinition WithDisplayName(string displayName) => new()
        {
            Role = Role,
            Id = Id,
            DisplayName = displayName,
            Group = Group,
            Category = Category,
            DefaultDN = DefaultDN,
            PressureClass = PressureClass,
            PipeSchedule = PipeSchedule,
            StandardSet = StandardSet,
            ShortDescription = ShortDescription,
            PnpClassName = PnpClassName,
            PrimaryEndType = PrimaryEndType,
            ExcelCloneSourcePartId = ExcelCloneSourcePartId,
            FlangeFacing = FlangeFacing,
            ParametricDN = ParametricDN,
            Sizes = Sizes,
            Skeleton = Skeleton,
            CatalogParams = CatalogParams,
            CatalogFrameRotation = CatalogFrameRotation,
        };
    }
}
