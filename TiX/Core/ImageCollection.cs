using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows;
using TiX.Utilities;

namespace TiX.Core
{
    internal class ImageCollection : List<ImageSet>, IDisposable
    {
        private readonly CancellationTokenSource m_cancel = new CancellationTokenSource();
        public CancellationToken Token => this.m_cancel.Token;

        public ImageCollection()
        {
        }
        ~ImageCollection()
        {
            this.Dispose(false);
        }
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool m_disposed = false;
        protected void Dispose(bool disposing)
        {
            if (this.m_disposed)
                return;
            this.m_disposed = true;

            if (disposing)
            {
                this.m_cancel.Cancel();
                this.m_cancel.Dispose();

                for (int i = 0; i < this.Count; ++i)
                    this[i].Dispose();
            }
        }
        
        public event EventHandler LoadedImage;
        public void RaiseEvent(ImageSet sender)
        {
            this.LoadedImage?.Invoke(sender, new EventArgs());
        }

        public void Add(IDataObject e)
        {
            if (e == null)
                return;

            if (e.GetDataPresent(DataFormats.FileDrop))
            {
                foreach (var path in (string[])e.GetData(DataFormats.FileDrop))
                    if (Uri.TryCreate(path, UriKind.RelativeOrAbsolute, out Uri uri))
                        this.Add(uri);
            }
            else
            {
                this.Add(new ImageSet(this, e));
            }
        }
        public void Add(byte[] rawData)
        {
            if (rawData == null)
                return;

            this.Add(new ImageSet(this, rawData));
        }
        public void Add(string path)
        {
            if (path == null)
                return;

            if (!TiXMain.CheckFile(path))
                return;

            if (!Uri.TryCreate(path, UriKind.RelativeOrAbsolute, out Uri uri))
                return;

            this.Add(new ImageSet(this, uri));
        }
        public void Add(Uri uri)
        {
            if (uri == null)
                return;

            if (!TiXMain.CheckFile(uri))
                return;

            this.Add(new ImageSet(this, uri));
        }
        public void Add(Image image)
        {
            if (image == null)
                return;

            this.Add(new ImageSet(this, image));
        }
        public void Add(IEnumerable<string> paths, bool orderByPath)
        {
            if (paths == null)
                return;

            if (orderByPath)
                foreach (var path in paths.OrderBy(ee => ee, ExtendStringComparer.Instance))
                    this.Add(path);
            else
                foreach (var path in paths)
                    this.Add(path);
        }
        public void Add(IEnumerable<Uri> uris, bool orderByPath)
        {
            if (uris == null)
                return;

            if (orderByPath)
                foreach (var uri in uris.OrderBy(ee => ee.IsFile ? ee.LocalPath : ee.AbsolutePath, ExtendStringComparer.Instance))
                    this.Add(uri);
            else
                foreach (var uri in uris)
                    this.Add(uri);
        }
        public void Add(IEnumerable<byte[]> datas)
        {
            if (datas == null)
                return;

            foreach (var data in datas)
                this.Add(data);
        }
        public void Add(IEnumerable<Image> images)
        {
            if (images == null)
                return;

            foreach (var image in images)
                this.Add(image);
        }

        protected new void Add(ImageSet value)
        {
            base.Add(value);
        }
    }
}
