using System;

namespace Plant3DCatalogComposer.Services
{
    /// <summary>Carries Port Manager state into modal P3DCOMPPICKPOINT command.</summary>
    internal static class PortPointPickSession
    {
        public static Guid? PendingParentNodeId { get; private set; }
        public static Guid? LastCreatedPortId { get; private set; }

        public static event Action<Guid>? PortCreated;

        public static void Begin(Guid? parentNodeId)
        {
            PendingParentNodeId = parentNodeId;
            LastCreatedPortId = null;
        }

        public static void Complete(Guid portId)
        {
            LastCreatedPortId = portId;
            PendingParentNodeId = null;
            PortCreated?.Invoke(portId);
        }

        public static void Cancel() => PendingParentNodeId = null;
    }
}
