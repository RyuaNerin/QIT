﻿using System;
using System.Drawing;
using System.Windows.Forms;

namespace TiX.Windows
{
	public partial class frmPreview : Form
	{
		static Size s = new Size(250, 250);

		//////////////////////////////////////////////////////////////////////////

		public frmPreview(Image img)
		{
			this._img = img;

			InitializeComponent();
            this.Text = String.Format("미리보기 ({0} x {1})", img.Width, img.Height);

			this.ClientSize = frmPreview.s;
            
			this.SetLocationMax();
			this.CheckPosition();

			this.Invalidate();
		}

		private void frmPreview_FormClosed(object sender, FormClosedEventArgs e)
		{
			frmPreview.s = this.ClientSize;
		}

		//////////////////////////////////////////////////////////////////////////

		Image	_img;
		Point	_location = new Point(0, 0);
		Point	_locationMax;
        Rectangle m_client;

		bool	_viewOriginal = false;

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

			RawTrans(e.Graphics, e.ClipRectangle);

			if (this.m_client.Width >= this._img.Width && this.m_client.Height >= this._img.Height)
			{
				e.Graphics.DrawImageUnscaledAndClipped(
					this._img,
					new Rectangle(
						e.ClipRectangle.Width / 2 - this._img.Width / 2,
						e.ClipRectangle.Height / 2 - this._img.Height / 2,
						this._img.Width,
						this._img.Height));
			}
			else if (this._viewOriginal)
			{
                var rect = e.ClipRectangle;
                int x, y;

                if (this._img.Width < this.m_client.Width)
                {
                    rect.X = (rect.Width - this._img.Width) / 2;
                    x = 0;
                }
                else
                    x = this._location.X;

                if (this._img.Height < this.m_client.Height)
                {
                    rect.Y = (rect.Height - this._img.Height) / 2;
                    y = 0;
                }
                else
                    y = this._location.Y;

				e.Graphics.DrawImage(
					this._img,
					rect,
					new Rectangle(x, y, rect.Width, rect.Height),
					GraphicsUnit.Pixel
					);
			}
			else
			{
				e.Graphics.DrawImage(
					this._img,
					this.getRectangle(e.ClipRectangle),
					new Rectangle(0, 0, this._img.Width, this._img.Height),
					GraphicsUnit.Pixel
					);
			}
		}
		private Rectangle getRectangle(Rectangle e)
		{
			double scale, scaleX, scaleY;

			scaleX = (double)e.Width / (double)this._img.Width;
			scaleY = (double)e.Height / (double)this._img.Height;

			scale = Math.Min(scaleX, scaleY);

			int w = (int)(this._img.Width * scale) + 1;
			int h = (int)(this._img.Height * scale) + 1;
			int l = e.Width / 2 - w / 2;
			int t = e.Height / 2 - h / 2;

			return new Rectangle(l, t, w, h);
		}
		private void RawTrans(Graphics g, Rectangle rec)
		{
			const int size = 24;

			int xm = (int)Math.Ceiling(rec.Width * 1.0d / size);
			int ym = (int)Math.Ceiling(rec.Height * 1.0d / size);

			int w, h;

            // 배경을 쫙 깜
            g.FillRectangle(Brushes.White, rec);

			for (int y = 0; y < ym; ++y)
			{
				for (int x = 0; x < xm; ++x)
				{
                    if ((y % 2 + x % 2) % 2 == 0) continue;

					w = rec.Left + x * size;
					if (w + size >= rec.Width) w = rec.Width - w;
					else w = size;

					h = rec.Top + y * size;
					if (h + size >= rec.Height) h = rec.Height - h;
					else h = size;

					g.FillRectangle(
                        Brushes.Gainsboro,
						rec.Left + x * size,
						rec.Top + y * size,
						w,
						h);
				}
			}
		}

		//////////////////////////////////////////////////////////////////////////

		bool	_isDown = false;
		int		_mousePointX;
		int		_mousePointY;
		private void frmPreview_Resize(object sender, EventArgs e)
		{
			this.SetLocationMax();

            this.CheckPosition();

			this.Invalidate();

            this.m_client = this.ClientRectangle;
		}
		private void SetLocationMax()
		{
            this._locationMax.X = this._img.Width - this.m_client.Width;
            this._locationMax.Y = this._img.Height - this.m_client.Height;
		}

		private void pic_MouseDoubleClick(object sender, MouseEventArgs e)
		{
            this._viewOriginal = !this._viewOriginal;
            this.SetLocationMax();
            this.CheckPosition();

			this._location = new Point(0, 0);
			this.Invalidate();
		}

		private void pic_MouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button != System.Windows.Forms.MouseButtons.Left)
				return;

			this._isDown = true;
			this._mousePointX = e.X;
			this._mousePointY = e.Y;
		}

		private void pic_MouseMove(object sender, MouseEventArgs e)
		{
			if (!this._isDown)
				return;

			this._location.X = this._location.X + (int)((this._mousePointX - e.X) * 1.0d * this._img.Width  / this.m_client.Width);
			this._location.Y = this._location.Y + (int)((this._mousePointY - e.Y) * 1.0d * this._img.Height / this.m_client.Height);

			this._mousePointX = e.X;
			this._mousePointY = e.Y;

			this.CheckPosition();

			this.Invalidate();
		}

		private void pic_MouseUp(object sender, MouseEventArgs e)
		{
			this._isDown = false;

			this.Invalidate();
		}

		private void CheckPosition()
		{
			if (this._location.X < 0)
				this._location.X = 0;
			else if (this._location.X > this._locationMax.X)
				this._location.X = this._locationMax.X;

			if (this._location.Y < 0)
				this._location.Y = 0;
			else if (this._location.Y > this._locationMax.Y)
				this._location.Y = this._locationMax.Y;
		}

		private void frmPreview_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Escape)
				this.Close();

			if (e.KeyCode == Keys.Up)
				this._location.Y -= this._img.Height / 20;

			if (e.KeyCode == Keys.Down)
				this._location.Y += this._img.Height / 20;

			if (e.KeyCode == Keys.Left)
				this._location.X -= this._img.Width / 20;

			if (e.KeyCode == Keys.Right)
				this._location.X += this._img.Width / 20;

			this.CheckPosition();

			this.Invalidate();
		}
	}
}
