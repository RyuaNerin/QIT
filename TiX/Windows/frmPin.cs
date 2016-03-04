using System;
using System.ComponentModel;
using System.Security.Permissions;
using System.Windows.Forms;
using TiX.Core;

namespace TiX.Windows
{
	public partial class frmPin : Form
	{
		string m_token, m_secret;

		public frmPin()
		{
			InitializeComponent();
		}

		private void frmPin_Load(object sender, EventArgs e)
        {
            this.ajax.Left = this.pnl.Width  / 2 - 8;
            this.ajax.Top  = this.pnl.Height / 2 - 8;

			this.ajax.Start();
			this.bgwBefore.RunWorkerAsync();
		}

        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x031D) // WM_CLIPBOARDUPDATE
            {
                var str = Clipboard.GetText();
                int i;
                if (str.Length == 7 && int.TryParse(str, out i))
                {
                    this.txtPin.Text = str;
                    this.Activate();
                }
            }

            base.WndProc(ref m);
        }

		private void bgwBefore_DoWork(object sender, DoWorkEventArgs e)
		{
            Program.Twitter.UserToken  = null;
            Program.Twitter.UserSecret = null;
            e.Result = Program.Twitter.RequestToken(out this.m_token, out this.m_secret);
		}

		private void bgwBefore_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
            if (e.Result == null || (bool)e.Result == false)
            {
                MessageBox.Show(this, "문제가 발생했어요 :(", Program.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
                return;
            }

			System.Diagnostics.Process.Start("explorer", String.Format("\"https://api.twitter.com/oauth/authorize?oauth_token={0}\"", this.m_token));

			this.ajax.Stop();
			this.txtPin.Enabled = true;
			this.txtPin.Focus();
		}

        private void txtPin_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                this.txtPin.Enabled = false;

                this.ajax.Start();
                this.bgwAfter.RunWorkerAsync(this.txtPin.Text);
            }

            if (e.KeyCode == Keys.Escape)
                this.Close();

            if (Char.IsNumber((char)e.KeyValue) ||
				(e.KeyCode == Keys.Back) ||
                (e.KeyCode == Keys.Delete))
                return;

            if (e.Modifiers == Keys.Control && e.KeyCode == Keys.C)
            {
                var str = Clipboard.GetText();
                int i;
                if (str.Length == 7 && int.TryParse(str, out i)) return;
            }

            e.Handled = true;
        }

		private void bgwAfter_DoWork(object sender, DoWorkEventArgs e)
        {
            if (e.Result == null || (bool)e.Result == false)
            {
                MessageBox.Show(this, "문제가 발생했어요 :(", Program.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
                return;
            }

            Program.Twitter.UserToken  = this.m_token;
            Program.Twitter.UserSecret = this.m_secret;
            if (Program.Twitter.AccessToken((string)e.Argument, out this.m_token, out this.m_secret))
            {
                Settings.UToken  = this.m_token;
                Settings.USecret = this.m_secret;

                Settings.Save();

                e.Result = true;
            }
            else
            {
                e.Result = false;
            }
		}

		private void bgwAfter_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			this.ajax.Stop();
            this.DialogResult = DialogResult.OK;
			this.Close();
		}
	}
}
