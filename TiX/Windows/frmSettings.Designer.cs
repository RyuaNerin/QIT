namespace TiX
{
    partial class frmSettings
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmSettings));
			this.chkTopMost = new System.Windows.Forms.CheckBox();
			this.chkReversedCtrl = new System.Windows.Forms.CheckBox();
			this.ctlUniformity = new System.Windows.Forms.CheckBox();
			this.button1 = new System.Windows.Forms.Button();
			this.button2 = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.button3 = new System.Windows.Forms.Button();
			this.chkEnableShell = new System.Windows.Forms.CheckBox();
			this.btnStasisField = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// chkTopMost
			// 
			this.chkTopMost.AutoSize = true;
			this.chkTopMost.Location = new System.Drawing.Point(13, 13);
			this.chkTopMost.Name = "chkTopMost";
			this.chkTopMost.Size = new System.Drawing.Size(125, 16);
			this.chkTopMost.TabIndex = 0;
			this.chkTopMost.Text = "Quicx를 항상 위로";
			this.chkTopMost.UseVisualStyleBackColor = true;
			// 
			// chkReversedCtrl
			// 
			this.chkReversedCtrl.AutoSize = true;
			this.chkReversedCtrl.Location = new System.Drawing.Point(12, 35);
			this.chkReversedCtrl.Name = "chkReversedCtrl";
			this.chkReversedCtrl.Size = new System.Drawing.Size(231, 16);
			this.chkReversedCtrl.TabIndex = 0;
			this.chkReversedCtrl.Text = "Ctrl키를 누른 채 드래그해야 본문 입력";
			this.chkReversedCtrl.UseVisualStyleBackColor = true;
			// 
			// ctlUniformity
			// 
			this.ctlUniformity.AutoSize = true;
			this.ctlUniformity.Location = new System.Drawing.Point(13, 57);
			this.ctlUniformity.Name = "ctlUniformity";
			this.ctlUniformity.Size = new System.Drawing.Size(252, 16);
			this.ctlUniformity.TabIndex = 0;
			this.ctlUniformity.Text = "한번에 여러 사진을 올리는 경우 내용 통일";
			this.ctlUniformity.UseVisualStyleBackColor = true;
			// 
			// button1
			// 
			this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.button1.Location = new System.Drawing.Point(202, 107);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(75, 23);
			this.button1.TabIndex = 1;
			this.button1.Text = "취소";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Click += new System.EventHandler(this.button1_Click);
			// 
			// button2
			// 
			this.button2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.button2.Location = new System.Drawing.Point(121, 107);
			this.button2.Name = "button2";
			this.button2.Size = new System.Drawing.Size(75, 23);
			this.button2.TabIndex = 1;
			this.button2.Text = "확인";
			this.button2.UseVisualStyleBackColor = true;
			this.button2.Click += new System.EventHandler(this.button2_Click);
			// 
			// label1
			// 
			this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label1.Location = new System.Drawing.Point(12, 99);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(103, 34);
			this.label1.TabIndex = 3;
			this.label1.Text = "RyuaNerin\r\nSasarino MARi";
			this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			this.label1.Click += new System.EventHandler(this.label1_Click);
			// 
			// button3
			// 
			this.button3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.button3.Location = new System.Drawing.Point(202, 78);
			this.button3.Name = "button3";
			this.button3.Size = new System.Drawing.Size(75, 23);
			this.button3.TabIndex = 4;
			this.button3.Text = "로그아웃";
			this.button3.UseVisualStyleBackColor = true;
			this.button3.Click += new System.EventHandler(this.button3_Click);
			// 
			// chkEnableShell
			// 
			this.chkEnableShell.AutoSize = true;
			this.chkEnableShell.Location = new System.Drawing.Point(13, 79);
			this.chkEnableShell.Name = "chkEnableShell";
			this.chkEnableShell.Size = new System.Drawing.Size(104, 16);
			this.chkEnableShell.TabIndex = 0;
			this.chkEnableShell.Text = "윈도우 쉘 확장";
			this.chkEnableShell.UseVisualStyleBackColor = true;
			// 
			// btnStasisField
			// 
			this.btnStasisField.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnStasisField.Location = new System.Drawing.Point(121, 78);
			this.btnStasisField.Name = "btnStasisField";
			this.btnStasisField.Size = new System.Drawing.Size(75, 23);
			this.btnStasisField.TabIndex = 4;
			this.btnStasisField.Text = "화면 캡처";
			this.btnStasisField.UseVisualStyleBackColor = true;
			this.btnStasisField.Click += new System.EventHandler(this.btnStasisField_Click);
			// 
			// frmSettings
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(289, 142);
			this.Controls.Add(this.btnStasisField);
			this.Controls.Add(this.button3);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.button2);
			this.Controls.Add(this.button1);
			this.Controls.Add(this.chkEnableShell);
			this.Controls.Add(this.ctlUniformity);
			this.Controls.Add(this.chkReversedCtrl);
			this.Controls.Add(this.chkTopMost);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.Name = "frmSettings";
			this.ShowIcon = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "설정";
			this.Load += new System.EventHandler(this.frmSettings_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox chkTopMost;
        private System.Windows.Forms.CheckBox chkReversedCtrl;
        private System.Windows.Forms.CheckBox ctlUniformity;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.CheckBox chkEnableShell;
        private System.Windows.Forms.Button btnStasisField;
    }
}