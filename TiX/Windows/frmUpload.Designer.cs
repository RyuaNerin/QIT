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
            this.picImage = new System.Windows.Forms.PictureBox();
            this.txtText = new System.Windows.Forms.TextBox();
            this.lblRange = new System.Windows.Forms.Label();
            this.lblLength = new System.Windows.Forms.Label();
            this.progress = new TiX.Windows.ProgressCircle();
            ((System.ComponentModel.ISupportInitialize)(this.picImage)).BeginInit();
            this.SuspendLayout();
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
            this.picImage.MouseMove += new System.Windows.Forms.MouseEventHandler(this.picImage_MouseMove);
            this.picImage.MouseUp += new System.Windows.Forms.MouseEventHandler(this.picImage_MouseUp);
            // 
            // txtText
            // 
            this.txtText.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
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
            // lblRange
            // 
            this.lblRange.Location = new System.Drawing.Point(82, 13);
            this.lblRange.Name = "lblRange";
            this.lblRange.Size = new System.Drawing.Size(54, 15);
            this.lblRange.TabIndex = 11;
            // 
            // lblLength
            // 
            this.lblLength.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblLength.Location = new System.Drawing.Point(208, 13);
            this.lblLength.Name = "lblLength";
            this.lblLength.Size = new System.Drawing.Size(103, 15);
            this.lblLength.TabIndex = 12;
            this.lblLength.Text = "0 / 0";
            this.lblLength.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // progress
            // 
            this.progress.Location = new System.Drawing.Point(12, 13);
            this.progress.Name = "progress";
            this.progress.Size = new System.Drawing.Size(64, 64);
            this.progress.SmallSize = false;
            this.progress.TabIndex = 13;
            this.progress.Text = "progressCircle1";
            this.progress.Value = 0;
            this.progress.Visible = false;
            // 
            // frmUpload
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(323, 90);
            this.Controls.Add(this.progress);
            this.Controls.Add(this.lblLength);
            this.Controls.Add(this.lblRange);
            this.Controls.Add(this.picImage);
            this.Controls.Add(this.txtText);
            this.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
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

        private System.Windows.Forms.PictureBox picImage;
        private System.Windows.Forms.TextBox txtText;
        private System.Windows.Forms.Label lblRange;
        private System.Windows.Forms.Label lblLength;
        private ProgressCircle progress;
    }
}

