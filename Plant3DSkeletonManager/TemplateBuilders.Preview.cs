using Inventor;

namespace Plant3DSkeletonManager
{
    /// <summary>Visual-only Inventor geometry interpolated from logical catalog parameters.</summary>
    internal static partial class TemplateBuilders
    {
        /// <summary>Pipe bend preview: sweep D along a 90° arc path (R, A on part for Plant 3D).</summary>
        public static void BuildElbowSweepPreview(Inventor.Application app, PartComponentDefinition def)
        {
            TransientGeometry g = app.TransientGeometry;

            PlanarSketch pathSk = def.Sketches.Add(def.WorkPlanes[3]);
            SketchArc bend = pathSk.SketchArcs.AddByCenterStartEndPoint(
                g.CreatePoint2d(8, 0), g.CreatePoint2d(0, 0), g.CreatePoint2d(8, 8), true);

            DimensionConstraint dimArc = (DimensionConstraint)pathSk.DimensionConstraints.AddRadius(
                (SketchEntity)bend, g.CreatePoint2d(10, 4));
            dimArc.Parameter.Expression = "V_arc_R";

            ObjectCollection pathEntities = app.TransientObjects.CreateObjectCollection();
            pathEntities.Add(bend);
            Path path = def.Features.CreatePath(pathEntities);

            PlanarSketch profileSk = def.Sketches.Add(def.WorkPlanes[2]);
            SketchCircle tube = profileSk.SketchCircles.AddByCenterRadius(g.CreatePoint2d(0, 0), 2);
            DimensionConstraint dimD = (DimensionConstraint)profileSk.DimensionConstraints.AddDiameter(
                (SketchEntity)tube, g.CreatePoint2d(3, 3));
            dimD.Parameter.Expression = "D";

            Profile profile = profileSk.Profiles.AddForSolid();
            SweepDefinition sweepDef = def.Features.SweepFeatures.CreateSweepDefinition(
                SweepTypeEnum.kPathSweepType,
                profile,
                path,
                PartFeatureOperationEnum.kNewBodyOperation);
            def.Features.SweepFeatures.Add(sweepDef);
        }

        /// <summary>Fallback when sweep fails: torus corner (still reads D, R).</summary>
        public static void BuildElbowTorusFallback(Inventor.Application app, PartComponentDefinition def)
        {
            TransientGeometry g = app.TransientGeometry;
            PlanarSketch sk = def.Sketches.Add(def.WorkPlanes[2]);

            SketchLine axis = sk.SketchLines.AddByTwoPoints(
                g.CreatePoint2d(0, -3), g.CreatePoint2d(0, 3));
            axis.Centerline = true;
            sk.GeometricConstraints.AddGround((SketchEntity)axis);

            SketchPoint origin = sk.SketchPoints.Add(g.CreatePoint2d(0, 0), false);
            sk.GeometricConstraints.AddGround((SketchEntity)origin);

            SketchCircle tube = sk.SketchCircles.AddByCenterRadius(g.CreatePoint2d(8, 0), 2);
            DimensionConstraint dimT = (DimensionConstraint)sk.DimensionConstraints.AddDiameter(
                (SketchEntity)tube, g.CreatePoint2d(10, 3));
            dimT.Parameter.Expression = "D";

            DimensionConstraint dimR = (DimensionConstraint)sk.DimensionConstraints.AddTwoPointDistance(
                origin, tube.CenterSketchPoint,
                DimensionOrientationEnum.kHorizontalDim, g.CreatePoint2d(4, -3));
            dimR.Parameter.Expression = "V_arc_R";

            RevolveBy(def, sk, axis);
        }

        /// <summary>Frustum preview: loft L×W base to smaller top at H1.</summary>
        public static void BuildPyramidLoftPreview(Inventor.Application app, PartComponentDefinition def)
        {
            TransientGeometry g = app.TransientGeometry;

            PlanarSketch bottom = def.Sketches.Add(def.WorkPlanes[3]);
            SketchEntitiesEnumerator bottomRect = bottom.SketchLines.AddAsTwoPointRectangle(
                g.CreatePoint2d(-7.5, -5), g.CreatePoint2d(7.5, 5));
            SketchLine bBottom = (SketchLine)bottomRect[1];
            SketchLine bRight = (SketchLine)bottomRect[2];
            SketchPoint bCenter = bottom.SketchPoints.Add(g.CreatePoint2d(0, 0), false);
            bottom.GeometricConstraints.AddGround((SketchEntity)bCenter);
            bottom.GeometricConstraints.AddMidpoint(bCenter, bBottom);

            DimensionConstraint dimL = (DimensionConstraint)bottom.DimensionConstraints.AddTwoPointDistance(
                bBottom.StartSketchPoint, bBottom.EndSketchPoint,
                DimensionOrientationEnum.kHorizontalDim, g.CreatePoint2d(0, -7));
            dimL.Parameter.Expression = "L";

            DimensionConstraint dimW = (DimensionConstraint)bottom.DimensionConstraints.AddTwoPointDistance(
                bRight.StartSketchPoint, bRight.EndSketchPoint,
                DimensionOrientationEnum.kVerticalDim, g.CreatePoint2d(10, 0));
            dimW.Parameter.Expression = "W1";

            WorkPlane topPlane = def.WorkPlanes.AddByPlaneAndOffset(
                def.WorkPlanes[3],
                def.Parameters.UserParameters["H1"],
                false);

            PlanarSketch top = def.Sketches.Add(topPlane);
            SketchEntitiesEnumerator topRect = top.SketchLines.AddAsTwoPointRectangle(
                g.CreatePoint2d(-1, -1), g.CreatePoint2d(1, 1));
            SketchLine tBottom = (SketchLine)topRect[1];
            SketchLine tRight = (SketchLine)topRect[2];
            SketchPoint tCenter = top.SketchPoints.Add(g.CreatePoint2d(0, 0), false);
            top.GeometricConstraints.AddGround((SketchEntity)tCenter);
            top.GeometricConstraints.AddMidpoint(tCenter, tBottom);

            DimensionConstraint dimTopL = (DimensionConstraint)top.DimensionConstraints.AddTwoPointDistance(
                tBottom.StartSketchPoint, tBottom.EndSketchPoint,
                DimensionOrientationEnum.kHorizontalDim, g.CreatePoint2d(0, -2));
            dimTopL.Parameter.Expression = "V_top_L";

            DimensionConstraint dimTopW = (DimensionConstraint)top.DimensionConstraints.AddTwoPointDistance(
                tRight.StartSketchPoint, tRight.EndSketchPoint,
                DimensionOrientationEnum.kVerticalDim, g.CreatePoint2d(3, 0));
            dimTopW.Parameter.Expression = "V_top_W";

            ObjectCollection sections = app.TransientObjects.CreateObjectCollection();
            sections.Add(bottom.Profiles.AddForSolid());
            sections.Add(top.Profiles.AddForSolid());

            LoftDefinition loftDef = def.Features.LoftFeatures.CreateLoftDefinition(
                sections, PartFeatureOperationEnum.kNewBodyOperation);
            def.Features.LoftFeatures.Add(loftDef);
        }

        /// <summary>Ellipsoid shell preview: revolve profile from RX / V_height.</summary>
        public static void BuildEllipsoidSegmentPreview(Inventor.Application app, PartComponentDefinition def)
        {
            TransientGeometry g = app.TransientGeometry;
            PlanarSketch sk = def.Sketches.Add(def.WorkPlanes[2]);

            SketchLine baseLine = sk.SketchLines.AddByTwoPoints(
                g.CreatePoint2d(0, 0), g.CreatePoint2d(6, 0));
            SketchLine side = sk.SketchLines.AddByTwoPoints(
                baseLine.EndSketchPoint, g.CreatePoint2d(0, 4));
            SketchLine axis = sk.SketchLines.AddByTwoPoints(
                side.EndSketchPoint, baseLine.StartSketchPoint);
            sk.GeometricConstraints.AddGround((SketchEntity)baseLine.StartSketchPoint);

            DimensionConstraint dimRx = (DimensionConstraint)sk.DimensionConstraints.AddTwoPointDistance(
                baseLine.StartSketchPoint, baseLine.EndSketchPoint,
                DimensionOrientationEnum.kHorizontalDim, g.CreatePoint2d(3, -2));
            dimRx.Parameter.Expression = "RX";

            DimensionConstraint dimRy = (DimensionConstraint)sk.DimensionConstraints.AddTwoPointDistance(
                baseLine.EndSketchPoint, side.EndSketchPoint,
                DimensionOrientationEnum.kVerticalDim, g.CreatePoint2d(8, 2));
            dimRy.Parameter.Expression = "V_height";

            RevolveBy(def, sk, axis);
        }

        private static void BuildElbowPreviewSafe(Inventor.Application app, PartComponentDefinition def)
        {
            try
            {
                BuildElbowSweepPreview(app, def);
            }
            catch
            {
                BuildElbowTorusFallback(app, def);
            }
        }

        private static void BuildPyramidPreviewSafe(Inventor.Application app, PartComponentDefinition def)
        {
            try
            {
                BuildPyramidLoftPreview(app, def);
            }
            catch
            {
                ExtrudeCenteredRect(app, def, "L", "W1", "H1");
            }
        }
    }
}
