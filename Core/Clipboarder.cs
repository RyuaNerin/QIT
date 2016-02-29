using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Quicx
{
	class Clipboarder
	{
		public static Image getClipboardImage()
		{
			if ( Clipboard.ContainsImage( ) )
			{
				using ( MemoryStream outStream = new MemoryStream( ) )
				{
					BitmapEncoder enc = new BmpBitmapEncoder();
					var bitmapsource = Clipboard.GetImage( );
                    enc.Frames.Add( BitmapFrame.Create( bitmapsource ) );
					enc.Save( outStream );
					var bitmap = new System.Drawing.Bitmap( outStream );
					return bitmap;
				}
			}
			return null;
		}
	}
}
