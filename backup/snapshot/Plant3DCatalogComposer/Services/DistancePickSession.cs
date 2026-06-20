using System;

namespace Plant3DCatalogComposer.Services
{
    public enum DistancePickTarget
    {
        /// <summary>Set Position step per axis from |ΔX|, |ΔY|, |ΔZ| between two picks.</summary>
        PositionStep,
        /// <summary>Move selected part by WCS vector (to − from).</summary>
        PositionMove,
    }

    /// <summary>Carries Scene tab state into modal distance pick command.</summary>
    internal static class DistancePickSession
    {
        public static DistancePickTarget? PendingTarget { get; private set; }

        public static event Action<DisplacementPickResult, DistancePickTarget>? Completed;

        public static void Begin(DistancePickTarget target) => PendingTarget = target;

        public static void Complete(DisplacementPickResult result, DistancePickTarget target)
        {
            PendingTarget = null;
            Completed?.Invoke(result, target);
        }

        public static void Cancel() => PendingTarget = null;
    }
}
