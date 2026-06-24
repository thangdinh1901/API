using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Plant3DCatalogComposer.Services;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer
{
    public partial class ComposerForm
    {
        private GroupBox? _grpCatalogProject;
        private TextBox? _txtCatalogName;
        private ComboBox? _cmbProjectDn;
        private ComboBox? _cmbProjectDn2;
        private ComboBox? _cmbClassSch;
        private TextBox? _txtTooltipShort;
        private TextBox? _txtTooltipLong;
        private Label? _lblScriptPreview;
        private Label? _lblCatalogName;
        private Label? _lblCatalogDn;
        private Label? _lblCatalogDn2;
        private Label? _lblClassSch;
        private Label? _lblCatalogTip;
        private Label? _lblPartCategory;
        private ComboBox? _cmbPartCategory;
        private Label? _lblPipingComponent;
        private ComboBox? _cmbPipingComponent;
        private Label? _lblPrimaryEnd;
        private ComboBox? _cmbPrimaryEnd;
        private Label? _lblShortDescription;
        private TextBox? _txtShortDescription;
        private Label? _lblExcelClone;
        private ComboBox? _cmbExcelClone;
        private int _partFamilyLoadDepth;
        private Label? _lblFlangeFacing;
        private ComboBox? _cmbFlangeFacing;

        private void InitializeCatalogSetupTab()
        {
            HideDuplicateCatalogPartFields();
            BuildCatalogProjectPanel();

            grpSceneTools.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            grpSceneTools.Resize += (_, _) => LayoutSceneToolsButtons();
            tabSetup.Resize += (_, _) => RelayoutSetupTab();

            StyleGroupBoxCaption(grpCatalogParts, "Standard Library Parts");
            tabSetup.Text = "Catalog";
            RelayoutSetupTab();
        }

        private void HideDuplicateCatalogPartFields()
        {
            lblCatalogDN.Visible = false;
            cmbCatalogDN.Visible = false;
            lblCatalogPressureClass.Visible = false;
            cmbCatalogPressureClass.Visible = false;
            btnInsertCatalogPart.Location = new Point(10, 96);
            grpCatalogParts.Height = 132;
        }

        private void BuildCatalogProjectPanel()
        {
            _partFamilyLoadDepth++;
            try
            {
            _grpCatalogProject = new GroupBox
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Location = new Point(8, 8),
                Size = new Size(296, 318),
                TabStop = false,
            };
            StyleGroupBoxCaption(_grpCatalogProject, "Part Family");
            _grpCatalogProject.Resize += (_, _) =>
            {
                if (!_catalogLayoutSizing)
                    LayoutCatalogProjectFields();
            };

            _lblCatalogName = new Label { Text = "Script name:" };
            _txtCatalogName = new TextBox
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            };
            _txtCatalogName.TextChanged += (_, _) =>
            {
                UpdateScriptPreviewLabel();
                OnPartFamilySuggestFieldsChanged(refreshPrimaryEnd: true);
            };

            _lblScriptPreview = new Label
            {
                AutoSize = true,
                ForeColor = SystemColors.GrayText,
                Text = "→ CUST_…",
            };

            _lblCatalogDn = new Label { Text = "DN large:" };
            _cmbProjectDn = new ComboBox
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                DropDownStyle = ComboBoxStyle.DropDownList,
                DisplayMember = nameof(PipeSizeOption.Display),
            };
            foreach (PipeSizeOption size in PipeSizeCatalog.NominalSizes)
                _cmbProjectDn.Items.Add(size);
            _cmbProjectDn.SelectedIndexChanged += (_, _) => UpdateDnSmallFieldState();

            _lblCatalogDn2 = new Label { Text = "DN small:" };
            _cmbProjectDn2 = new ComboBox
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                DropDownStyle = ComboBoxStyle.DropDownList,
                DisplayMember = nameof(PipeSizeOption.Display),
            };

            _lblClassSch = new Label { Text = "Class/Sch:" };
            _cmbClassSch = new ComboBox
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                DropDownStyle = ComboBoxStyle.DropDownList,
            };
            _toolTip.SetToolTip(_cmbClassSch, "Pressure class or pipe schedule (depends on Category / Component / Prim. End)");
            RefreshClassSchCombo();

            _lblCatalogTip = new Label { Text = "Tooltip:" };
            _txtTooltipShort = new TextBox
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
            };
            _txtTooltipLong = new TextBox
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            };
            _toolTip.SetToolTip(_txtTooltipShort, "TooltipShort (@activate)");
            _toolTip.SetToolTip(_txtTooltipLong, "TooltipLong (@activate)");

            _lblPartCategory = new Label { Text = "Category:" };
            _cmbPartCategory = new ComboBox
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                DropDownStyle = ComboBoxStyle.DropDownList,
            };
            foreach (CatalogCategoryOption cat in CatalogCategories.All)
                _cmbPartCategory.Items.Add(cat);
            _cmbPartCategory.SelectedIndexChanged += (_, _) =>
            {
                RefreshPipingComponentCombo();
                UpdateDnSmallFieldState();
                OnPartFamilySuggestFieldsChanged(refreshPrimaryEnd: true);
            };

            _lblPipingComponent = new Label { Text = "Component:" };
            _cmbPipingComponent = new ComboBox
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                DropDownStyle = ComboBoxStyle.DropDownList,
            };
            RefreshPipingComponentCombo();
            _cmbPipingComponent.SelectedIndexChanged += (_, _) =>
            {
                UpdateDnSmallFieldState();
                OnPartFamilySuggestFieldsChanged(refreshPrimaryEnd: true);
            };
            _lblPrimaryEnd = new Label { Text = "Prim. End:" };
            _cmbPrimaryEnd = new ComboBox
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                DropDownStyle = ComboBoxStyle.DropDownList,
                DisplayMember = nameof(PrimaryEndTypeOption.Display),
            };
            RefreshPrimaryEndCombo();
            _toolTip.SetToolTip(_cmbPrimaryEnd, "Plant 3D Primary End Type — filtered by Category / Component");
            _cmbPrimaryEnd.SelectedIndexChanged += (_, _) =>
                OnPartFamilySuggestFieldsChanged();

            _lblFlangeFacing = new Label { Text = "Facing:", Visible = false };
            _cmbFlangeFacing = new ComboBox
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Visible = false,
            };
            _toolTip.SetToolTip(_cmbFlangeFacing, "Flange facing (RF / FF) — filtered by end type and Excel template");

            _lblShortDescription = new Label { Text = "Short desc:" };
            _txtShortDescription = new TextBox { Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            _toolTip.SetToolTip(_txtShortDescription, "Excel ShortDescription / palette label");

            _lblExcelClone = new Label { Text = "Excel from:" };
            _cmbExcelClone = new ComboBox
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                DropDownStyle = ComboBoxStyle.DropDownList,
            };
            RefreshExcelCloneCombo();
            RefreshFacingCombo();
            UpdateFacingFieldState();
            _toolTip.SetToolTip(_cmbExcelClone, "Excel clone template — suggested from Category, Component, End type, and Class/Sch");

            _grpCatalogProject.Controls.AddRange(new Control[]
            {
                _lblCatalogName, _txtCatalogName, _lblScriptPreview,
                _lblPartCategory, _cmbPartCategory,
                _lblPipingComponent, _cmbPipingComponent,
                _lblPrimaryEnd, _cmbPrimaryEnd,
                _lblFlangeFacing, _cmbFlangeFacing,
                _lblCatalogDn, _cmbProjectDn,
                _lblCatalogDn2, _cmbProjectDn2,
                _lblClassSch, _cmbClassSch,
                _lblCatalogTip, _txtTooltipShort, _txtTooltipLong,
                _lblShortDescription, _txtShortDescription,
                _lblExcelClone, _cmbExcelClone,
            });

            tabSetup.Controls.Add(_grpCatalogProject);
            LayoutCatalogProjectFields();
            }
            finally
            {
                _partFamilyLoadDepth--;
            }
        }

        private void RelayoutSetupTab()
        {
            const int margin = 8;
            const int partFamilyToLibraryGap = 20;
            int contentWidth = Math.Max(200, tabSetup.ClientSize.Width - margin * 2);
            int y = margin;

            if (_grpCatalogProject != null)
            {
                _grpCatalogProject.Width = contentWidth;
                _grpCatalogProject.Location = new Point(margin, y);
                y = _grpCatalogProject.Bottom + partFamilyToLibraryGap;
            }

            grpCatalogParts.Width = contentWidth;
            grpCatalogParts.Location = new Point(margin, y);
            y = grpCatalogParts.Bottom + margin;

            grpSceneTools.Width = contentWidth;
            grpSceneTools.Height = 120;
            grpSceneTools.Location = new Point(margin, y);
            LayoutSceneToolsButtons();
            LayoutCatalogProjectFields();
        }

        /// <summary>2×2 button grid — equal columns, no Right-anchor overlap on narrow palettes.</summary>
        private void LayoutSceneToolsButtons()
        {
            const int pad = 10;
            const int gap = 8;
            const int row1 = 22;
            const int row2 = 54;
            const int row3 = 86;
            const int btnH = 28;

            int inner = Math.Max(120, grpSceneTools.ClientSize.Width - pad * 2);
            int colW = Math.Max(60, (inner - gap) / 2);
            int left2 = pad + colW + gap;

            void Place(Button button, int x, int y, int width)
            {
                button.Anchor = AnchorStyles.None;
                button.SetBounds(x, y, width, btnH);
            }

            Place(btnGenerateCode, pad, row1, colW);
            Place(btnRebuildScene, left2, row1, colW);
            Place(btnTestCatalog, pad, row2, colW);
            Place(btnDeployCatalog, left2, row2, colW);
            Place(btnPublishCatalog, pad, row3, inner);
        }

        private void LoadCatalogProjectFields(ValveProject project)
        {
            if (_txtCatalogName == null)
                return;

            _partFamilyLoadDepth++;
            try
            {
                _txtCatalogName.Text = string.IsNullOrWhiteSpace(project.ValveName)
                    ? "COMPOSER_PART"
                    : project.ValveName;

                if (project.Parameters.DN > 0)
                    SelectDnCombo(_cmbProjectDn!, project.Parameters.DN);
                else if (_cmbProjectDn!.Items.Count > 0 && _cmbProjectDn.SelectedIndex < 0)
                    _cmbProjectDn.SelectedIndex = 0;

                LoadPartFamilyFields(project);

                _txtTooltipShort!.Text = project.TooltipShort ?? "";
                _txtTooltipLong!.Text = project.TooltipLong ?? "";
                UpdateScriptPreviewLabel(project);
                RefreshCatalogPartList();
                LayoutCatalogProjectFields();
            }
            finally
            {
                _partFamilyLoadDepth--;
            }
        }

        private void LoadPartFamilyFields(ValveProject project)
        {
            _partFamilyLoadDepth++;
            try
            {
                string category = !string.IsNullOrWhiteSpace(project.CatalogCategory)
                    ? project.CatalogCategory
                    : CatalogCategories.FromActivateGroup(project.CatalogGroup);
                SelectCategoryCombo(category);
                RefreshPipingComponentCombo(project.PnpClassName);
                RefreshPrimaryEndCombo(CatalogStandardSetInference.ResolvePrimaryEndType(project));
                _txtShortDescription!.Text = project.ShortDescription ?? "";

                RefreshClassSchCombo(
                    project.Parameters.PressureClass,
                    project.Parameters.PipeSchedule);

                UpdateDnSmallFieldState(project.Parameters.DN2);

                string? savedClone = string.IsNullOrWhiteSpace(project.ExcelCloneSourcePartId)
                    ? null
                    : project.ExcelCloneSourcePartId.Trim();
                RefreshExcelCloneCombo(savedClone);
                RefreshFacingCombo(project.FlangeFacing);
                UpdateFacingFieldState();
            }
            finally
            {
                _partFamilyLoadDepth--;
            }
        }

        private void OnPartFamilySuggestFieldsChanged(bool refreshPrimaryEnd = false)
        {
            if (_partFamilyLoadDepth > 0)
                return;

            if (refreshPrimaryEnd)
                RefreshPrimaryEndCombo();

            RefreshClassSchCombo();
            RefreshExcelCloneCombo();
            RefreshFacingCombo();
            UpdateFacingFieldState();
            RefreshPortTypeComboFromPartFamily();
            LayoutCatalogProjectFields();
        }

        private void RefreshPrimaryEndCombo(string? selectCode = null)
        {
            if (_cmbPrimaryEnd == null)
                return;

            string? keep = selectCode ?? (_cmbPrimaryEnd.SelectedItem as PrimaryEndTypeOption)?.Code;
            IReadOnlyList<string> codes = CatalogPartFamilySuggestService.ListPrimaryEndTypes(
                GetPartFamilyCategoryId(),
                GetPartFamilyComponent(),
                GetPartFamilyScriptName());

            _cmbPrimaryEnd.Items.Clear();
            foreach (string code in codes)
            {
                _cmbPrimaryEnd.Items.Add(new PrimaryEndTypeOption(
                    code,
                    Plant3DEndTypes.FormatDisplay(code)));
            }

            if (_cmbPrimaryEnd.Items.Count == 0)
                return;

            string inferred = CatalogPartFamilySuggestService.InferPrimaryEndType(
                GetPartFamilyCategoryId(),
                GetPartFamilyComponent(),
                GetPartFamilyScriptName());

            string target = !string.IsNullOrWhiteSpace(keep)
                && codes.Any(c => c.Equals(keep, StringComparison.OrdinalIgnoreCase))
                    ? Plant3DEndTypes.NormalizeCode(keep)
                    : inferred;

            SelectPrimaryEndCombo(target);
        }

        private void RefreshFacingCombo(string? selectFacing = null)
        {
            if (_cmbFlangeFacing == null)
                return;

            string excelClone = _cmbExcelClone?.SelectedItem as string ?? InferExcelCloneForCurrentFamily();
            IReadOnlyList<string> options = CatalogPartFamilySuggestService.ListFacingOptions(
                GetPartFamilyCategoryId(),
                GetPartFamilyComponent(),
                GetPartFamilyPrimaryEnd(),
                GetPartFamilyScriptName(),
                excelClone);

            _cmbFlangeFacing.Items.Clear();
            foreach (string facing in options)
                _cmbFlangeFacing.Items.Add(facing);

            if (options.Count == 0)
            {
                _cmbFlangeFacing.SelectedIndex = -1;
                return;
            }

            string target = CatalogPartFamilySuggestService.InferFacing(
                GetPartFamilyCategoryId(),
                GetPartFamilyComponent(),
                GetPartFamilyPrimaryEnd(),
                GetPartFamilyScriptName(),
                excelClone,
                selectFacing ?? GetPartFamilyFacing());

            SelectFacingCombo(target);
        }

        private string GetPartFamilyCategoryId() =>
            _cmbPartCategory?.SelectedItem is CatalogCategoryOption cat
                ? cat.Id
                : CatalogCategories.Fittings;

        private string GetPartFamilyComponent() =>
            _cmbPipingComponent?.SelectedItem?.ToString() ?? "";

        private string GetPartFamilyPrimaryEnd() =>
            _cmbPrimaryEnd?.SelectedItem is PrimaryEndTypeOption endOpt
                ? endOpt.Code
                : "Undefined_ET";

        private ClassScheduleOption? GetSelectedClassSchedule() =>
            _cmbClassSch?.SelectedItem as ClassScheduleOption;

        private void RefreshClassSchCombo(string? pressureClass = null, string? pipeSchedule = null)
        {
            if (_cmbClassSch == null)
                return;

            ClassScheduleOption? current = GetSelectedClassSchedule();
            bool explicitValues = !string.IsNullOrWhiteSpace(pressureClass)
                || !string.IsNullOrWhiteSpace(pipeSchedule);

            IReadOnlyList<ClassScheduleOption> options = CatalogClassScheduleOptions.Resolve(
                GetPartFamilyCategoryId(),
                GetPartFamilyComponent(),
                GetPartFamilyPrimaryEnd());

            _cmbClassSch.Items.Clear();
            foreach (ClassScheduleOption opt in options)
                _cmbClassSch.Items.Add(opt);

            if (_cmbClassSch.Items.Count == 0)
                return;

            ClassScheduleOption match;
            if (explicitValues)
            {
                match = CatalogClassScheduleOptions.Match(
                    pressureClass,
                    pipeSchedule,
                    GetPartFamilyCategoryId(),
                    GetPartFamilyComponent(),
                    GetPartFamilyPrimaryEnd());
            }
            else if (current != null
                     && options.Any(o => o.Id.Equals(current.Id, StringComparison.OrdinalIgnoreCase)))
            {
                match = current;
            }
            else
            {
                match = CatalogClassScheduleOptions.InferDefault(
                    GetPartFamilyCategoryId(),
                    GetPartFamilyComponent(),
                    GetPartFamilyPrimaryEnd());
            }

            for (int i = 0; i < _cmbClassSch.Items.Count; i++)
            {
                if (_cmbClassSch.Items[i] is ClassScheduleOption opt &&
                    opt.Id.Equals(match.Id, StringComparison.OrdinalIgnoreCase))
                {
                    if (_cmbClassSch.SelectedIndex != i)
                        _cmbClassSch.SelectedIndex = i;
                    return;
                }
            }

            if (_cmbClassSch.SelectedIndex != 0)
                _cmbClassSch.SelectedIndex = 0;
        }

        private void SelectClassSchCombo(string? pressureClass, string? pipeSchedule) =>
            RefreshClassSchCombo(pressureClass, pipeSchedule);

        private void RefreshPipingComponentCombo(string? selectValue = null)
        {
            if (_cmbPartCategory == null || _cmbPipingComponent == null)
                return;

            string keep = selectValue
                ?? _cmbPipingComponent.SelectedItem?.ToString()
                ?? "";

            string categoryId = _cmbPartCategory.SelectedItem is CatalogCategoryOption cat
                ? cat.Id
                : CatalogCategories.Fittings;

            _cmbPipingComponent.Items.Clear();
            foreach (string pnp in CatalogPartFamilyOptions.GetPipingComponents(categoryId))
                _cmbPipingComponent.Items.Add(pnp);

            if (_cmbPipingComponent.Items.Count == 0)
                return;

            SelectStringCombo(
                _cmbPipingComponent,
                keep,
                CatalogPartFamilyOptions.GetPipingComponents(categoryId));
        }

        private void SelectCategoryCombo(string? categoryId)
        {
            if (_cmbPartCategory == null)
                return;

            if (string.IsNullOrWhiteSpace(categoryId))
            {
                if (_cmbPartCategory.Items.Count > 0 && _cmbPartCategory.SelectedIndex < 0)
                    _cmbPartCategory.SelectedIndex = 0;
                return;
            }

            string target = CatalogCategories.NormalizeCategoryId(categoryId);
            for (int i = 0; i < _cmbPartCategory.Items.Count; i++)
            {
                if (_cmbPartCategory.Items[i] is CatalogCategoryOption opt &&
                    opt.Id.Equals(target, StringComparison.OrdinalIgnoreCase))
                {
                    _cmbPartCategory.SelectedIndex = i;
                    return;
                }
            }

            if (_cmbPartCategory.Items.Count > 0)
                _cmbPartCategory.SelectedIndex = 0;
        }

        private void SelectPrimaryEndCombo(string? code)
        {
            if (_cmbPrimaryEnd == null)
                return;

            string target = Plant3DEndTypes.NormalizeCode(code);
            for (int i = 0; i < _cmbPrimaryEnd.Items.Count; i++)
            {
                if (_cmbPrimaryEnd.Items[i] is PrimaryEndTypeOption opt &&
                    opt.Code.Equals(target, StringComparison.OrdinalIgnoreCase))
                {
                    _cmbPrimaryEnd.SelectedIndex = i;
                    return;
                }
            }

            if (_cmbPrimaryEnd.Items.Count > 0)
                _cmbPrimaryEnd.SelectedIndex = 0;
        }

        private static void SelectStringCombo(
            ComboBox combo,
            string? value,
            IReadOnlyList<string>? allowed,
            bool allowEmpty = false)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                for (int i = 0; i < combo.Items.Count; i++)
                {
                    if (combo.Items[i]?.ToString()?.Equals(value, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        combo.SelectedIndex = i;
                        return;
                    }
                }

                if (allowed != null && allowed.Any(x => x.Equals(value, StringComparison.OrdinalIgnoreCase)))
                {
                    combo.Items.Add(value);
                    combo.SelectedItem = value;
                    return;
                }
            }

            if (allowEmpty)
            {
                combo.SelectedIndex = -1;
                return;
            }

            if (combo.Items.Count > 0 && combo.SelectedIndex < 0)
                combo.SelectedIndex = 0;
        }

        private void RefreshExcelCloneCombo(string? selectPartId = null)
        {
            if (_cmbExcelClone == null)
                return;

            _cmbExcelClone.BeginUpdate();
            _cmbExcelClone.Items.Clear();
            try
            {
                foreach (string partId in CatalogExcelCloneSuggestService.ListForPartFamily(
                             GetPartFamilyCategoryId(),
                             GetPartFamilyComponent(),
                             GetPartFamilyPrimaryEnd(),
                             GetSelectedClassSchedule()?.PressureClass,
                             GetProjectSchedule()))
                {
                    _cmbExcelClone.Items.Add(partId);
                }

                if (_cmbExcelClone.Items.Count == 0)
                    lblStatus.Text = "Excel from empty — check Part Family fields / template.";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Excel template error: " + ex.Message;
            }
            finally
            {
                _cmbExcelClone.EndUpdate();
            }

            string target = !string.IsNullOrWhiteSpace(selectPartId)
                ? selectPartId.Trim()
                : InferExcelCloneForCurrentFamily();

            if (!string.IsNullOrWhiteSpace(target))
            {
                bool selected = false;
                for (int i = 0; i < _cmbExcelClone.Items.Count; i++)
                {
                    if (_cmbExcelClone.Items[i]?.ToString()?.Equals(target, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        _cmbExcelClone.SelectedIndex = i;
                        selected = true;
                        break;
                    }
                }

                if (!selected && _cmbExcelClone.Items.Count > 0)
                    _cmbExcelClone.SelectedIndex = 0;
            }
            else if (_cmbExcelClone.Items.Count > 0)
                _cmbExcelClone.SelectedIndex = 0;
        }

        private string GetPartFamilyScriptName() =>
            _txtCatalogName?.Text?.Trim() ?? "";

        private string InferExcelCloneForCurrentFamily()
        {
            string category = GetPartFamilyCategoryId();
            string component = GetPartFamilyComponent();
            string group = CatalogPartFamilyOptions.ResolveActivateGroup(category, component);

            return CatalogExcelTemplateService.InferCloneSourcePartId(
                "",
                category,
                component,
                "",
                group,
                GetPartFamilyPrimaryEnd(),
                GetSelectedClassSchedule()?.PressureClass,
                GetProjectSchedule());
        }

        private void RefreshPartLibraryFromDisk()
        {
            CatalogPartLibrarySyncService.SyncFromDisk();
            RefreshCatalogPartList();
            RefreshExcelCloneCombo();
        }

        private void UpdateFacingFieldState()
        {
            if (_lblFlangeFacing == null || _cmbFlangeFacing == null)
                return;

            bool show = CatalogFlangeFacing.PrimaryEndUsesFacing(GetPartFamilyPrimaryEnd());
            _lblFlangeFacing.Visible = show;
            _cmbFlangeFacing.Visible = show;
            if (show && _cmbFlangeFacing.SelectedIndex < 0 && _cmbFlangeFacing.Items.Count > 0)
                _cmbFlangeFacing.SelectedIndex = 0;

            LayoutCatalogProjectFields();
        }

        private void SelectFacingCombo(string? facing)
        {
            if (_cmbFlangeFacing == null)
                return;

            string target = CatalogFlangeFacing.Normalize(facing);
            for (int i = 0; i < _cmbFlangeFacing.Items.Count; i++)
            {
                if (_cmbFlangeFacing.Items[i]?.ToString()?.Equals(target, StringComparison.OrdinalIgnoreCase) == true)
                {
                    _cmbFlangeFacing.SelectedIndex = i;
                    return;
                }
            }

            if (_cmbFlangeFacing.Items.Count > 0)
                _cmbFlangeFacing.SelectedIndex = 0;
        }

        private string GetPartFamilyFacing() =>
            _cmbFlangeFacing?.SelectedItem?.ToString() ?? "RF";

        private bool TryApplyCatalogFamilyFromUi(
            ValveProject project,
            bool refreshDesignDimensions,
            out string? errorMessage)
        {
            errorMessage = null;
            if (_txtCatalogName == null || _cmbProjectDn == null || _cmbClassSch == null)
            {
                errorMessage = "Part Family panel is not initialized.";
                return false;
            }

            if (!TryGetSelectedDn(_cmbProjectDn, out _))
            {
                errorMessage = "Select DN large (run / large end).";
                return false;
            }

            if (CatalogPartFamilyOptions.UsesDnSmall(
                    GetPartFamilyCategoryId(),
                    GetPartFamilyComponent())
                && !TryGetSelectedDn(_cmbProjectDn2!, out _))
            {
                errorMessage = "Select DN small (branch / small end).";
                return false;
            }

            if (GetSelectedClassSchedule() is not ClassScheduleOption)
            {
                errorMessage = "Select Class/Sch.";
                return false;
            }

            ApplyCatalogFamilyFromUi(project, refreshDesignDimensions);
            return true;
        }

        private void ApplyCatalogFamilyFromUi(ValveProject project, bool refreshDesignDimensions = true)
        {
            if (_txtCatalogName == null || _cmbProjectDn == null || _cmbClassSch == null)
                return;

            if (!TryGetSelectedDn(_cmbProjectDn, out double dn))
                return;

            double dn2 = 0;
            if (CatalogPartFamilyOptions.UsesDnSmall(
                    GetPartFamilyCategoryId(),
                    GetPartFamilyComponent()))
            {
                if (!TryGetSelectedDn(_cmbProjectDn2!, out dn2))
                    return;
            }

            if (GetSelectedClassSchedule() is not ClassScheduleOption classSch)
                return;

            string category = GetPartFamilyCategoryId();
            string pipingComponent = GetPartFamilyComponent();
            string primaryEnd = GetPartFamilyPrimaryEnd();
            string cloneSource = _cmbExcelClone?.SelectedItem as string ?? "";

            CatalogProjectService.Apply(
                project,
                _txtCatalogName.Text,
                dn,
                dn2,
                classSch.PressureClass,
                classSch.PipeSchedule,
                _txtTooltipShort?.Text ?? "",
                _txtTooltipLong?.Text ?? "",
                category,
                pipingComponent,
                primaryEnd,
                _txtShortDescription?.Text ?? "",
                cloneSource,
                GetPartFamilyFacing(),
                refreshDesignDimensions);
        }

        private void UpdateScriptPreviewLabel(ValveProject? project = null)
        {
            if (_lblScriptPreview == null || _txtCatalogName == null)
                return;

            if (project != null)
            {
                _lblScriptPreview.Text = "→ " + CatalogProjectService.PreviewScriptName(project);
                return;
            }

            string preview = CatalogProjectService.SanitizeCatalogName(_txtCatalogName.Text);
            _lblScriptPreview.Text = string.IsNullOrEmpty(preview)
                ? "→ CUST_…"
                : $"→ CUST_{preview}";
        }

        private string GetProjectPressureClass(ValveProject project) =>
            GetSelectedClassSchedule()?.PressureClass
            ?? (!string.IsNullOrWhiteSpace(project.Parameters.PressureClass)
                ? project.Parameters.PressureClass
                : "150");

        private string GetProjectSchedule(ValveProject? project = null)
        {
            ClassScheduleOption? selected = GetSelectedClassSchedule();
            if (selected != null && !string.IsNullOrEmpty(selected.PipeSchedule))
                return PipeScheduleCatalog.Normalize(selected.PipeSchedule);

            if (project != null && !string.IsNullOrWhiteSpace(project.Parameters.PipeSchedule))
                return PipeScheduleCatalog.Normalize(project.Parameters.PipeSchedule);

            return PipeScheduleCatalog.Default;
        }

        private double GetProjectDn(ValveProject project)
        {
            // UI combo is what the user sees — do not use stale project.Parameters.DN after they change DN large.
            if (_cmbProjectDn != null && TryGetSelectedDn(_cmbProjectDn, out double uiDn))
                return uiDn;
            if (project.Parameters.DN > 0)
                return project.Parameters.DN;
            return 100;
        }

        private static void SelectScheduleCombo(ComboBox combo, string scheduleId)
        {
            string id = PipeScheduleCatalog.Normalize(scheduleId);
            for (int i = 0; i < combo.Items.Count; i++)
            {
                if (combo.Items[i] is PipeScheduleOption opt &&
                    opt.Id.Equals(id, StringComparison.OrdinalIgnoreCase))
                {
                    combo.SelectedIndex = i;
                    return;
                }
            }
        }

        private void UpdateDnSmallFieldState(double preferredDn2 = 0)
        {
            if (_cmbProjectDn2 == null || _lblCatalogDn2 == null)
                return;

            bool use = CatalogPartFamilyOptions.UsesDnSmall(
                GetPartFamilyCategoryId(),
                GetPartFamilyComponent());

            _lblCatalogDn2.Enabled = use;
            if (!use)
            {
                _cmbProjectDn2.Enabled = false;
                _cmbProjectDn2.Items.Clear();
                _cmbProjectDn2.SelectedIndex = -1;
                _cmbProjectDn2.BackColor = SystemColors.Window;
                return;
            }

            _cmbProjectDn2.Enabled = true;
            _cmbProjectDn2.BackColor = SystemColors.Window;
            RefreshProjectDn2Combo(preferredDn2);
        }

        private void RefreshProjectDn2Combo(double preferredDn2 = 0)
        {
            if (_cmbProjectDn2 == null || _cmbProjectDn == null)
                return;

            if (!CatalogPartFamilyOptions.UsesDnSmall(
                    GetPartFamilyCategoryId(),
                    GetPartFamilyComponent()))
            {
                return;
            }

            if (!TryGetSelectedDn(_cmbProjectDn, out double largeDn))
            {
                _cmbProjectDn2.Items.Clear();
                return;
            }

            int large = (int)Math.Round(largeDn);
            int keep = preferredDn2 > 0
                ? (int)Math.Round(preferredDn2)
                : _cmbProjectDn2.SelectedItem is PipeSizeOption cur
                    ? cur.DnMm
                    : 0;

            _cmbProjectDn2.Items.Clear();
            foreach (int small in BwFittingSizeCatalog.ReducerSmallSizes(large))
            {
                PipeSizeOption? opt = PipeSizeCatalog.FindByDn(small);
                if (opt != null)
                    _cmbProjectDn2.Items.Add(opt);
            }

            if (_cmbProjectDn2.Items.Count == 0)
                return;

            if (keep > 0 && BwFittingSizeCatalog.IsValidReducerPair(large, keep))
                SelectDnCombo(_cmbProjectDn2, keep);
            else
                SelectDnCombo(_cmbProjectDn2, BwFittingSizeCatalog.DefaultReducerSmallDn(large));
        }
    }
}
