﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace TiX.Core
{
	public class ImageCollection
    {
        private enum DataTypes { File, IDataObject }
        private struct Data
        {
            public DataTypes DataType   { get; set; }
            public object    Object     { get; set; }
        }

        public static bool IsAvailable(DragEventArgs e)
        {
            return e == null ? false :
                (
                    e.Data.GetDataPresent(DataFormats.FileDrop) ||
					e.Data.GetDataPresent(DataFormats.Bitmap) ||
					e.Data.GetDataPresent(DataFormats.Dib) ||
					e.Data.GetDataPresent(DataFormats.Tiff) ||
					(e.Data.GetDataPresent(DataFormats.EnhancedMetafile) && e.Data.GetDataPresent(DataFormats.MetafilePict)) ||
					e.Data.GetDataPresent("HTML Format")
                );
        }

		//////////////////////////////////////////////////////////////////////////
        
		public ImageCollection()
        {
        }
        ~ImageCollection()
        {
            this.Dispose(false);
        }

        private bool _disposed = false;
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
        private void Dispose(bool disposing)
        {
            if (this._disposed) return;
            this._disposed = true;

            if (disposing)
            {
                while (this.m_image.Count > 0)
                {
                    try
                    {
                    	this.m_image.Dequeue().Dispose();
                    }
                    catch
                    {
                    }
                }
            }
        }

        private Queue<Data>     m_data  = new Queue<Data>();
        private Queue<Image>    m_image = new Queue<Image>();

        public Image Get()
        {
            if (this.m_image.Count == 0)
                this.GetImage();

            if (this.m_image.Count > 0)
                return this.m_image.Dequeue();
            
            return null;
        }

        public void Add(string path)
        {
            if (!Program.CheckFile(path)) return;

            this.m_data.Enqueue(new Data() { DataType = DataTypes.File, Object = path });
            ++this.Count;
        }
        public void Add(string[] paths)
        {
            for (int i = 0; i < paths.Length; ++i)
                this.Add(paths[i]);
        }
        public void Add(Image image)
        {
            this.AddImage(image);
            ++this.Count;
        }
        public void Add(IDataObject e)
        {
            if (e.GetDataPresent(DataFormats.FileDrop))
            {
                var paths = (string[])e.GetData(DataFormats.FileDrop);
                this.Add(paths);
            }
            else
            {
                this.m_data.Enqueue(new Data() { DataType = DataTypes.IDataObject, Object = e });
                ++this.Count;
            }
        }

        public int Count { get; private set; } 

        private static Regex regSrc             = new Regex(@"<img.*?src=[""'](.*?)[""'].*>", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);
        private static Regex regFragmentStart   = new Regex(@"^StartFragment:(\d+)", RegexOptions.Compiled | RegexOptions.Multiline);
        private static Regex regFragmentEnd     = new Regex(@"^EndFragment:(\d+)", RegexOptions.Compiled | RegexOptions.Multiline);
        private static Regex regBaseUrl         = new Regex(@"http://.*?/", RegexOptions.Compiled | RegexOptions.Multiline);
        private void GetImage()
        {
            try
            {
            	this.GetImagePri();
            }
            catch
            { }
        }
        private void GetImagePri()
        {
            var data = this.m_data.Dequeue();

            if (data.DataType == DataTypes.File)
            {
                this.GetImageFromFile(data.Object as string);
            }

            else if (data.DataType == DataTypes.IDataObject)
            {
                var idata = (IDataObject)data.Object;

                // Images
                if (idata.GetDataPresent(DataFormats.Bitmap))
                    this.AddImage((Image)idata.GetData(DataFormats.Bitmap));

                // Specifies the Windows device-independent bitmap
                else if (idata.GetDataPresent(DataFormats.Dib))
                    this.AddImage((Image)idata.GetData(DataFormats.Dib, true));

                else if (idata.GetDataPresent(DataFormats.Tiff))
                    this.AddImage((Image)idata.GetData(DataFormats.Tiff, true));

                // In Program like MS Word
                else if (idata.GetDataPresent(DataFormats.EnhancedMetafile) &&
                         idata.GetDataPresent(DataFormats.MetafilePict))
                {
                    using (var stream = (Stream)idata.GetData(DataFormats.MetafilePict))
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                        this.AddImage(new Metafile(stream));
                    }
                }

                // HTML
                else if (idata.GetDataPresent("HTML Format"))
                {
                    Uri uri;
                    string html;
                    string src;

                    html = (string)idata.GetData("HTML Format");

                    src = ImageCollection.regSrc.Match(html).Groups[1].Value;

                    if (!Uri.TryCreate(src, UriKind.Absolute, out uri))
                    {
                        int fragmentStart   = int.Parse(ImageCollection.regFragmentStart.Match(html).Groups[1].Value);
                        int fragmentEnd     = int.Parse(ImageCollection.regFragmentEnd.Match(html).Groups[1].Value);

                        string baseUrl = ImageCollection.regBaseUrl.Match(html, fragmentStart, fragmentEnd - fragmentStart).Groups[0].Value;

                        uri = new Uri(new Uri(baseUrl), src);
                    }

                    var req = WebRequest.Create(uri) as HttpWebRequest;
                    req.Referer = uri.ToString();

                    using (var res = req.GetResponse())
                    using (var stm = res.GetResponseStream())
                        this.AddImage(Image.FromStream(stm));
                }
            }
        }

        private void GetImageFromFile(string path)
        {
            switch (Path.GetExtension(path).ToLower())
            {
            case ".psd":
                {
                    using (var psd = new SimplePsd.CPSD())
                    {
                        using (var file = File.OpenRead(path))
                            psd.Load(file);

                        this.AddImage(Image.FromHbitmap(psd.HBitmap));
                    }
                }
                return;

            default:
                this.AddImage(Image.FromFile(path));
                return;
            }
        }
        private void AddImage(Image image)
        {
            var meta = image as Metafile;
            if (meta != null)
            {
                using (meta)
                {
                    var header = meta.GetMetafileHeader();
                    float scaleX = header.DpiX / 96f;
                    float scaleY = header.DpiY / 96f;

                    image = new Bitmap((int)(scaleX * meta.Width / header.DpiX * 100), (int)(scaleY * meta.Height / header.DpiY * 100), PixelFormat.Format32bppArgb);
                    using (Graphics g = Graphics.FromImage(image))
                    {
                        g.Clear(Color.Transparent);
                        g.ScaleTransform(scaleX, scaleY);
                        g.DrawImage(meta, 0, 0);
                    }
                }
            }
            
            this.m_image.Enqueue(image);
        }
	}
}
