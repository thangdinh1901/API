using System;
using System.IO;
using System.Reflection;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.Windows;

namespace Plant3DCatalogComposer
{
    /// <summary>Adds a ribbon tab with one-click access to P3DCOMPOSER.</summary>
    internal static class RibbonService
    {
        private const string TabId = "P3D_CATALOG_COMPOSER_TAB";
        private const string PanelId = "P3D_CATALOG_COMPOSER_PANEL";
        private const string OpenButtonId = "P3D_CATALOG_COMPOSER_OPEN";
        private const string RebuildButtonId = "P3D_CATALOG_COMPOSER_REBUILD";

        private static bool _created;
        private static bool _hooked;
        private static ImageSource? _composerLarge;
        private static ImageSource? _composerSmall;
        private static ImageSource? _rebuildLarge;
        private static ImageSource? _rebuildSmall;

        public static void Initialize()
        {
            if (_hooked)
            {
                TryCreateRibbon();
                return;
            }

            _hooked = true;
            TryCreateRibbon();
            ComponentManager.ItemInitialized += OnRibbonItemInitialized;
            Application.Idle += OnIdleOnce;
        }

        private static void OnRibbonItemInitialized(object? sender, RibbonItemEventArgs e)
        {
            TryCreateRibbon();
            if (_created)
                ComponentManager.ItemInitialized -= OnRibbonItemInitialized;
        }

        private static void OnIdleOnce(object? sender, EventArgs e)
        {
            TryCreateRibbon();
            if (_created)
                Application.Idle -= OnIdleOnce;
        }

        private static void TryCreateRibbon()
        {
            RibbonControl? ribbon = ComponentManager.Ribbon;
            if (ribbon == null)
                return;

            if (_created)
                return;

            RibbonTab? existing = FindTab(ribbon);
            if (existing != null)
                ribbon.Tabs.Remove(existing);

            _composerLarge = LoadIconFile("Composer32.png");
            _composerSmall = LoadIconFile("Composer16.png");
            _rebuildLarge = LoadIconFile("Rebuild32.png");
            _rebuildSmall = LoadIconFile("Rebuild16.png");

            var tab = new RibbonTab
            {
                Title = "P3D Composer",
                Id = TabId,
            };

            var panelSource = new RibbonPanelSource
            {
                Title = "Catalog",
                Id = PanelId,
            };

            panelSource.Items.Add(new RibbonButton
            {
                Id = OpenButtonId,
                Text = "Composer",
                ShowText = true,
                Size = RibbonItemSize.Large,
                Orientation = System.Windows.Controls.Orientation.Vertical,
                LargeImage = _composerLarge,
                Image = _composerSmall,
                CommandHandler = new RibbonCommandHandler("P3DCOMPOSER"),
            });

            panelSource.Items.Add(new RibbonButton
            {
                Id = RebuildButtonId,
                Text = "Rebuild",
                ShowText = true,
                Size = RibbonItemSize.Large,
                Orientation = System.Windows.Controls.Orientation.Vertical,
                LargeImage = _rebuildLarge,
                Image = _rebuildSmall,
                CommandHandler = new RibbonCommandHandler("P3DREBUILD"),
            });

            tab.Panels.Add(new RibbonPanel { Source = panelSource });
            ribbon.Tabs.Add(tab);
            _created = true;
            ComponentManager.ItemInitialized -= OnRibbonItemInitialized;
            Application.Idle -= OnIdleOnce;
        }

        private static ImageSource? LoadIconFile(string fileName)
        {
            string? dllDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (string.IsNullOrEmpty(dllDir))
                return null;

            string path = Path.Combine(dllDir, "Resources", fileName);
            if (!File.Exists(path))
                return null;

            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(path, UriKind.Absolute);
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit();
            image.Freeze();
            return image;
        }

        private static RibbonTab? FindTab(RibbonControl ribbon)
        {
            foreach (RibbonTab tab in ribbon.Tabs)
            {
                if (string.Equals(tab.Id, TabId, StringComparison.OrdinalIgnoreCase))
                    return tab;
            }

            return null;
        }

        private sealed class RibbonCommandHandler : ICommand
        {
            private readonly string _command;

            public RibbonCommandHandler(string command) => _command = command;

#pragma warning disable 67
            public event EventHandler? CanExecuteChanged;
#pragma warning restore 67

            public bool CanExecute(object? parameter) => true;

            public void Execute(object? parameter)
            {
                Document? doc = Application.DocumentManager.MdiActiveDocument;
                if (doc == null)
                    return;

                doc.SendStringToExecute(_command + " ", true, false, false);
            }
        }
    }
}
