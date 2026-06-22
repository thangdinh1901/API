using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Plant3DSkeletonManager.Core
{
    // Pure domain model - the Scene Graph is the single source of truth.
    // No Inventor references allowed in this namespace.

    public enum CatalogParamUnit
    {
        Millimeter,
        Degree,
        Unitless,
    }

    /// <summary>
    /// Native Plant 3D building blocks (direct _shape calls in primitives.py).
    /// Composite recipes (chamfer, fillet, spring, …) are built manually via boolean ops.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PrimitiveType
    {
        BOX,
        CYLINDER,
        CONE,
        TORUS,
        SPHERE,
        HALFSPHERE,
        REDUCED_ELBOW,
        ELBOW,
        SEGMENTED_ELBOW,
        ELLIPSOID_HEAD,
        ELLIPSOID_HEAD2,
        ELLIPSOID_SEGMENT,
        PYRAMID,
        ROUND_RECTANGLE,
        SPHERE_SEGMENT,
        TORISPHERIC_HEAD,
        TORISPHERIC_HEAD2,
        TORISPHERIC_HEAD_H,
        FILLET,
        CYLINDER_CHAMFERED,
        BOX_WITH_FILLET,
        CYLINDER_WITH_FILLET,
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum BooleanOpType
    {
        UNION,
        SUBTRACT,
        INTERSECT,
    }

    /// <summary>Plant 3D catalog end type code (matches PLANTENDCODES / FirstPortEndtypes).</summary>
    [JsonConverter(typeof(PortConnectionTypeJsonConverter))]
    public enum PortConnectionType
    {
        Undefined_ET,
        PL,
        BV,
        THDM,
        THDF,
        SW,
        FL,
        WF,
        LAP,
        GRV,
        SO,
        PPL,
        PSW,
        LFL,
        LLP,
        LUG,
        BELL,
        SPIG,
        TAP,
        MJM,
        MJF,
        MJP,
        PFS,
        Universal_ET,
        TC,
        C,
        FTG,
        FA,
        P,
        SL,
    }

    /// <summary>Connection port stored in the scene graph (parent-local or world coordinates).</summary>
    public sealed class ConnectionPort
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public int Number { get; set; } = 1;
        public PortConnectionType Type { get; set; } = PortConnectionType.FL;

        /// <summary>Legacy label from older projects; ignored on save.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? Name { get; set; }

        /// <summary>Primitive node this port is attached to (coordinates are parent-local when set).</summary>
        [JsonPropertyName("parent")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Guid? ParentNodeId { get; set; }

        /// <summary>Position in mm — parent-local when ParentNodeId is set, otherwise world.</summary>
        public double[] Position { get; set; } = { 0, 0, 0 };

        /// <summary>Unit connection direction vector (parent-local when ParentNodeId is set).</summary>
        public double[] Direction { get; set; } = { 1, 0, 0 };
    }

    /// <summary>Resolved value (mm) plus the optional skeleton expression that produced it.</summary>
    public sealed class ParamValue
    {
        public double Value { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Expression { get; set; }
    }

    /// <summary>How a scene node is built in Plant 3D preview.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SceneNodeKind
    {
        /// <summary>Composable solid from primitives.py (custom valve / special part).</summary>
        Primitive,

        /// <summary>Existing catalog script (CUST_*.py) — flange, gasket, pre-built valve, etc.</summary>
        Catalog,
    }

    /// <summary>One rotation jog from the Scene tab (world WCS or object-local axis).</summary>
    public sealed class RotationJog
    {
        /// <summary>
        /// True = orbit Origin about WCS (0,0,0) then post-multiply R (world spin at new center).
        /// False = post-multiply R only at current Origin (no CAD-origin orbit).
        /// </summary>
        public bool World { get; set; }

        public char Axis { get; set; }

        public double Degrees { get; set; }
    }

    /// <summary>One primitive in the scene graph. Transforms are absolute (world space, mm).</summary>
    public sealed class PrimitiveNode
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;

        public SceneNodeKind Kind { get; set; } = SceneNodeKind.Primitive;

        /// <summary>Library folder id when Kind is Catalog (e.g. WN_FLRF_CL150).</summary>
        [JsonPropertyName("catalogPartId")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? CatalogPartId { get; set; }

        public PrimitiveType Type { get; set; }

        /// <summary>World position, mm.</summary>
        public double[] Origin { get; set; } = { 0, 0, 0 };

        /// <summary>Unit vector of the primitive +Z axis.</summary>
        public double[] Direction { get; set; } = { 0, 0, 1 };

        /// <summary>3x3 rotation matrix, row-major.</summary>
        public double[] Rotation { get; set; } = { 1, 0, 0, 0, 1, 0, 0, 0, 1 };

        /// <summary>Ordered rotation jogs for Plant 3D preview (world vs local replay).</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<RotationJog>? RotationJogs { get; set; }

        /// <summary>
        /// Rotation baked into the catalog script (e.g. rotateY(90) on flanges). Used for object-axis jogs.
        /// </summary>
        [JsonPropertyName("catalogFrameRotation")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double[]? CatalogFrameRotation { get; set; }

        /// <summary>Logical parameter names per Plant 3D conventions (D, L, W, H, ...).</summary>
        public Dictionary<string, ParamValue> Parameters { get; set; } = new();

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Guid? Parent { get; set; }
    }

    /// <summary>Ordered boolean operation metadata. Inventor solids are never actually combined.</summary>
    public sealed class BooleanOperation
    {
        public int Order { get; set; }
        public BooleanOpType Type { get; set; }
        public Guid Target { get; set; }
        public List<Guid> Tools { get; set; } = new();
    }

    /// <summary>Global valve parameters. Lengths in mm.</summary>
    public sealed class SkeletonParameters
    {
        [JsonPropertyName("DN")]
        public double DN { get; set; }

        /// <summary>Small / branch nominal size (mm) for reducers and reducing tees.</summary>
        [JsonPropertyName("DN2")]
        public double DN2 { get; set; }

        [JsonPropertyName("PressureClass")]
        public string PressureClass { get; set; } = string.Empty;

        /// <summary>Pipe schedule code: 40, 80, 40S, 80S, 10, 10S.</summary>
        [JsonPropertyName("PipeSchedule")]
        public string PipeSchedule { get; set; } = "40";

        [JsonPropertyName("FaceToFace")]
        public double FaceToFace { get; set; }

        [JsonPropertyName("BodyOD")]
        public double BodyOD { get; set; }

        /// <summary>LR 90° elbow center-to-face (mm) from DN; 0 = use BodyOD * 1.5 fallback.</summary>
        [JsonPropertyName("ElbowCenterToFace")]
        public double ElbowCenterToFace { get; set; }

        [JsonPropertyName("BodyLength")]
        public double BodyLength { get; set; }

        [JsonPropertyName("BonnetHeight")]
        public double BonnetHeight { get; set; }

        [JsonPropertyName("StemDia")]
        public double StemDia { get; set; }

        [JsonPropertyName("HandwheelOD")]
        public double HandwheelOD { get; set; }

        /// <summary>User-defined design dimensions (mm) for valve / instrument authoring.</summary>
        [JsonPropertyName("customDimensions")]
        public Dictionary<string, double> CustomDimensions { get; set; } =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Resolves a skeleton parameter by name (used for expression lookup).</summary>
        public double Resolve(string name)
        {
            if (CustomDimensions.TryGetValue(name, out double custom))
                return custom;

            return name.ToUpperInvariant() switch
            {
                "DN" => DN,
                "DN2" => DN2,
                "FACETOFACE" => FaceToFace,
                "BODYOD" => BodyOD,
                "ELBOWCENTERTOFACE" => ElbowCenterToFace,
                "BODYLENGTH" => BodyLength,
                "BONNETHEIGHT" => BonnetHeight,
                "STEMDIA" => StemDia,
                "HANDWHEELOD" => HandwheelOD,
                _ => throw new KeyNotFoundException($"Unknown skeleton parameter '{name}'."),
            };
        }

        /// <summary>All names available to primitive parameter expressions.</summary>
        public IEnumerable<string> ExpressionParameterNames()
        {
            yield return "DN";
            yield return "DN2";
            yield return "FaceToFace";
            yield return "BodyOD";
            yield return "ElbowCenterToFace";
            yield return "BodyLength";
            yield return "BonnetHeight";
            yield return "StemDia";
            yield return "HandwheelOD";
            foreach (string key in CustomDimensions.Keys)
                yield return key;
        }
    }

    /// <summary>How a design dimension was measured and what scene element it relates to.</summary>
    public sealed class DimensionBinding
    {
        [JsonPropertyName("measureKind")]
        public string MeasureKind { get; set; } = "manual";

        [JsonPropertyName("fromPort")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? FromPort { get; set; }

        [JsonPropertyName("toPort")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? ToPort { get; set; }

        [JsonPropertyName("sceneNodeId")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Guid? SceneNodeId { get; set; }

        [JsonPropertyName("sceneNodeName")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? SceneNodeName { get; set; }

        [JsonPropertyName("paramKey")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ParamKey { get; set; }

        [JsonPropertyName("pickFromWcs")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double[]? PickFromWcs { get; set; }

        [JsonPropertyName("pickToWcs")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double[]? PickToWcs { get; set; }
    }

    /// <summary>Root of the scene graph; serialized to/from JSON.</summary>
    public sealed class ValveProject
    {
        public string SchemaVersion { get; set; } = "1.0";
        public string ValveName { get; set; } = string.Empty;

        /// <summary>Plant 3D @activate Group (Flange, Fitting, Valve, …). Empty → inferred on export.</summary>
        [JsonPropertyName("catalogGroup")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string CatalogGroup { get; set; } = string.Empty;

        [JsonPropertyName("tooltipShort")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string TooltipShort { get; set; } = string.Empty;

        [JsonPropertyName("tooltipLong")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string TooltipLong { get; set; } = string.Empty;

        /// <summary>Setup panel category (Buttwelded, Flange, …) → part.json category.</summary>
        [JsonPropertyName("catalogCategory")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string CatalogCategory { get; set; } = string.Empty;

        /// <summary>Plant Spec Editor PnP class (Elbow, Tee, Flange, …).</summary>
        [JsonPropertyName("pnpClassName")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string PnpClassName { get; set; } = string.Empty;

        /// <summary>Named size library (BW_SCH40, SW_CL3000, …) — inferred from primary end + schedule.</summary>
        [JsonPropertyName("standardSet")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string StandardSet { get; set; } = string.Empty;

        /// <summary>Plant 3D Primary End Type (Undefined_ET, FL, BV, …).</summary>
        [JsonPropertyName("primaryEndType")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string PrimaryEndType { get; set; } = string.Empty;

        /// <summary>Excel ShortDescription override (palette label).</summary>
        [JsonPropertyName("shortDescription")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string ShortDescription { get; set; } = string.Empty;

        /// <summary>Template sheet clone source part id (e.g. ELBOW_90_LR_BW_SCH40).</summary>
        [JsonPropertyName("excelCloneSourcePartId")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string ExcelCloneSourcePartId { get; set; } = string.Empty;

        /// <summary>Selected library part id from catalog_generator/parts (e.g. WN_FLRF_CL150).</summary>
        [JsonPropertyName("CustomPartId")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? CustomPartId { get; set; }

        public string Units { get; set; } = "mm";
        public SkeletonParameters Parameters { get; set; } = new();
        public List<PrimitiveNode> Parts { get; set; } = new();
        public List<BooleanOperation> Operations { get; set; } = new();
        public List<ConnectionPort> Ports { get; set; } = new();

        /// <summary>Per-dimension measure/bind metadata (pick, ports, scene node).</summary>
        [JsonPropertyName("dimensionBindings")]
        public Dictionary<string, DimensionBinding> DimensionBindings { get; set; } =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>When true, connection ports are drawn as arrows in the drawing.</summary>
        public bool ShowPortMarkers { get; set; } = true;

        public PrimitiveNode? FindNode(Guid id) => Parts.FirstOrDefault(n => n.Id == id);

        public ConnectionPort? FindPort(Guid id) => Ports.FirstOrDefault(p => p.Id == id);

        /// <summary>Next port number (1-based sequence).</summary>
        public int NextPortNumber()
        {
            if (Ports.Count == 0)
                return 1;
            return Ports.Max(p => p.Number) + 1;
        }

        /// <summary>Removes ports attached to deleted nodes; clears invalid parent refs.</summary>
        public int PrunePorts()
        {
            var nodeIds = new HashSet<Guid>(Parts.Select(p => p.Id));
            return Ports.RemoveAll(p =>
                p.ParentNodeId.HasValue && !nodeIds.Contains(p.ParentNodeId.Value));
        }

        /// <summary>Next free name for a prefix, scanning existing node names (CYL_001, CYL_002...).</summary>
        public string NextName(string prefix)
        {
            int max = 0;
            foreach (PrimitiveNode n in Parts)
            {
                if (n.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                    int.TryParse(n.Name.Substring(prefix.Length), out int v) && v > max)
                {
                    max = v;
                }
            }
            return prefix + (max + 1).ToString("000");
        }

        /// <summary>Removes operations that reference nodes which no longer exist.</summary>
        public int PruneOperations()
        {
            var ids = new HashSet<Guid>(Parts.Select(p => p.Id));
            return Operations.RemoveAll(op => !ids.Contains(op.Target) || op.Tools.Any(t => !ids.Contains(t)));
        }
    }
}
