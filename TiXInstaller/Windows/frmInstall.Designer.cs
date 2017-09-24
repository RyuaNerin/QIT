namespace TiX.Windows
{
    partial class frmInstall
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
            this.btn = new System.Windows.Forms.Button();
            this.chkStart = new System.Windows.Forms.CheckBox();
            this.chkStartMenu = new System.Windows.Forms.CheckBox();
            this.chkDesktop = new System.Windows.Forms.CheckBox();
            this.chkErrorReport = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // btn
            // 
            this.btn.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btn.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btn.Location = new System.Drawing.Point(12, 12);
            this.btn.Name = "btn";
            this.btn.Size = new System.Drawing.Size(137, 42);
            this.btn.TabIndex = 0;
            this.btn.Text = "TiX 설치하기";
            this.btn.UseVisualStyleBackColor = true;
            this.btn.Click += new System.EventHandler(this.btn_Click);
            // 
            // chkStart
            // 
            this.chkStart.AutoSize = true;
            this.chkStart.Checked = true;
            this.chkStart.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkStart.Location = new System.Drawing.Point(12, 60);
            this.chkStart.Name = "chkStart";
            this.chkStart.Size = new System.Drawing.Size(114, 19);
            this.chkStart.TabIndex = 1;
            this.chkStart.Text = "설치 후 TiX 시작";
            this.chkStart.UseVisualStyleBackColor = true;
            // 
            // chkStartMenu
            // 
            this.chkStartMenu.AutoSize = true;
            this.chkStartMenu.Checked = true;
            this.chkStartMenu.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkStartMenu.Location = new System.Drawing.Point(12, 85);
            this.chkStartMenu.Name = "chkStartMenu";
            this.chkStartMenu.Size = new System.Drawing.Size(118, 19);
            this.chkStartMenu.TabIndex = 2;
            this.chkStartMenu.Text = "시작 메뉴에 추가";
            this.chkStartMenu.UseVisualStyleBackColor = true;
            // 
            // chkDesktop
            // 
            this.chkDesktop.AutoSize = true;
            this.chkDesktop.Checked = true;
            this.chkDesktop.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkDesktop.Location = new System.Drawing.Point(12, 110);
            this.chkDesktop.Name = "chkDesktop";
            this.chkDesktop.Size = new System.Drawing.Size(118, 19);
            this.chkDesktop.TabIndex = 3;
            this.chkDesktop.Text = "바탕 화면에 추가";
            this.chkDesktop.UseVisualStyleBackColor = true;
            // 
            // chkErrorReport
            // 
            this.chkErrorReport.AutoSize = true;
            this.chkErrorReport.Checked = true;
            this.chkErrorReport.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkErrorReport.Location = new System.Drawing.Point(12, 136);
            this.chkErrorReport.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.chkErrorReport.Name = "chkErrorReport";
            this.chkErrorReport.Size = new System.Drawing.Size(110, 19);
            this.chkErrorReport.TabIndex = 9;
            this.chkErrorReport.Text = "TiX 개선에 참여";
            this.chkErrorReport.UseVisualStyleBackColor = true;
            // 
            // frmInstall
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(161, 168);
            this.Controls.Add(this.chkErrorReport);
            this.Controls.Add(this.chkDesktop);
            this.Controls.Add(this.chkStartMenu);
            this.Controls.Add(this.chkStart);
            this.Controls.Add(this.btn);
            this.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.MaximizeBox = false;
            this.Name = "frmInstall";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "frmInstall";
            this.TopMost = true;
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btn;
        private System.Windows.Forms.CheckBox chkStart;
        private System.Windows.Forms.CheckBox chkStartMenu;
        private System.Windows.Forms.CheckBox chkDesktop;
        private System.Windows.Forms.CheckBox chkErrorReport;
    }
}