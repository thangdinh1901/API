using System;
using System.Runtime.InteropServices;
using Inventor;

namespace BoxExtrudeAddIn
{
    /// <summary>
    /// Entry point Add-in Inventor. Đăng ký nút Ribbon để mở form tạo box.
    /// </summary>
    [Guid("b2c3d4e5-f6a7-8901-bcde-f12345678901")]
    [ComVisible(true)]
    public class StandardAddInServer : ApplicationAddInServer
    {
        private Application? _inventorApp;
        private ButtonDefinition? _buttonDef;

        public void Activate(ApplicationAddInSite addInSiteObject, bool firstTime)
        {
            _inventorApp = addInSiteObject.Application;

            if (firstTime)
                CreateRibbonButton(addInSiteObject);
        }

        public void Deactivate()
        {
            if (_buttonDef != null)
            {
                _buttonDef.OnExecute -= OnButtonExecute;
                _buttonDef = null;
            }

            _inventorApp = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        public void ExecuteCommand(int commandID) { }

        public object? Automation => null;

        private void CreateRibbonButton(ApplicationAddInSite addInSiteObject)
        {
            const string clientId = "b2c3d4e5-f6a7-8901-bcde-f12345678901";

            try
            {
                _buttonDef = _inventorApp!.CommandManager.ControlDefinitions.AddButtonDefinition(
                    "Primitives",
                    "BoxExtrudeCmd",
                    CommandTypesEnum.kShapeEditCmdType,
                    clientId,
                    "Mở form nhập kích thước và extrude khối box",
                    "Tạo khối box bằng Extrude",
                    Type.Missing,
                    Type.Missing,
                    ButtonDisplayEnum.kDisplayTextInLearningMode);

                _buttonDef.OnExecute += OnButtonExecute;

                Ribbon ribbon = _inventorApp.UserInterfaceManager.Ribbons["Part"];
                RibbonTab tab = ribbon.RibbonTabs["id_TabTools"];
                RibbonPanel panel = tab.RibbonPanels.Add(
                    "Box API",
                    "BoxExtrudePanel",
                    clientId,
                    "",
                    false);

                panel.CommandControls.AddButton(_buttonDef, true);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(
                    "Không thể tạo nút Ribbon: " + ex.Message,
                    "BoxExtrudeAddIn",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Warning);
            }
        }

        private void OnButtonExecute(NameValueMap context)
        {
            if (_inventorApp == null)
                return;

            using var form = new BoxForm(_inventorApp);
            form.ShowDialog();
        }
    }
}
