using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using TiX.ScreenCapture;
using TiX.Utilities;

namespace TiX
{
	static class Program
	{
		public static string ProductName = String.Format("Quicx v{0}", Application.ProductVersion);
        public const  string UniqueName = "C0E6D64A-23D2-4676-93F7-F4B9D8CE25DF";

        public static Form MainWindow { get; set; }

		[STAThread]
		static void Main( string[] args )
		{
            Form frm;

            using (var instance = new InstanceHelper(UniqueName))
            {
                if (instance.Check())
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);

                    Settings.Load();

                    if (String.IsNullOrEmpty(Settings.UToken))
                    {
                        frm = new frmPin();
                        instance.MainWindow = frm;
                        Application.Run(frm);

                        if (frm.DialogResult != DialogResult.OK)
                            return;
                    }
                    else
                    {
                        if (args.Length > 0)
                        {
                            if (args[0] == "stasis")
                            {
                                frm = new Stasisfield();
                                instance.MainWindow = frm;
                                Application.Run(frm);
                            }
                            else
                            {
                                var lst = new List<DragDropInfo>();

                                string UnifiedStr = string.Empty;
                                for (int i = 0; i < args.Length; ++i)
                                {
                                    if (!File.Exists(args[i])) continue;
                                    lst.Add(DragDropInfo.Create(args[i]));
                                }

                                frm = new frmUpload(lst, true) { AutoStart = false };
                                instance.MainWindow = frm;
                                Application.Run(frm);
                            }

                            return;
                        }
                    }

                    frm = new frmMain();
                    instance.MainWindow = frm;
                    Program.MainWindow = frm;
                    Application.Run(frm);
                }
            }
		}
	}
}
