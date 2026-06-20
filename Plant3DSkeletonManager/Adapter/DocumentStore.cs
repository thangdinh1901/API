using System;
using Inventor;
using Plant3DSkeletonManager.Core;

namespace Plant3DSkeletonManager.Adapter
{
    /// <summary>
    /// Persists the scene graph JSON inside the assembly document (AttributeSets),
    /// so the model travels with the .iam file.
    /// </summary>
    public static class DocumentStore
    {
        private const string SetName = "P3D_SceneGraph";
        private const string JsonAttr = "Json";

        public static ValveProject LoadOrCreate(AssemblyDocument doc)
        {
            string? json = ReadJson(doc);
            if (json != null)
            {
                ValveProject? project = JsonCodec.Deserialize(json);
                if (project != null)
                    return project;
            }

            return new ValveProject
            {
                ValveName = System.IO.Path.GetFileNameWithoutExtension(doc.DisplayName),
            };
        }

        public static void Save(AssemblyDocument doc, ValveProject project)
        {
            string json = JsonCodec.Serialize(project);

            AttributeSet? set = FindSet(doc);
            set ??= doc.AttributeSets.Add(SetName);

            foreach (Inventor.Attribute attr in set)
            {
                if (string.Equals(attr.Name, JsonAttr, StringComparison.OrdinalIgnoreCase))
                {
                    attr.Value = json;
                    return;
                }
            }
            set.Add(JsonAttr, ValueTypeEnum.kStringType, json);
        }

        private static string? ReadJson(AssemblyDocument doc)
        {
            AttributeSet? set = FindSet(doc);
            if (set == null)
                return null;

            foreach (Inventor.Attribute attr in set)
            {
                if (string.Equals(attr.Name, JsonAttr, StringComparison.OrdinalIgnoreCase))
                    return attr.Value as string;
            }
            return null;
        }

        private static AttributeSet? FindSet(AssemblyDocument doc)
        {
            foreach (AttributeSet s in doc.AttributeSets)
            {
                if (string.Equals(s.Name, SetName, StringComparison.OrdinalIgnoreCase))
                    return s;
            }
            return null;
        }
    }
}
