using System;

namespace Plant3DSkeletonManager.Core
{
    /// <summary>World/local transforms for connection ports attached to scene graph nodes.</summary>
    public static class PortTransformMath
    {
        public static double[] GetWorldPosition(ValveProject project, ConnectionPort port)
        {
            if (!port.ParentNodeId.HasValue)
                return (double[])port.Position.Clone();

            PrimitiveNode? parent = project.FindNode(port.ParentNodeId.Value);
            if (parent == null)
                return (double[])port.Position.Clone();

            return TransformPointByNodeChain(project, parent, port.Position);
        }

        public static double[] GetWorldDirection(ValveProject project, ConnectionPort port)
        {
            if (!port.ParentNodeId.HasValue)
                return Normalize((double[])port.Direction.Clone());

            PrimitiveNode? parent = project.FindNode(port.ParentNodeId.Value);
            if (parent == null)
                return Normalize((double[])port.Direction.Clone());

            return Normalize(TransformVectorByNodeChain(project, parent, port.Direction));
        }

        public static void SetWorldPosition(ValveProject project, ConnectionPort port, double x, double y, double z)
        {
            if (!port.ParentNodeId.HasValue)
            {
                port.Position = new[] { x, y, z };
                return;
            }

            PrimitiveNode? parent = project.FindNode(port.ParentNodeId.Value)
                ?? throw new InvalidOperationException("Port parent node not found.");

            port.Position = InverseTransformPointByNodeChain(project, parent, new[] { x, y, z });
        }

        public static void SetWorldDirection(ValveProject project, ConnectionPort port, double dx, double dy, double dz)
        {
            double[] worldDir = Normalize(new[] { dx, dy, dz });
            if (!port.ParentNodeId.HasValue)
            {
                port.Direction = worldDir;
                return;
            }

            PrimitiveNode? parent = project.FindNode(port.ParentNodeId.Value)
                ?? throw new InvalidOperationException("Port parent node not found.");

            port.Direction = InverseTransformVectorByNodeChain(project, parent, worldDir);
        }

        public static void TranslateWorld(ValveProject project, ConnectionPort port, double dx, double dy, double dz)
        {
            double[] world = GetWorldPosition(project, port);
            SetWorldPosition(project, port, world[0] + dx, world[1] + dy, world[2] + dz);
        }

        public static void RotateDirectionWorld(ValveProject project, ConnectionPort port, char axis, double degrees)
        {
            double[] worldDir = GetWorldDirection(project, port);
            double[] rotated = MultiplyMatrixVector(
                AxisRotationMatrix(axis, degrees * Math.PI / 180.0),
                worldDir);
            SetWorldDirection(project, port, rotated[0], rotated[1], rotated[2]);
        }

        public static void RotateDirectionLocal(ValveProject project, ConnectionPort port, char axis, double degrees)
        {
            double[] localDir = Normalize((double[])port.Direction.Clone());
            double[] rotated = MultiplyMatrixVector(
                AxisRotationMatrix(axis, degrees * Math.PI / 180.0),
                localDir);
            port.Direction = Normalize(rotated);
        }

        public static void RebindToParent(
            ValveProject project,
            ConnectionPort port,
            Guid? newParentId)
        {
            double[] worldPos = GetWorldPosition(project, port);
            double[] worldDir = GetWorldDirection(project, port);

            if (newParentId.HasValue && project.FindNode(newParentId.Value) == null)
                throw new InvalidOperationException("Parent node not found.");

            port.ParentNodeId = newParentId;

            SetWorldPosition(project, port, worldPos[0], worldPos[1], worldPos[2]);
            SetWorldDirection(project, port, worldDir[0], worldDir[1], worldDir[2]);
        }

        private static double[] TransformPointByNodeChain(
            ValveProject project,
            PrimitiveNode node,
            double[] localPoint)
        {
            double[] p = TransformPoint(node.Rotation, node.Origin, localPoint);
            if (!node.Parent.HasValue)
                return p;

            PrimitiveNode? parent = project.FindNode(node.Parent.Value);
            return parent == null ? p : TransformPointByNodeChain(project, parent, p);
        }

        private static double[] TransformVectorByNodeChain(
            ValveProject project,
            PrimitiveNode node,
            double[] localVector)
        {
            double[] v = TransformVector(node.Rotation, localVector);
            if (!node.Parent.HasValue)
                return v;

            PrimitiveNode? parent = project.FindNode(node.Parent.Value);
            return parent == null ? v : TransformVectorByNodeChain(project, parent, v);
        }

        private static double[] InverseTransformPointByNodeChain(
            ValveProject project,
            PrimitiveNode node,
            double[] worldPoint)
        {
            if (node.Parent.HasValue)
            {
                PrimitiveNode? parent = project.FindNode(node.Parent.Value);
                if (parent != null)
                    worldPoint = InverseTransformPointByNodeChain(project, parent, worldPoint);
            }

            return InverseTransformPoint(node.Rotation, node.Origin, worldPoint);
        }

        private static double[] InverseTransformVectorByNodeChain(
            ValveProject project,
            PrimitiveNode node,
            double[] worldVector)
        {
            if (node.Parent.HasValue)
            {
                PrimitiveNode? parent = project.FindNode(node.Parent.Value);
                if (parent != null)
                    worldVector = InverseTransformVectorByNodeChain(project, parent, worldVector);
            }

            return InverseTransformVector(node.Rotation, worldVector);
        }

        private static double[] TransformPoint(double[] rot, double[] origin, double[] pt) =>
            new[]
            {
                rot[0] * pt[0] + rot[1] * pt[1] + rot[2] * pt[2] + origin[0],
                rot[3] * pt[0] + rot[4] * pt[1] + rot[5] * pt[2] + origin[1],
                rot[6] * pt[0] + rot[7] * pt[1] + rot[8] * pt[2] + origin[2],
            };

        private static double[] TransformVector(double[] rot, double[] vec) =>
            new[]
            {
                rot[0] * vec[0] + rot[1] * vec[1] + rot[2] * vec[2],
                rot[3] * vec[0] + rot[4] * vec[1] + rot[5] * vec[2],
                rot[6] * vec[0] + rot[7] * vec[1] + rot[8] * vec[2],
            };

        private static double[] InverseTransformPoint(double[] rot, double[] origin, double[] worldPt)
        {
            double[] local = new[]
            {
                worldPt[0] - origin[0],
                worldPt[1] - origin[1],
                worldPt[2] - origin[2],
            };
            return InverseTransformVector(Transpose3x3(rot), local);
        }

        private static double[] InverseTransformVector(double[] invRot, double[] worldVec) =>
            MultiplyMatrixVector(invRot, worldVec);

        private static double[] MultiplyMatrixVector(double[] m, double[] v) =>
            new[]
            {
                m[0] * v[0] + m[1] * v[1] + m[2] * v[2],
                m[3] * v[0] + m[4] * v[1] + m[5] * v[2],
                m[6] * v[0] + m[7] * v[1] + m[8] * v[2],
            };

        private static double[] Transpose3x3(double[] m) =>
            new[] { m[0], m[3], m[6], m[1], m[4], m[7], m[2], m[5], m[8] };

        private static double[] Multiply3x3(double[] a, double[] b)
        {
            var c = new double[9];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    double sum = 0;
                    for (int k = 0; k < 3; k++)
                        sum += a[i * 3 + k] * b[k * 3 + j];
                    c[i * 3 + j] = sum;
                }
            }

            return c;
        }

        private static double[] AxisRotationMatrix(char axis, double rad)
        {
            double c = Math.Cos(rad);
            double s = Math.Sin(rad);

            return axis switch
            {
                'X' or 'x' => new[] { 1, 0, 0, 0, c, -s, 0, s, c },
                'Y' or 'y' => new[] { c, 0, s, 0, 1, 0, -s, 0, c },
                'Z' or 'z' => new[] { c, -s, 0, s, c, 0, 0, 0, 1 },
                _ => throw new ArgumentException($"Unknown axis '{axis}'."),
            };
        }

        private static double[] Normalize(double[] v)
        {
            double len = Math.Sqrt(v[0] * v[0] + v[1] * v[1] + v[2] * v[2]);
            if (len < 1e-12)
                return new[] { 1.0, 0.0, 0.0 };

            return new[] { v[0] / len, v[1] / len, v[2] / len };
        }
    }
}
