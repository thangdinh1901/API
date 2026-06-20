namespace BoxExtrudeAddIn
{
    partial class BoxForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblLength = new System.Windows.Forms.Label();
            this.lblWidth = new System.Windows.Forms.Label();
            this.lblHeight = new System.Windows.Forms.Label();
            this.lblUnit = new System.Windows.Forms.Label();
            this.txtLength = new System.Windows.Forms.TextBox();
            this.txtWidth = new System.Windows.Forms.TextBox();
            this.txtHeight = new System.Windows.Forms.TextBox();
            this.btnCreate = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // lblTitle
            //
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.lblTitle.Location = new System.Drawing.Point(16, 14);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(198, 20);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Tạo khối Box (Extrude)";
            //
            // lblUnit
            //
            this.lblUnit.AutoSize = true;
            this.lblUnit.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lblUnit.Location = new System.Drawing.Point(17, 40);
            this.lblUnit.Name = "lblUnit";
            this.lblUnit.Size = new System.Drawing.Size(220, 15);
            this.lblUnit.TabIndex = 1;
            this.lblUnit.Text = "Đơn vị: cm (đơn vị database Inventor)";
            //
            // lblLength
            //
            this.lblLength.AutoSize = true;
            this.lblLength.Location = new System.Drawing.Point(17, 72);
            this.lblLength.Name = "lblLength";
            this.lblLength.Size = new System.Drawing.Size(63, 15);
            this.lblLength.TabIndex = 2;
            this.lblLength.Text = "Chiều dài:";
            //
            // lblWidth
            //
            this.lblWidth.AutoSize = true;
            this.lblWidth.Location = new System.Drawing.Point(17, 108);
            this.lblWidth.Name = "lblWidth";
            this.lblWidth.Size = new System.Drawing.Size(74, 15);
            this.lblWidth.TabIndex = 4;
            this.lblWidth.Text = "Chiều rộng:";
            //
            // lblHeight
            //
            this.lblHeight.AutoSize = true;
            this.lblHeight.Location = new System.Drawing.Point(17, 144);
            this.lblHeight.Name = "lblHeight";
            this.lblHeight.Size = new System.Drawing.Size(64, 15);
            this.lblHeight.TabIndex = 6;
            this.lblHeight.Text = "Chiều cao:";
            //
            // txtLength
            //
            this.txtLength.Location = new System.Drawing.Point(120, 69);
            this.txtLength.Name = "txtLength";
            this.txtLength.Size = new System.Drawing.Size(160, 23);
            this.txtLength.TabIndex = 3;
            this.txtLength.Text = "10";
            //
            // txtWidth
            //
            this.txtWidth.Location = new System.Drawing.Point(120, 105);
            this.txtWidth.Name = "txtWidth";
            this.txtWidth.Size = new System.Drawing.Size(160, 23);
            this.txtWidth.TabIndex = 5;
            this.txtWidth.Text = "8";
            //
            // txtHeight
            //
            this.txtHeight.Location = new System.Drawing.Point(120, 141);
            this.txtHeight.Name = "txtHeight";
            this.txtHeight.Size = new System.Drawing.Size(160, 23);
            this.txtHeight.TabIndex = 7;
            this.txtHeight.Text = "5";
            //
            // btnCreate
            //
            this.btnCreate.Location = new System.Drawing.Point(120, 184);
            this.btnCreate.Name = "btnCreate";
            this.btnCreate.Size = new System.Drawing.Size(160, 32);
            this.btnCreate.TabIndex = 8;
            this.btnCreate.Text = "Tạo Box";
            this.btnCreate.UseVisualStyleBackColor = true;
            this.btnCreate.Click += new System.EventHandler(this.btnCreate_Click);
            //
            // BoxForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(304, 236);
            this.Controls.Add(this.btnCreate);
            this.Controls.Add(this.txtHeight);
            this.Controls.Add(this.txtWidth);
            this.Controls.Add(this.txtLength);
            this.Controls.Add(this.lblHeight);
            this.Controls.Add(this.lblWidth);
            this.Controls.Add(this.lblLength);
            this.Controls.Add(this.lblUnit);
            this.Controls.Add(this.lblTitle);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "BoxForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Box Extrude - Inventor API";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblLength;
        private System.Windows.Forms.Label lblWidth;
        private System.Windows.Forms.Label lblHeight;
        private System.Windows.Forms.Label lblUnit;
        private System.Windows.Forms.TextBox txtLength;
        private System.Windows.Forms.TextBox txtWidth;
        private System.Windows.Forms.TextBox txtHeight;
        private System.Windows.Forms.Button btnCreate;
    }
}
