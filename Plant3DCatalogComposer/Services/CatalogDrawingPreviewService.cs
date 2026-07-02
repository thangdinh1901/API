using Autodesk.AutoCAD.ApplicationServices;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer.Services
{
    /// <summary>Refresh Plant 3D geometry on the active DWG after export/deploy.</summary>
    internal static class CatalogDrawingPreviewService
    {
        /// <summary>Scene tab preview via wrapper → scene_builder (colored preview).</summary>
        public static void RebuildScenePreview(Document? doc, string? dwgPath, ValveProject project)
        {
            if (doc == null || project.Parts.Count == 0)
                return;

            IdleRebuildService.CancelPending();
            SceneRebuildService.RebuildInModalCommand(doc, dwgPath, project);
        }
    }
}
