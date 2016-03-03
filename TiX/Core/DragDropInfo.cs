﻿using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace TiX.Core
{
	public class DragDropInfo
    {
        public enum DataTypes { File, Image, IDataObject }

        public static bool isAvailable(DragEventArgs e)
        {
            return
                (
                    e.Data.GetDataPresent(DataFormats.FileDrop) ||
					e.Data.GetDataPresent(DataFormats.Bitmap) ||
					e.Data.GetDataPresent(DataFormats.Dib) ||
					(e.Data.GetDataPresent(DataFormats.EnhancedMetafile) && e.Data.GetDataPresent(DataFormats.MetafilePict)) ||
					e.Data.GetDataPresent("HTML Format")
                );
        }

		//////////////////////////////////////////////////////////////////////////
        
        private static Regex regSrc             = new Regex(@"<img.*?src=[""'](.*?)[""'].*>", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);
        private static Regex regFragmentStart   = new Regex(@"^StartFragment:(\d+)", RegexOptions.Compiled | RegexOptions.Multiline);
        private static Regex regFragmentEnd     = new Regex(@"^EndFragment:(\d+)", RegexOptions.Compiled | RegexOptions.Multiline);
        private static Regex regBaseUrl         = new Regex(@"http://.*?/", RegexOptions.Compiled | RegexOptions.Multiline);
		public static DragDropInfo Create( string path )
		{
            var info = new DragDropInfo();
            info.m_object = path;
            info.DataType = DataTypes.File;
            return info;
		}
		public static DragDropInfo Create( Image image )
        {
            var info = new DragDropInfo();
            info.m_object = image;
            info.DataType = DataTypes.Image;
            return info;
		}
		public static DragDropInfo Create( DragEventArgs e )
        {
            var info = new DragDropInfo();
            info.m_object = e.Data;
            info.DataType = DataTypes.IDataObject;
            return info;
		}
        
		private DragDropInfo()
        { }
        ~DragDropInfo()
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
                if (this.DataType == DataTypes.Image)
                    (this.m_object as Image).Dispose();
            }
        }
        
		public DataTypes DataType { get; private set; } 
        private object m_object;

        public Image GetImage()
        {
            Image img = null;

            if (this.DataType == DataTypes.Image)
                img = (Image)this.m_object;

            else if (this.DataType == DataTypes.File)
            {
                var path = (string)this.m_object;

                if (Path.GetExtension(path).ToLower() == ".psd")
                {
                    SimplePsd.CPSD psd = new SimplePsd.CPSD();
                    using (var file = File.OpenRead(path))
                        psd.Load(file);

                    img = Image.FromHbitmap(psd.HBitmap);
                }
                else
                    img = Image.FromFile(path);
            }

            else if (this.DataType == DataTypes.IDataObject)
            {
                var data = (IDataObject)this.m_object;

                // Images
                if (data.GetDataPresent(DataFormats.Bitmap))
                    img = (Image)data.GetData(DataFormats.Bitmap);

                // Specifies the Windows device-independent bitmap
                else if (data.GetDataPresent(DataFormats.Dib))
                    img = (Image)data.GetData(DataFormats.Dib, true);

                // In Program like MS Word
                else if (data.GetDataPresent(DataFormats.EnhancedMetafile) &&
                         data.GetDataPresent(DataFormats.MetafilePict))
                {
                    using (var stream = (Stream)data.GetData(DataFormats.MetafilePict))
                    {
                        stream.Seek(0, SeekOrigin.Begin);

                        img = new Metafile(stream);
                    }
                }

                // HTML
                else if (data.GetDataPresent("HTML Format"))
                {
                    Uri uri;
                    string html;
                    string src;

                    html = (string)data.GetData("HTML Format");

                    src = DragDropInfo.regSrc.Match(html).Groups[1].Value;

                    if (!Uri.TryCreate(src, UriKind.Absolute, out uri))
                    {
                        int fragmentStart   = int.Parse(DragDropInfo.regFragmentStart.Match(html).Groups[1].Value);
                        int fragmentEnd     = int.Parse(DragDropInfo.regFragmentEnd.Match(html).Groups[1].Value);

                        string baseUrl = DragDropInfo.regBaseUrl.Match(html, fragmentStart, fragmentEnd - fragmentStart).Groups[0].Value;

                        uri = new Uri(new Uri(baseUrl), src);
                    }

                    var req = WebRequest.Create(uri) as HttpWebRequest;
                    req.Referer = uri.ToString();

                    using (var res = req.GetResponse())
                    using (var stm = res.GetResponseStream())
                        img = Image.FromStream(stm);
                }
            }

            return img;
        }
	}
}
