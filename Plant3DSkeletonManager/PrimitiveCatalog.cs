using System;
using System.Collections.Generic;
using Plant3DSkeletonManager.Core;

namespace Plant3DSkeletonManager
{
    using CatalogParam = (string logical, string inv, string expr, Func<SkeletonParameters, double> val, CatalogParamUnit unit);

    internal static class PrimitiveCatalog
    {
        private static readonly Func<SkeletonParameters, double> Od = v => v.BodyOD;
        private static readonly Func<SkeletonParameters, double> Len = v => v.BodyLength;
        private static readonly Func<SkeletonParameters, double> Hgt = v => v.BonnetHeight;
        private static readonly Func<SkeletonParameters, double> Rad = v => v.BodyOD / 2.0;
        private static readonly Func<SkeletonParameters, double> Hw = v => v.HandwheelOD;

        public static IReadOnlyList<PrimitiveDefinition> All { get; } = new[]
        {
            Make(PrimitiveType.BOX, "Box", "BOX_",
                Param("L", "L", "BodyLength", Len, CatalogParamUnit.Millimeter),
                Param("W", "W1", "BodyOD", Od, CatalogParamUnit.Millimeter),
                Param("H", "H1", "BodyOD", Od, CatalogParamUnit.Millimeter),
                TemplateBuilders.Box),

            Make(PrimitiveType.CYLINDER, "Cylinder", "CYL_",
                Param("D", "D", "BodyOD", Od, CatalogParamUnit.Millimeter),
                Param("L", "L", "BodyLength", Len, CatalogParamUnit.Millimeter),
                Param("O", "O", "BodyOD / 2", Rad, CatalogParamUnit.Millimeter),
                Param("R2", "R2", "BodyOD / 2", Rad, CatalogParamUnit.Millimeter),
                TemplateBuilders.Cylinder),

            Make(PrimitiveType.CONE, "Cone", "CONE_",
                Param("D1", "D1", "BodyOD", Od, CatalogParamUnit.Millimeter),
                Param("D2", "D2", "BodyOD * 0.5", v => v.BodyOD * 0.5, CatalogParamUnit.Millimeter),
                Param("H", "H1", "BonnetHeight", Hgt, CatalogParamUnit.Millimeter),
                Param("E", "E1", "0", _ => 0, CatalogParamUnit.Unitless),
                TemplateBuilders.Cone),

            Make(PrimitiveType.TORUS, "Torus", "TOR_",
                Param("D", "D", "HandwheelOD", Hw, CatalogParamUnit.Millimeter),
                Param("T", "T1", "HandwheelOD * 0.15", v => v.HandwheelOD * 0.15, CatalogParamUnit.Millimeter),
                TemplateBuilders.Torus),

            Make(PrimitiveType.SPHERE, "Sphere", "SPH_",
                Param("R", "R", "BodyOD / 2", Rad, CatalogParamUnit.Millimeter),
                TemplateBuilders.Sphere),

            Make(PrimitiveType.HALFSPHERE, "Half Sphere", "HSPH_",
                Param("R", "R", "BodyOD / 2", Rad, CatalogParamUnit.Millimeter),
                TemplateBuilders.HalfSphere),

            Make(PrimitiveType.REDUCED_ELBOW, "Reduced Elbow", "RELB_",
                Param("D", "D", "BodyOD", Od, CatalogParamUnit.Millimeter),
                Param("D2", "D2", "BodyOD * 0.75", v => v.BodyOD * 0.75, CatalogParamUnit.Millimeter),
                Param("R", "R", "BodyOD * 1.5", v => v.BodyOD * 1.5, CatalogParamUnit.Millimeter),
                Param("A", "A", "90 deg", _ => 90, CatalogParamUnit.Degree),
                TemplateBuilders.ReducedElbow),

            Make(PrimitiveType.ELBOW, "Elbow", "ELB_",
                Param("D", "D", "BodyOD", Od, CatalogParamUnit.Millimeter),
                Param("R", "R", "BodyOD * 1.5", v => v.BodyOD * 1.5, CatalogParamUnit.Millimeter),
                Param("A", "A", "90 deg", _ => 90, CatalogParamUnit.Degree),
                TemplateBuilders.Elbow),

            Make(PrimitiveType.SEGMENTED_ELBOW, "Segmented Elbow", "SELB_",
                Param("D", "D", "BodyOD", Od, CatalogParamUnit.Millimeter),
                Param("R", "R", "BodyOD * 1.5", v => v.BodyOD * 1.5, CatalogParamUnit.Millimeter),
                Param("A", "A", "90 deg", _ => 90, CatalogParamUnit.Degree),
                Param("S", "S", "4", _ => 4, CatalogParamUnit.Unitless),
                TemplateBuilders.SegmentedElbow),

            Make(PrimitiveType.ELLIPSOID_HEAD, "Ellipsoid Head", "EHEAD_",
                Param("D", "D", "BodyOD", Od, CatalogParamUnit.Millimeter),
                TemplateBuilders.EllipsoidHead),

            Make(PrimitiveType.ELLIPSOID_HEAD2, "Ellipsoid Head 2", "EHEAD2_",
                Param("D", "D", "BodyOD", Od, CatalogParamUnit.Millimeter),
                TemplateBuilders.EllipsoidHead2),

            Make(PrimitiveType.ELLIPSOID_SEGMENT, "Ellipsoid Segment", "ESEG_",
                Param("RX", "RX", "BodyOD / 2", Rad, CatalogParamUnit.Millimeter),
                Param("RY", "RY", "BodyOD / 2", Rad, CatalogParamUnit.Millimeter),
                Param("A1", "A1", "90 deg", _ => 90, CatalogParamUnit.Degree),
                Param("A2", "A2", "0 deg", _ => 0, CatalogParamUnit.Degree),
                Param("A3", "A3", "0 deg", _ => 0, CatalogParamUnit.Degree),
                Param("A4", "A4", "360 deg", _ => 360, CatalogParamUnit.Degree),
                TemplateBuilders.EllipsoidSegment),

            Make(PrimitiveType.PYRAMID, "Pyramid", "PYR_",
                Param("L", "L", "BodyLength", Len, CatalogParamUnit.Millimeter),
                Param("W", "W1", "BodyOD", Od, CatalogParamUnit.Millimeter),
                Param("H", "H1", "BonnetHeight", Hgt, CatalogParamUnit.Millimeter),
                Param("HT", "HT", "0", _ => 0, CatalogParamUnit.Millimeter),
                TemplateBuilders.Pyramid),

            Make(PrimitiveType.ROUND_RECTANGLE, "Round Rectangle", "RRECT_",
                Param("L", "L", "BodyLength", Len, CatalogParamUnit.Millimeter),
                Param("W", "W1", "BodyOD", Od, CatalogParamUnit.Millimeter),
                Param("H", "H1", "BonnetHeight", Hgt, CatalogParamUnit.Millimeter),
                Param("R2", "R2", "BodyOD * 0.1", v => v.BodyOD * 0.1, CatalogParamUnit.Millimeter),
                Param("E", "E1", "0", _ => 0, CatalogParamUnit.Unitless),
                TemplateBuilders.RoundRectangle),

            Make(PrimitiveType.SPHERE_SEGMENT, "Sphere Segment", "SSEG_",
                Param("R", "R", "BodyOD / 2", Rad, CatalogParamUnit.Millimeter),
                Param("H", "H1", "BonnetHeight", Hgt, CatalogParamUnit.Millimeter),
                Param("SH", "SH", "0", _ => 0, CatalogParamUnit.Millimeter),
                TemplateBuilders.SphereSegment),

            Make(PrimitiveType.TORISPHERIC_HEAD, "Torispheric Head (auto H)", "THEAD_",
                Param("D", "D", "BodyOD", Od, CatalogParamUnit.Millimeter),
                TemplateBuilders.TorisphericHead),

            Make(PrimitiveType.TORISPHERIC_HEAD2, "Torispheric Head 2 (auto H)", "THEAD2_",
                Param("D", "D", "BodyOD", Od, CatalogParamUnit.Millimeter),
                TemplateBuilders.TorisphericHead2),

            Make(PrimitiveType.TORISPHERIC_HEAD_H, "Torispheric Head (fixed H)", "THEADH_",
                Param("D", "D", "BodyOD", Od, CatalogParamUnit.Millimeter),
                Param("H", "H1", "BonnetHeight", Hgt, CatalogParamUnit.Millimeter),
                TemplateBuilders.TorisphericHeadH),
        };

        private static CatalogParam Param(
            string logical,
            string inv,
            string expr,
            Func<SkeletonParameters, double> val,
            CatalogParamUnit unit) => (logical, inv, expr, val, unit);

        private static PrimitiveDefinition Make(
            PrimitiveType type,
            string displayName,
            string prefix,
            CatalogParam p1,
            Action<Inventor.Application, Inventor.PartComponentDefinition> build) =>
            Make(type, displayName, prefix, new[] { p1 }, build);

        private static PrimitiveDefinition Make(
            PrimitiveType type,
            string displayName,
            string prefix,
            CatalogParam p1, CatalogParam p2,
            Action<Inventor.Application, Inventor.PartComponentDefinition> build) =>
            Make(type, displayName, prefix, new[] { p1, p2 }, build);

        private static PrimitiveDefinition Make(
            PrimitiveType type,
            string displayName,
            string prefix,
            CatalogParam p1, CatalogParam p2, CatalogParam p3,
            Action<Inventor.Application, Inventor.PartComponentDefinition> build) =>
            Make(type, displayName, prefix, new[] { p1, p2, p3 }, build);

        private static PrimitiveDefinition Make(
            PrimitiveType type,
            string displayName,
            string prefix,
            CatalogParam p1, CatalogParam p2, CatalogParam p3, CatalogParam p4,
            Action<Inventor.Application, Inventor.PartComponentDefinition> build) =>
            Make(type, displayName, prefix, new[] { p1, p2, p3, p4 }, build);

        private static PrimitiveDefinition Make(
            PrimitiveType type,
            string displayName,
            string prefix,
            CatalogParam p1, CatalogParam p2, CatalogParam p3, CatalogParam p4, CatalogParam p5,
            Action<Inventor.Application, Inventor.PartComponentDefinition> build) =>
            Make(type, displayName, prefix, new[] { p1, p2, p3, p4, p5 }, build);

        private static PrimitiveDefinition Make(
            PrimitiveType type,
            string displayName,
            string prefix,
            CatalogParam p1, CatalogParam p2, CatalogParam p3, CatalogParam p4, CatalogParam p5, CatalogParam p6,
            Action<Inventor.Application, Inventor.PartComponentDefinition> build) =>
            Make(type, displayName, prefix, new[] { p1, p2, p3, p4, p5, p6 }, build);

        private static PrimitiveDefinition Make(
            PrimitiveType type,
            string displayName,
            string prefix,
            CatalogParam[] parameters,
            Action<Inventor.Application, Inventor.PartComponentDefinition> build)
        {
            var mapped = new (string, string, string, Func<SkeletonParameters, double>, CatalogParamUnit)[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                var p = parameters[i];
                mapped[i] = (p.logical, p.inv, p.expr, p.val, p.unit);
            }

            return new PrimitiveDefinition
            {
                Type = type,
                DisplayName = displayName,
                Prefix = prefix,
                TemplateFile = $"P3D_{type}.ipt",
                Parameters = mapped,
                BuildGeometry = build,
            };
        }
    }
}
