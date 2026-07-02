using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Plant3DLineVisibility.Models;
using Plant3DLineVisibility.Services;

namespace Plant3DLineVisibility
{
    public static class Commands
    {
        /// <summary>Open (or toggle) the Line Visibility palette.</summary>
        [CommandMethod("P3DLINEVIS", CommandFlags.Session)]
        public static void ShowLineVisibility()
        {
            try
            {
                RibbonService.Initialize();
                PaletteManager.Show();
                WriteStatus("Plant 3D Line Visibility Manager opened.");
            }
            catch (System.Exception ex)
            {
                WriteStatus("P3DLINEVIS error: " + ex.Message);
            }
        }

        /// <summary>Show all piping objects that were hidden by this plugin.</summary>
        [CommandMethod("P3DLINESHOWALL", CommandFlags.Session)]
        public static void ShowAllLines()
        {
            Document? doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            try
            {
                WriteStatus("Scanning drawing…");
                List<LineGroupInfo> groups = LineVisibilityService.ScanDrawing(doc);
                if (groups.Count == 0)
                {
                    WriteStatus("P3DLINESHOWALL: no piping line groups found.");
                    return;
                }

                LineVisibilityService.ShowAll(doc, groups);
                int totalParts = groups.Sum(g => g.ComponentCount);
                WriteStatus($"P3DLINESHOWALL: {totalParts} component(s) in {groups.Count} line(s) — all visible.");
            }
            catch (System.Exception ex)
            {
                WriteStatus("P3DLINESHOWALL error: " + ex.Message);
            }
        }

        /// <summary>Hide piping objects for a specific line number (command-line).</summary>
        [CommandMethod("P3DLINEHIDE", CommandFlags.Session)]
        public static void HideLineNumber()
        {
            Document? doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            try
            {
                PromptStringOptions opts = new PromptStringOptions("\nEnter Line Number to hide: ")
                {
                    AllowSpaces = true
                };
                PromptResult pr = doc.Editor.GetString(opts);
                if (pr.Status != PromptStatus.OK || string.IsNullOrWhiteSpace(pr.StringResult))
                    return;

                string lineTag = pr.StringResult.Trim();
                List<LineGroupInfo> groups = LineVisibilityService.ScanDrawing(doc);
                LineGroupInfo? target = groups.FirstOrDefault(
                    g => g.LineNumberTag.Equals(lineTag, StringComparison.OrdinalIgnoreCase));

                if (target == null)
                {
                    WriteStatus($"P3DLINEHIDE: line \"{lineTag}\" not found.");
                    return;
                }

                LineVisibilityService.SetVisibility(doc, target.ObjectIds, false);
                target.IsVisible = false;
                WriteStatus($"P3DLINEHIDE: hidden {target.ComponentCount} component(s) on \"{lineTag}\".");
            }
            catch (System.Exception ex)
            {
                WriteStatus("P3DLINEHIDE error: " + ex.Message);
            }
        }

        /// <summary>Show piping objects for a specific line number (command-line).</summary>
        [CommandMethod("P3DLINESHOW", CommandFlags.Session)]
        public static void ShowLineNumber()
        {
            Document? doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            try
            {
                PromptStringOptions opts = new PromptStringOptions("\nEnter Line Number to show: ")
                {
                    AllowSpaces = true
                };
                PromptResult pr = doc.Editor.GetString(opts);
                if (pr.Status != PromptStatus.OK || string.IsNullOrWhiteSpace(pr.StringResult))
                    return;

                string lineTag = pr.StringResult.Trim();
                List<LineGroupInfo> groups = LineVisibilityService.ScanDrawing(doc);
                LineGroupInfo? target = groups.FirstOrDefault(
                    g => g.LineNumberTag.Equals(lineTag, StringComparison.OrdinalIgnoreCase));

                if (target == null)
                {
                    WriteStatus($"P3DLINESHOW: line \"{lineTag}\" not found.");
                    return;
                }

                LineVisibilityService.SetVisibility(doc, target.ObjectIds, true);
                target.IsVisible = true;
                WriteStatus($"P3DLINESHOW: restored {target.ComponentCount} component(s) on \"{lineTag}\".");
            }
            catch (System.Exception ex)
            {
                WriteStatus("P3DLINESHOW error: " + ex.Message);
            }
        }

        /// <summary>Isolate a single line — show only it, hide everything else.</summary>
        [CommandMethod("P3DLINEISOLATE", CommandFlags.Session)]
        public static void IsolateLineNumber()
        {
            Document? doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            try
            {
                PromptStringOptions opts = new PromptStringOptions("\nEnter Line Number to isolate: ")
                {
                    AllowSpaces = true
                };
                PromptResult pr = doc.Editor.GetString(opts);
                if (pr.Status != PromptStatus.OK || string.IsNullOrWhiteSpace(pr.StringResult))
                    return;

                string lineTag = pr.StringResult.Trim();
                List<LineGroupInfo> groups = LineVisibilityService.ScanDrawing(doc);
                LineGroupInfo? target = groups.FirstOrDefault(
                    g => g.LineNumberTag.Equals(lineTag, StringComparison.OrdinalIgnoreCase));

                if (target == null)
                {
                    WriteStatus($"P3DLINEISOLATE: line \"{lineTag}\" not found.");
                    return;
                }

                LineVisibilityService.IsolateLine(doc, groups, lineTag);
                WriteStatus($"P3DLINEISOLATE: isolated \"{lineTag}\" ({target.ComponentCount} component(s)).");
            }
            catch (System.Exception ex)
            {
                WriteStatus("P3DLINEISOLATE error: " + ex.Message);
            }
        }

        private static void WriteStatus(string message)
        {
            Document? doc = Application.DocumentManager.MdiActiveDocument;
            doc?.Editor.WriteMessage("\n" + message);
        }
    }
}
