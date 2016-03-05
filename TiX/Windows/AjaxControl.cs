using System.Drawing;
using System.Threading;
using System.Windows.Forms;

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
            }
        }

        public AjaxControl()
        {
            this.DoubleBuffered = true;
        }

        protected override void Dispose(bool disposing)
        {
            lock (this._sync)
                this.bAjax = false;

            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.Clear(this.BackColor);
            e.Graphics.DrawImage(
                (this._size == 16 ? Properties.Resources.loading : Properties.Resources.loading32),
                new Rectangle(0, 0, this._size, this._size),
                new Rectangle(0, this._size * this.iAjax, this._size, this._size),
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
            while (this.bAjax && !this.Disposing && !this.IsDisposed)
            {
                this.iAjax = (this.iAjax + 1) % 24;

                try
                {
                    this.Invalidate();                	
                }
                catch
                {
                }

                Thread.Sleep(50);
            }
        }
    }
}
