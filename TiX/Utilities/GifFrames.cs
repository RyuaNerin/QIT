using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace TiX.Utilities
{
    internal class GifFrame
    {
        public GifFrame(Image img, int delay)
        {
            this.Image = img;
            this.Dalay = delay;
        }
        public Image Image { get; }
        public int Dalay { get; set; }
    }

    internal class GifFrames : List<GifFrame>, IDisposable
    {
        public const int DefaultDelay = 80;

        public GifFrames()
        {
        }
        public GifFrames(Image image)
        {
            int frames = image.GetFrameCount(FrameDimension.Time);
            if (frames < 1) throw new NotSupportedException();

            byte[] times = image.GetPropertyItem(0x5100).Value;
            int delay;

            try
            {
                for (int i = 0; i < frames; ++i)
                {
                    delay = BitConverter.ToInt32(times, 4 * i) * 10;
                    if (delay == 0) delay = DefaultDelay;

                    image.SelectActiveFrame(FrameDimension.Time, i);
                    this.Add(new GifFrame(new Bitmap(image), delay));
                }
            }
            catch (Exception ex)
            {
                foreach (var gf in this)
                {
                    gf.Image.Dispose();
                }
                throw ex;
            }
        }

        public Size Size => this.Count > 0 ? this[0].Image.Size : Size.Empty;

        ~GifFrames()
        {
            this.Dispose(false);
        }
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
        private bool m_disposed = false;
        private void Dispose(bool disposing)
        {
            if (this.m_disposed) return;
            this.m_disposed = true;

            if (disposing)
            {
                for (int i = 0; i < this.Count; ++i)
                {
                    try
                    {
                        this[i].Image.Dispose();
                    }
                    catch
                    { }
                }
            }
        }
    }
}
