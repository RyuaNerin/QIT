using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
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
            if (args.Length == 1 && args[0] == "install")
            {
                Console.Write(ShellExtension.Install());
                return;
            }
            if (args.Length == 2 && args[0] == "uninstall")
            {
                ShellExtension.Uninstall(args[1]);
                return;
            }
            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            int i;

            if (args.Length == 1 && args[0] == "stasis")
            {
                Application.Run(new Stasisfield());
                return;
            }
            else if (args.Length >= 1)
            {
                var lst = new List<DragDropInfo>();

                if (args.Length >= 2 && args[0] == "shell")
                {
                    var data = Encoding.UTF8.GetBytes(string.Join("\n", args, 1, args.Length - 1));

                    IList<byte[]> byteList;
                    using (var shell = new ShellHelper(ShellName))
                    {
                        byteList = shell.GetOrSend(data);
                        if (byteList == null) return; 
                    }

                    string[] str;
                    int j;
                    for (i = 0; i < byteList.Count; ++i)
                    {
                        str = Encoding.UTF8.GetString(byteList[i]).Split('\n');
                        for (j = 0; j < str.Length; ++j)
                        {
                            if (!File.Exists(str[j])) continue;
                            lst.Add(DragDropInfo.Create(str[j]));
                        }
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
                    Settings.Load();
                    
                    if (String.IsNullOrEmpty(Settings.UToken))
                    {
                        frm = new frmPin();
                        Application.Run(instance.MainWindow = frm);

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
