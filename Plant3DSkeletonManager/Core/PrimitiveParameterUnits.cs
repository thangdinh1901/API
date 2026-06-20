using System;
using System.Collections.Generic;

namespace Plant3DSkeletonManager.Core
{
    /// <summary>Parameter units for primitive validation (mirrors PrimitiveCatalog in Composer).</summary>
    internal static class PrimitiveParameterUnits
    {
        private static readonly Dictionary<(PrimitiveType, string), CatalogParamUnit> Units =
            new(PrimitiveTypeNameComparer.Instance)
            {
                [(PrimitiveType.BOX, "L")] = CatalogParamUnit.Millimeter,
                [(PrimitiveType.BOX, "W")] = CatalogParamUnit.Millimeter,
                [(PrimitiveType.BOX, "H")] = CatalogParamUnit.Millimeter,

                [(PrimitiveType.CYLINDER, "D")] = CatalogParamUnit.Millimeter,
                [(PrimitiveType.CYLINDER, "L")] = CatalogParamUnit.Millimeter,
                [(PrimitiveType.CYLINDER, "O")] = CatalogParamUnit.Millimeter,
                [(PrimitiveType.CYLINDER, "R2")] = CatalogParamUnit.Millimeter,

                [(PrimitiveType.CONE, "D1")] = CatalogParamUnit.Millimeter,
                [(PrimitiveType.CONE, "D2")] = CatalogParamUnit.Millimeter,
                [(PrimitiveType.CONE, "H")] = CatalogParamUnit.Millimeter,
                [(PrimitiveType.CONE, "E")] = CatalogParamUnit.Unitless,

                [(PrimitiveType.TORUS, "D")] = CatalogParamUnit.Millimeter,
                [(PrimitiveType.TORUS, "T")] = CatalogParamUnit.Millimeter,

                [(PrimitiveType.SPHERE, "R")] = CatalogParamUnit.Millimeter,
                [(PrimitiveType.HALFSPHERE, "R")] = CatalogParamUnit.Millimeter,

                [(PrimitiveType.REDUCED_ELBOW, "D")] = CatalogParamUnit.Millimeter,
                [(PrimitiveType.REDUCED_ELBOW, "D2")] = CatalogParamUnit.Millimeter,
                [(PrimitiveType.REDUCED_ELBOW, "R")] = CatalogParamUnit.Millimeter,
                [(PrimitiveType.REDUCED_ELBOW, "A")] = CatalogParamUnit.Degree,

                [(PrimitiveType.ELBOW, "D")] = CatalogParamUnit.Millimeter,
                [(PrimitiveType.ELBOW, "R")] = CatalogParamUnit.Millimeter,
                [(PrimitiveType.ELBOW, "A")] = CatalogParamUnit.Degree,

                [(PrimitiveType.SEGMENTED_ELBOW, "D")] = CatalogParamUnit.Millimeter,
                [(PrimitiveType.SEGMENTED_ELBOW, "R")] = CatalogParamUnit.Millimeter,
                [(PrimitiveType.SEGMENTED_ELBOW, "A")] = CatalogParamUnit.Degree,
                [(PrimitiveType.SEGMENTED_ELBOW, "S")] = CatalogParamUnit.Unitless,

                [(PrimitiveType.ELLIPSOID_HEAD, "D")] = CatalogParamUnit.Millimeter,
                [(PrimitiveType.ELLIPSOID_HEAD2, "D")] = CatalogParamUnit.Millimeter,

                [(PrimitiveType.ELLIPSOID_SEGMENT, "RX")] = CatalogParamUnit.Millimeter,
                [(PrimitiveType.ELLIPSOID_SEGMENT, "RY")] = CatalogParamUnit.Millimeter,
                [(PrimitiveType.ELLIPSOID_SEGMENT, "A1")] = CatalogParamUnit.Degree,
                [(PrimitiveType.ELLIPSOID_SEGMENT, "A2")] = CatalogParamUnit.Degree,
                [(PrimitiveType.ELLIPSOID_SEGMENT, "A3")] = CatalogParamUnit.Degree,
                [(PrimitiveType.ELLIPSOID_SEGMENT, "A4")] = CatalogParamUnit.Degree,

                [(PrimitiveType.PYRAMID, "L")] = CatalogParamUnit.Millimeter,
                [(PrimitiveType.PYRAMID, "W")] = CatalogParamUnit.Millimeter,
                [(PrimitiveType.PYRAMID, "H")] = CatalogParamUnit.Millimeter,
                [(PrimitiveType.PYRAMID, "HT")] = CatalogParamUnit.Millimeter,

                [(PrimitiveType.ROUND_RECTANGLE, "L")] = CatalogParamUnit.Millimeter,
                [(PrimitiveType.ROUND_RECTANGLE, "W")] = CatalogParamUnit.Millimeter,
                [(PrimitiveType.ROUND_RECTANGLE, "H")] = CatalogParamUnit.Millimeter,
                [(PrimitiveType.ROUND_RECTANGLE, "R2")] = CatalogParamUnit.Millimeter,
                [(PrimitiveType.ROUND_RECTANGLE, "E")] = CatalogParamUnit.Unitless,

                [(PrimitiveType.SPHERE_SEGMENT, "R")] = CatalogParamUnit.Millimeter,
                [(PrimitiveType.SPHERE_SEGMENT, "H")] = CatalogParamUnit.Millimeter,
                [(PrimitiveType.SPHERE_SEGMENT, "SH")] = CatalogParamUnit.Millimeter,

                [(PrimitiveType.TORISPHERIC_HEAD, "D")] = CatalogParamUnit.Millimeter,
                [(PrimitiveType.TORISPHERIC_HEAD2, "D")] = CatalogParamUnit.Millimeter,
                [(PrimitiveType.TORISPHERIC_HEAD_H, "D")] = CatalogParamUnit.Millimeter,
                [(PrimitiveType.TORISPHERIC_HEAD_H, "H")] = CatalogParamUnit.Millimeter,

                [(PrimitiveType.FILLET, "R")] = CatalogParamUnit.Millimeter,
                [(PrimitiveType.FILLET, "H")] = CatalogParamUnit.Millimeter,
                [(PrimitiveType.FILLET, "A")] = CatalogParamUnit.Degree,

                [(PrimitiveType.CYLINDER_CHAMFERED, "D")] = CatalogParamUnit.Millimeter,
                [(PrimitiveType.CYLINDER_CHAMFERED, "L")] = CatalogParamUnit.Millimeter,
                [(PrimitiveType.CYLINDER_CHAMFERED, "C")] = CatalogParamUnit.Millimeter,
                [(PrimitiveType.CYLINDER_CHAMFERED, "CA")] = CatalogParamUnit.Degree,
                [(PrimitiveType.CYLINDER_CHAMFERED, "DF")] = CatalogParamUnit.Unitless,

                [(PrimitiveType.BOX_WITH_FILLET, "L")] = CatalogParamUnit.Millimeter,
                [(PrimitiveType.BOX_WITH_FILLET, "W")] = CatalogParamUnit.Millimeter,
                [(PrimitiveType.BOX_WITH_FILLET, "H")] = CatalogParamUnit.Millimeter,
                [(PrimitiveType.BOX_WITH_FILLET, "R")] = CatalogParamUnit.Millimeter,
                [(PrimitiveType.BOX_WITH_FILLET, "NF")] = CatalogParamUnit.Unitless,

                [(PrimitiveType.CYLINDER_WITH_FILLET, "D")] = CatalogParamUnit.Millimeter,
                [(PrimitiveType.CYLINDER_WITH_FILLET, "L")] = CatalogParamUnit.Millimeter,
                [(PrimitiveType.CYLINDER_WITH_FILLET, "FR")] = CatalogParamUnit.Millimeter,
                [(PrimitiveType.CYLINDER_WITH_FILLET, "DF")] = CatalogParamUnit.Unitless,
            };

        public static CatalogParamUnit Resolve(PrimitiveType type, string paramName)
        {
            if (Units.TryGetValue((type, paramName), out CatalogParamUnit unit))
                return unit;

            return CatalogParamUnit.Millimeter;
        }

        public static bool RequiresPositiveValue(PrimitiveType type, string paramName)
        {
            CatalogParamUnit unit = Resolve(type, paramName);
            return unit switch
            {
                CatalogParamUnit.Millimeter => !AllowsZeroMillimeters(type, paramName),
                CatalogParamUnit.Unitless => IsPositiveCountParam(type, paramName),
                CatalogParamUnit.Degree => false,
                _ => true,
            };
        }

        public static string ValidationMessage(PrimitiveType type, string paramName)
        {
            return Resolve(type, paramName) switch
            {
                CatalogParamUnit.Millimeter => $"parameter '{paramName}' must be > 0 mm.",
                CatalogParamUnit.Unitless => $"parameter '{paramName}' must be > 0.",
                CatalogParamUnit.Degree => $"parameter '{paramName}' must be >= 0 deg.",
                _ => $"parameter '{paramName}' is invalid.",
            };
        }

        private static bool IsPositiveCountParam(PrimitiveType type, string paramName) =>
            type == PrimitiveType.SEGMENTED_ELBOW && paramName.Equals("S", StringComparison.OrdinalIgnoreCase)
            || type == PrimitiveType.BOX_WITH_FILLET && paramName.Equals("NF", StringComparison.OrdinalIgnoreCase);

        private static bool AllowsZeroMillimeters(PrimitiveType type, string paramName) =>
            type == PrimitiveType.CONE && paramName.Equals("D2", StringComparison.OrdinalIgnoreCase)
            || type == PrimitiveType.PYRAMID && paramName.Equals("HT", StringComparison.OrdinalIgnoreCase)
            || type == PrimitiveType.SPHERE_SEGMENT && paramName.Equals("SH", StringComparison.OrdinalIgnoreCase);

        private sealed class PrimitiveTypeNameComparer : IEqualityComparer<(PrimitiveType, string)>
        {
            public static PrimitiveTypeNameComparer Instance { get; } = new();

            public bool Equals((PrimitiveType, string) x, (PrimitiveType, string) y) =>
                x.Item1 == y.Item1
                && x.Item2.Equals(y.Item2, StringComparison.OrdinalIgnoreCase);

            public int GetHashCode((PrimitiveType, string) obj) =>
                HashCode.Combine(obj.Item1, StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Item2));
        }
    }
}
