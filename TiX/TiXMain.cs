using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
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
        public static readonly string ProductName        = $"TiX rev.{new Version(Application.ProductVersion).Revision}";
        public static readonly string GUIDApplication;
        public const           string GUIDShellExtension = "{9CE5906A-DFBB-4A5A-9EBF-9D262E5D29B9}";

        public static readonly string[] AllowExtension = { ".bmp", ".emf", ".exif", ".gif", ".ico", ".cur", ".jpg", ".jpeg", ".png", ".tif", ".tiff", ".wmf", ".psd", ".webp", ".svg" };

        public static readonly bool IsAdministratorMode;

        public static readonly OAuth Twitter = new OAuth("lQJwJWJoFlbvr2UQnDbg", "DsuIRA1Ak9mmSCGl9wnNvjhmWJTmb9vZlRdQ7sMqXww");
        
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

        public static bool IsRunningAsAdministrator()
        {
            using (var cur = WindowsIdentity.GetCurrent())
            {
                foreach (var role in cur.Groups)
                {
                    if (role.IsValidTargetType(typeof(SecurityIdentifier)))
                    {
                        var sid = (SecurityIdentifier)role.Translate(typeof(SecurityIdentifier));

                        if (sid.IsWellKnown(WellKnownSidType.AccountAdministratorSid) ||
                            sid.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid))
                            return true;
                    }
                }
            }

            return false;
        }

        static TiXMain()
        {
            GUIDApplication = ((GuidAttribute)System.Reflection.Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(GuidAttribute), true)[0]).Value;

            IsAdministratorMode = IsRunningAsAdministrator();
        }
        
        [STAThread]
        static int Main(string[] args)
        {
            var option = TiXOption.Parse(args);

            var isScheme = !string.IsNullOrWhiteSpace(option.SchemeData);

            // 보안 이슈
            if (!isScheme)
            {
                if (option.OptionInstallation == OptionInstallation.UninstallRunas)
                    return (int)Installer.Ap_Uninstall_Runas(option.Files[0]);

                if (option.OptionInstallation == OptionInstallation.InstallRunas)
                    return (int)Installer.Ap_Install_Runas(true, option.Files[0], option.Files[1]);

                if (option.OptionInstallation.HasFlag(OptionInstallation.Install))
                    return (int)Installer.Ap_Install(true, option.OptionInstallation);

                if (option.OptionTixSettings != 0)
                    return (int)Installer.TiXSetting(true, option.OptionTixSettings);
            }
            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            CrashReport.Init();

#if !DEBUG
            System.Net.HttpWebRequest.DefaultWebProxy = null;
#endif
            System.Net.HttpWebRequest.DefaultCachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
            System.Net.ServicePointManager.MaxServicePoints = 20;

            Settings.Instance.Load();
            TiXMain.Twitter.UserToken  = Settings.Instance.UToken;
            TiXMain.Twitter.UserSecret = Settings.Instance.USecret;

            if (!isScheme && option.OptionInstallation == OptionInstallation.Uninstall)
            {
                Application.Run(new frmUninstall());
                return 0;
            }
            
            if (!option.StandAlone && !File.Exists(Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "TiX.dat")))
            {
                Application.Run(new frmInstall());
                return 0;
            }

            if (isScheme)
                return MainPartOfScheme(option);

            if (option.CaptureScreenPart)
                return MainPartOfCaptureScreen(option);

            if (option.UsePipe)
                return MainPartOfFiles(option, GetLinesFromStream(Console.OpenStandardInput(), Encoding.UTF8));

            if (option.Files != null && option.Files.Count >= 1)
                return MainPartOfFiles(option, option.Files);

            using (var helper = new InstanceHelper(GUIDApplication))
                if (helper.LockOrActivate())
                    Application.Run(new frmMain(helper.WMMessage));

            return 0;
        }
        
        private static int MainPartOfScheme(TiXOption option)
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

        private static int MainPartOfCaptureScreen(TiXOption option)
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
        private static int MainPartOfFiles(TiXOption option, IEnumerable<string> items)
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
