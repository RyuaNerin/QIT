namespace TiX.Windows
{
	partial class frmUpload
	{
		/// <summary>
		/// 필수 디자이너 변수입니다.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// 사용 중인 모든 리소스를 정리합니다.
		/// </summary>
		/// <param name="disposing">관리되는 리소스를 삭제해야 하면 true이고, 그렇지 않으면 false입니다.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
                this.Clear();
			}
			base.Dispose(disposing);
		}

		#region Windows Form 디자이너에서 생성한 코드

		/// <summary>
		/// 디자이너 지원에 필요한 메서드입니다.
		/// 이 메서드의 내용을 코드 편집기로 수정하지 마십시오.
		/// </summary>
		private void InitializeComponent()
		{
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmUpload));
            this.bgwTweet = new System.ComponentModel.BackgroundWorker();
            this.picImage = new System.Windows.Forms.PictureBox();
            this.lblLength = new System.Windows.Forms.Label();
            this.txtText = new System.Windows.Forms.TextBox();
            this.lblImageSize = new System.Windows.Forms.Label();
            this.bgwResize = new System.ComponentModel.BackgroundWorker();
            this.ajax = new TiX.Windows.AjaxControl();
            ((System.ComponentModel.ISupportInitialize)(this.picImage)).BeginInit();
            this.SuspendLayout();
            // 
            // bgwTweet
            // 
            this.bgwTweet.DoWork += new System.ComponentModel.DoWorkEventHandler(this.bgwTweet_DoWork);
            this.bgwTweet.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.bgwTweet_RunWorkerCompleted);
            // 
            // picImage
            // 
            this.picImage.Location = new System.Drawing.Point(12, 13);
            this.picImage.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.picImage.Name = "picImage";
            this.picImage.Size = new System.Drawing.Size(64, 64);
            this.picImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.picImage.TabIndex = 4;
            this.picImage.TabStop = false;
            this.picImage.MouseDown += new System.Windows.Forms.MouseEventHandler(this.picImage_MouseDown);
            // 
            // lblLength
            // 
            this.lblLength.Location = new System.Drawing.Point(249, 9);
            this.lblLength.Name = "lblLength";
            this.lblLength.Size = new System.Drawing.Size(62, 15);
            this.lblLength.TabIndex = 5;
            this.lblLength.Text = "117 / 117";
            this.lblLength.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtText
            // 
            this.txtText.ForeColor = System.Drawing.SystemColors.WindowText;
            this.txtText.Location = new System.Drawing.Point(82, 28);
            this.txtText.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.txtText.MaxLength = 140;
            this.txtText.Multiline = true;
            this.txtText.Name = "txtText";
            this.txtText.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtText.Size = new System.Drawing.Size(229, 49);
            this.txtText.TabIndex = 6;
            this.txtText.TextChanged += new System.EventHandler(this.txtText_TextChanged);
            this.txtText.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtText_KeyDown);
            this.txtText.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtText_KeyPress);
            // 
            // lblImageSize
            // 
            this.lblImageSize.Location = new System.Drawing.Point(82, 13);
            this.lblImageSize.Name = "lblImageSize";
            this.lblImageSize.Size = new System.Drawing.Size(161, 15);
            this.lblImageSize.TabIndex = 8;
            // 
            // bgwResize
            // 
            this.bgwResize.DoWork += new System.ComponentModel.DoWorkEventHandler(this.bgwResize_DoWork);
            this.bgwResize.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.bgwResize_RunWorkerCompleted);
            // 
            // ajax
            // 
            this.ajax.Is16 = false;
            this.ajax.Location = new System.Drawing.Point(176, 42);
            this.ajax.Name = "ajax";
            this.ajax.Size = new System.Drawing.Size(32, 32);
            this.ajax.TabIndex = 7;
            this.ajax.Visible = false;
            // 
            // frmUpload
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(323, 90);
            this.Controls.Add(this.lblImageSize);
            this.Controls.Add(this.ajax);
            this.Controls.Add(this.picImage);
            this.Controls.Add(this.lblLength);
            this.Controls.Add(this.txtText);
            this.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.MaximizeBox = false;
            this.Name = "frmUpload";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "TiX";
            this.TopMost = true;
            this.Activated += new System.EventHandler(this.frmUpload_Activated);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.frmUpload_FormClosed);
            this.Shown += new System.EventHandler(this.frmUpload_Shown);
            this.Enter += new System.EventHandler(this.frmUpload_Enter);
            ((System.ComponentModel.ISupportInitialize)(this.picImage)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.ComponentModel.BackgroundWorker bgwTweet;
		private System.Windows.Forms.PictureBox picImage;
        private System.Windows.Forms.Label lblLength;
		private AjaxControl ajax;
		private System.Windows.Forms.Label lblImageSize;
        private System.Windows.Forms.TextBox txtText;
        private System.ComponentModel.BackgroundWorker bgwResize;
	}
}

