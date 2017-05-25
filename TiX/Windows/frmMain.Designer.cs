namespace TiX.Windows
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMain));
            this.lblStasis = new System.Windows.Forms.Label();
            this.lblCtrl = new System.Windows.Forms.Label();
            this.lblDragTweet = new System.Windows.Forms.Label();
            this.lblSetting = new System.Windows.Forms.Label();
            this.ofd = new System.Windows.Forms.OpenFileDialog();
            this.ntf = new System.Windows.Forms.NotifyIcon(this.components);
            this.SuspendLayout();
            // 
            // lblStasis
            // 
            this.lblStasis.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.lblStasis.ForeColor = System.Drawing.SystemColors.ControlText;
            this.lblStasis.Location = new System.Drawing.Point(12, 85);
            this.lblStasis.Name = "lblStasis";
            this.lblStasis.Size = new System.Drawing.Size(200, 66);
            this.lblStasis.TabIndex = 27;
            this.lblStasis.Text = "PrintScreen 으로 캡쳐하기\r\n+ Alt : 현재 모니터\r\n+ Ctrl : 현재 창 캡쳐하기\r\n+ Shift : 잘라서 캡쳐하기";
            this.lblStasis.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblStasis.MouseUp += new System.Windows.Forms.MouseEventHandler(this.frmMain_MouseUp);
            // 
            // lblCtrl
            // 
            this.lblCtrl.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblCtrl.ForeColor = System.Drawing.Color.MediumBlue;
            this.lblCtrl.Location = new System.Drawing.Point(12, 34);
            this.lblCtrl.Name = "lblCtrl";
            this.lblCtrl.Size = new System.Drawing.Size(200, 25);
            this.lblCtrl.TabIndex = 25;
            this.lblCtrl.Text = "Ctrl을 눌러 [바로] 작성";
            this.lblCtrl.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblCtrl.MouseUp += new System.Windows.Forms.MouseEventHandler(this.frmMain_MouseUp);
            // 
            // lblDragTweet
            // 
            this.lblDragTweet.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblDragTweet.Location = new System.Drawing.Point(12, 9);
            this.lblDragTweet.Name = "lblDragTweet";
            this.lblDragTweet.Size = new System.Drawing.Size(200, 25);
            this.lblDragTweet.TabIndex = 26;
            this.lblDragTweet.Text = "두번 누르거나 드래그하기";
            this.lblDragTweet.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblDragTweet.MouseUp += new System.Windows.Forms.MouseEventHandler(this.frmMain_MouseUp);
            // 
            // lblSetting
            // 
            this.lblSetting.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.lblSetting.ForeColor = System.Drawing.SystemColors.ControlText;
            this.lblSetting.Location = new System.Drawing.Point(12, 66);
            this.lblSetting.Name = "lblSetting";
            this.lblSetting.Size = new System.Drawing.Size(200, 19);
            this.lblSetting.TabIndex = 24;
            this.lblSetting.Text = "우클릭하여 설정 열기";
            this.lblSetting.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblSetting.MouseUp += new System.Windows.Forms.MouseEventHandler(this.frmMain_MouseUp);
            // 
            // ofd
            // 
            this.ofd.Filter = "지원하는 모든 파일|*.bmp;*.emf;*.exif;*.gif;*.ico;*.jpg;*.jpeg;*.png;*.tif;*.tiff;*.wmf;*" +
    ".psd";
            this.ofd.Multiselect = true;
            this.ofd.Title = "트윗할 이미지들을 선택해주세요";
            // 
            // ntf
            // 
            this.ntf.Icon = ((System.Drawing.Icon)(resources.GetObject("ntf.Icon")));
            this.ntf.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.ntf_MouseDoubleClick);
            // 
            // frmMain
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(224, 160);
            this.Controls.Add(this.lblStasis);
            this.Controls.Add(this.lblCtrl);
            this.Controls.Add(this.lblDragTweet);
            this.Controls.Add(this.lblSetting);
            this.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.KeyPreview = true;
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.MaximizeBox = false;
            this.Name = "frmMain";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmMain_FormClosing);
            this.Load += new System.EventHandler(this.frmMain_Load);
            this.Shown += new System.EventHandler(this.frmMain_Shown);
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.frmMain_DragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.frmMain_DragOverOrEnter);
            this.DragOver += new System.Windows.Forms.DragEventHandler(this.frmMain_DragOverOrEnter);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.frmMain_KeyDown);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.frmMain_MouseUp);
            this.Resize += new System.EventHandler(this.frmMain_Resize);
            this.ResumeLayout(false);

		}

		#endregion

        private System.Windows.Forms.Label lblStasis;
        private System.Windows.Forms.Label lblCtrl;
        private System.Windows.Forms.Label lblDragTweet;
        private System.Windows.Forms.Label lblSetting;
        private System.Windows.Forms.OpenFileDialog ofd;
        private System.Windows.Forms.NotifyIcon ntf;
    }
}
