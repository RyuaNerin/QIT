using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using TiX.Core;
using TiX.ScreenCapture;
using TiX.Utilities;
using TiX.Windows;

namespace TiX
{
    public class TiXResult : IDisposable
    {
        internal ImageSet m_imageSet;

        internal TiXResult(Bitmap bitmap)
        {
            this.m_imageSet = new ImageSet(bitmap);
        }

        ~TiXResult()
        {
            this.Dispose();
        }

        public void Dispose()
        {
            if (this.m_imageSet != null)
            {
                this.m_imageSet.Dispose();
                this.m_imageSet = null;
            }

            GC.SuppressFinalize(this);
        }

        public Image ResizedImage
        {
            get { return this.m_imageSet.Image; }
        }
        public Image Thumbnail
        {
            get { return this.m_imageSet.Thumbnail; }
        }
        public MemoryStream ResizeRawImage
        {
            get { return this.m_imageSet.RawStream; }
        }
        public double Ratio
        {
            get { return this.m_imageSet.Ratio; }
        }
        public GifFrames GifFrames
        {
            get { return this.m_imageSet.GifFrames; }
        }
        public Task<Image> Task
        {
            get { return this.m_imageSet.InnerTask; }
        }
    }

    public class LibTiX
    {
        static LibTiX()
        {
            Settings.Load();
            TiXMain.Twitter.UserToken  = Settings.UToken;
            TiXMain.Twitter.UserSecret = Settings.USecret;

            CrashReport.Init();

            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        }

        public static void ShowSettingDialog(IWin32Window owner)
        {
            using (var frm = new frmSettings(true))
                frm.ShowDialog(owner);
        }

        public static TiXResult ResizeImage(Bitmap bitmap)
        {
            var result = new TiXResult(bitmap);
            result.m_imageSet.StartLoad();

            return result;
        }

        public static bool TweetWithCapture(IWin32Window owner, string text, string in_reply_to_status_id, bool autoStart)
        {
            CheckPin(owner);

            Image cropedImage;
            using (var stasisForm = new Stasisfield())
            {
                stasisForm.ShowDialog();
                cropedImage = stasisForm.CropedImage;
            }

            if (cropedImage != null)
                return false;

            var ic = new ImageCollection();
            ic.Add(cropedImage);

            return Tweet(ic, owner, text, in_reply_to_status_id, autoStart);
        }

        public static bool TweetByTiX(IWin32Window owner, string[] files, bool orderByName, string text, string in_reply_to_status_id, bool autoStart)
        {
            if (files.Length == 0)
                throw new ArgumentException("files 의 길이는 1 이상이여야 합니다.");

            CheckPin(owner);

            var ic = new ImageCollection();

            if (orderByName)
            {
                ic.Add(files);
            }
            else
            {
                for (int i = 0; i < files.Length; ++i)
                    ic.Add(files[i]);
            }

            return Tweet(ic, owner, text, in_reply_to_status_id, autoStart);
        }

        private static bool Tweet(ImageCollection ic, IWin32Window owner, string text, string in_reply_to_status_id, bool autoStart)
        {
            using (var frm = new frmUpload(ic, false))
            {
                frm.InReplyToStatusId = in_reply_to_status_id;
                frm.AutoStart = autoStart;
                frm.TweetString = text;

                return frm.ShowDialog(owner) == DialogResult.OK;
            }
        }

        private static bool CheckPin(IWin32Window owner)
        {
            if (!string.IsNullOrEmpty(Settings.UToken) && !string.IsNullOrEmpty(Settings.USecret))
                return true;

            using (var frm = new frmPin())
                return frm.ShowDialog(owner) == DialogResult.OK;
        }
    }
}
