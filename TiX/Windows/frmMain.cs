using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using TiX.Utilities;
using System.Threading;
using System.Drawing;

namespace TiX.Windows
{
    public partial class frmMain : Form
    {
        public static Form Instance { get; private set; }

        private static GlobalKeyboardHook manager;
        public frmMain()
        {
            InitializeComponent();
            frmMain.Instance = this;

            this.Text = Program.ProductName;
            this.TopMost = Settings.Topmost;
            if (Settings.ReversedCtrl)
                label2.Text = "Ctrl을 눌러 [내용] 작성";
            else
                label2.Text = "Ctrl을 눌러 [바로] 작성";


            manager = new GlobalKeyboardHook();
			manager.Down.Add(Keys.C | Keys.Control | Keys.Shift);
            manager.KeyDown += GlobalKeyboardHook_KeyDown;

			this.KeyDown += FrmMain_KeyDown;
        }

		private void FrmMain_KeyDown( object sender, System.Windows.Forms.KeyEventArgs e )
		{
			if ( e.Modifiers == Keys.Control && e.KeyCode == Keys.V )
			{
				using (var clipImage = Clipboarder.getClipboardImage())
                {
                    if (clipImage == null) return;
                    TweetModerator.Tweet(clipImage, "클립보드 이미지 전송중");
                }
			}
		}

        private long m_stasis = 0;
        private void GlobalKeyboardHook_KeyDown(object sender, KeyHookEventArgs e)
        {
            this.BeginInvoke(new Action(this.GlobalKeyboardHook_KeyDown));
        }
        private void GlobalKeyboardHook_KeyDown()
        {
            if (Interlocked.CompareExchange(ref this.m_stasis, 1, 0) == 1) return;

            GenerateStasisField();
            Interlocked.Exchange(ref this.m_stasis, 0);
        }

        private void pnl_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.None;

            if (DragDropInfo.isAvailable(e))
            {
                bool allow = true;

                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop);

                    allow = false;
                    for (int i = 0; i < paths.Length; ++i)
                    {
                        if (Array.IndexOf<string>(Program.AllowExtension, Path.GetExtension(paths[i]).ToLower()) >= 0)
                        {
                            allow = true;
                            break;
                        }
                    }
                }

                if (allow) e.Effect = DragDropEffects.Move;
            }
        }

        private void pnl_DragDrop(object sender, DragEventArgs e)
        {
            try
            {                
                var data = new CallbackData();

                // Local Files
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop);

                    if (paths.Length > 10)
                        if (MessageBox.Show(
                            this,
                            String.Format("정말 {0} 개의 이미지를 트윗하시겠습니까?", paths.Length),
                            Program.ProductName,
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question) == DialogResult.No)
                            return;

                    for (int i = 0; i < paths.Length; ++i)
                        if (Array.IndexOf<string>(Program.AllowExtension, Path.GetExtension(paths[i]).ToLower()) >= 0)
                            data.List.Add(DragDropInfo.Create(paths[i]));
                }
                else
                {
                    DragDropInfo info = DragDropInfo.Create(e);
                    if (info != null)
                        data.List.Add(info);
                    else
                        info.Dispose();
                }

                if (data.List.Count > 0)
                {
                    data.AutoStart = ((e.KeyState & 8) == 8);
                    data.IAsyncResult = this.BeginInvoke(new Action<CallbackData>(this.Callback), data);
                }
            }
            catch
            { }
        }

        private class CallbackData
        {
            public bool AutoStart;
            public List<DragDropInfo> List = new List<DragDropInfo>();
            public IAsyncResult IAsyncResult;
        }
        private void Callback(CallbackData data)
        {
            this.EndInvoke(data.IAsyncResult);

            var frm = new frmUpload(data.List);
            frm.AutoStart = data.AutoStart;
            frm.Show(this);
        }

        private void pnl_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.None;

            if (DragDropInfo.isAvailable(e))
            {
                bool allow = true;

                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop);

                    for (int i = 0; i < paths.Length; ++i)
                    {
                        if (Array.IndexOf<string>(Program.AllowExtension, Path.GetExtension(paths[i]).ToLower()) < 0)
                        {
                            allow = false;
                            break;
                        }
                    }
                }

                if (allow)
                    e.Effect = DragDropEffects.Move;
            }
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            Settings.Save();
        }

        private void pnl_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {

            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                using (var frm = new frmSettings())
                    frm.ShowDialog();

                this.TopMost = Settings.Topmost;
                if (Settings.ReversedCtrl)
                    label2.Text = "Ctrl을 눌러 [내용] 작성";
                else
                    label2.Text = "Ctrl을 눌러 [바로] 작성";
            }
        }

		private void GenerateStasisField()
        {
            this.Opacity = 0;
			this.ShowInTaskbar = false;

            Image cropedImage;
            string targetTweetId;
            string targetUserId;
			using (var stasis = new TiX.ScreenCapture.Stasisfield( ))
            {
                stasis.ShowDialog( );
                cropedImage   = stasis.CropedImage;
                targetUserId  = stasis.TargetUserID;
                targetTweetId = stasis.TargetTweetID;
            }

			this.ShowInTaskbar = true;
			this.Opacity = 255;

            if (cropedImage != null)
            {
                using (cropedImage)
                {
                    if (!string.IsNullOrEmpty(targetUserId) || !string.IsNullOrEmpty(targetTweetId))
                        TweetModerator.Tweet(cropedImage, "캡처 화면 전송중", targetUserId, targetTweetId);
                    else
                        TweetModerator.Tweet(cropedImage, "캡처 화면 전송중");
                }
            }
		}
    }
}
