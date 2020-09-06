using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using RyuaNerin.Drawing;
using Sentry;
using TiX.Utilities;
using Encoder = System.Drawing.Imaging.Encoder;

namespace TiX.Core
{
    internal partial class ImageSet : IDisposable, INotifyPropertyChanged
    {
        public const int BufferSize = 16 * 1024;
        public enum Statues
        {
            None,
            Success,
            Error
        }

        public static bool IsAvailable(IDataObject e)
            => e != null && ExtractFromDataObject(null, e, CancellationToken.None);

        private ImageSet(ImageCollection collection)
        {
            this.m_collection = collection;

            int tries = 3;

            do
            {
                try
                {
                    this.TempPath = Path.GetTempFileName();
                    this.m_tempFile = new FileStream(
                        this.TempPath,
                        FileMode.OpenOrCreate,
                        FileAccess.ReadWrite,
                        FileShare.None,
                        4096,
                        FileOptions.DeleteOnClose);
                }
                catch
                {
                }
            } while (--tries > 0);

            if (this.m_tempFile == null)
                this.m_tempFile = new MemoryStream(3 * 1024 * 1024);
        }

        public ImageSet(ImageCollection collection, IDataObject dataObject)
            : this(collection)
        {
            this.m_baseDataObject = dataObject;
        }

        public ImageSet(ImageCollection collection, Uri uri)
            : this(collection)
        {
            this.m_baseUri = uri;
        }

        public ImageSet(ImageCollection collection, Image image)
            : this(collection)
        {
            this.m_baseImage = new Bitmap(image);
        }

        public ImageSet(ImageCollection collection, byte[] data)
            : this(collection)
        {
            this.m_tempFile.Write(data, 0, data.Length);
            this.m_baseIsBytes = true;
        }

        ~ImageSet()
        {
            this.Dispose(false);
        }
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
        private bool m_disposed;
        private void Dispose(bool disposing)
        {
            if (this.m_disposed) return;
            this.m_disposed = true;

            if (disposing)
            {
                this.m_tempFile.Dispose();

                try
                {
                    this.m_baseImage?.Dispose();
                }
                catch
                {
                }

                if (this.m_thread != null && this.m_thread.ThreadState == ThreadState.Running)
                    this.m_thread.Abort();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void InvokePropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private readonly ImageCollection m_collection;
        private readonly Stream m_tempFile;

        private readonly IDataObject m_baseDataObject;
        private readonly Uri         m_baseUri;
        private readonly bool        m_baseIsBytes;
        private readonly Image       m_baseImage;

        private Thread m_thread;

        public string TempPath { get; }
        public string Extension { get; private set; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private Statues m_status;
        public Statues Status
        {
            get => this.m_status;
            set
            {
                this.m_status = value;
                this.InvokePropertyChanged();
            }
        }

        private double m_ratio;
        public double Ratio
        {
            get => this.m_ratio;
            set
            {
                this.m_ratio = value;
                this.InvokePropertyChanged();
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public void Wait()
        {
            if (this.m_thread != null)
                this.m_thread.Join();
        }
        public void StartLoad()
        {
            this.m_thread = new Thread(this.StartLoadPriv);
            this.m_thread.Start(this.m_collection.Token);
        }

        private void StartLoadPriv(object ocancel)
        {
            var ct = (CancellationToken)ocancel;

            if (this.Status == Statues.None || this.m_tempFile.Length == 0)
            {
                var ret = false;
                try
                {
                    if (this.m_baseDataObject != null)
                    {
                        ret = ExtractFromDataObject(this.m_tempFile, this.m_baseDataObject, ct);
                    }
                    else if (this.m_baseUri != null)
                    {
                        if (this.m_baseUri.Scheme == "file")
                            ret = ExtractFromFile(this.m_tempFile, this.m_baseUri.ToString(), ct);
                        else
                            ret = ExtractFromWeb(this.m_tempFile, this.m_baseUri, ct);
                    }
                    else if (this.m_baseIsBytes)
                    {
                        ret = ExtractFromStream(this.m_tempFile, ct);
                    }
                    else if (this.m_baseImage != null)
                    {
                        ret = ExtractFromImage(this.m_baseImage);
                    }

                    if (!ret)
                        throw new OperationCanceledException();

                    if (ct.IsCancellationRequested)
                        throw new OperationCanceledException();

                    (this.Extension, this.Ratio) = Resize(this.m_tempFile, ct);

                    this.Status = Statues.Success;

                    this.m_tempFile.Position = 0;
                }
                catch (Exception ex)
                {
                    if (!(ex is OperationCanceledException))
                        SentrySdk.CaptureException(ex);

                    this.Status = Statues.Error;
                }
            }

            this.m_collection?.RaiseEvent(this);
        }

        private static readonly Regex regSrc             = new Regex(@"<img.*?src=[""'](.*?)[""'].*>", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);
        private static readonly Regex regFragmentStart   = new Regex(@"^StartFragment:(\d+)",          RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex regFragmentEnd     = new Regex(@"^EndFragment:(\d+)",            RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex regBaseUrl         = new Regex(@"http://.*?/",                   RegexOptions.Compiled | RegexOptions.Multiline);
        private static bool ExtractFromDataObject(Stream tempStream, IDataObject dataObject, CancellationToken ct)
        {
            // Images
            if (dataObject.GetDataPresent(DataFormats.Bitmap))
            {
                if (tempStream == null)
                    return true;

                try
                {
                    using (var img = (Image)dataObject.GetData(DataFormats.Bitmap))
                    {
                        if (ConvertToRegularFormat(img, tempStream))
                            return true;
                    }
                }
                catch
                {
                }
            }

            if (dataObject.GetDataPresent("PNG"))
            {
                if (tempStream == null)
                    return true;

                try
                {
                    using (var stream = (Stream)dataObject.GetData("PNG"))
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                        stream.CopyToAsync(tempStream, BufferSize, ct).GetAwaiter().GetResult();
                    }

                    if (SaveToRegularFormat(tempStream))
                        return true;
                }
                catch
                {
                }
            }

            if (dataObject.GetDataPresent("CF_DIBV5"))
            {
                if (tempStream == null)
                    return true;

                try
                {
                    using (var stream = (Stream)dataObject.GetData("CF_DIBV5"))
                    {
                        if (stream is MemoryStream memoryStream)
                        {
                            using (memoryStream)
                            {
                                memoryStream.Seek(0, SeekOrigin.Begin);
                                img = NativeMethods.CF_DIBV5ToBitmap(memoryStream.ToArray());
                            }

                            if (img != null)
                                return true;
                        }
                    }
                }
                catch
                {
                }
            }

            // Specifies the Windows device-independent bitmap
            if (dataObject.GetDataPresent(DataFormats.Dib))
            {
                if (tempStream == null)
                    return true;

                try
                {
                    img = dataObject.GetData(DataFormats.Dib) as Image;
                    if (img != null)
                    {
                        extension = GetExtension(img);
                        return true;
                    }
                }
                catch
                {
                }
            }

            if (dataObject.GetDataPresent(DataFormats.Tiff))
            {
                if (tempStream == null)
                    return true;

                try
                {
                    img = dataObject.GetData(DataFormats.Tiff, true) as Image;
                    if (img != null)
                    {
                        extension = GetExtension(img);
                        return true;
                    }
                }
                catch
                {
                }
            }

            // In Program like MS Word
            if (dataObject.GetDataPresent(DataFormats.EnhancedMetafile) &&
                dataObject.GetDataPresent(DataFormats.MetafilePicture))
            {
                if (tempStream == null)
                    return true;

                try
                {
                    var stream = dataObject.GetData(DataFormats.MetafilePicture) as Stream;
                    if (stream != null)
                    {
                        using (stream)
                        {
                            stream.Seek(0, SeekOrigin.Begin);
                            stream.CopyToAsync(tempStream, BufferSize, ct).GetAwaiter().GetResult();
                        }

                        (img, extension) = ExtractFromStream(tempStream, ct);
                        if (img != null)
                            return true;
                    }
                }
                catch
                {
                }
            }

            // HTML
            if (dataObject.GetDataPresent(DataFormats.Html))
            {
                if (tempStream == null)
                    return true;

                try
                {
                    string html;
                    string src;

                    html = (string)dataObject.GetData(DataFormats.Html, false);

                    src = regSrc.Match(html).Groups[1].Value;

                    if (!Uri.TryCreate(src, UriKind.Absolute, out Uri uri))
                    {
                        int fragmentStart = int.Parse(regFragmentStart.Match(html).Groups[1].Value);
                        int fragmentEnd = int.Parse(regFragmentEnd.Match(html).Groups[1].Value);

                        string baseUrl = regBaseUrl.Match(html, fragmentStart, fragmentEnd - fragmentStart).Groups[0].Value;

                        uri = new Uri(new Uri(baseUrl), src);
                    }

                    (img, extension) = ExtractFromWeb(tempStream, uri, ct);
                    if (img != null)
                        return true;
                }
                catch
                {
                }
            }

            // text/x-moz-url
            if (dataObject.GetDataPresent("text/x-moz-url"))
            {
                if (tempStream == null)
                    return true;

                try
                {
                    var mem = dataObject.GetData("text/x-moz-url", false) as MemoryStream;
                    using (mem)
                        (img, extension) = ExtractFromUri(tempStream, Encoding.Unicode.GetString(mem.ToArray()), ct);
                    if (img != null)
                        return true;
                }
                catch
                {
                }
            }

            // TEXT
            if (dataObject.GetDataPresent("UnicodeText"))
            {
                if (tempStream == null)
                    return true;

                try
                {
                    (img, extension) = ExtractFromUri(tempStream, dataObject.GetData("UnicodeText") as string, ct);
                    if (img != null)
                        return true;
                }
                catch
                {
                }
            }

            return false;
        }

        private static bool ExtractFromFile(Stream tempStream, string path, CancellationToken ct)
        {
            using (var fs = File.OpenRead(path))
                fs.CopyToAsync(tempStream, 16 * 1024, ct).GetAwaiter().GetResult();

            return ExtractFromStream(tempStream, ct);
        }

        private static bool ExtractFromUri(Stream tempStream, string uriStr, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(uriStr) || !Uri.TryCreate(uriStr, UriKind.Absolute, out Uri uri))
                return false;

            return ExtractFromWeb(tempStream, uri, ct);
        }

        private static bool ExtractFromWeb(Stream tempStream, Uri uri, CancellationToken ct)
        {
            var req = WebRequest.Create(uri) as HttpWebRequest;
            req.Method = "GET";
            req.Referer = uri.ToString();
            req.UserAgent = TiXMain.ProductName;

            WebResponse res;
            try
            {
                using (ct.Register(() => req.Abort(), false))
                {
                    res = req.GetResponseAsync().GetAwaiter().GetResult();
                }
            }
            catch (WebException ex)
            {
                res = ex.Response;
            }

            if (res == null)
                return false;

            using (res)
            {
                using (var stream = res.GetResponseStream())
                {
                    stream.CopyToAsync(tempStream, BufferSize, ct).GetAwaiter().GetResult();
                }
            }

            return ExtractFromStream(tempStream, ct);
        }

        private static bool ExtractFromStream(Stream tempStream, CancellationToken ct)
        {
            tempStream.Position = 0;

            switch (Signatures.CheckSignature(tempStream))
            {
                case Signatures.FileType.Psd:
                    using (var img = LoadPSD.Load(tempStream))
                        return ExtractFromImage(tempStream, img);

                case Signatures.FileType.WebP:
                    return true;

                default:
                    using (var img = Image.FromStream(tempStream))
                    {
                        ct.ThrowIfCancellationRequested();

                        if (img.RawFormat.Guid == ImageFormat.Icon.Guid)
                        {
                            try
                            {
                                tempStream.Position = 0;
                                using (var ie = new IconExtractor(tempStream, true))
                                {
                                    return ExtractFromImage(
                                        tempStream,
                                        ie.Aggregate((a, b) => ((double)a.Width * a.Height * a.BitsPerPixel) > ((double)b.Width * b.Height * b.BitsPerPixel) ? a : b).Image);
                                }
                            }
                            catch
                            {
                            }
                        }
                        
                        return ExtractFromImage(tempStream, img);
                    }
            }
        }

        private static readonly ImageCodecInfo    PngCodec = ImageCodecInfo.GetImageDecoders().First(e => e.FormatID == ImageFormat.Png.Guid);
        private static readonly EncoderParameters PngParam = new EncoderParameters(2)
        {
            Param = new EncoderParameter[]
            {
                new EncoderParameter(Encoder.ColorDepth, 32L),
                new EncoderParameter(Encoder.Compression, (long) EncoderValue.CompressionLZW),
            }
        };

        private static bool ExtractFromImage(Stream tempStream, Image image)
        {
            var guid = image.RawFormat.Guid;
            if (guid == ImageFormat.Gif.Guid ) return true;
            if (guid == ImageFormat.Png.Guid ) return true;
            if (guid == ImageFormat.Jpeg.Guid) return true;

            tempStream.SetLength(0);

            image.Save(tempStream, PngCodec, PngParam);

            return true;

            unsafe bool HasAlpha(Bitmap b, CancellationToken ct)
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

        }

        private static class NativeMethods
        {
            [StructLayout(LayoutKind.Sequential)]
            public struct BITMAPV5HEADER
            {
                public uint bV5Size;
                public int bV5Width;
                public int bV5Height;
                public ushort bV5Planes;
                public ushort bV5BitCount;
                public uint bV5Compression;
                public uint bV5SizeImage;
                public int bV5XPelsPerMeter;
                public int bV5YPelsPerMeter;
                public ushort bV5ClrUsed;
                public ushort bV5ClrImportant;
                public ushort bV5RedMask;
                public ushort bV5GreenMask;
                public ushort bV5BlueMask;
                public ushort bV5AlphaMask;
                public ushort bV5CSType;
                public IntPtr bV5Endpoints;
                public ushort bV5GammaRed;
                public ushort bV5GammaGreen;
                public ushort bV5GammaBlue;
                public ushort bV5Intent;
                public ushort bV5ProfileData;
                public ushort bV5ProfileSize;
                public ushort bV5Reserved;
            }

            public static Bitmap CF_DIBV5ToBitmap(byte[] data)
            {
                GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
                try
                {
                    var header = Marshal.PtrToStructure<BITMAPV5HEADER>(handle.AddrOfPinnedObject());

                    return new Bitmap(
                        header.bV5Width,
                        header.bV5Height,
                        -(int)(header.bV5SizeImage / header.bV5Height),
                        PixelFormat.Format32bppArgb,
                        new IntPtr(handle.AddrOfPinnedObject().ToInt32() + header.bV5Size + (header.bV5Height - 1) * (int)(header.bV5SizeImage / header.bV5Height)));
                }
                finally
                {
                    handle.Free();
                }
            }
        }
    }
}
