using System;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using Autodesk.AutoCAD.ApplicationServices;
using Plant3DCatalogComposer.Services;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer
{
    public partial class ComposerForm
    {
        private Guid? _selectedPortId;

        private void InitializePortManagerTab()
        {
            cmbPortType.Items.Clear();
            foreach ((PortConnectionType type, string _) in PortConnectionTypeHelper.CatalogEndTypes)
                cmbPortType.Items.Add(new PortTypeOption(type));

            lvPorts.Columns.Add("#", 28);
            lvPorts.Columns.Add("End type", 148);
            lvPorts.Columns.Add("Parent", 72);
            lvPorts.SelectedIndexChanged += (_, _) => OnPortSelectionChanged();

            StylePortMoveButtons();

            _toolTip.SetToolTip(btnPortMoveYPlus, "Move port +Y (world)");
            _toolTip.SetToolTip(btnPortMoveYMinus, "Move port -Y (world)");
            _toolTip.SetToolTip(btnPortMoveXMinus, "Move port -X (world)");
            _toolTip.SetToolTip(btnPortMoveXPlus, "Move port +X (world)");
            _toolTip.SetToolTip(btnPortMoveZPlus, "Move port +Z (world)");
            _toolTip.SetToolTip(btnPortMoveZMinus, "Move port -Z (world)");
            StylePortRotButtons();
            StyleAccentButton(btnAddPort, System.Drawing.Color.FromArgb(46, 125, 50));
            StyleAccentButton(btnPickPoint, System.Drawing.Color.FromArgb(0, 151, 167));
            StyleAccentButton(btnDeletePort, System.Drawing.Color.FromArgb(198, 40, 40));
            StyleAccentButton(btnCopyPort, System.Drawing.Color.FromArgb(0, 137, 123));
            StyleAccentButton(btnApplyPort, System.Drawing.Color.FromArgb(25, 118, 210));

            ApplyPortFieldLayout();
            tabPortManager.Resize += (_, _) => ApplyPortFieldLayout();

            _toolTip.SetToolTip(
                btnPickPoint,
                "Pick port position, then direction point in model");
            _toolTip.SetToolTip(
                cmbPortType,
                "Plant 3D catalog end type (FirstPortEndtypes metadata)");
            PortPointPickSession.PortCreated += OnPortPointPickCompleted;
        }

        public void OnPortCreatedFromPick(Guid portId)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<Guid>(OnPortCreatedFromPick), portId);
                return;
            }

            _selectedPortId = portId;
            RefreshPortUi();
            RefreshPortVisuals(highlightPortId: portId);
            try
            {
                string? dwg = DrawingContext.GetActiveDrawingPath();
                if (dwg != null)
                {
                    ValveProject project = DocumentStore.LoadOrCreate(
                        dwg, System.IO.Path.GetFileNameWithoutExtension(dwg));
                    RefreshCatalogCodeDisplay(project);
                }
            }
            catch
            {
                // ignore when document unavailable
            }

            lblPortStatus.Text = "Port created from picked point.";
        }

        private void OnPortPointPickCompleted(Guid portId) =>
            PaletteManager.NotifyPortCreatedFromPick(portId);

        private void StylePortMoveButtons()
        {
            StyleDirectionButton(btnPortMoveXMinus, System.Drawing.Color.FromArgb(66, 133, 244));
            StyleDirectionButton(btnPortMoveXPlus, System.Drawing.Color.FromArgb(26, 115, 232));
            StyleDirectionButton(btnPortMoveYPlus, System.Drawing.Color.FromArgb(102, 187, 106));
            StyleDirectionButton(btnPortMoveYMinus, System.Drawing.Color.FromArgb(56, 142, 60));
            StyleDirectionButton(btnPortMoveZPlus, System.Drawing.Color.FromArgb(171, 71, 188));
            StyleDirectionButton(btnPortMoveZMinus, System.Drawing.Color.FromArgb(123, 31, 162));
        }

        private void StylePortRotButtons()
        {
            StyleDirectionButton(btnPortRotX, System.Drawing.Color.FromArgb(66, 133, 244));
            StyleDirectionButton(btnPortRotY, System.Drawing.Color.FromArgb(102, 187, 106));
            StyleDirectionButton(btnPortRotZ, System.Drawing.Color.FromArgb(171, 71, 188));
        }

        private void tabPortManager_Enter(object sender, EventArgs e) => RefreshPortUi();

        private void RefreshPortUi()
        {
            try
            {
                string? dwg = DrawingContext.GetActiveDrawingPath();
                if (dwg == null)
                    return;

                ValveProject project = DocumentStore.LoadOrCreate(
                    dwg, System.IO.Path.GetFileNameWithoutExtension(dwg));
                chkShowPortMarkers.Checked = project.ShowPortMarkers;
                RefreshPortList(project);
                RefreshPortParentCombo(project);
                if (_selectedPortId.HasValue)
                {
                    ConnectionPort? port = project.FindPort(_selectedPortId.Value);
                    if (port != null)
                        LoadPortEditor(project, port);
                    else
                        ClearPortEditor();
                }
            }
            catch
            {
                // ignore when no drawing
            }
        }

        private void RefreshPortList(ValveProject project)
        {
            lvPorts.BeginUpdate();
            lvPorts.Items.Clear();
            foreach (ConnectionPort port in project.Ports.OrderBy(p => p.Number))
            {
                var item = new ListViewItem(port.Number.ToString(CultureInfo.InvariantCulture))
                {
                    Tag = port.Id,
                };
                item.SubItems.Add(PortConnectionTypeHelper.GetDisplayName(port.Type));
                item.SubItems.Add(PortService.ParentDisplayName(project, port));
                lvPorts.Items.Add(item);
            }
            lvPorts.EndUpdate();

            if (_selectedPortId.HasValue)
            {
                foreach (ListViewItem item in lvPorts.Items)
                {
                    if (item.Tag is Guid id && id == _selectedPortId.Value)
                    {
                        item.Selected = true;
                        item.Focused = true;
                        break;
                    }
                }
            }

            lblPortStatus.Text = project.Ports.Count == 0
                ? "No ports — click Add Port to define connection points."
                : $"{project.Ports.Count} port(s) in scene graph.";
        }

        private void RefreshPortParentCombo(ValveProject project)
        {
            object? prev = cmbPortParent.SelectedItem;
            cmbPortParent.Items.Clear();
            cmbPortParent.Items.Add(new PortParentOption(null, "(world)"));
            foreach (PrimitiveNode node in project.Parts.OrderBy(n => n.Name))
                cmbPortParent.Items.Add(new PortParentOption(node.Id, node.Name));

            cmbPortParent.DisplayMember = nameof(PortParentOption.Display);
            if (prev is PortParentOption opt)
            {
                foreach (PortParentOption item in cmbPortParent.Items)
                {
                    if (item.NodeId == opt.NodeId)
                    {
                        cmbPortParent.SelectedItem = item;
                        return;
                    }
                }
            }

            cmbPortParent.SelectedIndex = 0;
        }

        private void OnPortSelectionChanged()
        {
            if (lvPorts.SelectedItems.Count == 0)
            {
                _selectedPortId = null;
                ClearPortEditor();
                RefreshPortVisuals(highlightPortId: null);
                return;
            }

            _selectedPortId = (Guid)lvPorts.SelectedItems[0].Tag!;
            try
            {
                string dwg = DrawingContext.RequireActiveDrawingPath();
                ValveProject project = DocumentStore.LoadOrCreate(
                    dwg, System.IO.Path.GetFileNameWithoutExtension(dwg));
                ConnectionPort? port = project.FindPort(_selectedPortId.Value);
                if (port != null)
                {
                    LoadPortEditor(project, port);
                    RefreshPortVisuals(project, port.Id);
                }
            }
            catch
            {
                // ignore
            }
        }

        private void LoadPortEditor(ValveProject project, ConnectionPort port)
        {
            txtPortNumber.Text = port.Number.ToString(CultureInfo.InvariantCulture);
            SelectPortType(port.Type);

            foreach (PortParentOption item in cmbPortParent.Items)
            {
                if (item.NodeId == port.ParentNodeId)
                {
                    cmbPortParent.SelectedItem = item;
                    break;
                }
            }

            double[] pos = PortTransformMath.GetWorldPosition(project, port);
            double[] dir = PortTransformMath.GetWorldDirection(project, port);
            txtPortX.Text = PortService.FormatCoord(pos[0]);
            txtPortY.Text = PortService.FormatCoord(pos[1]);
            txtPortZ.Text = PortService.FormatCoord(pos[2]);
            txtPortDx.Text = PortService.FormatCoord(dir[0]);
            txtPortDy.Text = PortService.FormatCoord(dir[1]);
            txtPortDz.Text = PortService.FormatCoord(dir[2]);
        }

        private void SelectPortType(PortConnectionType type)
        {
            foreach (PortTypeOption item in cmbPortType.Items)
            {
                if (item.Type == type)
                {
                    cmbPortType.SelectedItem = item;
                    return;
                }
            }

            if (cmbPortType.Items.Count > 0)
                cmbPortType.SelectedIndex = 0;
        }

        private PortConnectionType GetSelectedPortType()
        {
            if (cmbPortType.SelectedItem is PortTypeOption option)
                return option.Type;
            return PortConnectionType.FL;
        }

        private void ClearPortEditor()
        {
            txtPortNumber.Text = "1";
            if (cmbPortType.Items.Count > 0)
                cmbPortType.SelectedIndex = 0;
            if (cmbPortParent.Items.Count > 0)
                cmbPortParent.SelectedIndex = 0;
            txtPortX.Text = txtPortY.Text = txtPortZ.Text = "0";
            txtPortDx.Text = "1";
            txtPortDy.Text = txtPortDz.Text = "0";
        }

        private void btnAddPort_Click(object sender, EventArgs e)
        {
            try
            {
                string dwg = DrawingContext.RequireActiveDrawingPath();
                ValveProject project = DocumentStore.LoadOrCreate(
                    dwg, System.IO.Path.GetFileNameWithoutExtension(dwg));

                Guid? parentId = _selectedNodeId;
                ConnectionPort port = PortService.Add(project, parentId);
                DocumentStore.Save(dwg, project);
                _selectedPortId = port.Id;
                RefreshPortList(project);
                RefreshPortParentCombo(project);
                LoadPortEditor(project, port);
                RefreshPortVisuals(project, port.Id);
                RefreshCatalogCodeDisplay(project);
                lblPortStatus.Text = $"Added {PortConnectionTypeHelper.PortLabel(port)}.";
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void btnPickPoint_Click(object sender, EventArgs e)
        {
            Document? doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                ShowWarning("Open a Plant 3D drawing first.");
                return;
            }

            if (!string.IsNullOrEmpty(doc.CommandInProgress))
            {
                ShowWarning("Finish the current command, then use Pick Point.");
                return;
            }

            PortPointPickSession.Begin(_selectedNodeId);
            lblPortStatus.Text = "Pick port position, then direction point...";
            doc.SendStringToExecute("P3DCOMPPICKPOINT\n", true, false, false);
        }

        private void btnDeletePort_Click(object sender, EventArgs e)
        {
            if (_selectedPortId == null)
            {
                ShowWarning("Select a port in the list first.");
                return;
            }

            try
            {
                string dwg = DrawingContext.RequireActiveDrawingPath();
                ValveProject project = DocumentStore.LoadOrCreate(
                    dwg, System.IO.Path.GetFileNameWithoutExtension(dwg));
                ConnectionPort? port = project.FindPort(_selectedPortId.Value);
                string label = port != null
                    ? PortConnectionTypeHelper.PortLabel(port)
                    : "port";
                PortService.Delete(project, _selectedPortId.Value);
                DocumentStore.Save(dwg, project);
                _selectedPortId = null;
                ClearPortEditor();
                RefreshPortList(project);
                RefreshPortVisuals(project);
                RefreshCatalogCodeDisplay(project);
                lblPortStatus.Text = $"Deleted {label}.";
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void btnCopyPort_Click(object sender, EventArgs e)
        {
            if (_selectedPortId == null)
            {
                ShowWarning("Select a port to copy.");
                return;
            }

            try
            {
                string dwg = DrawingContext.RequireActiveDrawingPath();
                ValveProject project = DocumentStore.LoadOrCreate(
                    dwg, System.IO.Path.GetFileNameWithoutExtension(dwg));
                ConnectionPort clone = PortService.Copy(project, _selectedPortId.Value);
                DocumentStore.Save(dwg, project);
                _selectedPortId = clone.Id;
                RefreshPortList(project);
                LoadPortEditor(project, clone);
                RefreshPortVisuals(project, clone.Id);
                RefreshCatalogCodeDisplay(project);
                lblPortStatus.Text = $"Copied to {PortConnectionTypeHelper.PortLabel(clone)}.";
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void btnApplyPort_Click(object sender, EventArgs e)
        {
            if (_selectedPortId == null)
            {
                ShowWarning("Select a port first.");
                return;
            }

            if (!TryParsePortFields(out double px, out double py, out double pz, out double dx, out double dy, out double dz))
                return;

            if (!TryReadPortNumberFromField(out int portNumber))
                return;

            try
            {
                string dwg = DrawingContext.RequireActiveDrawingPath();
                ValveProject project = DocumentStore.LoadOrCreate(
                    dwg, System.IO.Path.GetFileNameWithoutExtension(dwg));
                ConnectionPort? port = project.FindPort(_selectedPortId.Value);
                if (port == null)
                    return;

                Guid? parentId = (cmbPortParent.SelectedItem as PortParentOption)?.NodeId;

                PortService.Apply(
                    project,
                    port,
                    portNumber,
                    GetSelectedPortType(),
                    parentId,
                    px, py, pz,
                    dx, dy, dz);

                DocumentStore.Save(dwg, project);
                RefreshPortList(project);
                LoadPortEditor(project, port);
                RefreshPortVisuals(project, port.Id);
                RefreshCatalogCodeDisplay(project);
                lblPortStatus.Text = $"Applied {PortConnectionTypeHelper.PortLabel(port)}.";
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void chkShowPortMarkers_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                string? dwg = DrawingContext.GetActiveDrawingPath();
                if (dwg == null)
                    return;

                ValveProject project = DocumentStore.LoadOrCreate(
                    dwg, System.IO.Path.GetFileNameWithoutExtension(dwg));
                project.ShowPortMarkers = chkShowPortMarkers.Checked;
                DocumentStore.Save(dwg, project);
                RefreshPortVisuals(project, _selectedPortId);
            }
            catch
            {
                // ignore
            }
        }

        private void btnPortMoveXPlus_Click(object sender, EventArgs e) => JogPortPosition(+GetPortMoveStep(), 0, 0);
        private void btnPortMoveXMinus_Click(object sender, EventArgs e) => JogPortPosition(-GetPortMoveStep(), 0, 0);
        private void btnPortMoveYPlus_Click(object sender, EventArgs e) => JogPortPosition(0, +GetPortMoveStep(), 0);
        private void btnPortMoveYMinus_Click(object sender, EventArgs e) => JogPortPosition(0, -GetPortMoveStep(), 0);
        private void btnPortMoveZPlus_Click(object sender, EventArgs e) => JogPortPosition(0, 0, +GetPortMoveStep());
        private void btnPortMoveZMinus_Click(object sender, EventArgs e) => JogPortPosition(0, 0, -GetPortMoveStep());

        private void btnPortRotX_Click(object sender, EventArgs e) => JogPortDirection('X', GetPortRotStep());
        private void btnPortRotY_Click(object sender, EventArgs e) => JogPortDirection('Y', GetPortRotStep());
        private void btnPortRotZ_Click(object sender, EventArgs e) => JogPortDirection('Z', GetPortRotStep());

        private double GetPortMoveStep() => (double)numPortMoveStep.Value;
        private double GetPortRotStep() => (double)numPortRotStep.Value;

        private void JogPortPosition(double dx, double dy, double dz)
        {
            if (_selectedPortId == null)
            {
                ShowWarning("Select a port first.");
                return;
            }

            try
            {
                string dwg = DrawingContext.RequireActiveDrawingPath();
                ValveProject project = DocumentStore.LoadOrCreate(
                    dwg, System.IO.Path.GetFileNameWithoutExtension(dwg));
                ConnectionPort? port = project.FindPort(_selectedPortId.Value);
                if (port == null)
                    return;

                PortTransformMath.TranslateWorld(project, port, dx, dy, dz);
                DocumentStore.Save(dwg, project);
                LoadPortEditor(project, port);
                RefreshPortVisuals(project, port.Id);
                lblPortStatus.Text =
                    $"Moved {PortConnectionTypeHelper.PortLabel(port)} by ({PortService.FormatCoord(dx)}, {PortService.FormatCoord(dy)}, {PortService.FormatCoord(dz)}) mm.";
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void JogPortDirection(char axis, double degrees)
        {
            if (_selectedPortId == null)
            {
                ShowWarning("Select a port first.");
                return;
            }

            if (Math.Abs(degrees) < 1e-9)
            {
                ShowWarning("Enter a non-zero rotation step.");
                return;
            }

            try
            {
                string dwg = DrawingContext.RequireActiveDrawingPath();
                ValveProject project = DocumentStore.LoadOrCreate(
                    dwg, System.IO.Path.GetFileNameWithoutExtension(dwg));
                ConnectionPort? port = project.FindPort(_selectedPortId.Value);
                if (port == null)
                    return;

                if (rdoPortRotWorld.Checked)
                    PortTransformMath.RotateDirectionWorld(project, port, axis, degrees);
                else
                    PortTransformMath.RotateDirectionLocal(project, port, axis, degrees);

                DocumentStore.Save(dwg, project);
                LoadPortEditor(project, port);
                RefreshPortVisuals(project, port.Id);
                lblPortStatus.Text =
                    $"Rotated {PortConnectionTypeHelper.PortLabel(port)} direction {degrees:+0.###;-0.###;0} deg around {axis}.";
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private bool TryParsePortFields(
            out double px, out double py, out double pz,
            out double dx, out double dy, out double dz)
        {
            px = py = pz = dx = dy = dz = 0;
            if (!double.TryParse(txtPortX.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out px) ||
                !double.TryParse(txtPortY.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out py) ||
                !double.TryParse(txtPortZ.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out pz) ||
                !double.TryParse(txtPortDx.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out dx) ||
                !double.TryParse(txtPortDy.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out dy) ||
                !double.TryParse(txtPortDz.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out dz))
            {
                ShowWarning("Enter valid numeric values for position and direction.");
                return false;
            }

            return true;
        }

        private void RefreshPortVisuals(ValveProject? project = null, Guid? highlightPortId = null)
        {
            Document? doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
                return;

            try
            {
                if (project == null)
                {
                    string? dwg = DrawingContext.GetActiveDrawingPath();
                    if (dwg == null)
                        return;
                    project = DocumentStore.LoadOrCreate(
                        dwg, System.IO.Path.GetFileNameWithoutExtension(dwg));
                }

                PortVisualService.Refresh(doc, project, highlightPortId ?? _selectedPortId);
            }
            catch
            {
                // drawing may be busy during rebuild
            }
        }

        private bool TryReadPortNumberFromField(out int portNumber)
        {
            portNumber = 1;
            if (!int.TryParse(txtPortNumber.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out portNumber) ||
                portNumber < 1 || portNumber > 99)
            {
                ShowWarning("Port # must be an integer from 1 to 99.");
                return false;
            }

            return true;
        }

        private sealed class PortTypeOption
        {
            public PortTypeOption(PortConnectionType type) => Type = type;

            public PortConnectionType Type { get; }

            public override string ToString() => PortConnectionTypeHelper.GetDisplayName(Type);
        }

        private sealed class PortParentOption
        {
            public PortParentOption(Guid? nodeId, string display)
            {
                NodeId = nodeId;
                Display = display;
            }

            public Guid? NodeId { get; }
            public string Display { get; }
        }
    }
}
