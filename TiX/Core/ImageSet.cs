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
using WebPWrapper;

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
        {
            if (e == null)
                return false;

            return GetImageFromDataObject(null, e, CancellationToken.None, out _, out _);
        }

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

        public string    TempPath  { get; }
        public string    Extension { get; private set; }
        public double    Ratio     { get; private set; }

        private Thread m_thread;

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
            var cancel = (CancellationToken)ocancel;

            if (this.Status == Statues.None || this.m_tempFile.Length == 0)
            {
                Image bitmap = null;
                string extension = null;
                try
                {
                    if (this.m_baseDataObject != null)
                    {
                        _ = GetImageFromDataObject(this.m_tempFile, this.m_baseDataObject, cancel, out bitmap, out extension);
                    }
                    else if (this.m_baseUri != null)
                    {
                        if (this.m_baseUri.Scheme == "file")
                            (bitmap, extension) = GetImageFromFile(this.m_tempFile, this.m_baseUri.ToString(), cancel);
                        else
                            (bitmap, extension) = GetImageFromHttp(this.m_tempFile, this.m_baseUri, cancel);
                    }
                    else if (this.m_baseIsBytes)
                    {
                        (bitmap, extension) = GetImageFromStream(this.m_tempFile, cancel);
                    }
                    else if (this.m_baseImage != null)
                    {
                        bitmap = this.m_baseImage;
                        extension = GetExtension(bitmap);
                    }

                    if (cancel.IsCancellationRequested)
                        throw new OperationCanceledException();

                    using (bitmap)
                    {
                        this.Resize(bitmap, extension);

                        this.Status = Statues.Success;
                    }

                    this.m_tempFile.Position = 0;
                }
                catch (Exception ex)
                {
                    if (!(ex is OperationCanceledException))
                        SentrySdk.CaptureException(ex);

                    this.Status = Statues.Error;
                }
                finally
                {
                    bitmap?.Dispose();
                }
            }

            this.m_collection?.RaiseEvent(this);
        }

        private static readonly Regex regSrc             = new Regex(@"<img.*?src=[""'](.*?)[""'].*>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        private static readonly Regex regFragmentStart   = new Regex(@"^StartFragment:(\d+)", RegexOptions.Multiline);
        private static readonly Regex regFragmentEnd     = new Regex(@"^EndFragment:(\d+)", RegexOptions.Multiline);
        private static readonly Regex regBaseUrl         = new Regex(@"http://.*?/", RegexOptions.Multiline);
        private static bool GetImageFromDataObject(
            Stream tempStream,
            IDataObject dataObject,
            CancellationToken ct,
            out Image img,
            out string extension)
        {
            img = null;
            extension = null;

            // Images
            if (dataObject.GetDataPresent(DataFormats.Bitmap))
            {
                if (tempStream == null)
                    return true;

                img = dataObject.GetData(DataFormats.Bitmap) as Image;
                if (img != null)
                {
                    extension = GetExtension(img);
                    return true;
                }
            }

            if (dataObject.GetDataPresent("CF_DIBV5"))
            {
                if (tempStream == null)
                    return true;

                using (var stream = dataObject.GetData("CF_DIBV5") as MemoryStream)
                {
                    img = NativeMethods.CF_DIBV5ToBitmap(stream.ToArray());
                }
                if (img != null)
                {
                    extension = GetExtension(img);
                    return true;
                }
            }

            // Specifies the Windows device-independent bitmap
            if (dataObject.GetDataPresent(DataFormats.Dib))
            {
                if (tempStream == null)
                    return true;

                img = dataObject.GetData(DataFormats.Dib) as Image;
                if (img != null)
                {
                    extension = GetExtension(img);
                    return true;
                }
            }

            if (dataObject.GetDataPresent(DataFormats.Tiff))
            {
                if (tempStream == null)
                    return true;

                img = dataObject.GetData(DataFormats.Tiff, true) as Image;
                if (img != null)
                {
                    extension = GetExtension(img);
                    return true;
                }
            }

            // In Program like MS Word
            if (dataObject.GetDataPresent(DataFormats.EnhancedMetafile) &&
                dataObject.GetDataPresent(DataFormats.MetafilePicture))
            {
                if (tempStream == null)
                    return true;

                using (var stream = dataObject.GetData(DataFormats.MetafilePicture) as Stream)
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    stream.CopyToAsync(tempStream, BufferSize, ct).GetAwaiter().GetResult();
                }

                (img, extension) = GetImageFromStream(tempStream, ct);
                if (img != null)
                    return true;
            }

            // HTML
            if (dataObject.GetDataPresent(DataFormats.Html))
            {
                if (tempStream == null)
                    return true;

                string html;
                string src;

                html = (string)dataObject.GetData(DataFormats.Html, false);

                src = regSrc.Match(html).Groups[1].Value;

                if (!Uri.TryCreate(src, UriKind.Absolute, out Uri uri))
                {
                    int fragmentStart   = int.Parse(regFragmentStart.Match(html).Groups[1].Value);
                    int fragmentEnd     = int.Parse(regFragmentEnd.Match(html).Groups[1].Value);

                    string baseUrl = regBaseUrl.Match(html, fragmentStart, fragmentEnd - fragmentStart).Groups[0].Value;

                    uri = new Uri(new Uri(baseUrl), src);
                }

                (img, extension) = GetImageFromHttp(tempStream, uri, ct);
                if (img != null)
                    return true;
            }

            // text/x-moz-url
            if (dataObject.GetDataPresent("text/x-moz-url"))
            {
                if (tempStream == null)
                    return true;

                var mem = dataObject.GetData("text/x-moz-url", false) as MemoryStream;
                using (mem)
                    (img, extension) = GetImageFromUri(tempStream, Encoding.Unicode.GetString(mem.ToArray()), ct);
                if (img != null)
                    return true;
            }

            // TEXT
            if (dataObject.GetDataPresent("UnicodeText"))
            {
                if (tempStream == null)
                    return true;

                (img, extension) = GetImageFromUri(tempStream, dataObject.GetData("UnicodeText") as string, ct);
                if (img != null)
                    return true;
            }

            return false;
        }

        private static (Image, string) GetImageFromFile(Stream tempStream, string path, CancellationToken ct)
        {
            using (var fs = File.OpenRead(path))
                fs.CopyToAsync(tempStream, 16 * 1024, ct).GetAwaiter().GetResult();

            return GetImageFromStream(tempStream, ct);
        }

        private static (Image, string) GetImageFromUri(Stream tempStream, string uriStr, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(uriStr) || !Uri.TryCreate(uriStr, UriKind.Absolute, out Uri uri))
                return (null, null);

            return GetImageFromHttp(tempStream, uri, ct);
        }

        private static (Image, string) GetImageFromHttp(Stream tempStream, Uri uri, CancellationToken ct)
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
                    ct.ThrowIfCancellationRequested();
                }
            }
            catch (WebException ex)
            {
                res = ex.Response;
            }

            if (res == null)
                return (null, null);

            using (res)
            {
                using (var stream = res.GetResponseStream())
                {
                    stream.CopyToAsync(tempStream, BufferSize, ct).GetAwaiter().GetResult();
                }
            }

            return GetImageFromStream(tempStream, ct);
        }

        private static (Image, string) GetImageFromStream(Stream tempStream, CancellationToken ct)
        {
            tempStream.Position = 0;
            if (Signatures.CheckSignature(tempStream, Signatures.WebP))
            {
                tempStream.Position = 0;
                var buff = new byte[tempStream.Length];
                tempStream.Read(buff, 0, buff.Length);

                var webp = new WebP();
                return (webp.Decode(buff), ".webp");
            }

            tempStream.Position = 0;
            if (Signatures.CheckSignature(tempStream, Signatures.Psd))
            {
                tempStream.Position = 0;
                return (LoadPSD.Load(tempStream), null);
            }

            tempStream.Position = 0;
            {
                var img = Image.FromStream(tempStream);

                if (img.RawFormat.Guid == ImageFormat.Icon.Guid)
                {
                    img.Dispose();

                    tempStream.Position = 0;
                    using (var ie = new IconExtractor(tempStream, true))
                    {
                        img = ie.OrderByDescending(e => (double)e.Width * e.Height * e.BitsPerPixel)
                                .FirstOrDefault()
                                .Image
                                ?.Clone(new Rectangle(System.Drawing.Point.Empty, img.Size),
                                        PixelFormat.Format32bppArgb);
                    }
                }

                return (img, GetExtension(img));
            }
        }

        private static string GetExtension(Image image)
        {
            var guid = image.RawFormat.Guid;
            if (guid == ImageFormat.Gif.Guid)  return ".gif";
            if (guid == ImageFormat.Png.Guid)  return ".png";
            if (guid == ImageFormat.Jpeg.Guid) return ".jpg";

            return null;
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
