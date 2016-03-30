﻿using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using TiX.Utilities;

namespace TiX.Core
{
    public class ImageSet : IDisposable
    {
        public ImageSet()
        {
            this.RawStream = new MemoryStream(4 * 1024 * 1024);
        }

        public void Dispose()
        {
            if (this.Image      != null) this.Image.Dispose();
            if (this.GifFrames  != null) this.GifFrames.Dispose();
            if (this.Thumbnail  != null) this.Thumbnail.Dispose();
            if (this.RawStream  != null) this.RawStream.Dispose();

            GC.SuppressFinalize(this);
        }

        public Size Size
        {
            get
            {
                return this.GifFrames != null ? this.GifFrames.Size : this.Image.Size;
            }
        }
        public int Width { get { return this.Size.Width; } }
        public int Height { get { return this.Size.Height; } }

        public Image Image { get; set; }
        public GifFrames GifFrames { get; set; }
        public Image Thumbnail { get; set; }
        public Stream RawStream { get; set; }
        public string Extension { get; set; }
        public double Ratio { get; set; }

        public bool ResizeImage()
        {
            var szBefore = this.Image.Size;
            try
            {
                ImageResize.ResizeImage(this);
            }
            catch
            {
                return false;
            }
            var szAfter = this.Image.Size;

            this.Ratio = ((double)szAfter.Width * szAfter.Height) / (szBefore.Width * szBefore.Height) * 100d;

            // Thumbnail
            var ratio = Math.Min(64d / this.Image.Width, 64d / this.Image.Height);
            var newWidth  = (int)(this.Image.Width  * ratio);
            var newHeight = (int)(this.Image.Height * ratio);

            this.Thumbnail = new Bitmap(newWidth, newHeight);

            using (var g = Graphics.FromImage(this.Thumbnail))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                g.DrawImage(this.Image, new Rectangle(0, 0, newWidth, newHeight), new Rectangle(0, 0, this.Image.Width, this.Image.Height), GraphicsUnit.Pixel);
            }

            return true;
        }
    }
}
