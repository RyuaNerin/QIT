using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using TiX.Core;
using TiX.Utilities;

namespace TiX.Windows
{
    public partial class frmInstall : Form
    {
        public frmInstall(int wmMessage)
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
            this.btn.Enabled = false;
            
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TiX");
            Settings.Instance.LoadInDirectory(dir);

            using (var frm = new frmPin())
            {
                if (frm.ShowDialog(this) != DialogResult.OK)
                {
                    this.Error("트위터 인증을 진행해주세요!");
                    return;
                }
            }

            var option = OptionInstallation.InstallOrNone;
            if (this.chkDesktop.Checked    ) option |= OptionInstallation.ShoftcutInDesktop;
            if (this.chkStartMenu.Checked  ) option |= OptionInstallation.ShoftcutInStartMenu;
            if (this.chkErrorReport.Checked) option |= OptionInstallation.ReportError;

#if DEBUG
            option |= OptionInstallation.CreateLogFile;
#endif

            switch (Installer.Ap_Install(option))
            {
                case InstallerResult.UNKNOWN:
                case InstallerResult.FAIL_DLL_REGIST:
                case InstallerResult.DLL_CREATAION_FAIL:
                    this.Error("알 수 없는 문제가 발생했어요");
                    break;

                case InstallerResult.NOT_AUTHORIZED:
                    this.Error("관리자 권한으로 실행해주세요!");
                    break;

                case InstallerResult.FILE_USED:
                    this.Error("TiX 를 종료 후 재시도 해주세요!");
                    break;

                case InstallerResult.SUCCESS:
                    this.Infomation("TiX 를 설치했어요!");
                    
                    Settings.Instance.SaveInDirectory(dir);

                    if (this.chkStart.Checked)
                        Process.Start(Path.Combine(dir, "TiX.exe")).Dispose();

                    this.Close();
                    Application.Exit();
                    break;
            }

            this.btn.Enabled = true;
        }
    }
}