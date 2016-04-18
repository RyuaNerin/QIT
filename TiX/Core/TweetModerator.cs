using System;
using System.Drawing;
using System.Windows.Forms;
using TiX.Windows;
using CallbackData = TiX.Windows.frmMain.CallbackData;

namespace TiX.Core
{
    public static class TweetModerator
    {
        public static void Tweet(IDataObject dataObject, bool autoStart = false, string title = null, string defaultText = null, string inReplyToStatusId = null)
        {
            if (dataObject == null) return;

            var cbData = new CallbackData();
            cbData.Collection = new ImageCollection();
            cbData.Collection.Add(dataObject);

            if (cbData.Collection.Count == 0)
            {
                cbData.Collection.Dispose();
                return;
            }

            Tweet(cbData, autoStart, title, defaultText, inReplyToStatusId);
        }
        public static void Tweet(Image image, bool autoStart = false, string title = null, string defaultText = null, string inReplyToStatusId = null)
        {
            if (image == null) return;

            var cbData = new CallbackData();
            cbData.Collection = new ImageCollection();
            cbData.Collection.Add(image);

			Tweet(cbData, autoStart, title, defaultText, inReplyToStatusId);
		}
        private static void Tweet(CallbackData data, bool autoStart = false, string title = null, string defaultText = null, string inReplyToStatusId = null)
        {
            data.AutoStart          = autoStart;
            data.DefaultText        = defaultText;
            data.InReplyToStatusId  = inReplyToStatusId;

            if (frmMain.Instance != null)
            {
                data.Callback = true;
                data.IAsyncResult = frmMain.Instance.BeginInvoke(new Action<CallbackData>(frmMain.Callback), data);
            }
            else
			{
                frmMain.Callback(data);
			}
        }
    }
}
