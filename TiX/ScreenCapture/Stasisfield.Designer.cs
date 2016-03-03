namespace TiX.ScreenCapture
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Stasisfield));
            this.SuspendLayout();
            // 
            // Stasisfield
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 19F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(250, 272);
            this.Cursor = System.Windows.Forms.Cursors.Cross;
            this.DoubleBuffered = true;
            this.Font = new System.Drawing.Font("Consolas", 12F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
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