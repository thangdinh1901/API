using System;
using System.Globalization;
using System.Linq;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer.Services
{
    internal static class PortService
    {
        public static ConnectionPort Add(ValveProject project, Guid? parentNodeId = null)
        {
            var port = new ConnectionPort
            {
                Number = project.NextPortNumber(),
                Type = PortConnectionType.FL,
                ParentNodeId = parentNodeId,
            };

            if (parentNodeId.HasValue)
            {
                port.Position = new[] { 0.0, 0.0, 0.0 };
                port.Direction = new[] { 1.0, 0.0, 0.0 };
            }

            project.Ports.Add(port);
            return port;
        }

        public static ConnectionPort CreateFromPick(
            ValveProject project,
            Guid? parentNodeId,
            double centerX,
            double centerY,
            double centerZ,
            double normalX,
            double normalY,
            double normalZ,
            PortConnectionType type = PortConnectionType.FL)
        {
            ConnectionPort port = Add(project, parentNodeId);
            port.Type = type;
            PortTransformMath.SetWorldPosition(project, port, centerX, centerY, centerZ);
            PortTransformMath.SetWorldDirection(project, port, normalX, normalY, normalZ);
            return port;
        }

        public static ConnectionPort Copy(ValveProject project, Guid portId)
        {
            ConnectionPort source = project.FindPort(portId)
                ?? throw new InvalidOperationException("Port not found.");

            ConnectionPort clone = SceneGraphHelpers.ClonePort(source);
            clone.Number = project.NextPortNumber();
            project.Ports.Add(clone);
            return clone;
        }

        public static void Delete(ValveProject project, Guid portId) =>
            project.Ports.RemoveAll(p => p.Id == portId);

        public static void Apply(
            ValveProject project,
            ConnectionPort port,
            int number,
            PortConnectionType type,
            Guid? parentNodeId,
            double posX,
            double posY,
            double posZ,
            double dirX,
            double dirY,
            double dirZ)
        {
            port.Number = number;
            port.Type = type;

            if (parentNodeId != port.ParentNodeId)
                PortTransformMath.RebindToParent(project, port, parentNodeId);
            else
                port.ParentNodeId = parentNodeId;

            PortTransformMath.SetWorldPosition(project, port, posX, posY, posZ);
            PortTransformMath.SetWorldDirection(project, port, dirX, dirY, dirZ);
        }

        public static string ParentDisplayName(ValveProject project, ConnectionPort port)
        {
            if (!port.ParentNodeId.HasValue)
                return "(world)";

            PrimitiveNode? parent = project.FindNode(port.ParentNodeId.Value);
            return parent?.Name ?? port.ParentNodeId.Value.ToString();
        }

        public static string FormatCoord(double value) =>
            value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
