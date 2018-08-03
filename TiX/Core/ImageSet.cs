using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using CloudFlareUtilities;
using TiX.Utilities;

namespace TiX.Core
{
    internal class ImageSet : IDisposable
    {
        public static bool IsAvailable(DragEventArgs e)
        {
            if (e == null)
                return false;

#if DEBUG
            var dic = new System.Collections.Generic.Dictionary<string, object>();
            foreach (var format in e.Data.GetFormats())
            {
                var data = e.Data.GetData(format);

                if (data is MemoryStream mem)
                {
                    var bdata = mem.ToArray();
                    mem.Dispose();
                    data = Encoding.Unicode.GetString(bdata);
                }
                dic.Add(format, data);
            }
#endif

            return 
                   e.Data.GetDataPresent(DataFormats.FileDrop) ||
                   e.Data.GetDataPresent(DataFormats.Bitmap) ||
                   e.Data.GetDataPresent(DataFormats.Dib) ||
                   e.Data.GetDataPresent(DataFormats.Tiff) ||
                   (
                       e.Data.GetDataPresent(DataFormats.EnhancedMetafile) &&
                       e.Data.GetDataPresent(DataFormats.MetafilePict)
                   ) ||
                   e.Data.GetDataPresent(DataFormats.Html) ||
                   e.Data.GetDataPresent("text/x-moz-url") ||
                   e.Data.GetDataPresent("UnicodeText");
        }

        private ImageSet(ImageCollection collection, int index, DataTypes dataType)
        {
            this.m_dataType   = dataType;
            this.m_collection = collection;
            this.m_index      = index;

            int tries = 3;

            do
            {
                try
                {
                    this.m_tempPath = Path.GetTempFileName();
                    this.m_rawStream = new FileStream(
                        this.m_tempPath,
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

            if (this.m_rawStream == null)
                this.m_rawStream = new MemoryStream(3 * 1024 * 1024);
        }

        public ImageSet(ImageCollection collection, int index, IDataObject dataObject)
            : this(collection, index, DataTypes.IDataObject)
        {
            this.m_DataObject = dataObject;
        }

        public ImageSet(ImageCollection collection, int index, Uri uri)
            : this(collection, index, DataTypes.Uri)
        {
            this.m_DataObject = uri;
        }

        public ImageSet(ImageCollection collection, int index, Image image)
            : this(collection, index, DataTypes.Image)
        {
            this.Image = image;
        }

        public ImageSet(ImageCollection collection, int index, byte[] data)
            : this(collection, index, DataTypes.Bytes)
        {
            this.m_DataObject = data;
            this.m_rawStream.Write(data, 0, data.Length);
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
            if (this.m_disposed)
                return;
            this.m_disposed = true;

            if (this.Image     != null) this.Image    .Dispose();
            if (this.Thumbnail != null) this.Thumbnail.Dispose();
            if (this.RawStream != null) this.RawStream.Dispose();

            if (this.m_thread != null && this.m_thread.ThreadState == ThreadState.Running)
                this.m_thread.Abort();
        }
        
        public enum Statues
        {
            None,
            Success,
            Error
        }

        private readonly ImageCollection m_collection;

        private readonly string m_tempPath;

        private readonly int m_index;
        public int Index => this.m_index;

        private readonly Stream m_rawStream;
        public Stream RawStream => this.m_rawStream;

        private readonly DataTypes m_dataType;
        public DataTypes DataType => this.m_dataType;

        private readonly object m_DataObject;
        public object DataObject => this.m_DataObject;

        private volatile Statues m_status;
        public Statues Status
        {
            get => this.m_status;
            set => this.m_status = value;
        }

        public Image     Image     { get; set; }
        public GifFrames GifFrames { get; set; }
        public Image     Thumbnail { get; set; }
        public string    Extension { get; set; }
        public double    Ratio     { get; set; }

        private Thread m_thread;

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

            if (this.Status == Statues.None || this.m_rawStream.Length == 0)
            {
                try
                {
                    switch (this.DataType)
                    {
                        case DataTypes.Uri:
                            {
                                var uri = (Uri)this.DataObject;
                                if (uri.Scheme == "file")
                                    GetImageFromFile(uri.LocalPath);
                                else
                                    GetImageFromHttp(uri, cancel);
                            }
                            break;

                        case DataTypes.IDataObject:
                            GetImageFromIData(cancel);
                            break;

                        case DataTypes.Bytes:
                            {
                                using (var mem = new MemoryStream((byte[])this.DataObject))
                                    mem.CopyTo(this.m_rawStream);

                                GetImageFromStream();
                            }
                            break;
                    }

                    if (cancel.IsCancellationRequested)
                        throw new Exception("_");

                    ResizeImage.Resize(this);

                    this.Status = Statues.Success;
                }
                catch (Exception ex)
                {
                    if (ex.Message != "_")
                        CrashReport.Error(ex, null);

                    if (this.Image != null)
                    {
                        this.Image.Dispose();
                        this.Image = null;
                    }

                    this.Status = Statues.Error;
                }
            }

            if (this.m_collection != null)
            {
                try
                {
                    this.m_collection.RaiseEvent(this);
                }
                catch
                {
                }
            }
        }

        private void GetImageFromFile(string path)
        {
            using (var file = File.OpenRead(path))
                file.CopyTo(this.m_rawStream);

            this.GetImageFromStream();
        }
        
        private static Regex regSrc             = new Regex(@"<img.*?src=[""'](.*?)[""'].*>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        private static Regex regFragmentStart   = new Regex(@"^StartFragment:(\d+)", RegexOptions.Multiline);
        private static Regex regFragmentEnd     = new Regex(@"^EndFragment:(\d+)", RegexOptions.Multiline);
        private static Regex regBaseUrl         = new Regex(@"http://.*?/", RegexOptions.Multiline);
        private void GetImageFromIData(CancellationToken cancel)
        {
            var idata = this.DataObject as IDataObject;

            // Images
            if (idata.GetDataPresent(DataFormats.Bitmap))
            {
                this.Image = (Image)idata.GetData(DataFormats.Bitmap);
                return;
            }

            // Specifies the Windows device-independent bitmap
            if (idata.GetDataPresent(DataFormats.Dib))
            {
                this.Image = (Image)idata.GetData(DataFormats.Dib, true);
                return;
            }

            if (idata.GetDataPresent(DataFormats.Tiff))
            {
                this.Image = (Image)idata.GetData(DataFormats.Tiff, true);
                return;
            }

            // In Program like MS Word
            if (idata.GetDataPresent(DataFormats.EnhancedMetafile) &&
                     idata.GetDataPresent(DataFormats.MetafilePict))
            {
                using (var stream = (Stream)idata.GetData(DataFormats.MetafilePict))
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    this.Image = Image.FromStream(stream);
                }
                return;
            }

            // HTML
            if (idata.GetDataPresent(DataFormats.Html))
            {
                Uri uri;
                string html;
                string src;

                html = (string)idata.GetData(DataFormats.Html, false);

                src = regSrc.Match(html).Groups[1].Value;

                if (!Uri.TryCreate(src, UriKind.Absolute, out uri))
                {
                    int fragmentStart   = int.Parse(regFragmentStart.Match(html).Groups[1].Value);
                    int fragmentEnd     = int.Parse(regFragmentEnd.Match(html).Groups[1].Value);

                    string baseUrl = regBaseUrl.Match(html, fragmentStart, fragmentEnd - fragmentStart).Groups[0].Value;

                    uri = new Uri(new Uri(baseUrl), src);
                }

                if (GetImageFromHttp(uri, cancel))
                    return;
            }

            // text/x-moz-url
            if (idata.GetDataPresent("text/x-moz-url"))
            {
                var mem = idata.GetData("text/x-moz-url", false) as MemoryStream;
                if (mem != null)
                    using (mem)
                        if (this.GetImageFromUri(Encoding.Unicode.GetString(mem.ToArray()), cancel))
                            return;
            }

            // TEXT
            if (idata.GetDataPresent("UnicodeText"))
            {
                var data = idata.GetData("UnicodeText") as string;
                if (this.GetImageFromUri(data, cancel))
                    return;
            }
        }

        private bool GetImageFromUri(string uriStr, CancellationToken cancel)
        {
            if (string.IsNullOrWhiteSpace(uriStr) ||
                !Uri.TryCreate(uriStr, UriKind.Absolute, out Uri uri))
                return false;

            return GetImageFromHttp(uri, cancel);
        }

        private bool GetImageFromHttp(Uri uri, CancellationToken cancel)
        {
            using (var handler = new ClearanceHandler())
            using (var client = new HttpClient(handler))
            {
                try
                {
                    var getStream = client.GetStreamAsync(uri);

                    do
                    {
                        if (cancel.IsCancellationRequested)
                        {
                            client.CancelPendingRequests();
                            break;
                        }
                    } while (!getStream.Wait(100));

                    getStream.Wait();

                    if (getStream.IsCanceled)
                        return false;

                    if (getStream.IsCompleted)
                    {
                        this.m_rawStream.SetLength(0);
                    }

                    getStream.Result.CopyTo(this.m_rawStream);

                    this.m_rawStream.Position = 0;

                    GetImageFromStream();

                    return true;
                }
                catch
                {
                    this.m_rawStream.SetLength(0);
                }
            }

            return false;
        }

        private void GetImageFromStream()
        {
            this.m_rawStream.Position = 0;
            if (Signatures.CheckSignature(this.m_rawStream, Signatures.WebP))
            {
                this.m_rawStream.Position = 0;
                var buff = new byte[this.m_rawStream.Length];
                this.m_rawStream.Read(buff, 0, buff.Length);

                var decoder = new Imazen.WebP.SimpleDecoder();
                this.Image = decoder.DecodeFromBytes(buff, buff.Length);

                return;
            }

            this.m_rawStream.Position = 0;
            if (Signatures.CheckSignature(this.m_rawStream, Signatures.Psd))
            {
                this.m_rawStream.Position = 0;
                this.Image = RyuaNerin.Drawing.LoadPSD.Load(this.m_rawStream);

                return;
            }

            this.m_rawStream.Position = 0;
            {
                this.Image = Image.FromStream(this.m_rawStream);

                if (this.Image.RawFormat.Guid == ImageFormat.Icon.Guid)
                {
                    this.Image.Dispose();

                    this.m_rawStream.Position = 0;
                    using (var ie = new IconExtractor(this.m_rawStream, true))
                    {
                        var img = ie.OrderByDescending(e => (double)e.Width * e.Height * e.BitsPerPixel).First().Image;
                        this.Image = img.Clone(new Rectangle(Point.Empty, img.Size), PixelFormat.Format32bppArgb);
                    }
                }
            }
        }
    }
}
