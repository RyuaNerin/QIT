using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using TiX.Core;
using Timer = System.Threading.Timer;

namespace TiX.Windows
{
	public partial class frmPreview : Form
	{
		public frmPreview(ImageSet imageSet)
		{
            this.m_imageSet = imageSet;
            this.m_imgSize = imageSet.Size;
            
            this.m_img = this.m_imageSet.Image;

            InitializeComponent();
            this.Icon = TiX.Properties.Resources.TiX;
            this.Text = String.Format("미리보기 ({0} x {1}) ({2})", this.m_imgSize.Width, this.m_imgSize.Height, Helper.ToCapString(this.m_imageSet.RawStream.Length));
            
			this.CalcMaxLocation();
			this.CheckPosition();

			this.Invalidate();
		}

        private void frmPreview_Load(object sender, EventArgs e)
        {
            if (this.m_imageSet.GifFrames != null)
                this.m_gifTimer = new Timer(GifTimer_Callback, null, this.m_imageSet.GifFrames[0].Dalay, Timeout.Infinite);
        }

        private void frmPreview_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (this.m_gifTimer != null)
            {
                this.m_gifTimer.Change(Timeout.Infinite, Timeout.Infinite);
                this.m_gifTimer.Dispose();
                this.m_gifTimer = null;
            }
        }

		//////////////////////////////////////////////////////////////////////////

        Timer m_gifTimer;
        int m_gifIndex = 0;

		ImageSet m_imageSet;
        Image m_img;
        Size m_imgSize;

		Point m_location = new Point(0, 0);
		Point m_locationMax;
        Rectangle m_client;

		bool m_viewOriginal = false;

        bool m_mouseDown = false;
        Point m_mousePoint;

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

			RawTrans(e.Graphics, e.ClipRectangle);

			if (this.m_client.Width >= this.m_img.Width && this.m_client.Height >= this.m_img.Height)
			{
				e.Graphics.DrawImageUnscaledAndClipped(
					this.m_img,
					new Rectangle(
						e.ClipRectangle.Width / 2 - this.m_imgSize.Width / 2,
						e.ClipRectangle.Height / 2 - this.m_imgSize.Height / 2,
						this.m_img.Width,
						this.m_img.Height));
			}
			else if (this.m_viewOriginal)
			{
                var rect = e.ClipRectangle;
                int x, y;

                if (this.m_img.Width < this.m_client.Width)
                {
                    rect.X = (rect.Width - this.m_imgSize.Width) / 2;
                    x = 0;
                }
                else
                    x = this.m_location.X;

                if (this.m_img.Height < this.m_client.Height)
                {
                    rect.Y = (rect.Height - this.m_imgSize.Height) / 2;
                    y = 0;
                }
                else
                    y = this.m_location.Y;

				e.Graphics.DrawImage(
					this.m_img,
					rect,
					new Rectangle(x, y, rect.Width, rect.Height),
					GraphicsUnit.Pixel
					);
			}
			else
			{
				e.Graphics.DrawImage(
					this.m_img,
					this.getRectangle(e.ClipRectangle),
					new Rectangle(0, 0, this.m_imgSize.Width, this.m_imgSize.Height),
					GraphicsUnit.Pixel
					);
			}
		}
		private Rectangle getRectangle(Rectangle e)
		{
			double scale, scaleX, scaleY;

			scaleX = (double)e.Width  / (double)this.m_imgSize.Width;
			scaleY = (double)e.Height / (double)this.m_imgSize.Height;

			scale = Math.Min(scaleX, scaleY);

			int w = (int)(this.m_imgSize.Width  * scale) + 1;
			int h = (int)(this.m_imgSize.Height * scale) + 1;
			int l = e.Width  / 2 - w / 2;
			int t = e.Height / 2 - h / 2;

			return new Rectangle(l, t, w, h);
		}
		private void RawTrans(Graphics g, Rectangle rec)
		{
			const int size = 24;

            int x, y;
			int xm = rec.Width  / size + 1;
			int ym = rec.Height / size + 1;

            // 배경을 쫙 깜
            g.FillRectangle(Brushes.Gainsboro, rec);
            
			for (y = 0; y < ym; ++y)
			{
				for (x = 0; x < xm; ++x)
				{
                    if ((y % 2 + x % 2) % 2 == 0) continue;

					g.FillRectangle(
                        Brushes.LightGray,
						rec.Left + x * size,
						rec.Top  + y * size,
						size,
						size);
				}
			}
		}

		//////////////////////////////////////////////////////////////////////////

		private void frmPreview_Resize(object sender, EventArgs e)
        {
            this.m_client = this.ClientRectangle;

			this.CalcMaxLocation();

            this.CheckPosition();

			this.Invalidate();
		}
		private void CalcMaxLocation()
		{
            this.m_locationMax.X = this.m_imgSize.Width  - this.m_client.Width;
            this.m_locationMax.Y = this.m_imgSize.Height - this.m_client.Height;
		}

		private void pic_MouseDoubleClick(object sender, MouseEventArgs e)
		{
            this.m_viewOriginal = !this.m_viewOriginal;
            this.CalcMaxLocation();
            this.CheckPosition();

			this.m_mousePoint = new Point(0, 0);
			this.Invalidate();
		}

		private void pic_MouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button != System.Windows.Forms.MouseButtons.Left)
				return;

			this.m_mouseDown = true;
			this.m_mousePoint = e.Location;
		}

		private void pic_MouseMove(object sender, MouseEventArgs e)
		{
			if (!this.m_mouseDown)
				return;

			this.m_location.X = this.m_location.X + (int)((this.m_mousePoint.X - e.X) * 1.0d * this.m_imgSize.Width  / this.m_client.Width);
			this.m_location.Y = this.m_location.Y + (int)((this.m_mousePoint.Y - e.Y) * 1.0d * this.m_imgSize.Height / this.m_client.Height);

            this.m_mousePoint = e.Location;

			this.CheckPosition();

			this.Invalidate();
		}

		private void pic_MouseUp(object sender, MouseEventArgs e)
		{
			this.m_mouseDown = false;

			this.Invalidate();
		}

		private void CheckPosition()
		{
			if (this.m_location.X < 0)
				this.m_location.X = 0;
			else if (this.m_location.X > this.m_locationMax.X)
				this.m_location.X = this.m_locationMax.X;

			if (this.m_location.Y < 0)
				this.m_location.Y = 0;
			else if (this.m_location.Y > this.m_locationMax.Y)
				this.m_location.Y = this.m_locationMax.Y;
		}

		private void frmPreview_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Escape)
				this.Close();

			if (e.KeyCode == Keys.Up)
				this.m_location.Y -= this.m_imgSize.Height / 20;

			if (e.KeyCode == Keys.Down)
				this.m_location.Y += this.m_imgSize.Height / 20;

			if (e.KeyCode == Keys.Left)
				this.m_location.X -= this.m_imgSize.Width / 20;

			if (e.KeyCode == Keys.Right)
				this.m_location.X += this.m_imgSize.Width / 20;

			this.CheckPosition();

			this.Invalidate();
		}

        private void GifTimer_Callback(object state)
        {
            this.m_gifIndex = (this.m_gifIndex + 1) % this.m_imageSet.GifFrames.Count;
            this.m_img = this.m_imageSet.GifFrames[this.m_gifIndex].Image;

            try
            {
            	this.BeginInvoke(new Action(this.Invalidate));
            }
            catch
            { }

            this.m_gifTimer.Change(this.m_imageSet.GifFrames[this.m_gifIndex].Dalay, Timeout.Infinite);
        }
	}
}
