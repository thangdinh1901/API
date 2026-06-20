using System;
using System.Collections.Generic;
using Inventor;
using Plant3DSkeletonManager.Core;

namespace Plant3DSkeletonManager.Adapter
{
    /// <summary>
    /// Pulls the CURRENT state of all tagged occurrences into the scene graph.
    /// Final state only: transforms and dimensions are read as-is, no history.
    /// </summary>
    public static class SyncService
    {
        public sealed record SyncResult(int Updated, int Removed, int PrunedOperations);

        public static SyncResult Pull(AssemblyDocument asmDoc, ValveProject project)
        {
            var seen = new HashSet<Guid>();
            int updated = 0;

            foreach (ComponentOccurrence occ in asmDoc.ComponentDefinition.Occurrences)
            {
                Guid? id = OccurrenceTagger.GetNodeId(occ);
                if (id == null)
                    continue;

                PrimitiveNode? node = project.FindNode(id.Value);
                if (node == null)
                    continue;

                seen.Add(id.Value);

                node.Name = occ.Name;
                TransformConverter.ReadTransform(occ.Transformation, node);
                ReadNodeDimensions(occ, node);
                updated++;
            }

            // Occurrences deleted in Inventor disappear from the scene graph too
            int removed = project.Parts.RemoveAll(n => !seen.Contains(n.Id));
            int pruned = project.PruneOperations();

            return new SyncResult(updated, removed, pruned);
        }

        public static void ReadNodeDimensions(ComponentOccurrence occ, PrimitiveNode node)
        {
            PrimitiveDefinition? def = PrimitiveService.FindDefinition(node.Type);
            if (def == null || occ.Definition is not PartComponentDefinition partDef)
                return;

            foreach (var (logical, invName, _, _, unit) in def.Parameters)
            {
                try
                {
                    Parameter p = partDef.Parameters[invName];
                    double value = TransformConverter.ReadParamValue(p, unit);

                    if (!node.Parameters.TryGetValue(logical, out ParamValue? pv))
                        node.Parameters[logical] = pv = new ParamValue();
                    pv.Value = value;
                }
                catch
                {
                    // Parameter missing in a hand-edited part: keep the stored value
                }
            }
        }
    }
}
