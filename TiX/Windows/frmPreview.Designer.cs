namespace TiX.Windows
{
	partial class frmPreview
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
            this.components = new System.ComponentModel.Container();
            this.cmsQuality = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.tsmQualityLow = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmQualityHigh = new System.Windows.Forms.ToolStripMenuItem();
            this.cmsQuality.SuspendLayout();
            this.SuspendLayout();
            // 
            // cmsQuality
            // 
            this.cmsQuality.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmQualityLow,
            this.tsmQualityHigh});
            this.cmsQuality.Name = "cmsQuality";
            this.cmsQuality.Size = new System.Drawing.Size(153, 70);
            // 
            // tsmQualityLow
            // 
            this.tsmQualityLow.CheckOnClick = true;
            this.tsmQualityLow.Name = "tsmQualityLow";
            this.tsmQualityLow.Size = new System.Drawing.Size(152, 22);
            this.tsmQualityLow.Text = "저품질 / 빠름";
            this.tsmQualityLow.CheckedChanged += new System.EventHandler(this.tsmQualityLow_CheckedChanged);
            // 
            // tsmQualityHigh
            // 
            this.tsmQualityHigh.Checked = true;
            this.tsmQualityHigh.CheckOnClick = true;
            this.tsmQualityHigh.CheckState = System.Windows.Forms.CheckState.Checked;
            this.tsmQualityHigh.Name = "tsmQualityHigh";
            this.tsmQualityHigh.Size = new System.Drawing.Size(152, 22);
            this.tsmQualityHigh.Text = "고품질 / 느림";
            this.tsmQualityHigh.CheckedChanged += new System.EventHandler(this.tsmQualityHigh_CheckedChanged);
            // 
            // frmPreview
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(184, 166);
            this.ContextMenuStrip = this.cmsQuality;
            this.DoubleBuffered = true;
            this.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.KeyPreview = true;
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.MinimumSize = new System.Drawing.Size(200, 200);
            this.Name = "frmPreview";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "이미지 미리보기";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.frmPreview_FormClosed);
            this.Load += new System.EventHandler(this.frmPreview_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.frmPreview_KeyDown);
            this.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.pic_MouseDoubleClick);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pic_MouseDown);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pic_MouseMove);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.pic_MouseUp);
            this.Resize += new System.EventHandler(this.frmPreview_Resize);
            this.cmsQuality.ResumeLayout(false);
            this.ResumeLayout(false);

		}



        #endregion

        private System.Windows.Forms.ContextMenuStrip cmsQuality;
        private System.Windows.Forms.ToolStripMenuItem tsmQualityLow;
        private System.Windows.Forms.ToolStripMenuItem tsmQualityHigh;
    }
}
