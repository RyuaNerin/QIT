using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace QIT
{
	internal class AjaxControl16 : Control
	{
		public bool isAjax32 { get; set; }

		public AjaxControl16()
		{
			this.DoubleBuffered = true;

			this.Size = new Size(16, 16);
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			lock (this._sync)
				this.bAjax = false;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			e.Graphics.Clear(this.BackColor);
			e.Graphics.DrawImage(
				Properties.Resources.loading,
				new Rectangle(0, 0, 16, 16),
				new Rectangle(0, 16 * this.iAjax, 16, 16),
				GraphicsUnit.Pixel
				);
		}

		private object _sync = new object();
		private bool bAjax;

		private int iAjax;

		private Thread thd;

		public void Start()
		{
			if (!this.bAjax && ((this.thd == null) || !this.thd.IsAlive))
			{
				this.bAjax = true;

				this.Visible = true;

				this.thd = new Thread(Threadp);
				this.thd.Start();
			}
		}

		public void Stop()
		{
			lock (this._sync)
				this.bAjax = false;

			this.Visible = false;
		}

		private void Threadp()
		{
			bool b;
			while (true)
			{
				lock (this._sync)
					b = this.bAjax;

				if (!b)
					break;

				this.iAjax++;

				if (this.iAjax >= 24)
					this.iAjax = 0;

				this.Invalidate();

				Thread.Sleep(50);
			}
		}
	}
}
