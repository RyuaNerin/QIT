using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using TiX.Core;

namespace TiX.Windows
{
	public partial class frmPin : Form
	{
        private static class NativeMethods
        {
            [DllImport("user32.dll", SetLastError=true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool AddClipboardFormatListener(IntPtr hwnd);

            [DllImport("user32.dll", SetLastError=true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool RemoveClipboardFormatListener(IntPtr hwnd);
        }

		string m_token, m_secret;

		public frmPin()
		{
            InitializeComponent();
            this.Icon = TiX.Properties.Resources.TiX;
		}

		private void frmPin_Load(object sender, EventArgs e)
        {
            this.ajax.Left = this.ClientRectangle.Width  / 2 - 8;
            this.ajax.Top  = this.ClientRectangle.Height / 2 - 8;

			this.ajax.Start();
			this.bgwBefore.RunWorkerAsync();
		}

        private void frmPin_FormClosing(object sender, FormClosingEventArgs e)
        {
            NativeMethods.RemoveClipboardFormatListener(this.Handle);
        }

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
            TiXMain.Twitter.UserToken  = null;
            TiXMain.Twitter.UserSecret = null;
            e.Result = TiXMain.Twitter.RequestToken(out this.m_token, out this.m_secret);
		}

		private void bgwBefore_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
            if (e.Result == null || (bool)e.Result == false)
            {
                MessageBox.Show(this, "문제가 발생했어요 :(", TiXMain.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
                return;
            }

            NativeMethods.AddClipboardFormatListener(this.Handle);

            var url = String.Format("\"https://api.twitter.com/oauth/authorize?oauth_token={0}\"", this.m_token);
			using (var proc = Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true }))
            { }

			this.ajax.Stop();
			this.txtPin.Enabled = true;
			this.txtPin.Focus();
		}

        private void txtPin_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                NativeMethods.RemoveClipboardFormatListener(this.Handle);
                
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
            TiXMain.Twitter.UserToken  = this.m_token;
            TiXMain.Twitter.UserSecret = this.m_secret;

            try
            {
                if (TiXMain.Twitter.AccessToken((string)e.Argument, out this.m_token, out this.m_secret))
                {
                    Settings.UToken  = this.m_token;
                    Settings.USecret = this.m_secret;

                    Settings.Save();

                    e.Result = true;
                }
            }
            catch
            {
            }
		}

		private void bgwAfter_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{

            if (e.Result == null || (bool)e.Result == false)
            {
                MessageBox.Show(this, "문제가 발생했어요 :(", TiXMain.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
                return;
            }

			this.ajax.Stop();
            this.DialogResult = DialogResult.OK;
			this.Close();
		}
	}
}
