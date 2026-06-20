using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Inventor;
using Plant3DSkeletonManager.Adapter;
using Plant3DSkeletonManager.Core;

namespace Plant3DSkeletonManager
{
    /// <summary>
    /// UI hosted inside the "Plant3D Skeleton Manager" dockable window.
    /// </summary>
    public partial class SkeletonForm : Form
    {
        private readonly Inventor.Application _inventorApp;
        private readonly IReadOnlyList<PrimitiveDefinition> _primitiveChoices;
        private Guid? _selectedNodeId;

        public SkeletonForm(Inventor.Application inventorApp)
        {
            _inventorApp = inventorApp ?? throw new ArgumentNullException(nameof(inventorApp));
            InitializeComponent();

            foreach (string valveType in ParameterService.ValveTypes)
                cmbValveType.Items.Add(valveType);

            _primitiveChoices = PrimitiveService.Primitives
                .OrderBy(p => p.DisplayName)
                .ToList();

            cmbPrimitive.DisplayMember = nameof(PrimitiveDefinition.DisplayName);
            foreach (PrimitiveDefinition p in _primitiveChoices)
                cmbPrimitive.Items.Add(p);
            cmbPrimitive.SelectedIndex = 0;

            foreach (string name in ParameterService.DimensionNames)
                dgvParams.Rows.Add(name, "");

            cmbValveType.SelectedIndexChanged += (_, _) => FillSuggestedValues();
            txtDN.TextChanged += (_, _) => FillSuggestedValues();
            cmbValveType.SelectedIndex = 0;

            treeScene.AllowDrop = true;
            treeScene.ItemDrag += TreeScene_ItemDrag;
            treeScene.DragEnter += TreeScene_DragEnter;
            treeScene.DragDrop += TreeScene_DragDrop;
            treeScene.AfterSelect += (_, _) => OnTreeSelectionChanged();

            var tip = new ToolTip();
            tip.SetToolTip(btnPosYPlus, "Move +Y (world)");
            tip.SetToolTip(btnPosYMinus, "Move -Y (world)");
            tip.SetToolTip(btnPosXMinus, "Move -X (world)");
            tip.SetToolTip(btnPosXPlus, "Move +X (world)");
            tip.SetToolTip(btnPosZPlus, "Move +Z (world)");
            tip.SetToolTip(btnPosZMinus, "Move -Z (world)");
            tip.SetToolTip(btnRotX, "Rotate around local X axis");
            tip.SetToolTip(btnRotY, "Rotate around local Y axis");
            tip.SetToolTip(btnRotZ, "Rotate around local Z axis");

            lvBoolOps.Columns.Add("#", 28);
            lvBoolOps.Columns.Add("Type", 58);
            lvBoolOps.Columns.Add("Target", 78);
            lvBoolOps.Columns.Add("Tools", 115);

            foreach (BooleanOpType t in Enum.GetValues(typeof(BooleanOpType)))
                cmbBoolType.Items.Add(t);
            cmbBoolType.SelectedIndex = 1;
        }

        private void FillSuggestedValues()
        {
            if (cmbValveType.SelectedItem is not string valveType)
                return;
            if (!TryParsePositive(txtDN.Text, out double dn))
                return;

            double[] suggested = ParameterService.SuggestDimensions(valveType, dn);
            for (int i = 0; i < suggested.Length; i++)
                dgvParams.Rows[i].Cells[1].Value = Format(suggested[i]);
        }

        private void btnCreateSkeleton_Click(object sender, EventArgs e)
        {
            if (cmbValveType.SelectedItem is not string valveType)
            {
                ShowWarning("Please select a valve type.");
                return;
            }

            if (!TryParsePositive(txtDN.Text, out double dn))
            {
                ShowWarning("Please enter a valid positive number for DN.");
                return;
            }

            string pressureClass = txtPressureClass.Text.Trim();
            if (pressureClass.Length == 0)
            {
                ShowWarning("Please enter a pressure class.");
                return;
            }

            var values = new double[ParameterService.DimensionNames.Length];
            for (int i = 0; i < values.Length; i++)
            {
                string text = dgvParams.Rows[i].Cells[1].Value?.ToString() ?? "";
                if (!TryParsePositive(text, out values[i]))
                {
                    ShowWarning($"Please enter a valid positive value for {ParameterService.DimensionNames[i]} (mm).");
                    return;
                }
            }

            var data = new SkeletonParameters
            {
                DN = dn,
                PressureClass = pressureClass,
                FaceToFace = values[0],
                BodyOD = values[1],
                BodyLength = values[2],
                BonnetHeight = values[3],
                StemDia = values[4],
                HandwheelOD = values[5],
            };

            try
            {
                ParameterService.CreateSkeletonParameters(_inventorApp, valveType, data);
                lblStatus.Text = $"Skeleton parameters saved ({valveType}, DN{dn:0.###}).";
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void btnInsertPrimitive_Click(object sender, EventArgs e)
        {
            if (cmbPrimitive.SelectedItem is not PrimitiveDefinition primitive)
            {
                ShowWarning("Please select a primitive type.");
                return;
            }

            try
            {
                Guid nodeId = PrimitiveService.Insert(_inventorApp, primitive);
                _selectedNodeId = nodeId;
                RefreshSceneTree();
                SelectTreeNode(nodeId);
                PrimitiveNode? inserted = GetProject()?.FindNode(nodeId);
                if (inserted != null)
                    LoadNodeEditor(inserted);
                lblSceneStatus.Text =
                    $"Inserted {inserted?.Name ?? "primitive"} ({primitive.DisplayName}).";
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void btnSync_Click(object sender, EventArgs e)
        {
            try
            {
                AssemblyDocument asmDoc = RequireAssembly();
                ValveProject project = DocumentStore.LoadOrCreate(asmDoc);
                SyncService.SyncResult result = SyncService.Pull(asmDoc, project);
                DocumentStore.Save(asmDoc, project);

                lblStatus.Text =
                    $"Synced: {result.Updated} updated, {result.Removed} removed" +
                    (result.PrunedOperations > 0 ? $", {result.PrunedOperations} operations pruned." : ".");
                RefreshSceneTree();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            try
            {
                AssemblyDocument asmDoc = RequireAssembly();
                ValveProject project = DocumentStore.LoadOrCreate(asmDoc);

                SyncService.Pull(asmDoc, project);
                DocumentStore.Save(asmDoc, project);

                ValidationResult validation = ProjectValidator.Validate(project);
                if (!ConfirmExport(validation))
                    return;

                using var dialog = new SaveFileDialog
                {
                    Title = "Export Scene Graph JSON",
                    Filter = "Scene JSON (*.scene.json)|*.scene.json|JSON (*.json)|*.json",
                    FileName = (string.IsNullOrEmpty(project.ValveName) ? "valve" : project.ValveName) + ".scene.json",
                    InitialDirectory = string.IsNullOrEmpty(asmDoc.FullFileName)
                        ? System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments)
                        : System.IO.Path.GetDirectoryName(asmDoc.FullFileName),
                };
                if (dialog.ShowDialog() != DialogResult.OK)
                    return;

                System.IO.File.WriteAllText(dialog.FileName, JsonCodec.Serialize(project));
                lblStatus.Text = $"Exported {project.Parts.Count} parts, {project.Operations.Count} operations.";
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void btnImportJson_Click(object sender, EventArgs e)
        {
            try
            {
                AssemblyDocument asmDoc = RequireAssembly();

                using var dialog = new OpenFileDialog
                {
                    Title = "Import Scene Graph JSON",
                    Filter = "Scene JSON (*.scene.json)|*.scene.json|JSON (*.json)|*.json",
                    InitialDirectory = string.IsNullOrEmpty(asmDoc.FullFileName)
                        ? System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments)
                        : System.IO.Path.GetDirectoryName(asmDoc.FullFileName),
                };
                if (dialog.ShowDialog() != DialogResult.OK)
                    return;

                ValveProject imported = SceneImportExport.LoadJsonFile(dialog.FileName);
                ValidationResult validation = ProjectValidator.Validate(imported);
                if (!validation.IsValid)
                {
                    ShowValidationBlocked(validation);
                    return;
                }

                if (validation.Issues.Any(i => !i.IsError))
                {
                    if (!ConfirmWithWarnings(
                            "Import this scene graph?",
                            validation.Warnings.ToList()))
                    {
                        return;
                    }
                }

                SceneImportExport.ReplaceProject(asmDoc, imported);
                lblStatus.Text =
                    $"Imported {imported.Parts.Count} parts, {imported.Operations.Count} operations.";

                if (MessageBox.Show(
                        "Rebuild Inventor geometry from the imported scene graph?",
                        "Plant3D Skeleton Manager",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    SceneImportExport.RebuildResult rebuild =
                        SceneImportExport.RebuildFromProject(_inventorApp, asmDoc, imported);
                    lblStatus.Text =
                        $"Imported and rebuilt: {rebuild.Inserted} inserted, {rebuild.Removed} removed.";
                }

                RefreshSceneTree();
                RefreshBooleanUi();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void btnRebuildScene_Click(object sender, EventArgs e)
        {
            try
            {
                AssemblyDocument asmDoc = RequireAssembly();
                ValveProject project = DocumentStore.LoadOrCreate(asmDoc);

                if (project.Parts.Count == 0)
                {
                    ShowWarning("Scene graph has no primitives to rebuild.");
                    return;
                }

                ValidationResult validation = ProjectValidator.Validate(project);
                if (!validation.IsValid)
                {
                    ShowValidationBlocked(validation);
                    return;
                }

                if (MessageBox.Show(
                        "Delete all tagged primitives and recreate them from the scene graph?",
                        "Plant3D Skeleton Manager",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question) != DialogResult.Yes)
                {
                    return;
                }

                SceneImportExport.RebuildResult result =
                    SceneImportExport.RebuildFromProject(_inventorApp, asmDoc, project);

                RefreshSceneTree();
                RefreshBooleanUi();
                lblStatus.Text = $"Rebuilt: {result.Inserted} inserted, {result.Removed} removed.";
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void tabBooleans_Enter(object sender, EventArgs e) => RefreshBooleanUi();

        private void btnAddBoolOp_Click(object sender, EventArgs e)
        {
            if (_selectedNodeId == null)
            {
                ShowWarning("Select a target primitive on the Scene tab first.");
                return;
            }

            if (cmbBoolType.SelectedItem is not BooleanOpType opType)
            {
                ShowWarning("Select a boolean operation type.");
                return;
            }

            var toolIds = new List<Guid>();
            foreach (object item in clbBoolTools.CheckedItems)
            {
                if (item is NodeListItem nli)
                    toolIds.Add(nli.Id);
            }

            if (toolIds.Count == 0)
            {
                ShowWarning("Check at least one tool node.");
                return;
            }

            try
            {
                AssemblyDocument asmDoc = RequireAssembly();
                ValveProject project = DocumentStore.LoadOrCreate(asmDoc);
                BooleanGraph.AddOperation(project, opType, _selectedNodeId.Value, toolIds);
                DocumentStore.Save(asmDoc, project);
                RefreshBooleanUi();
                lblBoolStatus.Text = "Boolean operation added.";
            }
            catch (Exception ex)
            {
                ShowBoolError(ex);
            }
        }

        private void btnRemoveBoolOp_Click(object sender, EventArgs e)
        {
            if (lvBoolOps.SelectedItems.Count == 0)
            {
                ShowWarning("Select an operation in the list first.");
                return;
            }

            if (lvBoolOps.SelectedItems[0].Tag is not int order)
                return;

            try
            {
                AssemblyDocument asmDoc = RequireAssembly();
                ValveProject project = DocumentStore.LoadOrCreate(asmDoc);
                BooleanGraph.RemoveOperation(project, order);
                DocumentStore.Save(asmDoc, project);
                RefreshBooleanUi();
                lblBoolStatus.Text = $"Removed operation #{order}.";
            }
            catch (Exception ex)
            {
                ShowBoolError(ex);
            }
        }

        private void btnApplyBoolVisuals_Click(object sender, EventArgs e)
        {
            try
            {
                AssemblyDocument asmDoc = RequireAssembly();
                ValveProject project = DocumentStore.LoadOrCreate(asmDoc);
                BooleanAppearanceService.ApplyAll(asmDoc, project);
                lblBoolStatus.Text = $"Applied visual cues for {project.Operations.Count} operation(s).";
            }
            catch (Exception ex)
            {
                ShowBoolError(ex);
            }
        }

        private void RefreshBooleanUi()
        {
            lvBoolOps.BeginUpdate();
            lvBoolOps.Items.Clear();
            clbBoolTools.Items.Clear();
            UpdateBooleanTargetLabel();

            try
            {
                ValveProject? project = GetProject();
                if (project == null)
                    return;

                foreach (BooleanOperation op in project.Operations.OrderBy(o => o.Order))
                {
                    string targetName = project.FindNode(op.Target)?.Name ?? op.Target.ToString();
                    var toolNames = new List<string>();
                    foreach (Guid toolId in op.Tools)
                    {
                        PrimitiveNode? tool = project.FindNode(toolId);
                        toolNames.Add(tool?.Name ?? toolId.ToString());
                    }

                    var item = new ListViewItem(op.Order.ToString())
                    {
                        Tag = op.Order,
                    };
                    item.SubItems.Add(op.Type.ToString());
                    item.SubItems.Add(targetName);
                    item.SubItems.Add(string.Join(", ", toolNames));
                    lvBoolOps.Items.Add(item);
                }

                foreach (PrimitiveNode node in project.Parts.OrderBy(p => p.Name))
                {
                    if (_selectedNodeId.HasValue && node.Id == _selectedNodeId.Value)
                        continue;

                    clbBoolTools.Items.Add(new NodeListItem
                    {
                        Id = node.Id,
                        Label = $"{node.Name} ({node.Type})",
                    });
                }
            }
            finally
            {
                lvBoolOps.EndUpdate();
            }
        }

        private void UpdateBooleanTargetLabel()
        {
            if (_selectedNodeId.HasValue)
            {
                PrimitiveNode? node = GetProject()?.FindNode(_selectedNodeId.Value);
                lblBoolTargetValue.Text = node != null
                    ? $"{node.Name} ({node.Type})"
                    : _selectedNodeId.Value.ToString();
                lblBoolTargetValue.ForeColor = SystemColors.ControlText;
            }
            else
            {
                lblBoolTargetValue.Text = "(select node on Scene tab)";
                lblBoolTargetValue.ForeColor = SystemColors.GrayText;
            }
        }

        private void btnRefreshTree_Click(object sender, EventArgs e) => RefreshSceneTree();

        private void btnDeleteNode_Click(object sender, EventArgs e)
        {
            if (_selectedNodeId == null)
            {
                ShowWarning("Select a primitive in the scene tree first.");
                return;
            }

            PrimitiveNode? node = GetProject()?.FindNode(_selectedNodeId.Value);
            if (node == null)
                return;

            if (MessageBox.Show(
                    $"Delete '{node.Name}' and its children from the scene?",
                    "Plant3D Skeleton Manager",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                AssemblyDocument asmDoc = RequireAssembly();
                ValveProject project = DocumentStore.LoadOrCreate(asmDoc);
                SceneEditorService.Delete(_inventorApp, asmDoc, project, _selectedNodeId.Value);
                _selectedNodeId = null;
                ClearNodeEditor();
                RefreshSceneTree();
                lblSceneStatus.Text = $"Deleted {node.Name}.";
            }
            catch (Exception ex)
            {
                ShowSceneError(ex);
            }
        }

        private void btnDuplicateNode_Click(object sender, EventArgs e)
        {
            if (_selectedNodeId == null)
            {
                ShowWarning("Select a primitive in the scene tree first.");
                return;
            }

            try
            {
                AssemblyDocument asmDoc = RequireAssembly();
                ValveProject project = DocumentStore.LoadOrCreate(asmDoc);
                Guid newId = SceneEditorService.Duplicate(_inventorApp, asmDoc, project, _selectedNodeId.Value);
                _selectedNodeId = newId;
                RefreshSceneTree();
                SelectTreeNode(newId);
                lblSceneStatus.Text = "Duplicated primitive.";
            }
            catch (Exception ex)
            {
                ShowSceneError(ex);
            }
        }

        private void btnSelectInInventor_Click(object sender, EventArgs e)
        {
            if (_selectedNodeId == null)
                return;

            try
            {
                AssemblyDocument asmDoc = RequireAssembly();
                SceneEditorService.SelectInInventor(asmDoc, _selectedNodeId.Value);
            }
            catch (Exception ex)
            {
                ShowSceneError(ex);
            }
        }

        private void btnResolveExpr_Click(object sender, EventArgs e)
        {
            if (_selectedNodeId == null)
                return;

            try
            {
                AssemblyDocument asmDoc = RequireAssembly();
                ValveProject project = DocumentStore.LoadOrCreate(asmDoc);
                PrimitiveNode? node = project.FindNode(_selectedNodeId.Value);
                if (node == null)
                    return;

                PushService.ResolveExpressions(node, project.Parameters);
                LoadNodeEditor(node);
                lblSceneStatus.Text = "Expressions resolved from skeleton parameters.";
            }
            catch (Exception ex)
            {
                ShowSceneError(ex);
            }
        }

        private void btnApplyNode_Click(object sender, EventArgs e)
        {
            if (_selectedNodeId == null)
                return;

            try
            {
                AssemblyDocument asmDoc = RequireAssembly();
                ValveProject project = DocumentStore.LoadOrCreate(asmDoc);
                PrimitiveNode? node = project.FindNode(_selectedNodeId.Value);
                if (node == null)
                    return;

                if (!TryApplyEditorToNode(node, out string? error))
                {
                    ShowWarning(error ?? "Invalid property values.");
                    return;
                }

                CommitNodeTransform(asmDoc, project, node, $"Applied changes to {node.Name}.");
                RefreshSceneTree();
                SelectTreeNode(node.Id);
            }
            catch (Exception ex)
            {
                ShowSceneError(ex);
            }
        }

        private void btnPosXPlus_Click(object sender, EventArgs e) => JogPosition(+GetPosStep(), 0, 0);
        private void btnPosXMinus_Click(object sender, EventArgs e) => JogPosition(-GetPosStep(), 0, 0);
        private void btnPosYPlus_Click(object sender, EventArgs e) => JogPosition(0, +GetPosStep(), 0);
        private void btnPosYMinus_Click(object sender, EventArgs e) => JogPosition(0, -GetPosStep(), 0);
        private void btnPosZPlus_Click(object sender, EventArgs e) => JogPosition(0, 0, +GetPosStep());
        private void btnPosZMinus_Click(object sender, EventArgs e) => JogPosition(0, 0, -GetPosStep());

        private void btnRotX_Click(object sender, EventArgs e) => JogRotation('X', GetAngleStep());
        private void btnRotY_Click(object sender, EventArgs e) => JogRotation('Y', GetAngleStep());
        private void btnRotZ_Click(object sender, EventArgs e) => JogRotation('Z', GetAngleStep());

        private double GetPosStep() => (double)numPosStep.Value;
        private double GetAngleStep() => (double)numAngleStep.Value;

        private void JogPosition(double dx, double dy, double dz)
        {
            if (_selectedNodeId == null)
            {
                ShowWarning("Select a primitive in the scene tree first.");
                return;
            }

            try
            {
                AssemblyDocument asmDoc = RequireAssembly();
                ValveProject project = DocumentStore.LoadOrCreate(asmDoc);
                PrimitiveNode? node = project.FindNode(_selectedNodeId.Value);
                if (node == null)
                    return;

                TransformMath.TranslateWorld(node, dx, dy, dz);
                CommitNodeTransform(asmDoc, project, node,
                    $"Moved {node.Name} by ({Format(dx)}, {Format(dy)}, {Format(dz)}) mm.");
            }
            catch (Exception ex)
            {
                ShowSceneError(ex);
            }
        }

        private void JogRotation(char axis, double degrees)
        {
            if (_selectedNodeId == null)
            {
                ShowWarning("Select a primitive in the scene tree first.");
                return;
            }

            if (Math.Abs(degrees) < 1e-9)
            {
                ShowWarning("Enter a non-zero angle (°). Use negative values to reverse direction.");
                return;
            }

            try
            {
                AssemblyDocument asmDoc = RequireAssembly();
                ValveProject project = DocumentStore.LoadOrCreate(asmDoc);
                PrimitiveNode? node = project.FindNode(_selectedNodeId.Value);
                if (node == null)
                    return;

                TransformMath.RotateWorld(node, axis, degrees);
                CommitNodeTransform(asmDoc, project, node,
                    $"Rotated {node.Name} {degrees:+0.###;-0.###;0}° around world {axis}.");
            }
            catch (Exception ex)
            {
                ShowSceneError(ex);
            }
        }

        private void CommitNodeTransform(AssemblyDocument asmDoc, ValveProject project, PrimitiveNode node, string status)
        {
            SceneEditorService.Rename(asmDoc, project, node.Id, node.Name);
            PushService.PushNode(_inventorApp, asmDoc, project, node.Id);
            DocumentStore.Save(asmDoc, project);
            LoadNodeEditor(node);
            lblSceneStatus.Text = status;
        }

        private void TreeScene_ItemDrag(object? sender, ItemDragEventArgs e)
        {
            if (treeScene.SelectedNode != null)
                treeScene.DoDragDrop(treeScene.SelectedNode, DragDropEffects.Move);
        }

        private void TreeScene_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(typeof(TreeNode)) == true)
                e.Effect = DragDropEffects.Move;
        }

        private void TreeScene_DragDrop(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetData(typeof(TreeNode)) is not TreeNode dragged)
                return;

            Guid draggedId = (Guid)dragged.Tag!;
            System.Drawing.Point client = treeScene.PointToClient(new System.Drawing.Point(e.X, e.Y));
            TreeNode? target = treeScene.GetNodeAt(client);

            Guid? newParent = target?.Tag is Guid g ? g : null;
            if (target != null && target == dragged)
                return;

            try
            {
                AssemblyDocument asmDoc = RequireAssembly();
                ValveProject project = DocumentStore.LoadOrCreate(asmDoc);
                SceneEditorService.Reparent(project, draggedId, newParent);
                DocumentStore.Save(asmDoc, project);
                RefreshSceneTree();
                SelectTreeNode(draggedId);
                lblSceneStatus.Text = newParent.HasValue
                    ? "Reparented under " + project.FindNode(newParent.Value)?.Name + "."
                    : "Moved to root level.";
            }
            catch (Exception ex)
            {
                ShowSceneError(ex);
            }
        }

        private void OnTreeSelectionChanged()
        {
            if (treeScene.SelectedNode?.Tag is not Guid id)
            {
                _selectedNodeId = null;
                ClearNodeEditor();
                UpdateBooleanTargetLabel();
                return;
            }

            _selectedNodeId = id;
            PrimitiveNode? node = GetProject()?.FindNode(id);
            if (node != null)
                LoadNodeEditor(node);
            UpdateBooleanTargetLabel();
        }

        private void RefreshSceneTree()
        {
            Guid? keep = _selectedNodeId;
            treeScene.BeginUpdate();
            treeScene.Nodes.Clear();

            try
            {
                ValveProject? project = GetProject();
                if (project == null)
                    return;

                foreach (PrimitiveNode root in SceneGraphHelpers.ChildrenOf(project, null))
                    treeScene.Nodes.Add(CreateTreeNode(root, project));

                treeScene.ExpandAll();
                if (keep.HasValue)
                    SelectTreeNode(keep.Value);
            }
            finally
            {
                treeScene.EndUpdate();
            }
        }

        private static TreeNode CreateTreeNode(PrimitiveNode node, ValveProject project)
        {
            var tn = new TreeNode($"{node.Name} ({node.Type})") { Tag = node.Id };
            foreach (PrimitiveNode child in SceneGraphHelpers.ChildrenOf(project, node.Id))
                tn.Nodes.Add(CreateTreeNode(child, project));
            return tn;
        }

        private void SelectTreeNode(Guid id)
        {
            TreeNode? found = FindTreeNode(treeScene.Nodes, id);
            if (found != null)
                treeScene.SelectedNode = found;
        }

        private static TreeNode? FindTreeNode(TreeNodeCollection nodes, Guid id)
        {
            foreach (TreeNode n in nodes)
            {
                if (n.Tag is Guid g && g == id)
                    return n;
                TreeNode? child = FindTreeNode(n.Nodes, id);
                if (child != null)
                    return child;
            }
            return null;
        }

        private void LoadNodeEditor(PrimitiveNode node)
        {
            txtNodeName.Text = node.Name;
            txtOriginX.Text = Format(node.Origin[0]);
            txtOriginY.Text = Format(node.Origin[1]);
            txtOriginZ.Text = Format(node.Origin[2]);
            lblNodeType.Text = node.Type.ToString();

            dgvNodeParams.Rows.Clear();
            foreach (var kv in node.Parameters)
                dgvNodeParams.Rows.Add(kv.Key, Format(kv.Value.Value), kv.Value.Expression ?? "");
        }

        private void ClearNodeEditor()
        {
            txtNodeName.Text = "";
            txtOriginX.Text = txtOriginY.Text = txtOriginZ.Text = "";
            lblNodeType.Text = "(none)";
            dgvNodeParams.Rows.Clear();
        }

        private bool TryApplyEditorToNode(PrimitiveNode node, out string? error)
        {
            error = null;
            string name = txtNodeName.Text.Trim();
            if (name.Length == 0)
            {
                error = "Name cannot be empty.";
                return false;
            }
            node.Name = name;

            if (!TryParseDouble(txtOriginX.Text, out double ox) ||
                !TryParseDouble(txtOriginY.Text, out double oy) ||
                !TryParseDouble(txtOriginZ.Text, out double oz))
            {
                error = "Origin must be valid numbers (mm).";
                return false;
            }
            node.Origin = new[] { ox, oy, oz };

            foreach (DataGridViewRow row in dgvNodeParams.Rows)
            {
                if (row.IsNewRow)
                    continue;

                string paramName = row.Cells[0].Value?.ToString() ?? "";
                if (paramName.Length == 0)
                    continue;

                string valueText = row.Cells[1].Value?.ToString() ?? "";
                string exprText = row.Cells[2].Value?.ToString()?.Trim() ?? "";

                if (!TryParsePositive(valueText, out double value))
                {
                    error = $"Parameter '{paramName}' needs a positive value (mm).";
                    return false;
                }

                if (!node.Parameters.TryGetValue(paramName, out ParamValue? pv))
                    node.Parameters[paramName] = pv = new ParamValue();

                pv.Value = value;
                pv.Expression = exprText.Length > 0 ? exprText : null;
            }

            return true;
        }

        private ValveProject? GetProject()
        {
            if (_inventorApp.ActiveDocument is not AssemblyDocument asmDoc)
                return null;
            return DocumentStore.LoadOrCreate(asmDoc);
        }

        private AssemblyDocument RequireAssembly()
        {
            if (_inventorApp.ActiveDocument is not AssemblyDocument asmDoc)
                throw new InvalidOperationException(
                    "Please open or activate an assembly document (.iam) first.");
            return asmDoc;
        }

        private static string Format(double value) =>
            value.ToString("0.###", CultureInfo.InvariantCulture);

        private static bool TryParsePositive(string text, out double value)
        {
            return double.TryParse(
                       text.Trim().Replace(',', '.'),
                       NumberStyles.Float,
                       CultureInfo.InvariantCulture,
                       out value)
                   && value > 0;
        }

        private static bool TryParseDouble(string text, out double value) =>
            double.TryParse(
                text.Trim().Replace(',', '.'),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out value);

        private void ShowError(Exception ex)
        {
            lblStatus.Text = string.Empty;
            MessageBox.Show(ex.Message, "Plant3D Skeleton Manager",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void ShowSceneError(Exception ex)
        {
            lblSceneStatus.Text = string.Empty;
            MessageBox.Show(ex.Message, "Plant3D Skeleton Manager",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static void ShowWarning(string message)
        {
            MessageBox.Show(message, "Plant3D Skeleton Manager",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private static bool ConfirmExport(ValidationResult validation)
        {
            if (!validation.IsValid)
            {
                ShowValidationBlocked(validation);
                return false;
            }

            var warnings = validation.Warnings.ToList();
            if (warnings.Count == 0)
                return true;

            return ConfirmWithWarnings("Export scene graph anyway?", warnings);
        }

        private static void ShowValidationBlocked(ValidationResult validation)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Fix these errors before continuing:");
            foreach (string err in validation.Errors)
                sb.AppendLine("• " + err);

            MessageBox.Show(
                sb.ToString(),
                "Plant3D Skeleton Manager",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private static bool ConfirmWithWarnings(string prompt, IList<string> warnings)
        {
            var sb = new StringBuilder();
            sb.AppendLine(prompt);
            sb.AppendLine();
            sb.AppendLine("Warnings:");
            foreach (string w in warnings)
                sb.AppendLine("• " + w);

            return MessageBox.Show(
                sb.ToString(),
                "Plant3D Skeleton Manager",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) == DialogResult.Yes;
        }

        private void ShowBoolError(Exception ex)
        {
            lblBoolStatus.Text = string.Empty;
            MessageBox.Show(ex.Message, "Plant3D Skeleton Manager",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private sealed class NodeListItem
        {
            public required Guid Id { get; init; }
            public required string Label { get; init; }

            public override string ToString() => Label;
        }
    }
}
