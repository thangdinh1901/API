using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.Windows;

namespace Plant3DLineVisibility
{
    /// <summary>
    /// Creates the Ribbon tab and button for the Line Visibility Manager.
    /// </summary>
    public static class RibbonService
    {
        private const string TabId = "P3DLineVisTab";
        private const string TabTitle = "P3D Tools";
        private const string PanelId = "P3DLineVisPanel";

        private static bool _initialized;

        public static void Initialize()
        {
            if (_initialized) return;

            try
            {
                RibbonControl? ribbon = ComponentManager.Ribbon;
                if (ribbon == null)
                {
                    // Ribbon not ready yet — defer to Idle event
                    Application.Idle += OnIdle;
                    return;
                }

                CreateRibbonTab(ribbon);
                _initialized = true;
            }
            catch
            {
                // Ribbon may not be available (command-line AutoCAD)
            }
        }

        private static void OnIdle(object? sender, EventArgs e)
        {
            Application.Idle -= OnIdle;

            try
            {
                RibbonControl? ribbon = ComponentManager.Ribbon;
                if (ribbon != null)
                {
                    CreateRibbonTab(ribbon);
                    _initialized = true;
                }
            }
            catch { }
        }

        private static void CreateRibbonTab(RibbonControl ribbon)
        {
            // Avoid duplicates if tab already exists
            foreach (RibbonTab existing in ribbon.Tabs)
            {
                if (existing.Id == TabId)
                    return;
            }

            // --- Tab ---
            var tab = new RibbonTab
            {
                Title = TabTitle,
                Id = TabId,
                IsActive = false
            };

            // --- Panel: Line Visibility ---
            var panelSource = new RibbonPanelSource
            {
                Title = "Line Visibility",
                Id = PanelId
            };

            // Button: Open Line Visibility Manager
            var btnOpen = new RibbonButton
            {
                Text = "Line Visibility\nManager",
                ShowText = true,
                Size = RibbonItemSize.Large,
                Orientation = System.Windows.Controls.Orientation.Vertical,
                CommandHandler = new RibbonCommandHandler("P3DLINEVIS"),
                Id = "P3DLineVisBtn"
            };
            panelSource.Items.Add(btnOpen);

            // Button: Show All
            var btnShowAll = new RibbonButton
            {
                Text = "Show All",
                ShowText = true,
                Size = RibbonItemSize.Standard,
                CommandHandler = new RibbonCommandHandler("P3DLINESHOWALL"),
                Id = "P3DLineShowAllBtn"
            };
            panelSource.Items.Add(btnShowAll);

            var panel = new RibbonPanel();
            panel.Source = panelSource;

            tab.Panels.Add(panel);
            ribbon.Tabs.Add(tab);
        }
    }

    /// <summary>
    /// Simple ICommand implementation that sends a string command to AutoCAD.
    /// </summary>
    internal class RibbonCommandHandler : System.Windows.Input.ICommand
    {
        private readonly string _command;

        public RibbonCommandHandler(string command) => _command = command;

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter)
        {
            Document? doc = Application.DocumentManager.MdiActiveDocument;
            doc?.SendStringToExecute(_command + "\n", true, false, false);
        }
    }
}
