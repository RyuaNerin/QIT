using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace TiX.Utilities
{
    public static class ImageResize
    {
        private const int MaxSize = 2936012; // 2.8 MiB
        
        public static void ResizeImage(ref Image image, ref byte[] rawData, ref double ratio, ref string extension)
        {
            long oldSize, newSize;

            using (var buffer = new MemoryStream(MaxSize))
            {
                Bitmap              bitmap;
                ImageCodecInfo		codec;
                EncoderParameters	param;

                if (!(image is Bitmap))
                {
                    var newImage = new Bitmap(image.Width, image.Height, image.PixelFormat);
                    using (var g = Graphics.FromImage(newImage))
                        g.DrawImage(newImage, 0, 0, newImage.Width, newImage.Height);

                    image.Dispose();
                    image = newImage;
                }

                oldSize = image.Width * image.Height;

                bitmap = image as Bitmap;

                if (image.RawFormat.Guid == ImageFormat.Jpeg.Guid || !IsImageTransparent(bitmap))
                {
                    extension = "jpg";

                    codec = ImageCodecInfo.GetImageDecoders().First(e => e.FormatID == ImageFormat.Jpeg.Guid);
                    using (param = new EncoderParameters(1))
                    {
                        long quality = 90;
                        if (image.PropertyIdList.Any(e => e == 0x5010)) quality = image.PropertyItems.First(e => e.Id == 0x5010).Value[0];

                        param.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);

                        ResizeJpg(ref bitmap, buffer, codec, param);
                    }
                }
                else
                {
                    extension = "png";

                    codec = ImageCodecInfo.GetImageDecoders().First(e => e.FormatID == ImageFormat.Png.Guid);
                    using (param = new EncoderParameters(1))
                    {
                        param.Param[0] = new EncoderParameter(Encoder.ColorDepth, Bitmap.GetPixelFormatSize(image.PixelFormat));

                        ResizePng(ref bitmap, buffer, codec, param);
                    }
                }

                rawData = buffer.ToArray();

                image = bitmap;
            }

            newSize = image.Width * image.Height;
            ratio = newSize * 100d / oldSize;
        }

        private static void ResizeJpg(ref Bitmap image, MemoryStream rawData, ImageCodecInfo codec, EncoderParameters param)
        {
            int w = image.Width;
            int h = image.Height;

            rawData.SetLength(0);
            image.Save(rawData, codec, param);
            while (rawData.Length > MaxSize)
            {
                ResizeBySize(ref image, rawData, codec, param, w, h);

                w = (int)(w * 0.9f);
                h = (int)(h * 0.9f);
            }
        }

        private static void ResizePng(ref Bitmap image, MemoryStream rawData, ImageCodecInfo codec, EncoderParameters param)
        {
            int w, h;
            GetSizeFromPixels(MaxSize * param.Param[0].NumberOfValues / 8 * 2, image.Width, image.Height, out w, out h);
            
            rawData.SetLength(0);
            image.Save(rawData, codec, param);
            while (rawData.Length > MaxSize)
            {
                ResizeBySize(ref image, rawData, codec, param, w, h);

                w = (int)(w * 0.9f);
                h = (int)(h * 0.9f);
            }
        }

        private static void GetSizeFromPixels(int pixels, int oriW, int oriH, out int newW, out int newH)
        {
            newW = (int)Math.Ceiling(Math.Sqrt(pixels * oriW / oriH));
            newH = (int)Math.Ceiling(Math.Sqrt(pixels * oriH / oriW));

            if (newW > oriW) newW = oriW;
            if (newH > oriH) newH = oriH;
        }

        private static void ResizeBySize(ref Bitmap image, MemoryStream rawData, ImageCodecInfo codec, EncoderParameters param, int w, int h)
        {
            Bitmap imageNew = new Bitmap(w, h, image.PixelFormat);
            using (Graphics g = Graphics.FromImage(imageNew))
            {
                foreach (PropertyItem propertyItem in image.PropertyItems)
                    imageNew.SetPropertyItem(propertyItem);

                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                g.DrawImage(image, 0, 0, w, h);
            }

            rawData.SetLength(0);
            imageNew.Save(rawData, codec, param);

            image.Dispose();
            image = imageNew;
        }

        private static bool IsImageTransparent(Bitmap image)
        {
            PixelFormat[] formatsWithAlpha =
			{
				PixelFormat.Indexed,				PixelFormat.Gdi,				PixelFormat.Alpha,
				PixelFormat.PAlpha,					PixelFormat.Canonical,			PixelFormat.Format1bppIndexed,
				PixelFormat.Format4bppIndexed,		PixelFormat.Format8bppIndexed,	PixelFormat.Format16bppArgb1555,
				PixelFormat.Format32bppArgb,		PixelFormat.Format32bppPArgb,	PixelFormat.Format64bppArgb,
				PixelFormat.Format64bppPArgb
			};

            bool isTransparent = false;

            if (formatsWithAlpha.Contains(image.PixelFormat))
            {
                int x, y;
                for (y = 0; y < image.Height && !isTransparent; ++y)
                    for (x = 0; x < image.Width && !isTransparent; ++x)
                        if (image.GetPixel(x, y).A != 255)
                            isTransparent = true;
            }

            return isTransparent;
        }
    }
}
