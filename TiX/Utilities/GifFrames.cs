using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace TiX.Utilities
{
    public class GifFrame
    {
        public Bitmap Image { get; set; }
        public int Dalay { get; set; }
    }

    public class GifFrames : List<GifFrame>, IDisposable
    {        
        public GifFrames()
        { }
        public GifFrames(Image image)
        {
            int frames = image.GetFrameCount(FrameDimension.Time);
            if (frames <= 1) throw new Exception();

            byte[] times = image.GetPropertyItem(0x5100).Value;
            for (int i = 0; i < frames; ++i)
            {
                this.Add(new GifFrame { Image = new Bitmap(image), Dalay = BitConverter.ToInt32(times, 4 * i) * 10 });
                image.SelectActiveFrame(FrameDimension.Time, i);
            }
        }

        public Size Size { get { return this.Count > 0 ? this[0].Image.Size : Size.Empty; } }

        public void Dispose()
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

            this.Clear();

            GC.SuppressFinalize(this);
        }
    }
}
