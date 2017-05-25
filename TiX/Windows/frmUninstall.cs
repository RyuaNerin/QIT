using System;
using System.Windows.Forms;
using TiX.Utilities;

namespace TiX.Windows
{
    public partial class frmUninstall : Form
    {
        public frmUninstall()
        {
            InitializeComponent();

            this.Text = TiXMain.ProductName;

            UacIcon.SetUacIcon(TiXMain.IsAdministratorMode, this.btn, true);
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