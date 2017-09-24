using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using TiX.Windows;

namespace TiX.Core
{
    internal class TweetOption
    {
        public Action CloseEvent    { get; set; }
        public bool   AutoStart     { get; set; }
        public string WindowTitle   { get; set; }
        public string DefaultString { get; set; }
        public string InReply       { get; set; }
    }
    internal static class TweetModerator
    {
        public static void Tweet(IEnumerable<string> paths, TweetOption option)
        {
            Tweet(new ImageCollection { { paths, true } }, option);
        }
        public static void Tweet(IEnumerable<Uri> uris, TweetOption option)
        {
            Tweet(new ImageCollection { { uris,  true } }, option);
        }
        public static void Tweet(IDataObject data, TweetOption option)
        {
            Tweet(new ImageCollection { { data } }, option);
        }
        public static void Tweet(Image data, TweetOption option)
        {
            Tweet(new ImageCollection { { data } }, option);
        }
        public static void Tweet(IEnumerable<Image> data, TweetOption option)
        {
            Tweet(new ImageCollection { { data } }, option);
        }
        public static void Tweet(IEnumerable<byte[]> data, TweetOption option)
        {
            Tweet(new ImageCollection { { data } }, option);
        }

        private static void Tweet(ImageCollection coll, TweetOption option)
        {
            if (coll.Count == 0)
            {
                coll.Dispose();
                return;
            }

            var cbData = new CallbackData()
            {
                Collection = coll,
                Option     = option
            };

            if (frmMain.Instance != null)
            {
                cbData.Callback = true;
                cbData.IAsyncResult = frmMain.Instance.BeginInvoke(new Action<CallbackData>(Callback), cbData);
            }
            else
			{
                Callback(cbData);
			}
        }
        
        private class CallbackData
        {
            public ImageCollection Collection;
            public bool            Callback;
            public IAsyncResult    IAsyncResult;

            public TweetOption     Option;
        }
        private static void Callback(CallbackData data)
        {
            if (data.Callback)
                frmMain.Instance.EndInvoke(data.IAsyncResult);

            var frm = new frmUpload(data.Collection, data.Option.CloseEvent, frmMain.Instance != null)
            {
                AutoStart         = data.Option.AutoStart,
                TweetString       = data.Option.DefaultString,
                InReplyToStatusId = data.Option.InReply
            };
            frm.FormClosed += (s, e) => frm.Dispose();

            if (frmMain.Instance != null)
                frm.Show(frmMain.Instance);
            else
                Application.Run(frm);
        }
    }
}
