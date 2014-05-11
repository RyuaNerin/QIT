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
			this.tmrQueue = new System.Windows.Forms.Timer(this.components);
			this.SuspendLayout();
			// 
			// pnl
			// 
			this.pnl.AllowDrop = true;
			this.pnl.BackgroundImage = global::QIT.Properties.Resources.drag;
			this.pnl.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
			this.pnl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.pnl.Location = new System.Drawing.Point(0, 0);
			this.pnl.Name = "pnl";
			this.pnl.Size = new System.Drawing.Size(224, 112);
			this.pnl.TabIndex = 0;
			this.pnl.DragDrop += new System.Windows.Forms.DragEventHandler(this.pnl_DragDrop);
			this.pnl.DragEnter += new System.Windows.Forms.DragEventHandler(this.pnl_DragEnter);
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
			this.MaximizeBox = false;
			this.Name = "frmMain";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "QIT BETA 2.";
			this.TopMost = true;
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Panel pnl;
		private System.Windows.Forms.Timer tmrQueue;
	}
}