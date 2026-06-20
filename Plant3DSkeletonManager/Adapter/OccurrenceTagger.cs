using System;
using Inventor;
using Plant3DSkeletonManager.Core;

namespace Plant3DSkeletonManager.Adapter
{
    /// <summary>Links Inventor occurrences to scene graph nodes via AttributeSets.</summary>
    public static class OccurrenceTagger
    {
        private const string SetName = "P3D_Primitive";
        private const string NodeIdAttr = "NodeId";
        private const string TypeAttr = "PrimitiveType";

        public static void Tag(ComponentOccurrence occ, Guid nodeId, PrimitiveType type)
        {
            AttributeSet set = GetOrAddSet(occ.AttributeSets);
            SetValue(set, NodeIdAttr, nodeId.ToString("D"));
            SetValue(set, TypeAttr, type.ToString());
        }

        public static Guid? GetNodeId(ComponentOccurrence occ)
        {
            foreach (AttributeSet set in occ.AttributeSets)
            {
                if (!string.Equals(set.Name, SetName, StringComparison.OrdinalIgnoreCase))
                    continue;

                foreach (Inventor.Attribute attr in set)
                {
                    if (string.Equals(attr.Name, NodeIdAttr, StringComparison.OrdinalIgnoreCase) &&
                        attr.Value is string s &&
                        Guid.TryParse(s, out Guid id))
                    {
                        return id;
                    }
                }
            }
            return null;
        }

        private static AttributeSet GetOrAddSet(AttributeSets sets)
        {
            foreach (AttributeSet s in sets)
            {
                if (string.Equals(s.Name, SetName, StringComparison.OrdinalIgnoreCase))
                    return s;
            }
            return sets.Add(SetName);
        }

        private static void SetValue(AttributeSet set, string name, string value)
        {
            foreach (Inventor.Attribute attr in set)
            {
                if (string.Equals(attr.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    attr.Value = value;
                    return;
                }
            }
            set.Add(name, ValueTypeEnum.kStringType, value);
        }
    }
}
