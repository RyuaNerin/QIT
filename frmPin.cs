using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace QIT
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

		private void txtPin_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar == '\x0D' || e.KeyChar == '\x0A')
			{
				this.txtPin.Enabled = false;

				this.ajax.Start();
				this.bgwAfter.RunWorkerAsync(this.txtPin.Text);
			}

			if (e.KeyChar == (char)Keys.Escape)
				this.Close();

			if (!Char.IsNumber(e.KeyChar) &&
				(e.KeyChar != (char)Keys.Back) &&
				(e.KeyChar != (char)Keys.Delete))
				e.Handled = true;
		}

		private void bgwAfter_DoWork(object sender, DoWorkEventArgs e)
		{
			Twitter.TwitterAPI11.OAuth.access_token(
				this.t,
				this.s,
				(string)e.Argument,
				out Program.UToken,
				out Program.USecret);
		}

		private void bgwAfter_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			this.ajax.Stop();
			this.Close();
		}
	}
}
