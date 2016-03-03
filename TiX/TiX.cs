using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using TiX.Core;
using TiX.ScreenCapture;
using TiX.Utilities;
using TiX.Windows;

namespace TiX
{
	static class Program
	{
        public static string ProductName = String.Format("TiX v{0}", Application.ProductVersion);
        public const  string UniqueName = "C0E6D64A-23D2-4676-93F7-F4B9D8CE25DF";
        public const  string ShellName  = "C0E6D64A-23D2-4676-93F7-F4B9D8CE25DE";

        public static readonly string[] AllowExtension = { ".bmp", ".emf", ".exif", ".gif", ".ico", ".jpg", ".jpeg", ".png", ".tif", ".tiff", ".wmf", ".psd" };

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

            Settings.Load();

            int i;

            if (args.Length >= 1 && args[0].Equals("stasis", StringComparison.CurrentCultureIgnoreCase))
            {
                //args = new string[] { "Stasis" }; // 정지장 생성 테스트용
                //args = new string[] { "Stasis", "hsky_Lauren", "705274228010954752" }; // 정지장 멘션 테스트용

                Stasisfield stasisForm;

                if (args.Length == 3)
                    stasisForm = new Stasisfield(args[1], args[2]);
                else
                    stasisForm = new Stasisfield();

                Application.Run(stasisForm);
                var cropedImage = stasisForm.CropedImage;

                if (cropedImage != null)
                {
                    if (args.Length == 3)
                        TweetModerator.Tweet(cropedImage, "캡처 화면 전송중", args[1], args[2]);
                    else
                        TweetModerator.Tweet(cropedImage, "캡처 화면 전송중");
                }


                return;
            }
            else if (args.Length >= 1)
            {
                var lst = new List<DragDropInfo>();

                if (args.Length >= 2 && args[0].Equals("shell", StringComparison.CurrentCultureIgnoreCase))
                {
                    var data = Encoding.UTF8.GetBytes(string.Join("\n", args, 1, args.Length - 1));

                    IList<byte[]> byteList;
                    using (var shell = new ShellHelper(ShellName))
                    {
                        byteList = shell.GetOrSend(data);
                        if (byteList == null) return; 
                    }
                    
                    var fileList = new List<string>();
                    for (i = 0; i < byteList.Count; ++i)
                        fileList.AddRange(Encoding.UTF8.GetString(byteList[i]).Split('\n'));
                    fileList.Sort();

                    for (i = 0; i < fileList.Count; ++i)
                    {
                        if (!File.Exists(fileList[i])) continue;
                        lst.Add(DragDropInfo.Create(fileList[i]));
                    }
                }
                else
                {
                    for (i = 0; i < args.Length; ++i)
                    {
                        if (!File.Exists(args[i])) continue;
                        lst.Add(DragDropInfo.Create(args[i]));
                    }
                }

                if (lst.Count == 0) return;

                Application.Run(new frmUpload(lst, true) { AutoStart = false });
                return;
            }

            Form frm;
            using (var instance = new InstanceHelper(UniqueName))
            {
                if (instance.Check())
                {                    
                    if (String.IsNullOrEmpty(Settings.UToken))
                    {
                        frm = new frmPin();
                        Application.Run(instance.MainWindow = frm);
                        frm.Dispose();

                        if (frm.DialogResult != DialogResult.OK)
                            return;
                    }

                    frm = new frmMain();
                    Application.Run(instance.MainWindow = frm);
                }
            }
		}
	}
}
