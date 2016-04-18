﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using TiX.Utilities;

namespace TiX.Core
{
    public enum DataTypes { Image, File, IDataObject }

    public class ImageCollection : List<ImageSet>, IDisposable
    {
        public static bool IsAvailable(DragEventArgs e)
        {
            return e == null ? false :
                (
                    e.Data.GetDataPresent(DataFormats.FileDrop) ||
                    e.Data.GetDataPresent(DataFormats.Bitmap) ||
                    e.Data.GetDataPresent(DataFormats.Dib) ||
                    e.Data.GetDataPresent(DataFormats.Tiff) ||
                    (e.Data.GetDataPresent(DataFormats.EnhancedMetafile) && e.Data.GetDataPresent(DataFormats.MetafilePict)) ||
                    e.Data.GetDataPresent(DataFormats.Html)
                );
        }

        //////////////////////////////////////////////////////////////////////////

        public ImageCollection()
        {
            this.m_cancel = new CancellationTokenSource();
        }

        public void Dispose()
        {
            if (this.m_cancel != null)
            {
                this.m_cancel.Cancel();
                this.m_cancel.Dispose();
            }

            for (int i = 0; i < this.Count; ++i)
                this[i].Dispose();

            GC.SuppressFinalize(this);
        }
        
        public event EventHandler LoadedImage;
        public void RaiseEvent(ImageSet sender)
        {
            if (this.LoadedImage != null)
                this.LoadedImage.Invoke(sender, new EventArgs());
        }

        private readonly CancellationTokenSource m_cancel;
        public CancellationToken Token { get { return this.m_cancel.Token; } }

        public void Add(IDataObject e)
        {
            if (e.GetDataPresent(DataFormats.FileDrop))
            {
                this.Add((string[])e.GetData(DataFormats.FileDrop));
            }
            else
            {
                this.Add(new ImageSet(this, this.Count, DataTypes.IDataObject, e));
            }
        }
        public void Add(string path)
        {
            if (!TiXMain.CheckFile(path)) return;

            this.Add(new ImageSet(this, this.Count, DataTypes.File, path));
        }
        public void Add(IEnumerable<string> paths)
        {
            foreach (var path in paths.OrderBy(ee => ee, ExtendStringComparer.Instance))
                this.Add(path);
        }
        public void Add(Image image)
        {
            if (image == null) return;
            this.Add(new ImageSet(this, this.Count, image));
        }

        protected new void Add(ImageSet value)
        {
            base.Add(value);
        }
    }
}
