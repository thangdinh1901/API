using System;
using System.Linq;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer.Services
{
    internal static class DimensionMeasureService
    {
        public static double ValueFromPick(DisplacementPickResult pick, DimensionMeasureMode mode) =>
            mode switch
            {
                DimensionMeasureMode.DeltaX => Math.Abs(pick.Dx),
                DimensionMeasureMode.DeltaY => Math.Abs(pick.Dy),
                DimensionMeasureMode.DeltaZ => Math.Abs(pick.Dz),
                _ => pick.Distance,
            };

        public static string MeasureKindToken(DimensionMeasureMode mode) => mode switch
        {
            DimensionMeasureMode.DeltaX => "pickDeltaX",
            DimensionMeasureMode.DeltaY => "pickDeltaY",
            DimensionMeasureMode.DeltaZ => "pickDeltaZ",
            _ => "pickDistance",
        };

        public static DimensionBinding CreatePickBinding(
            DisplacementPickResult pick,
            DimensionMeasureMode mode,
            Guid? sceneNodeId = null,
            string? sceneNodeName = null,
            string? paramKey = null)
        {
            return new DimensionBinding
            {
                MeasureKind = MeasureKindToken(mode),
                SceneNodeId = sceneNodeId,
                SceneNodeName = sceneNodeName,
                ParamKey = string.IsNullOrWhiteSpace(paramKey) ? null : paramKey.Trim(),
                PickFromWcs = new[] { pick.FromX, pick.FromY, pick.FromZ },
                PickToWcs = new[] { pick.ToX, pick.ToY, pick.ToZ },
            };
        }

        public static bool TryMeasurePortDistance(
            ValveProject project,
            int fromPortNumber,
            int toPortNumber,
            out double valueMm,
            out DimensionBinding binding,
            out string? error)
        {
            valueMm = 0;
            binding = new DimensionBinding();
            error = null;

            ConnectionPort? from = project.Ports.FirstOrDefault(p => p.Number == fromPortNumber);
            ConnectionPort? to = project.Ports.FirstOrDefault(p => p.Number == toPortNumber);
            if (from == null || to == null)
            {
                error = $"Port {fromPortNumber} or {toPortNumber} not found.";
                return false;
            }

            if (fromPortNumber == toPortNumber)
            {
                error = "Select two different ports.";
                return false;
            }

            double[] w1 = PortTransformMath.GetWorldPosition(project, from);
            double[] w2 = PortTransformMath.GetWorldPosition(project, to);
            double dx = w2[0] - w1[0];
            double dy = w2[1] - w1[1];
            double dz = w2[2] - w1[2];
            valueMm = Math.Sqrt(dx * dx + dy * dy + dz * dz);
            if (valueMm < 0.1)
            {
                error = "Ports are too close together.";
                return false;
            }

            binding = new DimensionBinding
            {
                MeasureKind = "portDistance",
                FromPort = fromPortNumber,
                ToPort = toPortNumber,
                PickFromWcs = w1,
                PickToWcs = w2,
            };
            return true;
        }

        public static DimensionBinding CreateSceneBind(
            Guid sceneNodeId,
            string sceneNodeName,
            string? paramKey = null) =>
            new()
            {
                MeasureKind = "sceneBind",
                SceneNodeId = sceneNodeId,
                SceneNodeName = sceneNodeName,
                ParamKey = string.IsNullOrWhiteSpace(paramKey) ? null : paramKey.Trim(),
            };
    }
}
