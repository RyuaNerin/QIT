using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Windows;
using CoreTweet;
using TiX.Core;
using TiX.ScreenCapture;
using TiX.Utilities;
using TiX.Windows;

namespace TiX
{
    internal partial class TiXMain : Application
    {
        public const string GUIDApplication = "{9CE5906A-DFBB-4A5A-9EBF-9D262E5D29B8}";
        public const string GUIDShellExtension = "{9CE5906A-DFBB-4A5A-9EBF-9D262E5D29B9}";

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

        public static readonly Tokens Twitter = new Tokens
        {
            ConsumerKey = TwitterOAuthKey.AppKey,
            ConsumerSecret = TwitterOAuthKey.AppSecret,
        };

        public static readonly string ProductName;
        public static readonly string CurrentDirMutex;

        public static readonly bool IsInstalled;

        static TiXMain()
        {
            var asm = Assembly.GetExecutingAssembly();
            var asmLocation = asm.Location;

            ProductName = $"TiX rev.{asm.GetName().Version.Revision}";
            CurrentDirMutex = GUIDApplication + Path.GetDirectoryName(asmLocation).GetHashCode().ToString("X");
            IsInstalled = File.Exists(Path.Combine(Path.GetDirectoryName(asmLocation), "TiX.dat"));
        }


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

        private static CmdOption Args;

        private void App_Startup(object sender, StartupEventArgs e)
        {
            Args = CmdOption.Parse(e.Args);

            CrashReport.Init();

#if !DEBUG
            HttpWebRequest.DefaultWebProxy = null;
#endif
            HttpWebRequest.DefaultCachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);

            ServicePointManager.MaxServicePoints = 32;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            Settings.Instance.Load();

            // Check Pin Mutex
            while (string.IsNullOrWhiteSpace(Settings.Instance.UToken) ||
                   string.IsNullOrWhiteSpace(Settings.Instance.USecret))
            {
                using (var helper = new InstanceHelper(CurrentDirMutex))
                {
                    if (helper.LockOrActivate())
                    {
                        var win = new PinWindow(helper.WMMessage);
                        this.MainWindow = win;
                        if (!(win.ShowDialog() ?? false))
                        {
                            this.Shutdown();
                            return;
                        }
                    }
                    else
                    {
                        helper.WaitOne();
                        Settings.Instance.Load();
                    }
                }
            }

            AppMain();
        }

        public static void AppMain()
        {
            TiXMain.Twitter.AccessToken = Settings.Instance.UToken;
            TiXMain.Twitter.AccessTokenSecret = Settings.Instance.USecret;

            if (!string.IsNullOrWhiteSpace(Args.SchemeData))
            {
                MainPartOfScheme();
                return;
            }

            if (Args.CaptureScreenPart)
            {
                MainPartOfCaptureScreen();
                return;
            }

            if (Args.UsePipe)
            {
                MainPartOfFiles(GetLinesFromStream(Console.OpenStandardInput(), Encoding.UTF8));
                return;
            }

            if (Args.Files != null && Args.Files.Count >= 1)
            {
                MainPartOfFiles();
                return;
            }

            using (var helper = new InstanceHelper(CurrentDirMutex))
                if (helper.LockOrActivate())
                    (Current.MainWindow = new MainWindow(helper.WMMessage)).Show();
        }

        private static int MainPartOfScheme()
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
                        AutoStart = option.TweetWithoutText,
                        DefaultString = option.Text,
                        InReply = option.In_Reply_To_Status_Id,
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
                        AutoStart = option.TweetWithoutText,
                        DefaultString = option.Text,
                        InReply = option.In_Reply_To_Status_Id,
                    });
            }

            return 0;
        }

        private static int MainPartOfCaptureScreen(CmdOption option)
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
                        AutoStart = false,
                        DefaultString = option.Text,
                        InReply = option.In_Reply_To_Status_Id,
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
        private static int MainPartOfFiles(CmdOption option, IEnumerable<string> items)
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
                    AutoStart = option.TweetWithoutText,
                    DefaultString = option.Text,
                    InReply = option.In_Reply_To_Status_Id,
                });
            return 0;
        }
    }
}
