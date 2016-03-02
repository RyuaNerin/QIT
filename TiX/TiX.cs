using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace TiX
{
	static class Program
	{
		public static string ProductName = String.Format("TiX v{0}", Application.ProductVersion);

		[STAThread]
		static void Main( string[] args )
		{
			const string mtxName = "Tix_Twitter";
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

				if ( String.IsNullOrEmpty( Settings.UToken ) ) Application.Run( new frmPin( ) );

				if ( !String.IsNullOrEmpty( Settings.UToken ) )
				{
					if ( args.Length == 0 )
					{
						Application.Run( new frmMain( ) );
					}
					else
					{
						if ( args.Length == 1 )
						{
							Application.Run( new TiX.ScreenCapture.Stasisfield( ) );
						}
						else if ( args.Length == 3 )
						{
							var targetuserid = args[1];
							var targettweetid = args[2];

							Application.Run( new TiX.ScreenCapture.Stasisfield( targetuserid, targettweetid ));
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
