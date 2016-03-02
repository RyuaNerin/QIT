using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

namespace Quicx
{
    public partial class frmSettings : Form
    {
        public frmSettings()
        {
            InitializeComponent();
        }
        private void frmSettings_Load(object sender, EventArgs e)
        {
            this.TopMost = Settings.isTopmost;
            this.checkBox1.Checked = Settings.isTopmost;
            this.checkBox2.Checked = Settings.isReversedCtrl;
            this.checkBox3.Checked = Settings.isUniformityText;
            this.checkBox5.Checked = Settings.isEnabledShell;
        }


        private void button2_Click(object sender, EventArgs e)
        {
            Settings.isTopmost = this.checkBox1.Checked;
            Settings.isReversedCtrl = this.checkBox2.Checked;
            Settings.isUniformityText = this.checkBox3.Checked;
            if (Settings.isEnabledShell != this.checkBox5.Checked)
            {
                if (this.checkBox5.Checked)
                {
                    MessageBox.Show("Quicx를 삭제하기 전에 본 옵션의 체크를 해제하십시오.", "경고");
                    LaunchQuicxRegEditor(true);
                    Settings.isEnabledShell = true;
                }
                else
                {
                    LaunchQuicxRegEditor(false);
                    Settings.isEnabledShell = false;
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
			new Quicx.ScreenCapture.Stasisfield().ShowDialog();
        }

    }
}
