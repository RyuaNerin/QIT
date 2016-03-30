using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using TiX.Core;

namespace TiX.Utilities
{
    public static class ImageResize
    {
        private const int MaxSize = 3 * 1024 * 1024; // 3 MiB
        private const double JPGCompressionRatio = 16.0d; // : 1
        private const double PNGCompressionRatio =  2.5d; // : 1
        
        private enum Extension { Gif, Png, Jpg }
        private static Extension GetExtension(ImageSet image)
        {
            var guid = image.Image.RawFormat.Guid;

            if (guid == ImageFormat.Gif.Guid)
            {
                image.Extension = ".gif";
                return Extension.Gif;
            }

            if (guid == ImageFormat.Jpeg.Guid)
            {
                image.Extension = ".jpg";
                return Extension.Jpg;
            }

            image.Extension = ".png";
            return Extension.Png;
        }
        
        private const long ColorDepth = 32L;

        private static readonly ImageCodecInfo JpgCodec;
        private static readonly ImageCodecInfo PngCodec;
        private static readonly ImageCodecInfo GifCodec;
        private static readonly EncoderParameters JpgParam;
        private static readonly EncoderParameters PngParam;
        private static readonly EncoderParameters GifParamFrame;
        private static readonly EncoderParameters GifParamDimention;
        private static readonly EncoderParameters GifParamFlush;

        static ImageResize()
        {
            JpgCodec = ImageCodecInfo.GetImageDecoders().First(e => e.FormatID == ImageFormat.Jpeg.Guid);
            PngCodec = ImageCodecInfo.GetImageDecoders().First(e => e.FormatID == ImageFormat.Png.Guid);
            GifCodec = ImageCodecInfo.GetImageDecoders().First(e => e.FormatID == ImageFormat.Gif.Guid);

            JpgParam = new EncoderParameters(1);
            JpgParam.Param[0] = new EncoderParameter(Encoder.Quality, 90L);
            
            PngParam = new EncoderParameters(2);
            PngParam.Param[0] = new EncoderParameter(Encoder.ColorDepth, ColorDepth);
            PngParam.Param[1] = new EncoderParameter(Encoder.Compression, (long)EncoderValue.CompressionLZW);

            GifParamFrame = new EncoderParameters(2);
            GifParamFrame.Param[0] = new EncoderParameter(Encoder.SaveFlag, (long)EncoderValue.MultiFrame);
            GifParamFrame.Param[1] = new EncoderParameter(Encoder.Compression, (long)EncoderValue.CompressionLZW);

            GifParamDimention = new EncoderParameters(1);
            GifParamDimention.Param[0] = new EncoderParameter(Encoder.SaveFlag, (long)EncoderValue.FrameDimensionTime);

            GifParamFlush = new EncoderParameters(1);
            GifParamFlush.Param[0] = new EncoderParameter(Encoder.SaveFlag, (long)EncoderValue.Flush);
        }

        public static void ResizeImage(ImageSet imageSet)
        {
            var ext = GetExtension(imageSet);

#if !DEBUG
            // jpeg 혹은 png 파일의 크기가 일정 기준 이하면 리사이징을 하지 않고 넘어간다
            if ((ext == Extension.Jpg || ext == Extension.Png) && imageSet.RawStream.Length < MaxSize)
                return;
#endif

            if (ext == Extension.Gif)
                if (ResizeGif(imageSet))
                    return;

            bool containsAlpha;
            imageSet.Image = ConvertToBitmap(imageSet.Image, out containsAlpha);

            if (ext == Extension.Jpg || !containsAlpha)
            {
                imageSet.Extension = ".jpg";
                Resize(imageSet, JpgCodec, JpgParam, MaxSize * JPGCompressionRatio);
            }
            else
            {
                imageSet.Extension = ".png";
                Resize(imageSet, PngCodec, PngParam, MaxSize * PNGCompressionRatio * ColorDepth / 8);
            }
        }

        private static bool ResizeGif(ImageSet imageSet)
        {
            try
            {
                imageSet.GifFrames = new GifFrames(imageSet.Image);	
            }
            catch
            {
                return false;
            }


            int i;
            int w = imageSet.GifFrames.Size.Width;
            int h = imageSet.GifFrames.Size.Height;
            
            // Remove Transparent
            for (i = 0; i < imageSet.GifFrames.Count; ++i)
            {
                var newBitmap = imageSet.GifFrames[i].Image.Clone(new Rectangle(Point.Empty, imageSet.GifFrames[i].Image.Size), PixelFormat.Format24bppRgb);
                imageSet.GifFrames[i].Image.Dispose();
                imageSet.GifFrames[i].Image = newBitmap;
            }

            while (imageSet.RawStream.Length > MaxSize)
            {
                w = (int)(w * 0.9d);
                h = (int)(h * 0.9d);

                for (i = 0; i < imageSet.GifFrames.Count; ++i)
                    imageSet.GifFrames[i].Image = ResizeImage(imageSet.GifFrames[i].Image, w, h);

                imageSet.RawStream.SetLength(0);

                using (var newImage = new Bitmap(imageSet.GifFrames[0].Image))
                {
                    foreach (var propertyItem in imageSet.Image.PropertyItems)
                        newImage.SetPropertyItem(propertyItem);

                    for (i = 0; i < imageSet.GifFrames.Count; ++i)
                    {
                        if (i == 0)
                            newImage.Save(imageSet.RawStream, GifCodec, GifParamFrame);
                        else
                            newImage.SaveAdd(imageSet.GifFrames[i].Image, GifParamDimention);
                    }
                    newImage.SaveAdd(GifParamFlush);
                }
            }

            imageSet.Image.Dispose();
            imageSet.Image = imageSet.GifFrames[0].Image;

            return true;
        }

        private readonly static PixelFormat[] FormatIndexed =
        {
            PixelFormat.Indexed,
            PixelFormat.Format1bppIndexed,
            PixelFormat.Format4bppIndexed,
            PixelFormat.Format8bppIndexed,
            PixelFormat.Undefined
        };
        private readonly static PixelFormat[] FormatWithAlpha =
        {
            PixelFormat.Gdi,
            PixelFormat.Alpha,
			PixelFormat.PAlpha,
            PixelFormat.Canonical,
            PixelFormat.Format16bppArgb1555,
			PixelFormat.Format32bppArgb,
            PixelFormat.Format32bppPArgb,
            PixelFormat.Format64bppArgb,
			PixelFormat.Format64bppPArgb
        };

        private static Bitmap ConvertToBitmap(Image image, out bool containsAlpha)
        {
            var bitmap = image as Bitmap;
            if (bitmap != null)
            {
                if (FormatIndexed.Contains(image.PixelFormat))
                {
                    var newBitmap = bitmap.Clone(new Rectangle(Point.Empty, image.Size), PixelFormat.Format32bppRgb);
                    bitmap.Dispose();
                    bitmap = newBitmap;
                }
                
                if (FormatWithAlpha.Contains(image.PixelFormat))
                {
                    containsAlpha = IsImageTransparent(bitmap);
                    if (!containsAlpha)
                    {
                        var newBitmap = bitmap.Clone(new Rectangle(Point.Empty, bitmap.Size), PixelFormat.Format24bppRgb);
                        bitmap.Dispose();
                        bitmap = newBitmap;
                    }
                }
                else
                    containsAlpha = false;
                
                return bitmap;
            }

            var meta = image as Metafile;
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
                        foreach (var propertyItem in meta.PropertyItems)
                            bitmap.SetPropertyItem(propertyItem);

                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        g.SmoothingMode = SmoothingMode.AntiAlias;

                        g.ScaleTransform(scaleX, scaleY);
                        g.DrawImageUnscaledAndClipped(meta, new Rectangle(0, 0, image.Width, image.Height));
                    }

                    return ConvertToBitmap(bitmap, out containsAlpha);
                }
            }
                        
            using (image)
            {
                bitmap = new Bitmap(image.Width, image.Height, image.PixelFormat);
                using (var g = Graphics.FromImage(bitmap))
                    g.DrawImageUnscaledAndClipped(image, new Rectangle(0, 0, image.Width, image.Height));

                return ConvertToBitmap(bitmap, out containsAlpha);
            }
        }

        private static void Resize(ImageSet imageSet, ImageCodecInfo codec, EncoderParameters param, double size)
        {
            int w, h;

            GetSizeFromPixels(size, imageSet.Width, imageSet.Height, out w, out h);

            do
            {
                ResizeBySize(imageSet, codec, param, w, h);

                w = (int)(w * 0.9f);
                h = (int)(h * 0.9f);
            } while (imageSet.RawStream.Length > MaxSize);
        }

        private static void GetSizeFromPixels(double pixels, int oriW, int oriH, out int newW, out int newH)
        {
            newW = (int)Math.Ceiling(Math.Sqrt(pixels / oriH * oriW));
            newH = (int)Math.Ceiling(Math.Sqrt(pixels / oriW * oriH));

            if (newW > oriW) newW = oriW;
            if (newH > oriH) newH = oriH;
        }

        private static void ResizeBySize(ImageSet imageSet, ImageCodecInfo codec, EncoderParameters param, int w, int h)
        {
            imageSet.Image = ResizeImage(imageSet.Image, w, h);

            imageSet.RawStream.SetLength(0);
            imageSet.Image.Save(imageSet.RawStream, codec, param);
        }

        private static Bitmap ResizeImage(Image oldImage, int w, int h)
        {
            var newImage = new Bitmap(w, h, oldImage.PixelFormat);
            foreach (var propertyItem in oldImage.PropertyItems)
                newImage.SetPropertyItem(propertyItem);
            using (var g = Graphics.FromImage(newImage))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                g.DrawImage(oldImage, 0, 0, w, h);
            }

            oldImage.Dispose();

            return newImage;
        }

        private static bool IsImageTransparent(Bitmap image)
        {
            int x, y;
            for (y = 0; y < image.Height; ++y)
                for (x = 0; x < image.Width; ++x)
                    if (image.GetPixel(x, y).A != 255)
                        return true;

            return false;
        }
    }
}
