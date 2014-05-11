using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace QIT
{
	static class Program
	{
		// Edited
		public static string CKey = 0;
		public static string CSecret = 0;
		public static string UToken = null;
		public static string USecret = null;

		/// <summary>
		/// 해당 응용 프로그램의 주 진입점입니다.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			Twitter.TwitterAPI11.consumerToken = Program.CKey;
			Twitter.TwitterAPI11.consumerSecret = Program.CSecret;

			string path = Path.Combine(Application.StartupPath, "QIT.info");

			try
			{
				string file = File.ReadAllText(path, Encoding.UTF8);

				UToken = file.Substring(0, file.IndexOf('.'));
				USecret = file.Substring(file.IndexOf('.') + 1);
			}
			catch
			{ }

			if (String.IsNullOrEmpty(UToken))
			{
				Application.Run(new frmPin());
				File.WriteAllText(path, String.Format("{0}.{1}", Program.UToken, Program.USecret), Encoding.UTF8);
			}

			if (args.Length == 0)
			{
				Application.Run(new frmMain());
			}
			else
			{
				if (!String.IsNullOrEmpty(UToken))
				{
					frmUpload frm = new frmUpload();

					frm.SetImage(args[0]);

					frm.AutoStart = (Array.IndexOf(args, "/a") >= 0);
						
					Application.Run(frm);
				}
			}
		}
	}
}
