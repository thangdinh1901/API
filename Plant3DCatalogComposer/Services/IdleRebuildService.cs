using Autodesk.AutoCAD.ApplicationServices;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer.Services
{
    /// <summary>Coalesces panel rebuilds; runs rebuild Lisp once CAD is idle.</summary>
    internal static class IdleRebuildService
    {
        private static bool _pending;
        private static bool _hooked;
        private static Document? _doc;
        private static ValveProject? _pendingProject;

        public static void CancelPending()
        {
            _pending = false;
            _pendingProject = null;
            _doc = null;
            if (!_hooked)
                return;

            Application.Idle -= OnIdle;
            _hooked = false;
        }

        public static void Request(Document doc, ValveProject project)
        {
            _doc = doc;
            _pendingProject = project;
            _pending = true;
            if (_hooked)
                return;

            Application.Idle += OnIdle;
            _hooked = true;
        }

        private static void OnIdle(object? sender, System.EventArgs e)
        {
            if (!_pending)
                return;

            Document? doc = _doc ?? Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
                return;

            if (!doc.Editor.IsQuiescent || !string.IsNullOrEmpty(doc.CommandInProgress))
                return;

            _pending = false;
            Application.Idle -= OnIdle;
            _hooked = false;

            doc.Editor.WriteMessage("\nP3D Composer: CAD idle — rebuild ...");
            ValveProject? project = _pendingProject ?? RebuildDnResolver.LoadActiveScene();
            _pendingProject = null;
            if (project == null || project.Parts.Count == 0)
            {
                doc.Editor.WriteMessage("\nP3D Composer: no scene parts to rebuild.");
                return;
            }

            CompWrapCommand.RunRebuild(doc, project);
        }
    }
}
