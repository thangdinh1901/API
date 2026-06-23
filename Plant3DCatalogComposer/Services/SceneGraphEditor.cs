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
            SceneParamBindingService.SanitizeManualParameterOverrides(project);
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
            foreach (KeyValuePair<string, ParamValue> kv in node.Parameters)
            {
                ParamValue pv = kv.Value;
                if (string.IsNullOrWhiteSpace(pv.Expression))
                    continue;

                if (SceneParamBindingService.HasManualOverride(pv, skeleton))
                {
                    pv.Expression = null;
                    continue;
                }

                string expression = pv.Expression.Trim();
                double resolved = ExpressionEvaluator.EvaluateOrValue(
                    expression, skeleton, pv.Value);

                // Keep a positive manual override when the bound expression collapses to zero
                // (e.g. HandwheelOD unset while the user typed D/T in the Scene grid).
                if (node.Kind == SceneNodeKind.Primitive
                    && PrimitiveParameterUnits.RequiresPositiveValue(node.Type, kv.Key)
                    && resolved <= 0
                    && pv.Value > 0)
                {
                    pv.Expression = null;
                    continue;
                }

                // Optional params default to literal "0" (Pyramid HT, Cone D2, etc.) — do not
                // clobber a user-entered value when the stored literal expression still says 0.
                if (ExpressionEvaluator.IsNumericLiteral(expression)
                    && Math.Abs(resolved - pv.Value) > 1e-9)
                {
                    pv.Expression = null;
                    continue;
                }

                pv.Value = resolved;
            }
        }
    }
}
