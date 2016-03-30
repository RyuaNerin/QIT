﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TiX.Core
{
    /*
     * ImageCollection 메모
     * 
     * 호출 순서
     * Add -> GetImage -> Get
     * 
     * GetImage
     * 토큰 생성
     * Task 생성
     * -> 쓰레드 내부에서 Parallel << 이때 토큰이 들어감
     * --> 토큰이 만료되면 웹에서 이미지 다운로드를 그만두고 Return.
     *
     * Dispose
     * > 토큰을 만기시킴
     * > Task 가 만기될때까지 쓰레드 락
     * 쓰레드가 멈추면 List 를 돌면서 항목 Dispose
     * 
     * 주의점
     * 갑작스런 Dispose 호출 시 오랜 시간이 걸릴 수 있으므로 비동기로 호출하는게 좋음.
    */
	public class ImageCollection : IDisposable
    {
        private enum DataTypes { None, File, IDataObject }
        private class Data
        {
            private Data()
            {
                this.ImageSet   = new ImageSet();
            }
            public Data(DataTypes type, object dataObject) : this()
            {
                this.DataType   = type;
                this.DataObject = dataObject;
                this.Event      = new ManualResetEvent(false);

            }
            public Data(Image image) : this()
            {
                this.ImageSet.Image = image;
            }

            public DataTypes        DataType    { get; private set; }
            public object           DataObject  { get; private set; }
            public ImageSet         ImageSet    { get; set; }
            public ManualResetEvent Event       { get; private set; }
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
                if (this.m_cancel != null && this.m_task.Status != TaskStatus.RanToCompletion)
                {
                    this.m_cancel.Cancel();
                    this.m_task.Wait();
                }

                lock (this.m_data)
                {
                    for (int i = 0; i < this.m_data.Count; ++i)
                    {
                        if (this.m_data[i].ImageSet != null)
                    	    this.m_data[i].ImageSet.Dispose();

                        if (this.m_data[i].Event != null)
                            this.m_data[i].Event.Dispose();
                    }
                }
            }
        }

        private List<Data> m_data = new List<Data>();
        
        public int Count { get { return this.m_data.Count; } }

        private Task m_task;
        private CancellationTokenSource m_cancel;

        public void Get(int index, out ImageSet image)
        {
            var data = this.m_data[index];

            if (data.Event != null)
                data.Event.WaitOne();

            image   = data.ImageSet;
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
                this.m_data.Add(new Data(DataTypes.IDataObject, e));
            }
        }
        public void Add(string path)
        {
            if (!Program.CheckFile(path)) return;

            this.m_data.Add(new Data(DataTypes.File, path));
        }
        public void Add(string[] paths)
        {
            for (int i = 0; i < paths.Length; ++i)
                this.Add(paths[i]);
        }
        public void Add(Image image)
        {
            if (image == null) return;

            this.m_data.Add(new Data(image));
        }

        public void GetImage()
        {
            this.m_cancel = new CancellationTokenSource();
            this.m_task = Task.Factory.StartNew(this.GetImageTask, this.m_cancel.Token);
        }

        private void GetImageTask(object state)
        {
            var token = (CancellationToken)state;
            var option = new ParallelOptions();
            option.CancellationToken = token;

            try
            {
                Parallel.ForEach(this.m_data, option, GetImageParallel);
            }
            catch
            {
            }
        }
        
        private static Regex regSrc             = new Regex(@"<img.*?src=[""'](.*?)[""'].*>", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);
        private static Regex regFragmentStart   = new Regex(@"^StartFragment:(\d+)", RegexOptions.Compiled | RegexOptions.Multiline);
        private static Regex regFragmentEnd     = new Regex(@"^EndFragment:(\d+)", RegexOptions.Compiled | RegexOptions.Multiline);
        private static Regex regBaseUrl         = new Regex(@"http://.*?/", RegexOptions.Compiled | RegexOptions.Multiline);
        private static void GetImageParallel(Data data, ParallelLoopState state)
        {
            try
            {
                if (data.DataType == DataTypes.None)
                    data.ImageSet = data.ImageSet;
                
                else if (data.DataType == DataTypes.File)
                    GetImageFromFile(data);

                else if (data.DataType == DataTypes.IDataObject)
                    GetImageFromIData(data, state);
            }
            catch
            { }

            if (data.Event != null)
                data.Event.Set();
        }
        private static void GetImageFromFile(Data data)
        {
            var path = data.DataObject as string;

            switch (Path.GetExtension(path).ToLower())
            {
            case ".psd":
                using (var psd = new SimplePsd.CPSD())
                {                    
                    using (var file = File.OpenRead(path))
                        psd.Load(file);

                    data.ImageSet.Image = Image.FromHbitmap(psd.HBitmap);
                }
                break;

            default:
                using (var file = File.OpenRead(path))
                    file.CopyTo(data.ImageSet.RawStream);

                data.ImageSet.RawStream.Position = 0;
                data.ImageSet.Image = Image.FromStream(data.ImageSet.RawStream);
                break;
            }
        }
        private static void GetImageFromIData(Data data, ParallelLoopState state)
        {
            var idata = data.DataObject as IDataObject;

            // Images
            if (idata.GetDataPresent(DataFormats.Bitmap))
                data.ImageSet.Image = (Image)idata.GetData(DataFormats.Bitmap);

            // Specifies the Windows device-independent bitmap
            else if (idata.GetDataPresent(DataFormats.Dib))
                data.ImageSet.Image = (Image)idata.GetData(DataFormats.Dib, true);

            else if (idata.GetDataPresent(DataFormats.Tiff))
                data.ImageSet.Image = (Image)idata.GetData(DataFormats.Tiff, true);

            // In Program like MS Word
            else if (idata.GetDataPresent(DataFormats.EnhancedMetafile) &&
                        idata.GetDataPresent(DataFormats.MetafilePict))
            {
                using (var stream = (Stream)idata.GetData(DataFormats.MetafilePict))
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    data.ImageSet.Image = new Metafile(stream);
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
                req.Referer = new Uri(uri, "/").ToString();

                using (var res = req.GetResponse() as HttpWebResponse)
                using (var stm = res.GetResponseStream())
                {
                    int rd;
                    var buff = new byte[40960]; // 40k

                    while (!state.IsStopped && !state.ShouldExitCurrentIteration && (rd = stm.Read(buff, 0, 40960)) > 0)
                        data.ImageSet.RawStream.Write(buff, 0, rd);
                }

                if (data.ImageSet.RawStream.Length > 0)
                {
                    data.ImageSet.RawStream.Position = 0;
                    data.ImageSet.Image = Image.FromStream(data.ImageSet.RawStream);
                }
            }
        }
	}
}
