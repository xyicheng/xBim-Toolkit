namespace Xbim.COBie.Client
{
    partial class COBieGenerator
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.btnBrowseTemplate = new System.Windows.Forms.Button();
            this.txtTemplate = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txtPath = new System.Windows.Forms.ComboBox();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.btnGenerate = new System.Windows.Forms.Button();
            this.btnClear = new System.Windows.Forms.Button();
            this.txtOutput = new System.Windows.Forms.RichTextBox();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.ProgressBar = new System.Windows.Forms.ToolStripProgressBar();
            this.StatusMsg = new System.Windows.Forms.ToolStripStatusLabel();
            this.gbFilter = new System.Windows.Forms.GroupBox();
            this.rbNoFilters = new System.Windows.Forms.RadioButton();
            this.rbPickList = new System.Windows.Forms.RadioButton();
            this.rbDefault = new System.Windows.Forms.RadioButton();
            this.MergeChkBox = new System.Windows.Forms.CheckBox();
            this.groupBox1.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.gbFilter.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.MergeChkBox);
            this.groupBox1.Controls.Add(this.btnBrowseTemplate);
            this.groupBox1.Controls.Add(this.txtTemplate);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.txtPath);
            this.groupBox1.Controls.Add(this.btnBrowse);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Location = new System.Drawing.Point(13, 13);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(498, 96);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "IFC File Location";
            // 
            // btnBrowseTemplate
            // 
            this.btnBrowseTemplate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowseTemplate.Location = new System.Drawing.Point(417, 44);
            this.btnBrowseTemplate.Name = "btnBrowseTemplate";
            this.btnBrowseTemplate.Size = new System.Drawing.Size(75, 23);
            this.btnBrowseTemplate.TabIndex = 4;
            this.btnBrowseTemplate.Text = "&Browse...";
            this.btnBrowseTemplate.UseVisualStyleBackColor = true;
            this.btnBrowseTemplate.Click += new System.EventHandler(this.btnBrowseTemplate_Click);
            // 
            // txtTemplate
            // 
            this.txtTemplate.FormattingEnabled = true;
            this.txtTemplate.Items.AddRange(new object[] {
            "COBie-UK-2012-template.xls",
            "COBie-US-2_4-template.xls"});
            this.txtTemplate.Location = new System.Drawing.Point(79, 46);
            this.txtTemplate.Name = "txtTemplate";
            this.txtTemplate.Size = new System.Drawing.Size(326, 21);
            this.txtTemplate.TabIndex = 3;
            this.txtTemplate.Text = "COBie-UK-2012-template.xls";
            this.txtTemplate.TextChanged += new System.EventHandler(this.txtTemplate_TextChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 49);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(54, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Template:";
            // 
            // txtPath
            // 
            this.txtPath.FormattingEnabled = true;
            this.txtPath.Items.AddRange(new object[] {
            "2012-03-23-Duplex-Design.ifc",
            "2012-03-23-Duplex-Design.xbim",
            "Clinic_A_20110906.ifc",
            "Clinic_A_20110906.xbim",
            "2012-09-03-Clinic-Handover.ifc",
            "2012-09-03-Clinic-Handover.xbim",
            "BCU-XX-XX-A-VCUK-M3-00-0001.Xbim"});
            this.txtPath.Location = new System.Drawing.Point(79, 20);
            this.txtPath.Name = "txtPath";
            this.txtPath.Size = new System.Drawing.Size(326, 21);
            this.txtPath.TabIndex = 0;
            this.txtPath.Text = "2012-03-23-Duplex-Design.ifc";
            // 
            // btnBrowse
            // 
            this.btnBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowse.Location = new System.Drawing.Point(417, 18);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(75, 23);
            this.btnBrowse.TabIndex = 1;
            this.btnBrowse.Text = "&Browse...";
            this.btnBrowse.UseVisualStyleBackColor = true;
            this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 23);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(56, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Select file:";
            // 
            // btnGenerate
            // 
            this.btnGenerate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnGenerate.Location = new System.Drawing.Point(430, 321);
            this.btnGenerate.Name = "btnGenerate";
            this.btnGenerate.Size = new System.Drawing.Size(75, 23);
            this.btnGenerate.TabIndex = 2;
            this.btnGenerate.Text = "&Generate";
            this.btnGenerate.UseVisualStyleBackColor = true;
            this.btnGenerate.Click += new System.EventHandler(this.btnGenerate_Click);
            // 
            // btnClear
            // 
            this.btnClear.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClear.Location = new System.Drawing.Point(430, 291);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(75, 23);
            this.btnClear.TabIndex = 4;
            this.btnClear.Text = "&Clear";
            this.btnClear.UseVisualStyleBackColor = true;
            this.btnClear.Click += new System.EventHandler(this.btnClear_Click);
            // 
            // txtOutput
            // 
            this.txtOutput.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtOutput.Location = new System.Drawing.Point(13, 115);
            this.txtOutput.Name = "txtOutput";
            this.txtOutput.ReadOnly = true;
            this.txtOutput.Size = new System.Drawing.Size(400, 229);
            this.txtOutput.TabIndex = 5;
            this.txtOutput.Text = "";
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ProgressBar,
            this.StatusMsg});
            this.statusStrip1.Location = new System.Drawing.Point(0, 353);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(523, 22);
            this.statusStrip1.TabIndex = 7;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // ProgressBar
            // 
            this.ProgressBar.AutoSize = false;
            this.ProgressBar.Name = "ProgressBar";
            this.ProgressBar.Size = new System.Drawing.Size(150, 16);
            // 
            // StatusMsg
            // 
            this.StatusMsg.Name = "StatusMsg";
            this.StatusMsg.Size = new System.Drawing.Size(0, 17);
            // 
            // gbFilter
            // 
            this.gbFilter.Controls.Add(this.rbNoFilters);
            this.gbFilter.Controls.Add(this.rbPickList);
            this.gbFilter.Controls.Add(this.rbDefault);
            this.gbFilter.Location = new System.Drawing.Point(424, 115);
            this.gbFilter.Name = "gbFilter";
            this.gbFilter.Size = new System.Drawing.Size(87, 98);
            this.gbFilter.TabIndex = 9;
            this.gbFilter.TabStop = false;
            this.gbFilter.Text = "Class Filter ";
            // 
            // rbNoFilters
            // 
            this.rbNoFilters.AutoSize = true;
            this.rbNoFilters.Location = new System.Drawing.Point(6, 75);
            this.rbNoFilters.Name = "rbNoFilters";
            this.rbNoFilters.Size = new System.Drawing.Size(69, 17);
            this.rbNoFilters.TabIndex = 2;
            this.rbNoFilters.Text = "No Filters";
            this.rbNoFilters.UseVisualStyleBackColor = true;
            // 
            // rbPickList
            // 
            this.rbPickList.AutoSize = true;
            this.rbPickList.Location = new System.Drawing.Point(6, 47);
            this.rbPickList.Name = "rbPickList";
            this.rbPickList.Size = new System.Drawing.Size(65, 17);
            this.rbPickList.TabIndex = 1;
            this.rbPickList.Text = "Pick List";
            this.rbPickList.UseVisualStyleBackColor = true;
            // 
            // rbDefault
            // 
            this.rbDefault.AutoSize = true;
            this.rbDefault.Checked = true;
            this.rbDefault.Location = new System.Drawing.Point(6, 19);
            this.rbDefault.Name = "rbDefault";
            this.rbDefault.Size = new System.Drawing.Size(59, 17);
            this.rbDefault.TabIndex = 0;
            this.rbDefault.TabStop = true;
            this.rbDefault.Text = "Default";
            this.rbDefault.UseVisualStyleBackColor = true;
            // 
            // MergeChkBox
            // 
            this.MergeChkBox.AutoSize = true;
            this.MergeChkBox.Location = new System.Drawing.Point(9, 73);
            this.MergeChkBox.Name = "MergeChkBox";
            this.MergeChkBox.Size = new System.Drawing.Size(196, 17);
            this.MergeChkBox.TabIndex = 5;
            this.MergeChkBox.Text = "Merge two XLS files into one IFC file";
            this.MergeChkBox.UseVisualStyleBackColor = true;
            this.MergeChkBox.CheckedChanged += new System.EventHandler(this.MergeChkBox_CheckedChanged);
            // 
            // COBieGenerator
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(523, 375);
            this.Controls.Add(this.gbFilter);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.txtOutput);
            this.Controls.Add(this.btnClear);
            this.Controls.Add(this.btnGenerate);
            this.Controls.Add(this.groupBox1);
            this.Name = "COBieGenerator";
            this.Text = "Xbim COBie Test Harness";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.gbFilter.ResumeLayout(false);
            this.gbFilter.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button btnBrowse;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnGenerate;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.RichTextBox txtOutput;
        private System.Windows.Forms.ComboBox txtPath;
        private System.Windows.Forms.Button btnBrowseTemplate;
        private System.Windows.Forms.ComboBox txtTemplate;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel StatusMsg;
        private System.Windows.Forms.ToolStripProgressBar ProgressBar;
        private System.Windows.Forms.GroupBox gbFilter;
        private System.Windows.Forms.RadioButton rbNoFilters;
        private System.Windows.Forms.RadioButton rbPickList;
        private System.Windows.Forms.RadioButton rbDefault;
        private System.Windows.Forms.CheckBox MergeChkBox;
    }
}

