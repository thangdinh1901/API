using System.IO;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer.Services
{
    /// <summary>Persists scene graph JSON and mirrors to Plant 3D CustomScripts.</summary>
    public static class DocumentStore
    {
        public static ValveProject LoadOrCreate(string? dwgPath, string displayName)
        {
            string path = ResolveSceneStorePathForSave(dwgPath);
            if (!File.Exists(path))
                TryMigrateLegacyScene(dwgPath, path);

            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                ValveProject? project = JsonCodec.Deserialize(json);
                if (project != null)
                    return project;
            }

            return new ValveProject
            {
                ValveName = string.IsNullOrEmpty(displayName)
                    ? "Untitled"
                    : Path.GetFileNameWithoutExtension(displayName),
            };
        }

        public static void Save(string? dwgPath, ValveProject project)
        {
            SceneParamBindingService.SanitizeManualParameterOverrides(project);
            string json = JsonCodec.Serialize(project);
            string path = ResolveSceneStorePathForSave(dwgPath);
            Directory.CreateDirectory(ProjectPaths.SceneStoreDirectory);
            File.WriteAllText(path, json);

            string? genDir = Path.GetDirectoryName(ProjectPaths.ActiveSceneJson);
            if (!string.IsNullOrEmpty(genDir))
            {
                Directory.CreateDirectory(genDir);
                File.WriteAllText(ProjectPaths.ActiveSceneJson, json);
            }

            MirrorToCustomScripts(json, path);
            ComposerLiveScriptService.Write(project);
        }

        public static void MirrorToCustomScripts(string json, string? canonicalScenePath = null)
        {
            string dest = ProjectPaths.CustomScriptsActiveSceneJson;
            string? destDir = Path.GetDirectoryName(dest);
            if (string.IsNullOrEmpty(destDir) || !Directory.Exists(destDir))
                return;

            Directory.CreateDirectory(destDir);
            File.WriteAllText(dest, json);
            File.WriteAllText(ProjectPaths.CustomScriptsModeFlag, "1");

            if (!string.IsNullOrEmpty(canonicalScenePath))
                File.WriteAllText(ProjectPaths.CustomScriptsScenePointer, canonicalScenePath);
        }

        private static string ResolveSceneStorePathForSave(string? dwgPath) =>
            ProjectPaths.GetSceneStorePath(dwgPath);

        private static void TryMigrateLegacyScene(string? dwgPath, string keyedPath)
        {
            if (string.IsNullOrWhiteSpace(dwgPath))
                return;

            string full = Path.GetFullPath(dwgPath);
            if (!Path.IsPathRooted(full) || !File.Exists(full))
                return;

            string legacy = ProjectPaths.LegacySceneStorePath(full);
            if (!File.Exists(legacy) || File.Exists(keyedPath))
                return;

            Directory.CreateDirectory(ProjectPaths.SceneStoreDirectory);
            File.Copy(legacy, keyedPath, overwrite: false);
        }
    }
}
