using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Principal;
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
        public static string ProductName = String.Format("TiX rev.{0}", new Version(Application.ProductVersion).Revision);
        public const  string InstanceName = "C0E6D64A-23D2-4676-93F7-F4B9D8CE25DF";

        public static readonly bool IsAdministratorMode;

        public static readonly OAuth Twitter = new OAuth("lQJwJWJoFlbvr2UQnDbg", "DsuIRA1Ak9mmSCGl9wnNvjhmWJTmb9vZlRdQ7sMqXww");

        public static readonly string[] AllowExtension = { ".bmp", ".emf", ".exif", ".gif", ".ico", ".jpg", ".jpeg", ".png", ".tif", ".tiff", ".wmf", ".psd" };
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
            try
            {
                using (var cur = WindowsIdentity.GetCurrent())
                    IsAdministratorMode = new WindowsPrincipal(cur).IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                IsAdministratorMode = false;
            }
        }

        [STAThread]
        static int Main(string[] args)
        {
            if ((args.Length == 4) && args[0].Equals("install", StringComparison.OrdinalIgnoreCase))
                return (int)ShellExtension.Install(args[1] == "1", args[2] == "1", args[3], true);

            if (args.Length == 1 && args[0].Equals("uninstall", StringComparison.OrdinalIgnoreCase))
                return (int)ShellExtension.Uninstall(true);
            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            CrashReport.Init();

            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            
#if !DEBUG
            System.Net.HttpWebRequest.DefaultWebProxy = null;
#endif
            System.Net.HttpWebRequest.DefaultCachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
            System.Net.ServicePointManager.MaxServicePoints = 20;

            Settings.Load();
            TiXMain.Twitter.UserToken  = Settings.UToken;
            TiXMain.Twitter.UserSecret = Settings.USecret;

            int i;

            if (args.Length >= 1 && args[0].Equals("stasis", StringComparison.OrdinalIgnoreCase))
            {
                Image cropedImage;
                using (var stasisForm = new Stasisfield())
                {
                    Application.Run(stasisForm);
                    cropedImage = stasisForm.CropedImage;
                }

                if (cropedImage != null)
                {
                    if (args.Length == 3)
                        TweetModerator.Tweet(cropedImage, false, "캡처 화면 전송중", args[1], args[2]);
                    else
                        TweetModerator.Tweet(cropedImage, false, "캡처 화면 전송중");
                }

                return 0;
            }
            else if (args.Length >= 1)
            {
                var data = new ImageCollection();
                bool autoStart = args.Any(e => e.Equals("--notext", StringComparison.OrdinalIgnoreCase));

                var lst = new List<string>(args.Length);

                if (args.Any(e => e.Equals("--pipe", StringComparison.OrdinalIgnoreCase)))
                {
                    var reader = new StreamReader(Console.OpenStandardInput(), Encoding.UTF8);
                    string path;
                    while (true)
                    {
                        path = reader.ReadLine();
                        if (string.IsNullOrEmpty(path)) break;

                        if (CheckFile(path))
                            lst.Add(path);
                    }
                }
                else
                {
                    for (i = 0; i < args.Length; ++i)
                        if (CheckFile(args[i]))
                            lst.Add(args[i]);
                }

                if (lst.Count == 0) return 0;

                data.Add(lst);


                Application.Run(new frmUpload(data, true) { AutoStart = autoStart });
                return 0;
            }

            Form frm;
            using (var instance = new InstanceHelper(InstanceName))
            {
                if (instance.Check())
                {                    
                    if (String.IsNullOrEmpty(Settings.UToken) | String.IsNullOrEmpty(Settings.USecret))
                    {
                        using (frm = new frmPin())
                        {
                            Application.Run(instance.MainWindow = frm);

                            if (frm.DialogResult != DialogResult.OK)
                                return 0;
                        }
                    }

                    frm = new frmMain();
                    Application.Run(instance.MainWindow = frm);
                }
            }

            return 0;
        }
    }
}
