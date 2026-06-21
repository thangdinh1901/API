using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Plant3DCatalogComposer.Services;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer
{
    public partial class ComposerForm : UserControl
    {
        private static readonly Color TabPageBackColor = Color.FromArgb(255, 244, 224);

        private static readonly Color[] TabHeaderColors =
        {
            Color.FromArgb(25, 118, 210),   // Setup
            Color.FromArgb(46, 125, 50),      // Scene
            Color.FromArgb(230, 81, 0),       // Booleans
            Color.FromArgb(0, 151, 167),      // Port Manager
            Color.FromArgb(94, 53, 177),      // Code
        };

        private readonly IReadOnlyList<CustomPartDefinition> _catalogPartChoices;
        private readonly IReadOnlyList<PrimitiveDefinition> _primitiveChoices;
        private readonly ToolTip _toolTip = new();
        private Guid? _selectedNodeId;

        public ComposerForm()
        {
            InitializeComponent();
            ConfigureForPaletteHost();

            foreach (string valveType in ParameterService.ValveTypes)
                cmbValveType.Items.Add(valveType);

            foreach (PipeSizeOption size in PipeSizeCatalog.NominalSizes)
            {
                cmbDn.Items.Add(size);
                cmbCatalogDN.Items.Add(size);
            }
            foreach (string pc in PipeSizeCatalog.PressureClasses)
            {
                cmbPressureClass.Items.Add(pc);
                cmbCatalogPressureClass.Items.Add(pc);
            }

            cmbDn.DisplayMember = nameof(PipeSizeOption.Display);
            cmbCatalogDN.DisplayMember = nameof(PipeSizeOption.Display);
            SelectDnCombo(cmbDn, 50);
            SelectDnCombo(cmbCatalogDN, 100);
            if (cmbPressureClass.Items.Count > 0)
                cmbPressureClass.SelectedIndex = 0;
            if (cmbCatalogPressureClass.Items.Count > 0)
                cmbCatalogPressureClass.SelectedIndex = 0;

            foreach (CatalogCategoryOption category in CatalogCategories.All)
                cmbCatalogCategory.Items.Add(category);

            _catalogPartChoices = CustomPartCatalog.InsertableParts;
            cmbCatalogCategory.DisplayMember = nameof(CatalogCategoryOption.Display);
            cmbCatalogPart.DisplayMember = nameof(CustomPartDefinition.DisplayName);
            if (cmbCatalogCategory.Items.Count > 0)
                cmbCatalogCategory.SelectedIndex = 0;
            RefreshCatalogPartList();
            cmbCatalogCategory.SelectedIndexChanged += (_, _) => RefreshCatalogPartList();
            cmbCatalogPart.SelectedIndexChanged += (_, _) => OnCatalogPartSelectionChanged();
            cmbCatalogPressureClass.SelectedIndexChanged += (_, _) => RefreshCatalogPartList();

            _primitiveChoices = Services.PrimitiveService.Primitives
                .OrderBy(p => p.DisplayName)
                .ToList();

            cmbPrimitive.DisplayMember = nameof(PrimitiveDefinition.DisplayName);
            foreach (PrimitiveDefinition p in _primitiveChoices)
                cmbPrimitive.Items.Add(p);
            cmbPrimitive.SelectedIndex = 0;

            foreach (string name in ParameterService.DimensionNames)
                dgvParams.Rows.Add(name, "");

            cmbValveType.SelectedIndexChanged += (_, _) => FillSuggestedValues();
            cmbDn.SelectedIndexChanged += (_, _) => FillSuggestedValues();
            cmbValveType.SelectedIndex = 0;

            treeScene.AllowDrop = true;
            treeScene.ItemDrag += TreeScene_ItemDrag;
            treeScene.DragEnter += TreeScene_DragEnter;
            treeScene.DragDrop += TreeScene_DragDrop;
            treeScene.AfterSelect += (_, _) => OnTreeSelectionChanged();
            treeScene.KeyDown += TreeScene_KeyDown;

            _toolTip.SetToolTip(btnPosYPlus, "Move +Y (world)");
            _toolTip.SetToolTip(btnPosYMinus, "Move -Y (world)");
            _toolTip.SetToolTip(btnPosXMinus, "Move -X (world)");
            _toolTip.SetToolTip(btnPosXPlus, "Move +X (world)");
            _toolTip.SetToolTip(btnPosZPlus, "Move +Z (world)");
            _toolTip.SetToolTip(btnPosZMinus, "Move -Z (world)");
            UpdateRotationAxisTooltips();
            _toolTip.SetToolTip(btnGenerateCode,
                "Custom parts → export to catalog_generator/parts. Standard parts → port reference only (_composer_exports), library unchanged.");
            _toolTip.SetToolTip(btnDeployCatalog,
                "Copy library to CustomScripts, clear __pycache__, PLANTREGISTERCUSTOMSCRIPTS.");
            _toolTip.SetToolTip(
                btnPublishCatalog,
                "Export Catalog Builder Excel workbook (.xlsx) for Gasket / Flange / Valve parts");
            _toolTip.SetToolTip(
                btnTestCatalog,
                "Run testacpscript for the current catalog part (DN from Catalog Project)");
            _toolTip.SetToolTip(btnExport, "Export scene graph to JSON file");
            _toolTip.SetToolTip(btnImportJson, "Import scene graph from JSON file");
            _toolTip.SetToolTip(btnRebuildScene, "Force rebuild geometry in Plant 3D");
            _toolTip.SetToolTip(
                btnDeleteNode,
                "Delete selected primitive. Selection moves to the next item (or previous if last) "
                + "so you can press Delete repeatedly.");
            _toolTip.SetToolTip(numPosStepX, "Jog step along X (mm, integer)");
            _toolTip.SetToolTip(numPosStepY, "Jog step along Y (mm, integer)");
            _toolTip.SetToolTip(numPosStepZ, "Jog step along Z (mm, integer)");
            _toolTip.SetToolTip(btnPickPosStep, "Pick two CAD points — sets step X/Y/Z from |ΔX|, |ΔY|, |ΔZ|");
            _toolTip.SetToolTip(btnAlignPos, "Pick on part, then target — moves part by ΔX, ΔY, ΔZ in WCS");

            StylePositionDirectionButtons();
            StyleSceneToolButtons();
            StylePrimaryActionButtons();
            DistancePickSession.Completed += OnDisplacementPickSessionCompleted;
            InitializeCatalogSetupTab();
            InitializePortManagerTab();

            lvBoolOps.Columns.Add("#", 28);
            lvBoolOps.Columns.Add("Type", 58);
            lvBoolOps.Columns.Add("Target", 78);
            lvBoolOps.Columns.Add("Tools", 115);

            foreach (BooleanOpType t in Enum.GetValues(typeof(BooleanOpType)))
                cmbBoolType.Items.Add(t);
            cmbBoolType.SelectedIndex = 1;

            cmbBoolCutter.DisplayMember = nameof(PrimitiveDefinition.DisplayName);
            foreach (PrimitiveDefinition cutter in PrimitiveCatalog.Cutters)
                cmbBoolCutter.Items.Add(cutter);
            if (cmbBoolCutter.Items.Count > 0)
                cmbBoolCutter.SelectedIndex = 0;
        }

        /// <summary>WinForms GroupBox ignores FontStyle.Bold on the caption — draw it ourselves.</summary>
        private static void StyleGroupBoxCaption(GroupBox groupBox, string caption)
        {
            groupBox.Text = string.Empty;
            groupBox.Paint += (_, e) => DrawGroupBoxCaption(e, groupBox, caption);
        }

        private static void DrawGroupBoxCaption(PaintEventArgs e, GroupBox groupBox, string caption)
        {
            const float titlePt = 9f;
            const int x = 8;
            const int y = 6;
            using Font font = new Font("Segoe UI", titlePt, FontStyle.Bold, GraphicsUnit.Point);
            Size textSize = TextRenderer.MeasureText(caption, font);
            Rectangle clear = new Rectangle(x - 1, y, textSize.Width + 2, textSize.Height);
            using (Brush bg = new SolidBrush(groupBox.BackColor))
                e.Graphics.FillRectangle(bg, clear);
            TextRenderer.DrawText(
                e.Graphics,
                caption,
                font,
                new Point(x, y),
                groupBox.ForeColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        }

        private void ConfigureForPaletteHost()
        {
            BackColor = TabPageBackColor;
            StyleTabColors();
        }

        private void StyleTabColors()
        {
            tabMain.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabMain.DrawItem += TabMain_DrawItem;
            tabMain.SelectedIndexChanged += (_, _) => tabMain.Invalidate();
            tabMain.Padding = new Point(10, 4);
            tabMain.BackColor = TabPageBackColor;

            foreach (TabPage page in tabMain.TabPages)
            {
                page.UseVisualStyleBackColor = false;
                page.BackColor = TabPageBackColor;
                ApplySheetBackColor(page);
            }
        }

        private static void ApplySheetBackColor(Control root)
        {
            foreach (Control child in root.Controls)
            {
                if (child is GroupBox groupBox)
                    groupBox.BackColor = TabPageBackColor;
                else if (child is Panel or Label or RadioButton or CheckBox)
                {
                    child.BackColor = TabPageBackColor;
                }

                if (child.HasChildren)
                    ApplySheetBackColor(child);
            }
        }

        private void TabMain_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= tabMain.TabPages.Count)
                return;

            TabPage page = tabMain.TabPages[e.Index];
            bool selected = tabMain.SelectedIndex == e.Index;
            Color header = TabHeaderColors[e.Index % TabHeaderColors.Length];
            Color fill = selected ? header : ControlPaint.LightLight(header);
            Color text = selected ? Color.White : ControlPaint.Dark(header);

            using var brush = new SolidBrush(fill);
            e.Graphics.FillRectangle(brush, e.Bounds);

            TextRenderer.DrawText(
                e.Graphics,
                page.Text,
                e.Font ?? tabMain.Font,
                e.Bounds,
                text,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        /// <summary>Axis-colored position pad — X blue, Y green, Z purple.</summary>
        private void StylePositionDirectionButtons()
        {
            Color xLight = Color.FromArgb(66, 133, 244);
            Color xDark = Color.FromArgb(26, 115, 232);
            Color yLight = Color.FromArgb(102, 187, 106);
            Color yDark = Color.FromArgb(56, 142, 60);
            Color zLight = Color.FromArgb(171, 71, 188);
            Color zDark = Color.FromArgb(123, 31, 162);

            StyleDirectionButton(btnPosXMinus, xLight);
            StyleDirectionButton(btnPosXPlus, xDark);
            StyleDirectionButton(btnPosYPlus, yLight);
            StyleDirectionButton(btnPosYMinus, yDark);
            StyleDirectionButton(btnPosZPlus, zLight);
            StyleDirectionButton(btnPosZMinus, zDark);
            StyleAccentButton(btnPickPosStep, Color.FromArgb(0, 151, 167));
            StyleAccentButton(btnAlignPos, Color.FromArgb(0, 121, 137));
        }

        private static void StyleDirectionButton(Button button, Color backColor)
        {
            StyleAccentButton(button, backColor);
        }

        private void StylePrimaryActionButtons()
        {
            Color back = Color.FromArgb(187, 222, 251);
            Color border = Color.FromArgb(144, 202, 249);
            Color fore = Color.FromArgb(21, 54, 92);
            StyleLightActionButton(btnCreateSkeleton, back, border, fore);
            StyleLightActionButton(btnInsertCatalogPart, back, border, fore);
        }

        private static void StyleLightActionButton(Button button, Color back, Color border, Color fore)
        {
            button.UseVisualStyleBackColor = false;
            button.FlatStyle = FlatStyle.Flat;
            button.BackColor = back;
            button.ForeColor = fore;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = border;
        }

        private void StyleSceneToolButtons()
        {
            StyleAccentButton(btnGenerateCode, Color.FromArgb(96, 125, 139));
            StyleAccentButton(btnDeployCatalog, Color.FromArgb(69, 90, 100));
            StyleAccentButton(btnPublishCatalog, Color.FromArgb(46, 125, 50));
            StyleAccentButton(btnTestCatalog, Color.FromArgb(0, 137, 123));
            StyleAccentButton(btnExport, Color.FromArgb(0, 137, 123));
            StyleAccentButton(btnImportJson, Color.FromArgb(239, 108, 0));
            StyleAccentButton(btnRebuildScene, Color.FromArgb(25, 118, 210));
        }

        private static void StyleAccentButton(Button button, Color backColor)
        {
            button.UseVisualStyleBackColor = false;
            button.FlatStyle = FlatStyle.Flat;
            button.BackColor = backColor;
            button.ForeColor = Color.White;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = ControlPaint.Dark(backColor);
        }

        public void RefreshFromDocument()
        {
            try
            {
                string? dwg = DrawingContext.GetActiveDrawingPath();
                if (dwg == null)
                {
                    lblStatus.Text = "No active DWG.";
                    return;
                }

                ValveProject project = DocumentStore.LoadOrCreate(
                    dwg, Path.GetFileNameWithoutExtension(dwg));
                LoadSkeletonFields(project);
                LoadCatalogProjectFields(project);
                RefreshSceneTree();
                RefreshBooleanUi();
                RefreshPortUi();
                RefreshPortVisuals(project);
                lblStatus.Text = $"Loaded: {Path.GetFileName(dwg)} · {PluginVersion.StatusSuffix}";
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void LoadSkeletonFields(ValveProject project)
        {
            SkeletonParameters p = project.Parameters;
            if (p.DN > 0)
            {
                SelectDnCombo(cmbDn, p.DN);
                SelectDnCombo(cmbCatalogDN, p.DN);
            }
            if (!string.IsNullOrEmpty(p.PressureClass))
            {
                SelectPressureClassCombo(cmbPressureClass, p.PressureClass);
                SelectPressureClassCombo(cmbCatalogPressureClass, p.PressureClass);
                RefreshCatalogPartList();
            }

            double[] values =
            {
                p.FaceToFace, p.BodyOD, p.BodyLength,
                p.BonnetHeight, p.StemDia, p.HandwheelOD,
            };
            for (int i = 0; i < values.Length && i < dgvParams.Rows.Count; i++)
            {
                if (values[i] > 0)
                    dgvParams.Rows[i].Cells[1].Value = Format(values[i]);
            }
        }

        private void FillSuggestedValues()
        {
            if (cmbValveType.SelectedItem is not string valveType)
                return;
            if (!TryGetSelectedDn(cmbDn, out double dn))
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

            if (!TryGetSelectedDn(cmbDn, out double dn))
            {
                ShowWarning("Please select a nominal pipe size (DN).");
                return;
            }

            if (cmbPressureClass.SelectedItem is not string pressureClass || pressureClass.Length == 0)
            {
                ShowWarning("Please select a pressure class.");
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
                string dwg = DrawingContext.RequireActiveDrawingPath();
                ValveProject project = DocumentStore.LoadOrCreate(
                    dwg, Path.GetFileNameWithoutExtension(dwg));
                ParameterService.ApplySkeleton(project, valveType, data);
                DocumentStore.Save(dwg, project);
                lblStatus.Text = $"Skeleton saved ({valveType}, DN{dn:0.###}).";
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void RefreshCatalogPartList()
        {
            string pc = _cmbProjectPressureClass?.SelectedItem as string
                ?? cmbCatalogPressureClass.SelectedItem as string
                ?? "150";
            string schedule = GetProjectSchedule();
            string category = (cmbCatalogCategory.SelectedItem as CatalogCategoryOption)?.Id
                ?? CatalogCategories.Flange;
            int prevIndex = cmbCatalogPart.SelectedIndex;

            cmbCatalogPart.Items.Clear();
            foreach (CustomPartDefinition part in _catalogPartChoices)
            {
                if (!part.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!part.PressureClass.Equals(pc, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!PipeScheduleCatalog.PartMatches(schedule, part.PipeSchedule))
                    continue;
                cmbCatalogPart.Items.Add(part);
            }

            if (cmbCatalogPart.Items.Count == 0)
            {
                btnInsertCatalogPart.Enabled = false;
                return;
            }

            btnInsertCatalogPart.Enabled = true;
            cmbCatalogPart.SelectedIndex = prevIndex >= 0 && prevIndex < cmbCatalogPart.Items.Count
                ? prevIndex
                : 0;
            OnCatalogPartSelectionChanged();
        }

        private void OnCatalogPartSelectionChanged()
        {
            if (cmbCatalogPart.SelectedItem is not CustomPartDefinition part)
                return;

            if (part.ParametricDN)
            {
                cmbCatalogDN.Enabled = true;
                if (cmbCatalogDN.SelectedItem == null)
                    SelectDnCombo(cmbCatalogDN, part.DefaultDN);
            }
            else
            {
                SelectDnCombo(cmbCatalogDN, part.DefaultDN);
                cmbCatalogDN.Enabled = false;
            }
        }

        private bool TryGetSelectedDn(ComboBox combo, out double dn)
        {
            dn = 0;
            if (combo.SelectedItem is PipeSizeOption size)
            {
                dn = size.DnMm;
                return true;
            }
            return false;
        }

        private static void SelectDnCombo(ComboBox combo, double dn)
        {
            for (int i = 0; i < combo.Items.Count; i++)
            {
                if (combo.Items[i] is PipeSizeOption size && Math.Abs(size.DnMm - dn) < 0.5)
                {
                    combo.SelectedIndex = i;
                    return;
                }
            }
        }

        private static void SelectPressureClassCombo(ComboBox combo, string pressureClass)
        {
            for (int i = 0; i < combo.Items.Count; i++)
            {
                if (combo.Items[i] is string pc &&
                    pc.Equals(pressureClass, StringComparison.OrdinalIgnoreCase))
                {
                    combo.SelectedIndex = i;
                    return;
                }
            }
        }

        private void btnInsertCatalogPart_Click(object sender, EventArgs e)
        {
            if (cmbCatalogPart.SelectedItem is not CustomPartDefinition part)
            {
                ShowWarning("Please select a standard catalog part.");
                return;
            }

            if (cmbCatalogPart.Items.Count == 0)
            {
                string catLabel = (cmbCatalogCategory.SelectedItem as CatalogCategoryOption)?.Display ?? "category";
                ShowWarning($"No parts in {catLabel} for Class {cmbCatalogPressureClass.SelectedItem ?? "150"}.");
                return;
            }

            try
            {
                string dwg = DrawingContext.RequireActiveDrawingPath();
                ValveProject project = DocumentStore.LoadOrCreate(
                    dwg, Path.GetFileNameWithoutExtension(dwg));

                double projectDn = GetProjectDn(project);
                if (projectDn <= 0)
                {
                    ShowWarning("Set DN in Catalog Project and click Apply.");
                    return;
                }

                double? dn = part.ParametricDN ? projectDn : null;
                Guid nodeId = CatalogPartService.Insert(dwg, project, part, dn);
                _selectedNodeId = nodeId;
                RefreshSceneTree();
                SelectTreeNode(nodeId);
                PrimitiveNode? inserted = project.FindNode(nodeId);
                if (inserted != null)
                    LoadNodeEditor(inserted);

                var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                if (doc != null)
                {
                    RefreshGeneratedPython(project);
                    SceneRebuildService.RebuildInModalCommand(doc, dwg, project);
                    lblSceneStatus.Text = $"Inserted {part.DisplayName} — rebuild sent to Plant 3D.";
                }
                else
                {
                    lblSceneStatus.Text = $"Inserted {part.DisplayName} (no active document for rebuild).";
                }

                lblStatus.Text = $"Scene: {project.Parts.Count} part(s).";
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
                string dwg = DrawingContext.RequireActiveDrawingPath();
                ValveProject project = DocumentStore.LoadOrCreate(
                    dwg, Path.GetFileNameWithoutExtension(dwg));
                Guid nodeId = Services.PrimitiveService.Insert(dwg, project, primitive);
                _selectedNodeId = nodeId;
                RefreshSceneTree();
                SelectTreeNode(nodeId);
                PrimitiveNode? inserted = project.FindNode(nodeId);
                if (inserted != null)
                    LoadNodeEditor(inserted);
                lblSceneStatus.Text =
                    $"Inserted {inserted?.Name ?? "primitive"} ({primitive.DisplayName}). Rebuild queued.";
                TriggerRebuild(project, quiet: true);
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void btnDeployCatalog_Click(object sender, EventArgs e)
        {
            try
            {
                btnDeployCatalog.Enabled = false;
                Cursor = Cursors.WaitCursor;

                string dwg = DrawingContext.RequireActiveDrawingPath();
                ValveProject project = DocumentStore.LoadOrCreate(
                    dwg, Path.GetFileNameWithoutExtension(dwg));

                ValidationResult preflight = CatalogPreflightService.ValidateForDeploy(project);
                if (!preflight.IsValid)
                {
                    ShowWarning(string.Join("\n", preflight.Errors));
                    return;
                }

                Autodesk.AutoCAD.ApplicationServices.Document? doc =
                    Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                CatalogDeployFullResult result = CatalogDeployFullService.Deploy(
                    dwg,
                    project,
                    ResolveCatalogExportDirectory,
                    doc,
                    allowExportWithWarnings: true);

                if (result.Success)
                {
                    if (project.Parts.Count > 0)
                    {
                        string catalogCode = ComposerLiveScriptService.GenerateCatalogPackage(project);
                        txtGeneratedCode.Text = catalogCode;
                        tabMain.SelectedTab = tabCode;
                    }

                    lblStatus.Text = result.Message;
                    NotifyPluginUpdateIfNeeded(result.PluginDeploy);
                }
                else
                {
                    lblStatus.Text = result.Message;
                    ShowWarning(result.Message);
                }
            }
            catch (OperationCanceledException)
            {
                // export folder picker cancelled
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
            finally
            {
                btnDeployCatalog.Enabled = true;
                Cursor = Cursors.Default;
            }
        }

        private void btnPublishCatalog_Click(object sender, EventArgs e)
        {
            try
            {
                btnPublishCatalog.Enabled = false;
                Cursor = Cursors.WaitCursor;

                string dwg = DrawingContext.RequireActiveDrawingPath();
                ValveProject project = DocumentStore.LoadOrCreate(
                    dwg, Path.GetFileNameWithoutExtension(dwg));

                ValidationResult preflight = CatalogPreflightService.ValidateForExcelPublish(project);
                if (!preflight.IsValid)
                {
                    ShowWarning(string.Join("\n", preflight.Errors));
                    return;
                }

                string outputPath = ResolveCatalogExcelOutputPath(project, dwg);
                CatalogPublishResult result = CatalogPublishService.Publish(
                    dwg,
                    project,
                    outputPath,
                    allowExportWithWarnings: true);

                if (result.Success)
                {
                    lblStatus.Text = result.Message;
                }
                else
                {
                    lblStatus.Text = result.Message;
                    ShowWarning(result.Message);
                }
            }
            catch (OperationCanceledException)
            {
                // save dialog cancelled
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
            finally
            {
                btnPublishCatalog.Enabled = true;
                Cursor = Cursors.Default;
            }
        }

        private void btnTestCatalog_Click(object sender, EventArgs e)
        {
            try
            {
                string dwg = DrawingContext.RequireActiveDrawingPath();
                ValveProject project = DocumentStore.LoadOrCreate(
                    dwg, Path.GetFileNameWithoutExtension(dwg));
                CatalogTestResult test = CatalogTestService.BuildTestCommand(project);

                if (!test.CanRun)
                {
                    MessageBox.Show(
                        test.Message,
                        "Test Catalog",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                Autodesk.AutoCAD.ApplicationServices.Document? doc =
                    Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                if (doc == null || !CatalogTestService.TryQueueTest(doc, project))
                {
                    ShowWarning("Could not queue testacpscript.");
                    return;
                }

                lblStatus.Text = $"Testing {test.ScriptName} — see command line for result.";
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private static bool ConfirmPreflight(ValidationResult preflight, string title, out bool allowWarnings)
        {
            allowWarnings = true;
            if (!preflight.IsValid)
            {
                MessageBox.Show(
                    "Cannot continue:\n\n" + string.Join("\n", preflight.Errors),
                    title,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                allowWarnings = false;
                return false;
            }

            var warnings = preflight.Warnings.ToList();
            if (warnings.Count == 0)
                return true;

            string text = "Pre-flight warnings:\n\n• " + string.Join("\n• ", warnings) +
                          "\n\nContinue?";
            DialogResult dr = MessageBox.Show(
                text,
                title,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            allowWarnings = dr == DialogResult.Yes;
            return allowWarnings;
        }

        private static bool ConfirmPublishPreflight(ValidationResult preflight, out bool allowWarnings) =>
            ConfirmPreflight(preflight, "Publish Catalog", out allowWarnings);

        private void btnGenerateCode_Click(object sender, EventArgs e)
        {
            try
            {
                string dwg = DrawingContext.RequireActiveDrawingPath();
                ValveProject project = DocumentStore.LoadOrCreate(
                    dwg, Path.GetFileNameWithoutExtension(dwg));

                DocumentStore.Save(dwg, project);
                CatalogPackage package = ComposerLiveScriptService.BuildCatalogPackage(project);
                if (CatalogGroupResolver.WouldRemapValveToFitting(project.CatalogGroup, project.Ports))
                {
                    MessageBox.Show(
                        "Plant group is Valve but ports are butt-weld (BV) or socket-weld (SW).\n\n"
                        + "Plant 3D Spec Editor treats Group=\"Valve\" as flanged (FL) and ignores BV — "
                        + "this causes FL in Spec Editor and \"Can't find symbol\" when placing.\n\n"
                        + "Export will use Group=\"Fitting\" (same as Tee Equal BW). Geometry is unchanged.",
                        "Plant 3D Catalog Composer",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }

                if (package.PortManagerPortCount == 0 &&
                    !StandardCatalogGuard.IsStandardReferenceScene(project) &&
                    MessageBox.Show(
                        "Port Manager has no ports saved in the scene graph.\n\n"
                        + "Generated catalog will use library built-in ports (flange) or TODO defaults.\n"
                        + "Add a port in Port Manager → Apply, then Generate again for custom prim.set_port.\n\n"
                        + "Continue export?",
                        "Plant 3D Catalog Composer",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning) != DialogResult.Yes)
                {
                    return;
                }

                string exportRoot = ResolveCatalogExportDirectory(dwg);
                IReadOnlyList<string> exported = CatalogExportService.Export(package, exportRoot);
                string catalogCode = package.ToDisplayText();
                ComposerLiveScriptService.WriteCatalogPackage(project, catalogCode);
                txtGeneratedCode.Text = catalogCode;
                tabMain.SelectedTab = tabCode;

                string partFolder = Path.Combine(exportRoot, package.ExportFolderName);
                string status = package.IsStandardPortReference
                    ? $"Port reference → {partFolder} (standard {package.StandardPartId} unchanged)"
                    : $"Exported {exported.Count} file(s) → {partFolder}; ScriptGroup/variants synced — Deploy Catalog for Spec Editor";
                lblStatus.Text = status;
                Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager
                    .MdiActiveDocument?.Editor.WriteMessage(
                        $"\nP3D Composer: catalog exported → {partFolder} ({exported.Count} files)");
            }
            catch (OperationCanceledException)
            {
                // User cancelled folder picker when deploy.json is not configured.
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private static string ResolveCatalogExcelOutputPath(ValveProject project, string dwg)
        {
            string catalogName = CatalogProjectService.SanitizeCatalogName(project.ValveName ?? "");
            if (string.IsNullOrWhiteSpace(catalogName))
                catalogName = "CatalogBuilder";

            string initialDir = ResolveCatalogExportInitialDirectory(dwg);
            using var dialog = new SaveFileDialog
            {
                Title = "Publish Catalog — save Catalog Builder Excel",
                Filter = "Excel workbook (*.xlsx)|*.xlsx",
                FileName = catalogName + ".xlsx",
                InitialDirectory = initialDir,
            };
            if (dialog.ShowDialog() != DialogResult.OK)
                throw new OperationCanceledException("Excel export cancelled.");

            return dialog.FileName;
        }

        private static string ResolveCatalogExportDirectory(string dwg)
        {
            string? devParts = ProjectPaths.TryResolveDevPartsDir();
            if (devParts != null)
                return devParts;

            string partsDir = ProjectPaths.ResolvePartsDir();
            if (Directory.Exists(partsDir))
                return partsDir;

            using var folderDialog = new FolderBrowserDialog
            {
                Description = "Select folder to export catalog files (creates <PartName>/ subfolder)",
                UseDescriptionForTitle = true,
                InitialDirectory = ResolveCatalogExportInitialDirectory(dwg),
            };
            if (folderDialog.ShowDialog() != DialogResult.OK)
                throw new OperationCanceledException("Export cancelled.");

            return folderDialog.SelectedPath;
        }

        private static string ResolveCatalogExportInitialDirectory(string dwg)
        {
            string partsDir = ProjectPaths.ResolvePartsDir();
            if (Directory.Exists(partsDir))
                return partsDir;

            string? dwgDir = Path.GetDirectoryName(dwg);
            if (!string.IsNullOrEmpty(dwgDir) && Directory.Exists(dwgDir))
                return dwgDir;

            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            try
            {
                string dwg = DrawingContext.RequireActiveDrawingPath();
                ValveProject project = DocumentStore.LoadOrCreate(
                    dwg, Path.GetFileNameWithoutExtension(dwg));
                DocumentStore.Save(dwg, project);

                ValidationResult validation = ProjectValidator.Validate(project);
                if (!ConfirmExport(validation))
                    return;

                using var dialog = new SaveFileDialog
                {
                    Title = "Export Scene Graph JSON",
                    Filter = "Scene JSON (*.scene.json)|*.scene.json|JSON (*.json)|*.json",
                    FileName = (string.IsNullOrEmpty(project.ValveName) ? "valve" : project.ValveName) + ".scene.json",
                    InitialDirectory = string.IsNullOrEmpty(dwg)
                        ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                        : Path.GetDirectoryName(dwg),
                };
                if (dialog.ShowDialog() != DialogResult.OK)
                    return;

                File.WriteAllText(dialog.FileName, JsonCodec.Serialize(project));
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
                string dwg = DrawingContext.RequireActiveDrawingPath();

                using var dialog = new OpenFileDialog
                {
                    Title = "Import Scene Graph JSON",
                    Filter = "Scene JSON (*.scene.json)|*.scene.json|JSON (*.json)|*.json",
                    InitialDirectory = string.IsNullOrEmpty(dwg)
                        ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                        : Path.GetDirectoryName(dwg),
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
                    if (!ConfirmWithWarnings("Import this scene graph?", validation.Warnings.ToList()))
                        return;
                }

                SceneImportExport.ReplaceProject(dwg, imported);
                LoadSkeletonFields(imported);
                lblStatus.Text =
                    $"Imported {imported.Parts.Count} parts, {imported.Operations.Count} operations.";

                if (MessageBox.Show(
                        "Rebuild geometry in Plant 3D from the imported scene graph?",
                        "Plant 3D Catalog Composer",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    TriggerRebuild(imported, quiet: true);
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
                string dwg = DrawingContext.RequireActiveDrawingPath();
                ValveProject project = DocumentStore.LoadOrCreate(
                    dwg, Path.GetFileNameWithoutExtension(dwg));

                if (project.Parts.Count > 0)
                {
                    ValidationResult validation = ProjectValidator.Validate(project);
                    if (!validation.IsValid)
                    {
                        ShowValidationBlocked(validation);
                        return;
                    }
                }

                TriggerRebuild(project, quiet: true);
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void TriggerRebuild(ValveProject project, bool quiet = false)
        {
            string dwg = DrawingContext.RequireActiveDrawingPath();
            RefreshGeneratedPython(project);

            var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            if (doc != null)
                Services.SceneRebuildService.RequestFromPanel(doc, dwg, project);

            lblStatus.Text = $"Scene saved: {project.Parts.Count} part(s), {project.Operations.Count} boolean op(s).";
            lblSceneStatus.Text = project.Parts.Count == 0
                ? "Insert a primitive to build catalog geometry."
                : $"Scene saved — rebuild when CAD idle ({project.Parts.Count} part(s)).";
            RefreshPortVisuals(project);
        }

        /// <summary>Modal rebuild after move/rotate so preview updates immediately.</summary>
        private void RebuildSceneNow(string dwg, ValveProject project)
        {
            RefreshGeneratedPython(project);

            var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                TriggerRebuild(project, quiet: true);
                return;
            }

            Services.SceneRebuildService.RebuildInModalCommand(doc, dwg, project);
            lblStatus.Text = $"Scene saved: {project.Parts.Count} part(s), {project.Operations.Count} boolean op(s).";
            lblSceneStatus.Text = $"Rebuilt {project.Parts.Count} part(s) with current transform.";
            RefreshPortVisuals(project);
        }

        private void tabCode_Enter(object sender, EventArgs e)
        {
            try
            {
                string? dwg = DrawingContext.GetActiveDrawingPath();
                if (dwg == null)
                    return;
                ValveProject project = DocumentStore.LoadOrCreate(
                    dwg, Path.GetFileNameWithoutExtension(dwg));
                RefreshGeneratedCode(project, preferCatalog: true);
            }
            catch
            {
                // ignore when no drawing
            }
        }

        private void RefreshGeneratedCode(ValveProject project, bool preferCatalog)
        {
            txtGeneratedCode.Text = preferCatalog
                ? ComposerLiveScriptService.GenerateCatalogPackage(project)
                : ComposerLiveScriptService.GeneratePreview(project);
        }

        /// <summary>Writes composer_live.py for rebuild; keeps catalog code display in sync with the scene.</summary>
        private void RefreshGeneratedPython(ValveProject project)
        {
            ComposerLiveScriptService.Write(project);
            RefreshCatalogCodeDisplay(project);
        }

        private void RefreshCatalogCodeDisplay(ValveProject project) =>
            RefreshGeneratedCode(project, preferCatalog: true);

        private void tabBooleans_Enter(object sender, EventArgs e) => RefreshBooleanUi();

        private void btnInsertBoolCutter_Click(object sender, EventArgs e) =>
            InsertBoolCutter(addSubtract: false);

        private void btnBoolCutterSubtract_Click(object sender, EventArgs e) =>
            InsertBoolCutter(addSubtract: true);

        private void InsertBoolCutter(bool addSubtract)
        {
            if (cmbBoolCutter.SelectedItem is not PrimitiveDefinition primitive)
            {
                ShowWarning("Select a fillet/chamfer cutter type.");
                return;
            }

            if (addSubtract && _selectedNodeId == null)
            {
                ShowWarning("Select a target primitive on the Scene tab first.");
                return;
            }

            try
            {
                string dwg = DrawingContext.RequireActiveDrawingPath();
                ValveProject project = DocumentStore.LoadOrCreate(
                    dwg, Path.GetFileNameWithoutExtension(dwg));
                Guid cutterId = PrimitiveService.Insert(dwg, project, primitive);

                if (addSubtract)
                {
                    BooleanGraph.AddOperation(
                        project,
                        BooleanOpType.SUBTRACT,
                        _selectedNodeId!.Value,
                        new List<Guid> { cutterId });
                    DocumentStore.Save(dwg, project);
                    lblBoolStatus.Text =
                        $"Inserted {primitive.DisplayName} and subtracted from target.";
                }
                else
                {
                    lblBoolStatus.Text =
                        $"Inserted {primitive.DisplayName} — check it as a tool, then Add.";
                }

                RefreshBooleanUi();
                CheckBoolTool(cutterId);
                TriggerRebuild(project, quiet: true);
            }
            catch (Exception ex)
            {
                ShowBoolError(ex);
            }
        }

        private void CheckBoolTool(Guid nodeId)
        {
            for (int i = 0; i < clbBoolTools.Items.Count; i++)
            {
                if (clbBoolTools.Items[i] is NodeListItem item && item.Id == nodeId)
                {
                    clbBoolTools.SetItemChecked(i, true);
                    break;
                }
            }
        }

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
                string dwg = DrawingContext.RequireActiveDrawingPath();
                ValveProject project = DocumentStore.LoadOrCreate(
                    dwg, Path.GetFileNameWithoutExtension(dwg));
                BooleanGraph.AddOperation(project, opType, _selectedNodeId.Value, toolIds);
                DocumentStore.Save(dwg, project);
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
                string dwg = DrawingContext.RequireActiveDrawingPath();
                ValveProject project = DocumentStore.LoadOrCreate(
                    dwg, Path.GetFileNameWithoutExtension(dwg));
                BooleanGraph.RemoveOperation(project, order);
                DocumentStore.Save(dwg, project);
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
                string dwg = DrawingContext.RequireActiveDrawingPath();
                ValveProject project = DocumentStore.LoadOrCreate(
                    dwg, Path.GetFileNameWithoutExtension(dwg));
                TriggerRebuild(project, quiet: true);
                lblBoolStatus.Text = $"Rebuild prepared for {project.Operations.Count} operation(s).";
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

                    var item = new ListViewItem(op.Order.ToString()) { Tag = op.Order };
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

        private void btnDeleteNode_Click(object sender, EventArgs e) => DeleteSelectedSceneNode();

        private void TreeScene_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                DeleteSelectedSceneNode();
                e.Handled = true;
            }
        }

        private void DeleteSelectedSceneNode()
        {
            if (_selectedNodeId == null)
            {
                ShowWarning("Select a primitive in the scene tree first.");
                return;
            }

            PrimitiveNode? node = GetProject()?.FindNode(_selectedNodeId.Value);
            if (node == null)
                return;

            try
            {
                string dwg = DrawingContext.RequireActiveDrawingPath();
                ValveProject project = DocumentStore.LoadOrCreate(
                    dwg, Path.GetFileNameWithoutExtension(dwg));
                Guid deletedId = _selectedNodeId.Value;
                Guid? nextId = SceneGraphHelpers.GetSelectionAfterDelete(project, deletedId);

                SceneGraphEditor.Delete(dwg, project, deletedId);

                _selectedNodeId = nextId;
                if (!nextId.HasValue)
                    ClearNodeEditor();

                RefreshSceneTree();
                RefreshBooleanUi();
                lblSceneStatus.Text = nextId.HasValue
                    ? $"Deleted {node.Name}. Select next or press Delete again."
                    : $"Deleted {node.Name}. Updating preview...";
                TriggerRebuild(project, quiet: true);
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
                string dwg = DrawingContext.RequireActiveDrawingPath();
                ValveProject project = DocumentStore.LoadOrCreate(
                    dwg, Path.GetFileNameWithoutExtension(dwg));
                Guid newId = SceneGraphEditor.Duplicate(dwg, project, _selectedNodeId.Value);
                _selectedNodeId = newId;
                RefreshSceneTree();
                SelectTreeNode(newId);
                lblSceneStatus.Text = "Duplicated primitive. Updating preview...";
                TriggerRebuild(project, quiet: true);
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
                string dwg = DrawingContext.RequireActiveDrawingPath();
                ValveProject project = DocumentStore.LoadOrCreate(
                    dwg, Path.GetFileNameWithoutExtension(dwg));
                PrimitiveNode? node = project.FindNode(_selectedNodeId.Value);
                if (node == null)
                    return;

                SceneGraphEditor.ResolveExpressions(node, project.Parameters);
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
                string dwg = DrawingContext.RequireActiveDrawingPath();
                ValveProject project = DocumentStore.LoadOrCreate(
                    dwg, Path.GetFileNameWithoutExtension(dwg));
                PrimitiveNode? node = project.FindNode(_selectedNodeId.Value);
                if (node == null)
                    return;

                if (!TryApplyEditorToNode(node, out string? error))
                {
                    ShowWarning(error ?? "Invalid property values.");
                    return;
                }

                DocumentStore.Save(dwg, project);
                LoadNodeEditor(node);
                RefreshSceneTree();
                SelectTreeNode(node.Id);
                lblSceneStatus.Text = $"Applied changes to {node.Name}. Updating preview...";
                TriggerRebuild(project, quiet: true);
            }
            catch (Exception ex)
            {
                ShowSceneError(ex);
            }
        }

        private void btnPosXPlus_Click(object sender, EventArgs e) => JogPosition(+GetPosStepX(), 0, 0);
        private void btnPosXMinus_Click(object sender, EventArgs e) => JogPosition(-GetPosStepX(), 0, 0);
        private void btnPosYPlus_Click(object sender, EventArgs e) => JogPosition(0, +GetPosStepY(), 0);
        private void btnPosYMinus_Click(object sender, EventArgs e) => JogPosition(0, -GetPosStepY(), 0);
        private void btnPosZPlus_Click(object sender, EventArgs e) => JogPosition(0, 0, +GetPosStepZ());
        private void btnPosZMinus_Click(object sender, EventArgs e) => JogPosition(0, 0, -GetPosStepZ());

        private void btnRotX_Click(object sender, EventArgs e) => JogRotation('X', GetAngleStep());
        private void btnRotY_Click(object sender, EventArgs e) => JogRotation('Y', GetAngleStep());
        private void btnRotZ_Click(object sender, EventArgs e) => JogRotation('Z', GetAngleStep());

        private void rdoRotAxisMode_CheckedChanged(object? sender, EventArgs e)
        {
            if (sender is RadioButton radio && !radio.Checked)
                return;
            UpdateRotationAxisTooltips();
        }

        private void UpdateRotationAxisTooltips()
        {
            bool world = rdoRotWorld.Checked;
            string scope = world ? "world" : "object local";
            _toolTip.SetToolTip(btnRotX, $"Rotate around {scope} X");
            _toolTip.SetToolTip(btnRotY, $"Rotate around {scope} Y");
            _toolTip.SetToolTip(btnRotZ, $"Rotate around {scope} Z");
            _toolTip.SetToolTip(rdoRotWorld,
                "Orbit part center about CAD (0,0,0), then rotate at the new position (WCS axis).");
            _toolTip.SetToolTip(rdoRotLocal,
                "Rotate around the part's own axes at its current position only (no orbit).");
        }

        private double GetPosStepX() => (double)numPosStepX.Value;
        private double GetPosStepY() => (double)numPosStepY.Value;
        private double GetPosStepZ() => (double)numPosStepZ.Value;

        private static void SetAxisStep(NumericUpDown control, double component)
        {
            decimal step = (decimal)Math.Round(Math.Abs(component), 0, MidpointRounding.AwayFromZero);
            if (step > control.Maximum)
                step = control.Maximum;
            control.Value = step;
        }
        private double GetAngleStep() => (double)numAngleStep.Value;

        private void btnPickPosStep_Click(object sender, EventArgs e)
        {
            Autodesk.AutoCAD.ApplicationServices.Document? doc =
                Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                ShowWarning("Open a Plant 3D drawing first.");
                return;
            }

            if (!string.IsNullOrEmpty(doc.CommandInProgress))
            {
                ShowWarning("Finish the current command, then use Measure.");
                return;
            }

            DistancePickSession.Begin(DistancePickTarget.PositionStep);
            lblStatus.Text = "Pick two points — step will use |ΔX|, |ΔY|, |ΔZ| per axis.";
            doc.SendStringToExecute("P3DCOMPPICKDIST\n", true, false, false);
        }

        private void btnAlignPos_Click(object sender, EventArgs e)
        {
            if (_selectedNodeId == null)
            {
                ShowWarning("Select a primitive in the scene tree first.");
                return;
            }

            Autodesk.AutoCAD.ApplicationServices.Document? doc =
                Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                ShowWarning("Open a Plant 3D drawing first.");
                return;
            }

            if (!string.IsNullOrEmpty(doc.CommandInProgress))
            {
                ShowWarning("Finish the current command, then use Align.");
                return;
            }

            DistancePickSession.Begin(DistancePickTarget.PositionMove);
            lblStatus.Text = "Align: pick on part, then target point...";
            doc.SendStringToExecute("P3DCOMPPICKDIST\n", true, false, false);
        }

        private void OnDisplacementPickSessionCompleted(DisplacementPickResult result, DistancePickTarget target) =>
            PaletteManager.NotifyDisplacementPicked(result, target);

        public void OnDisplacementPicked(DisplacementPickResult result, DistancePickTarget target)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<DisplacementPickResult, DistancePickTarget>(OnDisplacementPicked), result, target);
                return;
            }

            if (target == DistancePickTarget.PositionStep)
            {
                SetAxisStep(numPosStepX, result.Dx);
                SetAxisStep(numPosStepY, result.Dy);
                SetAxisStep(numPosStepZ, result.Dz);
                lblStatus.Text =
                    $"Step: X={numPosStepX.Value:0} Y={numPosStepY.Value:0} Z={numPosStepZ.Value:0} mm " +
                    $"(|ΔX|, |ΔY|, |ΔZ| from CAD).";
                return;
            }

            if (target != DistancePickTarget.PositionMove || _selectedNodeId == null)
                return;

            try
            {
                string dwg = DrawingContext.RequireActiveDrawingPath();
                ValveProject project = DocumentStore.LoadOrCreate(
                    dwg, Path.GetFileNameWithoutExtension(dwg));
                PrimitiveNode? node = project.FindNode(_selectedNodeId.Value);
                if (node == null)
                    return;

                TransformMath.TranslateWorld(node, result.Dx, result.Dy, result.Dz);
                DocumentStore.Save(dwg, project);
                LoadNodeEditor(node);
                lblSceneStatus.Text =
                    $"Aligned {node.Name}: Δ ({Format(result.Dx)}, {Format(result.Dy)}, {Format(result.Dz)}) mm.";
                lblStatus.Text = lblSceneStatus.Text;
                RebuildSceneNow(dwg, project);
                RefreshPortVisuals(project);
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void JogPosition(double dx, double dy, double dz)
        {
            if (_selectedNodeId == null)
            {
                ShowWarning("Select a primitive in the scene tree first.");
                return;
            }

            try
            {
                string dwg = DrawingContext.RequireActiveDrawingPath();
                ValveProject project = DocumentStore.LoadOrCreate(
                    dwg, Path.GetFileNameWithoutExtension(dwg));
                PrimitiveNode? node = project.FindNode(_selectedNodeId.Value);
                if (node == null)
                    return;

                TransformMath.TranslateWorld(node, dx, dy, dz);
                DocumentStore.Save(dwg, project);
                LoadNodeEditor(node);
                lblSceneStatus.Text =
                    $"Moved {node.Name} by ({Format(dx)}, {Format(dy)}, {Format(dz)}) mm.";
                RebuildSceneNow(dwg, project);
                RefreshPortVisuals(project);
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
                ShowWarning("Enter a non-zero angle (Deg). Use negative values to reverse direction.");
                return;
            }

            try
            {
                string dwg = DrawingContext.RequireActiveDrawingPath();
                ValveProject project = DocumentStore.LoadOrCreate(
                    dwg, Path.GetFileNameWithoutExtension(dwg));
                PrimitiveNode? node = project.FindNode(_selectedNodeId.Value);
                if (node == null)
                    return;

                bool worldAxes = rdoRotWorld.Checked;
                if (worldAxes)
                    TransformMath.RotateWorld(node, axis, degrees);
                else if (node.CatalogFrameRotation is { Length: 9 } cf
                    && !TransformMath.IsIdentityRotation(cf))
                    TransformMath.RotateLocalInBodyFrame(node, axis, degrees, cf);
                else
                    TransformMath.RotateLocal(node, axis, degrees);

                node.RotationJogs ??= new List<RotationJog>();
                node.RotationJogs.Add(new RotationJog
                {
                    World = worldAxes,
                    Axis = axis,
                    Degrees = degrees,
                });

                DocumentStore.Save(dwg, project);
                LoadNodeEditor(node);
                string scope = worldAxes ? "world" : "object";
                lblSceneStatus.Text = worldAxes
                    ? $"World {axis} {degrees:+0.###;-0.###;0}° — rigid @ (0,0,0): origin ({Format(node.Origin[0])}, {Format(node.Origin[1])}, {Format(node.Origin[2])}) mm."
                    : $"Object {axis} {degrees:+0.###;-0.###;0}° @ origin ({Format(node.Origin[0])}, {Format(node.Origin[1])}, {Format(node.Origin[2])}) mm.";

                var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                doc?.Editor.WriteMessage(
                    $"\nP3D Composer: {scope.ToUpperInvariant()} {axis} {degrees:+0.###;-0.###;0} deg — jogs={node.RotationJogs.Count}"
                    + (worldAxes
                        ? $" — origin ({Format(node.Origin[0])}, {Format(node.Origin[1])}, {Format(node.Origin[2])})"
                        : ""));
                RebuildSceneNow(dwg, project);
                RefreshPortVisuals(project);
            }
            catch (Exception ex)
            {
                ShowSceneError(ex);
            }
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
            Point client = treeScene.PointToClient(new Point(e.X, e.Y));
            TreeNode? target = treeScene.GetNodeAt(client);

            Guid? newParent = target?.Tag is Guid g ? g : null;
            if (target != null && target == dragged)
                return;

            try
            {
                string dwg = DrawingContext.RequireActiveDrawingPath();
                ValveProject project = DocumentStore.LoadOrCreate(
                    dwg, Path.GetFileNameWithoutExtension(dwg));
                SceneGraphEditor.Reparent(dwg, project, draggedId, newParent);
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
            string kind = node.Kind == SceneNodeKind.Catalog
                ? CustomPartCatalog.FindById(node.CatalogPartId)?.DisplayName ?? node.CatalogPartId ?? "Catalog"
                : node.Type.ToString();
            var tn = new TreeNode($"{node.Name} [{kind}]") { Tag = node.Id };
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
            lblNodeType.Text = node.Kind == SceneNodeKind.Catalog
                ? $"Catalog: {CustomPartCatalog.FindById(node.CatalogPartId)?.DisplayName ?? node.CatalogPartId}"
                : $"Primitive: {node.Type}";

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

                if (!TryParseNodeParameter(node, paramName, valueText, out double value, out string? paramError))
                {
                    error = paramError;
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
            string? dwg = DrawingContext.GetActiveDrawingPath();
            if (dwg == null)
                return null;
            return DocumentStore.LoadOrCreate(dwg, Path.GetFileNameWithoutExtension(dwg));
        }

        private static string Format(double value) =>
            value.ToString("0.###", CultureInfo.InvariantCulture);

        private static bool TryParseNodeParameter(
            PrimitiveNode node,
            string paramName,
            string text,
            out double value,
            out string? error)
        {
            error = null;
            if (!TryParseDouble(text, out value))
            {
                error = $"Parameter '{paramName}' must be a valid number.";
                return false;
            }

            if (node.Kind == SceneNodeKind.Catalog)
            {
                if (paramName.Equals("DN", StringComparison.OrdinalIgnoreCase) && value <= 0)
                {
                    error = $"Parameter '{paramName}' must be > 0.";
                    return false;
                }

                if (value < 0)
                {
                    error = $"Parameter '{paramName}' cannot be negative.";
                    return false;
                }

                return true;
            }

            if (PrimitiveParameterUnits.RequiresPositiveValue(node.Type, paramName))
            {
                if (value <= 0)
                {
                    error = $"Parameter '{paramName}' needs a positive value (mm).";
                    return false;
                }

                return true;
            }

            if (value < 0)
            {
                error = $"Parameter '{paramName}' cannot be negative.";
                return false;
            }

            return true;
        }

        private static bool TryParsePositive(string text, out double value) =>
            double.TryParse(
                text.Trim().Replace(',', '.'),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out value) && value > 0;

        private static bool TryParseDouble(string text, out double value) =>
            double.TryParse(
                text.Trim().Replace(',', '.'),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out value);

        private void ShowError(Exception ex)
        {
            lblStatus.Text = string.Empty;
            MessageBox.Show(ex.Message, "Plant 3D Catalog Composer",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void ShowSceneError(Exception ex)
        {
            lblSceneStatus.Text = string.Empty;
            MessageBox.Show(ex.Message, "Plant 3D Catalog Composer",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static void NotifyPluginUpdateIfNeeded(PluginDeployResult? plugin)
        {
            if (plugin?.BuiltDllIsNewerThanLoaded != true)
                return;

            string body = plugin.CopiedToBundle
                ? "Plugin DLL updated — restart Plant 3D to load the new version."
                : "Plugin DLL is locked (Plant 3D is using it).\n"
                  + "Close Plant 3D, run dotnet build, then open CAD again.";

            if (!plugin.CopiedToNetload)
            {
                body += Environment.NewLine + Environment.NewLine
                    + "NETLOAD (after restart):" + Environment.NewLine
                    + plugin.NetloadDllPath;
            }

            MessageBox.Show(
                body,
                "Plugin Update",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private static void ShowWarning(string message)
        {
            MessageBox.Show(message, "Plant 3D Catalog Composer",
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

            MessageBox.Show(sb.ToString(), "Plant 3D Catalog Composer",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static bool ConfirmWithWarnings(string prompt, IList<string> warnings)
        {
            var sb = new StringBuilder();
            sb.AppendLine(prompt);
            sb.AppendLine();
            sb.AppendLine("Warnings:");
            foreach (string w in warnings)
                sb.AppendLine("• " + w);

            return MessageBox.Show(sb.ToString(), "Plant 3D Catalog Composer",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
        }

        private void ShowBoolError(Exception ex)
        {
            lblBoolStatus.Text = string.Empty;
            MessageBox.Show(ex.Message, "Plant 3D Catalog Composer",
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
