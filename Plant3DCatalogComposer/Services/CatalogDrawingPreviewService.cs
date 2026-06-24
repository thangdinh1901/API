using Autodesk.AutoCAD.ApplicationServices;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer.Services
{
    /// <summary>Refresh Plant 3D geometry on the active DWG after export/deploy.</summary>
    internal static class CatalogDrawingPreviewService
    {
        /// <summary>
        /// Erase preview solids and run deployed CUST_* on the current drawing
        /// (same as Test Catalog — no need to open a new DWG).
        /// </summary>
        public static bool TryQueueCatalogTest(Document? doc, string dwgPath)
        {
            if (doc == null)
                return false;

            CatalogTestResult test = CatalogTestService.BuildTestCommand(dwgPath);
            if (!test.CanRun)
            {
                doc.Editor.WriteMessage(
                    $"\nP3D Composer: could not refresh drawing — {test.Message.Replace("\n", " ")}");
                return false;
            }

            doc.Editor.WriteMessage(
                "\nP3D Composer: updating current drawing from deployed catalog (Test Catalog)...");
            return CatalogTestService.TryQueueTest(doc, dwgPath);
        }

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
