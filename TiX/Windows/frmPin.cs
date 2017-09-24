using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

using TiX.Core;
using TiX.Utilities;

namespace TiX.Windows
{
    internal partial class frmPin : Form
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

		private string m_token, m_secret;

		public frmPin(int wmMessage = 0)
		{
            this.m_wmMessage = wmMessage;

            InitializeComponent();
        
            this.Icon = TiX.Resources.TiX;
        }

        private new void Activate()
        {
            if (this.WindowState == FormWindowState.Minimized)
                this.WindowState = FormWindowState.Normal;

            var  topMost = this.TopMost;
            this.TopMost = true;
            this.TopMost = topMost;

            base.Activate();
            this.Focus();
        }

        private readonly int m_wmMessage;
        protected override void WndProc(ref Message m)
        {
            if (this.m_wmMessage != 0 && m.Msg == this.m_wmMessage)
            {
                this.Activate();
            }
            else if (m.Msg == 0x031D) // WM_CLIPBOARDUPDATE
            {
                var str = Clipboard.GetText();
                if (!string.IsNullOrWhiteSpace(str) && str.Length == 7 && int.TryParse(str, out int i))
                {
                    this.txtPin.Text = str;
                    
                    this.Activate();
                }
            }

            base.WndProc(ref m);
        }

        private void frmPin_Load(object sender, EventArgs e)
        {
            this.ajax.Left = this.ClientRectangle.Width  / 2 - 8;
            this.ajax.Top  = this.ClientRectangle.Height / 2 - 8;

			this.ajax.Start();

            GetRequestToken();
		}

        private void frmPin_FormClosing(object sender, FormClosingEventArgs e)
        {
            NativeMethods.RemoveClipboardFormatListener(this.Handle);
        }

        private async void GetRequestToken()
        {
            TiXMain.Twitter.UserToken  = null;
            TiXMain.Twitter.UserSecret = null;
            
            if (!await Task.Factory.StartNew<bool>(() => TiXMain.Twitter.RequestToken(out this.m_token, out this.m_secret)))
            {
                this.Error("문제가 발생했어요 :(");
                this.Close();
                return;
            }

            NativeMethods.AddClipboardFormatListener(this.Handle);

            var url = string.Format("\"https://api.twitter.com/oauth/authorize?oauth_token={0}\"", this.m_token);
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
                GetAccessToken(this.txtPin.Text);
                return;
            }

            if (e.KeyCode == Keys.Escape)
                this.Close();

            if (char.IsNumber((char)e.KeyValue) ||
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

		private async void GetAccessToken(string pin)
        {
            TiXMain.Twitter.UserToken  = this.m_token;
            TiXMain.Twitter.UserSecret = this.m_secret;

            bool success = false;
            try
            {
                if (await Task.Factory.StartNew<bool>(() => TiXMain.Twitter.AccessToken(pin, out this.m_token, out this.m_secret)))
                {
                    Settings.Instance.UToken  = this.m_token;
                    Settings.Instance.USecret = this.m_secret;

                    Settings.Instance.Save();
                    success = true;
                }
            }
            catch
            {
            }

            if (!success)
            {
                this.Error("문제가 발생했어요 :(");
                this.Close();
                return;
            }
        
            this.ajax.Stop();
            this.DialogResult = DialogResult.OK;
            this.Close();
		}
	}
}
