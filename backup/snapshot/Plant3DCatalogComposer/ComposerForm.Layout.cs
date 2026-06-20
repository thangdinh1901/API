using System;
using System.Drawing;
using System.Windows.Forms;

namespace Plant3DCatalogComposer
{
    public partial class ComposerForm
    {
        private const int FieldLabelLeft = 8;
        private const int FieldValueLeft = 76;
        private const int FieldRightPad = 8;
        private const int CatalogLabelWidth = 82;
        private const int CatalogValueLeft = 96;
        private bool _catalogLayoutSizing;

        private static class PortFieldLayout
        {
            public const int LabelWidth = 68;
            public const int ComboFieldWidth = 160;
            public const int PortNumberTop = 152;
            public const int EndTypeTop = 182;
            public const int ParentTop = 210;
            public const int PositionTop = 238;
            public const int DirectionTop = 266;
        }

        private int TabContentWidth(Control tab) =>
            Math.Max(200, tab.ClientSize.Width - FieldRightPad * 2);

        private int ValueColumnWidth(Control host) =>
            Math.Max(120, host.ClientSize.Width - FieldValueLeft - FieldRightPad);

        private void LayoutCatalogProjectFields()
        {
            if (_grpCatalogProject == null || _txtCatalogName == null)
                return;

            const int rowH = 23;
            const int rowGap = 30;
            const int previewAfterName = 6;
            const int afterPreviewGap = 12;
            const int applyGap = 12;

            int valueW = Math.Max(
                100,
                _grpCatalogProject.ClientSize.Width - CatalogValueLeft - FieldRightPad);

            void PlaceRow(Label? label, Control value, int top)
            {
                if (label != null)
                {
                    label.AutoSize = false;
                    label.SetBounds(FieldLabelLeft, top, CatalogLabelWidth, rowH);
                    label.TextAlign = ContentAlignment.MiddleLeft;
                }

                value.SetBounds(CatalogValueLeft, top, valueW, rowH);
            }

            int row = 16;
            PlaceRow(_lblCatalogName, _txtCatalogName, row);
            int previewY = row + rowH + previewAfterName;
            _lblScriptPreview!.Location = new Point(CatalogValueLeft, previewY);

            row = previewY + 16 + afterPreviewGap;
            PlaceRow(_lblCatalogGroup, _cmbPlantGroup!, row);

            row += rowGap;
            PlaceRow(_lblCatalogDn, _cmbProjectDn!, row);

            row += rowGap;
            PlaceRow(_lblCatalogDn2, _cmbProjectDn2!, row);

            row += rowGap;
            PlaceRow(_lblCatalogClass, _cmbProjectPressureClass!, row);

            row += rowGap;
            PlaceRow(_lblCatalogSchedule, _cmbProjectSchedule!, row);

            row += rowGap;
            if (_lblCatalogTip != null)
            {
                _lblCatalogTip.AutoSize = false;
                _lblCatalogTip.SetBounds(FieldLabelLeft, row, CatalogLabelWidth, rowH);
                _lblCatalogTip.TextAlign = ContentAlignment.MiddleLeft;
            }

            int half = Math.Max(60, (valueW - 6) / 2);
            _txtTooltipShort!.SetBounds(CatalogValueLeft, row, half, rowH);
            _txtTooltipLong!.SetBounds(CatalogValueLeft + half + 6, row, valueW - half - 6, rowH);

            if (_btnApplyCatalogProject == null)
                return;

            const int applyH = 26;
            int applyY = row + rowH + applyGap;
            int applyX = (_grpCatalogProject.ClientSize.Width - _btnApplyCatalogProject.Width) / 2;
            _btnApplyCatalogProject.Location = new Point(Math.Max(FieldRightPad, applyX), applyY);

            const int groupChrome = 26;
            int targetHeight = applyY + applyH + groupChrome;
            if (!_catalogLayoutSizing && Math.Abs(_grpCatalogProject.Height - targetHeight) > 1)
            {
                _catalogLayoutSizing = true;
                _grpCatalogProject.Height = targetHeight;
                _catalogLayoutSizing = false;
            }
        }

        private void ApplyPortFieldLayout()
        {
            int valueW = ValueColumnWidth(tabPortManager);
            int comboW = Math.Min(PortFieldLayout.ComboFieldWidth, valueW);
            int gap = 6;
            int cellW = Math.Max(44, (valueW - gap * 2) / 3);

            void AlignLabel(Label label, int top, string text)
            {
                label.AutoSize = false;
                label.Location = new Point(FieldLabelLeft, top);
                label.Size = new Size(PortFieldLayout.LabelWidth, 23);
                label.Text = text;
                label.TextAlign = ContentAlignment.MiddleLeft;
            }

            void PlaceComboRow(Control field, int top)
            {
                field.SetBounds(FieldValueLeft, top, comboW, 23);
                field.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            }

            AlignLabel(lblPortNumber, PortFieldLayout.PortNumberTop, "Port #");
            PlaceComboRow(txtPortNumber, PortFieldLayout.PortNumberTop);
            txtPortNumber.TextAlign = HorizontalAlignment.Left;

            AlignLabel(lblPortType, PortFieldLayout.EndTypeTop, "End type");
            PlaceComboRow(cmbPortType, PortFieldLayout.EndTypeTop);

            AlignLabel(lblPortParent, PortFieldLayout.ParentTop, "Parent");
            PlaceComboRow(cmbPortParent, PortFieldLayout.ParentTop);

            int x1 = FieldValueLeft;
            int x2 = x1 + cellW + gap;
            int x3 = x2 + cellW + gap;

            AlignLabel(lblPortPos, PortFieldLayout.PositionTop, "Position");
            txtPortX.SetBounds(x1, PortFieldLayout.PositionTop, cellW, 23);
            txtPortY.SetBounds(x2, PortFieldLayout.PositionTop, cellW, 23);
            txtPortZ.SetBounds(x3, PortFieldLayout.PositionTop, cellW, 23);

            AlignLabel(lblPortDir, PortFieldLayout.DirectionTop, "Direction");
            txtPortDx.SetBounds(x1, PortFieldLayout.DirectionTop, cellW, 23);
            txtPortDy.SetBounds(x2, PortFieldLayout.DirectionTop, cellW, 23);
            txtPortDz.SetBounds(x3, PortFieldLayout.DirectionTop, cellW, 23);

            foreach (TextBox box in new[] { txtPortX, txtPortY, txtPortZ, txtPortDx, txtPortDy, txtPortDz })
                box.Anchor = AnchorStyles.Top | AnchorStyles.Left;

            lblPortNumber.BringToFront();
            txtPortNumber.BringToFront();
        }
    }
}
