using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;

[assembly: ExtensionApplication(typeof(Plant3DLineVisibility.Plugin))]
[assembly: CommandClass(typeof(Plant3DLineVisibility.Commands))]

namespace Plant3DLineVisibility
{
    /// <summary>
    /// Plugin entry point — loaded automatically by AutoCAD via ApplicationPlugins bundle.
    /// </summary>
    public class Plugin : IExtensionApplication
    {
        public void Initialize()
        {
            var dm = Application.DocumentManager;
            dm.DocumentActivated += OnDocumentActivated;
            dm.MdiActiveDocument?.Editor.WriteMessage(
                "\nPlant 3D Line Visibility Manager loaded. Command: P3DLINEVIS");
            RibbonService.Initialize();
        }

        public void Terminate()
        {
            var dm = Application.DocumentManager;
            dm.DocumentActivated -= OnDocumentActivated;
            PaletteManager.Terminate();
        }

        private static void OnDocumentActivated(
            object? sender,
            DocumentCollectionEventArgs e)
        {
            try
            {
                PaletteManager.NotifyDocumentActivated();
            }
            catch
            {
                // Non-fatal on drawing switch
            }
        }
    }
}
