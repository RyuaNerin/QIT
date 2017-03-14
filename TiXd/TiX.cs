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

        internal TiXResult(Image bitmap)
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
        }

        public static void ShowSettingDialog(IWin32Window owner)
        {
            using (var frm = new frmSettings(true))
                frm.ShowDialog(owner);
        }

        public static TiXResult ResizeImage(Image bitmap)
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

        public static bool TweetByTiX(IWin32Window owner, Image[] images, string text, string in_reply_to_status_id, bool autoStart)
        {
            if (images == null)     throw new ArgumentNullException("images 는 null 이 아니여야 합니다.");
            if (images.Length == 0) throw new ArgumentException("files 의 길이는 1 이상이여야 합니다.");

            if (!CheckPin(owner))
                return false;

            var ic = new ImageCollection();
            ic.Add(images);

            return Tweet(ic, owner, text, in_reply_to_status_id, autoStart);
        }

        public static bool TweetByTiX(IWin32Window owner, string[] files, bool orderByName, string text, string in_reply_to_status_id, bool autoStart)
        {
            if (files == null)        throw new ArgumentNullException("files 는 null 이 아니여야 합니다.");
            if (files.Length == 0)    throw new ArgumentException("files 의 길이는 1 이상이여야 합니다.");

            if (!CheckPin(owner))
                return false;

            var ic = new ImageCollection();
            ic.Add(files, orderByName);

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
