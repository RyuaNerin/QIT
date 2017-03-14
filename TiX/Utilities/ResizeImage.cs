// https://dev.twitter.com/rest/media/uploading-media

// GIF
// Resolution should be <= 1280x1080 (width x height)
// Number of frames <= 350
// Number of pixels (width * height * num_frames) <= 300 million

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using TiX.Core;

namespace TiX.Utilities
{
    internal static class ResizeImage
    {
        private const int ImgMaxSize = 3 * 1024 * 1024;
        private const int GifMaxSize = 5 * 1024 * 1024;
        private const double JpgCompressionRatio = 10.0d; // : 1
        private const double PngCompressionRatio =  2.5d; // : 1

        private static readonly ImageCodecInfo JpgCodec;
        private static readonly ImageCodecInfo PngCodec;
        private static readonly ImageCodecInfo GifCodec;
        private static readonly EncoderParameters JpgParam;
        private static readonly EncoderParameters PngParam;
        private static readonly EncoderParameters GifParamFrame;
        private static readonly EncoderParameters GifParamDimention;
        private static readonly EncoderParameters GifParamFlush;

        static ResizeImage()
        {
            JpgCodec = ImageCodecInfo.GetImageDecoders().First(e => e.FormatID == ImageFormat.Jpeg.Guid);
            PngCodec = ImageCodecInfo.GetImageDecoders().First(e => e.FormatID == ImageFormat.Png.Guid);
            GifCodec = ImageCodecInfo.GetImageDecoders().First(e => e.FormatID == ImageFormat.Gif.Guid);

            JpgParam = new EncoderParameters(2);
            JpgParam.Param[0] = new EncoderParameter(Encoder.Quality, 95L);
            JpgParam.Param[1] = new EncoderParameter(Encoder.Compression, (long)EncoderValue.CompressionLZW);
            
            PngParam = new EncoderParameters(2);
            PngParam.Param[0] = new EncoderParameter(Encoder.ColorDepth, 32L);
            PngParam.Param[1] = new EncoderParameter(Encoder.Compression, (long)EncoderValue.CompressionLZW);

            GifParamFrame = new EncoderParameters(2);
            GifParamFrame.Param[0] = new EncoderParameter(Encoder.SaveFlag, (long)EncoderValue.MultiFrame);
            GifParamFrame.Param[1] = new EncoderParameter(Encoder.Compression, (long)EncoderValue.CompressionLZW);

            GifParamDimention = new EncoderParameters(1);
            GifParamDimention.Param[0] = new EncoderParameter(Encoder.SaveFlag, (long)EncoderValue.FrameDimensionTime);

            GifParamFlush = new EncoderParameters(1);
            GifParamFlush.Param[0] = new EncoderParameter(Encoder.SaveFlag, (long)EncoderValue.Flush);
        }
        
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

        public static bool Resize(ImageSet imageSet)
        {            
            var szBefore = imageSet.Image.Size;
            try
            {
                ResizeImagePrivate(imageSet);
            }
#if !TiXd
            catch (Exception ex)
            {
                CrashReport.Error(ex, null);
#else
            catch
            {
#endif
                return false;
            }
            var szAfter = imageSet.Image.Size;

            imageSet.Ratio = ((double)szAfter.Width * szAfter.Height) / (szBefore.Width * szBefore.Height) * 100d;

            // Thumbnail
            var ratio = Math.Min(64d / imageSet.Image.Width, 64d / imageSet.Image.Height);
            var newWidth  = (int)(imageSet.Image.Width  * ratio);
            var newHeight = (int)(imageSet.Image.Height * ratio);

            imageSet.Thumbnail = ResizeBySize(imageSet.Image, newWidth, newHeight, PixelFormat.Format32bppArgb);

            return true;
        }

        private static void ResizeImagePrivate(ImageSet imageSet)
        {
            var ext = GetExtension(imageSet);
            if (ext == Extension.Gif)
            {
                try
                {
                    imageSet.GifFrames = new GifFrames(imageSet.Image);
                }
#if !TiXd
                catch (Exception ex)
                {
                    if (ex.Message != "_")
                        CrashReport.Error(ex, null);
#else
                catch
                {
#endif
                }

                // 프레임이 포함된 애니메이션일 경우
                if (imageSet.GifFrames != null)
                {
                    if (imageSet.GifFrames.Count > 1)
                    {
                        ResizeGif(imageSet);
                        return;
                    }
                    else
                    {
                        imageSet.GifFrames.Dispose();
                        imageSet.GifFrames = null;
                    }
                }
            }

            // 크기가 일정 기준 이하면 리사이징을 하지 않고 넘어간다
            if (0 < imageSet.RawStream.Length && imageSet.RawStream.Length < ImgMaxSize)
                return;

            bool containsAlpha;
            imageSet.Image = ConvertToBitmap(imageSet.Image, out containsAlpha);

            ImageCodecInfo codec;
            EncoderParameters param;
            double pixels;

            if (ext == Extension.Jpg || !containsAlpha)
            {
                imageSet.Extension = ".jpg";
                codec = JpgCodec;
                param = JpgParam;
                pixels = ImgMaxSize * JpgCompressionRatio;
            }
            else
            {
                imageSet.Extension = ".png";
                codec = PngCodec;
                param = PngParam;
                pixels = ImgMaxSize * PngCompressionRatio * 4;
            }
            
            if (imageSet.RawStream.Length == 0)
            {
                imageSet.Image.Save(imageSet.RawStream, codec, param);

                if (0 < imageSet.RawStream.Length && imageSet.RawStream.Length < ImgMaxSize)
                    return;
            }

            Resize(imageSet, codec, param, pixels);
        }

        private static void ResizeGif(ImageSet imageSet)
        {
            int i;
            int w = imageSet.Image.Width;
            int h = imageSet.Image.Height;
            bool requireResize = imageSet.RawStream.Length >= GifMaxSize;

            // Resolution should be <= 1280x1080 (width x height)
            if (w > 1280 || h > 1080)
            {
                requireResize = true;

                var ratio = Math.Min(1280d / imageSet.Image.Width, 1080d / imageSet.Image.Height);
                w  = (int)(imageSet.Image.Width  * ratio);
                h = (int)(imageSet.Image.Height * ratio);
            }

            // Number of frames <= 350
            if (imageSet.GifFrames.Count > 350)
            {
                var delCount = imageSet.GifFrames.Count - 350;
                var sep = imageSet.GifFrames.Count / (delCount + 1);
                
                for (i = 0; i < delCount; ++i)
                {
                    requireResize = true;

                    imageSet.GifFrames[sep * i].Image.Dispose();
                    imageSet.GifFrames.RemoveAt(sep * i);
                }
            }

            // Number of pixels (width * height * num_frames) <= 300,000,000
            {
                double pixels = w * h * imageSet.GifFrames.Count;
                if (pixels > 300000000)
                {
                    requireResize = true;

                    int nw, nh;
                    GetSizeFromPixels(300000000d / imageSet.GifFrames.Count, w, h, out nw, out nh);

                    w = nw;
                    h = nh;
                }
            }
            

            Bitmap[] image = new Bitmap[imageSet.GifFrames.Count];
            while (requireResize && imageSet.RawStream.Length >= GifMaxSize)
            {
                w = (int)(w * 0.9d);
                h = (int)(h * 0.9d);

                for (i = 0; i < imageSet.GifFrames.Count; ++i)
                {
                    if (image[i] != null)
                        image[i].Dispose();
                    image[i] = ResizeBySize(imageSet.GifFrames[i].Image, w, h);
                }

                imageSet.RawStream.SetLength(0);

                using (var newImage = image[0].Clone() as Bitmap)
                {
                    CopyProperties(imageSet.Image, newImage);

                    for (i = 0; i < imageSet.GifFrames.Count; ++i)
                    {
                        if (i == 0)
                            newImage.Save(imageSet.RawStream, GifCodec, GifParamFrame);
                        else
                            newImage.SaveAdd(image[i], GifParamDimention);
                    }
                    newImage.SaveAdd(GifParamFlush);
                }
            }
            if (requireResize)
            {
                for (i = 0; i < imageSet.GifFrames.Count; ++i)
                {
                    imageSet.GifFrames[i].Image.Dispose();
                    imageSet.GifFrames[i].Image = image[i];
                }
            }

            imageSet.Image.Dispose();
            imageSet.Image = imageSet.GifFrames[0].Image;
            imageSet.RawStream.Position = 0;
        }

        private readonly static PixelFormat[] FormatIndexed =
        {
            PixelFormat.Indexed,
            PixelFormat.Format1bppIndexed,
            PixelFormat.Format4bppIndexed,
            PixelFormat.Format8bppIndexed
        };

        private static Bitmap ConvertToBitmap(Image image, out bool containsAlpha)
        {
            var bitmap = image as Bitmap;
            if (bitmap != null)
            {
                if (FormatIndexed.Contains(image.PixelFormat))
                {
                    var newBitmap = bitmap.Clone(new Rectangle(Point.Empty, image.Size), PixelFormat.Format32bppRgb);
                    CopyProperties(bitmap, newBitmap);

                    bitmap.Dispose();
                    bitmap = newBitmap;
                }
                
                if (Image.IsAlphaPixelFormat(image.PixelFormat))
                {
                    containsAlpha = IsImageTransparent(bitmap);
                    if (!containsAlpha)
                    {
                        var newBitmap = bitmap.Clone(new Rectangle(Point.Empty, bitmap.Size), PixelFormat.Format24bppRgb);
                        CopyProperties(bitmap, newBitmap);

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
                    CopyProperties(meta, bitmap);
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
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
                CopyProperties(image, bitmap);
                using (var g = Graphics.FromImage(bitmap))
                    g.DrawImageUnscaledAndClipped(image, new Rectangle(0, 0, image.Width, image.Height));

                return ConvertToBitmap(bitmap, out containsAlpha);
            }
        }

        private static void CopyProperties(Image from, Image to)
        {
            foreach (var propertyItem in from.PropertyItems)
                to.SetPropertyItem(propertyItem);
        }

        private static void Resize(ImageSet imageSet, ImageCodecInfo codec, EncoderParameters param, double size)
        {
            int w, h;

            GetSizeFromPixels(size, imageSet.Image.Width, imageSet.Image.Height, out w, out h);

            Bitmap newImage = null;
            do
            {
                if (newImage != null)
                    newImage.Dispose();

                newImage = ResizeAndSave(imageSet, codec, param, w, h);

                w = (int)(w * 0.9f);
                h = (int)(h * 0.9f);
            } while (imageSet.RawStream.Length >= ImgMaxSize);

            imageSet.Image.Dispose();
            imageSet.Image = newImage;
        }

        private static void GetSizeFromPixels(double pixels, int oriW, int oriH, out int newW, out int newH)
        {
            newW = (int)Math.Ceiling(Math.Sqrt(pixels / oriH * oriW));
            newH = (int)Math.Ceiling(Math.Sqrt(pixels / oriW * oriH));

            if (newW > oriW) newW = oriW;
            if (newH > oriH) newH = oriH;
        }

        private static Bitmap ResizeAndSave(ImageSet imageSet, ImageCodecInfo codec, EncoderParameters param, int w, int h)
        {
            var newImage = ResizeBySize(imageSet.Image, w, h);
            imageSet.RawStream.SetLength(0);
            newImage.Save(imageSet.RawStream, codec, param);

            return newImage;
        }

        private static Bitmap ResizeBySize(Image oldImage, int w, int h)
        {
            return ResizeBySize(oldImage, w, h, oldImage.PixelFormat);
        }
        private static Bitmap ResizeBySize(Image oldImage, int w, int h, PixelFormat pixelFormat)
        {
            var newImage = new Bitmap(w, h, pixelFormat);
            using (var g = Graphics.FromImage(newImage))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                g.DrawImage(oldImage, new Rectangle(0, 0, w, h), new Rectangle(Point.Empty, oldImage.Size), GraphicsUnit.Pixel);
            }

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
