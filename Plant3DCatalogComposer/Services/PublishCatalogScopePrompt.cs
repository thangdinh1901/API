using System.Drawing;
using System.Windows.Forms;

namespace Plant3DCatalogComposer.Services
{
    internal enum PublishCatalogScope
    {
        Cancelled,
        CurrentPart,
        AllParts,
    }

    internal static class PublishCatalogScopePrompt
    {
        public static PublishCatalogScope Show(IWin32Window? owner, string partId)
        {
            using var form = new Form
            {
                Text = "Publish Catalog",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                ClientSize = new Size(400, 168),
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false,
            };

            var lbl = new Label
            {
                AutoSize = false,
                Location = new Point(12, 12),
                Size = new Size(376, 36),
                Text = "Export Catalog Builder Excel — which parts?",
            };

            var rbCurrent = new RadioButton
            {
                AutoSize = true,
                Location = new Point(16, 52),
                Text = $"Current part only — one sheet ({partId})",
                Checked = true,
            };

            var rbAll = new RadioButton
            {
                AutoSize = true,
                Location = new Point(16, 76),
                Text = "All registered parts",
            };

            var lblHint = new Label
            {
                AutoSize = false,
                ForeColor = SystemColors.GrayText,
                Location = new Point(32, 98),
                Size = new Size(356, 32),
                Text = "Current part: workbook contains only that part's sheet (no pipe/stud/other families).",
            };

            var btnOk = new Button
            {
                DialogResult = DialogResult.OK,
                Location = new Point(232, 128),
                Size = new Size(75, 28),
                Text = "OK",
            };

            var btnCancel = new Button
            {
                DialogResult = DialogResult.Cancel,
                Location = new Point(313, 128),
                Size = new Size(75, 28),
                Text = "Cancel",
            };

            form.Controls.AddRange(new Control[] { lbl, rbCurrent, rbAll, lblHint, btnOk, btnCancel });
            form.AcceptButton = btnOk;
            form.CancelButton = btnCancel;

            return form.ShowDialog(owner) != DialogResult.OK
                ? PublishCatalogScope.Cancelled
                : rbAll.Checked
                    ? PublishCatalogScope.AllParts
                    : PublishCatalogScope.CurrentPart;
        }
    }
}
