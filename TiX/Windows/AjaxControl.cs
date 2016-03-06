using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using Timer = System.Threading.Timer;

namespace TiX.Windows
{
    [System.ComponentModel.DesignerCategory("CODE")]
    internal class AjaxControl : Control
    {
        private int _size = 16;
        public bool Is16
        {
            get
            {
                return (this._size == 16);
            }
            set
            {
                this._size = (value ? 16 : 32);

                this.Width = this.Height = this._size;
                this.m_image = this._size == 16 ? Properties.Resources.loading : Properties.Resources.loading32;
            }
        }

        public AjaxControl()
        {
            this.DoubleBuffered = true;

            this.m_timer = new Timer(this.Callback, null, Timeout.Infinite, Timeout.Infinite);
        }

        protected override void Dispose(bool disposing)
        {
            this.m_timer.Dispose();

            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.Clear(this.BackColor);
            e.Graphics.DrawImage(
                this.m_image,
                new Rectangle(0, 0, this._size, this._size),
                new Rectangle(0, this._size * this.m_index, this._size, this._size),
                GraphicsUnit.Pixel
                );
        }

        private Image   m_image;
        private Timer   m_timer;
        private int     m_index;
        
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
            this.m_index = (this.m_index + 1) % 24;

            try
            {
                this.Invoke(new Action(() => this.Invalidate()));
            }
            catch
            {
            }
        }
    }
}
