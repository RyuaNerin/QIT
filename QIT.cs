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
            {
                // Write consumer data here
                Settings.CKey = "B0aTOpfzNlEKcPs80sSAcUFVe";
                Settings.CSecret = "3nCNQttOgjN3jbHxWddMzwGagZXcaMTzTWS9BjnT635TRpmRfc";


                Twitter.TwitterAPI11.consumerToken = Settings.CKey;
                Twitter.TwitterAPI11.consumerSecret = Settings.CSecret;

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                Settings.Load();
                if(Settings.isEnabledShell && Settings.lastExecutablePath != Application.ExecutablePath)
                {
                    frmSettings.LaunchQITxRegEditor(true);
                }

                if (String.IsNullOrEmpty(Settings.UToken)) Application.Run(new frmPin());

                if (!String.IsNullOrEmpty(Settings.UToken))
                {
                    if (args.Length == 0)
                    {
                        Application.Run(new frmMain());
                    }
                    else
                    {
                        string UnifiedStr = string.Empty;
                        for (int i = 0; i < args.Length; ++i)
                        {
                            using (frmUpload frm = new frmUpload())
                            {
                                if (File.Exists(args[i]))
                                {
                                    frm.AutoStart = false;
                                    frm.Text = String.Format("{0} ({1} / {2})", Program.ProductName, i + 1, args.Length);

                                    if (frm.SetImage(new DragDropInfo(args[i])))
                                    {
                                        frm.Index = i;
                                        if (Settings.isUniformityText && i != 0) frm.SetText(UnifiedStr);
                                        frm.ShowDialog();
                                        if (Settings.isUniformityText && i == 0) UnifiedStr = frm.GetText();
                                    }
                                    frm.Dispose();
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
