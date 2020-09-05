// https://developer.twitter.com/en/docs/twitter-api/v1/media/upload-media/uploading-media/media-best-practices

// Supported image media types: JPG, PNG, GIF, WEBP
// Image size <= 5 MB, animated GIF size <= 15 MB

// GIF
// Resolution should be <= 1280x1080 (width x height)
// Number of frames <= 350
// Number of pixels (width * height * num_frames) <= 300 million

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TiX.Utilities;
using WebPWrapper;

namespace TiX.Core
{
    partial class ImageSet
    {
        private const double ReductionRatio = 0.90d;

        private const int ImgMaxSize   =  5 * 1024 * 1024;
        private const int GifMaxSize   = 15 * 1024 * 1024;
        private const int GifMaxWidth  = 1280;
        private const int GifMaxHeight = 1080;
        private const int GifFrames    = 350;
        private const int GifMaxPixels = 300 * 1000 * 1000;

        private static readonly ImageCodecInfo GifCodec = ImageCodecInfo.GetImageDecoders().First(e => e.FormatID == ImageFormat.Gif.Guid);
        private static readonly EncoderParameters GifParamFrame = new EncoderParameters
        {
            Param = new EncoderParameter[]
            {
                new EncoderParameter(Encoder.SaveFlag, (long)EncoderValue.MultiFrame),
                new EncoderParameter(Encoder.Compression, (long)EncoderValue.CompressionLZW),
            }
        };
        private static readonly EncoderParameters GifParamDimention = new EncoderParameters
        {
            Param = new EncoderParameter[]
            {
                new EncoderParameter(Encoder.SaveFlag, (long)EncoderValue.FrameDimensionTime),
            }
        };
        private static readonly EncoderParameters GifParamFlush = new EncoderParameters
        {
            Param = new EncoderParameter[]
            {
                new EncoderParameter(Encoder.SaveFlag, (long)EncoderValue.Flush),
            }
        };

        private void Resize(Image img, string extension, CancellationToken ct)
        {
            if (img.RawFormat.Guid == ImageFormat.Gif.Guid)
            {
                this.ResizeGif(img, ct);
            }
            else
            {
                this.ResizeImg(img, extension, ct);
            }
        }

        private void ResizeGif(Image gif, CancellationToken ct)
        {
            using (var gf = new GifFrames(gif))
            {
                // 프레임이 하나면 그냥 이미지로 바꿔서 트윗
                if (gf.Count == 1)
                {
                    this.ResizeImg(gif, null, ct);
                    return;
                }

                // Gif 조건을 만족하면 처리할 필요가 없음.
                var baseSize = gf.Size;
                if (0 < this.m_tempFile.Length && this.m_tempFile.Length < GifMaxSize &&
                    baseSize.Width < GifMaxWidth && baseSize.Height < GifMaxHeight &&
                    gf.Count < GifFrames &&
                    baseSize.Width * baseSize.Height * gf.Count <= GifMaxPixels)
                {
                    return;
                }

                var targetSize = baseSize;

                // Width / Height 줄이기
                if (baseSize.Width > GifMaxWidth || baseSize.Height > GifMaxHeight)
                {
                    var ratio = Math.Min(
                        GifMaxWidth / baseSize.Width,
                        GifMaxHeight / baseSize.Height
                    );
                    targetSize.Width = (int)(baseSize.Width  * ratio);
                    targetSize.Height = (int)(baseSize.Height * ratio);
                }

                // 프레임 수 줄이기
                if (gf.Count > 350)
                {
                    var delCount = gf.Count - 350;
                    var sep = gf.Count / (delCount + 1);

                    for (var i = 0; i < delCount; ++i)
                    {
                        gf[sep * i - 1].Dalay += gf[sep * i].Dalay;
                        gf[sep * i].Image.Dispose();
                        gf.RemoveAt(sep * i);
                    }
                }

                // Max Pixels
                {
                    if (targetSize.Width * targetSize.Height * gf.Count > 300000000)
                    {
                        targetSize = GetSizeFromPixels((double)GifMaxPixels / gf.Count, targetSize);
                    }
                }

                // File-Size
                var image = new Bitmap[gf.Count];

                try
                {
                    while (baseSize != targetSize && this.m_tempFile.Length < GifMaxSize)
                    {
                        ct.ThrowIfCancellationRequested();

                        _ = Parallel.For(
                            0,
                            gf.Count,
                            new ParallelOptions
                            {
                                CancellationToken = ct,
                            },
                            index =>
                            {
                                image[index]?.Dispose();
                                image[index] = ResizeBySize(gf[index].Image, targetSize);
                            });

                        this.m_tempFile.SetLength(0);

                        using (var newImage = new Bitmap(image[0]))
                        {
                            CopyProperties(gif, newImage);

                            newImage.Save(this.m_tempFile, GifCodec, GifParamFrame);

                            for (var i = 1; i < gf.Count; ++i)
                            {
                                ct.ThrowIfCancellationRequested();
                                newImage.SaveAdd(image[i], GifParamDimention);
                            }

                            ct.ThrowIfCancellationRequested();
                            newImage.SaveAdd(GifParamFlush);
                        }

                        targetSize.Width  = (int)(targetSize.Width  * ReductionRatio);
                        targetSize.Height = (int)(targetSize.Height * ReductionRatio);

                        this.Extension = ".gif";
                        this.Ratio = ((double)targetSize.Width * targetSize.Height) / ((double)baseSize.Width * baseSize.Height) * 100d;
                    }
                }
                finally
                {
                    foreach (var img in image)
                    {
                        img?.Dispose();
                    }
                }
            }
        }

        private void ResizeImg(Image img, string extension, CancellationToken ct)
        {
            switch (extension)
            {
                case ".jpg":
                case ".png":
                case ".webp":
                    if (0 < this.m_tempFile.Length && this.m_tempFile.Length < ImgMaxSize)
                    {
                        this.Extension = extension;
                        this.Ratio = 1;
                        return;
                    }
                    break;
            }

            var bitmap = img as Bitmap;

            try
            {
                // Bitmap 으로 변경
                if (bitmap == null)
                {
                    bitmap = new Bitmap(img.Width, img.Height, PixelFormat.Format32bppArgb);

                    using (var g = Graphics.FromImage(bitmap))
                        g.DrawImageUnscaledAndClipped(img, new Rectangle(Point.Empty, bitmap.Size));
                }
                ct.ThrowIfCancellationRequested();

                var hasAlpha = extension != ".jpg" && HasAlpha(bitmap, ct);

                // 24 RGB 혹은 32 ARGB 로 변경
                if (img.PixelFormat != PixelFormat.Format24bppRgb && img.PixelFormat != PixelFormat.Format32bppArgb)
                {
                    var bitmap2 = bitmap.Clone(
                        new Rectangle(Point.Empty, img.Size),
                        hasAlpha ? PixelFormat.Format32bppArgb : PixelFormat.Format24bppRgb);

                    CopyProperties(bitmap, bitmap2);

                    bitmap.Dispose();
                    bitmap = bitmap2;
                }
                ct.ThrowIfCancellationRequested();

                var baseSize = bitmap.Size;
                var targetSize = baseSize;
                do
                {
                    ct.ThrowIfCancellationRequested();

                    this.m_tempFile.SetLength(0);

                    Bitmap bitmapResized = null;
                    try
                    {
                        bitmapResized = baseSize == targetSize ? ResizeBySize(bitmap, targetSize) : null;

                        using (var webp = new WebP())
                        {
                            byte[] buff;

                            if (hasAlpha)
                            {
                                buff = webp.EncodeLossless(bitmapResized ?? bitmap);
                            }
                            else
                            {
                                buff = webp.EncodeLossy(bitmapResized ?? bitmap);
                            }

                            this.m_tempFile.WriteAsync(buff, 0, buff.Length, ct).GetAwaiter().GetResult();
                        }
                    }
                    finally
                    {
                        bitmapResized?.Dispose();
                    }

                    targetSize.Width  = (int)Math.Ceiling(targetSize.Width  * ReductionRatio);
                    targetSize.Height = (int)Math.Ceiling(targetSize.Height * ReductionRatio);
                } while (this.m_tempFile.Length < ImgMaxSize);

                this.Extension = ".webp";
                this.Ratio = ((double)targetSize.Width * targetSize.Height) / ((double)baseSize.Width * baseSize.Height) * 100d;
            }
            finally
            {
                bitmap?.Dispose();
            }
        }

        private unsafe static bool HasAlpha(Bitmap image, CancellationToken ct)
        {
            switch (image.PixelFormat)
            {
                case PixelFormat.Format16bppRgb555:
                case PixelFormat.Format16bppRgb565:
                case PixelFormat.Format24bppRgb:
                case PixelFormat.Format32bppRgb:
                case PixelFormat.Format48bppRgb:
                    return false;

                case PixelFormat.Format32bppArgb:
                case PixelFormat.Format32bppPArgb:
                case PixelFormat.Format64bppArgb:
                case PixelFormat.Format64bppPArgb:
                    {
                        BitmapData bits = null;

                        try
                        {
                            bits = image.LockBits(
                                new Rectangle(Point.Empty, image.Size),
                                ImageLockMode.ReadOnly,
                                image.PixelFormat);

                            var ptr = (byte*)bits.Scan0;

                            var bpp = Image.GetPixelFormatSize(bits.PixelFormat);
                            var v = (1 << (bpp * 2)) - 1;

                            var pr = Parallel.For(
                                0,
                                bits.Height,
                                new ParallelOptions
                                {
                                    CancellationToken = ct,
                                },
                                (y, state) =>
                                {
                                    var bptr = ptr + bits.Stride * y;

                                    var buff = new byte[4];
                                    for (int x = 0; x < bits.Width && !state.IsStopped; x += bpp)
                                    {
                                        buff[0] = bptr[x + 0];
                                        buff[1] = bptr[x + 1];
                                        buff[2] = bptr[x + 2];
                                        buff[3] = bptr[x + 3];

                                        if ((BitConverter.ToInt32(buff, 0) >> (bpp * 6)) != v)
                                            state.Break();
                                    }
                                });

                            return pr.IsCompleted;
                        }
                        catch
                        {
                            return true;
                        }
                        finally
                        {
                            if (bits != null)
                                image.UnlockBits(bits);
                        }
                    }

                // ㅜㅜ
                default:
                    {
                        var alpha = image.GetPixel(0, 0).A;

                        var result = Parallel.For(
                            0,
                            image.Height,
                            new ParallelOptions
                            {
                                CancellationToken = ct,
                            },
                            (y, state) =>
                            {
                                for (int x = 0; x < image.Width; ++x)
                                    if (image.GetPixel(x, y).A != alpha)
                                        state.Break();
                            });

                        return !result.IsCompleted;
                    }
            }
        }

        private static Size GetSizeFromPixels(double pixels, Size old)
        {
            var @new = new Size(
                (int)Math.Ceiling(Math.Sqrt(pixels / old.Height * old.Width )),
                (int)Math.Ceiling(Math.Sqrt(pixels / old.Width  * old.Height)));

            if (@new.Width  > old.Width ) @new.Width  = old.Width ;
            if (@new.Height > old.Height) @new.Height = old.Height;

            return @new;
        }

        private static void CopyProperties(Image from, Image to)
        {
            foreach (var propertyItem in from.PropertyItems)
                to.SetPropertyItem(propertyItem);
        }

        private static Bitmap ResizeBySize(Image oldImage, Size sz)
        {
            return ResizeBySize(oldImage, sz, oldImage.PixelFormat);
        }
        private static Bitmap ResizeBySize(Image oldImage, Size size, PixelFormat pixelFormat)
        {
            var newImage = new Bitmap(size.Width, size.Height, pixelFormat);
            using (var g = Graphics.FromImage(newImage))
            {
                // 하쒸 프리징
                var ratio = (float)oldImage.Width / size.Width * oldImage.Height / size.Height;
                if (ratio < 0.25)
                    g.InterpolationMode = InterpolationMode.High;
                else if (ratio < 0.5)
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                else
                    g.InterpolationMode = InterpolationMode.HighQualityBilinear;

                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                g.DrawImage(
                    oldImage,
                    new Rectangle(Point.Empty, size),
                    new Rectangle(Point.Empty, oldImage.Size),
                    GraphicsUnit.Pixel);
            }

            return newImage;
        }
    }
}
