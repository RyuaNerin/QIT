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
        private const int MaxSize = 3040870; // 2.9 MiB
        
        public static void ResizeImage(ref Image image, MemoryStream rawData, out string extension)
        {
            // jpeg 거나 png 파일의 rawData 가 일정 기준 이하면 리사이징을 하지 않고 넘어간다
            if ((0 < rawData.Length && rawData.Length < MaxSize) && (image.RawFormat.Guid == ImageFormat.Jpeg.Guid || image.RawFormat.Guid == ImageFormat.Png.Guid))
            {
                extension = image.RawFormat.Guid == ImageFormat.Jpeg.Guid ? ".jpg" : ".png";
                return;
            }
            
            Bitmap              bitmap = ConvertToBitmap(image);
            ImageCodecInfo		codec;
            EncoderParameters	param;
            
            if (image.RawFormat.Guid == ImageFormat.Jpeg.Guid || !IsImageTransparent(bitmap))
            {
                extension = ".jpg";

                codec = ImageCodecInfo.GetImageDecoders().First(e => e.FormatID == ImageFormat.Jpeg.Guid);
                using (param = new EncoderParameters(1))
                {
                    long quality = 90;
                    if (image.PropertyIdList.Any(e => e == 0x5010)) quality = image.PropertyItems.First(e => e.Id == 0x5010).Value[0];

                    param.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);

                    ResizeJpg(ref bitmap, rawData, codec, param);
                }
            }
            else
            {
                extension = ".png";

                codec = ImageCodecInfo.GetImageDecoders().First(e => e.FormatID == ImageFormat.Png.Guid);
                using (param = new EncoderParameters(1))
                {
                    param.Param[0] = new EncoderParameter(Encoder.ColorDepth, Bitmap.GetPixelFormatSize(image.PixelFormat));

                    ResizePng(ref bitmap, rawData, codec, param);
                }
            }

            image = bitmap;
        }

        private static Bitmap ConvertToBitmap(Image image)
        {
            Metafile meta;
            Bitmap bitmap;

            meta = image as Metafile;
            if (meta != null)
            {
                using (meta)
                {
                    var header = meta.GetMetafileHeader();
                    float scaleX = header.DpiX / 96f;
                    float scaleY = header.DpiY / 96f;

                    bitmap = new Bitmap((int)(scaleX * meta.Width / header.DpiX * 100), (int)(scaleY * meta.Height / header.DpiY * 100), PixelFormat.Format32bppArgb);
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;

                        g.Clear(Color.Transparent);
                        g.ScaleTransform(scaleX, scaleY);
                        g.DrawImageUnscaledAndClipped(meta, new Rectangle(0, 0, image.Width, image.Height));
                    }

                    return bitmap;
                }
            }

            bitmap = image as Bitmap;
            if (bitmap != null)
                return bitmap;
                        
            using (image)
            {
                bitmap = new Bitmap(image.Width, image.Height, image.PixelFormat);
                using (var g = Graphics.FromImage(bitmap))
                    g.DrawImageUnscaledAndClipped(image, new Rectangle(0, 0, image.Width, image.Height));

                return bitmap;
            }
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
