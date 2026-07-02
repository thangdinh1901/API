using System.Drawing;
using Autodesk.AutoCAD.ApplicationServices;

namespace Plant3DCatalogComposer
{
    /// <summary>
    /// Palette colours that follow the AutoCAD UI theme (COLORTHEME sysvar:
    /// 0 = dark, 1 = light). Read once when the form is styled; the palette
    /// re-styles on show so a theme switch is picked up next time it opens.
    /// </summary>
    internal static class ComposerTheme
    {
        public static bool IsDark { get; private set; } = true;

        public static void Refresh()
        {
            try
            {
                object? v = Application.GetSystemVariable("COLORTHEME");
                // 0 = Dark, 1 = Light (AutoCAD 2026).
                IsDark = v == null || System.Convert.ToInt32(v) == 0;
            }
            catch
            {
                IsDark = true;
            }
        }

        // AutoCAD 2026 dark palette (approx. of the ribbon/palette greys).
        private static readonly Color DarkWindow = Color.FromArgb(56, 56, 56);    // panel/tab background
        private static readonly Color DarkGroup = Color.FromArgb(64, 64, 64);     // group boxes
        private static readonly Color DarkField = Color.FromArgb(45, 45, 45);     // textbox/combo field
        private static readonly Color DarkText = Color.FromArgb(220, 220, 220);   // primary text
        private static readonly Color DarkSubtle = Color.FromArgb(150, 150, 150); // secondary text
        private static readonly Color DarkBorder = Color.FromArgb(82, 82, 82);
        private static readonly Color DarkAccent = Color.FromArgb(0, 122, 204);   // selection/highlight

        private static readonly Color LightWindow = Color.FromArgb(240, 240, 240);
        private static readonly Color LightGroup = Color.FromArgb(240, 240, 240);
        private static readonly Color LightField = Color.White;
        private static readonly Color LightText = Color.FromArgb(30, 30, 30);
        private static readonly Color LightSubtle = Color.FromArgb(110, 110, 110);
        private static readonly Color LightBorder = Color.FromArgb(180, 180, 180);
        private static readonly Color LightAccent = Color.FromArgb(0, 120, 215);

        public static Color Window => IsDark ? DarkWindow : LightWindow;
        public static Color GroupBackground => IsDark ? DarkGroup : LightGroup;
        public static Color Field => IsDark ? DarkField : LightField;
        public static Color Text => IsDark ? DarkText : LightText;
        public static Color SubtleText => IsDark ? DarkSubtle : LightSubtle;
        public static Color Border => IsDark ? DarkBorder : LightBorder;
        public static Color Accent => IsDark ? DarkAccent : LightAccent;
        public static Color AccentText => Color.White;
    }
}
