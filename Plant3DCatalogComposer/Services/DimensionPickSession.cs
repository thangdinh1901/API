using System;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer.Services
{
    public enum DimensionMeasureMode
    {
        Distance,
        DeltaX,
        DeltaY,
        DeltaZ,
    }

    public readonly struct DimensionPickResult
    {
        public string DimensionName { get; init; }
        public double ValueMm { get; init; }
        public DimensionBinding Binding { get; init; }
    }

    /// <summary>Carries Dimensions tab state into modal pick command.</summary>
    internal static class DimensionPickSession
    {
        public static string? PendingDimensionName { get; private set; }
        public static DimensionMeasureMode MeasureMode { get; private set; } = DimensionMeasureMode.Distance;
        public static Guid? BindSceneNodeId { get; private set; }
        public static string? BindSceneNodeName { get; private set; }
        public static string? BindParamKey { get; private set; }

        public static event Action<DimensionPickResult>? Completed;

        public static void Begin(
            string dimensionName,
            DimensionMeasureMode mode,
            Guid? sceneNodeId = null,
            string? sceneNodeName = null,
            string? paramKey = null)
        {
            PendingDimensionName = dimensionName;
            MeasureMode = mode;
            BindSceneNodeId = sceneNodeId;
            BindSceneNodeName = sceneNodeName;
            BindParamKey = string.IsNullOrWhiteSpace(paramKey) ? null : paramKey.Trim();
        }

        public static void Complete(DimensionPickResult result)
        {
            PendingDimensionName = null;
            Completed?.Invoke(result);
        }

        public static void Cancel() => PendingDimensionName = null;
    }
}
