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
	public partial class frmPreview : Form
	{
		static frmPreview()
		{
			frmPreview.s = new Size(160, 160);
		}
		static Size s;

		public frmPreview()
		{
			InitializeComponent();

			this.ClientSize = frmPreview.s;

			this.SetLocationMax();
		}

		public void SetImage(Image img)
		{
			this._img = img;
			this._imageSize = img.Size;

			this.Text = String.Format("이미지 미리보기 ({0} x {1})", this._imageSize.Width, this._imageSize.Height);

			this.CheckPosition();

			this.Invalidate();
		}

		Image	_img;

		Size	_imageSize;
		Point	_location = new Point(0, 0);
		Point	_locationMax;

		bool	_viewOriginal = false;

		protected override void OnPaint(PaintEventArgs e)
		{
			if (this._viewOriginal || (this.Width >= this._img.Width && this.Height >= this._img.Height))
				e.Graphics.DrawImage(
					this._img,
					e.ClipRectangle,
					new Rectangle(this._location.X, this._location.Y, this.Width, this.Height),
					GraphicsUnit.Pixel
					);

			else
				e.Graphics.DrawImage(
					this._img,
					this.getRectangle(e.ClipRectangle),
					new Rectangle(0, 0, this._img.Width, this._img.Height),
					GraphicsUnit.Pixel
					);
		}
		public Rectangle getRectangle(Rectangle e)
		{
			double scale, scaleX, scaleY;

			scaleX = (double)e.Width / (double)this._imageSize.Width;
			scaleY = (double)e.Height / (double)this._imageSize.Height;

			scale = Math.Min(scaleX, scaleY);

			int w = (int)(this._imageSize.Width * scale) + 1;
			int h = (int)(this._imageSize.Height * scale) + 1;
			int l = e.Width / 2 - w / 2;
			int t = e.Height / 2 - h / 2;

			return new Rectangle(l, t, w, h);
		}

		protected override void OnMouseDoubleClick(MouseEventArgs e)
		{
			this._viewOriginal = !this._viewOriginal;

			this._location = new Point(0, 0);
			this.Invalidate();
		}

		//////////////////////////////////////////////////////////////////////////

		bool	_isDown = false;
		int		_mousePointX;
		int		_mousePointY;
		protected override void OnResize(EventArgs e)
		{
			frmPreview.s = this.ClientSize;

			this.SetLocationMax();

			this.Invalidate();
		}
		private void SetLocationMax()
		{
			this._locationMax.X = this._imageSize.Width - this.Width;
			this._locationMax.Y = this._imageSize.Height - this.Height;
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			if (e.Button != System.Windows.Forms.MouseButtons.Left)
				return;

			this._isDown = true;
			this._mousePointX = e.X;
			this._mousePointY = e.Y;
		}
		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (!this._isDown)
				return;

			this._location.X = this._location.X + (this._mousePointX - e.X);
			this._location.Y = this._location.Y + (this._mousePointY - e.Y);

			Console.WriteLine(this._location.ToString());

			this._mousePointX = e.X;
			this._mousePointY = e.Y;

			this.CheckPosition();

			this.Invalidate();
		}
		protected override void OnMouseUp(MouseEventArgs e)
		{
			this._isDown = false;
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
		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (e.KeyData == Keys.Escape)
				this.Close();

			if (e.KeyCode == Keys.Up)
				this._location.Y -= this._imageSize.Height / 20;

			if (e.KeyCode == Keys.Down)
				this._location.Y += this._imageSize.Height / 20;

			if (e.KeyCode == Keys.Left)
				this._location.X -= this._imageSize.Width / 20;

			if (e.KeyCode == Keys.Right)
				this._location.X += this._imageSize.Width / 20;

			this.CheckPosition();

			this.Invalidate();
		}
	}
}
