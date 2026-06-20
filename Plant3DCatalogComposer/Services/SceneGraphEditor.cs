using System;
using System.Collections.Generic;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer.Services
{
    public static class SceneGraphEditor
    {
        public static void Delete(string? dwgPath, ValveProject project, Guid nodeId)
        {
            HashSet<Guid> toRemove = SceneGraphHelpers.CollectSubtree(project, nodeId);
            project.Parts.RemoveAll(n => toRemove.Contains(n.Id));
            project.Ports.RemoveAll(p => p.ParentNodeId.HasValue && toRemove.Contains(p.ParentNodeId.Value));
            project.PruneOperations();
            project.PrunePorts();
            DocumentStore.Save(dwgPath, project);
        }

        public static Guid Duplicate(string? dwgPath, ValveProject project, Guid nodeId)
        {
            PrimitiveNode? source = project.FindNode(nodeId)
                ?? throw new InvalidOperationException("Node not found.");

            string prefix = SceneGraphHelpers.PrefixFromName(source.Name);
            string newName = project.NextName(prefix);
            PrimitiveNode clone = SceneGraphHelpers.CloneNode(source, newName);
            project.Parts.Add(clone);
            DocumentStore.Save(dwgPath, project);
            return clone.Id;
        }

        public static void Reparent(string? dwgPath, ValveProject project, Guid nodeId, Guid? newParentId)
        {
            if (!SceneGraphHelpers.CanReparent(project, nodeId, newParentId))
                throw new InvalidOperationException("Cannot reparent: would create a cycle.");

            PrimitiveNode? node = project.FindNode(nodeId)
                ?? throw new InvalidOperationException("Node not found.");

            if (newParentId.HasValue && project.FindNode(newParentId.Value) == null)
                throw new InvalidOperationException("Parent node not found.");

            node.Parent = newParentId;
            DocumentStore.Save(dwgPath, project);
        }

        public static void ResolveExpressions(PrimitiveNode node, SkeletonParameters skeleton)
        {
            foreach (ParamValue pv in node.Parameters.Values)
            {
                if (!string.IsNullOrWhiteSpace(pv.Expression))
                {
                    pv.Value = ExpressionEvaluator.EvaluateOrValue(
                        pv.Expression, skeleton, pv.Value);
                }
            }
        }
    }
}
