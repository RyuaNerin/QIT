using System;
using System.IO;
using System.Windows.Forms;

namespace QIT
{
	static class Program
	{
		public static string ProductName = String.Format("QITx v{0}", Application.ProductVersion);

		[STAThread]
		static void Main(string[] args)
		{
            // Write consumer data here
            Settings.CKey = "B0aTOpfzNlEKcPs80sSAcUFVe";
            Settings.CSecret = "3nCNQttOgjN3jbHxWddMzwGagZXcaMTzTWS9BjnT635TRpmRfc";

			Twitter.TwitterAPI11.consumerToken = Settings.CKey;
			Twitter.TwitterAPI11.consumerSecret = Settings.CSecret;

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			Settings.Load();

			if (String.IsNullOrEmpty(Settings.UToken))
				Application.Run(new frmPin());

			if (!String.IsNullOrEmpty(Settings.UToken))
			{
				if (args.Length == 0)
				{
					Application.Run(new frmMain());
				}
				else if (File.Exists(args[0]))
				{
					frmUpload frm = new frmUpload();

					frm.AutoStart = (Array.IndexOf(args, "/a") >= 0);

					if (frm.SetImage(new DragDropInfo(args[0])))
						Application.Run(frm);
				}
			}
		}
	}
}
