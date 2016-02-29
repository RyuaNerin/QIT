using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quicx
{
	class TweetModerator
	{
		public static void Tweet(Image image, string title, string defaultText = "")
		{
			using ( frmUpload frm = new frmUpload( ) )
			{
				frm.AutoStart = false;
				frm.Text = title;
				frm.txtText.Text = defaultText;
				frm.SetImage( image );
				frm.ShowDialog( );
			}
		}
	}
}
