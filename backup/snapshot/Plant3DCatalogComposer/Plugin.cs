using Autodesk.AutoCAD.Runtime;

[assembly: ExtensionApplication(typeof(Plant3DCatalogComposer.Plugin))]
[assembly: CommandClass(typeof(Plant3DCatalogComposer.Commands))]

namespace Plant3DCatalogComposer
{
    public class Plugin : IExtensionApplication
    {
        public void Initialize()
        {
            Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager
                .MdiActiveDocument?.Editor.WriteMessage(
                    $"\nP3D Catalog Composer loaded — {PluginVersion.StatusSuffix}. Command: P3DCOMPOSER");
            RibbonService.Initialize();
        }

        public void Terminate()
        {
            PaletteManager.Terminate();
        }
    }
}
