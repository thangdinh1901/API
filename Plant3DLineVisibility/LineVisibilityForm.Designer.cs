using System.Drawing;
using System.Windows.Forms;

namespace Plant3DLineVisibility
{
    partial class LineVisibilityForm
    {
        private System.ComponentModel.IContainer? components = null;

        private Panel panelTop;
        private TextBox txtSearch;
        private DataGridView dgvLines;
        private DataGridViewCheckBoxColumn colVisible;
        private DataGridViewTextBoxColumn colLineNumber;
        private DataGridViewTextBoxColumn colCount;
        private Panel panelButtons;
        private Button btnRefresh;
        private Button btnShowAll;
        private Button btnHideAll;
        private Button btnIsolate;
        private Label lblStatus;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            Font baseFont = new Font("Segoe UI", 9F, FontStyle.Regular);

            // ═══════════════════════════════════════════════════════
            //  Top panel — search bar
            // ═══════════════════════════════════════════════════════
            panelTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 36,
                Padding = new Padding(4, 6, 4, 4)
            };

            txtSearch = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = baseFont,
                PlaceholderText = "🔍  Filter line numbers...",
                BorderStyle = BorderStyle.FixedSingle
            };
            txtSearch.TextChanged += TxtSearch_TextChanged;
            panelTop.Controls.Add(txtSearch);

            // ═══════════════════════════════════════════════════════
            //  Bottom panel — action buttons + status
            // ═══════════════════════════════════════════════════════
            panelButtons = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 68,
                Padding = new Padding(4, 4, 4, 2)
            };

            var flowButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 32,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = false
            };

            btnRefresh = CreateButton("⟳ Refresh", "Scan drawing for line numbers");
            btnRefresh.Click += BtnRefresh_Click;

            btnShowAll = CreateButton("👁 Show All", "Show all piping objects");
            btnShowAll.Click += BtnShowAll_Click;

            btnHideAll = CreateButton("⊘ Hide All", "Hide all piping objects");
            btnHideAll.Click += BtnHideAll_Click;

            btnIsolate = CreateButton("◎ Isolate", "Show only the selected line");
            btnIsolate.Click += BtnIsolate_Click;

            flowButtons.Controls.AddRange(new Control[] { btnRefresh, btnShowAll, btnHideAll, btnIsolate });

            lblStatus = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 24,
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.FromArgb(120, 120, 120),
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "Click Refresh to scan the drawing.",
                Padding = new Padding(4, 0, 0, 0)
            };

            panelButtons.Controls.Add(flowButtons);
            panelButtons.Controls.Add(lblStatus);

            // ═══════════════════════════════════════════════════════
            //  DataGridView — line number list
            // ═══════════════════════════════════════════════════════
            dgvLines = new DataGridView
            {
                Dock = DockStyle.Fill,
                Font = baseFont,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 30,
                EnableHeadersVisualStyles = false,
                GridColor = Color.FromArgb(230, 230, 230),
                MultiSelect = false,
                ReadOnly = false,
                RowHeadersVisible = false,
                RowTemplate = { Height = 26 },
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            // Header style
            dgvLines.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(55, 71, 79),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 9F),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(4, 0, 0, 0),
                SelectionBackColor = Color.FromArgb(55, 71, 79),
                SelectionForeColor = Color.White
            };

            // Default cell style
            dgvLines.DefaultCellStyle = new DataGridViewCellStyle
            {
                SelectionBackColor = Color.FromArgb(200, 230, 255),
                SelectionForeColor = Color.Black,
                Padding = new Padding(4, 0, 0, 0)
            };

            // Alternating row style
            dgvLines.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(245, 247, 250),
                SelectionBackColor = Color.FromArgb(200, 230, 255),
                SelectionForeColor = Color.Black
            };

            // --- Columns ---
            colVisible = new DataGridViewCheckBoxColumn
            {
                Name = "colVisible",
                HeaderText = "👁",
                Width = 36,
                MinimumWidth = 36,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                FillWeight = 10,
                FlatStyle = FlatStyle.Standard,
                TrueValue = true,
                FalseValue = false,
                ReadOnly = false
            };

            colLineNumber = new DataGridViewTextBoxColumn
            {
                Name = "colLineNumber",
                HeaderText = "Line Number",
                FillWeight = 70,
                ReadOnly = true
            };

            colCount = new DataGridViewTextBoxColumn
            {
                Name = "colCount",
                HeaderText = "Parts",
                Width = 50,
                MinimumWidth = 45,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                FillWeight = 20,
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                }
            };

            dgvLines.Columns.AddRange(colVisible, colLineNumber, colCount);

            // Wire events for immediate checkbox toggle
            dgvLines.CellContentClick += DgvLines_CellContentClick;
            dgvLines.CellValueChanged += DgvLines_CellValueChanged;

            // ═══════════════════════════════════════════════════════
            //  Assemble form
            // ═══════════════════════════════════════════════════════
            SuspendLayout();
            Controls.Add(dgvLines);      // Fill — added first so it fills remaining space
            Controls.Add(panelTop);      // Top
            Controls.Add(panelButtons);  // Bottom
            Name = "LineVisibilityForm";
            Size = new Size(320, 500);
            ResumeLayout(false);
        }

        private static Button CreateButton(string text, string tooltip)
        {
            var btn = new Button
            {
                Text = text,
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.25F),
                Height = 28,
                Margin = new Padding(2),
                Padding = new Padding(4, 0, 4, 0),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(180, 180, 180);
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(220, 235, 252);

            var tt = new ToolTip();
            tt.SetToolTip(btn, tooltip);
            return btn;
        }
    }
}
