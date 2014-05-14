namespace QIT
{
	partial class frmMain
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
			this.pnl = new System.Windows.Forms.Panel();
			this.cms = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.btnExtOrig = new System.Windows.Forms.ToolStripMenuItem();
			this.tssAuto = new System.Windows.Forms.ToolStripSeparator();
			this.btnExtPNG = new System.Windows.Forms.ToolStripMenuItem();
			this.btnExtJPG = new System.Windows.Forms.ToolStripMenuItem();
			this.tssExt = new System.Windows.Forms.ToolStripSeparator();
			this.btnExtPNGTrans = new System.Windows.Forms.ToolStripMenuItem();
			this.tssTrans = new System.Windows.Forms.ToolStripSeparator();
			this.btmCopyright = new System.Windows.Forms.ToolStripMenuItem();
			this.tmrQueue = new System.Windows.Forms.Timer(this.components);
			this.cms.SuspendLayout();
			this.SuspendLayout();
			// 
			// pnl
			// 
			this.pnl.AllowDrop = true;
			this.pnl.BackgroundImage = global::QIT.Properties.Resources.drag;
			this.pnl.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
			this.pnl.ContextMenuStrip = this.cms;
			this.pnl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.pnl.Location = new System.Drawing.Point(0, 0);
			this.pnl.Name = "pnl";
			this.pnl.Size = new System.Drawing.Size(224, 112);
			this.pnl.TabIndex = 0;
			this.pnl.DragDrop += new System.Windows.Forms.DragEventHandler(this.pnl_DragDrop);
			this.pnl.DragOver += new System.Windows.Forms.DragEventHandler(this.pnl_DragOver);
			// 
			// cms
			// 
			this.cms.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnExtOrig,
            this.tssAuto,
            this.btnExtPNG,
            this.btnExtJPG,
            this.tssExt,
            this.btnExtPNGTrans,
            this.tssTrans,
            this.btmCopyright});
			this.cms.Name = "cms";
			this.cms.Size = new System.Drawing.Size(191, 132);
			// 
			// btnExtOrig
			// 
			this.btnExtOrig.Checked = true;
			this.btnExtOrig.CheckOnClick = true;
			this.btnExtOrig.CheckState = System.Windows.Forms.CheckState.Checked;
			this.btnExtOrig.Name = "btnExtOrig";
			this.btnExtOrig.Size = new System.Drawing.Size(190, 22);
			this.btnExtOrig.Text = "자동 판단";
			this.btnExtOrig.CheckedChanged += new System.EventHandler(this.btnExtOrig_CheckedChanged);
			// 
			// tssAuto
			// 
			this.tssAuto.Name = "tssAuto";
			this.tssAuto.Size = new System.Drawing.Size(187, 6);
			// 
			// btnExtPNG
			// 
			this.btnExtPNG.CheckOnClick = true;
			this.btnExtPNG.Name = "btnExtPNG";
			this.btnExtPNG.Size = new System.Drawing.Size(190, 22);
			this.btnExtPNG.Text = "PNG 파일로 리사이징";
			this.btnExtPNG.CheckedChanged += new System.EventHandler(this.btnExtPNG_CheckedChanged);
			// 
			// btnExtJPG
			// 
			this.btnExtJPG.CheckOnClick = true;
			this.btnExtJPG.Name = "btnExtJPG";
			this.btnExtJPG.Size = new System.Drawing.Size(190, 22);
			this.btnExtJPG.Text = "JPG 파일로 리사이징";
			this.btnExtJPG.CheckedChanged += new System.EventHandler(this.btnExtJPG_CheckedChanged);
			// 
			// tssExt
			// 
			this.tssExt.Name = "tssExt";
			this.tssExt.Size = new System.Drawing.Size(187, 6);
			// 
			// btnExtPNGTrans
			// 
			this.btnExtPNGTrans.CheckOnClick = true;
			this.btnExtPNGTrans.Name = "btnExtPNGTrans";
			this.btnExtPNGTrans.Size = new System.Drawing.Size(190, 22);
			this.btnExtPNGTrans.Text = "PNG 투명성 유지";
			this.btnExtPNGTrans.CheckedChanged += new System.EventHandler(this.btnExtPNGTrans_CheckedChanged);
			// 
			// tssTrans
			// 
			this.tssTrans.Name = "tssTrans";
			this.tssTrans.Size = new System.Drawing.Size(187, 6);
			// 
			// btmCopyright
			// 
			this.btmCopyright.Name = "btmCopyright";
			this.btmCopyright.Size = new System.Drawing.Size(190, 22);
			this.btmCopyright.Text = "Made By RyuaNerin";
			this.btmCopyright.Click += new System.EventHandler(this.btmCopyright_Click);
			// 
			// tmrQueue
			// 
			this.tmrQueue.Interval = 1;
			this.tmrQueue.Tick += new System.EventHandler(this.tmrQueue_Tick);
			// 
			// frmMain
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(224, 112);
			this.Controls.Add(this.pnl);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.KeyPreview = true;
			this.MaximizeBox = false;
			this.Name = "frmMain";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.TopMost = true;
			this.cms.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Panel pnl;
		private System.Windows.Forms.Timer tmrQueue;
		private System.Windows.Forms.ContextMenuStrip cms;
		private System.Windows.Forms.ToolStripMenuItem btnExtOrig;
		private System.Windows.Forms.ToolStripMenuItem btnExtJPG;
		private System.Windows.Forms.ToolStripMenuItem btnExtPNG;
		private System.Windows.Forms.ToolStripSeparator tssExt;
		private System.Windows.Forms.ToolStripMenuItem btmCopyright;
		private System.Windows.Forms.ToolStripSeparator tssAuto;
		private System.Windows.Forms.ToolStripMenuItem btnExtPNGTrans;
		private System.Windows.Forms.ToolStripSeparator tssTrans;
	}
}