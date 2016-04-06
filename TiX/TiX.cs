using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Limitation;
using TiX.Core;
using TiX.ScreenCapture;
using TiX.Utilities;
using TiX.Windows;

namespace TiX
{
    static class Program
    {
        public static string ProductName = String.Format("TiX rev.{0}", Application.ProductVersion);
        public const  string UniqueName = "C0E6D64A-23D2-4676-93F7-F4B9D8CE25DF";
        public const  string ShellName  = "C0E6D64A-23D2-4676-93F7-F4B9D8CE25DE";

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

        [STAThread]
        static void Main( string[] args )
        {
            if (args.Length == 1 && args[0].Equals("install", StringComparison.CurrentCultureIgnoreCase))
            {
                Console.Write(ShellExtension.Install());
                return;
            }
            if (args.Length == 2 && args[0].Equals("uninstall", StringComparison.CurrentCultureIgnoreCase))
            {
                ShellExtension.Uninstall(args[1]);
                return;
            }
            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            TiX.ExternalLibrary.Resolver.Init(typeof(Properties.Resources));

            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            AppDomain.CurrentDomain.UnhandledException += (s, e) => WriteException(e.ExceptionObject as Exception);
            TaskScheduler.UnobservedTaskException += (s, e) => WriteException(e.Exception);
            Application.ThreadException += (s, e) => WriteException(e.Exception);
            
            System.Net.HttpWebRequest.DefaultCachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
            System.Net.HttpWebRequest.DefaultWebProxy = null;

            Settings.Load();
            Program.Twitter.UserToken  = Settings.UToken;
            Program.Twitter.UserSecret = Settings.USecret;

            int i;

            if (args.Length >= 1 && args[0].Equals("stasis", StringComparison.CurrentCultureIgnoreCase))
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

                return;
            }
            else if (args.Length >= 1)
            {
                var data = new ImageCollection();

                if (args.Length >= 2 && args[0].Equals("shell", StringComparison.CurrentCultureIgnoreCase))
                {
                    var rawData = Encoding.UTF8.GetBytes(string.Join("\n", args, 1, args.Length - 1));

                    var byteList = new ShellHelper(ShellName).GetOrSend(rawData);
                    if (byteList == null) return;
                    
                    var fileList = new List<string>();
                    for (i = 0; i < byteList.Count; ++i)
                        fileList.AddRange(Encoding.UTF8.GetString(byteList[i]).Split('\n'));
                    fileList.Sort();

                    for (i = 0; i < fileList.Count; ++i)
                        data.Add(fileList[i]);
                }
                else
                {
                    for (i = 0; i < args.Length; ++i)
                        data.Add(args[i]);
                }

                if (data.Count == 0) return;

                Application.Run(new frmUpload(data, true) { AutoStart = false });
                return;
            }

            Form frm;
            using (var instance = new InstanceHelper(UniqueName))
            {
                if (instance.Check())
                {                    
                    if (String.IsNullOrEmpty(Settings.UToken) | String.IsNullOrEmpty(Settings.USecret))
                    {
                        using (frm = new frmPin())
                        {
                            Application.Run(instance.MainWindow = frm);

                            if (frm.DialogResult != DialogResult.OK)
                                return;
                        }
                    }

                    frm = new frmMain();
                    Application.Run(instance.MainWindow = frm);
                }
            }
        }

        private static void WriteException(Exception exception)
        {
            var date = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
            var file = string.Format("Crash-{0}.txt", date);

            using (var writer = new StreamWriter(Path.Combine(Application.StartupPath, file)))
            {
                writer.WriteLine("Decchi Crash Report");
                writer.WriteLine("Date    : " + date);
                writer.WriteLine();
                writer.WriteLine("Exception");
                writer.WriteLine(exception.ToString());
            }

            Application.Exit();
        }
    }
}
