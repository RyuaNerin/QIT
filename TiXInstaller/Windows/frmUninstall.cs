using System;
using System.Windows.Forms;
using TiX.Utilities;

namespace TiX.Windows
{
    public partial class frmUninstall : Form
    {
        public frmUninstall(int wmMessage)
        {
            this.m_wmMessage = wmMessage;

            InitializeComponent();

            this.Text = TiXMain.ProductName;
            this.Icon = TiX.Resources.TiX;

            UacIcon.SetUacIcon(TiXMain.IsAdministratorMode, this.btn, true);
        }

        private readonly int m_wmMessage;
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == this.m_wmMessage)
            {
                if (this.WindowState == FormWindowState.Minimized)
                    this.WindowState = FormWindowState.Normal;

                var topMost = this.TopMost;
                this.TopMost = true;
                this.TopMost = topMost;

                this.Activate();
                this.Focus();
            }

            base.WndProc(ref m);
        }

        private void btn_Click(object sender, EventArgs e)
        {
            switch (Installer.Ap_Uninstall())
            {
                case InstallerResult.UNKNOWN:
                    this.Error("알 수 없는 문제가 발생했어요");
                    break;

                case InstallerResult.SUCCESS:
                    this.Close();
                    Application.Exit();
                    break;
            }

            this.btn.Enabled = true;
        }
    }
}