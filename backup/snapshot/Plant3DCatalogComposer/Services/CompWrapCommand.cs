using System.Threading;
using Autodesk.AutoCAD.ApplicationServices;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer.Services
{
    internal static class CompWrapCommand
    {
        private static int _sequence;

        public static void RunRebuild(Document doc, ValveProject project)
        {
            int seq = Interlocked.Increment(ref _sequence);
            // Always route through wrapper → hot_reload → scene_builder so origin/rotation apply.
            string mode = seq > 1 ? "erase+wait+wrapper+scene_builder" : "wrapper+scene_builder";

            doc.Editor.WriteMessage($"\nP3D Composer: rebuild #{seq} — {mode} ...");
            doc.SendStringToExecute(
                WrapperScript.BuildRunRebuild(seq, project), true, false, false);
        }
    }
}
