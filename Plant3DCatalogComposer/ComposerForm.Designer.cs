namespace Plant3DCatalogComposer
{
    partial class ComposerForm
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
            this.tabCode = new System.Windows.Forms.TabPage();
            this.txtGeneratedCode = new System.Windows.Forms.TextBox();
            this.lblCodeHint = new System.Windows.Forms.Label();
            this.tabBooleans = new System.Windows.Forms.TabPage();
            this.tabPortManager = new System.Windows.Forms.TabPage();
            this.lvPorts = new System.Windows.Forms.ListView();
            this.btnAddPort = new System.Windows.Forms.Button();
            this.btnPickPoint = new System.Windows.Forms.Button();
            this.btnDeletePort = new System.Windows.Forms.Button();
            this.btnCopyPort = new System.Windows.Forms.Button();
            this.chkShowPortMarkers = new System.Windows.Forms.CheckBox();
            this.lblPortNumber = new System.Windows.Forms.Label();
            this.txtPortNumber = new System.Windows.Forms.TextBox();
            this.lblPortType = new System.Windows.Forms.Label();
            this.cmbPortType = new System.Windows.Forms.ComboBox();
            this.lblPortParent = new System.Windows.Forms.Label();
            this.cmbPortParent = new System.Windows.Forms.ComboBox();
            this.lblPortPos = new System.Windows.Forms.Label();
            this.txtPortX = new System.Windows.Forms.TextBox();
            this.txtPortY = new System.Windows.Forms.TextBox();
            this.txtPortZ = new System.Windows.Forms.TextBox();
            this.lblPortDir = new System.Windows.Forms.Label();
            this.txtPortDx = new System.Windows.Forms.TextBox();
            this.txtPortDy = new System.Windows.Forms.TextBox();
            this.txtPortDz = new System.Windows.Forms.TextBox();
            this.btnApplyPort = new System.Windows.Forms.Button();
            this.grpPortMove = new System.Windows.Forms.GroupBox();
            this.lblPortMoveStep = new System.Windows.Forms.Label();
            this.numPortMoveStep = new System.Windows.Forms.NumericUpDown();
            this.btnPortMoveXPlus = new System.Windows.Forms.Button();
            this.btnPortMoveXMinus = new System.Windows.Forms.Button();
            this.btnPortMoveYPlus = new System.Windows.Forms.Button();
            this.btnPortMoveYMinus = new System.Windows.Forms.Button();
            this.btnPortMoveZPlus = new System.Windows.Forms.Button();
            this.btnPortMoveZMinus = new System.Windows.Forms.Button();
            this.grpPortRotDir = new System.Windows.Forms.GroupBox();
            this.rdoPortRotWorld = new System.Windows.Forms.RadioButton();
            this.rdoPortRotLocal = new System.Windows.Forms.RadioButton();
            this.lblPortRotStep = new System.Windows.Forms.Label();
            this.numPortRotStep = new System.Windows.Forms.NumericUpDown();
            this.btnPortRotX = new System.Windows.Forms.Button();
            this.btnPortRotY = new System.Windows.Forms.Button();
            this.btnPortRotZ = new System.Windows.Forms.Button();
            this.lblPortStatus = new System.Windows.Forms.Label();

            this.lblValveType = new System.Windows.Forms.Label();
            this.lblDN = new System.Windows.Forms.Label();
            this.lblPressureClass = new System.Windows.Forms.Label();
            this.cmbValveType = new System.Windows.Forms.ComboBox();
            this.cmbDn = new System.Windows.Forms.ComboBox();
            this.cmbPressureClass = new System.Windows.Forms.ComboBox();
            this.grpSkeleton = new System.Windows.Forms.GroupBox();
            this.btnCreateSkeleton = new System.Windows.Forms.Button();
            this.grpCatalogParts = new System.Windows.Forms.GroupBox();
            this.lblCatalogCategory = new System.Windows.Forms.Label();
            this.cmbCatalogCategory = new System.Windows.Forms.ComboBox();
            this.lblCatalogPart = new System.Windows.Forms.Label();
            this.cmbCatalogPart = new System.Windows.Forms.ComboBox();
            this.lblCatalogDN = new System.Windows.Forms.Label();
            this.cmbCatalogDN = new System.Windows.Forms.ComboBox();
            this.lblCatalogPressureClass = new System.Windows.Forms.Label();
            this.cmbCatalogPressureClass = new System.Windows.Forms.ComboBox();
            this.btnInsertCatalogPart = new System.Windows.Forms.Button();
            this.lblPrimitiveBuild = new System.Windows.Forms.Label();
            this.lblPrimitive = new System.Windows.Forms.Label();
            this.cmbPrimitive = new System.Windows.Forms.ComboBox();
            this.btnInsertPrimitive = new System.Windows.Forms.Button();
            this.btnDeployCatalog = new System.Windows.Forms.Button();
            this.btnPublishCatalog = new System.Windows.Forms.Button();
            this.btnTestCatalog = new System.Windows.Forms.Button();
            this.grpSceneTools = new System.Windows.Forms.GroupBox();
            this.btnGenerateCode = new System.Windows.Forms.Button();
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
            this.lblStepX = new System.Windows.Forms.Label();
            this.lblStepY = new System.Windows.Forms.Label();
            this.lblStepZ = new System.Windows.Forms.Label();
            this.numPosStepX = new System.Windows.Forms.NumericUpDown();
            this.numPosStepY = new System.Windows.Forms.NumericUpDown();
            this.numPosStepZ = new System.Windows.Forms.NumericUpDown();
            this.btnPickPosStep = new System.Windows.Forms.Button();
            this.btnAlignPos = new System.Windows.Forms.Button();
            this.btnPosYPlus = new System.Windows.Forms.Button();
            this.btnPosXMinus = new System.Windows.Forms.Button();
            this.btnPosXPlus = new System.Windows.Forms.Button();
            this.btnPosYMinus = new System.Windows.Forms.Button();
            this.btnPosZPlus = new System.Windows.Forms.Button();
            this.btnPosZMinus = new System.Windows.Forms.Button();
            this.grpRotation = new System.Windows.Forms.GroupBox();
            this.rdoRotLocal = new System.Windows.Forms.RadioButton();
            this.rdoRotWorld = new System.Windows.Forms.RadioButton();
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
            this.grpBoolCutters = new System.Windows.Forms.GroupBox();
            this.cmbBoolCutter = new System.Windows.Forms.ComboBox();
            this.btnInsertBoolCutter = new System.Windows.Forms.Button();
            this.btnBoolCutterSubtract = new System.Windows.Forms.Button();
            this.tabMain.SuspendLayout();
            this.grpSceneTools.SuspendLayout();
            this.tabSetup.SuspendLayout();
            this.grpSkeleton.SuspendLayout();
            this.grpCatalogParts.SuspendLayout();
            this.tabScene.SuspendLayout();
            this.tabCode.SuspendLayout();
            this.tabBooleans.SuspendLayout();
            this.tabPortManager.SuspendLayout();
            this.grpPortMove.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numPortMoveStep)).BeginInit();
            this.grpPortRotDir.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numPortRotStep)).BeginInit();
            this.grpBoolCutters.SuspendLayout();
            this.grpPosition.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numPosStepX)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPosStepY)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPosStepZ)).BeginInit();
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
            this.tabMain.Controls.Add(this.tabPortManager);
            this.tabMain.Controls.Add(this.tabCode);
            this.tabMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabMain.Location = new System.Drawing.Point(0, 0);
            this.tabMain.Name = "tabMain";
            this.tabMain.SelectedIndex = 0;
            this.tabMain.Size = new System.Drawing.Size(320, 720);
            this.tabMain.TabIndex = 0;
            //
            // tabSetup
            //
            this.tabSetup.Controls.Add(this.grpSceneTools);
            this.tabSetup.Controls.Add(this.grpSkeleton);
            this.tabSetup.Controls.Add(this.grpCatalogParts);
            this.tabSetup.Controls.Add(this.cmbPressureClass);
            this.tabSetup.Controls.Add(this.lblPressureClass);
            this.tabSetup.Controls.Add(this.cmbDn);
            this.tabSetup.Controls.Add(this.lblDN);
            this.tabSetup.Controls.Add(this.cmbValveType);
            this.tabSetup.Controls.Add(this.lblValveType);
            this.tabSetup.Controls.Add(this.lblStatus);
            this.tabSetup.Location = new System.Drawing.Point(4, 24);
            this.tabSetup.Name = "tabSetup";
            this.tabSetup.Padding = new System.Windows.Forms.Padding(8);
            this.tabSetup.Size = new System.Drawing.Size(312, 592);
            this.tabSetup.TabIndex = 0;
            this.tabSetup.Text = "Setup";
            this.tabSetup.UseVisualStyleBackColor = true;
            this.tabSetup.AutoScroll = true;
            //
            // tabScene
            //
            this.tabScene.Controls.Add(this.lblSceneStatus);
            this.tabScene.Controls.Add(this.btnInsertPrimitive);
            this.tabScene.Controls.Add(this.cmbPrimitive);
            this.tabScene.Controls.Add(this.lblPrimitive);
            this.tabScene.Controls.Add(this.lblPrimitiveBuild);
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
            this.tabScene.AutoScroll = true;
            //
            // tabPortManager
            //
            this.tabPortManager.Controls.Add(this.lvPorts);
            this.tabPortManager.Controls.Add(this.btnAddPort);
            this.tabPortManager.Controls.Add(this.btnPickPoint);
            this.tabPortManager.Controls.Add(this.btnDeletePort);
            this.tabPortManager.Controls.Add(this.btnCopyPort);
            this.tabPortManager.Controls.Add(this.chkShowPortMarkers);
            this.tabPortManager.Controls.Add(this.lblPortType);
            this.tabPortManager.Controls.Add(this.cmbPortType);
            this.tabPortManager.Controls.Add(this.lblPortParent);
            this.tabPortManager.Controls.Add(this.cmbPortParent);
            this.tabPortManager.Controls.Add(this.lblPortPos);
            this.tabPortManager.Controls.Add(this.txtPortX);
            this.tabPortManager.Controls.Add(this.txtPortY);
            this.tabPortManager.Controls.Add(this.txtPortZ);
            this.tabPortManager.Controls.Add(this.lblPortDir);
            this.tabPortManager.Controls.Add(this.txtPortDx);
            this.tabPortManager.Controls.Add(this.txtPortDy);
            this.tabPortManager.Controls.Add(this.txtPortDz);
            this.tabPortManager.Controls.Add(this.btnApplyPort);
            this.tabPortManager.Controls.Add(this.grpPortMove);
            this.tabPortManager.Controls.Add(this.grpPortRotDir);
            this.tabPortManager.Controls.Add(this.lblPortStatus);
            this.tabPortManager.Controls.Add(this.lblPortNumber);
            this.tabPortManager.Controls.Add(this.txtPortNumber);
            this.tabPortManager.Location = new System.Drawing.Point(4, 24);
            this.tabPortManager.Name = "tabPortManager";
            this.tabPortManager.Padding = new System.Windows.Forms.Padding(8);
            this.tabPortManager.Size = new System.Drawing.Size(312, 692);
            this.tabPortManager.TabIndex = 4;
            this.tabPortManager.Text = "Port Manager";
            this.tabPortManager.UseVisualStyleBackColor = true;
            this.tabPortManager.AutoScroll = true;
            this.tabPortManager.Enter += new System.EventHandler(this.tabPortManager_Enter);
            //
            // lvPorts
            //
            this.lvPorts.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.lvPorts.FullRowSelect = true;
            this.lvPorts.GridLines = true;
            this.lvPorts.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.lvPorts.HideSelection = false;
            this.lvPorts.Location = new System.Drawing.Point(8, 8);
            this.lvPorts.Name = "lvPorts";
            this.lvPorts.Size = new System.Drawing.Size(296, 82);
            this.lvPorts.TabIndex = 0;
            this.lvPorts.UseCompatibleStateImageBehavior = false;
            this.lvPorts.View = System.Windows.Forms.View.Details;
            //
            // btnAddPort
            //
            this.btnAddPort.Location = new System.Drawing.Point(8, 96);
            this.btnAddPort.Name = "btnAddPort";
            this.btnAddPort.Size = new System.Drawing.Size(58, 22);
            this.btnAddPort.TabIndex = 1;
            this.btnAddPort.Text = "Add";
            this.btnAddPort.UseVisualStyleBackColor = true;
            this.btnAddPort.Click += new System.EventHandler(this.btnAddPort_Click);
            //
            // btnPickPoint
            //
            this.btnPickPoint.Location = new System.Drawing.Point(70, 96);
            this.btnPickPoint.Name = "btnPickPoint";
            this.btnPickPoint.Size = new System.Drawing.Size(96, 22);
            this.btnPickPoint.TabIndex = 2;
            this.btnPickPoint.Text = "Pick Point";
            this.btnPickPoint.UseVisualStyleBackColor = true;
            this.btnPickPoint.Click += new System.EventHandler(this.btnPickPoint_Click);
            //
            // btnDeletePort
            //
            this.btnDeletePort.Location = new System.Drawing.Point(170, 96);
            this.btnDeletePort.Name = "btnDeletePort";
            this.btnDeletePort.Size = new System.Drawing.Size(58, 22);
            this.btnDeletePort.TabIndex = 3;
            this.btnDeletePort.Text = "Delete";
            this.btnDeletePort.UseVisualStyleBackColor = true;
            this.btnDeletePort.Click += new System.EventHandler(this.btnDeletePort_Click);
            //
            // btnCopyPort
            //
            this.btnCopyPort.Location = new System.Drawing.Point(232, 96);
            this.btnCopyPort.Name = "btnCopyPort";
            this.btnCopyPort.Size = new System.Drawing.Size(64, 22);
            this.btnCopyPort.TabIndex = 4;
            this.btnCopyPort.Text = "Copy";
            this.btnCopyPort.UseVisualStyleBackColor = true;
            this.btnCopyPort.Click += new System.EventHandler(this.btnCopyPort_Click);
            //
            // chkShowPortMarkers
            //
            this.chkShowPortMarkers.AutoSize = true;
            this.chkShowPortMarkers.Checked = true;
            this.chkShowPortMarkers.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkShowPortMarkers.Location = new System.Drawing.Point(8, 122);
            this.chkShowPortMarkers.Name = "chkShowPortMarkers";
            this.chkShowPortMarkers.Size = new System.Drawing.Size(280, 19);
            this.chkShowPortMarkers.TabIndex = 5;
            this.chkShowPortMarkers.Text = "Show port arrows in model";
            this.chkShowPortMarkers.UseVisualStyleBackColor = true;
            this.chkShowPortMarkers.CheckedChanged += new System.EventHandler(this.chkShowPortMarkers_CheckedChanged);
            //
            // lblPortNumber
            //
            this.lblPortNumber.AutoSize = false;
            this.lblPortNumber.Location = new System.Drawing.Point(8, 152);
            this.lblPortNumber.Name = "lblPortNumber";
            this.lblPortNumber.Size = new System.Drawing.Size(68, 23);
            this.lblPortNumber.TabIndex = 6;
            this.lblPortNumber.Text = "Port #";
            this.lblPortNumber.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // txtPortNumber
            //
            this.txtPortNumber.Location = new System.Drawing.Point(76, 152);
            this.txtPortNumber.Name = "txtPortNumber";
            this.txtPortNumber.Size = new System.Drawing.Size(160, 23);
            this.txtPortNumber.TabIndex = 7;
            this.txtPortNumber.Text = "1";
            this.txtPortNumber.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            //
            // lblPortType
            //
            this.lblPortType.AutoSize = false;
            this.lblPortType.Location = new System.Drawing.Point(8, 182);
            this.lblPortType.Name = "lblPortType";
            this.lblPortType.Size = new System.Drawing.Size(68, 23);
            this.lblPortType.TabIndex = 8;
            this.lblPortType.Text = "End type";
            this.lblPortType.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // cmbPortType
            //
            this.cmbPortType.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left;
            this.cmbPortType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPortType.Location = new System.Drawing.Point(76, 182);
            this.cmbPortType.Name = "cmbPortType";
            this.cmbPortType.Size = new System.Drawing.Size(160, 23);
            this.cmbPortType.TabIndex = 9;
            //
            // lblPortParent
            //
            this.lblPortParent.AutoSize = false;
            this.lblPortParent.Location = new System.Drawing.Point(8, 210);
            this.lblPortParent.Name = "lblPortParent";
            this.lblPortParent.Size = new System.Drawing.Size(68, 23);
            this.lblPortParent.TabIndex = 10;
            this.lblPortParent.Text = "Parent";
            this.lblPortParent.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // cmbPortParent
            //
            this.cmbPortParent.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left;
            this.cmbPortParent.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPortParent.Location = new System.Drawing.Point(76, 210);
            this.cmbPortParent.Name = "cmbPortParent";
            this.cmbPortParent.Size = new System.Drawing.Size(160, 23);
            this.cmbPortParent.TabIndex = 11;
            //
            // lblPortPos
            //
            this.lblPortPos.AutoSize = false;
            this.lblPortPos.Location = new System.Drawing.Point(8, 238);
            this.lblPortPos.Name = "lblPortPos";
            this.lblPortPos.Size = new System.Drawing.Size(68, 23);
            this.lblPortPos.TabIndex = 12;
            this.lblPortPos.Text = "Position";
            this.lblPortPos.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // txtPortX
            //
            this.txtPortX.Location = new System.Drawing.Point(76, 238);
            this.txtPortX.Name = "txtPortX";
            this.txtPortX.Size = new System.Drawing.Size(50, 23);
            this.txtPortX.TabIndex = 13;
            //
            // txtPortY
            //
            this.txtPortY.Location = new System.Drawing.Point(130, 238);
            this.txtPortY.Name = "txtPortY";
            this.txtPortY.Size = new System.Drawing.Size(50, 23);
            this.txtPortY.TabIndex = 14;
            //
            // txtPortZ
            //
            this.txtPortZ.Location = new System.Drawing.Point(184, 238);
            this.txtPortZ.Name = "txtPortZ";
            this.txtPortZ.Size = new System.Drawing.Size(50, 23);
            this.txtPortZ.TabIndex = 15;
            //
            // lblPortDir
            //
            this.lblPortDir.AutoSize = false;
            this.lblPortDir.Location = new System.Drawing.Point(8, 266);
            this.lblPortDir.Name = "lblPortDir";
            this.lblPortDir.Size = new System.Drawing.Size(68, 23);
            this.lblPortDir.TabIndex = 16;
            this.lblPortDir.Text = "Direction";
            this.lblPortDir.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // txtPortDx
            //
            this.txtPortDx.Location = new System.Drawing.Point(76, 266);
            this.txtPortDx.Name = "txtPortDx";
            this.txtPortDx.Size = new System.Drawing.Size(50, 23);
            this.txtPortDx.TabIndex = 17;
            //
            // txtPortDy
            //
            this.txtPortDy.Location = new System.Drawing.Point(130, 266);
            this.txtPortDy.Name = "txtPortDy";
            this.txtPortDy.Size = new System.Drawing.Size(50, 23);
            this.txtPortDy.TabIndex = 18;
            //
            // txtPortDz
            //
            this.txtPortDz.Location = new System.Drawing.Point(184, 266);
            this.txtPortDz.Name = "txtPortDz";
            this.txtPortDz.Size = new System.Drawing.Size(50, 23);
            this.txtPortDz.TabIndex = 19;
            //
            // btnApplyPort
            //
            this.btnApplyPort.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.btnApplyPort.Location = new System.Drawing.Point(8, 294);
            this.btnApplyPort.Name = "btnApplyPort";
            this.btnApplyPort.Size = new System.Drawing.Size(296, 24);
            this.btnApplyPort.TabIndex = 20;
            this.btnApplyPort.Text = "Apply Port";
            this.btnApplyPort.UseVisualStyleBackColor = true;
            this.btnApplyPort.Click += new System.EventHandler(this.btnApplyPort_Click);
            //
            // grpPortMove
            //
            this.grpPortMove.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.grpPortMove.Controls.Add(this.btnPortMoveZMinus);
            this.grpPortMove.Controls.Add(this.btnPortMoveZPlus);
            this.grpPortMove.Controls.Add(this.btnPortMoveYMinus);
            this.grpPortMove.Controls.Add(this.btnPortMoveYPlus);
            this.grpPortMove.Controls.Add(this.btnPortMoveXMinus);
            this.grpPortMove.Controls.Add(this.btnPortMoveXPlus);
            this.grpPortMove.Controls.Add(this.numPortMoveStep);
            this.grpPortMove.Controls.Add(this.lblPortMoveStep);
            this.grpPortMove.Location = new System.Drawing.Point(8, 324);
            this.grpPortMove.Name = "grpPortMove";
            this.grpPortMove.Size = new System.Drawing.Size(296, 118);
            this.grpPortMove.TabIndex = 23;
            this.grpPortMove.TabStop = false;
            this.grpPortMove.Text = "Move Port";
            //
            // lblPortMoveStep
            //
            this.lblPortMoveStep.AutoSize = true;
            this.lblPortMoveStep.Location = new System.Drawing.Point(8, 22);
            this.lblPortMoveStep.Name = "lblPortMoveStep";
            this.lblPortMoveStep.Size = new System.Drawing.Size(61, 15);
            this.lblPortMoveStep.TabIndex = 0;
            this.lblPortMoveStep.Text = "Step (mm):";
            //
            // numPortMoveStep
            //
            this.numPortMoveStep.DecimalPlaces = 0;
            this.numPortMoveStep.Location = new System.Drawing.Point(108, 20);
            this.numPortMoveStep.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            this.numPortMoveStep.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numPortMoveStep.Name = "numPortMoveStep";
            this.numPortMoveStep.Size = new System.Drawing.Size(60, 23);
            this.numPortMoveStep.TabIndex = 1;
            this.numPortMoveStep.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numPortMoveStep.Value = new decimal(new int[] { 10, 0, 0, 0 });
            //
            // btnPortMoveYPlus
            //
            this.btnPortMoveYPlus.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnPortMoveYPlus.Location = new System.Drawing.Point(48, 52);
            this.btnPortMoveYPlus.Name = "btnPortMoveYPlus";
            this.btnPortMoveYPlus.Size = new System.Drawing.Size(32, 28);
            this.btnPortMoveYPlus.TabIndex = 2;
            this.btnPortMoveYPlus.Text = "\u2191";
            this.btnPortMoveYPlus.UseVisualStyleBackColor = true;
            this.btnPortMoveYPlus.Click += new System.EventHandler(this.btnPortMoveYPlus_Click);
            //
            // btnPortMoveXMinus
            //
            this.btnPortMoveXMinus.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnPortMoveXMinus.Location = new System.Drawing.Point(12, 82);
            this.btnPortMoveXMinus.Name = "btnPortMoveXMinus";
            this.btnPortMoveXMinus.Size = new System.Drawing.Size(32, 28);
            this.btnPortMoveXMinus.TabIndex = 3;
            this.btnPortMoveXMinus.Text = "\u2190";
            this.btnPortMoveXMinus.UseVisualStyleBackColor = true;
            this.btnPortMoveXMinus.Click += new System.EventHandler(this.btnPortMoveXMinus_Click);
            //
            // btnPortMoveYMinus
            //
            this.btnPortMoveYMinus.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnPortMoveYMinus.Location = new System.Drawing.Point(48, 82);
            this.btnPortMoveYMinus.Name = "btnPortMoveYMinus";
            this.btnPortMoveYMinus.Size = new System.Drawing.Size(32, 28);
            this.btnPortMoveYMinus.TabIndex = 4;
            this.btnPortMoveYMinus.Text = "\u2193";
            this.btnPortMoveYMinus.UseVisualStyleBackColor = true;
            this.btnPortMoveYMinus.Click += new System.EventHandler(this.btnPortMoveYMinus_Click);
            //
            // btnPortMoveXPlus
            //
            this.btnPortMoveXPlus.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnPortMoveXPlus.Location = new System.Drawing.Point(84, 82);
            this.btnPortMoveXPlus.Name = "btnPortMoveXPlus";
            this.btnPortMoveXPlus.Size = new System.Drawing.Size(32, 28);
            this.btnPortMoveXPlus.TabIndex = 5;
            this.btnPortMoveXPlus.Text = "\u2192";
            this.btnPortMoveXPlus.UseVisualStyleBackColor = true;
            this.btnPortMoveXPlus.Click += new System.EventHandler(this.btnPortMoveXPlus_Click);
            //
            // btnPortMoveZPlus
            //
            this.btnPortMoveZPlus.Location = new System.Drawing.Point(130, 52);
            this.btnPortMoveZPlus.Name = "btnPortMoveZPlus";
            this.btnPortMoveZPlus.Size = new System.Drawing.Size(40, 28);
            this.btnPortMoveZPlus.TabIndex = 6;
            this.btnPortMoveZPlus.Text = "Z+";
            this.btnPortMoveZPlus.UseVisualStyleBackColor = true;
            this.btnPortMoveZPlus.Click += new System.EventHandler(this.btnPortMoveZPlus_Click);
            //
            // btnPortMoveZMinus
            //
            this.btnPortMoveZMinus.Location = new System.Drawing.Point(130, 82);
            this.btnPortMoveZMinus.Name = "btnPortMoveZMinus";
            this.btnPortMoveZMinus.Size = new System.Drawing.Size(40, 28);
            this.btnPortMoveZMinus.TabIndex = 7;
            this.btnPortMoveZMinus.Text = "Z-";
            this.btnPortMoveZMinus.UseVisualStyleBackColor = true;
            this.btnPortMoveZMinus.Click += new System.EventHandler(this.btnPortMoveZMinus_Click);
            //
            // grpPortRotDir
            //
            this.grpPortRotDir.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.grpPortRotDir.Controls.Add(this.btnPortRotZ);
            this.grpPortRotDir.Controls.Add(this.btnPortRotY);
            this.grpPortRotDir.Controls.Add(this.btnPortRotX);
            this.grpPortRotDir.Controls.Add(this.numPortRotStep);
            this.grpPortRotDir.Controls.Add(this.lblPortRotStep);
            this.grpPortRotDir.Controls.Add(this.rdoPortRotLocal);
            this.grpPortRotDir.Controls.Add(this.rdoPortRotWorld);
            this.grpPortRotDir.Location = new System.Drawing.Point(8, 448);
            this.grpPortRotDir.Name = "grpPortRotDir";
            this.grpPortRotDir.Size = new System.Drawing.Size(296, 82);
            this.grpPortRotDir.TabIndex = 24;
            this.grpPortRotDir.TabStop = false;
            this.grpPortRotDir.Text = "Rotate Direction";
            //
            // rdoPortRotWorld
            //
            this.rdoPortRotWorld.AutoSize = true;
            this.rdoPortRotWorld.Checked = true;
            this.rdoPortRotWorld.Location = new System.Drawing.Point(10, 20);
            this.rdoPortRotWorld.Name = "rdoPortRotWorld";
            this.rdoPortRotWorld.Size = new System.Drawing.Size(86, 19);
            this.rdoPortRotWorld.TabIndex = 0;
            this.rdoPortRotWorld.TabStop = true;
            this.rdoPortRotWorld.Text = "World axes";
            this.rdoPortRotWorld.UseVisualStyleBackColor = true;
            //
            // rdoPortRotLocal
            //
            this.rdoPortRotLocal.AutoSize = true;
            this.rdoPortRotLocal.Location = new System.Drawing.Point(110, 20);
            this.rdoPortRotLocal.Name = "rdoPortRotLocal";
            this.rdoPortRotLocal.Size = new System.Drawing.Size(88, 19);
            this.rdoPortRotLocal.TabIndex = 1;
            this.rdoPortRotLocal.Text = "Object axes";
            this.rdoPortRotLocal.UseVisualStyleBackColor = true;
            //
            // lblPortRotStep
            //
            this.lblPortRotStep.AutoSize = true;
            this.lblPortRotStep.Location = new System.Drawing.Point(10, 48);
            this.lblPortRotStep.Name = "lblPortRotStep";
            this.lblPortRotStep.Size = new System.Drawing.Size(58, 15);
            this.lblPortRotStep.TabIndex = 2;
            this.lblPortRotStep.Text = "Step (deg):";
            //
            // numPortRotStep
            //
            this.numPortRotStep.Location = new System.Drawing.Point(108, 46);
            this.numPortRotStep.Maximum = new decimal(new int[] { 360, 0, 0, 0 });
            this.numPortRotStep.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numPortRotStep.Name = "numPortRotStep";
            this.numPortRotStep.Size = new System.Drawing.Size(60, 23);
            this.numPortRotStep.TabIndex = 3;
            this.numPortRotStep.Value = new decimal(new int[] { 15, 0, 0, 0 });
            //
            // btnPortRotX
            //
            this.btnPortRotX.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnPortRotX.Location = new System.Drawing.Point(180, 44);
            this.btnPortRotX.Name = "btnPortRotX";
            this.btnPortRotX.Size = new System.Drawing.Size(32, 22);
            this.btnPortRotX.TabIndex = 4;
            this.btnPortRotX.Text = "X";
            this.btnPortRotX.UseVisualStyleBackColor = true;
            this.btnPortRotX.Click += new System.EventHandler(this.btnPortRotX_Click);
            //
            // btnPortRotY
            //
            this.btnPortRotY.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnPortRotY.Location = new System.Drawing.Point(218, 44);
            this.btnPortRotY.Name = "btnPortRotY";
            this.btnPortRotY.Size = new System.Drawing.Size(32, 22);
            this.btnPortRotY.TabIndex = 5;
            this.btnPortRotY.Text = "Y";
            this.btnPortRotY.UseVisualStyleBackColor = true;
            this.btnPortRotY.Click += new System.EventHandler(this.btnPortRotY_Click);
            //
            // btnPortRotZ
            //
            this.btnPortRotZ.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnPortRotZ.Location = new System.Drawing.Point(256, 44);
            this.btnPortRotZ.Name = "btnPortRotZ";
            this.btnPortRotZ.Size = new System.Drawing.Size(32, 22);
            this.btnPortRotZ.TabIndex = 6;
            this.btnPortRotZ.Text = "Z";
            this.btnPortRotZ.UseVisualStyleBackColor = true;
            this.btnPortRotZ.Click += new System.EventHandler(this.btnPortRotZ_Click);
            //
            // lblPortStatus
            //
            this.lblPortStatus.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.lblPortStatus.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lblPortStatus.Location = new System.Drawing.Point(8, 526);
            this.lblPortStatus.Name = "lblPortStatus";
            this.lblPortStatus.Size = new System.Drawing.Size(296, 32);
            this.lblPortStatus.TabIndex = 25;
            //
            //
            // tabCode
            //
            this.tabCode.Controls.Add(this.lblCodeHint);
            this.tabCode.Controls.Add(this.txtGeneratedCode);
            this.tabCode.Location = new System.Drawing.Point(4, 24);
            this.tabCode.Name = "tabCode";
            this.tabCode.Padding = new System.Windows.Forms.Padding(8);
            this.tabCode.Size = new System.Drawing.Size(312, 592);
            this.tabCode.TabIndex = 3;
            this.tabCode.Text = "Code";
            this.tabCode.UseVisualStyleBackColor = true;
            this.tabCode.Enter += new System.EventHandler(this.tabCode_Enter);
            //
            // lblCodeHint
            //
            this.lblCodeHint.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblCodeHint.Location = new System.Drawing.Point(8, 8);
            this.lblCodeHint.Name = "lblCodeHint";
            this.lblCodeHint.Size = new System.Drawing.Size(296, 48);
            this.lblCodeHint.TabIndex = 0;
            this.lblCodeHint.Text = "Custom parts export to catalog_generator/parts. Standard parts: preview/test only — Generate Code writes port reference to parts/_composer_exports (library unchanged).";
            //
            // txtGeneratedCode
            //
            this.txtGeneratedCode.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtGeneratedCode.Font = new System.Drawing.Font("Consolas", 8.25F);
            this.txtGeneratedCode.Location = new System.Drawing.Point(8, 56);
            this.txtGeneratedCode.Multiline = true;
            this.txtGeneratedCode.Name = "txtGeneratedCode";
            this.txtGeneratedCode.ReadOnly = true;
            this.txtGeneratedCode.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtGeneratedCode.TabIndex = 1;
            this.txtGeneratedCode.WordWrap = false;
            //
            // tabBooleans
            //
            this.tabBooleans.Controls.Add(this.lblBoolStatus);
            this.tabBooleans.Controls.Add(this.grpBoolCutters);
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
            this.tabBooleans.AutoScroll = true;
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
            this.clbBoolTools.Size = new System.Drawing.Size(285, 88);
            this.clbBoolTools.TabIndex = 5;
            //
            // btnAddBoolOp
            //
            this.btnAddBoolOp.Location = new System.Drawing.Point(11, 354);
            this.btnAddBoolOp.Name = "btnAddBoolOp";
            this.btnAddBoolOp.Size = new System.Drawing.Size(88, 28);
            this.btnAddBoolOp.TabIndex = 6;
            this.btnAddBoolOp.Text = "Add";
            this.btnAddBoolOp.UseVisualStyleBackColor = true;
            this.btnAddBoolOp.Click += new System.EventHandler(this.btnAddBoolOp_Click);
            //
            // btnRemoveBoolOp
            //
            this.btnRemoveBoolOp.Location = new System.Drawing.Point(105, 354);
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
            this.btnApplyBoolVisuals.Location = new System.Drawing.Point(199, 354);
            this.btnApplyBoolVisuals.Name = "btnApplyBoolVisuals";
            this.btnApplyBoolVisuals.Size = new System.Drawing.Size(97, 28);
            this.btnApplyBoolVisuals.TabIndex = 8;
            this.btnApplyBoolVisuals.Text = "Rebuild Plant 3D";
            this.btnApplyBoolVisuals.UseVisualStyleBackColor = true;
            this.btnApplyBoolVisuals.Click += new System.EventHandler(this.btnApplyBoolVisuals_Click);
            //
            // lblBoolStatus
            //
            this.lblBoolStatus.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;
            this.lblBoolStatus.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lblBoolStatus.Location = new System.Drawing.Point(11, 392);
            this.lblBoolStatus.Name = "lblBoolStatus";
            this.lblBoolStatus.Size = new System.Drawing.Size(285, 40);
            this.lblBoolStatus.TabIndex = 13;
            //
            // grpBoolCutters
            //
            this.grpBoolCutters.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;
            this.grpBoolCutters.Controls.Add(this.btnBoolCutterSubtract);
            this.grpBoolCutters.Controls.Add(this.btnInsertBoolCutter);
            this.grpBoolCutters.Controls.Add(this.cmbBoolCutter);
            this.grpBoolCutters.Location = new System.Drawing.Point(11, 268);
            this.grpBoolCutters.Name = "grpBoolCutters";
            this.grpBoolCutters.Size = new System.Drawing.Size(285, 78);
            this.grpBoolCutters.TabIndex = 10;
            this.grpBoolCutters.TabStop = false;
            this.grpBoolCutters.Text = "Fillet / chamfer cutters";
            //
            // cmbBoolCutter
            //
            this.cmbBoolCutter.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;
            this.cmbBoolCutter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbBoolCutter.Location = new System.Drawing.Point(10, 22);
            this.cmbBoolCutter.Name = "cmbBoolCutter";
            this.cmbBoolCutter.Size = new System.Drawing.Size(265, 23);
            this.cmbBoolCutter.TabIndex = 0;
            //
            // btnInsertBoolCutter
            //
            this.btnInsertBoolCutter.Location = new System.Drawing.Point(10, 50);
            this.btnInsertBoolCutter.Name = "btnInsertBoolCutter";
            this.btnInsertBoolCutter.Size = new System.Drawing.Size(128, 24);
            this.btnInsertBoolCutter.TabIndex = 1;
            this.btnInsertBoolCutter.Text = "Insert cutter";
            this.btnInsertBoolCutter.UseVisualStyleBackColor = true;
            this.btnInsertBoolCutter.Click += new System.EventHandler(this.btnInsertBoolCutter_Click);
            //
            // btnBoolCutterSubtract
            //
            this.btnBoolCutterSubtract.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Right;
            this.btnBoolCutterSubtract.Location = new System.Drawing.Point(144, 50);
            this.btnBoolCutterSubtract.Name = "btnBoolCutterSubtract";
            this.btnBoolCutterSubtract.Size = new System.Drawing.Size(131, 24);
            this.btnBoolCutterSubtract.TabIndex = 2;
            this.btnBoolCutterSubtract.Text = "Insert && subtract";
            this.btnBoolCutterSubtract.UseVisualStyleBackColor = true;
            this.btnBoolCutterSubtract.Click += new System.EventHandler(this.btnBoolCutterSubtract_Click);
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
            // cmbDn
            //
            this.cmbDn.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;
            this.cmbDn.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbDn.Location = new System.Drawing.Point(110, 47);
            this.cmbDn.Name = "cmbDn";
            this.cmbDn.Size = new System.Drawing.Size(186, 23);
            this.cmbDn.TabIndex = 3;
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
            // cmbPressureClass
            //
            this.cmbPressureClass.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;
            this.cmbPressureClass.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPressureClass.Location = new System.Drawing.Point(110, 82);
            this.cmbPressureClass.Name = "cmbPressureClass";
            this.cmbPressureClass.Size = new System.Drawing.Size(186, 23);
            this.cmbPressureClass.TabIndex = 5;
            //
            // dgvParams
            //
            this.dgvParams.AllowUserToAddRows = false;
            this.dgvParams.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;
            this.dgvParams.AllowUserToDeleteRows = false;
            this.dgvParams.AllowUserToResizeRows = false;
            this.dgvParams.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvParams.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvParams.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                this.colName,
                this.colValue});
            this.dgvParams.Location = new System.Drawing.Point(10, 22);
            this.dgvParams.MultiSelect = false;
            this.dgvParams.Name = "dgvParams";
            this.dgvParams.RowHeadersVisible = false;
            this.dgvParams.Size = new System.Drawing.Size(276, 96);
            this.dgvParams.TabIndex = 0;
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
            // grpSkeleton
            //
            this.grpSkeleton.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;
            this.grpSkeleton.Controls.Add(this.btnCreateSkeleton);
            this.grpSkeleton.Controls.Add(this.dgvParams);
            this.grpSkeleton.Location = new System.Drawing.Point(8, 108);
            this.grpSkeleton.Name = "grpSkeleton";
            this.grpSkeleton.Size = new System.Drawing.Size(296, 178);
            this.grpSkeleton.TabIndex = 6;
            this.grpSkeleton.TabStop = false;
            this.grpSkeleton.Text = "Valve Skeleton";
            //
            // btnCreateSkeleton
            //
            this.btnCreateSkeleton.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;
            this.btnCreateSkeleton.Location = new System.Drawing.Point(10, 132);
            this.btnCreateSkeleton.Name = "btnCreateSkeleton";
            this.btnCreateSkeleton.Size = new System.Drawing.Size(276, 32);
            this.btnCreateSkeleton.TabIndex = 1;
            this.btnCreateSkeleton.Text = "Create Skeleton";
            this.btnCreateSkeleton.UseVisualStyleBackColor = true;
            this.btnCreateSkeleton.Click += new System.EventHandler(this.btnCreateSkeleton_Click);
            //
            // grpCatalogParts
            //
            this.grpCatalogParts.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;
            this.grpCatalogParts.Controls.Add(this.btnInsertCatalogPart);
            this.grpCatalogParts.Controls.Add(this.cmbCatalogPressureClass);
            this.grpCatalogParts.Controls.Add(this.lblCatalogPressureClass);
            this.grpCatalogParts.Controls.Add(this.cmbCatalogDN);
            this.grpCatalogParts.Controls.Add(this.lblCatalogDN);
            this.grpCatalogParts.Controls.Add(this.cmbCatalogPart);
            this.grpCatalogParts.Controls.Add(this.lblCatalogPart);
            this.grpCatalogParts.Controls.Add(this.cmbCatalogCategory);
            this.grpCatalogParts.Controls.Add(this.lblCatalogCategory);
            this.grpCatalogParts.Location = new System.Drawing.Point(8, 292);
            this.grpCatalogParts.Name = "grpCatalogParts";
            this.grpCatalogParts.Size = new System.Drawing.Size(296, 196);
            this.grpCatalogParts.TabIndex = 8;
            this.grpCatalogParts.Padding = new System.Windows.Forms.Padding(8, 8, 8, 8);
            this.grpCatalogParts.TabStop = false;
            this.grpCatalogParts.Text = "";
            //
            // lblCatalogCategory
            //
            this.lblCatalogCategory.AutoSize = true;
            this.lblCatalogCategory.Location = new System.Drawing.Point(8, 32);
            this.lblCatalogCategory.Name = "lblCatalogCategory";
            this.lblCatalogCategory.Size = new System.Drawing.Size(58, 15);
            this.lblCatalogCategory.TabIndex = 7;
            this.lblCatalogCategory.Text = "Category:";
            //
            // cmbCatalogCategory
            //
            this.cmbCatalogCategory.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;
            this.cmbCatalogCategory.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbCatalogCategory.Location = new System.Drawing.Point(72, 29);
            this.cmbCatalogCategory.Name = "cmbCatalogCategory";
            this.cmbCatalogCategory.Size = new System.Drawing.Size(216, 23);
            this.cmbCatalogCategory.TabIndex = 0;
            //
            // lblCatalogPart
            //
            this.lblCatalogPart.AutoSize = true;
            this.lblCatalogPart.Location = new System.Drawing.Point(8, 64);
            this.lblCatalogPart.Name = "lblCatalogPart";
            this.lblCatalogPart.Size = new System.Drawing.Size(31, 15);
            this.lblCatalogPart.TabIndex = 0;
            this.lblCatalogPart.Text = "Part:";
            //
            // cmbCatalogPart
            //
            this.cmbCatalogPart.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;
            this.cmbCatalogPart.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbCatalogPart.Location = new System.Drawing.Point(48, 61);
            this.cmbCatalogPart.Name = "cmbCatalogPart";
            this.cmbCatalogPart.Size = new System.Drawing.Size(229, 23);
            this.cmbCatalogPart.TabIndex = 1;
            //
            // lblCatalogDN
            //
            this.lblCatalogDN.AutoSize = true;
            this.lblCatalogDN.Location = new System.Drawing.Point(8, 96);
            this.lblCatalogDN.Name = "lblCatalogDN";
            this.lblCatalogDN.Size = new System.Drawing.Size(27, 15);
            this.lblCatalogDN.TabIndex = 3;
            this.lblCatalogDN.Text = "DN:";
            //
            // cmbCatalogDN
            //
            this.cmbCatalogDN.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;
            this.cmbCatalogDN.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbCatalogDN.Location = new System.Drawing.Point(48, 93);
            this.cmbCatalogDN.Name = "cmbCatalogDN";
            this.cmbCatalogDN.Size = new System.Drawing.Size(229, 23);
            this.cmbCatalogDN.TabIndex = 4;
            //
            // lblCatalogPressureClass
            //
            this.lblCatalogPressureClass.AutoSize = true;
            this.lblCatalogPressureClass.Location = new System.Drawing.Point(8, 128);
            this.lblCatalogPressureClass.Name = "lblCatalogPressureClass";
            this.lblCatalogPressureClass.Size = new System.Drawing.Size(85, 15);
            this.lblCatalogPressureClass.TabIndex = 5;
            this.lblCatalogPressureClass.Text = "Pressure Class:";
            //
            // cmbCatalogPressureClass
            //
            this.cmbCatalogPressureClass.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;
            this.cmbCatalogPressureClass.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbCatalogPressureClass.Location = new System.Drawing.Point(99, 125);
            this.cmbCatalogPressureClass.Name = "cmbCatalogPressureClass";
            this.cmbCatalogPressureClass.Size = new System.Drawing.Size(178, 23);
            this.cmbCatalogPressureClass.TabIndex = 6;
            //
            // btnInsertCatalogPart
            //
            this.btnInsertCatalogPart.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;
            this.btnInsertCatalogPart.Location = new System.Drawing.Point(10, 158);
            this.btnInsertCatalogPart.Name = "btnInsertCatalogPart";
            this.btnInsertCatalogPart.Size = new System.Drawing.Size(276, 32);
            this.btnInsertCatalogPart.TabIndex = 2;
            this.btnInsertCatalogPart.Text = "Insert Catalog Part";
            this.btnInsertCatalogPart.UseVisualStyleBackColor = true;
            this.btnInsertCatalogPart.Click += new System.EventHandler(this.btnInsertCatalogPart_Click);
            //
            // lblPrimitiveBuild
            //
            this.lblPrimitiveBuild.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;
            this.lblPrimitiveBuild.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lblPrimitiveBuild.Location = new System.Drawing.Point(11, 8);
            this.lblPrimitiveBuild.Name = "lblPrimitiveBuild";
            this.lblPrimitiveBuild.Size = new System.Drawing.Size(285, 15);
            this.lblPrimitiveBuild.TabIndex = 22;
            this.lblPrimitiveBuild.Text = "Custom build (primitives + booleans for special valves):";
            //
            // lblPrimitive
            //
            this.lblPrimitive.AutoSize = true;
            this.lblPrimitive.Location = new System.Drawing.Point(11, 28);
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
            this.cmbPrimitive.Location = new System.Drawing.Point(110, 25);
            this.cmbPrimitive.Name = "cmbPrimitive";
            this.cmbPrimitive.Size = new System.Drawing.Size(186, 23);
            this.cmbPrimitive.TabIndex = 19;
            //
            // btnInsertPrimitive
            //
            this.btnInsertPrimitive.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;
            this.btnInsertPrimitive.Location = new System.Drawing.Point(11, 54);
            this.btnInsertPrimitive.Name = "btnInsertPrimitive";
            this.btnInsertPrimitive.Size = new System.Drawing.Size(285, 28);
            this.btnInsertPrimitive.TabIndex = 20;
            this.btnInsertPrimitive.Text = "Insert Primitive";
            this.btnInsertPrimitive.UseVisualStyleBackColor = true;
            this.btnInsertPrimitive.Click += new System.EventHandler(this.btnInsertPrimitive_Click);
            //
            // grpSceneTools
            //
            this.grpSceneTools.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;
            this.grpSceneTools.Controls.Add(this.btnPublishCatalog);
            this.grpSceneTools.Controls.Add(this.btnTestCatalog);
            this.grpSceneTools.Controls.Add(this.btnDeployCatalog);
            this.grpSceneTools.Controls.Add(this.btnRebuildScene);
            this.grpSceneTools.Controls.Add(this.btnImportJson);
            this.grpSceneTools.Controls.Add(this.btnExport);
            this.grpSceneTools.Controls.Add(this.btnGenerateCode);
            this.grpSceneTools.Location = new System.Drawing.Point(8, 496);
            this.grpSceneTools.Name = "grpSceneTools";
            this.grpSceneTools.Size = new System.Drawing.Size(296, 152);
            this.grpSceneTools.TabIndex = 9;
            this.grpSceneTools.TabStop = false;
            this.grpSceneTools.Text = "Scene Tools";
            //
            // btnGenerateCode
            //
            this.btnGenerateCode.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left;
            this.btnGenerateCode.Location = new System.Drawing.Point(10, 22);
            this.btnGenerateCode.Name = "btnGenerateCode";
            this.btnGenerateCode.Size = new System.Drawing.Size(134, 28);
            this.btnGenerateCode.TabIndex = 0;
            this.btnGenerateCode.Text = "Generate Code";
            this.btnGenerateCode.UseVisualStyleBackColor = true;
            this.btnGenerateCode.Click += new System.EventHandler(this.btnGenerateCode_Click);
            //
            // btnDeployCatalog
            //
            this.btnDeployCatalog.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left;
            this.btnDeployCatalog.Location = new System.Drawing.Point(152, 86);
            this.btnDeployCatalog.Name = "btnDeployCatalog";
            this.btnDeployCatalog.Size = new System.Drawing.Size(134, 28);
            this.btnDeployCatalog.TabIndex = 4;
            this.btnDeployCatalog.Text = "Deploy Catalog";
            this.btnDeployCatalog.UseVisualStyleBackColor = true;
            this.btnDeployCatalog.Click += new System.EventHandler(this.btnDeployCatalog_Click);
            //
            // btnTestCatalog
            //
            this.btnTestCatalog.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left;
            this.btnTestCatalog.Location = new System.Drawing.Point(10, 86);
            this.btnTestCatalog.Name = "btnTestCatalog";
            this.btnTestCatalog.Size = new System.Drawing.Size(134, 28);
            this.btnTestCatalog.TabIndex = 5;
            this.btnTestCatalog.Text = "Test Catalog";
            this.btnTestCatalog.UseVisualStyleBackColor = true;
            this.btnTestCatalog.Click += new System.EventHandler(this.btnTestCatalog_Click);
            //
            // btnPublishCatalog
            //
            this.btnPublishCatalog.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.btnPublishCatalog.Location = new System.Drawing.Point(10, 118);
            this.btnPublishCatalog.Name = "btnPublishCatalog";
            this.btnPublishCatalog.Size = new System.Drawing.Size(276, 28);
            this.btnPublishCatalog.TabIndex = 6;
            this.btnPublishCatalog.Text = "Publish Catalog";
            this.btnPublishCatalog.UseVisualStyleBackColor = true;
            this.btnPublishCatalog.Click += new System.EventHandler(this.btnPublishCatalog_Click);
            //
            // btnExport
            //
            this.btnExport.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.btnExport.Location = new System.Drawing.Point(152, 22);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(134, 28);
            this.btnExport.TabIndex = 1;
            this.btnExport.Text = "Export";
            this.btnExport.UseVisualStyleBackColor = true;
            this.btnExport.Click += new System.EventHandler(this.btnExport_Click);
            //
            // btnImportJson
            //
            this.btnImportJson.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left;
            this.btnImportJson.Location = new System.Drawing.Point(10, 54);
            this.btnImportJson.Name = "btnImportJson";
            this.btnImportJson.Size = new System.Drawing.Size(134, 28);
            this.btnImportJson.TabIndex = 2;
            this.btnImportJson.Text = "Import";
            this.btnImportJson.UseVisualStyleBackColor = true;
            this.btnImportJson.Click += new System.EventHandler(this.btnImportJson_Click);
            //
            // btnRebuildScene
            //
            this.btnRebuildScene.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.btnRebuildScene.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnRebuildScene.Location = new System.Drawing.Point(152, 54);
            this.btnRebuildScene.Name = "btnRebuildScene";
            this.btnRebuildScene.Size = new System.Drawing.Size(134, 28);
            this.btnRebuildScene.TabIndex = 3;
            this.btnRebuildScene.Text = "Rebuild";
            this.btnRebuildScene.UseVisualStyleBackColor = true;
            this.btnRebuildScene.Click += new System.EventHandler(this.btnRebuildScene_Click);
            //
            // lblStatus
            //
            this.lblStatus.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblStatus.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lblStatus.Location = new System.Drawing.Point(8, 562);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Padding = new System.Windows.Forms.Padding(3, 4, 3, 2);
            this.lblStatus.Size = new System.Drawing.Size(296, 22);
            this.lblStatus.TabIndex = 13;
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // treeScene
            //
            this.treeScene.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;
            this.treeScene.HideSelection = false;
            this.treeScene.Location = new System.Drawing.Point(11, 90);
            this.treeScene.Name = "treeScene";
            this.treeScene.Size = new System.Drawing.Size(285, 72);
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
            this.grpPosition.Controls.Add(this.btnAlignPos);
            this.grpPosition.Controls.Add(this.btnPickPosStep);
            this.grpPosition.Controls.Add(this.numPosStepZ);
            this.grpPosition.Controls.Add(this.lblStepZ);
            this.grpPosition.Controls.Add(this.numPosStepY);
            this.grpPosition.Controls.Add(this.lblStepY);
            this.grpPosition.Controls.Add(this.numPosStepX);
            this.grpPosition.Controls.Add(this.lblStepX);
            this.grpPosition.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;
            this.grpPosition.Location = new System.Drawing.Point(8, 280);
            this.grpPosition.Name = "grpPosition";
            this.grpPosition.Size = new System.Drawing.Size(296, 118);
            this.grpPosition.TabIndex = 16;
            this.grpPosition.TabStop = false;
            this.grpPosition.Text = "Position";
            //
            // lblStepX
            //
            this.lblStepX.AutoSize = true;
            this.lblStepX.Location = new System.Drawing.Point(10, 22);
            this.lblStepX.Name = "lblStepX";
            this.lblStepX.Size = new System.Drawing.Size(14, 15);
            this.lblStepX.TabIndex = 0;
            this.lblStepX.Text = "X";
            //
            // numPosStepX
            //
            this.numPosStepX.DecimalPlaces = 0;
            this.numPosStepX.Increment = new decimal(new int[] { 1, 0, 0, 0 });
            this.numPosStepX.Location = new System.Drawing.Point(24, 20);
            this.numPosStepX.Maximum = new decimal(new int[] { 10000, 0, 0, 0 });
            this.numPosStepX.Name = "numPosStepX";
            this.numPosStepX.Size = new System.Drawing.Size(54, 23);
            this.numPosStepX.TabIndex = 1;
            this.numPosStepX.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numPosStepX.Value = new decimal(new int[] { 10, 0, 0, 0 });
            //
            // lblStepY
            //
            this.lblStepY.AutoSize = true;
            this.lblStepY.Location = new System.Drawing.Point(86, 22);
            this.lblStepY.Name = "lblStepY";
            this.lblStepY.Size = new System.Drawing.Size(14, 15);
            this.lblStepY.TabIndex = 2;
            this.lblStepY.Text = "Y";
            //
            // numPosStepY
            //
            this.numPosStepY.DecimalPlaces = 0;
            this.numPosStepY.Increment = new decimal(new int[] { 1, 0, 0, 0 });
            this.numPosStepY.Location = new System.Drawing.Point(100, 20);
            this.numPosStepY.Maximum = new decimal(new int[] { 10000, 0, 0, 0 });
            this.numPosStepY.Name = "numPosStepY";
            this.numPosStepY.Size = new System.Drawing.Size(54, 23);
            this.numPosStepY.TabIndex = 3;
            this.numPosStepY.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numPosStepY.Value = new decimal(new int[] { 10, 0, 0, 0 });
            //
            // lblStepZ
            //
            this.lblStepZ.AutoSize = true;
            this.lblStepZ.Location = new System.Drawing.Point(162, 22);
            this.lblStepZ.Name = "lblStepZ";
            this.lblStepZ.Size = new System.Drawing.Size(14, 15);
            this.lblStepZ.TabIndex = 4;
            this.lblStepZ.Text = "Z";
            //
            // numPosStepZ
            //
            this.numPosStepZ.DecimalPlaces = 0;
            this.numPosStepZ.Increment = new decimal(new int[] { 1, 0, 0, 0 });
            this.numPosStepZ.Location = new System.Drawing.Point(176, 20);
            this.numPosStepZ.Maximum = new decimal(new int[] { 10000, 0, 0, 0 });
            this.numPosStepZ.Name = "numPosStepZ";
            this.numPosStepZ.Size = new System.Drawing.Size(54, 23);
            this.numPosStepZ.TabIndex = 5;
            this.numPosStepZ.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numPosStepZ.Value = new decimal(new int[] { 10, 0, 0, 0 });
            //
            // btnPickPosStep
            //
            this.btnPickPosStep.Location = new System.Drawing.Point(200, 52);
            this.btnPickPosStep.Name = "btnPickPosStep";
            this.btnPickPosStep.Size = new System.Drawing.Size(88, 28);
            this.btnPickPosStep.TabIndex = 10;
            this.btnPickPosStep.Text = "Pick";
            this.btnPickPosStep.UseVisualStyleBackColor = true;
            this.btnPickPosStep.Click += new System.EventHandler(this.btnPickPosStep_Click);
            //
            // btnAlignPos
            //
            this.btnAlignPos.Location = new System.Drawing.Point(200, 82);
            this.btnAlignPos.Name = "btnAlignPos";
            this.btnAlignPos.Size = new System.Drawing.Size(88, 28);
            this.btnAlignPos.TabIndex = 11;
            this.btnAlignPos.Text = "Align";
            this.btnAlignPos.UseVisualStyleBackColor = true;
            this.btnAlignPos.Click += new System.EventHandler(this.btnAlignPos_Click);
            //
            // btnPosYPlus
            //
            this.btnPosYPlus.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnPosYPlus.Location = new System.Drawing.Point(48, 52);
            this.btnPosYPlus.Name = "btnPosYPlus";
            this.btnPosYPlus.Size = new System.Drawing.Size(32, 28);
            this.btnPosYPlus.TabIndex = 6;
            this.btnPosYPlus.Text = "\u2191";
            this.btnPosYPlus.UseVisualStyleBackColor = true;
            this.btnPosYPlus.Click += new System.EventHandler(this.btnPosYPlus_Click);
            //
            // btnPosXMinus
            //
            this.btnPosXMinus.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnPosXMinus.Location = new System.Drawing.Point(12, 82);
            this.btnPosXMinus.Name = "btnPosXMinus";
            this.btnPosXMinus.Size = new System.Drawing.Size(32, 28);
            this.btnPosXMinus.TabIndex = 7;
            this.btnPosXMinus.Text = "\u2190";
            this.btnPosXMinus.UseVisualStyleBackColor = true;
            this.btnPosXMinus.Click += new System.EventHandler(this.btnPosXMinus_Click);
            //
            // btnPosXPlus
            //
            this.btnPosXPlus.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnPosXPlus.Location = new System.Drawing.Point(84, 82);
            this.btnPosXPlus.Name = "btnPosXPlus";
            this.btnPosXPlus.Size = new System.Drawing.Size(32, 28);
            this.btnPosXPlus.TabIndex = 8;
            this.btnPosXPlus.Text = "\u2192";
            this.btnPosXPlus.UseVisualStyleBackColor = true;
            this.btnPosXPlus.Click += new System.EventHandler(this.btnPosXPlus_Click);
            //
            // btnPosYMinus
            //
            this.btnPosYMinus.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnPosYMinus.Location = new System.Drawing.Point(48, 82);
            this.btnPosYMinus.Name = "btnPosYMinus";
            this.btnPosYMinus.Size = new System.Drawing.Size(32, 28);
            this.btnPosYMinus.TabIndex = 9;
            this.btnPosYMinus.Text = "\u2193";
            this.btnPosYMinus.UseVisualStyleBackColor = true;
            this.btnPosYMinus.Click += new System.EventHandler(this.btnPosYMinus_Click);
            //
            // btnPosZPlus
            //
            this.btnPosZPlus.Location = new System.Drawing.Point(130, 52);
            this.btnPosZPlus.Name = "btnPosZPlus";
            this.btnPosZPlus.Size = new System.Drawing.Size(40, 28);
            this.btnPosZPlus.TabIndex = 12;
            this.btnPosZPlus.Text = "Z+";
            this.btnPosZPlus.UseVisualStyleBackColor = true;
            this.btnPosZPlus.Click += new System.EventHandler(this.btnPosZPlus_Click);
            //
            // btnPosZMinus
            //
            this.btnPosZMinus.Location = new System.Drawing.Point(130, 82);
            this.btnPosZMinus.Name = "btnPosZMinus";
            this.btnPosZMinus.Size = new System.Drawing.Size(40, 28);
            this.btnPosZMinus.TabIndex = 13;
            this.btnPosZMinus.Text = "Z-";
            this.btnPosZMinus.UseVisualStyleBackColor = true;
            this.btnPosZMinus.Click += new System.EventHandler(this.btnPosZMinus_Click);
            //
            // grpRotation
            //
            this.grpRotation.Controls.Add(this.rdoRotLocal);
            this.grpRotation.Controls.Add(this.rdoRotWorld);
            this.grpRotation.Controls.Add(this.btnRotZ);
            this.grpRotation.Controls.Add(this.btnRotY);
            this.grpRotation.Controls.Add(this.btnRotX);
            this.grpRotation.Controls.Add(this.numAngleStep);
            this.grpRotation.Controls.Add(this.lblAngleStep);
            this.grpRotation.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;
            this.grpRotation.Location = new System.Drawing.Point(8, 404);
            this.grpRotation.Name = "grpRotation";
            this.grpRotation.Size = new System.Drawing.Size(296, 104);
            this.grpRotation.TabIndex = 17;
            this.grpRotation.TabStop = false;
            this.grpRotation.Text = "Rotation";
            //
            // rdoRotWorld
            //
            this.rdoRotWorld.AutoSize = true;
            this.rdoRotWorld.Checked = true;
            this.rdoRotWorld.Location = new System.Drawing.Point(8, 44);
            this.rdoRotWorld.Name = "rdoRotWorld";
            this.rdoRotWorld.Size = new System.Drawing.Size(88, 19);
            this.rdoRotWorld.TabIndex = 5;
            this.rdoRotWorld.TabStop = true;
            this.rdoRotWorld.Text = "World axes";
            this.rdoRotWorld.UseVisualStyleBackColor = true;
            this.rdoRotWorld.CheckedChanged += new System.EventHandler(this.rdoRotAxisMode_CheckedChanged);
            //
            // rdoRotLocal
            //
            this.rdoRotLocal.AutoSize = true;
            this.rdoRotLocal.Location = new System.Drawing.Point(148, 44);
            this.rdoRotLocal.Name = "rdoRotLocal";
            this.rdoRotLocal.Size = new System.Drawing.Size(92, 19);
            this.rdoRotLocal.TabIndex = 6;
            this.rdoRotLocal.TabStop = true;
            this.rdoRotLocal.Text = "Object axes";
            this.rdoRotLocal.UseVisualStyleBackColor = true;
            this.rdoRotLocal.CheckedChanged += new System.EventHandler(this.rdoRotAxisMode_CheckedChanged);
            //
            // lblAngleStep
            //
            this.lblAngleStep.AutoSize = true;
            this.lblAngleStep.Location = new System.Drawing.Point(8, 22);
            this.lblAngleStep.Name = "lblAngleStep";
            this.lblAngleStep.Size = new System.Drawing.Size(78, 15);
            this.lblAngleStep.TabIndex = 0;
            this.lblAngleStep.Text = "Angle (Deg):";
            //
            // numAngleStep
            //
            this.numAngleStep.DecimalPlaces = 1;
            this.numAngleStep.Increment = new decimal(new int[] { 90, 0, 0, 0 });
            this.numAngleStep.Location = new System.Drawing.Point(108, 20);
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
            this.btnRotX.Location = new System.Drawing.Point(8, 70);
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
            this.btnRotY.Location = new System.Drawing.Point(100, 70);
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
            this.btnRotZ.Location = new System.Drawing.Point(203, 70);
            this.btnRotZ.Name = "btnRotZ";
            this.btnRotZ.Size = new System.Drawing.Size(86, 28);
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
            this.dgvNodeParams.Location = new System.Drawing.Point(11, 504);
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
            this.btnResolveExpr.Location = new System.Drawing.Point(11, 624);
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
            this.btnApplyNode.Location = new System.Drawing.Point(158, 624);
            this.btnApplyNode.Name = "btnApplyNode";
            this.btnApplyNode.Size = new System.Drawing.Size(138, 30);
            this.btnApplyNode.TabIndex = 14;
            this.btnApplyNode.Text = "Apply";
            this.btnApplyNode.UseVisualStyleBackColor = true;
            this.btnApplyNode.Click += new System.EventHandler(this.btnApplyNode_Click);
            //
            // lblSceneStatus
            //
            this.lblSceneStatus.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;
            this.lblSceneStatus.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lblSceneStatus.Location = new System.Drawing.Point(11, 662);
            this.lblSceneStatus.Name = "lblSceneStatus";
            this.lblSceneStatus.Size = new System.Drawing.Size(285, 36);
            this.lblSceneStatus.TabIndex = 15;
            //
            // ComposerForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.Size = new System.Drawing.Size(320, 720);
            this.Controls.Add(this.tabMain);
            this.Name = "ComposerForm";
            this.tabMain.ResumeLayout(false);
            this.grpSceneTools.ResumeLayout(false);
            this.grpSkeleton.ResumeLayout(false);
            this.grpCatalogParts.ResumeLayout(false);
            this.tabSetup.ResumeLayout(false);
            this.tabSetup.PerformLayout();
            this.tabScene.ResumeLayout(false);
            this.tabScene.PerformLayout();
            this.tabCode.ResumeLayout(false);
            this.tabCode.PerformLayout();

            this.tabPortManager.ResumeLayout(false);
            this.tabPortManager.PerformLayout();
            this.grpPortMove.ResumeLayout(false);
            this.grpPortMove.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numPortMoveStep)).EndInit();
            this.grpPortRotDir.ResumeLayout(false);
            this.grpPortRotDir.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numPortRotStep)).EndInit();
            this.tabBooleans.ResumeLayout(false);
            this.tabBooleans.PerformLayout();
            this.grpBoolCutters.ResumeLayout(false);
            this.grpPosition.ResumeLayout(false);
            this.grpPosition.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numPosStepX)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPosStepY)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPosStepZ)).EndInit();
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

        private System.Windows.Forms.TabPage tabPortManager;
        private System.Windows.Forms.ListView lvPorts;
        private System.Windows.Forms.Button btnAddPort;
        private System.Windows.Forms.Button btnPickPoint;
        private System.Windows.Forms.Button btnDeletePort;
        private System.Windows.Forms.Button btnCopyPort;
        private System.Windows.Forms.CheckBox chkShowPortMarkers;
        private System.Windows.Forms.Label lblPortNumber;
        private System.Windows.Forms.TextBox txtPortNumber;
        private System.Windows.Forms.Label lblPortType;
        private System.Windows.Forms.ComboBox cmbPortType;
        private System.Windows.Forms.Label lblPortParent;
        private System.Windows.Forms.ComboBox cmbPortParent;
        private System.Windows.Forms.Label lblPortPos;
        private System.Windows.Forms.TextBox txtPortX;
        private System.Windows.Forms.TextBox txtPortY;
        private System.Windows.Forms.TextBox txtPortZ;
        private System.Windows.Forms.Label lblPortDir;
        private System.Windows.Forms.TextBox txtPortDx;
        private System.Windows.Forms.TextBox txtPortDy;
        private System.Windows.Forms.TextBox txtPortDz;
        private System.Windows.Forms.Button btnApplyPort;
        private System.Windows.Forms.GroupBox grpPortMove;
        private System.Windows.Forms.Label lblPortMoveStep;
        private System.Windows.Forms.NumericUpDown numPortMoveStep;
        private System.Windows.Forms.Button btnPortMoveXPlus;
        private System.Windows.Forms.Button btnPortMoveXMinus;
        private System.Windows.Forms.Button btnPortMoveYPlus;
        private System.Windows.Forms.Button btnPortMoveYMinus;
        private System.Windows.Forms.Button btnPortMoveZPlus;
        private System.Windows.Forms.Button btnPortMoveZMinus;
        private System.Windows.Forms.GroupBox grpPortRotDir;
        private System.Windows.Forms.RadioButton rdoPortRotWorld;
        private System.Windows.Forms.RadioButton rdoPortRotLocal;
        private System.Windows.Forms.Label lblPortRotStep;
        private System.Windows.Forms.NumericUpDown numPortRotStep;
        private System.Windows.Forms.Button btnPortRotX;
        private System.Windows.Forms.Button btnPortRotY;
        private System.Windows.Forms.Button btnPortRotZ;
        private System.Windows.Forms.Label lblPortStatus;
        private System.Windows.Forms.TabPage tabCode;
        private System.Windows.Forms.TextBox txtGeneratedCode;
        private System.Windows.Forms.Label lblCodeHint;
        private System.Windows.Forms.TabPage tabBooleans;
        private System.Windows.Forms.Label lblValveType;
        private System.Windows.Forms.Label lblDN;
        private System.Windows.Forms.Label lblPressureClass;
        private System.Windows.Forms.ComboBox cmbValveType;
        private System.Windows.Forms.ComboBox cmbDn;
        private System.Windows.Forms.ComboBox cmbPressureClass;
        private System.Windows.Forms.GroupBox grpSkeleton;
        private System.Windows.Forms.Button btnCreateSkeleton;
        private System.Windows.Forms.GroupBox grpCatalogParts;
        private System.Windows.Forms.Label lblCatalogCategory;
        private System.Windows.Forms.ComboBox cmbCatalogCategory;
        private System.Windows.Forms.Label lblCatalogPart;
        private System.Windows.Forms.ComboBox cmbCatalogPart;
        private System.Windows.Forms.Label lblCatalogDN;
        private System.Windows.Forms.ComboBox cmbCatalogDN;
        private System.Windows.Forms.Label lblCatalogPressureClass;
        private System.Windows.Forms.ComboBox cmbCatalogPressureClass;
        private System.Windows.Forms.Button btnInsertCatalogPart;
        private System.Windows.Forms.Label lblPrimitiveBuild;
        private System.Windows.Forms.Label lblPrimitive;
        private System.Windows.Forms.ComboBox cmbPrimitive;
        private System.Windows.Forms.Button btnInsertPrimitive;
        private System.Windows.Forms.GroupBox grpSceneTools;
        private System.Windows.Forms.Button btnGenerateCode;
        private System.Windows.Forms.Button btnDeployCatalog;
        private System.Windows.Forms.Button btnPublishCatalog;
        private System.Windows.Forms.Button btnTestCatalog;
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
        private System.Windows.Forms.Label lblStepX;
        private System.Windows.Forms.Label lblStepY;
        private System.Windows.Forms.Label lblStepZ;
        private System.Windows.Forms.NumericUpDown numPosStepX;
        private System.Windows.Forms.NumericUpDown numPosStepY;
        private System.Windows.Forms.NumericUpDown numPosStepZ;
        private System.Windows.Forms.Button btnPickPosStep;
        private System.Windows.Forms.Button btnAlignPos;
        private System.Windows.Forms.Button btnPosYPlus;
        private System.Windows.Forms.Button btnPosXMinus;
        private System.Windows.Forms.Button btnPosXPlus;
        private System.Windows.Forms.Button btnPosYMinus;
        private System.Windows.Forms.Button btnPosZPlus;
        private System.Windows.Forms.Button btnPosZMinus;
        private System.Windows.Forms.GroupBox grpRotation;
        private System.Windows.Forms.RadioButton rdoRotWorld;
        private System.Windows.Forms.RadioButton rdoRotLocal;
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
        private System.Windows.Forms.GroupBox grpBoolCutters;
        private System.Windows.Forms.ComboBox cmbBoolCutter;
        private System.Windows.Forms.Button btnInsertBoolCutter;
        private System.Windows.Forms.Button btnBoolCutterSubtract;
    }
}


