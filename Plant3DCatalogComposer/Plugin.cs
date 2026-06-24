using Autodesk.AutoCAD.Runtime;
using Plant3DCatalogComposer.Services;

[assembly: ExtensionApplication(typeof(Plant3DCatalogComposer.Plugin))]
[assembly: CommandClass(typeof(Plant3DCatalogComposer.Commands))]

namespace Plant3DCatalogComposer
{
    public class Plugin : IExtensionApplication
    {
        public void Initialize()
        {
            var dm = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager;
            dm.DocumentActivated += OnDocumentActivated;
            dm.MdiActiveDocument?.Editor.WriteMessage(
                $"\nP3D Catalog Composer loaded — {PluginVersion.StatusSuffix}. Command: P3DCOMPOSER");
            RibbonService.Initialize();
            System.Threading.Tasks.Task.Run(CatalogExcelTemplateService.WarmTemplateCache);
        }

        public void Terminate()
        {
            var dm = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager;
            dm.DocumentActivated -= OnDocumentActivated;
            PaletteManager.Terminate();
        }

        private static void OnDocumentActivated(object? sender, Autodesk.AutoCAD.ApplicationServices.DocumentCollectionEventArgs e)
        {
            try
            {
                IdleRebuildService.CancelPending();
                string? path = e.Document?.Name;
                if (string.IsNullOrWhiteSpace(path) || path.StartsWith("*", System.StringComparison.Ordinal))
                    return;

                DrawingSceneRuntimeSync.MirrorActiveDrawing(path);
                PaletteManager.NotifyDocumentActivated();
            }
            catch
            {
                // non-fatal on drawing switch
            }
        }
    }
}
