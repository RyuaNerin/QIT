using System.Drawing;
using TiX.Windows;

namespace TiX
{
	class TweetModerator
	{
		public static void Tweet(Image image, string title, string defaultText = "")
		{
			using (frmUpload frm = new frmUpload(DragDropInfo.Create(image)))
			{
				frm.AutoStart = false;
				frm.Text = title;
				frm.TweetString = defaultText;
				frm.ShowDialog();
			}
		}

		public static void Tweet(Image image, string title, string TargetUserID, string TargetTweetID)
		{
			using (frmUpload frm = new frmUpload(DragDropInfo.Create(image)))
			{
				frm.AutoStart = false;
				frm.Text = title;
				frm.TweetString = string.Format("@{0} ", TargetUserID);
				frm.InReplyToStatusId = TargetTweetID;
				frm.ShowDialog();
			}
		}
	}
}
