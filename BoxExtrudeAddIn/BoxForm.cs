using System;
using System.Globalization;
using System.Windows.Forms;

namespace BoxExtrudeAddIn
{
    public partial class BoxForm : Form
    {
        private readonly Inventor.Application _inventorApp;

        public BoxForm(Inventor.Application inventorApp)
        {
            _inventorApp = inventorApp ?? throw new ArgumentNullException(nameof(inventorApp));
            InitializeComponent();
        }

        private void btnCreate_Click(object sender, EventArgs e)
        {
            if (!TryParseDimension(txtLength.Text, out double length) ||
                !TryParseDimension(txtWidth.Text, out double width) ||
                !TryParseDimension(txtHeight.Text, out double height))
            {
                MessageBox.Show(
                    "Vui lòng nhập số hợp lệ cho Chiều dài, Chiều rộng và Chiều cao.",
                    "Lỗi nhập liệu",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            try
            {
                BoxExtrudeService.CreateBox(_inventorApp, length, width, height);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Lỗi Inventor API",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private static bool TryParseDimension(string text, out double value)
        {
            return double.TryParse(
                text.Trim().Replace(',', '.'),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out value);
        }
    }
}
