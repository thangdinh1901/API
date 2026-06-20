using System;
using System.IO;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer.Services
{
    public static class SceneImportExport
    {
        public static ValveProject LoadJsonFile(string path)
        {
            string json = File.ReadAllText(path);
            ValveProject? project = JsonCodec.Deserialize(json)
                ?? throw new InvalidOperationException("Invalid scene JSON file.");
            project.PruneOperations();
            return project;
        }

        public static void SaveJsonFile(string path, ValveProject project) =>
            File.WriteAllText(path, JsonCodec.Serialize(project));

        public static void ReplaceProject(string? dwgPath, ValveProject project) =>
            DocumentStore.Save(dwgPath, project);
    }
}
