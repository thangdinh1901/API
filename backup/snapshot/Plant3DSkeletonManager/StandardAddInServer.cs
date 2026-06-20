using System;
using System.Runtime.InteropServices;
using Inventor;

namespace Plant3DSkeletonManager
{
    /// <summary>
    /// Add-in entry point. Hosts the "Plant3D Skeleton Manager" dockable window
    /// and a ribbon button to show it.
    /// </summary>
    [Guid(ClientIdValue)]
    [ComVisible(true)]
    public class StandardAddInServer : ApplicationAddInServer
    {
        private const string ClientIdValue = "9d2e6b1c-4f8a-4d37-b5a9-c3e7f2148a60";
        private const string ClientId = "{" + ClientIdValue + "}";
        private const string WindowInternalName = "Plant3DSkeletonManager.DockableWindow";

        private Inventor.Application? _app;
        private ButtonDefinition? _buttonDef;
        private DockableWindow? _dockableWindow;
        private SkeletonForm? _form;

        public void Activate(ApplicationAddInSite addInSiteObject, bool firstTime)
        {
            _app = addInSiteObject.Application;

            CreateDockableWindow();
            CreateRibbonButton();
        }

        public void Deactivate()
        {
            if (_buttonDef != null)
            {
                _buttonDef.OnExecute -= OnButtonExecute;
                _buttonDef = null;
            }

            _form?.Dispose();
            _form = null;
            _dockableWindow = null;
            _app = null;

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        public void ExecuteCommand(int commandID) { }

        public object? Automation => null;

        private void CreateDockableWindow()
        {
            UserInterfaceManager uiMgr = _app!.UserInterfaceManager;

            // The window definition persists between sessions; reuse it if it exists
            foreach (DockableWindow w in uiMgr.DockableWindows)
            {
                if (string.Equals(w.InternalName, WindowInternalName, StringComparison.OrdinalIgnoreCase))
                {
                    _dockableWindow = w;
                    break;
                }
            }

            _dockableWindow ??= uiMgr.DockableWindows.Add(
                ClientId, WindowInternalName, "Plant3D Skeleton Manager");

            _form = new SkeletonForm(_app);
            _dockableWindow.AddChild(_form.Handle);
            _form.Show();

            _dockableWindow.ShowVisibilityCheckBox = true;
            _dockableWindow.Visible = false;
        }

        private void CreateRibbonButton()
        {
            _buttonDef = _app!.CommandManager.ControlDefinitions.AddButtonDefinition(
                "Skeleton\nManager",
                "Plant3DSkeletonManager.ShowWindowCmd",
                CommandTypesEnum.kNonShapeEditCmdType,
                ClientId,
                "Show the Plant3D Skeleton Manager window",
                "Plant3D Skeleton Manager",
                Type.Missing,
                Type.Missing,
                ButtonDisplayEnum.kDisplayTextInLearningMode);

            _buttonDef.OnExecute += OnButtonExecute;

            // The target document type is an assembly, so add the button there
            AddButtonToRibbon("Assembly");
        }

        private void AddButtonToRibbon(string ribbonName)
        {
            try
            {
                Ribbon ribbon = _app!.UserInterfaceManager.Ribbons[ribbonName];
                RibbonTab tab = ribbon.RibbonTabs["id_TabTools"];
                RibbonPanel panel = tab.RibbonPanels.Add(
                    "Plant3D", "Plant3DSkeletonManager.Panel." + ribbonName, ClientId, "", false);
                panel.CommandControls.AddButton(_buttonDef, true);
            }
            catch
            {
                // Panel may already exist from a previous load in the same session
            }
        }

        private void OnButtonExecute(NameValueMap context)
        {
            if (_dockableWindow != null)
                _dockableWindow.Visible = !_dockableWindow.Visible;
        }
    }
}
