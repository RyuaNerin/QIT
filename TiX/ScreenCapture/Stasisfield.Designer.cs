﻿namespace TiX.ScreenCapture
{
	partial class Stasisfield
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose( bool disposing )
		{
			if ( disposing && ( components != null ) )
			{
				components.Dispose( );
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent( )
		{
            this.SuspendLayout();
            // 
            // Stasisfield
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Cursor = System.Windows.Forms.Cursors.Cross;
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "Stasisfield";
            this.ShowInTaskbar = false;
            this.Text = "Stasisfield";
            this.TopMost = true;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Stasisfield_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Stasisfield_FormClosed);
            this.Shown += new System.EventHandler(this.Stasisfield_Shown);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Stasisfield_KeyDown);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Stasisfield_MouseDown);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.Stasisfield_MouseMove);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.Stasisfield_MouseUp);
            this.ResumeLayout(false);

		}

		#endregion
	}
}