using System;
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

        private bool    m_admin;

        public frmSettings()
        {
            InitializeComponent();
            this.Icon = TiX.Properties.Resources.TiX;

            this.m_admin = IsAdministrator();
        }
        private void frmSettings_Load(object sender, EventArgs e)
        {
            this.TopMost = Settings.Topmost;

            this.chkTopMost.Checked         = Settings.Topmost;
            this.chkReversedCtrl.Checked    = Settings.ReversedCtrl;
            this.ctlUniformity.Checked      = Settings.UniformityText;
            this.chkEnableShell.Checked     = Settings.EnabledShell;
        }

        public static bool IsAdministrator()
        {
            try
            {
                using (var identity = WindowsIdentity.GetCurrent())
                {
                    if (identity == null) return false;

                    var principal = new WindowsPrincipal(identity);
                    return principal.IsInRole(WindowsBuiltInRole.Administrator);
                }
            }
            catch
            {
                return false;
            }
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
                    MessageBox.Show(this, "TiX를 삭제하기 전에 본 옵션의 체크를 해제하십시오.", "경고", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    ShellExtension.Install(this.m_admin);
                }
                else
                {
                    ShellExtension.Uninstall(this.m_admin);
                }
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
            using (System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { UseShellExecute = true, FileName = "\"https://github.com/RyuaNerin/QIT\"" }))
            { }
        }

        private void chkEnableShell_CheckedChanged(object sender, EventArgs e)
        {
            if (!this.m_admin)
            {
                if (Settings.EnabledShell != this.chkEnableShell.Checked)
                    NativeMethods.SendMessage(this.btnOK.Handle, NativeMethods.BCM_SETSHIELD, IntPtr.Zero, new IntPtr(1));
                else
                    NativeMethods.SendMessage(this.btnOK.Handle, NativeMethods.BCM_SETSHIELD, IntPtr.Zero, new IntPtr(0));
            }
        }
    }
}
