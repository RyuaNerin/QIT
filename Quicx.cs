﻿using System;
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
				// Write consumer data here
				Settings.CKey = "B0aTOpfzNlEKcPs80sSAcUFVe";
				Settings.CSecret = "3nCNQttOgjN3jbHxWddMzwGagZXcaMTzTWS9BjnT635TRpmRfc";


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
							new Quicx.ScreenCapture.Stasisfield( ).ShowDialog( );
						}
						else
						{
							string UnifiedStr = string.Empty;
							for ( int i = 0; i < args.Length; ++i )
							{
								using ( frmUpload frm = new frmUpload( ) )
								{
									if ( File.Exists( args[i] ) )
									{
										frm.AutoStart = false;
										frm.Text = String.Format( "{0} ({1} / {2})", Program.ProductName, i + 1, args.Length );

										if ( frm.SetImage( new DragDropInfo( args[i] ) ) )
										{
											frm.Index = i;
											if ( Settings.isUniformityText && i != 0 ) frm.SetText( UnifiedStr );
											frm.ShowDialog( );
											if ( Settings.isUniformityText && i == 0 ) UnifiedStr = frm.GetText( );
										}
										frm.Dispose( );
									}
								}
							}
						}
					}
				}
			}
		}
	}
}
