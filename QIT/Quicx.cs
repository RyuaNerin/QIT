using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace Quicx
{
	static class Program
	{
		public static string ProductName = String.Format("Quicx v{0}", Application.ProductVersion);

		[STAThread]
		static void Main( string[] args )
		{
			const string mtxName = "Quicx_Twitter";
			Mutex mtx = new Mutex(true, mtxName);

			TimeSpan tsWait = new TimeSpan(0, 0, 2);
			bool mtxSuccess = mtx.WaitOne(tsWait);

			if ( mtxSuccess )
			{
                // QIT
                Settings.CKey = "lQJwJWJoFlbvr2UQnDbg";
                Settings.CSecret = "DsuIRA1Ak9mmSCGl9wnNvjhmWJTmb9vZlRdQ7sMqXww";


				Twitter.TwitterAPI11.consumerToken = Settings.CKey;
				Twitter.TwitterAPI11.consumerSecret = Settings.CSecret;

				Application.EnableVisualStyles( );
				Application.SetCompatibleTextRenderingDefault( false );

				Settings.Load( );
				//if(Settings.isEnabledShell && Settings.lastExecutablePath != Application.ExecutablePath)
				////{
				//frmSettings.LaunchQuicxRegEditor(true);
				//}

				//Application.Run( new Quicx.ScreenCapture.Stasisfield() );
				//return;

				//Application.Run(new )

				if ( String.IsNullOrEmpty( Settings.UToken ) ) Application.Run( new frmPin( ) );

				if ( !String.IsNullOrEmpty( Settings.UToken ) )
				{
					if ( args.Length == 0 )
					{
						Application.Run( new frmMain( ) );
					}
					else
					{
						if ( args[0] == "Stasis" )
						{
							Application.Run( new Quicx.ScreenCapture.Stasisfield( ) );
						}
						else
						{
                            var lst = new List<DragDropInfo>();

							string UnifiedStr = string.Empty;
							for ( int i = 0; i < args.Length; ++i )
							{
                                if (!File.Exists(args[i])) continue;
                                lst.Add(DragDropInfo.Create(args[i]));
							}

                            var frm = new frmUpload(lst);
                            frm.AutoStart = false;

                            Application.Run(frm);
						}
					}
				}
			}
		}
	}
}
