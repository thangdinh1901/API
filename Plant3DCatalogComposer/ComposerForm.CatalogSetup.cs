using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using Plant3DCatalogComposer.Services;
using Plant3DSkeletonManager.Core;

namespace Plant3DCatalogComposer
{
    public partial class ComposerForm
    {
        private GroupBox? _grpCatalogProject;
        private TextBox? _txtCatalogName;
        private ComboBox? _cmbPlantGroup;
        private ComboBox? _cmbProjectDn;
        private ComboBox? _cmbProjectDn2;
        private ComboBox? _cmbProjectPressureClass;
        private ComboBox? _cmbProjectSchedule;
        private TextBox? _txtTooltipShort;
        private TextBox? _txtTooltipLong;
        private Label? _lblScriptPreview;
        private Label? _lblCatalogName;
        private Label? _lblCatalogGroup;
        private Label? _lblCatalogDn;
        private Label? _lblCatalogDn2;
        private Label? _lblCatalogClass;
        private Label? _lblCatalogSchedule;
        private Label? _lblCatalogTip;
        private Button? _btnApplyCatalogProject;
        private CheckBox? _chkShowValveSkeleton;

        private void InitializeCatalogSetupTab()
        {
            ReparentValveControlsIntoSkeleton();
            HideDuplicateCatalogPartFields();
            BuildCatalogProjectPanel();

            _chkShowValveSkeleton = new CheckBox
            {
                AutoSize = true,
                Text = "Show valve skeleton builder (custom valves only)",
                Width = 280,
            };
            _chkShowValveSkeleton.CheckedChanged += (_, _) =>
            {
                grpSkeleton.Visible = _chkShowValveSkeleton.Checked;
                RelayoutSetupTab();
            };
            tabSetup.Controls.Add(_chkShowValveSkeleton);
            grpSkeleton.Visible = false;

            grpSceneTools.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            grpSceneTools.Resize += (_, _) => LayoutSceneToolsButtons();
            tabSetup.Resize += (_, _) => RelayoutSetupTab();

            StyleGroupBoxCaption(grpCatalogParts, "Standard Library Parts");
            grpSkeleton.Text = "Valve Skeleton (optional)";
            tabSetup.Text = "Catalog";
            RelayoutSetupTab();
        }

        private void ReparentValveControlsIntoSkeleton()
        {
            tabSetup.Controls.Remove(lblValveType);
            tabSetup.Controls.Remove(cmbValveType);
            tabSetup.Controls.Remove(lblDN);
            tabSetup.Controls.Remove(cmbDn);
            tabSetup.Controls.Remove(lblPressureClass);
            tabSetup.Controls.Remove(cmbPressureClass);

            lblValveType.Location = new Point(8, 18);
            cmbValveType.Location = new Point(96, 15);
            cmbValveType.Width = 180;
            lblDN.Location = new Point(8, 48);
            cmbDn.Location = new Point(96, 45);
            cmbDn.Width = 180;
            lblPressureClass.Location = new Point(8, 78);
            cmbPressureClass.Location = new Point(96, 75);
            cmbPressureClass.Width = 180;
            dgvParams.Location = new Point(10, 108);
            dgvParams.Height = 88;
            btnCreateSkeleton.Location = new Point(10, 202);

            grpSkeleton.Controls.Add(lblValveType);
            grpSkeleton.Controls.Add(cmbValveType);
            grpSkeleton.Controls.Add(lblDN);
            grpSkeleton.Controls.Add(cmbDn);
            grpSkeleton.Controls.Add(lblPressureClass);
            grpSkeleton.Controls.Add(cmbPressureClass);
            grpSkeleton.Height = 244;
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
            _grpCatalogProject = new GroupBox
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Location = new Point(8, 8),
                Size = new Size(296, 210),
                TabStop = false,
            };
            StyleGroupBoxCaption(_grpCatalogProject, "Catalog Project");
            _grpCatalogProject.Resize += (_, _) => LayoutCatalogProjectFields();

            _lblCatalogName = new Label { Text = "Script name:" };
            _txtCatalogName = new TextBox
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            };
            _txtCatalogName.TextChanged += (_, _) => UpdateScriptPreviewLabel();

            _lblScriptPreview = new Label
            {
                AutoSize = true,
                ForeColor = SystemColors.GrayText,
                Text = "→ CUST_…",
            };

            _lblCatalogGroup = new Label { Text = "Plant group:" };
            _cmbPlantGroup = new ComboBox
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                DropDownStyle = ComboBoxStyle.DropDownList,
            };
            foreach (string group in CatalogPlantGroups.All)
                _cmbPlantGroup.Items.Add(group);
            _toolTip.SetToolTip(_cmbPlantGroup,
                "Plant 3D @activate Group. BV/SW ports export as Fitting — Group Valve forces FL in Spec Editor.");

            _lblCatalogDn = new Label { Text = "DN large:" };
            _cmbProjectDn = new ComboBox
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                DropDownStyle = ComboBoxStyle.DropDownList,
                DisplayMember = nameof(PipeSizeOption.Display),
            };
            foreach (PipeSizeOption size in PipeSizeCatalog.NominalSizes)
                _cmbProjectDn.Items.Add(size);
            _cmbProjectDn.SelectedIndexChanged += (_, _) => RefreshProjectDn2Combo();

            _lblCatalogDn2 = new Label { Text = "DN small:" };
            _cmbProjectDn2 = new ComboBox
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                DropDownStyle = ComboBoxStyle.DropDownList,
                DisplayMember = nameof(PipeSizeOption.Display),
            };

            _lblCatalogClass = new Label { Text = "Class:" };
            _cmbProjectPressureClass = new ComboBox
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                DropDownStyle = ComboBoxStyle.DropDownList,
            };
            foreach (string pc in PipeSizeCatalog.PressureClasses)
                _cmbProjectPressureClass.Items.Add(pc);
            _cmbProjectPressureClass.SelectedIndexChanged += (_, _) => RefreshCatalogPartList();

            _lblCatalogSchedule = new Label { Text = "Schedule:" };
            _cmbProjectSchedule = new ComboBox
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                DropDownStyle = ComboBoxStyle.DropDownList,
            };
            foreach (PipeScheduleOption schedule in PipeScheduleCatalog.All)
                _cmbProjectSchedule.Items.Add(schedule);
            _cmbProjectSchedule.SelectedIndexChanged += (_, _) => RefreshCatalogPartList();
            _toolTip.SetToolTip(_cmbProjectSchedule, "Pipe schedule: 40, 80, 40S, 80S, 10, 10S.");

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

            _btnApplyCatalogProject = new Button
            {
                Anchor = AnchorStyles.None,
                Size = new Size(86, 26),
                Text = "Apply",
            };
            StyleAccentButton(_btnApplyCatalogProject, Color.FromArgb(25, 118, 210));
            _btnApplyCatalogProject.Click += btnApplyCatalogProject_Click;

            _grpCatalogProject.Controls.AddRange(new Control[]
            {
                _lblCatalogName, _txtCatalogName, _lblScriptPreview,
                _lblCatalogGroup, _cmbPlantGroup,
                _lblCatalogDn, _cmbProjectDn,
                _lblCatalogDn2, _cmbProjectDn2,
                _lblCatalogClass, _cmbProjectPressureClass,
                _lblCatalogSchedule, _cmbProjectSchedule,
                _lblCatalogTip, _txtTooltipShort, _txtTooltipLong,
            });
            _grpCatalogProject.Controls.Add(_btnApplyCatalogProject);

            tabSetup.Controls.Add(_grpCatalogProject);
            LayoutCatalogProjectFields();
        }

        private void RelayoutSetupTab()
        {
            const int margin = 8;
            int contentWidth = Math.Max(200, tabSetup.ClientSize.Width - margin * 2);
            int y = margin;

            if (_grpCatalogProject != null)
            {
                _grpCatalogProject.Width = contentWidth;
                _grpCatalogProject.Location = new Point(margin, y);
                y = _grpCatalogProject.Bottom + margin;
            }

            grpCatalogParts.Width = contentWidth;
            grpCatalogParts.Location = new Point(margin, y);
            y = grpCatalogParts.Bottom + margin;

            if (_chkShowValveSkeleton != null)
            {
                _chkShowValveSkeleton.Location = new Point(margin + 3, y);
                y += _chkShowValveSkeleton.Height + 4;
            }

            if (grpSkeleton.Visible)
            {
                grpSkeleton.Width = contentWidth;
                grpSkeleton.Location = new Point(margin, y);
                y = grpSkeleton.Bottom + margin;
            }

            grpSceneTools.Width = contentWidth;
            grpSceneTools.Height = 152;
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
            const int row4 = 118;
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
            Place(btnExport, left2, row1, colW);
            Place(btnImportJson, pad, row2, colW);
            Place(btnRebuildScene, left2, row2, colW);
            Place(btnTestCatalog, pad, row3, colW);
            Place(btnDeployCatalog, left2, row3, colW);
            Place(btnPublishCatalog, pad, row4, inner);
        }

        private void LoadCatalogProjectFields(ValveProject project)
        {
            if (_txtCatalogName == null)
                return;

            _txtCatalogName.Text = string.IsNullOrWhiteSpace(project.ValveName)
                ? "COMPOSER_PART"
                : project.ValveName;

            SelectPlantGroup(project.CatalogGroup);
            if (project.Parameters.DN > 0)
                SelectDnCombo(_cmbProjectDn!, project.Parameters.DN);
            else if (_cmbProjectDn!.Items.Count > 0 && _cmbProjectDn.SelectedIndex < 0)
                _cmbProjectDn.SelectedIndex = 0;

            RefreshProjectDn2Combo(project.Parameters.DN2);

            if (!string.IsNullOrEmpty(project.Parameters.PressureClass))
                SelectPressureClassCombo(_cmbProjectPressureClass!, project.Parameters.PressureClass);
            else if (_cmbProjectPressureClass!.Items.Count > 0 && _cmbProjectPressureClass.SelectedIndex < 0)
                _cmbProjectPressureClass.SelectedIndex = 0;

            SelectScheduleCombo(
                _cmbProjectSchedule!,
                string.IsNullOrWhiteSpace(project.Parameters.PipeSchedule)
                    ? PipeScheduleCatalog.Default
                    : project.Parameters.PipeSchedule);
            if (_cmbProjectSchedule!.SelectedIndex < 0 && _cmbProjectSchedule.Items.Count > 0)
                _cmbProjectSchedule.SelectedIndex = 0;

            _txtTooltipShort!.Text = project.TooltipShort ?? "";
            _txtTooltipLong!.Text = project.TooltipLong ?? "";
            UpdateScriptPreviewLabel(project);
            RefreshCatalogPartList();
        }

        private void SelectPlantGroup(string? group)
        {
            if (_cmbPlantGroup == null || string.IsNullOrWhiteSpace(group))
            {
                if (_cmbPlantGroup?.Items.Count > 0 && _cmbPlantGroup.SelectedIndex < 0)
                    _cmbPlantGroup.SelectedIndex = 0;
                return;
            }

            for (int i = 0; i < _cmbPlantGroup.Items.Count; i++)
            {
                if (_cmbPlantGroup.Items[i]?.ToString()?.Equals(group, StringComparison.OrdinalIgnoreCase) == true)
                {
                    _cmbPlantGroup.SelectedIndex = i;
                    return;
                }
            }

            if (_cmbPlantGroup.Items.Count > 0)
                _cmbPlantGroup.SelectedIndex = 0;
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

        private void btnApplyCatalogProject_Click(object? sender, EventArgs e)
        {
            if (_txtCatalogName == null || _cmbPlantGroup == null ||
                _cmbProjectDn == null || _cmbProjectPressureClass == null ||
                _cmbProjectSchedule == null)
            {
                return;
            }

            if (!TryGetSelectedDn(_cmbProjectDn, out double dn))
            {
                ShowWarning("Select DN large (run / large end).");
                return;
            }

            if (!TryGetSelectedDn(_cmbProjectDn2!, out double dn2))
            {
                ShowWarning("Select DN small (branch / small end).");
                return;
            }

            if (_cmbProjectPressureClass.SelectedItem is not string pressureClass ||
                pressureClass.Length == 0)
            {
                ShowWarning("Select a pressure class.");
                return;
            }

            if (_cmbProjectSchedule.SelectedItem is not PipeScheduleOption scheduleOpt)
            {
                ShowWarning("Select a pipe schedule.");
                return;
            }

            try
            {
                string dwg = DrawingContext.RequireActiveDrawingPath();
                ValveProject project = DocumentStore.LoadOrCreate(
                    dwg, Path.GetFileNameWithoutExtension(dwg));

                CatalogProjectService.Apply(
                    project,
                    _txtCatalogName.Text,
                    _cmbPlantGroup.SelectedItem?.ToString() ?? "Custom",
                    dn,
                    dn2,
                    pressureClass,
                    scheduleOpt.Id,
                    _txtTooltipShort?.Text ?? "",
                    _txtTooltipLong?.Text ?? "");

                DocumentStore.Save(dwg, project);
                LoadCatalogProjectFields(project);
                RefreshCatalogCodeDisplay(project);
                lblStatus.Text = $"Catalog project saved → {CatalogProjectService.PreviewScriptName(project)}";
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private string GetProjectPressureClass(ValveProject project) =>
            !string.IsNullOrWhiteSpace(project.Parameters.PressureClass)
                ? project.Parameters.PressureClass
                : _cmbProjectPressureClass?.SelectedItem as string ?? "150";

        private string GetProjectSchedule(ValveProject? project = null)
        {
            if (project != null && !string.IsNullOrWhiteSpace(project.Parameters.PipeSchedule))
                return PipeScheduleCatalog.Normalize(project.Parameters.PipeSchedule);

            if (_cmbProjectSchedule?.SelectedItem is PipeScheduleOption opt)
                return opt.Id;

            return PipeScheduleCatalog.Default;
        }

        private double GetProjectDn(ValveProject project)
        {
            if (project.Parameters.DN > 0)
                return project.Parameters.DN;
            if (_cmbProjectDn != null && TryGetSelectedDn(_cmbProjectDn, out double dn))
                return dn;
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

        private void RefreshProjectDn2Combo(double preferredDn2 = 0)
        {
            if (_cmbProjectDn2 == null || _cmbProjectDn == null)
                return;

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
