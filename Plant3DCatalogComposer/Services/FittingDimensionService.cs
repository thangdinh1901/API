using System;
using System.Collections.Generic;
using System.Linq;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer.Services
{
    /// <summary>
    /// Pipe run diameter (BodyOD) and catalog center-to-face (ElbowCenterToFace) from DN.
    /// Scene elbow <c>R</c> is bend radius (geometry) — not linked to ElbowCenterToFace.
    /// </summary>
    internal static class FittingDimensionService
    {
        internal enum ConnectionStyle
        {
            ButtWeld,
            SocketWeld,
            Threaded,
        }

        private static readonly Dictionary<int, double> BwLr90CenterToFaceMm = new()
        {
            [15] = 38,
            [20] = 38,
            [25] = 38,
            [32] = 48,
            [40] = 57,
            [50] = 76,
            [65] = 95,
            [80] = 114,
            [90] = 133,
            [100] = 152,
            [125] = 190,
            [150] = 229,
            [200] = 305,
            [250] = 381,
            [300] = 457,
            [350] = 533,
            [400] = 610,
            [450] = 686,
        };

        /// <summary>B16.11 SW — A: center to inner socket shoulder (mm).</summary>
        private static readonly Dictionary<int, double> SwCenterToSocketBottomMm = new()
        {
            [6] = 11.0,
            [8] = 11.0,
            [10] = 13.5,
            [15] = 15.5,
            [20] = 19.0,
            [25] = 22.5,
            [32] = 27.0,
            [40] = 32.0,
            [50] = 38.0,
            [65] = 41.0,
            [80] = 57.0,
            [100] = 66.5,
        };

        /// <summary>B16.11 SW — J: minimum socket depth (mm).</summary>
        private static readonly Dictionary<int, double> SwSocketDepthMm = new()
        {
            [6] = 9.5,
            [8] = 9.5,
            [10] = 9.5,
            [15] = 9.5,
            [20] = 12.5,
            [25] = 12.5,
            [32] = 12.5,
            [40] = 12.5,
            [50] = 16.0,
            [65] = 16.0,
            [80] = 16.0,
            [100] = 19.0,
        };

        public static bool UsesPipeRunDimensions(string? catalogGroup) =>
            string.Equals(catalogGroup, "Fitting", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(catalogGroup, "Olet", StringComparison.OrdinalIgnoreCase);

        /// <summary>Typical LR bend radius (1.5× pipe OD) for new elbow primitives — not center-to-face.</summary>
        public static double DefaultElbowBendRadiusMm(SkeletonParameters p) =>
            p.BodyOD > 0 ? p.BodyOD * 1.5 : 0;

        /// <summary>True when elbow D should follow project BodyOD (Catalog DN sync), not manual Scene edits.</summary>
        internal static bool IsElbowDiameterBoundToBodyOd(ParamValue param, SkeletonParameters skeleton)
        {
            double bodyOd = skeleton.BodyOD;
            string expr = param.Expression?.Trim() ?? "";
            if (expr.Equals("BodyOD", StringComparison.OrdinalIgnoreCase)
                || expr.Equals("pipe OD from DN", StringComparison.OrdinalIgnoreCase))
            {
                return !SceneParamBindingService.HasManualOverride(param, skeleton);
            }

            if (!string.IsNullOrEmpty(expr))
                return false;

            return param.Value <= 0 || Math.Abs(param.Value - bodyOd) <= 1e-9;
        }

        /// <summary>Link elbow D→BodyOD; decouple R (bend radius) from ElbowCenterToFace.</summary>
        public static void NormalizeElbowDimensionBindings(ValveProject project)
        {
            foreach (PrimitiveNode node in project.Parts)
            {
                if (node.Kind != SceneNodeKind.Primitive)
                    continue;

                if (node.Type is not (PrimitiveType.ELBOW or PrimitiveType.SEGMENTED_ELBOW or PrimitiveType.REDUCED_ELBOW))
                    continue;

                RewireParamExpression(node, "D", "BodyOD", "pipe OD from DN", project.Parameters);
                DecoupleBendRadiusFromCenterToFace(node, project.Parameters);
            }
        }

        private static void DecoupleBendRadiusFromCenterToFace(PrimitiveNode node, SkeletonParameters skeleton)
        {
            if (!node.Parameters.TryGetValue("R", out ParamValue? param))
                return;

            string expr = param.Expression?.Trim() ?? "";
            if (!IsCenterToFaceExpression(expr))
                return;

            param.Value = ResolveElbowBendRadiusMm(node, skeleton);
            param.Expression = null;
        }

        /// <summary>Scene elbow bend radius (mm) — never center-to-face.</summary>
        public static double ResolveElbowBendRadiusMm(PrimitiveNode node, SkeletonParameters skeleton)
        {
            double fallback = DefaultElbowBendRadiusMm(skeleton);
            if (!node.Parameters.TryGetValue("R", out ParamValue? param))
                return fallback > 0 ? fallback : 0;

            string expr = param.Expression?.Trim() ?? "";
            if (IsCenterToFaceExpression(expr))
                return fallback > 0 ? fallback : 0;

            double ctf = skeleton.ElbowCenterToFace;
            if (param.Value > 0)
            {
                if (ctf > 0 && Math.Abs(param.Value - ctf) < 0.01)
                    return fallback > 0 ? fallback : param.Value;

                return param.Value;
            }

            return fallback > 0 ? fallback : 0;
        }

        public static bool TryGetSceneBendRadiusMm(ValveProject project, out double bendRadiusMm)
        {
            bendRadiusMm = 0;
            foreach (PrimitiveNode node in project.Parts)
            {
                if (node.Kind != SceneNodeKind.Primitive)
                    continue;

                if (node.Type is not (PrimitiveType.ELBOW or PrimitiveType.SEGMENTED_ELBOW or PrimitiveType.REDUCED_ELBOW))
                    continue;

                double bend = ResolveElbowBendRadiusMm(node, project.Parameters);
                if (bend > 0)
                {
                    bendRadiusMm = bend;
                    return true;
                }
            }

            return false;
        }

        private static bool IsCenterToFaceExpression(string expr) =>
            expr.Equals("ElbowCenterToFace", StringComparison.OrdinalIgnoreCase)
            || expr.Equals("LR 90 center-to-face", StringComparison.OrdinalIgnoreCase);

        private static void RewireParamExpression(
            PrimitiveNode node,
            string paramKey,
            string designName,
            string legacyLabel,
            SkeletonParameters skeleton)
        {
            if (!node.Parameters.TryGetValue(paramKey, out ParamValue? param))
                return;

            string expr = param.Expression?.Trim() ?? "";
            if (expr.Equals(designName, StringComparison.OrdinalIgnoreCase))
            {
                if (SceneParamBindingService.HasManualOverride(param, skeleton))
                    param.Expression = null;

                return;
            }

            // Keep manual Scene diameter (no expression, value set by user).
            if (string.IsNullOrEmpty(expr) && param.Value > 0
                && !IsElbowDiameterBoundToBodyOd(param, skeleton))
            {
                return;
            }

            if (expr.Equals(legacyLabel, StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrEmpty(expr)
                || !ExpressionEvaluator.TryEvaluate(expr, skeleton, out _))
            {
                param.Expression = designName;
            }
        }

        /// <summary>Pipe OD (mm) for the nominal size — same for BW and SW run modeling.</summary>
        public static double RunDiameterMm(double dnMm) =>
            PipeSizeCatalog.OdSch40Mm(dnMm);

        public static double ElbowCenterToFaceMm(double dnMm, ConnectionStyle style)
        {
            int dn = (int)Math.Round(dnMm);
            if (style is ConnectionStyle.SocketWeld or ConnectionStyle.Threaded)
                return SwCenterToOuterSocketMm(dn);

            if (BwLr90CenterToFaceMm.TryGetValue(dn, out double bw))
                return bw;

            PipeSizeOption? size = PipeSizeCatalog.FindByDn(dnMm);
            if (size != null && BwLr90CenterToFaceMm.TryGetValue(size.DnMm, out bw))
                return bw;

            return dnMm * 1.5;
        }

        /// <summary>Center to outer socket face for routing — A + J per B16.11.</summary>
        public static double SwCenterToOuterSocketMm(int dn)
        {
            if (SwCenterToSocketBottomMm.TryGetValue(dn, out double a)
                && SwSocketDepthMm.TryGetValue(dn, out double j))
                return a + j;

            PipeSizeOption? size = PipeSizeCatalog.FindByDn(dn);
            if (size != null)
                return SwCenterToOuterSocketMm(size.DnMm);

            return dn;
        }

        public static ConnectionStyle InferConnectionStyle(ValveProject project)
        {
            if (project.Ports.Count > 0)
            {
                bool anySw = project.Ports.Any(p => p.Type == PortConnectionType.SW);
                bool anyBv = project.Ports.Any(p => p.Type == PortConnectionType.BV);
                bool anyThread = project.Ports.Any(p =>
                    p.Type is PortConnectionType.THDM or PortConnectionType.THDF);

                if (anySw && !anyBv)
                    return ConnectionStyle.SocketWeld;
                if (anyThread && !anyBv && !anySw)
                    return ConnectionStyle.Threaded;
                if (anyBv)
                    return ConnectionStyle.ButtWeld;
            }

            return ConnectionStyle.ButtWeld;
        }
    }
}
