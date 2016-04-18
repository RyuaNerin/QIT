using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Windows.Forms;
using TiX.Core;

namespace TiX.Windows
{
    public partial class frmSettings : Form
    {
        private static class NativeMethods
        {
            [DllImport("user32")]
            public static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

            internal const int BCM_FIRST = 0x1600; //Normal button
            internal const int BCM_SETSHIELD = (BCM_FIRST + 0x000C);
        }

        private bool m_loaded = false;

        public frmSettings()
        {
            InitializeComponent();
            this.Icon = TiX.Properties.Resources.TiX;
        }
        private void frmSettings_Load(object sender, EventArgs e)
        {
            this.TopMost = Settings.Topmost;

            this.chkTopMost.Checked         = Settings.Topmost;
            this.chkReversedCtrl.Checked    = Settings.ReversedCtrl;
            this.ctlUniformity.Checked      = Settings.UniformityText;
            this.chkEnableShell.Checked     = Settings.EnabledShell;

            this.m_loaded = true;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            Settings.Topmost        = this.chkTopMost.Checked;
            Settings.ReversedCtrl   = this.chkReversedCtrl.Checked;
            Settings.UniformityText = this.ctlUniformity.Checked;

            if (Settings.EnabledShell != this.chkEnableShell.Checked)
            {
                if (this.chkEnableShell.Checked)
                {
                    switch (ShellExtension.Install(Settings.Shells))
                    {
                    case ShellExtension.Result.NO_ERROR:
                        Settings.EnabledShell = true;
                        break;

                    case ShellExtension.Result.FAIL_REG:
                        MessageBox.Show(this, "DLL 을 등록하지 못했어요", TiXMain.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        break;

                    case ShellExtension.Result.DLL_CREATAION_FAIL:
                        MessageBox.Show(this, "DLL 파일을 만들지 못했어요", TiXMain.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        break;

                    case ShellExtension.Result.NOT_AUTHORIZED:
                        MessageBox.Show(this, "관리자 권한으로 실행해주세요!", TiXMain.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        break;

                    case ShellExtension.Result.UNKNOWN:
                        MessageBox.Show(this, "알 수 없는 문제가 발생했어요!", TiXMain.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        break;
                    }
                    Settings.Shells = null;
                }
                else
                {
                    switch (ShellExtension.Uninstall())
                    {
                    case ShellExtension.Result.FAIL_REG:
                        MessageBox.Show(this, "DLL 을 등록 해제하지 못했어요", TiXMain.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        break;

                    case ShellExtension.Result.DLL_NOT_EXITED:
                        MessageBox.Show(this, "DLL 파일이 없어요", TiXMain.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        break;

                    case ShellExtension.Result.NOT_AUTHORIZED:
                        MessageBox.Show(this, "관리자 권한으로 실행해주세요!", TiXMain.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        break;

                    case ShellExtension.Result.UNKNOWN:
                        MessageBox.Show(this, "알 수 없는 문제가 발생했어요!", TiXMain.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        break;
                    }
                    Settings.EnabledShell = false;
                }

                this.RestartExplorer();
            }

            Settings.Save();
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void lblCopyRight_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { UseShellExecute = true, FileName = "\"https://github.com/RyuaNerin/QIT\"" }).Dispose();
        }

        private static bool m_showShellMessage = true;
        private void chkEnableShell_CheckedChanged(object sender, EventArgs e)
        {
            if (!this.m_loaded) return;

            if (m_showShellMessage && this.chkEnableShell.Checked)
            {
                MessageBox.Show(this, "TiX를 삭제하기 전에 이 옵션을 체크 해제해주세요!", TiXMain.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                m_showShellMessage = false;
            }

            if (!TiXMain.IsAdministratorMode)
            {
                if (Settings.EnabledShell != this.chkEnableShell.Checked)
                    NativeMethods.SendMessage(this.btnOK.Handle, NativeMethods.BCM_SETSHIELD, IntPtr.Zero, new IntPtr(1));
                else
                    NativeMethods.SendMessage(this.btnOK.Handle, NativeMethods.BCM_SETSHIELD, IntPtr.Zero, new IntPtr(0));
            }
        }

        private void RestartExplorer()
        {
            if (MessageBox.Show(this, "적용을 위해 탐색기를 재시작 할까요?", TiXMain.ProductName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                //taskkill /IM explorer.exe /F & explorer.exe
                using (var proc = Process.Start(new ProcessStartInfo { Arguments = "/IM explorer.exe /F", FileName = "taskkill", WindowStyle = ProcessWindowStyle.Hidden, UseShellExecute = true }))
                    proc.WaitForExit();

                Process.Start("explorer.exe").Dispose();
            }
        }
    }
}
