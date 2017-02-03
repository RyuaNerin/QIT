using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using TiX.Core;
using TiX.ScreenCapture;
using TiX.Utilities;

namespace TiX.Windows
{
    internal partial class frmMain : Form
    {
        public static Form Instance { get; private set; }

        private static GlobalKeyboardHook m_manager;
        public frmMain()
        {
            frmMain.Instance = this;

            InitializeComponent();
            this.Text = TiXMain.ProductName;
            this.Icon = TiX.Properties.Resources.TiX;
            
            this.TopMost = Settings.Topmost;
            if (Settings.ReversedCtrl)
                this.lblCtrl.Text = "Ctrl을 눌러 [내용] 작성";
            else
                this.lblCtrl.Text = "Ctrl을 눌러 [바로] 작성";
            
            m_manager = new GlobalKeyboardHook();
			m_manager.Down.Add(Keys.C | Keys.Control | Keys.Shift);
            m_manager.KeyDown += GlobalKeyboardHook_KeyDown;
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            Settings.Save();
        }

        private void frmMain_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                using (var frm = new frmSettings(false))
                    frm.ShowDialog();

                this.TopMost = Settings.Topmost;
                if (Settings.ReversedCtrl)
                    lblCtrl.Text = "Ctrl을 눌러 [내용] 작성";
                else
                    lblCtrl.Text = "Ctrl을 눌러 [바로] 작성";
            }
        }

        private void frmMain_DragOverOrEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.None;

            if (ImageCollection.IsAvailable(e))
            {
                bool allow = true;

                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop);

                    allow = false;
                    for (int i = 0; i < paths.Length; ++i)
                    {
                        if (TiXMain.CheckFile(paths[i]))
                        {
                            allow = true;
                            break;
                        }
                    }
                }

                if (allow)
                    e.Effect = DragDropEffects.Move;
            }
        }

        private void frmMain_DragDrop(object sender, DragEventArgs e)
        {
            var autoStart = (Settings.ReversedCtrl && ((e.KeyState & 8) != 8)) || (!Settings.ReversedCtrl && ((e.KeyState & 8) == 8));

            TweetModerator.Tweet(e.Data, autoStart);
        }

        private void frmMain_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Modifiers == Keys.Control && e.KeyCode == Keys.V)
                TweetModerator.Tweet(Clipboard.GetDataObject(), false, "클립보드 이미지 전송중");
        }
        
        private long m_stasis = 0;
        private void GlobalKeyboardHook_KeyDown(object sender, KeyHookEventArgs e)
        {
            if (Interlocked.CompareExchange(ref this.m_stasis, 1, 0) == 1) return;

            this.Invoke(new Action(this.GlobalKeyboardHook_KeyDown));
        }
        private void GlobalKeyboardHook_KeyDown()
        {
            GenerateStasisField();
            Interlocked.Exchange(ref this.m_stasis, 0);
        }
        private void GenerateStasisField()
        {
            this.Opacity = 0;
            this.ShowInTaskbar = false;

            Image cropedImage;
            using (var stasis = new Stasisfield())
            {
                stasis.ShowDialog();
                cropedImage   = stasis.CropedImage;
            }

            this.ShowInTaskbar = true;
            this.Opacity = 255;

            if (cropedImage != null)
                TweetModerator.Tweet(cropedImage, false, "캡처 화면 전송중");
        }
        
        internal class CallbackData
        {
            public ImageCollection Collection;
            public bool         Callback;
            public IAsyncResult IAsyncResult;
            public bool         AutoStart;
            public string       DefaultText;
            public string       InReplyToStatusId;
        }
        internal static void Callback(CallbackData data)
        {
            if (data.Callback)
                frmMain.Instance.EndInvoke(data.IAsyncResult);
            
            var frm = new frmUpload(data.Collection);
            frm.AutoStart           = data.AutoStart;
            frm.TweetString         = data.DefaultText;
            frm.InReplyToStatusId   = data.InReplyToStatusId;

            frm.FormClosed += (s, e) => frm.Dispose();

            if (frmMain.Instance != null)
                frm.Show(frmMain.Instance);
            else
                Application.Run(frm);
        }

        private void frmMain_Shown(object sender, EventArgs e)
        {
            Task.Factory.StartNew(new Action(() =>
            {
                var update = LastRelease.CheckNewVersion();
                if (update != null)
                    if (MessageBox.Show(this, "새 업데이트가 있어요!", Application.ProductName, MessageBoxButtons.OKCancel, MessageBoxIcon.Information) == DialogResult.OK)
                        Process.Start(new ProcessStartInfo { UseShellExecute = true, FileName = string.Format("\"{0}\"", update.HtmlUrl) }).Dispose();
            }));
        }
    }
}
