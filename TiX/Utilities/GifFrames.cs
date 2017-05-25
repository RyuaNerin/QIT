using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace TiX.Utilities
{
    public class GifFrame
    {
        public Bitmap Image { get; internal set; }
        public int Dalay { get; internal set; }
    }

    public class GifFrames : List<GifFrame>, IDisposable
    {
        public const int DefaultDelay = 80;

        internal GifFrames()
        { }
        internal GifFrames(Image image)
        {
            int frames = image.GetFrameCount(FrameDimension.Time);
            if (frames < 1) throw new NotSupportedException();

            byte[] times = image.GetPropertyItem(0x5100).Value;
            int delay;
            for (int i = 0; i < frames; ++i)
            {
                delay = BitConverter.ToInt32(times, 4 * i) * 10;
                if (delay == 0) delay = DefaultDelay;

                image.SelectActiveFrame(FrameDimension.Time, i);
                this.Add(new GifFrame { Image = CopyImage(image), Dalay = delay });
            }
        }

        public Size Size { get { return this.Count > 0 ? this[0].Image.Size : Size.Empty; } }

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
            if (this.m_disposed)
                return;
            this.m_disposed = true;

            for (int i = 0; i < this.Count; ++i)
            {
                try
                {
                    this[i].Image.Dispose();
                }
                catch
                { }
            }

            this.Clear();
        }
        
        private static Bitmap CopyImage(Image orig)
        {
            var bitmap = new Bitmap(orig.Width, orig.Height, PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(bitmap))
                g.DrawImageUnscaledAndClipped(orig, new Rectangle(Point.Empty, orig.Size));

            return bitmap;
        }
    }
}
