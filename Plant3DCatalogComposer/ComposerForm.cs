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
        private static Color TabPageBackColor => ComposerTheme.Window;

        private readonly IReadOnlyList<CustomPartDefinition> _catalogPartChoices;
        private readonly IReadOnlyList<PrimitiveDefinition> _primitiveChoices;
        private readonly ToolTip _toolTip = new();
        private Guid? _selectedNodeId;

        public ComposerForm()
        {
            SuspendLayout();
            try
            {
            InitializeComponent();
            tabMain.SuspendLayout();
            try
            {
            ConfigureForPaletteHost();

            foreach (PipeSizeOption size in PipeSizeCatalog.NominalSizes)
                cmbCatalogDN.Items.Add(size);
            foreach (string pc in PipeSizeCatalog.PressureClasses)
                cmbCatalogPressureClass.Items.Add(pc);

            cmbCatalogDN.DisplayMember = nameof(PipeSizeOption.Display);
            SelectDnCombo(cmbCatalogDN, 100);
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
                "Apply Part Family, seed dimensions, export catalog files, register part.json + Excel template sheet");
            _toolTip.SetToolTip(btnDeployCatalog,
                "Copy library to CustomScripts, clear __pycache__, register scripts (PLANTREGISTERCUSTOMSCRIPTS, wait).");
            _toolTip.SetToolTip(
                btnPublishCatalog,
                "Export Catalog Builder Excel, register scripts, then open Catalog Builder.");
            _toolTip.SetToolTip(
                btnTestCatalog,
                "Run deployed CUST_* on the current drawing (auto-runs after Deploy Catalog)");
            _toolTip.SetToolTip(btnRebuildScene, "Scene preview (scene_builder) on the current drawing");
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
            InitializeDimensionsTab();
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
            finally
            {
                tabMain.ResumeLayout(performLayout: false);
            }
            }
            finally
            {
                ResumeLayout(performLayout: true);
            }
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
            ComposerTheme.Refresh();
            BackColor = ComposerTheme.Window;
            ForeColor = ComposerTheme.Text;
            StyleTabColors();
        }

        private void StyleTabColors()
        {
            tabMain.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabMain.DrawItem += TabMain_DrawItem;
            tabMain.SelectedIndexChanged += (_, _) => tabMain.Invalidate();
            tabMain.Padding = new Point(10, 4);
            tabMain.BackColor = ComposerTheme.Window;

            foreach (TabPage page in tabMain.TabPages)
            {
                page.UseVisualStyleBackColor = false;
                page.BackColor = ComposerTheme.Window;
                ApplyThemeToTree(page);
            }
        }

        /// <summary>Recursively theme every control so the palette matches the AutoCAD UI.</summary>
        private static void ApplyThemeToTree(Control root)
        {
            foreach (Control child in root.Controls)
            {
                switch (child)
                {
                    case TextBox tb:
                        tb.BackColor = ComposerTheme.Field;
                        tb.ForeColor = ComposerTheme.Text;
                        tb.BorderStyle = BorderStyle.FixedSingle;
                        break;
                    case ComboBox cb:
                        cb.BackColor = ComposerTheme.Field;
                        cb.ForeColor = ComposerTheme.Text;
                        cb.FlatStyle = FlatStyle.Flat;
                        break;
                    case DataGridView dgv:
                        StyleGrid(dgv);
                        break;
                    case Button btn when btn.Tag as string == "accent":
                        // Accent buttons keep their own colours (styled elsewhere).
                        break;
                    case Button btn:
                        btn.FlatStyle = FlatStyle.Flat;
                        btn.FlatAppearance.BorderColor = ComposerTheme.Border;
                        btn.BackColor = ComposerTheme.GroupBackground;
                        btn.ForeColor = ComposerTheme.Text;
                        break;
                    case GroupBox gb:
                        gb.BackColor = ComposerTheme.GroupBackground;
                        gb.ForeColor = ComposerTheme.Text;
                        break;
                    case Label lbl:
                        lbl.BackColor = Color.Transparent;
                        lbl.ForeColor = lbl.ForeColor == SystemColors.GrayText
                            ? ComposerTheme.SubtleText
                            : ComposerTheme.Text;
                        break;
                    case Panel or CheckBox or RadioButton:
                        child.BackColor = ComposerTheme.Window;
                        child.ForeColor = ComposerTheme.Text;
                        break;
                }

                if (child.HasChildren)
                    ApplyThemeToTree(child);
            }
        }

        private static void StyleGrid(DataGridView dgv)
        {
            // Grids tagged "light" keep their own white/black styling for readability.
            if (dgv.Tag as string == "light")
                return;

            dgv.EnableHeadersVisualStyles = false;
            dgv.BackgroundColor = ComposerTheme.Window;
            dgv.GridColor = ComposerTheme.Border;
            dgv.BorderStyle = BorderStyle.None;
            dgv.DefaultCellStyle.BackColor = ComposerTheme.Field;
            dgv.DefaultCellStyle.ForeColor = ComposerTheme.Text;
            dgv.DefaultCellStyle.SelectionBackColor = ComposerTheme.Accent;
            dgv.DefaultCellStyle.SelectionForeColor = ComposerTheme.AccentText;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = ComposerTheme.GroupBackground;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = ComposerTheme.Text;
            dgv.RowHeadersDefaultCellStyle.BackColor = ComposerTheme.GroupBackground;
            dgv.RowHeadersDefaultCellStyle.ForeColor = ComposerTheme.Text;
        }

        private void TabMain_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= tabMain.TabPages.Count)
                return;

            TabPage page = tabMain.TabPages[e.Index];
            bool selected = tabMain.SelectedIndex == e.Index;
            Color fill = selected ? ComposerTheme.Accent : ComposerTheme.GroupBackground;
            Color text = selected ? ComposerTheme.AccentText : ComposerTheme.SubtleText;

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

        public void QueueRefreshFromDocument(bool deferCadWork = false)
        {
            if (IsDisposed)
                return;

            void Run()
            {
                if (!IsDisposed)
                    RefreshFromDocument(deferCadWork);
            }

            if (IsHandleCreated)
            {
                BeginInvoke(Run);
                return;
            }

            EventHandler? handler = null;
            handler = (_, _) =>
            {
                HandleCreated -= handler!;
                Run();
            };
            HandleCreated += handler;
        }

        public void RefreshFromDocument(bool deferCadWork = false)
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
                LoadCatalogProjectFields(project);
                LoadDimensionFields(project);
                RefreshSceneTree(expandAll: !deferCadWork);
                RefreshBooleanUi();
                RefreshPortUi();
                try
                {
                    RefreshPortVisuals(project);
                }
                catch
                {
                    // drawing may be busy while palette opens
                }

                DrawingSceneRuntimeSync.MirrorActiveDrawing(dwg);
                lblStatus.Text = $"Loaded: {Path.GetFileName(dwg)} · {PluginVersion.StatusSuffix}";
            }
            catch (Exception ex)
            {
                ShowError(ex);
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

        // Insert box is self-contained: filter standard library parts by its own Category +
        // Pressure class. (It must NOT read the Part Family Class/Sch combo — that excluded
        // Flange/Fastener parts whenever Part Family was on a different class, e.g. valve CL3000.)
        private void RefreshCatalogPartList()
        {
            string category = (cmbCatalogCategory.SelectedItem as CatalogCategoryOption)?.Id
                ?? CatalogCategories.Fittings;
            string pc = cmbCatalogPressureClass.SelectedItem as string ?? "150";
            int prevIndex = cmbCatalogPart.SelectedIndex;

            cmbCatalogPart.BeginUpdate();
            cmbCatalogPart.Items.Clear();
            foreach (CustomPartDefinition part in CustomPartCatalog.InsertableParts)
            {
                if (!CatalogCategories.CategoriesMatch(part.Category, category))
                    continue;
                if (!part.PressureClass.Equals(pc, StringComparison.OrdinalIgnoreCase))
                    continue;

                cmbCatalogPart.Items.Add(part);
            }
            cmbCatalogPart.EndUpdate();

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

            // Populate the DN combo with only the sizes this part has in its catalog
            // sheet (part.json "sizes"). Empty → fall back to all pipe sizes. This stops
            // the user from picking a DN the geometry has no data for (→ cubic fallback).
            PopulateCatalogDnCombo(part);

            if (part.ParametricDN)
            {
                cmbCatalogDN.Enabled = cmbCatalogDN.Items.Count > 0;
                if (cmbCatalogDN.SelectedItem == null && cmbCatalogDN.Items.Count > 0)
                    SelectDnCombo(cmbCatalogDN, part.DefaultDN);
            }
            else
            {
                SelectDnCombo(cmbCatalogDN, part.DefaultDN);
                cmbCatalogDN.Enabled = false;
            }
        }

        private void PopulateCatalogDnCombo(CustomPartDefinition part)
        {
            cmbCatalogDN.BeginUpdate();
            cmbCatalogDN.Items.Clear();
            if (part.Sizes.Count > 0)
            {
                foreach (int dn in part.Sizes)
                {
                    PipeSizeOption? opt = PipeSizeCatalog.FindByDn(dn);
                    cmbCatalogDN.Items.Add(opt ?? new PipeSizeOption(dn, dn.ToString(), dn));
                }
            }
            else
            {
                foreach (PipeSizeOption size in PipeSizeCatalog.NominalSizes)
                    cmbCatalogDN.Items.Add(size);
            }
            cmbCatalogDN.EndUpdate();
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

                // Ensure DN (and the rest of the Part Family) is written to the project first, so
                // primitives can be sized from DN — the user does not need a separate Apply step.
                if (!TryApplyCatalogFamilyFromUi(project, refreshDesignDimensions: false, out string? familyError))
                {
                    ShowWarning(familyError ?? "Set DN in the Catalog tab before inserting primitives.");
                    return;
                }

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

                // DN comes from the Standard Library Parts combo, independent of the
                // Part Family / skeleton DN (the inserted native fitting is its own size).
                double libraryDn = cmbCatalogDN.SelectedItem is PipeSizeOption dnOpt
                    ? dnOpt.DnMm
                    : part.DefaultDN;
                if (part.ParametricDN && libraryDn <= 0)
                {
                    ShowWarning("Select a DN in Standard Library Parts.");
                    return;
                }

                double? dn = part.ParametricDN ? libraryDn : null;
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
                    double previewDn = part.ParametricDN ? libraryDn : part.DefaultDN;
                    if (CatalogTestService.TryQueueLibraryPartPreview(doc, part, previewDn, project))
                    {
                        lblSceneStatus.Text =
                            $"Preview {part.DisplayName} — library part only (custom scene not rebuilt).";
                    }
                    else
                    {
                        CatalogTestResult preview = CatalogTestService.BuildLibraryPartPreview(
                            part, previewDn, project);
                        ShowWarning(preview.Message);
                        lblSceneStatus.Text = $"Inserted {part.DisplayName} in scene tree — preview not sent.";
                    }
                }
                else
                {
                    lblSceneStatus.Text = $"Inserted {part.DisplayName} (no active document for preview).";
                }

                lblStatus.Text = $"Scene: {project.Parts.Count} part(s).";
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

                if (!TryApplyCatalogFamilyFromUi(project, refreshDesignDimensions: false, out string? familyError))
                {
                    ShowWarning(familyError ?? "Complete Part Family fields first.");
                    return;
                }

                if (!TryFlushSceneEditorToProject(dwg, project, out string? sceneError))
                {
                    ShowWarning(sceneError ?? "Invalid scene parameter values.");
                    return;
                }

                DocumentStore.Save(dwg, project);

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
                    }

                    lblStatus.Text = result.Message;
                    NotifyPluginUpdateIfNeeded(result.PluginDeploy);
                }
                else
                {
                    lblStatus.Text = result.Message;
                    ShowWarning(result.Message);
                }

                RefreshSceneTree();
                if (_selectedNodeId.HasValue)
                {
                    PrimitiveNode? selected = project.FindNode(_selectedNodeId.Value);
                    if (selected != null)
                        LoadNodeEditor(selected);
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
                RefreshPartLibraryFromDisk();
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

                if (!TryApplyCatalogFamilyFromUi(project, refreshDesignDimensions: false, out string? familyError))
                {
                    ShowWarning(familyError ?? "Complete Part Family fields first.");
                    return;
                }

                DocumentStore.Save(dwg, project);
                CatalogPartRegistrationResult register = CatalogPartRegistrationService.Register(project);
                if (!register.Success)
                {
                    ShowWarning(register.Message);
                    return;
                }

                ValidationResult preflight = CatalogPreflightService.ValidateForExcelPublish(project);
                if (!preflight.IsValid)
                {
                    ShowWarning(string.Join("\n", preflight.Errors));
                    return;
                }

                string partId = CatalogProjectService.SanitizeCatalogName(project.ValveName ?? "");
                if (string.IsNullOrWhiteSpace(partId))
                {
                    ShowWarning("Set a catalog part name before publishing.");
                    return;
                }

                PublishCatalogScope scope = PublishCatalogScopePrompt.Show(FindForm(), partId);
                if (scope == PublishCatalogScope.Cancelled)
                    return;

                string outputPath = ResolveCatalogExcelOutputPath(project, dwg);
                IReadOnlyList<string>? partFilter = scope == PublishCatalogScope.AllParts
                    ? null
                    : new[] { partId };
                CatalogPublishResult result = CatalogPublishService.Publish(
                    dwg,
                    project,
                    outputPath,
                    allowExportWithWarnings: true,
                    partIdFilter: partFilter);

                if (result.Success)
                {
                    Autodesk.AutoCAD.ApplicationServices.Document? doc =
                        Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                    CatalogScriptRegistrationResult registration =
                        CatalogScriptRegistrationService.QueueRegister(doc);

                    string status = result.Message;
                    if (registration.Success)
                        status += Environment.NewLine + registration.Message;
                    else
                        ShowWarning(registration.Message);

                    if (PlantCatalogBuilderLaunchService.TryLaunch(result.OutputPath, out string launchMsg))
                        status += Environment.NewLine + launchMsg;
                    else
                        ShowWarning(launchMsg);

                    lblStatus.Text = status;
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

                if (!TryApplyCatalogFamilyFromUi(project, refreshDesignDimensions: false, out string? familyError))
                {
                    ShowWarning(familyError ?? "Complete Part Family fields first.");
                    return;
                }

                if (!TryFlushSceneEditorToProject(dwg, project, out string? sceneError))
                {
                    ShowWarning(sceneError ?? "Invalid scene parameter values.");
                    return;
                }

                CatalogExportPrepareService.PrepareSceneForExport(project);
                IdleRebuildService.CancelPending();
                DocumentStore.Save(dwg, project);

                CatalogTestResult test = CatalogTestService.BuildTestCommand(dwg);

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
                if (doc == null || !CatalogTestService.TryQueueTest(doc, dwg))
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

        private void btnGenerateCode_Click(object sender, EventArgs e)
        {
            try
            {
                RefreshPartLibraryFromDisk();

                string dwg = DrawingContext.RequireActiveDrawingPath();
                ValveProject project = DocumentStore.LoadOrCreate(
                    dwg, Path.GetFileNameWithoutExtension(dwg));

                if (!TryApplyCatalogFamilyFromUi(project, refreshDesignDimensions: false, out string? familyError))
                {
                    ShowWarning(familyError ?? "Complete Part Family fields first.");
                    return;
                }

                if (!TryFlushSceneEditorToProject(dwg, project, out string? sceneError))
                {
                    ShowWarning(sceneError ?? "Invalid scene parameter values.");
                    return;
                }

                DocumentStore.Save(dwg, project);

                ValidationResult generatePreflight = CatalogPreflightService.ValidateForGenerate(project);
                if (!generatePreflight.IsValid)
                {
                    ShowWarning(string.Join(Environment.NewLine, generatePreflight.Errors));
                    return;
                }

                if (generatePreflight.Warnings.Any()
                    && MessageBox.Show(
                        string.Join(Environment.NewLine + Environment.NewLine, generatePreflight.Warnings)
                        + Environment.NewLine + Environment.NewLine + "Continue Generate Code?",
                        "Plant 3D Catalog Composer",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning) != DialogResult.Yes)
                {
                    return;
                }

                CatalogProjectService.SeedDesignDimensionsIfEmpty(project);
                CatalogExportPrepareService.PrepareSceneForExport(project);
                DocumentStore.Save(dwg, project);
                RefreshSceneTree();
                if (_selectedNodeId.HasValue)
                {
                    PrimitiveNode? selected = project.FindNode(_selectedNodeId.Value);
                    if (selected != null)
                        LoadNodeEditor(selected);
                }
                CatalogPackage package = ComposerLiveScriptService.BuildCatalogPackage(project);
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
                IReadOnlyList<string> exported = CatalogExportService.Export(package, exportRoot, project);
                string catalogCode = package.ToDisplayText();
                ComposerLiveScriptService.WriteCatalogPackage(project, catalogCode);

                CatalogPartRegistrationResult register = CatalogPartRegistrationService.Register(project);
                if (!register.Success)
                {
                    ShowWarning(register.Message);
                }

                if (ProjectPaths.TryResolveDevPartsDir() != null)
                {
                    var previewDoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager
                        .MdiActiveDocument;
                    CatalogDrawingPreviewService.RebuildScenePreview(previewDoc, dwg, project);
                    previewDoc?.Editor.WriteMessage(
                        "\nP3D Composer: use Deploy Catalog to copy scripts to CustomScripts, then Test Catalog.");
                }
                else
                {
                    var previewDoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager
                        .MdiActiveDocument;
                    CatalogDrawingPreviewService.RebuildScenePreview(previewDoc, dwg, project);
                }

                LoadCatalogProjectFields(project);
                LoadDimensionFields(project);

                txtGeneratedCode.Text = catalogCode;

                string partFolder = Path.Combine(exportRoot, package.ExportFolderName);
                string status = package.IsStandardPortReference
                    ? $"Port reference → {partFolder} (standard {package.StandardPartId} unchanged)"
                    : $"Exported {exported.Count} file(s) → {partFolder}";
                if (register.Success)
                    status += $"; registered Excel sheet '{register.ExcelSheetName}'";
                else if (!package.IsStandardPortReference)
                    status += "; Register for Excel failed — see warning";
                lblStatus.Text = status;
                var editor = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager
                    .MdiActiveDocument?.Editor;
                editor?.WriteMessage(
                    $"\nP3D Composer: catalog exported → {partFolder} ({exported.Count} files)");
                editor?.WriteMessage($"\nP3D Composer: {CatalogSceneManifest.Build(project)}");
                if (ProjectPaths.TryResolveDevPartsDir() == null)
                {
                    editor?.WriteMessage(
                        "\nP3D Composer: run Deploy Catalog to copy scripts to CustomScripts for Test Catalog.");
                }
            }
            catch (OperationCanceledException)
            {
                // User cancelled folder picker when deploy.json is not configured.
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
            finally
            {
                RefreshPartLibraryFromDisk();
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
            // User-authored composite parts always land under parts/CUSTOM/<name>/ so they
            // never collide with — or get pruned alongside — the native library parts.
            string? devParts = ProjectPaths.TryResolveDevPartsDir();
            if (devParts != null)
                return EnsureCustomSubdir(devParts);

            string partsDir = ProjectPaths.ResolvePartsDir();
            if (Directory.Exists(partsDir))
                return EnsureCustomSubdir(partsDir);

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

        private static string EnsureCustomSubdir(string partsRoot)
        {
            string customRoot = Path.Combine(partsRoot, CatalogPartsDiscovery.CustomFolderName);
            Directory.CreateDirectory(customRoot);
            return customRoot;
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
                string? targetName = addSubtract
                    ? project.FindNode(_selectedNodeId!.Value)?.Name
                    : null;
                Guid cutterId = PrimitiveService.Insert(dwg, project, primitive);

                if (addSubtract)
                {
                    // Start the cutter at the target's position so it lands on the part to be cut,
                    // then the user nudges it with Move / Rotate.
                    PrimitiveNode? targetNode = project.FindNode(_selectedNodeId!.Value);
                    PrimitiveNode? cutterNode = project.FindNode(cutterId);
                    if (targetNode != null && cutterNode != null && targetNode.Origin.Length >= 3)
                        cutterNode.Origin = new[] { targetNode.Origin[0], targetNode.Origin[1], targetNode.Origin[2] };

                    BooleanGraph.AddOperation(
                        project,
                        BooleanOpType.SUBTRACT,
                        _selectedNodeId!.Value,
                        new List<Guid> { cutterId });
                    DocumentStore.Save(dwg, project);
                }

                RefreshBooleanUi();

                // Make the new cutter the active node and jump to the Scene tab so the user can
                // immediately Move / Rotate it into place (Position / Rotation tools live there).
                _selectedNodeId = cutterId;
                RefreshSceneTree();
                SelectTreeNode(cutterId);
                PrimitiveNode? insertedCutter = project.FindNode(cutterId);
                if (insertedCutter != null)
                    LoadNodeEditor(insertedCutter);
                tabMain.SelectedTab = tabScene;

                lblSceneStatus.Text = addSubtract
                    ? $"Inserted {primitive.DisplayName} & subtracted from {targetName}. "
                      + "Move / Rotate it here — rebuild re-cuts automatically."
                    : $"Inserted {primitive.DisplayName}. Move / Rotate it here, then on Booleans: "
                      + "select target, check this tool, Add.";
                lblBoolStatus.Text = lblSceneStatus.Text;

                TriggerRebuild(project, quiet: true);
            }
            catch (Exception ex)
            {
                ShowBoolError(ex);
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
                DocumentStore.Save(dwg, project);
                LoadNodeEditor(node);
                lblSceneStatus.Text =
                    "Parameters synced from Dimensions tab (BodyOD, BodyLength, …). Click Apply if you edit values.";
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

                if (!TryApplyEditorToNode(node, project.Parameters, out string? error))
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
            TryPersistSelectedNodeEdits();

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

        private void RefreshSceneTree(bool expandAll = true)
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

                if (expandAll)
                    treeScene.ExpandAll();
                else if (treeScene.Nodes.Count > 0)
                    treeScene.Nodes[0].Expand();

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

        private bool TryFlushSceneEditorToProject(string dwg, ValveProject project, out string? error)
        {
            error = null;
            if (_selectedNodeId == null)
                return true;

            PrimitiveNode? node = project.FindNode(_selectedNodeId.Value);
            if (node == null)
                return true;

            if (!TryApplyEditorToNode(node, project.Parameters, out error))
                return false;

            DocumentStore.Save(dwg, project);
            return true;
        }

        private void TryPersistSelectedNodeEdits()
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

                if (!TryApplyEditorToNode(node, project.Parameters, out _))
                    return;

                DocumentStore.Save(dwg, project);
            }
            catch
            {
                // Palette may not have an active drawing during host teardown.
            }
        }

        private bool TryApplyEditorToNode(
            PrimitiveNode node,
            SkeletonParameters skeleton,
            out string? error)
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

                if (exprText.Length > 0)
                {
                    bool keepExpression = ExpressionEvaluator.TryEvaluate(exprText, skeleton, out double eval)
                        && Math.Abs(eval - value) <= 1e-9;
                    if (!keepExpression)
                        exprText = "";
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
