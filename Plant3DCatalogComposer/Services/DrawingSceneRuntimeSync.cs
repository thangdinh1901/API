using System.IO;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer.Services
{
    /// <summary>
    /// Keep CustomScripts/.active_scene.json aligned with the active DWG's keyed scene store.
    /// Rebuild Scene reads .active_scene.json — without sync, switching drawings shows stale geometry.
    /// </summary>
    internal static class DrawingSceneRuntimeSync
    {
        public static void MirrorActiveDrawing(string? dwgPath)
        {
            if (string.IsNullOrWhiteSpace(dwgPath))
                return;

            string full = Path.GetFullPath(dwgPath);
            if (!Path.IsPathRooted(full))
                return;

            ValveProject project = DocumentStore.LoadOrCreate(
                full, Path.GetFileNameWithoutExtension(full));
            string json = JsonCodec.Serialize(project);
            DocumentStore.MirrorToCustomScripts(json, ProjectPaths.GetSceneStorePath(full));
        }
    }
}
