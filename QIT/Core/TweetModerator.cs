using System.Drawing;

namespace TiX
{
	class TweetModerator
	{
		public static void Tweet(Image image, string title, string defaultText = "")
		{
			using ( frmUpload frm = new frmUpload(DragDropInfo.Create(image)) )
			{
				frm.AutoStart = false;
				frm.Text = title;
				frm.TweetString = defaultText;
				frm.ShowDialog( );
			}
		}
	}
}
