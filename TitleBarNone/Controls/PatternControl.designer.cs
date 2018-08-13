namespace Atma.TitleBarNone.Controls
{
    partial class PatternControl
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.ctxMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.insertTag = new System.Windows.Forms.ToolStripMenuItem();
            this.resetSep = new System.Windows.Forms.ToolStripSeparator();
            this.setToDefault = new System.Windows.Forms.ToolStripMenuItem();
            this.timerUpdate = new System.Windows.Forms.Timer(this.components);
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.lbPreview = new System.Windows.Forms.Label();
            this.txEdit = new System.Windows.Forms.TextBox();
            this.lbMore = new System.Windows.Forms.Label();
            this.ctxMenu.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // ctxMenu
            // 
            this.ctxMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.insertTag,
            this.resetSep,
            this.setToDefault});
            this.ctxMenu.Name = "ctxMenu";
            this.ctxMenu.Size = new System.Drawing.Size(154, 54);
            this.ctxMenu.Opening += new System.ComponentModel.CancelEventHandler(this.ctxMenu_Opening);
            // 
            // insertTag
            // 
            this.insertTag.Name = "insertTag";
            this.insertTag.Size = new System.Drawing.Size(153, 22);
            this.insertTag.Text = "insert [{0}]";
            this.insertTag.Click += new System.EventHandler(this.insertTag_Click);
            // 
            // resetSep
            // 
            this.resetSep.Name = "resetSep";
            this.resetSep.Size = new System.Drawing.Size(150, 6);
            // 
            // setToDefault
            // 
            this.setToDefault.Name = "setToDefault";
            this.setToDefault.Size = new System.Drawing.Size(153, 22);
            this.setToDefault.Text = "reset to default";
            this.setToDefault.Click += new System.EventHandler(this.setToDefault_Click);
            // 
            // timerUpdate
            // 
            this.timerUpdate.Interval = 500;
            this.timerUpdate.Tick += new System.EventHandler(this.timerUpdate_Tick);
            // 
            // groupBox1
            // 
            this.groupBox1.AutoSize = true;
            this.groupBox1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.groupBox1.Controls.Add(this.tableLayoutPanel1);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox1.Location = new System.Drawing.Point(0, 0);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(511, 70);
            this.groupBox1.TabIndex = 3;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Edit Pattern (click on preview or \"...\" to insert tags or reset to default)";
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.AutoSize = true;
            this.tableLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.Controls.Add(this.lbPreview, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.txEdit, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.lbMore, 1, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(3, 16);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(505, 51);
            this.tableLayoutPanel1.TabIndex = 3;
            // 
            // lbPreview
            // 
            this.lbPreview.AutoSize = true;
            this.lbPreview.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.tableLayoutPanel1.SetColumnSpan(this.lbPreview, 2);
            this.lbPreview.ContextMenuStrip = this.ctxMenu;
            this.lbPreview.Cursor = System.Windows.Forms.Cursors.PanSW;
            this.lbPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbPreview.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lbPreview.Location = new System.Drawing.Point(3, 23);
            this.lbPreview.Margin = new System.Windows.Forms.Padding(3);
            this.lbPreview.MaximumSize = new System.Drawing.Size(499, 500);
            this.lbPreview.Name = "lbPreview";
            this.lbPreview.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.lbPreview.Size = new System.Drawing.Size(499, 25);
            this.lbPreview.TabIndex = 6;
            this.lbPreview.Text = "change text to update preview here";
            this.lbPreview.MouseDown += new System.Windows.Forms.MouseEventHandler(this.lbMore_MouseDown);
            // 
            // txEdit
            // 
            this.txEdit.Dock = System.Windows.Forms.DockStyle.Top;
            this.txEdit.Location = new System.Drawing.Point(0, 0);
            this.txEdit.Margin = new System.Windows.Forms.Padding(0);
            this.txEdit.Name = "txEdit";
            this.txEdit.Size = new System.Drawing.Size(468, 20);
            this.txEdit.TabIndex = 1;
            this.txEdit.TextChanged += new System.EventHandler(this.txEdit_TextChanged);
            // 
            // lbMore
            // 
            this.lbMore.BackColor = System.Drawing.SystemColors.Info;
            this.lbMore.Dock = System.Windows.Forms.DockStyle.Top;
            this.lbMore.ForeColor = System.Drawing.SystemColors.InfoText;
            this.lbMore.Location = new System.Drawing.Point(471, 0);
            this.lbMore.Name = "lbMore";
            this.lbMore.Size = new System.Drawing.Size(31, 20);
            this.lbMore.TabIndex = 5;
            this.lbMore.Text = "...";
            this.lbMore.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lbMore.MouseDown += new System.Windows.Forms.MouseEventHandler(this.lbMore_MouseDown);
            // 
            // PatternControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.Controls.Add(this.groupBox1);
            this.ForeColor = System.Drawing.SystemColors.ButtonFace;
            this.MinimumSize = new System.Drawing.Size(0, 70);
            this.Name = "PatternControl";
            this.Size = new System.Drawing.Size(511, 70);
            this.ctxMenu.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ContextMenuStrip ctxMenu;
        private System.Windows.Forms.ToolStripMenuItem setToDefault;
        private System.Windows.Forms.ToolStripMenuItem insertTag;
        private System.Windows.Forms.ToolStripSeparator resetSep;
        private System.Windows.Forms.Timer timerUpdate;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label lbPreview;
        private System.Windows.Forms.TextBox txEdit;
        private System.Windows.Forms.Label lbMore;
    }
}
