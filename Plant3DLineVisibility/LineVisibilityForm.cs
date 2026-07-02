using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Autodesk.AutoCAD.ApplicationServices;
using Plant3DLineVisibility.Models;
using Plant3DLineVisibility.Services;

using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace Plant3DLineVisibility
{
    /// <summary>
    /// Palette user-control: displays all Line Numbers in the active drawing
    /// with checkboxes to toggle piping-object visibility.
    /// </summary>
    public partial class LineVisibilityForm : UserControl
    {
        private List<LineGroupInfo> _allGroups = new();
        private List<LineGroupInfo> _filteredGroups = new();
        private bool _suppressCellEvents;

        public LineVisibilityForm()
        {
            InitializeComponent();
        }

        // ────────────────────────────────────────────────────────
        //  Public API (called by PaletteManager)
        // ────────────────────────────────────────────────────────

        /// <summary>Called when the user switches to a different drawing.</summary>
        public void OnDocumentSwitched()
        {
            _allGroups.Clear();
            _filteredGroups.Clear();
            RefreshGrid();
            lblStatus.Text = "Document changed — click Refresh to scan.";
        }

        // ────────────────────────────────────────────────────────
        //  Button handlers
        // ────────────────────────────────────────────────────────

        private void BtnRefresh_Click(object? sender, EventArgs e)
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                lblStatus.Text = "No active drawing.";
                return;
            }

            try
            {
                Cursor = Cursors.WaitCursor;
                lblStatus.Text = "Scanning…";
                System.Windows.Forms.Application.DoEvents();

                _allGroups = LineVisibilityService.ScanDrawing(doc);
                ApplyFilter();
                lblStatus.Text = FormatStatus();
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Scan error: " + ex.Message;
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void BtnShowAll_Click(object? sender, EventArgs e)
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null || _allGroups.Count == 0) return;

            try
            {
                Cursor = Cursors.WaitCursor;
                LineVisibilityService.ShowAll(doc, _allGroups);
                RefreshGrid();
                lblStatus.Text = FormatStatus();
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Error: " + ex.Message;
            }
            finally { Cursor = Cursors.Default; }
        }

        private void BtnHideAll_Click(object? sender, EventArgs e)
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null || _allGroups.Count == 0) return;

            try
            {
                Cursor = Cursors.WaitCursor;
                LineVisibilityService.HideAll(doc, _allGroups);
                RefreshGrid();
                lblStatus.Text = FormatStatus();
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Error: " + ex.Message;
            }
            finally { Cursor = Cursors.Default; }
        }

        private void BtnIsolate_Click(object? sender, EventArgs e)
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            LineGroupInfo? selected = GetSelectedGroup();
            if (selected == null)
            {
                lblStatus.Text = "Select a line number first.";
                return;
            }

            try
            {
                Cursor = Cursors.WaitCursor;
                LineVisibilityService.IsolateLine(doc, _allGroups, selected.LineNumberTag);
                RefreshGrid();
                lblStatus.Text = $"Isolated: {selected.LineNumberTag}";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Error: " + ex.Message;
            }
            finally { Cursor = Cursors.Default; }
        }

        // ────────────────────────────────────────────────────────
        //  Search / filter
        // ────────────────────────────────────────────────────────

        private void TxtSearch_TextChanged(object? sender, EventArgs e)
        {
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            string filter = txtSearch.Text.Trim();

            _filteredGroups = string.IsNullOrEmpty(filter)
                ? _allGroups.ToList()
                : _allGroups
                    .Where(g => g.LineNumberTag.Contains(filter, StringComparison.OrdinalIgnoreCase))
                    .ToList();

            RefreshGrid();
        }

        // ────────────────────────────────────────────────────────
        //  DataGridView management
        // ────────────────────────────────────────────────────────

        private void RefreshGrid()
        {
            _suppressCellEvents = true;
            try
            {
                dgvLines.Rows.Clear();

                foreach (LineGroupInfo info in _filteredGroups)
                {
                    int rowIndex = dgvLines.Rows.Add(
                        info.IsVisible,
                        info.LineNumberTag,
                        info.ComponentCount);
                    dgvLines.Rows[rowIndex].Tag = info;
                }
            }
            finally
            {
                _suppressCellEvents = false;
            }
        }

        /// <summary>
        /// Commit the checkbox edit immediately on click (instead of waiting
        /// for the row to lose focus).
        /// </summary>
        private void DgvLines_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (e.ColumnIndex == colVisible.Index)
            {
                dgvLines.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        /// <summary>
        /// Handle checkbox toggle — show/hide the line's piping objects.
        /// </summary>
        private void DgvLines_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (_suppressCellEvents) return;
            if (e.RowIndex < 0 || e.ColumnIndex != colVisible.Index) return;

            DataGridViewRow row = dgvLines.Rows[e.RowIndex];
            LineGroupInfo? info = row.Tag as LineGroupInfo;
            if (info == null) return;

            bool visible = (bool)(row.Cells[colVisible.Index].Value ?? true);

            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            try
            {
                Cursor = Cursors.WaitCursor;
                LineVisibilityService.SetVisibility(doc, info.ObjectIds, visible);
                info.IsVisible = visible;
                lblStatus.Text = FormatStatus();
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Error: " + ex.Message;
            }
            finally { Cursor = Cursors.Default; }
        }

        // ────────────────────────────────────────────────────────
        //  Helpers
        // ────────────────────────────────────────────────────────

        private LineGroupInfo? GetSelectedGroup()
        {
            if (dgvLines.SelectedRows.Count == 0) return null;
            return dgvLines.SelectedRows[0].Tag as LineGroupInfo;
        }

        private string FormatStatus()
        {
            int total = _allGroups.Count;
            int visible = _allGroups.Count(g => g.IsVisible);
            int totalParts = _allGroups.Sum(g => g.ComponentCount);
            return $"{visible}/{total} lines visible  •  {totalParts} components";
        }
    }
}
