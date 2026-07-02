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
        private const int ColDimName = 0;
        private const int ColDimValue = 1;

        private GroupBox? _grpDimensions;
        private Label? _lblDimCatalogHint;
        private Label? _lblDimCatalogContext;
        private Label? _lblDimPickMode;
        private ComboBox? _cmbDimPickMode;
        private DataGridView? _dgvDimensions;
        private Button? _btnDimAdd;
        private Button? _btnDimDelete;
        private Button? _btnDimPick;
        private Button? _btnDimApply;
        private Button? _btnDimResolveScene;
        private Label? _lblDimStatus;

        private readonly Dictionary<string, DimensionBinding> _dimensionBindings =
            new(StringComparer.OrdinalIgnoreCase);

        private void InitializeDimensionsTab()
        {
            tabDimensions.Text = "Dimensions";
            tabDimensions.AutoScroll = true;
            tabDimensions.Padding = new Padding(8);
            tabDimensions.Resize += (_, _) => RelayoutDimensionsTab();
            tabDimensions.Enter += (_, _) => RefreshDimensionsTabFromDocument();

            _lblDimCatalogHint = new Label
            {
                AutoSize = true,
                Text = "Part Family Apply seeds rows from Excel from. Edit values, Pick on preview, then Apply.",
            };

            _lblDimCatalogContext = new Label
            {
                AutoSize = true,
                ForeColor = SystemColors.GrayText,
                Text = "Catalog: —",
            };

            _lblDimPickMode = new Label { AutoSize = true, Text = "Pick:" };
            _cmbDimPickMode = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 88,
            };
            _cmbDimPickMode.Items.AddRange(new object[]
            {
                "|Δ|",
                "ΔX",
                "ΔY",
                "ΔZ",
            });
            _cmbDimPickMode.SelectedIndex = 0;

            _dgvDimensions = new DataGridView
            {
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                MultiSelect = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ReadOnly = false,
                Tag = "light",
            };
            // Dimension table stays light (white cells, black text) for readability,
            // regardless of the dark palette theme.
            _dgvDimensions.EnableHeadersVisualStyles = false;
            _dgvDimensions.BackgroundColor = Color.White;
            _dgvDimensions.DefaultCellStyle.BackColor = Color.White;
            _dgvDimensions.DefaultCellStyle.ForeColor = Color.Black;
            _dgvDimensions.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 120, 215);
            _dgvDimensions.DefaultCellStyle.SelectionForeColor = Color.White;
            _dgvDimensions.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(230, 230, 230);
            _dgvDimensions.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black;
            _dgvDimensions.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Name",
                Name = "colDimName",
                FillWeight = 45,
            });
            _dgvDimensions.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Value (mm)",
                Name = "colDimValue",
                FillWeight = 55,
            });

            const int dimBtnH = 24;
            _btnDimAdd = new Button { Text = "+", Width = 28, Height = dimBtnH };
            _btnDimDelete = new Button { Text = "−", Width = 28, Height = dimBtnH };
            _btnDimPick = new Button { Text = "Pick", Width = 44, Height = dimBtnH };
            _btnDimApply = new Button { Text = "Apply", Width = 48, Height = dimBtnH };
            _btnDimResolveScene = new Button { Text = "Scene", Width = 48, Height = dimBtnH };

            StyleAccentButton(_btnDimApply, Color.FromArgb(156, 39, 176));
            StyleAccentButton(_btnDimResolveScene, Color.FromArgb(123, 31, 162));
            StyleAccentButton(_btnDimPick, Color.FromArgb(0, 151, 167));
            StyleLightActionButton(
                _btnDimAdd,
                Color.FromArgb(225, 190, 231),
                Color.FromArgb(186, 104, 200),
                Color.FromArgb(74, 20, 87));
            StyleLightActionButton(
                _btnDimDelete,
                Color.FromArgb(225, 190, 231),
                Color.FromArgb(186, 104, 200),
                Color.FromArgb(74, 20, 87));

            _btnDimAdd.Click += (_, _) => AddDimensionRow();
            _btnDimDelete.Click += (_, _) => DeleteSelectedDimensionRows();
            _btnDimPick.Click += (_, _) => BeginDimensionPick();
            _btnDimApply.Click += (_, _) => ApplyDimensionsFromGrid(save: true);
            _btnDimResolveScene.Click += (_, _) => ResolveSceneFromDimensions();

            _lblDimStatus = new Label
            {
                AutoSize = true,
                ForeColor = SystemColors.GrayText,
                Text = "Select a row, then Pick two CAD points on the preview.",
            };

            _grpDimensions = new GroupBox
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                TabStop = false,
            };
            StyleGroupBoxCaption(_grpDimensions, "Design Dimensions");
            _grpDimensions.Controls.Add(_dgvDimensions);

            tabDimensions.Controls.Add(_lblDimCatalogHint);
            tabDimensions.Controls.Add(_lblDimCatalogContext);
            tabDimensions.Controls.Add(_lblDimPickMode);
            tabDimensions.Controls.Add(_cmbDimPickMode);
            tabDimensions.Controls.Add(_grpDimensions);
            foreach (Button b in new[] { _btnDimAdd!, _btnDimDelete!, _btnDimPick!, _btnDimApply!, _btnDimResolveScene! })
                tabDimensions.Controls.Add(b);
            tabDimensions.Controls.Add(_lblDimStatus);

            _toolTip.SetToolTip(_btnDimAdd, "Add dimension row");
            _toolTip.SetToolTip(_btnDimDelete, "Delete selected row");
            _toolTip.SetToolTip(_btnDimPick, "Pick two points in the drawing for the selected row");
            _toolTip.SetToolTip(_btnDimApply, "Save dimension values to the project");
            _toolTip.SetToolTip(_btnDimResolveScene, "Save and push dimensions into Scene expressions");

            DimensionPickSession.Completed += OnDimensionPickSessionCompleted;

            RelayoutDimensionsTab();
        }

        private void OnDimensionPickSessionCompleted(DimensionPickResult result) =>
            PaletteManager.NotifyDimensionPicked(result);

        public void OnDimensionPicked(DimensionPickResult result)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<DimensionPickResult>(OnDimensionPicked), result);
                return;
            }

            if (_dgvDimensions == null)
                return;

            DataGridViewRow? row = FindDimensionRow(result.DimensionName);
            if (row == null)
            {
                int idx = _dgvDimensions.Rows.Add(result.DimensionName, FormatDimension(result.ValueMm));
                row = _dgvDimensions.Rows[idx];
            }
            else
            {
                row.Cells[ColDimValue].Value = FormatDimension(result.ValueMm);
            }

            _dimensionBindings[result.DimensionName] = result.Binding;

            if (_lblDimStatus != null)
                _lblDimStatus.Text = $"{result.DimensionName} = {FormatDimension(result.ValueMm)} mm — saved.";
            ApplyDimensionsFromGrid(save: true);
        }

        private void RelayoutDimensionsTab()
        {
            if (_grpDimensions == null || _dgvDimensions == null)
                return;

            const int margin = 8;
            const int btnH = 24;
            const int btnGap = 4;
            const int footerH = btnH + 28;

            int width = Math.Max(200, tabDimensions.ClientSize.Width - margin * 2);
            int y = margin;

            if (_lblDimCatalogHint != null)
            {
                _lblDimCatalogHint.MaximumSize = new Size(width, 0);
                _lblDimCatalogHint.Location = new Point(margin, y);
                y = _lblDimCatalogHint.Bottom + 4;
            }

            if (_lblDimCatalogContext != null)
            {
                _lblDimCatalogContext.MaximumSize = new Size(width, 0);
                _lblDimCatalogContext.Location = new Point(margin, y);
                y = _lblDimCatalogContext.Bottom + 6;
            }

            if (_lblDimPickMode != null)
                _lblDimPickMode.Location = new Point(margin, y + 2);
            if (_cmbDimPickMode != null)
                _cmbDimPickMode.Location = new Point(margin + 36, y);
            y += btnH + 6;

            int gridHeight = Math.Max(120, tabDimensions.ClientSize.Height - y - footerH);
            _grpDimensions.SetBounds(margin, y, width, gridHeight);
            _dgvDimensions.SetBounds(10, 20, width - 20, gridHeight - 28);

            int btnY = _grpDimensions.Bottom + 6;
            int x = margin;
            foreach (Button? b in new[] { _btnDimAdd, _btnDimDelete, _btnDimPick, _btnDimApply, _btnDimResolveScene })
            {
                if (b == null)
                    continue;
                b.Location = new Point(x, btnY);
                b.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                x += b.Width + btnGap;
            }

            if (_lblDimStatus != null)
            {
                _lblDimStatus.MaximumSize = new Size(width, 0);
                _lblDimStatus.Location = new Point(margin, btnY + btnH + 4);
            }
        }

        /// <summary>Reload from the on-disk scene when the Dimensions tab gains focus, so
        /// native-seed rows reflect any Catalog parts inserted from the Scene tab in the
        /// meantime (LoadDimensionFields otherwise only runs on document open / Generate Code).</summary>
        private void RefreshDimensionsTabFromDocument()
        {
            try
            {
                string? dwg = DrawingContext.GetActiveDrawingPath();
                if (dwg == null)
                    return;

                ValveProject project = DocumentStore.LoadOrCreate(
                    dwg, Path.GetFileNameWithoutExtension(dwg));

                // One-time cleanup: drop FaceToFace/BodyOD/ElbowCenterToFace/L/CEL/T rows left
                // over from the old auto-seed path (removed — envelope dims are now user-declared).
                bool pruned = CatalogProjectService.PruneStaleAutoSuggestedDimensions(project);

                // Persist the sync so stale native-seed keys (from deleted/renamed/retyped nodes)
                // are pruned on disk, not just in the grid.
                bool synced = CatalogNativeDimensionSeedService.Sync(project);
                if (pruned || synced)
                    DocumentStore.Save(dwg, project);

                LoadDimensionFields(project);
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void LoadDimensionFields(ValveProject project)
        {
            if (_dgvDimensions == null)
                return;

            CatalogNativeDimensionSeedService.Sync(project);

            UpdateDimensionCatalogContext(project);

            _dimensionBindings.Clear();
            foreach (KeyValuePair<string, DimensionBinding> pair in project.DimensionBindings)
                _dimensionBindings[pair.Key] = CloneBinding(pair.Value);

            _dgvDimensions.Rows.Clear();
            foreach (string name in ProjectDimensionService.LoadRowNames(project))
            {
                double value = ProjectDimensionService.GetValue(project, name);
                string valueText = value > 0 ? FormatDimension(value) : "";
                int idx = _dgvDimensions.Rows.Add(name, valueText);
                if (CatalogNativeDimensionSeedService.IsNativeSeedName(name))
                    MarkRowAsNativeSeed(_dgvDimensions.Rows[idx]);
            }

            if (_lblDimStatus != null && _dgvDimensions.Rows.Count == 0)
                _lblDimStatus.Text = "No dimensions yet — Apply Part Family or Add a row.";
        }

        /// <summary>Native-seed rows (name has a dot, e.g. "ELBO_001.R") are recomputed from the
        /// scene on every load — read-only so the user cannot hand-edit a value that will be
        /// silently overwritten, and greyed to distinguish from declared/pick dimensions.</summary>
        private static void MarkRowAsNativeSeed(DataGridViewRow row)
        {
            row.Cells[ColDimName].ReadOnly = true;
            row.Cells[ColDimValue].ReadOnly = true;
            row.DefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240);
            row.DefaultCellStyle.ForeColor = Color.FromArgb(90, 90, 90);
        }

        private void UpdateDimensionCatalogContext(ValveProject project)
        {
            if (_lblDimCatalogContext == null)
                return;

            SkeletonParameters p = project.Parameters;
            string dn = p.DN > 0 ? $"DN{p.DN:0.###}" : "DN —";
            string dn2 = p.DN2 > 0 && Math.Abs(p.DN2 - p.DN) > 0.01 ? $", DN2 {p.DN2:0.###}" : "";
            string pc = string.IsNullOrWhiteSpace(p.PressureClass) ? "Class —" : $"Class {p.PressureClass}";
            string group = string.IsNullOrWhiteSpace(project.CatalogGroup) ? "Custom" : project.CatalogGroup;
            _lblDimCatalogContext.Text = $"Catalog: {group} · {dn}{dn2} · {pc}";
        }

        private void AddDimensionRow()
        {
            if (_dgvDimensions == null)
                return;

            string name = NextSuggestedDimensionName();
            int index = _dgvDimensions.Rows.Add(name, "");
            _dimensionBindings[name] = new DimensionBinding { MeasureKind = "manual" };
            _dgvDimensions.CurrentCell = _dgvDimensions.Rows[index].Cells[ColDimValue];
            _dgvDimensions.BeginEdit(true);
        }

        private string NextSuggestedDimensionName()
        {
            if (_dgvDimensions == null)
                return "Dim1";

            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (DataGridViewRow row in _dgvDimensions.Rows)
            {
                string? text = row.Cells[ColDimName].Value?.ToString();
                if (!string.IsNullOrWhiteSpace(text))
                    used.Add(text.Trim());
            }

            foreach (string builtIn in ProjectDimensionService.BuiltInNames)
            {
                if (!used.Contains(builtIn))
                    return builtIn;
            }

            for (int i = 1; i < 1000; i++)
            {
                string candidate = $"Dim{i}";
                if (!used.Contains(candidate))
                    return candidate;
            }

            return "DimNew";
        }

        private void DeleteSelectedDimensionRows()
        {
            if (_dgvDimensions == null || _dgvDimensions.SelectedRows.Count == 0)
            {
                ShowWarning("Select one or more rows to delete.");
                return;
            }

            foreach (DataGridViewRow row in _dgvDimensions.SelectedRows.Cast<DataGridViewRow>().ToList())
            {
                if (row.IsNewRow)
                    continue;

                string? name = row.Cells[ColDimName].Value?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(name))
                    _dimensionBindings.Remove(name);
                _dgvDimensions.Rows.Remove(row);
            }
        }

        private bool TryGetSelectedDimensionRow(out DataGridViewRow row, out string name)
        {
            row = null!;
            name = "";
            if (_dgvDimensions == null || _dgvDimensions.SelectedRows.Count == 0)
                return false;

            row = _dgvDimensions.SelectedRows[0];
            name = row.Cells[ColDimName].Value?.ToString()?.Trim() ?? "";
            if (name.Length == 0)
            {
                ShowWarning("Enter a dimension name in the selected row first.");
                return false;
            }

            return true;
        }

        private DataGridViewRow? FindDimensionRow(string dimensionName)
        {
            if (_dgvDimensions == null)
                return null;

            foreach (DataGridViewRow row in _dgvDimensions.Rows)
            {
                string? n = row.Cells[ColDimName].Value?.ToString()?.Trim();
                if (n != null && n.Equals(dimensionName, StringComparison.OrdinalIgnoreCase))
                    return row;
            }

            return null;
        }

        private DimensionMeasureMode SelectedPickMode()
        {
            if (_cmbDimPickMode == null || _cmbDimPickMode.SelectedIndex < 0)
                return DimensionMeasureMode.Distance;

            return _cmbDimPickMode.SelectedIndex switch
            {
                1 => DimensionMeasureMode.DeltaX,
                2 => DimensionMeasureMode.DeltaY,
                3 => DimensionMeasureMode.DeltaZ,
                _ => DimensionMeasureMode.Distance,
            };
        }

        private void BeginDimensionPick()
        {
            if (!TryGetSelectedDimensionRow(out _, out string name))
            {
                ShowWarning("Select a dimension row to measure.");
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
                ShowWarning("Finish the current command, then Pick.");
                return;
            }

            Guid? nodeId = _selectedNodeId;
            string? nodeName = null;
            if (nodeId.HasValue)
            {
                try
                {
                    string dwg = DrawingContext.RequireActiveDrawingPath();
                    ValveProject project = DocumentStore.LoadOrCreate(
                        dwg, Path.GetFileNameWithoutExtension(dwg));
                    nodeName = project.FindNode(nodeId.Value)?.Name;
                }
                catch
                {
                    nodeId = null;
                }
            }

            DimensionPickSession.Begin(
                name,
                SelectedPickMode(),
                nodeId,
                nodeName,
                paramKey: null);

            if (_lblDimStatus != null)
                _lblDimStatus.Text = $"Pick two points for {name}…";
            doc.SendStringToExecute("P3DCOMPPICKDIM\n", true, false, false);
        }

        private void ApplyDimensionsFromGrid(bool save)
        {
            if (_dgvDimensions == null)
                return;

            if (!TryReadDimensionRows(out List<(string Name, double ValueMm)> rows, out string? error))
            {
                if (!string.IsNullOrEmpty(error))
                    ShowWarning(error);
                return;
            }

            try
            {
                string dwg = DrawingContext.RequireActiveDrawingPath();
                ValveProject project = DocumentStore.LoadOrCreate(
                    dwg, Path.GetFileNameWithoutExtension(dwg));

                ProjectDimensionService.ApplyToProject(project, rows, _dimensionBindings);
                CatalogNativeDimensionSeedService.Sync(project);
                if (save)
                    DocumentStore.Save(dwg, project);

                if (_lblDimStatus != null)
                    _lblDimStatus.Text = rows.Count == 0
                        ? "No dimensions saved."
                        : $"Saved {rows.Count} dimension(s).";
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void ResolveSceneFromDimensions()
        {
            if (!TryReadDimensionRows(out List<(string Name, double ValueMm)> rows, out string? error))
            {
                ShowWarning(error ?? "Invalid dimensions.");
                return;
            }

            try
            {
                string dwg = DrawingContext.RequireActiveDrawingPath();
                ValveProject project = DocumentStore.LoadOrCreate(
                    dwg, Path.GetFileNameWithoutExtension(dwg));

                ProjectDimensionService.ApplyToProject(project, rows, _dimensionBindings);
                CatalogNativeDimensionSeedService.Sync(project);
                CatalogExportPrepareService.PrepareSceneForExport(project);
                DocumentStore.Save(dwg, project);
                RefreshSceneTree();
                if (_selectedNodeId.HasValue)
                {
                    PrimitiveNode? node = project.FindNode(_selectedNodeId.Value);
                    if (node != null)
                        LoadNodeEditor(node);
                }

                if (_lblDimStatus != null)
                    _lblDimStatus.Text = "Dimensions saved and Scene updated.";
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private bool TryReadDimensionRows(
            out List<(string Name, double ValueMm)> rows,
            out string? error)
        {
            rows = new List<(string, double)>();
            error = null;

            if (_dgvDimensions == null)
            {
                error = "Dimension table is not ready.";
                return false;
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (DataGridViewRow row in _dgvDimensions.Rows)
            {
                if (row.IsNewRow)
                    continue;

                string name = row.Cells[ColDimName].Value?.ToString() ?? "";
                string valueText = row.Cells[ColDimValue].Value?.ToString() ?? "";

                // Native-seed rows (e.g. "ELBO_001.R") are recomputed by
                // CatalogNativeDimensionSeedService.Sync on every load/save — never carried
                // through the manual-row pipeline, which rejects '.' in names.
                if (CatalogNativeDimensionSeedService.IsNativeSeedName(name))
                    continue;

                if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(valueText))
                    continue;

                if (!ProjectDimensionService.TryValidateRow(name, valueText, out error, out string normalized, out double valueMm))
                    return false;

                if (!seen.Add(normalized))
                {
                    error = $"Duplicate dimension name '{normalized}'.";
                    return false;
                }

                rows.Add((normalized, valueMm));

                if (!_dimensionBindings.ContainsKey(normalized))
                    _dimensionBindings[normalized] = new DimensionBinding { MeasureKind = "manual" };
            }

            return true;
        }

        private static DimensionBinding CloneBinding(DimensionBinding source) =>
            new()
            {
                MeasureKind = source.MeasureKind,
                FromPort = source.FromPort,
                ToPort = source.ToPort,
                SceneNodeId = source.SceneNodeId,
                SceneNodeName = source.SceneNodeName,
                ParamKey = source.ParamKey,
                PickFromWcs = source.PickFromWcs?.ToArray(),
                PickToWcs = source.PickToWcs?.ToArray(),
            };

        private static string FormatDimension(double value) =>
            value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
