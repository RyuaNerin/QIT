using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using Timer = System.Threading.Timer;

namespace TiX.Windows
{
    [System.ComponentModel.DesignerCategory("CODE")]
    internal class ProgressCircle : Control
    {
        public ProgressCircle()
        {
            this.DoubleBuffered = true;
            this.SmallSize = false;

            this.m_timer = new Timer(this.Callback, null, Timeout.Infinite, Timeout.Infinite);
        }

        private Rectangle   m_rectCircle;
        private RectangleF  m_rectText;
        private Timer       m_timer;
        private Image       m_image;
        private int         m_imageIndex;

        private void CalcRect()
        {
            this.m_rectCircle = new Rectangle(
                (this.Width  - this.m_size) / 2,
                (this.Height - this.m_size) / 2,
                this.m_size,
                this.m_size);

            this.m_rectText   = new RectangleF(
                0,
                0,
                this.Width,
                this.Height);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            this.CalcRect();
        }

        protected override void Dispose(bool disposing)
        {
            this.m_timer.Dispose();

            base.Dispose(disposing);
        }

        private int m_size = 16;
        public bool SmallSize
        {
            get
            {
                return (this.m_size == 16);
            }
            set
            {
                this.m_size = (value ? 16 : 32);
                this.m_image = value ? Resources.loading : Resources.loading32;

                this.CalcRect();
            }
        }

        private int m_value = -1;
        public int Value
        {
            get => this.m_value;
            set
            {
                this.m_value = value;

                this.Invalidate();
            }
        }

        private static readonly StringFormat SfCenter = new StringFormat
        {
            Alignment     = StringAlignment.Center,
            FormatFlags   = StringFormatFlags.NoClip | StringFormatFlags.NoWrap,
            LineAlignment = StringAlignment.Center,
            Trimming      = StringTrimming.None
        };
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.Clear(this.BackColor);
            e.Graphics.DrawImage(
                this.m_image,
                this.m_rectCircle,
                new Rectangle(0, this.m_size * this.m_imageIndex, this.m_size, this.m_size),
                GraphicsUnit.Pixel
                );

            if (this.m_value != -1)
            {
                e.Graphics.DrawString(
                    this.m_value.ToString(),
                    this.Font,
                    Brushes.Black,
                    this.m_rectText,
                    ProgressCircle.SfCenter);
            }
        }
        
        public void Start()
        {
            this.m_timer.Change(0, 50);
            this.Visible = true;
        }

        public void Stop()
        {
            this.m_timer.Change(Timeout.Infinite, Timeout.Infinite);
            this.Visible = false;
        }

        private void Callback(object state)
        {
            this.m_imageIndex = (this.m_imageIndex + 1) % 24;

            try
            {
                this.Invoke(new Action(this.Invalidate));
            }
            catch
            {
            }
        }
    }
}
