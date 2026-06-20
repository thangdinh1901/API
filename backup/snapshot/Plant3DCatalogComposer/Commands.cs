using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using Plant3DCatalogComposer.Services;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer
{
    public static class Commands
    {
        [CommandMethod("P3DCOMPOSER", CommandFlags.Session)]
        public static void ShowComposer()
        {
            try
            {
                RibbonService.Initialize();
                WriteStatus(
                    "Plant 3D Catalog Composer — form → scene JSON → wrapper → scene_builder.");
                PaletteManager.Show();
            }
            catch (System.Exception ex)
            {
                WriteStatus("P3DCOMPOSER error: " + ex.Message);
                System.Windows.Forms.MessageBox.Show(
                    ex.Message,
                    "Plant 3D Catalog Composer",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error);
            }
        }

        /// <summary>Manual rebuild (same Lisp path as form auto-rebuild).</summary>
        [CommandMethod("P3DCOMPWRAP", CommandFlags.Modal)]
        public static void CompWrap()
        {
            Document? doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
                return;

            try
            {
                string dwgPath = DrawingContext.GetActiveDrawingPath() ?? "unsaved";
                ValveProject project = DocumentStore.LoadOrCreate(
                    dwgPath,
                    System.IO.Path.GetFileNameWithoutExtension(dwgPath));
                if (project.Parts.Count == 0)
                {
                    WriteStatus("P3DCOMPWRAP: scene empty.");
                    return;
                }

                CompWrapCommand.RunRebuild(doc, project);
            }
            catch (System.Exception ex)
            {
                WriteStatus("P3DCOMPWRAP error: " + ex.Message);
            }
        }

        [CommandMethod("P3DREBUILD", CommandFlags.Modal)]
        public static void RebuildGeometry()
        {
            try
            {
                string dwgPath = DrawingContext.RequireActiveDrawingPath();
                ValveProject project = DocumentStore.LoadOrCreate(
                    dwgPath,
                    System.IO.Path.GetFileNameWithoutExtension(dwgPath));

                Document? doc = Application.DocumentManager.MdiActiveDocument;
                if (doc != null)
                    SceneRebuildService.RebuildInModalCommand(doc, dwgPath, project);

                WriteStatus($"P3DREBUILD: {project.Parts.Count} part(s) queued.");
            }
            catch (System.Exception ex)
            {
                WriteStatus("P3DREBUILD error: " + ex.Message);
                System.Windows.Forms.MessageBox.Show(
                    ex.Message,
                    "Plant 3D Catalog Composer",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error);
            }
        }

        private static void WriteStatus(string message)
        {
            Document? doc = Application.DocumentManager.MdiActiveDocument;
            doc?.Editor.WriteMessage("\n" + message);
        }

        /// <summary>Modal point pick — creates a port from WCS position + direction.</summary>
        [CommandMethod("P3DCOMPPICKPOINT", CommandFlags.Modal)]
        public static void PickPointForPort()
        {
            Document? doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
                return;

            try
            {
                if (!PortPointPickService.TryPickPoint(doc, out PointPickResult? pick) || pick == null)
                {
                    PortPointPickSession.Cancel();
                    return;
                }

                string dwgPath = DrawingContext.RequireActiveDrawingPath();
                ValveProject project = DocumentStore.LoadOrCreate(
                    dwgPath,
                    System.IO.Path.GetFileNameWithoutExtension(dwgPath));

                ConnectionPort port = PortService.CreateFromPick(
                    project,
                    PortPointPickSession.PendingParentNodeId,
                    pick.Position.X,
                    pick.Position.Y,
                    pick.Position.Z,
                    pick.Direction.X,
                    pick.Direction.Y,
                    pick.Direction.Z);

                DocumentStore.Save(dwgPath, project);
                PortVisualService.Refresh(doc, project, port.Id);
                PortPointPickSession.Complete(port.Id);

                doc.Editor.WriteMessage(
                    $"\nP3D Composer: {PortConnectionTypeHelper.PortLabel(port)} ({PortConnectionTypeHelper.ToEndTypeCode(port.Type)}) — "
                    + $"({PortService.FormatCoord(pick.Position.X)}, "
                    + $"{PortService.FormatCoord(pick.Position.Y)}, {PortService.FormatCoord(pick.Position.Z)}) mm, "
                    + $"dir ({PortService.FormatCoord(pick.Direction.X)}, {PortService.FormatCoord(pick.Direction.Y)}, "
                    + $"{PortService.FormatCoord(pick.Direction.Z)}).");
            }
            catch (System.Exception ex)
            {
                PortPointPickSession.Cancel();
                WriteStatus("P3DCOMPPICKPOINT error: " + ex.Message);
            }
        }

        /// <summary>Modal two-point pick — sets Position step from |ΔX|, |ΔY|, |ΔZ| or moves part (Align).</summary>
        [CommandMethod("P3DCOMPPICKDIST", CommandFlags.Modal)]
        public static void PickDistanceForStep()
        {
            Document? doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
                return;

            DistancePickTarget target = DistancePickSession.PendingTarget ?? DistancePickTarget.PositionStep;

            try
            {
                if (!DistancePickService.TryPickDisplacement(doc, out DisplacementPickResult pick))
                {
                    DistancePickSession.Cancel();
                    return;
                }

                DistancePickSession.Complete(pick, target);
                doc.Editor.WriteMessage(
                    $"\nP3D Composer: Δ=({pick.Dx:0.###}, {pick.Dy:0.###}, {pick.Dz:0.###}) mm, |Δ|={pick.Distance:0.###} mm.");
            }
            catch (System.Exception ex)
            {
                DistancePickSession.Cancel();
                WriteStatus("P3DCOMPPICKDIST error: " + ex.Message);
            }
        }

        /// <summary>Save API repo snapshot to backup/snapshot (requires deploy.json).</summary>
        [CommandMethod("P3DCOMPBACKUP", CommandFlags.Session)]
        public static void SaveProjectBackup()
        {
            (bool ok, string message) = ProjectBackupService.SaveSnapshot("from-plant3d");
            WriteStatus(ok ? "P3DCOMPBACKUP: " + message : "P3DCOMPBACKUP failed: " + message);
            if (!ok)
            {
                System.Windows.Forms.MessageBox.Show(
                    message,
                    "Save Backup",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Warning);
            }
        }

        /// <summary>Restore API repo from backup/snapshot.</summary>
        [CommandMethod("P3DCOMPRESTORE", CommandFlags.Session)]
        public static void RestoreProjectBackup()
        {
            if (System.Windows.Forms.MessageBox.Show(
                    "Restore the last saved backup over the dev repo?\n\n"
                    + "Rebuild the plugin and Deploy Catalog after restore.",
                    "Restore Backup",
                    System.Windows.Forms.MessageBoxButtons.YesNo,
                    System.Windows.Forms.MessageBoxIcon.Warning) != System.Windows.Forms.DialogResult.Yes)
            {
                return;
            }

            (bool ok, string message) = ProjectBackupService.RestoreSnapshot();
            WriteStatus(ok ? "P3DCOMPRESTORE: " + message : "P3DCOMPRESTORE failed: " + message);
            if (!ok)
            {
                System.Windows.Forms.MessageBox.Show(
                    message,
                    "Restore Backup",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error);
            }
        }

        /// <summary>Generate + deploy + register catalog for active drawing scene.</summary>
        [CommandMethod("P3DCOMPPUBLISH", CommandFlags.Session)]
        public static void PublishCatalog()
        {
            try
            {
                string dwg = DrawingContext.RequireActiveDrawingPath();
                ValveProject project = DocumentStore.LoadOrCreate(
                    dwg,
                    System.IO.Path.GetFileNameWithoutExtension(dwg));

                ValidationResult preflight = CatalogPreflightService.ValidateForPublish(project);
                if (!preflight.IsValid)
                {
                    WriteStatus("P3DCOMPPUBLISH blocked: " + string.Join("; ", preflight.Errors));
                    return;
                }

                Document? doc = Application.DocumentManager.MdiActiveDocument;
                CatalogPublishResult result = CatalogPublishService.Publish(
                    dwg,
                    project,
                    dwgPath =>
                    {
                        string? devParts = ProjectPaths.TryResolveDevPartsDir();
                        if (devParts != null)
                            return devParts;
                        throw new System.InvalidOperationException(
                            "deploy.json not configured — use Publish Catalog from the palette.");
                    },
                    doc,
                    allowExportWithWarnings: true);

                WriteStatus(result.Success ? "P3DCOMPPUBLISH: " + result.Message : "P3DCOMPPUBLISH: " + result.Message);
            }
            catch (System.Exception ex)
            {
                WriteStatus("P3DCOMPPUBLISH error: " + ex.Message);
            }
        }

        /// <summary>Run testacpscript for the catalog part of the active scene.</summary>
        [CommandMethod("P3DCOMPTEST", CommandFlags.Session)]
        public static void TestCatalog()
        {
            try
            {
                string dwg = DrawingContext.RequireActiveDrawingPath();
                ValveProject project = DocumentStore.LoadOrCreate(
                    dwg,
                    System.IO.Path.GetFileNameWithoutExtension(dwg));
                CatalogTestResult test = CatalogTestService.BuildTestCommand(project);
                if (!test.CanRun)
                {
                    WriteStatus("P3DCOMPTEST: " + test.Message);
                    return;
                }

                Document? doc = Application.DocumentManager.MdiActiveDocument;
                if (doc != null && CatalogTestService.TryQueueTest(doc, project))
                    WriteStatus("P3DCOMPTEST: " + test.CommandLine);
            }
            catch (System.Exception ex)
            {
                WriteStatus("P3DCOMPTEST error: " + ex.Message);
            }
        }
    }
}
