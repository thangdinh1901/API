using System;
using System.Collections.Generic;
using System.Linq;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer
{
    using CatalogParam = (string logical, string expr, Func<SkeletonParameters, double> val, CatalogParamUnit unit);

    internal static class PrimitiveCatalog
    {
        // BodyOD is guaranteed > 0 at insert (PrimitiveService.Insert). BodyLength / BonnetHeight are
        // only set when the user fills the Dimensions tab, so they are usually 0 — which collapsed
        // length/height-driven primitives into a degenerate bounding box. Fall back to BodyOD-derived
        // dimensions so an inserted primitive always shows real geometry.
        private static readonly Func<SkeletonParameters, double> Od = v => v.BodyOD;
        private static readonly Func<SkeletonParameters, double> Len = v =>
            v.BodyLength > 0 ? v.BodyLength : v.BodyOD * 2.0;
        private static readonly Func<SkeletonParameters, double> Hgt = v =>
            v.BonnetHeight > 0 ? v.BonnetHeight : v.BodyOD * 0.5;
        private static readonly Func<SkeletonParameters, double> Rad = v => v.BodyOD / 2.0;
        private static readonly Func<SkeletonParameters, double> Hw = v =>
            v.HandwheelOD > 0 ? v.HandwheelOD : v.BodyOD;
        private static readonly Func<SkeletonParameters, double> BendR = v =>
            v.BodyOD > 0 ? v.BodyOD * 1.5 : 0;

        public static IReadOnlyList<PrimitiveDefinition> All { get; } = new[]
        {
            Make(PrimitiveType.BOX, "Box", "BOX_",
                Param("L", "BodyLength", Len, CatalogParamUnit.Millimeter),
                Param("W", "BodyOD", Od, CatalogParamUnit.Millimeter),
                Param("H", "BodyOD", Od, CatalogParamUnit.Millimeter)),

            Make(PrimitiveType.CYLINDER, "Cylinder", "CYL_",
                Param("D", "BodyOD", Od, CatalogParamUnit.Millimeter),
                Param("L", "BodyLength", Len, CatalogParamUnit.Millimeter),
                Param("O", "BodyOD / 2", Rad, CatalogParamUnit.Millimeter),
                Param("R2", "BodyOD / 2", Rad, CatalogParamUnit.Millimeter)),

            Make(PrimitiveType.CONE, "Cone", "CONE_",
                Param("D1", "BodyOD", Od, CatalogParamUnit.Millimeter),
                Param("D2", "BodyOD * 0.5", v => v.BodyOD * 0.5, CatalogParamUnit.Millimeter),
                Param("H", "BonnetHeight", Hgt, CatalogParamUnit.Millimeter),
                Param("E", "0", _ => 0, CatalogParamUnit.Unitless)),

            Make(PrimitiveType.TORUS, "Torus", "TOR_",
                Param("D", "BodyOD", Od, CatalogParamUnit.Millimeter),
                Param("T", "BodyOD * 0.15", v => v.BodyOD * 0.15, CatalogParamUnit.Millimeter)),

            Make(PrimitiveType.SPHERE, "Sphere", "SPH_",
                Param("R", "BodyOD / 2", Rad, CatalogParamUnit.Millimeter)),

            Make(PrimitiveType.HALFSPHERE, "Half Sphere", "HSPH_",
                Param("R", "BodyOD / 2", Rad, CatalogParamUnit.Millimeter)),

            Make(PrimitiveType.REDUCED_ELBOW, "Reduced Elbow", "RELB_",
                Param("D", "BodyOD", Od, CatalogParamUnit.Millimeter),
                Param("D2", "BodyOD * 0.75", v => v.BodyOD * 0.75, CatalogParamUnit.Millimeter),
                Param("R", "BodyOD * 1.5", BendR, CatalogParamUnit.Millimeter),
                Param("A", "90", _ => 90, CatalogParamUnit.Degree)),

            Make(PrimitiveType.ELBOW, "Elbow", "ELB_",
                Param("D", "BodyOD", Od, CatalogParamUnit.Millimeter),
                Param("R", "BodyOD * 1.5", BendR, CatalogParamUnit.Millimeter),
                Param("A", "90", _ => 90, CatalogParamUnit.Degree)),

            Make(PrimitiveType.SEGMENTED_ELBOW, "Segmented Elbow", "SELB_",
                Param("D", "BodyOD", Od, CatalogParamUnit.Millimeter),
                Param("R", "BodyOD * 1.5", BendR, CatalogParamUnit.Millimeter),
                Param("A", "90", _ => 90, CatalogParamUnit.Degree),
                Param("S", "4", _ => 4, CatalogParamUnit.Unitless)),

            Make(PrimitiveType.ELLIPSOID_HEAD, "Ellipsoid Head", "EHEAD_",
                Param("D", "BodyOD", Od, CatalogParamUnit.Millimeter)),

            Make(PrimitiveType.ELLIPSOID_HEAD2, "Ellipsoid Head 2", "EHEAD2_",
                Param("D", "BodyOD", Od, CatalogParamUnit.Millimeter)),

            // ELLIPSOID_SEGMENT is an SDK shell (surface) primitive — it never forms a usable solid
            // in the scene (always previews as a bounding box), so it is hidden from the insert list.
            // The enum + Python/scene_builder mapping are kept so older scenes still load.

            Make(PrimitiveType.PYRAMID, "Pyramid", "PYR_",
                Param("L", "BodyLength", Len, CatalogParamUnit.Millimeter),
                Param("W", "BodyOD", Od, CatalogParamUnit.Millimeter),
                Param("H", "BonnetHeight", Hgt, CatalogParamUnit.Millimeter),
                Param("HT", "0", _ => 0, CatalogParamUnit.Millimeter)),

            Make(PrimitiveType.ROUND_RECTANGLE, "Round Rectangle", "RRECT_",
                Param("L", "BodyLength", Len, CatalogParamUnit.Millimeter),
                Param("W", "BodyOD", Od, CatalogParamUnit.Millimeter),
                Param("H", "BonnetHeight", Hgt, CatalogParamUnit.Millimeter),
                Param("R2", "BodyOD * 0.1", v => v.BodyOD * 0.1, CatalogParamUnit.Millimeter),
                Param("E", "0", _ => 0, CatalogParamUnit.Unitless)),

            Make(PrimitiveType.SPHERE_SEGMENT, "Sphere Segment", "SSEG_",
                Param("R", "BodyOD / 2", Rad, CatalogParamUnit.Millimeter),
                Param("H", "BonnetHeight", Hgt, CatalogParamUnit.Millimeter),
                Param("SH", "0", _ => 0, CatalogParamUnit.Millimeter)),

            Make(PrimitiveType.TORISPHERIC_HEAD, "Torispheric Head (auto H)", "THEAD_",
                Param("D", "BodyOD", Od, CatalogParamUnit.Millimeter)),

            Make(PrimitiveType.TORISPHERIC_HEAD2, "Torispheric Head 2 (auto H)", "THEAD2_",
                Param("D", "BodyOD", Od, CatalogParamUnit.Millimeter)),

            Make(PrimitiveType.TORISPHERIC_HEAD_H, "Torispheric Head (fixed H)", "THEADH_",
                Param("D", "BodyOD", Od, CatalogParamUnit.Millimeter),
                Param("H", "BonnetHeight", Hgt, CatalogParamUnit.Millimeter)),

            Make(PrimitiveType.FILLET, "Fillet cutter", "FIL_",
                Param("R", "BodyOD * 0.1", v => v.BodyOD * 0.1, CatalogParamUnit.Millimeter),
                Param("H", "BodyLength", Len, CatalogParamUnit.Millimeter),
                Param("A", "90 deg", _ => 90, CatalogParamUnit.Degree)),

            Make(PrimitiveType.CYLINDER_CHAMFERED, "Cylinder chamfered", "CCH_",
                Param("D", "BodyOD", Od, CatalogParamUnit.Millimeter),
                Param("L", "BodyLength", Len, CatalogParamUnit.Millimeter),
                Param("C", "BodyOD * 0.05", v => v.BodyOD * 0.05, CatalogParamUnit.Millimeter),
                Param("CA", "45 deg", _ => 45, CatalogParamUnit.Degree),
                Param("DF", "0", _ => 0, CatalogParamUnit.Unitless)),

            Make(PrimitiveType.BOX_WITH_FILLET, "Box with fillet", "BFIL_",
                Param("L", "BodyLength", Len, CatalogParamUnit.Millimeter),
                Param("W", "BodyOD", Od, CatalogParamUnit.Millimeter),
                Param("H", "BonnetHeight", Hgt, CatalogParamUnit.Millimeter),
                Param("R", "BodyOD * 0.1", v => v.BodyOD * 0.1, CatalogParamUnit.Millimeter),
                Param("NF", "4", _ => 4, CatalogParamUnit.Unitless)),

            Make(PrimitiveType.CYLINDER_WITH_FILLET, "Cylinder with fillet", "CFIL_",
                Param("D", "BodyOD", Od, CatalogParamUnit.Millimeter),
                Param("L", "BodyLength", Len, CatalogParamUnit.Millimeter),
                Param("FR", "BodyOD * 0.1", v => v.BodyOD * 0.1, CatalogParamUnit.Millimeter),
                Param("DF", "0", _ => 0, CatalogParamUnit.Unitless)),
        };

        /// <summary>Fillet/chamfer shapes used as boolean subtract tools.</summary>
        public static IReadOnlyList<PrimitiveDefinition> Cutters { get; } = All
            .Where(p => IsCutterType(p.Type))
            .ToArray();

        /// <summary>Cutter primitives are boolean tools — they must not be auto-unioned into the body.</summary>
        public static bool IsCutterType(PrimitiveType type) =>
            type is PrimitiveType.FILLET
                or PrimitiveType.CYLINDER_CHAMFERED
                or PrimitiveType.BOX_WITH_FILLET
                or PrimitiveType.CYLINDER_WITH_FILLET;

        private static CatalogParam Param(
            string logical,
            string expr,
            Func<SkeletonParameters, double> val,
            CatalogParamUnit unit) => (logical, expr, val, unit);

        private static PrimitiveDefinition Make(
            PrimitiveType type,
            string displayName,
            string prefix,
            CatalogParam p1) =>
            Make(type, displayName, prefix, new[] { p1 });

        private static PrimitiveDefinition Make(
            PrimitiveType type,
            string displayName,
            string prefix,
            CatalogParam p1, CatalogParam p2) =>
            Make(type, displayName, prefix, new[] { p1, p2 });

        private static PrimitiveDefinition Make(
            PrimitiveType type,
            string displayName,
            string prefix,
            CatalogParam p1, CatalogParam p2, CatalogParam p3) =>
            Make(type, displayName, prefix, new[] { p1, p2, p3 });

        private static PrimitiveDefinition Make(
            PrimitiveType type,
            string displayName,
            string prefix,
            CatalogParam p1, CatalogParam p2, CatalogParam p3, CatalogParam p4) =>
            Make(type, displayName, prefix, new[] { p1, p2, p3, p4 });

        private static PrimitiveDefinition Make(
            PrimitiveType type,
            string displayName,
            string prefix,
            CatalogParam p1, CatalogParam p2, CatalogParam p3, CatalogParam p4, CatalogParam p5) =>
            Make(type, displayName, prefix, new[] { p1, p2, p3, p4, p5 });

        private static PrimitiveDefinition Make(
            PrimitiveType type,
            string displayName,
            string prefix,
            CatalogParam p1, CatalogParam p2, CatalogParam p3, CatalogParam p4, CatalogParam p5, CatalogParam p6) =>
            Make(type, displayName, prefix, new[] { p1, p2, p3, p4, p5, p6 });

        private static PrimitiveDefinition Make(
            PrimitiveType type,
            string displayName,
            string prefix,
            CatalogParam[] parameters)
        {
            var mapped = new (string, string, Func<SkeletonParameters, double>, CatalogParamUnit)[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                var p = parameters[i];
                mapped[i] = (p.logical, p.expr, p.val, p.unit);
            }

            return new PrimitiveDefinition
            {
                Type = type,
                DisplayName = displayName,
                Prefix = prefix,
                Parameters = mapped,
            };
        }
    }
}
