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
    }
}
