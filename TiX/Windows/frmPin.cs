using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace TiX
{
	public partial class frmPin : Form
	{
		string t, s;

		public frmPin()
		{
			InitializeComponent();
		}

		private void frmPin_Load(object sender, EventArgs e)
		{
			this.ajax.Start();
			this.bgwBefore.RunWorkerAsync();

			this.ajax.Left = this.pnl.Width / 2 - 8;
			this.ajax.Top = this.pnl.Height / 2 - 8;
		}

		private void bgwBefore_DoWork(object sender, DoWorkEventArgs e)
		{
			Twitter.TwitterAPI11.OAuth.request_token(null, out this.t, out this.s);
		}

		private void bgwBefore_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			System.Diagnostics.Process.Start("explorer", String.Format("\"https://api.twitter.com/oauth/authorize?oauth_token={0}\"", this.t));

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
			Twitter.TwitterAPI11.OAuth.access_token(
				this.t,
				this.s,
				(string)e.Argument,
				out Settings.UToken,
				out Settings.USecret);

			Settings.Save();
		}

		private void bgwAfter_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			this.ajax.Stop();
			this.Close();
		}
	}
}
