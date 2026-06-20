using System;
using System.Collections.Generic;
using System.Linq;

namespace Plant3DSkeletonManager.Core
{
    public static class SceneGraphHelpers
    {
        /// <summary>Deep-clones a node with a new Id and optional name override.</summary>
        public static PrimitiveNode CloneNode(PrimitiveNode source, string? newName = null)
        {
            var clone = new PrimitiveNode
            {
                Id = Guid.NewGuid(),
                Name = newName ?? source.Name,
                Kind = source.Kind,
                CatalogPartId = source.CatalogPartId,
                Type = source.Type,
                Origin = (double[])source.Origin.Clone(),
                Direction = (double[])source.Direction.Clone(),
                Rotation = (double[])source.Rotation.Clone(),
                RotationJogs = source.RotationJogs?.Select(j => new RotationJog
                {
                    World = j.World,
                    Axis = j.Axis,
                    Degrees = j.Degrees,
                }).ToList(),
                CatalogFrameRotation = source.CatalogFrameRotation != null
                    ? (double[])source.CatalogFrameRotation.Clone()
                    : null,
                Parent = source.Parent,
            };

            foreach (KeyValuePair<string, ParamValue> kv in source.Parameters)
            {
                clone.Parameters[kv.Key] = new ParamValue
                {
                    Value = kv.Value.Value,
                    Expression = kv.Value.Expression,
                };
            }

            return clone;
        }

        public static ConnectionPort ClonePort(ConnectionPort source) =>
            new()
            {
                Id = Guid.NewGuid(),
                Number = source.Number,
                Type = source.Type,
                ParentNodeId = source.ParentNodeId,
                Position = (double[])source.Position.Clone(),
                Direction = (double[])source.Direction.Clone(),
            };

        /// <summary>Collects a node and all descendants (for delete).</summary>
        public static HashSet<Guid> CollectSubtree(ValveProject project, Guid rootId)
        {
            var ids = new HashSet<Guid> { rootId };
            bool added;
            do
            {
                added = false;
                foreach (PrimitiveNode n in project.Parts)
                {
                    if (n.Parent.HasValue && ids.Contains(n.Parent.Value) && ids.Add(n.Id))
                        added = true;
                }
            } while (added);

            return ids;
        }

        /// <summary>Returns false if newParent would create a cycle.</summary>
        public static bool CanReparent(ValveProject project, Guid nodeId, Guid? newParentId)
        {
            if (newParentId == null || newParentId == nodeId)
                return newParentId != nodeId;

            var subtree = CollectSubtree(project, nodeId);
            return !subtree.Contains(newParentId.Value);
        }

        public static string PrefixFromName(string name)
        {
            int i = name.Length - 1;
            while (i >= 0 && char.IsDigit(name[i]))
                i--;
            return i >= 0 ? name.Substring(0, i + 1) : name;
        }

        public static IEnumerable<PrimitiveNode> ChildrenOf(ValveProject project, Guid? parentId) =>
            project.Parts.Where(p => p.Parent == parentId);

        /// <summary>Scene tree display order (depth-first, same as TreeView).</summary>
        public static List<Guid> GetDepthFirstOrder(ValveProject project)
        {
            var list = new List<Guid>();
            void Walk(PrimitiveNode node)
            {
                list.Add(node.Id);
                foreach (PrimitiveNode child in ChildrenOf(project, node.Id))
                    Walk(child);
            }

            foreach (PrimitiveNode root in ChildrenOf(project, null))
                Walk(root);

            return list;
        }

        /// <summary>
        /// After deleting nodeId, pick the next tree neighbor for continuous delete:
        /// prefer the node below in tree order, else the node above.
        /// </summary>
        public static Guid? GetSelectionAfterDelete(ValveProject project, Guid nodeId)
        {
            HashSet<Guid> subtree = CollectSubtree(project, nodeId);
            List<Guid> order = GetDepthFirstOrder(project);
            int index = order.IndexOf(nodeId);
            if (index < 0)
                return null;

            for (int j = index + 1; j < order.Count; j++)
            {
                if (!subtree.Contains(order[j]))
                    return order[j];
            }

            for (int j = index - 1; j >= 0; j--)
            {
                if (!subtree.Contains(order[j]))
                    return order[j];
            }

            return null;
        }
    }
}
