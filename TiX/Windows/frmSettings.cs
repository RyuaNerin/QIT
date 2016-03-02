using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace TiX
{
    public partial class frmSettings : Form
    {
        private bool m_shellExists;

        public frmSettings()
        {
            InitializeComponent();
        }
        private void frmSettings_Load(object sender, EventArgs e)
        {
            this.m_shellExists = File.Exists("QuicxRegEditor.exe");
            this.chkEnableShell.Enabled = this.m_shellExists;

            this.TopMost = Settings.Topmost;
            this.chkTopMost.Checked = Settings.Topmost;
            this.chkReversedCtrl.Checked = Settings.ReversedCtrl;
            this.ctlUniformity.Checked = Settings.UniformityText;
            this.chkEnableShell.Checked = Settings.EnabledShell;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Settings.Topmost = this.chkTopMost.Checked;
            Settings.ReversedCtrl = this.chkReversedCtrl.Checked;
            Settings.UniformityText = this.ctlUniformity.Checked;

            if (this.m_shellExists && Settings.EnabledShell != this.chkEnableShell.Checked)
            {
                if (this.chkEnableShell.Checked)
                {
                    MessageBox.Show(this, "Quicx를 삭제하기 전에 본 옵션의 체크를 해제하십시오.", "경고", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    LaunchQuicxRegEditor(true);
                    Settings.EnabledShell = true;
                }
                else
                {
                    LaunchQuicxRegEditor(false);
                    Settings.EnabledShell = false;
                }
            }
            Settings.Save();
            this.Close();
        }

        public static void LaunchQuicxRegEditor(bool add)
        {
            Process p = new Process();
            p.StartInfo.FileName = "QuicxRegEditor.exe";
            p.StartInfo.Arguments = (add) ? "add" : "remove";
            p.Start();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void label1_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { UseShellExecute = true, FileName = "\"https://github.com/RyuaNerin/QIT\"" });
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Settings.UToken = Settings.USecret = string.Empty;
            Application.Exit();
        }

        private void btnStasisField_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
			Thread.Sleep( 200 );
			new TiX.ScreenCapture.Stasisfield().ShowDialog();
			this.WindowState = FormWindowState.Normal;
        }

    }
}
