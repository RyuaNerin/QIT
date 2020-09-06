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
using NetVips;

using Image = System.Drawing.Image;
using VipsImg = NetVips.Image;
using System.IO;

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

        private const double WebpQ = 90;

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

        /// <summary>이미지는 Gif, Png, Webp, Jpeg 중 하나여야 함.</summary>
        /// <returns>(extension, ratio)</returns>
        private static (string, double) Resize(Stream stream, CancellationToken ct)
        {
            // Gif Signature 확인
            stream.Position = 0;

            var fileType = Signatures.CheckSignature(stream);
            stream.Position = 0;

            if (fileType == Signatures.FileType.Gif)
            {
                var r = ResizeGif(stream, ct);
                if (r.Item1 != null)
                    return r;
            }

            return ResizeImg(stream, fileType, ct);
        }

        /// <returns>
        /// (extension, ratio)
        /// extension = null --> webp 파일로 변환할 필요가 있다면 
        /// </returns>
        private static (string, double) ResizeGif(Stream stream, CancellationToken ct)
        {
            using (var img = Image.FromStream(stream))
            {
                var gifReader = new GifFrameReader(img);

                // 프레임이 하나면 그냥 이미지로 바꿔서 트윗
                if (gifReader.FrameCount == 1)
                    return (null, 0);

                var baseSize = gifReader.Size;

                var overFrames = gifReader.FrameCount < GifFrames;
                var overSize = baseSize.Width > GifMaxWidth || baseSize.Height > GifMaxHeight;
                var overPixels = (double)baseSize.Width * baseSize.Height * gifReader.FrameCount > GifMaxPixels;

                // Gif 조건을 만족하면 처리할 필요가 없음.
                if (stream.Length < GifMaxSize && !overFrames && !overSize && !overPixels)
                    return (".gif", 1);

                var gifFrameInfo = gifReader.Frames.ToArray();

                var ratio = ReductionRatio;

                // Width / Height 줄이기
                if (overSize)
                {
                    ratio = Math.Min(
                        GifMaxWidth / baseSize.Width,
                        GifMaxHeight / baseSize.Height
                    );
                }

                // 프레임 수 줄이기
                if (overFrames)
                {
                    var delCount = gifReader.FrameCount - 350;
                    var sep = gifReader.FrameCount / (delCount + 1);

                    for (var i = 0; i < delCount; ++i)
                        gifFrameInfo[sep * i - 1].Delay += gifFrameInfo[sep * i].Delay;
                }

                // Max Pixels
                if (overPixels)
                {
                    ratio = (int)Math.Ceiling(Math.Sqrt((double)GifMaxPixels / gifFrameInfo.Length / baseSize.Height * baseSize.Width)) / baseSize.Width;
                }

                // File-Size
                var gifFrameOld = new Image[gifFrameInfo.Length];
                var gifFrameNew = new Image[gifFrameInfo.Length];

                try
                {
                    for (var i = 0; i < gifFrameInfo.Length; i++)
                        gifFrameOld[i] = gifReader.GetImage(gifFrameInfo[i].Offset);

                    do
                    {
                        ratio *= ReductionRatio;

                        var targetSize = new Size(
                            (int)(baseSize.Width * ratio),
                            (int)(baseSize.Height * ratio));

                        ct.ThrowIfCancellationRequested();

                        _ = Parallel.For(
                            0,
                            gifFrameInfo.Length,
                            new ParallelOptions
                            {
                                CancellationToken = ct,
                            },
                            index =>
                            {
                                gifFrameNew[index]?.Dispose();
                                gifFrameNew[index] = ResizeBySize(gifFrameOld[index], targetSize);
                            });

                        stream.SetLength(0);

                        gifFrameNew[0].SetPropertyItem(GifFrameReader.ToPropertyItem(gifFrameInfo));
                        gifFrameNew[0].Save(stream, GifCodec, GifParamFrame);

                        for (var i = 1; i < gifFrameInfo.Length; ++i)
                        {
                            ct.ThrowIfCancellationRequested();
                            gifFrameNew[0].SaveAdd(gifFrameNew[i], GifParamDimention);
                        }

                        ct.ThrowIfCancellationRequested();
                        gifFrameNew[0].SaveAdd(GifParamFlush);
                    }
                    while (stream.Length < GifMaxSize);

                    return (".gif", ratio);
                }
                finally
                {
                    foreach (var frame in gifFrameOld)
                        frame?.Dispose();

                    foreach (var frame in gifFrameNew)
                        frame?.Dispose();
                }
            }
        }

        /// <returns>(extension, ratio)</returns>
        private static (string, double) ResizeImg(Stream stream, Signatures.FileType fileType, CancellationToken ct)
        {
            if (fileType != Signatures.FileType.Unknown && stream.Length < ImgMaxSize)
            {
                switch (fileType)
                {
                    case Signatures.FileType.Png:  return (".png",  1);
                    case Signatures.FileType.Jpeg: return (".png",  1);
                    case Signatures.FileType.WebP: return (".webp", 1);
                }
            }

            using (var img = VipsImg.NewFromStream(stream))
            {
                var ratio = ReductionRatio;

                var hasAlpha = img.HasAlpha();

                do
                {
                    ct.ThrowIfCancellationRequested();

                    ratio *= ReductionRatio;

                    using (var newImg = img.Resize(ratio, Enums.Kernel.Lanczos3))
                    {
                        newImg.WebpsaveStream(
                            stream,
                            lossless: hasAlpha || fileType == Signatures.FileType.Png || fileType == Signatures.FileType.WebP
                        );
                        stream.SetLength(0);
                    }

                } while (stream.Length < ImgMaxSize);

                return (".webp", ratio);
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
