using System;
using System.Drawing;
using System.Windows.Forms;
using Autodesk.AutoCAD.Windows;
using Plant3DCatalogComposer.Services;

namespace Plant3DCatalogComposer
{
    internal static class PaletteManager
    {
        private static readonly Guid PaletteId = new("f7e3b2a1-4c5d-6e7f-8091-a2b3c4d5e6f7");
        private static PaletteSet? _paletteSet;
        private static ComposerForm? _form;

        public static void Initialize()
        {
            if (_paletteSet != null)
                return;

            _paletteSet = new PaletteSet(
                $"P3D Catalog Composer ({PluginVersion.BuildStamp})",
                "P3DCatalogComposer",
                PaletteId)
            {
                Style = PaletteSetStyles.ShowPropertiesMenu
                    | PaletteSetStyles.ShowAutoHideButton
                    | PaletteSetStyles.ShowCloseButton,
                MinimumSize = new Size(320, 400),
                Size = new Size(340, 720),
                DockEnabled = DockSides.Left | DockSides.Right,
            };

            _form = new ComposerForm
            {
                Dock = DockStyle.Fill,
            };

            _paletteSet.Add("Composer", _form);
        }

        public static void Show()
        {
            Initialize();
            _paletteSet!.Visible = true;
            _paletteSet.Activate(0);
            _form?.RefreshFromDocument();
        }

        public static void NotifyPortCreatedFromPick(Guid portId) =>
            _form?.OnPortCreatedFromPick(portId);

        public static void NotifyDisplacementPicked(DisplacementPickResult result, DistancePickTarget target) =>
            _form?.OnDisplacementPicked(result, target);

        public static void NotifyDimensionPicked(DimensionPickResult result) =>
            _form?.OnDimensionPicked(result);

        public static void Terminate()
        {
            _form?.Dispose();
            _form = null;
            _paletteSet?.Dispose();
            _paletteSet = null;
        }
    }
}
