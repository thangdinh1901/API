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
        private const int ColDimBind = 2;
        private const int ColDimUsedIn = 3;

        private GroupBox? _grpDimensions;
        private Label? _lblDimCatalogHint;
        private Label? _lblDimCatalogContext;
        private Label? _lblDimPickMode;
        private ComboBox? _cmbDimPickMode;
        private Label? _lblDimBindParam;
        private TextBox? _txtDimBindParam;
        private DataGridView? _dgvDimensions;
        private Button? _btnDimAdd;
        private Button? _btnDimDelete;
        private Button? _btnDimPick;
        private Button? _btnDimBindScene;
        private Button? _btnDimScanUsed;
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

            _lblDimCatalogHint = new Label
            {
                AutoSize = true,
                Text = "Pick or measure dimensions on the rebuilt preview. Bind links a row to Scene; Scan Used reads expressions.",
            };

            _lblDimCatalogContext = new Label
            {
                AutoSize = true,
                ForeColor = SystemColors.GrayText,
                Text = "Catalog: —",
            };

            _lblDimPickMode = new Label { AutoSize = true, Text = "Pick mode:" };
            _cmbDimPickMode = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 100,
            };
            _cmbDimPickMode.Items.AddRange(new object[]
            {
                "|Δ| distance",
                "ΔX",
                "ΔY",
                "ΔZ",
            });
            _cmbDimPickMode.SelectedIndex = 0;

            _lblDimBindParam = new Label { AutoSize = true, Text = "Bind param:" };
            _txtDimBindParam = new TextBox { Width = 48, Text = "" };
            _toolTip.SetToolTip(_txtDimBindParam, "Optional primitive param key when binding Scene (D, L, H, W, …).");

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
            };
            _dgvDimensions.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Name",
                Name = "colDimName",
                FillWeight = 22,
            });
            _dgvDimensions.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Value (mm)",
                Name = "colDimValue",
                FillWeight = 18,
            });
            _dgvDimensions.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Bind",
                Name = "colDimBind",
                FillWeight = 18,
                ReadOnly = true,
            });
            _dgvDimensions.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Used in",
                Name = "colDimUsedIn",
                FillWeight = 42,
                ReadOnly = true,
            });

            _btnDimAdd = new Button { Text = "Add", Width = 58, Height = 28 };
            _btnDimDelete = new Button { Text = "Delete", Width = 58, Height = 28 };
            _btnDimPick = new Button { Text = "Pick", Width = 58, Height = 28 };
            _btnDimBindScene = new Button { Text = "Bind Scene", Width = 82, Height = 28 };
            _btnDimScanUsed = new Button { Text = "Scan Used", Width = 82, Height = 28 };
            _btnDimApply = new Button { Text = "Apply", Width = 58, Height = 28 };
            _btnDimResolveScene = new Button { Text = "Resolve", Width = 68, Height = 28 };

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
            StyleLightActionButton(
                _btnDimBindScene,
                Color.FromArgb(225, 190, 231),
                Color.FromArgb(186, 104, 200),
                Color.FromArgb(74, 20, 87));
            StyleLightActionButton(
                _btnDimScanUsed,
                Color.FromArgb(225, 190, 231),
                Color.FromArgb(186, 104, 200),
                Color.FromArgb(74, 20, 87));

            _btnDimAdd.Click += (_, _) => AddDimensionRow();
            _btnDimDelete.Click += (_, _) => DeleteSelectedDimensionRows();
            _btnDimPick.Click += (_, _) => BeginDimensionPick();
            _btnDimBindScene.Click += (_, _) => BindSelectedDimensionToScene();
            _btnDimScanUsed.Click += (_, _) => ScanAllDimensionUsedIn();
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
            foreach (Button b in new[]
                     {
                         _btnDimAdd!, _btnDimDelete!, _btnDimPick!,
                         _btnDimBindScene!, _btnDimScanUsed!, _btnDimApply!, _btnDimResolveScene!,
                     })
            {
                _grpDimensions.Controls.Add(b);
            }

            tabDimensions.Controls.Add(_lblDimCatalogHint);
            tabDimensions.Controls.Add(_lblDimCatalogContext);
            tabDimensions.Controls.Add(_lblDimPickMode);
            tabDimensions.Controls.Add(_cmbDimPickMode);
            tabDimensions.Controls.Add(_lblDimBindParam);
            tabDimensions.Controls.Add(_txtDimBindParam);
            tabDimensions.Controls.Add(_grpDimensions);
            tabDimensions.Controls.Add(_lblDimStatus);

            _toolTip.SetToolTip(_btnDimPick, "Pick two points in the drawing for the selected dimension row.");
            _toolTip.SetToolTip(_btnDimBindScene, "Link selected row to the primitive selected on Scene tab.");
            _toolTip.SetToolTip(_btnDimScanUsed, "Refresh Used in from Scene parameter expressions.");

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
                int idx = _dgvDimensions.Rows.Add(result.DimensionName, FormatDimension(result.ValueMm), "", "");
                row = _dgvDimensions.Rows[idx];
            }
            else
            {
                row.Cells[ColDimValue].Value = FormatDimension(result.ValueMm);
            }

            _dimensionBindings[result.DimensionName] = result.Binding;
            row.Cells[ColDimBind].Value = ProjectDimensionService.FormatBinding(result.Binding);

            if (_lblDimStatus != null)
                _lblDimStatus.Text = $"{result.DimensionName} = {FormatDimension(result.ValueMm)} mm (pick).";
        }

        private void RelayoutDimensionsTab()
        {
            if (_grpDimensions == null || _dgvDimensions == null)
                return;

            const int margin = 8;
            int width = Math.Max(200, tabDimensions.ClientSize.Width - margin * 2);
            int y = margin;

            void PlaceLabel(Label? lbl, int xRef, ref int rowY)
            {
                if (lbl == null)
                    return;
                lbl.Location = new Point(xRef, rowY + 2);
            }

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

            int toolY = y;
            PlaceLabel(_lblDimPickMode, margin, ref toolY);
            if (_cmbDimPickMode != null)
                _cmbDimPickMode.Location = new Point(margin + 68, toolY);

            PlaceLabel(_lblDimBindParam, margin + 180, ref toolY);
            if (_txtDimBindParam != null)
                _txtDimBindParam.Location = new Point(margin + 258, toolY);
            y = toolY + 30;

            const int btnH = 28;
            const int btnGap = 4;
            const int btnRows = 62;
            int gridHeight = Math.Max(100, tabDimensions.ClientSize.Height - y - btnRows - 36);

            _grpDimensions.SetBounds(margin, y, width, gridHeight + btnRows);
            _dgvDimensions.SetBounds(10, 22, width - 20, gridHeight);

            int row1 = gridHeight + 28;
            int row2 = row1 + btnH + btnGap;
            int x = 10;
            foreach (Button? b in new[]
                     {
                         _btnDimAdd, _btnDimDelete, _btnDimPick,
                     })
            {
                if (b == null)
                    continue;
                PlaceDimButton(b, x, row1);
                x += b.Width + btnGap;
            }

            x = 10;
            foreach (Button? b in new[]
                     {
                         _btnDimBindScene, _btnDimScanUsed, _btnDimApply, _btnDimResolveScene,
                     })
            {
                if (b == null)
                    continue;
                PlaceDimButton(b, x, row2);
                x += b.Width + btnGap;
            }

            if (_lblDimStatus != null)
            {
                _lblDimStatus.MaximumSize = new Size(width, 0);
                _lblDimStatus.Location = new Point(margin, _grpDimensions.Bottom + 6);
            }
        }

        private static void PlaceDimButton(Button button, int x, int y)
        {
            button.Anchor = AnchorStyles.None;
            button.Location = new Point(x, y);
        }

        private void LoadDimensionFields(ValveProject project)
        {
            if (_dgvDimensions == null)
                return;

            UpdateDimensionCatalogContext(project);

            _dimensionBindings.Clear();
            foreach (KeyValuePair<string, DimensionBinding> pair in project.DimensionBindings)
                _dimensionBindings[pair.Key] = CloneBinding(pair.Value);

            _dgvDimensions.Rows.Clear();
            foreach (string name in ProjectDimensionService.LoadRowNames(project))
            {
                double value = ProjectDimensionService.GetValue(project, name);
                project.DimensionBindings.TryGetValue(name, out DimensionBinding? binding);
                if (!_dimensionBindings.TryGetValue(name, out binding))
                    binding = null;

                string valueText = value > 0 ? FormatDimension(value) : "";
                string bindText = ProjectDimensionService.FormatBinding(binding);
                string usedIn = ProjectDimensionService.ScanUsedIn(project, name);
                _dgvDimensions.Rows.Add(name, valueText, bindText, usedIn);
            }

            if (_lblDimStatus != null && _dgvDimensions.Rows.Count == 0)
                _lblDimStatus.Text = "No dimensions yet — Add a row, then Pick on the preview.";
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
            int index = _dgvDimensions.Rows.Add(name, "", "—", "—");
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
                _txtDimBindParam?.Text);

            if (_lblDimStatus != null)
                _lblDimStatus.Text = $"Pick two points for {name}…";
            doc.SendStringToExecute("P3DCOMPPICKDIM\n", true, false, false);
        }

        private void BindSelectedDimensionToScene()
        {
            if (!TryGetSelectedDimensionRow(out DataGridViewRow row, out string name))
            {
                ShowWarning("Select a dimension row.");
                return;
            }

            if (_selectedNodeId == null)
            {
                ShowWarning("Select a primitive on the Scene tab first.");
                return;
            }

            try
            {
                string dwg = DrawingContext.RequireActiveDrawingPath();
                ValveProject project = DocumentStore.LoadOrCreate(
                    dwg, Path.GetFileNameWithoutExtension(dwg));
                PrimitiveNode? node = project.FindNode(_selectedNodeId.Value);
                if (node == null)
                {
                    ShowWarning("Selected scene node not found.");
                    return;
                }

                DimensionBinding binding = DimensionMeasureService.CreateSceneBind(
                    node.Id,
                    node.Name,
                    _txtDimBindParam?.Text);

                if (_dimensionBindings.TryGetValue(name, out DimensionBinding? existing))
                {
                    binding.MeasureKind = existing.MeasureKind;
                    binding.FromPort = existing.FromPort;
                    binding.ToPort = existing.ToPort;
                    binding.PickFromWcs = existing.PickFromWcs;
                    binding.PickToWcs = existing.PickToWcs;
                }

                _dimensionBindings[name] = binding;
                row.Cells[ColDimBind].Value = ProjectDimensionService.FormatBinding(binding);

                if (_lblDimStatus != null)
                    _lblDimStatus.Text = $"Bound {name} → {ProjectDimensionService.FormatBinding(binding)}.";
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void ScanAllDimensionUsedIn()
        {
            if (_dgvDimensions == null)
                return;

            try
            {
                string dwg = DrawingContext.RequireActiveDrawingPath();
                ValveProject project = DocumentStore.LoadOrCreate(
                    dwg, Path.GetFileNameWithoutExtension(dwg));
                ProjectDimensionService.RefreshAllUsedIn(project, _dgvDimensions, ColDimUsedIn);

                if (_lblDimStatus != null)
                    _lblDimStatus.Text = "Used in column refreshed from Scene expressions.";
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void ApplyDimensionsFromGrid(bool save)
        {
            if (_dgvDimensions == null)
                return;

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
                if (save)
                    DocumentStore.Save(dwg, project);

                ProjectDimensionService.RefreshAllUsedIn(project, _dgvDimensions, ColDimUsedIn);

                if (_lblDimStatus != null)
                    _lblDimStatus.Text = $"Saved {rows.Count} dimension(s) with bindings.";
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
                ProjectDimensionService.ResolveAllPrimitiveExpressions(project);
                DocumentStore.Save(dwg, project);
                RefreshSceneTree();
                ProjectDimensionService.RefreshAllUsedIn(project, _dgvDimensions!, ColDimUsedIn);
                if (_selectedNodeId.HasValue)
                {
                    PrimitiveNode? node = project.FindNode(_selectedNodeId.Value);
                    if (node != null)
                        LoadNodeEditor(node);
                }

                if (_lblDimStatus != null)
                    _lblDimStatus.Text = "Dimensions applied and Scene expressions resolved.";
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

                row.Cells[ColDimBind].Value = ProjectDimensionService.FormatBinding(_dimensionBindings[normalized]);
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
