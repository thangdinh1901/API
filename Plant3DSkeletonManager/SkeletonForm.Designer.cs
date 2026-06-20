namespace Plant3DSkeletonManager
{
    partial class SkeletonForm
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
            this.tabMain = new System.Windows.Forms.TabControl();
            this.tabSetup = new System.Windows.Forms.TabPage();
            this.tabScene = new System.Windows.Forms.TabPage();
            this.tabBooleans = new System.Windows.Forms.TabPage();
            this.lblValveType = new System.Windows.Forms.Label();
            this.lblDN = new System.Windows.Forms.Label();
            this.lblPressureClass = new System.Windows.Forms.Label();
            this.cmbValveType = new System.Windows.Forms.ComboBox();
            this.txtDN = new System.Windows.Forms.TextBox();
            this.txtPressureClass = new System.Windows.Forms.TextBox();
            this.btnCreateSkeleton = new System.Windows.Forms.Button();
            this.lblPrimitive = new System.Windows.Forms.Label();
            this.cmbPrimitive = new System.Windows.Forms.ComboBox();
            this.btnInsertPrimitive = new System.Windows.Forms.Button();
            this.btnSync = new System.Windows.Forms.Button();
            this.btnExport = new System.Windows.Forms.Button();
            this.btnImportJson = new System.Windows.Forms.Button();
            this.btnRebuildScene = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.dgvParams = new System.Windows.Forms.DataGridView();
            this.colName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.treeScene = new System.Windows.Forms.TreeView();
            this.btnRefreshTree = new System.Windows.Forms.Button();
            this.btnDeleteNode = new System.Windows.Forms.Button();
            this.btnDuplicateNode = new System.Windows.Forms.Button();
            this.btnSelectInInventor = new System.Windows.Forms.Button();
            this.lblNodeName = new System.Windows.Forms.Label();
            this.txtNodeName = new System.Windows.Forms.TextBox();
            this.lblNodeType = new System.Windows.Forms.Label();
            this.lblOrigin = new System.Windows.Forms.Label();
            this.txtOriginX = new System.Windows.Forms.TextBox();
            this.txtOriginY = new System.Windows.Forms.TextBox();
            this.txtOriginZ = new System.Windows.Forms.TextBox();
            this.dgvNodeParams = new System.Windows.Forms.DataGridView();
            this.colNodeParam = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colNodeValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colNodeExpr = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.btnResolveExpr = new System.Windows.Forms.Button();
            this.btnApplyNode = new System.Windows.Forms.Button();
            this.lblSceneStatus = new System.Windows.Forms.Label();
            this.grpPosition = new System.Windows.Forms.GroupBox();
            this.lblPosStep = new System.Windows.Forms.Label();
            this.numPosStep = new System.Windows.Forms.NumericUpDown();
            this.btnPosYPlus = new System.Windows.Forms.Button();
            this.btnPosXMinus = new System.Windows.Forms.Button();
            this.btnPosXPlus = new System.Windows.Forms.Button();
            this.btnPosYMinus = new System.Windows.Forms.Button();
            this.btnPosZPlus = new System.Windows.Forms.Button();
            this.btnPosZMinus = new System.Windows.Forms.Button();
            this.grpRotation = new System.Windows.Forms.GroupBox();
            this.lblAngleStep = new System.Windows.Forms.Label();
            this.numAngleStep = new System.Windows.Forms.NumericUpDown();
            this.btnRotX = new System.Windows.Forms.Button();
            this.btnRotY = new System.Windows.Forms.Button();
            this.btnRotZ = new System.Windows.Forms.Button();
            this.lvBoolOps = new System.Windows.Forms.ListView();
            this.lblBoolType = new System.Windows.Forms.Label();
            this.cmbBoolType = new System.Windows.Forms.ComboBox();
            this.lblBoolTarget = new System.Windows.Forms.Label();
            this.lblBoolTargetValue = new System.Windows.Forms.Label();
            this.clbBoolTools = new System.Windows.Forms.CheckedListBox();
            this.btnAddBoolOp = new System.Windows.Forms.Button();
            this.btnRemoveBoolOp = new System.Windows.Forms.Button();
            this.btnApplyBoolVisuals = new System.Windows.Forms.Button();
            this.lblBoolStatus = new System.Windows.Forms.Label();
            this.tabMain.SuspendLayout();
            this.tabSetup.SuspendLayout();
            this.tabScene.SuspendLayout();
            this.tabBooleans.SuspendLayout();
            this.grpPosition.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numPosStep)).BeginInit();
            this.grpRotation.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numAngleStep)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvParams)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvNodeParams)).BeginInit();
            this.SuspendLayout();
            //
            // tabMain
            //
            this.tabMain.Controls.Add(this.tabSetup);
            this.tabMain.Controls.Add(this.tabScene);
            this.tabMain.Controls.Add(this.tabBooleans);
            this.tabMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabMain.Location = new System.Drawing.Point(0, 0);
            this.tabMain.Name = "tabMain";
            this.tabMain.SelectedIndex = 0;
            this.tabMain.Size = new System.Drawing.Size(320, 720);
            this.tabMain.TabIndex = 0;
            //
            // tabSetup
            //
            this.tabSetup.Controls.Add(this.lblStatus);
            this.tabSetup.Controls.Add(this.btnRebuildScene);
            this.tabSetup.Controls.Add(this.btnImportJson);
            this.tabSetup.Controls.Add(this.btnExport);
            this.tabSetup.Controls.Add(this.btnSync);
            this.tabSetup.Controls.Add(this.btnCreateSkeleton);
            this.tabSetup.Controls.Add(this.dgvParams);
            this.tabSetup.Controls.Add(this.txtPressureClass);
            this.tabSetup.Controls.Add(this.lblPressureClass);
            this.tabSetup.Controls.Add(this.txtDN);
            this.tabSetup.Controls.Add(this.lblDN);
            this.tabSetup.Controls.Add(this.cmbValveType);
            this.tabSetup.Controls.Add(this.lblValveType);
            this.tabSetup.Location = new System.Drawing.Point(4, 24);
            this.tabSetup.Name = "tabSetup";
            this.tabSetup.Padding = new System.Windows.Forms.Padding(8);
            this.tabSetup.Size = new System.Drawing.Size(312, 592);
            this.tabSetup.TabIndex = 0;
            this.tabSetup.Text = "Setup";
            this.tabSetup.UseVisualStyleBackColor = true;
            //
            // tabScene
            //
            this.tabScene.Controls.Add(this.lblSceneStatus);
            this.tabScene.Controls.Add(this.btnInsertPrimitive);
            this.tabScene.Controls.Add(this.cmbPrimitive);
            this.tabScene.Controls.Add(this.lblPrimitive);
            this.tabScene.Controls.Add(this.btnApplyNode);
            this.tabScene.Controls.Add(this.btnResolveExpr);
            this.tabScene.Controls.Add(this.dgvNodeParams);
            this.tabScene.Controls.Add(this.grpRotation);
            this.tabScene.Controls.Add(this.grpPosition);
            this.tabScene.Controls.Add(this.txtOriginZ);
            this.tabScene.Controls.Add(this.txtOriginY);
            this.tabScene.Controls.Add(this.txtOriginX);
            this.tabScene.Controls.Add(this.lblOrigin);
            this.tabScene.Controls.Add(this.lblNodeType);
            this.tabScene.Controls.Add(this.txtNodeName);
            this.tabScene.Controls.Add(this.lblNodeName);
            this.tabScene.Controls.Add(this.btnSelectInInventor);
            this.tabScene.Controls.Add(this.btnDuplicateNode);
            this.tabScene.Controls.Add(this.btnDeleteNode);
            this.tabScene.Controls.Add(this.btnRefreshTree);
            this.tabScene.Controls.Add(this.treeScene);
            this.tabScene.Location = new System.Drawing.Point(4, 24);
            this.tabScene.Name = "tabScene";
            this.tabScene.Padding = new System.Windows.Forms.Padding(8);
            this.tabScene.Size = new System.Drawing.Size(312, 692);
            this.tabScene.TabIndex = 1;
            this.tabScene.Text = "Scene";
            this.tabScene.UseVisualStyleBackColor = true;
            //
            // tabBooleans
            //
            this.tabBooleans.Controls.Add(this.lblBoolStatus);
            this.tabBooleans.Controls.Add(this.btnApplyBoolVisuals);
            this.tabBooleans.Controls.Add(this.btnRemoveBoolOp);
            this.tabBooleans.Controls.Add(this.btnAddBoolOp);
            this.tabBooleans.Controls.Add(this.clbBoolTools);
            this.tabBooleans.Controls.Add(this.lblBoolTargetValue);
            this.tabBooleans.Controls.Add(this.lblBoolTarget);
            this.tabBooleans.Controls.Add(this.cmbBoolType);
            this.tabBooleans.Controls.Add(this.lblBoolType);
            this.tabBooleans.Controls.Add(this.lvBoolOps);
            this.tabBooleans.Location = new System.Drawing.Point(4, 24);
            this.tabBooleans.Name = "tabBooleans";
            this.tabBooleans.Padding = new System.Windows.Forms.Padding(8);
            this.tabBooleans.Size = new System.Drawing.Size(312, 692);
            this.tabBooleans.TabIndex = 2;
            this.tabBooleans.Text = "Booleans";
            this.tabBooleans.UseVisualStyleBackColor = true;
            this.tabBooleans.Enter += new System.EventHandler(this.tabBooleans_Enter);
            //
            // lvBoolOps
            //
            this.lvBoolOps.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;
            this.lvBoolOps.FullRowSelect = true;
            this.lvBoolOps.GridLines = true;
            this.lvBoolOps.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.lvBoolOps.HideSelection = false;
            this.lvBoolOps.Location = new System.Drawing.Point(11, 11);
            this.lvBoolOps.MultiSelect = false;
            this.lvBoolOps.Name = "lvBoolOps";
            this.lvBoolOps.Size = new System.Drawing.Size(285, 100);
            this.lvBoolOps.TabIndex = 0;
            this.lvBoolOps.UseCompatibleStateImageBehavior = false;
            this.lvBoolOps.View = System.Windows.Forms.View.Details;
            //
            // lblBoolType
            //
            this.lblBoolType.AutoSize = true;
            this.lblBoolType.Location = new System.Drawing.Point(11, 122);
            this.lblBoolType.Name = "lblBoolType";
            this.lblBoolType.Size = new System.Drawing.Size(35, 15);
            this.lblBoolType.TabIndex = 1;
            this.lblBoolType.Text = "Type:";
            //
            // cmbBoolType
            //
            this.cmbBoolType.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;
            this.cmbBoolType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbBoolType.Location = new System.Drawing.Point(110, 119);
            this.cmbBoolType.Name = "cmbBoolType";
            this.cmbBoolType.Size = new System.Drawing.Size(186, 23);
            this.cmbBoolType.TabIndex = 2;
            //
            // lblBoolTarget
            //
            this.lblBoolTarget.AutoSize = true;
            this.lblBoolTarget.Location = new System.Drawing.Point(11, 150);
            this.lblBoolTarget.Name = "lblBoolTarget";
            this.lblBoolTarget.Size = new System.Drawing.Size(45, 15);
            this.lblBoolTarget.TabIndex = 3;
            this.lblBoolTarget.Text = "Target:";
            //
            // lblBoolTargetValue
            //
            this.lblBoolTargetValue.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;
            this.lblBoolTargetValue.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lblBoolTargetValue.Location = new System.Drawing.Point(110, 150);
            this.lblBoolTargetValue.Name = "lblBoolTargetValue";
            this.lblBoolTargetValue.Size = new System.Drawing.Size(186, 15);
            this.lblBoolTargetValue.TabIndex = 4;
            this.lblBoolTargetValue.Text = "(select node on Scene tab)";
            //
            // clbBoolTools
            //
            this.clbBoolTools.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;
            this.clbBoolTools.CheckOnClick = true;
            this.clbBoolTools.FormattingEnabled = true;
            this.clbBoolTools.Location = new System.Drawing.Point(11, 172);
            this.clbBoolTools.Name = "clbBoolTools";
            this.clbBoolTools.Size = new System.Drawing.Size(285, 112);
            this.clbBoolTools.TabIndex = 5;
            //
            // btnAddBoolOp
            //
            this.btnAddBoolOp.Location = new System.Drawing.Point(11, 294);
            this.btnAddBoolOp.Name = "btnAddBoolOp";
            this.btnAddBoolOp.Size = new System.Drawing.Size(88, 28);
            this.btnAddBoolOp.TabIndex = 6;
            this.btnAddBoolOp.Text = "Add";
            this.btnAddBoolOp.UseVisualStyleBackColor = true;
            this.btnAddBoolOp.Click += new System.EventHandler(this.btnAddBoolOp_Click);
            //
            // btnRemoveBoolOp
            //
            this.btnRemoveBoolOp.Location = new System.Drawing.Point(105, 294);
            this.btnRemoveBoolOp.Name = "btnRemoveBoolOp";
            this.btnRemoveBoolOp.Size = new System.Drawing.Size(88, 28);
            this.btnRemoveBoolOp.TabIndex = 7;
            this.btnRemoveBoolOp.Text = "Remove";
            this.btnRemoveBoolOp.UseVisualStyleBackColor = true;
            this.btnRemoveBoolOp.Click += new System.EventHandler(this.btnRemoveBoolOp_Click);
            //
            // btnApplyBoolVisuals
            //
            this.btnApplyBoolVisuals.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Right;
            this.btnApplyBoolVisuals.Location = new System.Drawing.Point(199, 294);
            this.btnApplyBoolVisuals.Name = "btnApplyBoolVisuals";
            this.btnApplyBoolVisuals.Size = new System.Drawing.Size(97, 28);
            this.btnApplyBoolVisuals.TabIndex = 8;
            this.btnApplyBoolVisuals.Text = "Apply Visuals";
            this.btnApplyBoolVisuals.UseVisualStyleBackColor = true;
            this.btnApplyBoolVisuals.Click += new System.EventHandler(this.btnApplyBoolVisuals_Click);
            //
            // lblBoolStatus
            //
            this.lblBoolStatus.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;
            this.lblBoolStatus.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lblBoolStatus.Location = new System.Drawing.Point(11, 330);
            this.lblBoolStatus.Name = "lblBoolStatus";
            this.lblBoolStatus.Size = new System.Drawing.Size(285, 40);
            this.lblBoolStatus.TabIndex = 9;
            //
            // lblValveType
            //
            this.lblValveType.AutoSize = true;
            this.lblValveType.Location = new System.Drawing.Point(11, 15);
            this.lblValveType.Name = "lblValveType";
            this.lblValveType.Size = new System.Drawing.Size(65, 15);
            this.lblValveType.TabIndex = 0;
            this.lblValveType.Text = "Valve Type:";
            //
            // cmbValveType
            //
            this.cmbValveType.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;
            this.cmbValveType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbValveType.Location = new System.Drawing.Point(110, 12);
            this.cmbValveType.Name = "cmbValveType";
            this.cmbValveType.Size = new System.Drawing.Size(186, 23);
            this.cmbValveType.TabIndex = 1;
            //
            // lblDN
            //
            this.lblDN.AutoSize = true;
            this.lblDN.Location = new System.Drawing.Point(11, 50);
            this.lblDN.Name = "lblDN";
            this.lblDN.Size = new System.Drawing.Size(27, 15);
            this.lblDN.TabIndex = 2;
            this.lblDN.Text = "DN:";
            //
            // txtDN
            //
            this.txtDN.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;
            this.txtDN.Location = new System.Drawing.Point(110, 47);
            this.txtDN.Name = "txtDN";
            this.txtDN.Size = new System.Drawing.Size(186, 23);
            this.txtDN.TabIndex = 3;
            this.txtDN.Text = "50";
            //
            // lblPressureClass
            //
            this.lblPressureClass.AutoSize = true;
            this.lblPressureClass.Location = new System.Drawing.Point(11, 85);
            this.lblPressureClass.Name = "lblPressureClass";
            this.lblPressureClass.Size = new System.Drawing.Size(85, 15);
            this.lblPressureClass.TabIndex = 4;
            this.lblPressureClass.Text = "Pressure Class:";
            //
            // txtPressureClass
            //
            this.txtPressureClass.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;
            this.txtPressureClass.Location = new System.Drawing.Point(110, 82);
            this.txtPressureClass.Name = "txtPressureClass";
            this.txtPressureClass.Size = new System.Drawing.Size(186, 23);
            this.txtPressureClass.TabIndex = 5;
            this.txtPressureClass.Text = "150";
            //
            // dgvParams
            //
            this.dgvParams.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;
            this.dgvParams.AllowUserToAddRows = false;
            this.dgvParams.AllowUserToDeleteRows = false;
            this.dgvParams.AllowUserToResizeRows = false;
            this.dgvParams.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvParams.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvParams.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                this.colName,
                this.colValue});
            this.dgvParams.Location = new System.Drawing.Point(11, 120);
            this.dgvParams.MultiSelect = false;
            this.dgvParams.Name = "dgvParams";
            this.dgvParams.RowHeadersVisible = false;
            this.dgvParams.Size = new System.Drawing.Size(285, 192);
            this.dgvParams.TabIndex = 6;
            //
            // colName
            //
            this.colName.HeaderText = "Parameter";
            this.colName.Name = "colName";
            this.colName.ReadOnly = true;
            //
            // colValue
            //
            this.colValue.HeaderText = "Value (mm)";
            this.colValue.Name = "colValue";
            //
            // btnCreateSkeleton
            //
            this.btnCreateSkeleton.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;
            this.btnCreateSkeleton.Location = new System.Drawing.Point(11, 320);
            this.btnCreateSkeleton.Name = "btnCreateSkeleton";
            this.btnCreateSkeleton.Size = new System.Drawing.Size(285, 32);
            this.btnCreateSkeleton.TabIndex = 7;
            this.btnCreateSkeleton.Text = "Create Skeleton";
            this.btnCreateSkeleton.UseVisualStyleBackColor = true;
            this.btnCreateSkeleton.Click += new System.EventHandler(this.btnCreateSkeleton_Click);
            //
            // lblPrimitive
            //
            this.lblPrimitive.AutoSize = true;
            this.lblPrimitive.Location = new System.Drawing.Point(11, 14);
            this.lblPrimitive.Name = "lblPrimitive";
            this.lblPrimitive.Size = new System.Drawing.Size(58, 15);
            this.lblPrimitive.TabIndex = 18;
            this.lblPrimitive.Text = "Primitive:";
            //
            // cmbPrimitive
            //
            this.cmbPrimitive.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;
            this.cmbPrimitive.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPrimitive.Location = new System.Drawing.Point(110, 11);
            this.cmbPrimitive.Name = "cmbPrimitive";
            this.cmbPrimitive.Size = new System.Drawing.Size(186, 23);
            this.cmbPrimitive.TabIndex = 19;
            //
            // btnInsertPrimitive
            //
            this.btnInsertPrimitive.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;
            this.btnInsertPrimitive.Location = new System.Drawing.Point(11, 40);
            this.btnInsertPrimitive.Name = "btnInsertPrimitive";
            this.btnInsertPrimitive.Size = new System.Drawing.Size(285, 28);
            this.btnInsertPrimitive.TabIndex = 20;
            this.btnInsertPrimitive.Text = "Insert Primitive";
            this.btnInsertPrimitive.UseVisualStyleBackColor = true;
            this.btnInsertPrimitive.Click += new System.EventHandler(this.btnInsertPrimitive_Click);
            //
            // btnSync
            //
            this.btnSync.Location = new System.Drawing.Point(11, 360);
            this.btnSync.Name = "btnSync";
            this.btnSync.Size = new System.Drawing.Size(138, 30);
            this.btnSync.TabIndex = 11;
            this.btnSync.Text = "Sync from Inventor";
            this.btnSync.UseVisualStyleBackColor = true;
            this.btnSync.Click += new System.EventHandler(this.btnSync_Click);
            //
            // btnExport
            //
            this.btnExport.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Right;
            this.btnExport.Location = new System.Drawing.Point(158, 360);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(138, 30);
            this.btnExport.TabIndex = 12;
            this.btnExport.Text = "Export JSON";
            this.btnExport.UseVisualStyleBackColor = true;
            this.btnExport.Click += new System.EventHandler(this.btnExport_Click);
            //
            // btnImportJson
            //
            this.btnImportJson.Location = new System.Drawing.Point(11, 398);
            this.btnImportJson.Name = "btnImportJson";
            this.btnImportJson.Size = new System.Drawing.Size(138, 30);
            this.btnImportJson.TabIndex = 14;
            this.btnImportJson.Text = "Import JSON";
            this.btnImportJson.UseVisualStyleBackColor = true;
            this.btnImportJson.Click += new System.EventHandler(this.btnImportJson_Click);
            //
            // btnRebuildScene
            //
            this.btnRebuildScene.Location = new System.Drawing.Point(158, 398);
            this.btnRebuildScene.Name = "btnRebuildScene";
            this.btnRebuildScene.Size = new System.Drawing.Size(138, 30);
            this.btnRebuildScene.TabIndex = 15;
            this.btnRebuildScene.Text = "Rebuild Scene";
            this.btnRebuildScene.UseVisualStyleBackColor = true;
            this.btnRebuildScene.Click += new System.EventHandler(this.btnRebuildScene_Click);
            //
            // lblStatus
            //
            this.lblStatus.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;
            this.lblStatus.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lblStatus.Location = new System.Drawing.Point(11, 436);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(285, 40);
            this.lblStatus.TabIndex = 13;
            //
            // treeScene
            //
            this.treeScene.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;
            this.treeScene.HideSelection = false;
            this.treeScene.Location = new System.Drawing.Point(11, 76);
            this.treeScene.Name = "treeScene";
            this.treeScene.Size = new System.Drawing.Size(285, 88);
            this.treeScene.TabIndex = 0;
            //
            // btnRefreshTree
            //
            this.btnRefreshTree.Location = new System.Drawing.Point(11, 168);
            this.btnRefreshTree.Name = "btnRefreshTree";
            this.btnRefreshTree.Size = new System.Drawing.Size(90, 26);
            this.btnRefreshTree.TabIndex = 1;
            this.btnRefreshTree.Text = "Refresh";
            this.btnRefreshTree.UseVisualStyleBackColor = true;
            this.btnRefreshTree.Click += new System.EventHandler(this.btnRefreshTree_Click);
            //
            // btnDeleteNode
            //
            this.btnDeleteNode.Location = new System.Drawing.Point(107, 168);
            this.btnDeleteNode.Name = "btnDeleteNode";
            this.btnDeleteNode.Size = new System.Drawing.Size(60, 26);
            this.btnDeleteNode.TabIndex = 2;
            this.btnDeleteNode.Text = "Delete";
            this.btnDeleteNode.UseVisualStyleBackColor = true;
            this.btnDeleteNode.Click += new System.EventHandler(this.btnDeleteNode_Click);
            //
            // btnDuplicateNode
            //
            this.btnDuplicateNode.Location = new System.Drawing.Point(173, 168);
            this.btnDuplicateNode.Name = "btnDuplicateNode";
            this.btnDuplicateNode.Size = new System.Drawing.Size(70, 26);
            this.btnDuplicateNode.TabIndex = 3;
            this.btnDuplicateNode.Text = "Duplicate";
            this.btnDuplicateNode.UseVisualStyleBackColor = true;
            this.btnDuplicateNode.Click += new System.EventHandler(this.btnDuplicateNode_Click);
            //
            // btnSelectInInventor
            //
            this.btnSelectInInventor.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Right;
            this.btnSelectInInventor.Location = new System.Drawing.Point(249, 168);
            this.btnSelectInInventor.Name = "btnSelectInInventor";
            this.btnSelectInInventor.Size = new System.Drawing.Size(47, 26);
            this.btnSelectInInventor.TabIndex = 4;
            this.btnSelectInInventor.Text = "Sel";
            this.btnSelectInInventor.UseVisualStyleBackColor = true;
            this.btnSelectInInventor.Click += new System.EventHandler(this.btnSelectInInventor_Click);
            //
            // lblNodeName
            //
            this.lblNodeName.AutoSize = true;
            this.lblNodeName.Location = new System.Drawing.Point(11, 202);
            this.lblNodeName.Name = "lblNodeName";
            this.lblNodeName.Size = new System.Drawing.Size(42, 15);
            this.lblNodeName.TabIndex = 5;
            this.lblNodeName.Text = "Name:";
            //
            // txtNodeName
            //
            this.txtNodeName.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;
            this.txtNodeName.Location = new System.Drawing.Point(60, 199);
            this.txtNodeName.Name = "txtNodeName";
            this.txtNodeName.Size = new System.Drawing.Size(236, 23);
            this.txtNodeName.TabIndex = 6;
            //
            // lblNodeType
            //
            this.lblNodeType.AutoSize = true;
            this.lblNodeType.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lblNodeType.Location = new System.Drawing.Point(11, 228);
            this.lblNodeType.Name = "lblNodeType";
            this.lblNodeType.Size = new System.Drawing.Size(45, 15);
            this.lblNodeType.TabIndex = 7;
            this.lblNodeType.Text = "(none)";
            //
            // lblOrigin
            //
            this.lblOrigin.AutoSize = true;
            this.lblOrigin.Location = new System.Drawing.Point(11, 252);
            this.lblOrigin.Name = "lblOrigin";
            this.lblOrigin.Size = new System.Drawing.Size(68, 15);
            this.lblOrigin.TabIndex = 8;
            this.lblOrigin.Text = "Origin (mm):";
            //
            // txtOriginX
            //
            this.txtOriginX.Location = new System.Drawing.Point(85, 249);
            this.txtOriginX.Name = "txtOriginX";
            this.txtOriginX.Size = new System.Drawing.Size(60, 23);
            this.txtOriginX.TabIndex = 9;
            //
            // txtOriginY
            //
            this.txtOriginY.Location = new System.Drawing.Point(151, 249);
            this.txtOriginY.Name = "txtOriginY";
            this.txtOriginY.Size = new System.Drawing.Size(60, 23);
            this.txtOriginY.TabIndex = 10;
            //
            // txtOriginZ
            //
            this.txtOriginZ.Location = new System.Drawing.Point(217, 249);
            this.txtOriginZ.Name = "txtOriginZ";
            this.txtOriginZ.Size = new System.Drawing.Size(60, 23);
            this.txtOriginZ.TabIndex = 11;
            //
            // grpPosition
            //
            this.grpPosition.Controls.Add(this.btnPosZMinus);
            this.grpPosition.Controls.Add(this.btnPosZPlus);
            this.grpPosition.Controls.Add(this.btnPosYMinus);
            this.grpPosition.Controls.Add(this.btnPosXPlus);
            this.grpPosition.Controls.Add(this.btnPosXMinus);
            this.grpPosition.Controls.Add(this.btnPosYPlus);
            this.grpPosition.Controls.Add(this.numPosStep);
            this.grpPosition.Controls.Add(this.lblPosStep);
            this.grpPosition.Location = new System.Drawing.Point(11, 280);
            this.grpPosition.Name = "grpPosition";
            this.grpPosition.Size = new System.Drawing.Size(285, 108);
            this.grpPosition.TabIndex = 16;
            this.grpPosition.TabStop = false;
            this.grpPosition.Text = "Position";
            //
            // lblPosStep
            //
            this.lblPosStep.AutoSize = true;
            this.lblPosStep.Location = new System.Drawing.Point(8, 22);
            this.lblPosStep.Name = "lblPosStep";
            this.lblPosStep.Size = new System.Drawing.Size(61, 15);
            this.lblPosStep.TabIndex = 0;
            this.lblPosStep.Text = "Step (mm):";
            //
            // numPosStep
            //
            this.numPosStep.DecimalPlaces = 1;
            this.numPosStep.Increment = new decimal(new int[] { 10, 0, 0, 0 });
            this.numPosStep.Location = new System.Drawing.Point(75, 20);
            this.numPosStep.Maximum = new decimal(new int[] { 10000, 0, 0, 0 });
            this.numPosStep.Minimum = new decimal(new int[] { 1, 0, 0, 65536 });
            this.numPosStep.Name = "numPosStep";
            this.numPosStep.Size = new System.Drawing.Size(60, 23);
            this.numPosStep.TabIndex = 1;
            this.numPosStep.Value = new decimal(new int[] { 10, 0, 0, 0 });
            //
            // btnPosYPlus
            //
            this.btnPosYPlus.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnPosYPlus.Location = new System.Drawing.Point(121, 44);
            this.btnPosYPlus.Name = "btnPosYPlus";
            this.btnPosYPlus.Size = new System.Drawing.Size(34, 26);
            this.btnPosYPlus.TabIndex = 2;
            this.btnPosYPlus.Text = "\u2191";
            this.btnPosYPlus.UseVisualStyleBackColor = true;
            this.btnPosYPlus.Click += new System.EventHandler(this.btnPosYPlus_Click);
            //
            // btnPosXMinus
            //
            this.btnPosXMinus.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnPosXMinus.Location = new System.Drawing.Point(81, 74);
            this.btnPosXMinus.Name = "btnPosXMinus";
            this.btnPosXMinus.Size = new System.Drawing.Size(34, 26);
            this.btnPosXMinus.TabIndex = 3;
            this.btnPosXMinus.Text = "\u2190";
            this.btnPosXMinus.UseVisualStyleBackColor = true;
            this.btnPosXMinus.Click += new System.EventHandler(this.btnPosXMinus_Click);
            //
            // btnPosXPlus
            //
            this.btnPosXPlus.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnPosXPlus.Location = new System.Drawing.Point(161, 74);
            this.btnPosXPlus.Name = "btnPosXPlus";
            this.btnPosXPlus.Size = new System.Drawing.Size(34, 26);
            this.btnPosXPlus.TabIndex = 4;
            this.btnPosXPlus.Text = "\u2192";
            this.btnPosXPlus.UseVisualStyleBackColor = true;
            this.btnPosXPlus.Click += new System.EventHandler(this.btnPosXPlus_Click);
            //
            // btnPosYMinus
            //
            this.btnPosYMinus.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnPosYMinus.Location = new System.Drawing.Point(121, 74);
            this.btnPosYMinus.Name = "btnPosYMinus";
            this.btnPosYMinus.Size = new System.Drawing.Size(34, 26);
            this.btnPosYMinus.TabIndex = 5;
            this.btnPosYMinus.Text = "\u2193";
            this.btnPosYMinus.UseVisualStyleBackColor = true;
            this.btnPosYMinus.Click += new System.EventHandler(this.btnPosYMinus_Click);
            //
            // btnPosZPlus
            //
            this.btnPosZPlus.Location = new System.Drawing.Point(232, 46);
            this.btnPosZPlus.Name = "btnPosZPlus";
            this.btnPosZPlus.Size = new System.Drawing.Size(40, 26);
            this.btnPosZPlus.TabIndex = 6;
            this.btnPosZPlus.Text = "Z+";
            this.btnPosZPlus.UseVisualStyleBackColor = true;
            this.btnPosZPlus.Click += new System.EventHandler(this.btnPosZPlus_Click);
            //
            // btnPosZMinus
            //
            this.btnPosZMinus.Location = new System.Drawing.Point(232, 74);
            this.btnPosZMinus.Name = "btnPosZMinus";
            this.btnPosZMinus.Size = new System.Drawing.Size(40, 26);
            this.btnPosZMinus.TabIndex = 7;
            this.btnPosZMinus.Text = "Z-";
            this.btnPosZMinus.UseVisualStyleBackColor = true;
            this.btnPosZMinus.Click += new System.EventHandler(this.btnPosZMinus_Click);
            //
            // grpRotation
            //
            this.grpRotation.Controls.Add(this.btnRotZ);
            this.grpRotation.Controls.Add(this.btnRotY);
            this.grpRotation.Controls.Add(this.btnRotX);
            this.grpRotation.Controls.Add(this.numAngleStep);
            this.grpRotation.Controls.Add(this.lblAngleStep);
            this.grpRotation.Location = new System.Drawing.Point(11, 394);
            this.grpRotation.Name = "grpRotation";
            this.grpRotation.Size = new System.Drawing.Size(285, 78);
            this.grpRotation.TabIndex = 17;
            this.grpRotation.TabStop = false;
            this.grpRotation.Text = "Rotation";
            //
            // lblAngleStep
            //
            this.lblAngleStep.AutoSize = true;
            this.lblAngleStep.Location = new System.Drawing.Point(8, 22);
            this.lblAngleStep.Name = "lblAngleStep";
            this.lblAngleStep.Size = new System.Drawing.Size(88, 15);
            this.lblAngleStep.TabIndex = 0;
            this.lblAngleStep.Text = "Angle (°):";
            //
            // numAngleStep
            //
            this.numAngleStep.DecimalPlaces = 1;
            this.numAngleStep.Increment = new decimal(new int[] { 90, 0, 0, 0 });
            this.numAngleStep.Location = new System.Drawing.Point(75, 20);
            this.numAngleStep.Maximum = new decimal(new int[] { 360, 0, 0, 0 });
            this.numAngleStep.Minimum = new decimal(new int[] { 360, 0, 0, -2147483648 });
            this.numAngleStep.Name = "numAngleStep";
            this.numAngleStep.Size = new System.Drawing.Size(70, 23);
            this.numAngleStep.TabIndex = 1;
            this.numAngleStep.Value = new decimal(new int[] { 90, 0, 0, 0 });
            //
            // btnRotX
            //
            this.btnRotX.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.btnRotX.Location = new System.Drawing.Point(8, 44);
            this.btnRotX.Name = "btnRotX";
            this.btnRotX.Size = new System.Drawing.Size(86, 28);
            this.btnRotX.TabIndex = 2;
            this.btnRotX.Text = "X";
            this.btnRotX.UseVisualStyleBackColor = true;
            this.btnRotX.Click += new System.EventHandler(this.btnRotX_Click);
            //
            // btnRotY
            //
            this.btnRotY.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.btnRotY.Location = new System.Drawing.Point(100, 44);
            this.btnRotY.Name = "btnRotY";
            this.btnRotY.Size = new System.Drawing.Size(86, 28);
            this.btnRotY.TabIndex = 3;
            this.btnRotY.Text = "Y";
            this.btnRotY.UseVisualStyleBackColor = true;
            this.btnRotY.Click += new System.EventHandler(this.btnRotY_Click);
            //
            // btnRotZ
            //
            this.btnRotZ.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.btnRotZ.Location = new System.Drawing.Point(192, 44);
            this.btnRotZ.Name = "btnRotZ";
            this.btnRotZ.Size = new System.Drawing.Size(82, 28);
            this.btnRotZ.TabIndex = 4;
            this.btnRotZ.Text = "Z";
            this.btnRotZ.UseVisualStyleBackColor = true;
            this.btnRotZ.Click += new System.EventHandler(this.btnRotZ_Click);
            //
            // dgvNodeParams
            //
            this.dgvNodeParams.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;
            this.dgvNodeParams.AllowUserToAddRows = false;
            this.dgvNodeParams.AllowUserToDeleteRows = false;
            this.dgvNodeParams.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvNodeParams.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvNodeParams.ColumnHeadersHeight = 22;
            this.dgvNodeParams.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                this.colNodeParam,
                this.colNodeValue,
                this.colNodeExpr});
            this.dgvNodeParams.Location = new System.Drawing.Point(11, 478);
            this.dgvNodeParams.Name = "dgvNodeParams";
            this.dgvNodeParams.RowHeadersVisible = false;
            this.dgvNodeParams.RowTemplate.Height = 22;
            this.dgvNodeParams.Size = new System.Drawing.Size(285, 112);
            this.dgvNodeParams.TabIndex = 12;
            //
            // colNodeParam
            //
            this.colNodeParam.HeaderText = "Param";
            this.colNodeParam.Name = "colNodeParam";
            this.colNodeParam.ReadOnly = true;
            //
            // colNodeValue
            //
            this.colNodeValue.HeaderText = "Value";
            this.colNodeValue.Name = "colNodeValue";
            //
            // colNodeExpr
            //
            this.colNodeExpr.HeaderText = "Expression";
            this.colNodeExpr.Name = "colNodeExpr";
            //
            // btnResolveExpr
            //
            this.btnResolveExpr.Location = new System.Drawing.Point(11, 598);
            this.btnResolveExpr.Name = "btnResolveExpr";
            this.btnResolveExpr.Size = new System.Drawing.Size(138, 30);
            this.btnResolveExpr.TabIndex = 13;
            this.btnResolveExpr.Text = "Resolve Expressions";
            this.btnResolveExpr.UseVisualStyleBackColor = true;
            this.btnResolveExpr.Click += new System.EventHandler(this.btnResolveExpr_Click);
            //
            // btnApplyNode
            //
            this.btnApplyNode.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Right;
            this.btnApplyNode.Location = new System.Drawing.Point(158, 598);
            this.btnApplyNode.Name = "btnApplyNode";
            this.btnApplyNode.Size = new System.Drawing.Size(138, 30);
            this.btnApplyNode.TabIndex = 14;
            this.btnApplyNode.Text = "Apply to Inventor";
            this.btnApplyNode.UseVisualStyleBackColor = true;
            this.btnApplyNode.Click += new System.EventHandler(this.btnApplyNode_Click);
            //
            // lblSceneStatus
            //
            this.lblSceneStatus.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;
            this.lblSceneStatus.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lblSceneStatus.Location = new System.Drawing.Point(11, 636);
            this.lblSceneStatus.Name = "lblSceneStatus";
            this.lblSceneStatus.Size = new System.Drawing.Size(285, 36);
            this.lblSceneStatus.TabIndex = 15;
            //
            // SkeletonForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(320, 720);
            this.Controls.Add(this.tabMain);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "SkeletonForm";
            this.Text = "Plant3D Skeleton Manager";
            this.tabMain.ResumeLayout(false);
            this.tabSetup.ResumeLayout(false);
            this.tabSetup.PerformLayout();
            this.tabScene.ResumeLayout(false);
            this.tabScene.PerformLayout();
            this.tabBooleans.ResumeLayout(false);
            this.tabBooleans.PerformLayout();
            this.grpPosition.ResumeLayout(false);
            this.grpPosition.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numPosStep)).EndInit();
            this.grpRotation.ResumeLayout(false);
            this.grpRotation.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numAngleStep)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvParams)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvNodeParams)).EndInit();
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.TabControl tabMain;
        private System.Windows.Forms.TabPage tabSetup;
        private System.Windows.Forms.TabPage tabScene;
        private System.Windows.Forms.TabPage tabBooleans;
        private System.Windows.Forms.Label lblValveType;
        private System.Windows.Forms.Label lblDN;
        private System.Windows.Forms.Label lblPressureClass;
        private System.Windows.Forms.ComboBox cmbValveType;
        private System.Windows.Forms.TextBox txtDN;
        private System.Windows.Forms.TextBox txtPressureClass;
        private System.Windows.Forms.Button btnCreateSkeleton;
        private System.Windows.Forms.Label lblPrimitive;
        private System.Windows.Forms.ComboBox cmbPrimitive;
        private System.Windows.Forms.Button btnInsertPrimitive;
        private System.Windows.Forms.Button btnSync;
        private System.Windows.Forms.Button btnExport;
        private System.Windows.Forms.Button btnImportJson;
        private System.Windows.Forms.Button btnRebuildScene;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.DataGridView dgvParams;
        private System.Windows.Forms.DataGridViewTextBoxColumn colName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colValue;
        private System.Windows.Forms.TreeView treeScene;
        private System.Windows.Forms.Button btnRefreshTree;
        private System.Windows.Forms.Button btnDeleteNode;
        private System.Windows.Forms.Button btnDuplicateNode;
        private System.Windows.Forms.Button btnSelectInInventor;
        private System.Windows.Forms.Label lblNodeName;
        private System.Windows.Forms.TextBox txtNodeName;
        private System.Windows.Forms.Label lblNodeType;
        private System.Windows.Forms.Label lblOrigin;
        private System.Windows.Forms.TextBox txtOriginX;
        private System.Windows.Forms.TextBox txtOriginY;
        private System.Windows.Forms.TextBox txtOriginZ;
        private System.Windows.Forms.DataGridView dgvNodeParams;
        private System.Windows.Forms.DataGridViewTextBoxColumn colNodeParam;
        private System.Windows.Forms.DataGridViewTextBoxColumn colNodeValue;
        private System.Windows.Forms.DataGridViewTextBoxColumn colNodeExpr;
        private System.Windows.Forms.Button btnResolveExpr;
        private System.Windows.Forms.Button btnApplyNode;
        private System.Windows.Forms.Label lblSceneStatus;
        private System.Windows.Forms.GroupBox grpPosition;
        private System.Windows.Forms.Label lblPosStep;
        private System.Windows.Forms.NumericUpDown numPosStep;
        private System.Windows.Forms.Button btnPosYPlus;
        private System.Windows.Forms.Button btnPosXMinus;
        private System.Windows.Forms.Button btnPosXPlus;
        private System.Windows.Forms.Button btnPosYMinus;
        private System.Windows.Forms.Button btnPosZPlus;
        private System.Windows.Forms.Button btnPosZMinus;
        private System.Windows.Forms.GroupBox grpRotation;
        private System.Windows.Forms.Label lblAngleStep;
        private System.Windows.Forms.NumericUpDown numAngleStep;
        private System.Windows.Forms.Button btnRotX;
        private System.Windows.Forms.Button btnRotY;
        private System.Windows.Forms.Button btnRotZ;
        private System.Windows.Forms.ListView lvBoolOps;
        private System.Windows.Forms.Label lblBoolType;
        private System.Windows.Forms.ComboBox cmbBoolType;
        private System.Windows.Forms.Label lblBoolTarget;
        private System.Windows.Forms.Label lblBoolTargetValue;
        private System.Windows.Forms.CheckedListBox clbBoolTools;
        private System.Windows.Forms.Button btnAddBoolOp;
        private System.Windows.Forms.Button btnRemoveBoolOp;
        private System.Windows.Forms.Button btnApplyBoolVisuals;
        private System.Windows.Forms.Label lblBoolStatus;
    }
}
