using System.IO;
using Autodesk.AutoCAD.ApplicationServices;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer.Services
{
    internal static class SceneRebuildService
    {
        public static void RequestFromPanel(Document doc, string? dwgPath, ValveProject project)
        {
            DocumentStore.Save(dwgPath, project);
            LogSavedPaths(doc, dwgPath, project.Parts.Count);

            if (project.Parts.Count == 0)
            {
                doc.Editor.WriteMessage(
                    "\nP3D Composer: no parts in scene — insert a primitive first.");
                return;
            }

            IdleRebuildService.Request(doc, project);
        }

        public static void RebuildInModalCommand(Document doc, string? dwgPath, ValveProject project)
        {
            DocumentStore.Save(dwgPath, project);
            LogSavedPaths(doc, dwgPath, project.Parts.Count);

            if (project.Parts.Count == 0)
            {
                doc.Editor.WriteMessage("\nP3D Composer: scene empty — nothing to build.");
                return;
            }

            CompWrapCommand.RunRebuild(doc, project);
        }

        private static void LogSavedPaths(Document doc, string? dwgPath, int partCount)
        {
            string scenePath = ProjectPaths.GetSceneStorePath(dwgPath);
            string livePy = Path.Combine(ProjectPaths.CustomScriptsLibDir, ComposerLiveScriptService.LiveFileName);
            doc.Editor.WriteMessage($"\nP3D Composer: scene saved — {partCount} part(s).");
            doc.Editor.WriteMessage($"\n  JSON: {scenePath}");
            doc.Editor.WriteMessage($"\n  Python: {livePy}");
        }
    }
}
