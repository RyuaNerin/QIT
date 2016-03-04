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
            var data = new CallbackData();

            // Local Files
            if (dataObject.GetDataPresent(DataFormats.FileDrop))
            {
                string[] paths = (string[])dataObject.GetData(DataFormats.FileDrop);

                if (paths.Length > 10 && frmMain.Instance != null)
                    if (MessageBox.Show(frmMain.Instance,
                                        String.Format("정말 {0} 개의 이미지를 트윗하시겠습니까?", paths.Length),
                                        Program.ProductName,
                                        MessageBoxButtons.YesNo,
                                        MessageBoxIcon.Question) == DialogResult.No)
                        return;

                for (int i = 0; i < paths.Length; ++i)
                    if (Program.CheckFile(paths[i]))
                        data.List.Add(DragDropInfo.Create(paths[i]));
            }
            else
            {
                DragDropInfo info = DragDropInfo.Create(dataObject);
                if (info != null)
                    data.List.Add(info);
                else
                    info.Dispose();
            }

            if (data.List.Count == 0) return;

            Tweet(data, autoStart, title, defaultText, inReplyToStatusId);
        }
        public static void Tweet(Image dataObject, bool autoStart = false, string title = null, string defaultText = null, string inReplyToStatusId = null)
        {
            if (dataObject == null) return;

            var data = new CallbackData();
            data.List.Add(DragDropInfo.Create(dataObject));

            Tweet(data, autoStart, title, defaultText, inReplyToStatusId);
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
                frmMain.Callback(data);
        }
    }
}
