using System;
using System.Collections.Generic;
using System.Linq;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer.Services
{
    /// <summary>
    /// Default run diameter (D) and LR 90° elbow center-to-face (R) from catalog DN.
    /// D is always the connected <b>pipe OD</b> (B36.10 Sch-40) — including socket weld:
    /// the pipe inserts into the socket; primitive Elbow diameter models the pipe run, not forging OD.
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

        public static void SyncProjectDimensions(ValveProject project)
        {
            if (!UsesPipeRunDimensions(project.CatalogGroup))
                return;

            if (project.Parameters.DN <= 0)
                return;

            int dn = (int)Math.Round(project.Parameters.DN);
            ConnectionStyle style = InferConnectionStyle(project);
            project.Parameters.BodyOD = RunDiameterMm(dn);
            project.Parameters.ElbowCenterToFace = ElbowCenterToFaceMm(dn, style);
            RefreshElbowPrimitiveParameters(project);
        }

        private static void RefreshElbowPrimitiveParameters(ValveProject project)
        {
            SkeletonParameters p = project.Parameters;
            foreach (PrimitiveNode node in project.Parts)
            {
                if (node.Kind != SceneNodeKind.Primitive)
                    continue;

                if (node.Type is not (PrimitiveType.ELBOW or PrimitiveType.SEGMENTED_ELBOW or PrimitiveType.REDUCED_ELBOW))
                    continue;

                SetNodeParam(node, "D", p.BodyOD, "pipe OD from DN");
                SetNodeParam(node, "R", p.ElbowCenterToFace, "LR 90 center-to-face");
            }
        }

        private static void SetNodeParam(PrimitiveNode node, string key, double value, string expression)
        {
            if (!node.Parameters.TryGetValue(key, out ParamValue? param))
            {
                param = new ParamValue();
                node.Parameters[key] = param;
            }

            param.Value = value;
            param.Expression = expression;
        }

        public static void EnsureRunDimensions(ValveProject project)
        {
            if (!UsesPipeRunDimensions(project.CatalogGroup))
                return;

            SyncProjectDimensions(project);
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
