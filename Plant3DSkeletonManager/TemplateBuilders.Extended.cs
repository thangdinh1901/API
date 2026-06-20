using Inventor;

namespace Plant3DSkeletonManager
{
    /// <summary>Template builders for extended Plant 3D native primitives.</summary>
    internal static partial class TemplateBuilders
    {
        public static void ReducedElbow(Inventor.Application app, PartComponentDefinition def)
        {
            AddLength(def, "D", "100 mm");
            AddLength(def, "D2", "75 mm");
            AddLength(def, "R", "150 mm");
            AddAngle(def, "A", "90 deg");
            PreviewGeometry.AddReducedElbowPreviewParams(def);
            BuildElbowPreviewSafe(app, def);
        }

        public static void Elbow(Inventor.Application app, PartComponentDefinition def)
        {
            AddLength(def, "D", "100 mm");
            AddLength(def, "R", "150 mm");
            AddAngle(def, "A", "90 deg");
            PreviewGeometry.AddElbowPreviewParams(def);
            BuildElbowPreviewSafe(app, def);
        }

        public static void SegmentedElbow(Inventor.Application app, PartComponentDefinition def)
        {
            AddLength(def, "D", "100 mm");
            AddLength(def, "R", "150 mm");
            AddAngle(def, "A", "90 deg");
            AddUnitless(def, "S", 4);
            PreviewGeometry.AddElbowPreviewParams(def);
            BuildElbowPreviewSafe(app, def);
        }

        public static void EllipsoidHead(Inventor.Application app, PartComponentDefinition def)
        {
            AddLength(def, "D", "100 mm");
            RevolveDishProfile(app, def, "D / 4");
        }

        public static void EllipsoidHead2(Inventor.Application app, PartComponentDefinition def)
        {
            AddLength(def, "D", "100 mm");
            RevolveDishProfile(app, def, "D / 3.5");
        }

        public static void EllipsoidSegment(Inventor.Application app, PartComponentDefinition def)
        {
            AddLength(def, "RX", "50 mm");
            AddLength(def, "RY", "50 mm");
            AddAngle(def, "A1", "90 deg");
            AddAngle(def, "A2", "0 deg");
            AddAngle(def, "A3", "0 deg");
            AddAngle(def, "A4", "360 deg");
            PreviewGeometry.AddEllipsoidSegmentPreviewParams(def);
            BuildEllipsoidSegmentPreview(app, def);
        }

        public static void Pyramid(Inventor.Application app, PartComponentDefinition def)
        {
            AddLength(def, "L", "150 mm");
            AddLength(def, "W1", "100 mm");
            AddLength(def, "H1", "100 mm");
            AddLength(def, "HT", "0 mm");
            PreviewGeometry.AddPyramidPreviewParams(def);
            BuildPyramidPreviewSafe(app, def);
        }

        public static void RoundRectangle(Inventor.Application app, PartComponentDefinition def)
        {
            AddLength(def, "L", "150 mm");
            AddLength(def, "W1", "100 mm");
            AddLength(def, "H1", "80 mm");
            AddLength(def, "R2", "10 mm");
            AddUnitless(def, "E1", 0);
            ExtrudeCenteredRect(app, def, "L", "W1", "H1");
            TryEdgeFillet(app, def, "R2");
        }

        public static void SphereSegment(Inventor.Application app, PartComponentDefinition def)
        {
            AddLength(def, "R", "50 mm");
            AddLength(def, "H1", "30 mm");
            AddLength(def, "SH", "0 mm");
            RevolveSphericalCap(app, def, "R", "H1");
        }

        public static void TorisphericHead(Inventor.Application app, PartComponentDefinition def)
        {
            AddLength(def, "D", "100 mm");
            RevolveDishProfile(app, def, "D / 6");
        }

        public static void TorisphericHead2(Inventor.Application app, PartComponentDefinition def)
        {
            AddLength(def, "D", "100 mm");
            RevolveDishProfile(app, def, "D / 5");
        }

        public static void TorisphericHeadH(Inventor.Application app, PartComponentDefinition def)
        {
            AddLength(def, "D", "100 mm");
            AddLength(def, "H1", "25 mm");
            RevolveDishProfile(app, def, "H1");
        }

        private static void RevolveDishProfile(
            Inventor.Application app,
            PartComponentDefinition def,
            string heightExpr)
        {
            TransientGeometry g = app.TransientGeometry;
            PlanarSketch sk = def.Sketches.Add(def.WorkPlanes[2]);

            SketchLine baseLine = sk.SketchLines.AddByTwoPoints(
                g.CreatePoint2d(0, 0), g.CreatePoint2d(5, 0));
            SketchLine side = sk.SketchLines.AddByTwoPoints(
                baseLine.EndSketchPoint, g.CreatePoint2d(0, 2.5));
            SketchLine axis = sk.SketchLines.AddByTwoPoints(
                side.EndSketchPoint, baseLine.StartSketchPoint);

            sk.GeometricConstraints.AddGround((SketchEntity)baseLine.StartSketchPoint);

            DimensionConstraint dimR = (DimensionConstraint)sk.DimensionConstraints.AddTwoPointDistance(
                baseLine.StartSketchPoint, baseLine.EndSketchPoint,
                DimensionOrientationEnum.kHorizontalDim, g.CreatePoint2d(3, -2));
            dimR.Parameter.Expression = "D / 2";

            DimensionConstraint dimH = (DimensionConstraint)sk.DimensionConstraints.AddTwoPointDistance(
                baseLine.EndSketchPoint, side.EndSketchPoint,
                DimensionOrientationEnum.kVerticalDim, g.CreatePoint2d(7, 1));
            dimH.Parameter.Expression = heightExpr;

            RevolveBy(def, sk, axis);
        }

        private static void RevolveSphericalCap(
            Inventor.Application app,
            PartComponentDefinition def,
            string radiusExpr,
            string heightExpr)
        {
            TransientGeometry g = app.TransientGeometry;
            PlanarSketch sk = def.Sketches.Add(def.WorkPlanes[2]);

            SketchPoint basePt = sk.SketchPoints.Add(g.CreatePoint2d(0, 0), false);
            sk.GeometricConstraints.AddGround((SketchEntity)basePt);

            SketchArc arc = sk.SketchArcs.AddByCenterStartEndPoint(
                g.CreatePoint2d(0, 5), g.CreatePoint2d(0, 0), g.CreatePoint2d(5, 5), true);
            SketchLine axis = sk.SketchLines.AddByTwoPoints(
                arc.EndSketchPoint, arc.StartSketchPoint);

            sk.GeometricConstraints.AddCoincident(
                (SketchEntity)basePt, (SketchEntity)arc.StartSketchPoint);

            DimensionConstraint dimR = (DimensionConstraint)sk.DimensionConstraints.AddRadius(
                (SketchEntity)arc, g.CreatePoint2d(6, 6));
            dimR.Parameter.Expression = radiusExpr;

            RevolveBy(def, sk, axis);
        }

        private static void ExtrudeCenteredRect(
            Inventor.Application app,
            PartComponentDefinition def,
            string lengthExpr,
            string widthExpr,
            string heightExpr)
        {
            TransientGeometry g = app.TransientGeometry;
            PlanarSketch sk = def.Sketches.Add(def.WorkPlanes[3]);
            SketchEntitiesEnumerator lines = sk.SketchLines.AddAsTwoPointRectangle(
                g.CreatePoint2d(-7.5, -5), g.CreatePoint2d(7.5, 5));
            SketchLine bottom = (SketchLine)lines[1];
            SketchLine right = (SketchLine)lines[2];

            SketchPoint center = sk.SketchPoints.Add(g.CreatePoint2d(0, 0), false);
            sk.GeometricConstraints.AddGround((SketchEntity)center);
            sk.GeometricConstraints.AddMidpoint(center, bottom);

            DimensionConstraint dimL = (DimensionConstraint)sk.DimensionConstraints.AddTwoPointDistance(
                bottom.StartSketchPoint, bottom.EndSketchPoint,
                DimensionOrientationEnum.kHorizontalDim, g.CreatePoint2d(0, -7));
            dimL.Parameter.Expression = lengthExpr;

            DimensionConstraint dimW = (DimensionConstraint)sk.DimensionConstraints.AddTwoPointDistance(
                right.StartSketchPoint, right.EndSketchPoint,
                DimensionOrientationEnum.kVerticalDim, g.CreatePoint2d(10, 0));
            dimW.Parameter.Expression = widthExpr;

            ExtrudeBy(def, sk, heightExpr);
        }

        private static void TryEdgeFillet(Inventor.Application app, PartComponentDefinition def, string radiusExpr)
        {
            try
            {
                EdgeCollection edges = app.TransientObjects.CreateEdgeCollection();
                foreach (SurfaceBody body in def.SurfaceBodies)
                {
                    foreach (Edge edge in body.Edges)
                        edges.Add(edge);
                }
                if (edges.Count > 0)
                {
                    def.Features.FilletFeatures.AddSimple(
                        edges, radiusExpr, false, false, false, false, false, false);
                }
            }
            catch
            {
            }
        }

        private static void AddLength(PartComponentDefinition def, string name, string expr) =>
            AddLength(def, name, expr, UnitsTypeEnum.kMillimeterLengthUnits);

        private static void AddLength(PartComponentDefinition def, string name, string expr, UnitsTypeEnum units)
        {
            UserParameters up = def.Parameters.UserParameters;
            if (ParameterExists(up, name))
                return;

            if (units == UnitsTypeEnum.kUnitlessUnits)
            {
                up.AddByValue(
                    name,
                    double.Parse(expr, System.Globalization.CultureInfo.InvariantCulture),
                    units);
            }
            else
            {
                up.AddByExpression(name, expr, units);
            }
        }

        private static void AddUnitless(PartComponentDefinition def, string name, double value)
        {
            UserParameters up = def.Parameters.UserParameters;
            if (!ParameterExists(up, name))
                up.AddByValue(name, value, UnitsTypeEnum.kUnitlessUnits);
        }

        private static void AddAngle(PartComponentDefinition def, string name, string expr)
        {
            UserParameters up = def.Parameters.UserParameters;
            if (!ParameterExists(up, name))
                up.AddByExpression(name, expr, UnitsTypeEnum.kDegreeAngleUnits);
        }

        private static bool ParameterExists(UserParameters up, string name)
        {
            foreach (UserParameter p in up)
            {
                if (string.Equals(p.Name, name, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}
