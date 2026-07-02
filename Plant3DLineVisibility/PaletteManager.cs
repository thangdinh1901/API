using Autodesk.AutoCAD.Windows;

namespace Plant3DLineVisibility
{
    /// <summary>
    /// Manages the dockable PaletteSet that hosts the LineVisibilityForm.
    /// </summary>
    public static class PaletteManager
    {
        private static PaletteSet? _paletteSet;
        private static LineVisibilityForm? _form;

        private static readonly System.Guid PaletteGuid =
            new System.Guid("D7E4A3B1-9C2F-4D8E-B6A5-1F3C7E9D2B4A");

        /// <summary>Show the Line Visibility palette (create on first call).</summary>
        public static void Show()
        {
            if (_paletteSet == null)
            {
                _paletteSet = new PaletteSet(
                    "Line Visibility Manager",
                    "P3DLINEVIS",
                    PaletteGuid)
                {
                    Style = PaletteSetStyles.ShowPropertiesMenu
                          | PaletteSetStyles.ShowAutoHideButton
                          | PaletteSetStyles.ShowCloseButton,
                    MinimumSize = new System.Drawing.Size(280, 200),
                    DockEnabled = DockSides.Left | DockSides.Right,
                    Dock = DockSides.Right
                };

                _form = new LineVisibilityForm();
                _paletteSet.Add("Lines", _form);
                _paletteSet.KeepFocus = false;
            }

            _paletteSet.Visible = true;
        }

        /// <summary>Toggle palette visibility.</summary>
        public static void Toggle()
        {
            if (_paletteSet != null && _paletteSet.Visible)
                _paletteSet.Visible = false;
            else
                Show();
        }

        /// <summary>Notify the form that the active document changed.</summary>
        public static void NotifyDocumentActivated()
        {
            _form?.OnDocumentSwitched();
        }

        /// <summary>Cleanup on plugin unload.</summary>
        public static void Terminate()
        {
            _form?.Dispose();
            _paletteSet?.Dispose();
            _paletteSet = null;
            _form = null;
        }
    }
}
