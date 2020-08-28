using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Windows;
using CoreTweet;
using TiX.Core;
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
            if (string.IsNullOrWhiteSpace(Settings.Instance.UToken) || string.IsNullOrWhiteSpace(Settings.Instance.USecret))
            {
                while (string.IsNullOrWhiteSpace(Settings.Instance.UToken) || string.IsNullOrWhiteSpace(Settings.Instance.USecret))
                {
                    using (var helper = new InstanceHelper(CurrentDirMutex))
                    {
                        if (helper.LockOrActivate())
                        {
                            var win = new PinWindow(helper.WMMessage);
                            this.MainWindow = win;
                            win.Closing += (ls, le) =>
                            {
                                if (win.DialogResult ?? false)
                                {
                                    this.AppMain();
                                }
                            };
                            win.Show();
                        }
                        else
                        {
                            helper.WaitOne();
                            Settings.Instance.Load();
                        }
                    }
                }
            }
            else
            {
                this.AppMain();
            }

        }

        private void AppMain()
        {
            Twitter.AccessToken = Settings.Instance.UToken;
            Twitter.AccessTokenSecret = Settings.Instance.USecret;

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
                MainPartOfFiles(Args.Files);
                return;
            }

            using (var helper = new InstanceHelper(CurrentDirMutex))
                if (helper.LockOrActivate())
                    (Current.MainWindow = new MainWindow(helper.WMMessage)).Show();
        }

        private static void MainPartOfScheme()
        {
            if (!Uri.TryCreate(Args.SchemeData, UriKind.RelativeOrAbsolute, out var uri))
            {
                Current.Shutdown(1);
                return;
            }

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
                        AutoStart = Args.TweetWithoutText,
                        DefaultString = Args.Text,
                        InReply = Args.In_Reply_To_Status_Id,
                    });
            }
        }

        private static void MainPartOfCaptureScreen()
        {
            var win = new CaptureWindow();
            Current.MainWindow = win;
            win.Closed += (ls, le) =>
            {
                if (win.DialogResult ?? false)
                {
                    TweetModerator.Tweet(
                        win.CropedImage,
                        new TweetOption
                        {
                            AutoStart = false,
                            DefaultString = Args.Text,
                            InReply = Args.In_Reply_To_Status_Id,
                        });
                }
            };
            win.Show();
        }

        private static IEnumerable<string> GetLinesFromStream(Stream stream, Encoding encoding)
        {
            string line;

            using (var reader = new StreamReader(stream, encoding))
                while (!string.IsNullOrWhiteSpace(line = reader.ReadLine()))
                    yield return line;
        }
        private static int MainPartOfFiles(IEnumerable<string> items)
        {
            var lst = items.Select(e => Uri.TryCreate(e, UriKind.RelativeOrAbsolute, out var uri) ? uri : null).Where(e => e != null).ToArray();

            if (lst.Length == 0) return 0;

            TweetModerator.Tweet(
                lst,
                new TweetOption
                {
                    AutoStart = Args.TweetWithoutText,
                    DefaultString = Args.Text,
                    InReply = Args.In_Reply_To_Status_Id,
                });
            return 0;
        }
    }
}
