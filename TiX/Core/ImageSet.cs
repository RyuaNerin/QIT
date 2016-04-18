﻿﻿using System;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using TiX.Utilities;

namespace TiX.Core
{
    public class ImageSet : IDisposable
    {
        private ImageSet(ImageCollection collection, int index)
        {
            this.Index = index;
            this.RawStream = new MemoryStream(3 * 1024 * 1024);
            this.m_collection = collection;
            this.IsLoading  = new ManualResetEvent(false);
        }
        
        public ImageSet(ImageCollection collection, int index, DataTypes type, object dataObject) : this(collection, index)
        {
            this.DataType   = type;
            this.DataObject = dataObject;

            this.Status = Statues.None;
        }
        public ImageSet(ImageCollection collection, int index, Image image) : this(collection, index)
        {
            this.DataType = DataTypes.Image;
            this.Image = image;

            this.Status = Statues.Success;
        }

        public void Dispose()
        {
            if (this.IsLoading  != null) this.IsLoading.Dispose();
            if (this.Image      != null) this.Image.Dispose();
            if (this.Thumbnail  != null) this.Thumbnail.Dispose();
            if (this.RawStream  != null) this.RawStream.Dispose();

            GC.SuppressFinalize(this);
        }
        
        public enum Statues
        {
            None,
            Success,
            Error
        }

        private object m_statusSync = new object();
        private Statues m_status;
        public Statues Status
        {
            get { lock (this.m_statusSync) return this.m_status; }
            set { lock (this.m_statusSync) this.m_status = value; }
        }

        private ImageCollection m_collection;
        public int Index { get; private set; }

        public DataTypes DataType { get; private set; }
        public object DataObject { get; private set; }
        public ManualResetEvent IsLoading { get; private set; }

        public Image Image { get; set; }
        public GifFrames GifFrames { get; set; }
        public Image Thumbnail { get; set; }
        public MemoryStream RawStream { get; set; }
        public string Extension { get; set; }
        public double Ratio { get; set; }

        public string TwitterMediaId { get; set; }

        public void StartLoad()
        {
            Task.Factory.StartNew(new Action<object>(this.StartLoadPriv), this.m_collection.Token);
        }

        private void StartLoadPriv(object ocancel)
        {
            var cancel = (CancellationToken)ocancel;

            if (this.Status == Statues.None || this.RawStream.Length == 0)
            {
                try
                {
                    if (this.DataType != DataTypes.Image)
                    {
                        if (this.DataType == DataTypes.File)
                            GetImageFromFile();
                        else if (this.DataType == DataTypes.IDataObject)
                            GetImageFromIData(cancel);
                    }

                    if (cancel.IsCancellationRequested)
                        throw new Exception();

                    ResizeImage.Resize(this);

                    this.Status = Statues.Success;
                }
                catch
                {
                    if (this.Image != null)
                    {
                        this.Image.Dispose();
                        this.Image = null;
                    }

                    this.Status = Statues.Error;
                }
            }

            try
            {
                this.IsLoading.Set();
                this.m_collection.RaiseEvent(this);
            }
            catch
            {
            }
        }

        private void GetImageFromFile()
        {
            var path = this.DataObject as string;

            switch (Path.GetExtension(path).ToLower())
            {
            case ".psd":
                this.Image = RyuaNerin.Drawing.LoadPSD.Load(path);
                break;

            default:
                using (var file = File.OpenRead(path))
                    file.CopyTo(this.RawStream);

                this.RawStream.Position = 0;
                this.Image = Image.FromStream(this.RawStream);
                break;
            }
        }
        
        private static Regex regSrc             = new Regex(@"<img.*?src=[""'](.*?)[""'].*>", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);
        private static Regex regFragmentStart   = new Regex(@"^StartFragment:(\d+)", RegexOptions.Compiled | RegexOptions.Multiline);
        private static Regex regFragmentEnd     = new Regex(@"^EndFragment:(\d+)", RegexOptions.Compiled | RegexOptions.Multiline);
        private static Regex regBaseUrl         = new Regex(@"http://.*?/", RegexOptions.Compiled | RegexOptions.Multiline);
        private void GetImageFromIData(CancellationToken cancel)
        {
            var idata = this.DataObject as IDataObject;

            // Images
            if (idata.GetDataPresent(DataFormats.Bitmap))
                this.Image = (Image)idata.GetData(DataFormats.Bitmap);

            // Specifies the Windows device-independent bitmap
            else if (idata.GetDataPresent(DataFormats.Dib))
                this.Image = (Image)idata.GetData(DataFormats.Dib, true);

            else if (idata.GetDataPresent(DataFormats.Tiff))
                this.Image = (Image)idata.GetData(DataFormats.Tiff, true);

            // In Program like MS Word
            else if (idata.GetDataPresent(DataFormats.EnhancedMetafile) &&
                     idata.GetDataPresent(DataFormats.MetafilePict))
            {
                using (var stream = (Stream)idata.GetData(DataFormats.MetafilePict))
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    this.Image = Image.FromStream(stream);
                }
            }

            // HTML
            else if (idata.GetDataPresent(DataFormats.Html))
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

                if (!Http.GetResponse(uri, this.RawStream, cancel))
                {
                    this.RawStream.SetLength(0);
                    this.RawStream.Capacity = 0;
                }

                if (this.RawStream.Length > 0)
                {
                    this.RawStream.Position = 0;
                    this.Image = Image.FromStream(this.RawStream);
                }
            }
        }
    }
}