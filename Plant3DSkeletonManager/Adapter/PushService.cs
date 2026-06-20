using System;
using Inventor;
using Plant3DSkeletonManager.Core;

namespace Plant3DSkeletonManager.Adapter
{
    /// <summary>
    /// Writes scene graph node state to Inventor occurrences (transform, name, dimensions).
    /// </summary>
    public static class PushService
    {
        public static void PushNode(
            Inventor.Application app,
            AssemblyDocument asmDoc,
            ValveProject project,
            Guid nodeId)
        {
            PrimitiveNode? node = project.FindNode(nodeId);
            if (node == null)
                throw new InvalidOperationException("Node not found in scene graph.");

            ComponentOccurrence? occ = OccurrenceLookup.FindByNodeId(asmDoc, nodeId);
            if (occ == null)
                throw new InvalidOperationException(
                    $"No Inventor occurrence found for '{node.Name}'. Try rebuilding or re-insert.");

            Transaction tx = app.TransactionManager.StartTransaction(
                (_Document)asmDoc, $"Update {node.Name}");
            try
            {
                occ.Name = node.Name;
                occ.Transformation = TransformConverter.CreateMatrix(app.TransientGeometry, node);
                PushDimensions(occ, node);
                tx.End();
            }
            catch
            {
                tx.Abort();
                throw;
            }

            asmDoc.Update();
        }

        /// <summary>Re-evaluates parameter expressions from skeleton, then pushes all nodes.</summary>
        public static int ResolveAndPushAll(
            Inventor.Application app, AssemblyDocument asmDoc, ValveProject project)
        {
            foreach (PrimitiveNode node in project.Parts)
                ResolveExpressions(node, project.Parameters);

            int count = 0;
            foreach (PrimitiveNode node in project.Parts)
            {
                if (OccurrenceLookup.FindByNodeId(asmDoc, node.Id) != null)
                {
                    PushNode(app, asmDoc, project, node.Id);
                    count++;
                }
            }
            return count;
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

        private static void PushDimensions(ComponentOccurrence occ, PrimitiveNode node) =>
            PushDimensionsOnly(occ, node);

        public static void PushDimensionsOnly(ComponentOccurrence occ, PrimitiveNode node)
        {
            PrimitiveDefinition? def = PrimitiveService.FindDefinition(node.Type);
            if (def == null || occ.Definition is not PartComponentDefinition partDef)
                return;

            foreach (var (logical, invName, _, _, unit) in def.Parameters)
            {
                if (!node.Parameters.TryGetValue(logical, out ParamValue? pv))
                    continue;

                partDef.Parameters[invName].Expression =
                    TransformConverter.ParamExpression(pv.Value, unit);
            }
        }
    }
}
