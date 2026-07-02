using System;
using System.IO;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;

namespace Plant3DCatalogComposer.Services
{
    internal sealed class CatalogScriptRegistrationResult
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
        public int PycFileCount { get; init; }
    }

    /// <summary>
    /// Queues PLANTREGISTERCUSTOMSCRIPTS so Plant 3D compiles .py→.pyc and refreshes symbols
    /// for spec insert — without a CAD restart or a manual command.
    ///
    /// Fire-and-forget by design: SendStringToExecute only runs the queued LISP AFTER the
    /// current .NET call returns to AutoCAD's command loop, so blocking the UI thread to
    /// "wait" for it deadlocks — the command can't start until we return, yet the old code
    /// spun a 90s DoEvents/Sleep loop waiting for a command that never began (that was the
    /// Composer freeze). We queue it, let the LISP block internally (CMDACTIVE loop) and print
    /// its own completion sentinel, and use a one-shot Application.Idle handler to report the
    /// .pyc count once registration lands — none of it blocks the UI.
    /// </summary>
    internal static class CatalogScriptRegistrationService
    {
        private static readonly TimeSpan ReportTimeout = TimeSpan.FromSeconds(90);

        public static CatalogScriptRegistrationResult QueueRegister(Document? doc)
        {
            if (doc == null)
            {
                return new CatalogScriptRegistrationResult
                {
                    Success = false,
                    Message = "No active drawing — open a drawing before Deploy/Publish.",
                };
            }

            if (!Directory.Exists(ProjectPaths.CustomScriptsDir))
            {
                return new CatalogScriptRegistrationResult
                {
                    Success = false,
                    Message = $"CustomScripts not found: {ProjectPaths.CustomScriptsDir}",
                };
            }

            try
            {
                doc.SendStringToExecute(
                    WrapperScript.BuildRegisterCustomScripts(),
                    activate: true,
                    wrapUpInactiveDoc: false,
                    echoCommand: false);

                AttachIdleReport(doc);

                return new CatalogScriptRegistrationResult
                {
                    Success = true,
                    Message =
                        "Registering scripts with Plant 3D in the background — "
                        + "ready for spec insert shortly "
                        + "(watch for \"PLANTREGISTERCUSTOMSCRIPTS finished\" on the command line).",
                };
            }
            catch (Exception ex)
            {
                return new CatalogScriptRegistrationResult
                {
                    Success = false,
                    Message = "Script registration failed: " + ex.Message,
                };
            }
        }

        /// <summary>
        /// Non-blocking follow-up: once the queued register command lands (AutoCAD returns to
        /// idle and .pyc appear) print the count to the command line, then self-detach. Also
        /// self-detaches on timeout so the handler never lingers.
        /// </summary>
        private static void AttachIdleReport(Document doc)
        {
            DateTime start = DateTime.UtcNow;
            int baseline = CountPycFiles();
            EventHandler? handler = null;

            handler = (_, _) =>
            {
                try
                {
                    // Idle fires only while AutoCAD is quiescent (never mid-command), so the
                    // signal that the queued command actually ran is the .pyc count changing.
                    int pyc = CountPycFiles();
                    bool progressed = pyc > baseline;
                    bool timedOut = DateTime.UtcNow - start > ReportTimeout;

                    if (!progressed && !timedOut)
                        return;

                    Application.Idle -= handler;

                    if (pyc > 0)
                    {
                        doc.Editor.WriteMessage(
                            $"\nP3D Composer: scripts registered ({pyc} .pyc) — ready for spec insert.");
                    }
                    else if (timedOut)
                    {
                        doc.Editor.WriteMessage(
                            "\nP3D Composer: registration is taking longer than expected — "
                            + "check the command line for PLANTREGISTERCUSTOMSCRIPTS errors.");
                    }
                }
                catch
                {
                    // Never let a background report break the editor's idle loop.
                    Application.Idle -= handler;
                }
            };

            Application.Idle += handler;
        }

        private static int CountPycFiles()
        {
            if (!Directory.Exists(ProjectPaths.CustomScriptsDir))
                return 0;

            return Directory.EnumerateFiles(ProjectPaths.CustomScriptsDir, "*.pyc", SearchOption.AllDirectories)
                .Count();
        }
    }
}
