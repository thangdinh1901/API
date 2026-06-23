using System.Globalization;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer.Services
{
    internal static class WrapperScript
    {
        /// <summary>
        /// One Lisp progn: optional ERASE ALL (seq&gt;1), wait until CMDACTIVE=0, then testacpscript.
        /// </summary>
        public static string BuildRunRebuild(int sequence, ValveProject project)
        {
            string eraseBlock = BuildEraseBlock(sequence);
            string invoke = BuildWrapperInvoke(sequence, RebuildDnResolver.Resolve(project));
            return WrapProgn(eraseBlock, invoke);
        }

        /// <summary>Scene graph via wrapper → hot_reload → scene_builder (always applies transforms).</summary>
        private static string BuildWrapperInvoke(int sequence, double nominalDn)
        {
            string dnText = nominalDn.ToString(CultureInfo.InvariantCulture);
            return $"(testacpscript \"wrapper\" \"D\" \"{dnText}\" \"K\" \"{sequence}\")";
        }

        private static string BuildEraseBlock(int sequence) =>
            sequence > 1
                ? "(setq _p3d_cmdecho (getvar \"CMDECHO\")) (setvar \"CMDECHO\" 0) " +
                  "(command \"_.ERASE\" \"ALL\" \"\") " +
                  "(while (> (getvar \"CMDACTIVE\") 0) (progn)) " +
                  "(setvar \"CMDECHO\" _p3d_cmdecho) "
                : "";

        private static string WrapProgn(string eraseBlock, string testacpscriptInvoke) =>
            "(progn (vl-load-com) " +
            "(if (not (member \"PnP3dACPAdapter\" (arx))) (arxload \"PnP3dACPAdapter\")) " +
            eraseBlock +
            testacpscriptInvoke + " " +
            "(while (> (getvar \"CMDACTIVE\") 0) (progn)) " +
            "(princ \"\\nP3D Composer: rebuild finished.\")) ";

        public static string WrapTestCatalogInvoke(
            int sequence,
            string testacpscriptInvoke,
            bool registerScripts = false,
            bool erasePreviewGeometry = true)
        {
            string registerBlock = registerScripts
                ? "(setq _p3d_cmdecho (getvar \"CMDECHO\")) (setvar \"CMDECHO\" 0) " +
                  "(command-s \"_.PLANTREGISTERCUSTOMSCRIPTS\") " +
                  "(while (> (getvar \"CMDACTIVE\") 0) (progn)) " +
                  "(setvar \"CMDECHO\" _p3d_cmdecho) "
                : "";

            // Erase preview bodies before catalog test; vl-catch-all keeps "Function cancelled"
            // from aborting the progn before testacpscript runs.
            string eraseBlock = erasePreviewGeometry
                ? "(setq _p3d_cmdecho (getvar \"CMDECHO\")) (setvar \"CMDECHO\" 0) " +
                  "(vl-catch-all-apply 'command-s (list \"_.ERASE\" \"ALL\" \"\")) " +
                  "(while (> (getvar \"CMDACTIVE\") 0) (progn)) " +
                  "(setvar \"CMDECHO\" _p3d_cmdecho) "
                : "";

            _ = sequence;
            return WrapProgn(eraseBlock, registerBlock + testacpscriptInvoke);
        }
    }
}
