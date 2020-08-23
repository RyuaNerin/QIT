using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Limitation;
using TiX.Core;
using TiX.ScreenCapture;
using TiX.Utilities;
using TiX.Windows;

namespace TiX
{
    internal static class TiXMain
    {
        public static readonly string ProductName        = $"TiX rev.{new Version(Application.ProductVersion).Revision}";
        public const           string GUIDApplication    = "{9CE5906A-DFBB-4A5A-9EBF-9D262E5D29B8}";
        public const           string GUIDShellExtension = "{9CE5906A-DFBB-4A5A-9EBF-9D262E5D29B9}";

        public static readonly string CurrentDirMutex    = GUIDApplication + Path.GetDirectoryName(Application.ExecutablePath).GetHashCode().ToString("X");

        public static readonly string[] AllowExtension = {
            ".bmp",
            ".emf", ".exif",
            ".gif",
            ".ico", ".cur",
            ".jpg", ".jpeg",
            ".png",
            ".tif", ".tiff",
            ".wmf",
            ".psd",
            ".webp",
            ".svg"
        };

        public static readonly OAuth Twitter = new OAuth("lQJwJWJoFlbvr2UQnDbg", "DsuIRA1Ak9mmSCGl9wnNvjhmWJTmb9vZlRdQ7sMqXww");

        public static readonly bool IsInstalled;

        public static bool CheckFile(Uri uri)
        {
            if (!uri.IsFile)
                return true;

            return CheckFile(uri.LocalPath);
        }
        public static bool CheckFile(string path)
        {
            if (!File.Exists(path)) return false;

            var ext = Path.GetExtension(path).ToLower();

            for (int i = 0; i < AllowExtension.Length; ++i)
                if (AllowExtension[i] == ext)
                    return true;

            return false;
        }
        static TiXMain()
        {
            IsInstalled = File.Exists(Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "TiX.dat"));
        }
        
        [STAThread]
        static int Main(string[] args)
        {
            var option = Args.Parse(args);
            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            CrashReport.Init();

#if !DEBUG
            //System.Net.HttpWebRequest.DefaultWebProxy = null;
#endif
            System.Net.HttpWebRequest.DefaultCachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
            System.Net.ServicePointManager.MaxServicePoints = 20;

            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            Settings.Instance.Load();

            // Check Pin Mutex
            while (string.IsNullOrWhiteSpace(Settings.Instance.UToken ) ||
                   string.IsNullOrWhiteSpace(Settings.Instance.USecret))
            {
                using (var helper = new InstanceHelper(CurrentDirMutex))
                {
                    if (helper.LockOrActivate())
                    {
                        using (var frm = new frmPin(helper.WMMessage))
                        {
                            Application.Run(frm);

                            if (frm.DialogResult != DialogResult.OK)
                                return 0;
                        }
                    }
                    else
                    {
                        helper.WaitOne();
                        Settings.Instance.Load();
                    }
                }
            }

            TiXMain.Twitter.UserToken = Settings.Instance.UToken;
            TiXMain.Twitter.UserSecret = Settings.Instance.USecret;

            if (!string.IsNullOrWhiteSpace(option.SchemeData))
                return MainPartOfScheme(option);

            if (option.CaptureScreenPart)
                return MainPartOfCaptureScreen(option);

            if (option.UsePipe)
                return MainPartOfFiles(option, GetLinesFromStream(Console.OpenStandardInput(), Encoding.UTF8));

            if (option.Files != null && option.Files.Count >= 1)
                return MainPartOfFiles(option, option.Files);

            using (var helper = new InstanceHelper(CurrentDirMutex))
                if (helper.LockOrActivate())
                    Application.Run(new frmMain(helper.WMMessage));

            return 0;
        }
        
        private static int MainPartOfScheme(Args option)
        {
            Uri uri = new Uri(option.SchemeData);
            if (!Uri.TryCreate(option.SchemeData, UriKind.RelativeOrAbsolute, out uri))
                return 1;

            var paq = uri.PathAndQuery.Substring(1);

            if (uri.Host == "uri")
            {
                var uris = new List<Uri>();

                foreach (var uriPart in paq.Split('|'))
                    if (Uri.TryCreate(Uri.UnescapeDataString(uriPart), UriKind.Absolute, out uri))
                        uris.Add(uri);

                TweetModerator.Tweet(uris,
                    new TweetOption
                    {
                        AutoStart     = option.TweetWithoutText,
                        DefaultString = option.Text,
                        InReply       = option.In_Reply_To_Status_Id,
                    });
            }
            else if (uri.Host == "base64")
            {
                var datas = new List<byte[]>();

                foreach (var base64Part in paq.Split('|'))
                    datas.Add(Convert.FromBase64String(Uri.UnescapeDataString(base64Part)));

                TweetModerator.Tweet(datas,
                    new TweetOption
                    {
                        AutoStart     = option.TweetWithoutText,
                        DefaultString = option.Text,
                        InReply       = option.In_Reply_To_Status_Id,
                    });
            }

            return 0;
        }

        private static int MainPartOfCaptureScreen(Args option)
        {
            Image cropedImage;
            using (var stasisForm = new Stasisfield())
            {
                Application.Run(stasisForm);
                cropedImage = stasisForm.CropedImage;
            }

            if (cropedImage != null)
                TweetModerator.Tweet(cropedImage,
                    new TweetOption
                    {
                        AutoStart     = false,
                        DefaultString = option.Text,
                        InReply       = option.In_Reply_To_Status_Id,
                    });

            return 0;
        }
        
        private static IEnumerable<string> GetLinesFromStream(Stream stream, Encoding encoding)
        {
            string line;

            using (var reader = new StreamReader(stream, encoding))
                while (!string.IsNullOrWhiteSpace(line = reader.ReadLine()))
                    yield return line;
        }
        private static int MainPartOfFiles(Args option, IEnumerable<string> items)
        {
            var lst = new List<Uri>(8);
            Uri uri;

            foreach (var item in items)
                if (Uri.TryCreate(item, UriKind.RelativeOrAbsolute, out uri))
                    lst.Add(uri);

            if (lst.Count == 0) return 0;

            TweetModerator.Tweet(lst,
                new TweetOption
                {
                    AutoStart     = option.TweetWithoutText,
                    DefaultString = option.Text,
                    InReply       = option.In_Reply_To_Status_Id,
                });
            return 0;
        }
    }
}
