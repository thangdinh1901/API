using System;
using Inventor;

namespace Plant3DSkeletonManager.Adapter
{
    public static class OccurrenceLookup
    {
        public static ComponentOccurrence? FindByNodeId(AssemblyDocument asmDoc, Guid nodeId)
        {
            foreach (ComponentOccurrence occ in asmDoc.ComponentDefinition.Occurrences)
            {
                Guid? id = OccurrenceTagger.GetNodeId(occ);
                if (id == nodeId)
                    return occ;
            }
            return null;
        }
    }
}
