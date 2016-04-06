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
            if (disposing)
            {
                if (this.m_ic != null)
                {
                    this.m_ic.Dispose();
                    this.m_ic = null;
                }

                if (components != null)
                    components.Dispose();
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
            this.bgwTweet = new System.ComponentModel.BackgroundWorker();
            this.picImage = new System.Windows.Forms.PictureBox();
            this.txtText = new System.Windows.Forms.TextBox();
            this.bgwResize = new System.ComponentModel.BackgroundWorker();
            this.ajax = new TiX.Windows.AjaxControl();
            this.lblRange = new System.Windows.Forms.Label();
            this.lblLength = new System.Windows.Forms.Label();
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
            // txtText
            // 
            this.txtText.ForeColor = System.Drawing.SystemColors.WindowText;
            this.txtText.Location = new System.Drawing.Point(82, 32);
            this.txtText.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.txtText.MaxLength = 256;
            this.txtText.Multiline = true;
            this.txtText.Name = "txtText";
            this.txtText.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtText.Size = new System.Drawing.Size(229, 45);
            this.txtText.TabIndex = 6;
            this.txtText.TextChanged += new System.EventHandler(this.txtText_TextChanged);
            this.txtText.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtText_KeyDown);
            this.txtText.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtText_KeyPress);
            // 
            // ajax
            // 
            this.ajax.Is16 = true;
            this.ajax.Location = new System.Drawing.Point(295, 12);
            this.ajax.Name = "ajax";
            this.ajax.Size = new System.Drawing.Size(16, 16);
            this.ajax.TabIndex = 7;
            this.ajax.Visible = false;
            // 
            // lblRange
            // 
            this.lblRange.Location = new System.Drawing.Point(82, 13);
            this.lblRange.Name = "lblRange";
            this.lblRange.Size = new System.Drawing.Size(54, 15);
            this.lblRange.TabIndex = 11;
            // 
            // lblLength
            // 
            this.lblLength.Location = new System.Drawing.Point(186, 13);
            this.lblLength.Name = "lblLength";
            this.lblLength.Size = new System.Drawing.Size(103, 15);
            this.lblLength.TabIndex = 12;
            this.lblLength.Text = "0 / 0";
            this.lblLength.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // frmUpload
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(323, 90);
            this.Controls.Add(this.lblLength);
            this.Controls.Add(this.lblRange);
            this.Controls.Add(this.ajax);
            this.Controls.Add(this.picImage);
            this.Controls.Add(this.txtText);
            this.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.MaximizeBox = false;
            this.Name = "frmUpload";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "TiX";
            this.TopMost = true;
            this.Activated += new System.EventHandler(this.frmUpload_Activated);
            this.Shown += new System.EventHandler(this.frmUpload_Shown);
            this.Enter += new System.EventHandler(this.frmUpload_Enter);
            ((System.ComponentModel.ISupportInitialize)(this.picImage)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.ComponentModel.BackgroundWorker bgwTweet;
        private System.Windows.Forms.PictureBox picImage;
        private AjaxControl ajax;
        private System.Windows.Forms.TextBox txtText;
        private System.ComponentModel.BackgroundWorker bgwResize;
        private System.Windows.Forms.Label lblRange;
        private System.Windows.Forms.Label lblLength;
    }
}

