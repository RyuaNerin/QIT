using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
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
        
        private readonly GlobalKeyboardHook m_manager = new GlobalKeyboardHook();
        public frmMain(int wmMessage)
        {
            this.m_wmMessage = wmMessage;

            frmMain.Instance = this;

            InitializeComponent();
            this.Text = TiXMain.ProductName;
            this.Icon = TiX.Resources.TiX;
            
            this.TopMost = Settings.Instance.Topmost;
            if (Settings.Instance.ReversedCtrl)
                this.lblCtrl.Text = "Ctrl을 눌러 [내용] 작성";
            else
                this.lblCtrl.Text = "Ctrl을 눌러 [바로] 작성";
            
            this.m_manager.Down.Add(Keys.PrintScreen);
            this.m_manager.Down.Add(Keys.PrintScreen | Keys.Shift);
            this.m_manager.Down.Add(Keys.PrintScreen | Keys.Control);
            this.m_manager.Down.Add(Keys.PrintScreen | Keys.Alt);
            this.m_manager.KeyDown += this.GlobalKeyboardHook_KeyDown;
        }

        private readonly int m_wmMessage;
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == this.m_wmMessage)
            {
                if (this.WindowState == FormWindowState.Minimized)
                    this.WindowState = FormWindowState.Normal;

                var  topMost = this.TopMost;
                this.TopMost = true;
                this.TopMost = topMost;

                this.Activate();
                this.Focus();
            }

            base.WndProc(ref m);
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            if (Settings.Instance.StartInTray)
            {
                this.ntf.Visible = true;
                this.Hide();
            }
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            Settings.Instance.Save();
        }

        private void frmMain_Resize(object sender, EventArgs e)
        {
            if (Settings.Instance.MinizeToTray)
            {
                if (this.WindowState == FormWindowState.Minimized)
                {
                    this.ntf.Visible = true;
                    this.Hide();
                }
                else
                {
                    this.ntf.Visible = false;
                }
            }
        }

        private DateTime m_lastLeftUp = DateTime.MinValue;
        private void frmMain_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if ((DateTime.Now - this.m_lastLeftUp).TotalMilliseconds >= SystemInformation.DoubleClickTime)
                {
                    this.m_lastLeftUp = DateTime.Now;
                }
                else
                {
                    this.m_lastLeftUp = DateTime.MinValue;

                    if (this.ofd.ShowDialog() != DialogResult.OK)
                        return;

                    var autoStart = (Control.ModifierKeys & Keys.Control) == Keys.Control;
                    autoStart = (Settings.Instance.ReversedCtrl && !autoStart) || (!Settings.Instance.ReversedCtrl && autoStart);

                    TweetModerator.Tweet(this.ofd.FileNames, new TweetOption { AutoStart = autoStart });
                }
            }
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                using (var frm = new frmSettings())
                    frm.ShowDialog(this);

                this.TopMost = Settings.Instance.Topmost;
                if (Settings.Instance.ReversedCtrl)
                    this.lblCtrl.Text = "Ctrl을 눌러 [내용] 작성";
                else
                    this.lblCtrl.Text = "Ctrl을 눌러 [바로] 작성";
            }
        }

        private void frmMain_DragOverOrEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.None;

            if (ImageSet.IsAvailable(e))
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
                    e.Effect = DragDropEffects.Move | DragDropEffects.Link | DragDropEffects.Copy;
            }
        }

        private void frmMain_DragDrop(object sender, DragEventArgs e)
        {
            //                 A                              *  !B                       +   !A                              *  B
            //                 ==============================    =======================      ===============================    =======================
            //var autoStart = (Settings.Instance.ReversedCtrl && ((e.KeyState & 8) != 8)) || (!Settings.Instance.ReversedCtrl && ((e.KeyState & 8) == 8));

            // (A * !B) + (!A * B) = A ^ B
            var autoStart = Settings.Instance.ReversedCtrl ^ ((e.KeyState & 8) == 8);

            TweetModerator.Tweet(e.Data, new TweetOption { AutoStart = autoStart });
        }

        private void frmMain_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Modifiers == Keys.Control && e.KeyCode == Keys.V)
                TweetModerator.Tweet(Clipboard.GetDataObject(),
                    new TweetOption
                    {
                        AutoStart = false,
                        WindowTitle = "클립보드 이미지 전송중"
                    });
        }
        
        private readonly ManualResetEvent m_captureing = new ManualResetEvent(true);
        private void GlobalKeyboardHook_KeyDown(object sender, KeyHookEventArgs e)
        {
            if (!this.m_captureing.WaitOne(TimeSpan.Zero))
                return;

            this.Invoke(new Action<Keys>(this.GlobalKeyboardHook_KeyDown), e.Keys);

            e.Handled = true;
        }
        private void GlobalKeyboardHook_KeyDown(Keys key)
        {
            this.m_captureing.Reset();

            try
            {
                if ((key & Keys.Alt) == Keys.Alt)
                    CaptureCurrentScreen();
                else if ((key & Keys.Control) == Keys.Control)
                    CaptureCurrentWindow();
                else if ((key & Keys.Shift) == Keys.Shift)
                    CaptureAndClip();
                else
                    CaptureScreen();
            }
            catch
            {
                this.m_captureing.Set();
            }
        }
        private void CaptureScreen()
        {
            var sr = SystemInformation.VirtualScreen;

            CaptureScreen(new Rectangle(sr.Location, sr.Size));
        }
        private void CaptureCurrentScreen()
        {
            var fg = NativeMethods.GetForegroundWindow();
            if (fg == IntPtr.Zero)
            {
                this.m_captureing.Set();
                return;
            }
            
            var sr = Screen.FromHandle(fg).Bounds;
            CaptureScreen(new Rectangle(sr.Location, sr.Size));
        }
        private void CaptureScreen(Rectangle captureRect, bool hideTix = true)
        {
            if (hideTix)
            {
                this.Opacity = 0;
                this.ShowInTaskbar = false;
            }

            var capture = new Bitmap(captureRect.Width, captureRect.Height, PixelFormat.Format24bppRgb);
            using (Graphics g = Graphics.FromImage(capture))
                g.CopyFromScreen(captureRect.Left, captureRect.Top, 0, 0, captureRect.Size);

            if (hideTix)
            {
                this.ShowInTaskbar = true;
                this.Opacity = 255;
            }

            CaptureTweet(capture);
        }
        private void CaptureCurrentWindow()
        {
            var hwnd = NativeMethods.GetForegroundWindow();
            if (hwnd == IntPtr.Zero)
            {
                this.m_captureing.Set();
                return;
            }
            
            if (!NativeMethods.GetWindowRect(hwnd, out NativeMethods.RECT wRect))
            {
                this.m_captureing.Set();
                return;
            }

            var nRect = new Rectangle(wRect.Left, wRect.Top, wRect.Right - wRect.Left, wRect.Bottom - wRect.Top);
                        
            var img = new Bitmap(nRect.Width, nRect.Height, PixelFormat.Format32bppArgb);

            using (var g = Graphics.FromImage(img))
            {
                var hdc = g.GetHdc();
                NativeMethods.PrintWindow(hwnd, hdc, NativeMethods.PW_RENDERFULLCONTENT);
                g.ReleaseHdc(hdc);

                var hRgn = IntPtr.Zero;
                try
                {
                    hRgn = NativeMethods.CreateRectRgn(0, 0, 0, 0);

                    if (hRgn != IntPtr.Zero)
                    {
                        NativeMethods.GetWindowRgn(hwnd, hRgn);
                        var region = Region.FromHrgn(hRgn);
                        if (!region.IsEmpty(g))
                        {
                            g.ExcludeClip(region);
                            g.Clear(Color.Transparent);
                        }
                    }
                }
                finally
                {
                    if (hRgn != IntPtr.Zero)
                        NativeMethods.DeleteObject(hRgn);
                }
            }

            CaptureTweet(img);
        }
        private void CaptureAndClip()
        {
            this.Opacity = 0;
            this.ShowInTaskbar = false;

            Image cropedImage;
            using (var stasis = new Stasisfield())
            {
                stasis.ShowDialog();
                cropedImage = stasis.CropedImage;
            }

            this.ShowInTaskbar = true;
            this.Opacity = 255;

            if (cropedImage == null)
            {
                this.m_captureing.Set();
                return;
            }

            CaptureTweet(cropedImage);
        }
        private void CaptureTweet(Image image)
        {
            this.WindowState = FormWindowState.Normal;
            this.Show();

            TweetModerator.Tweet(image,
                new TweetOption
                {
                    CloseEvent = this.CaptureCompleted,
                    AutoStart = false,
                    WindowTitle = "캡처 화면 전송중"
                });
        }
        private void CaptureCompleted()
        {
            this.m_captureing.Set();
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

        private void ntf_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.ntf.Visible = false;
        }

        private static class NativeMethods
        {
            [DllImport("user32.dll")]
            public static extern IntPtr GetForegroundWindow();

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

            [DllImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool PrintWindow(IntPtr hwnd, IntPtr hDC, uint nFlags);

            [DllImport("gdi32.dll")]
            public static extern IntPtr CreateRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);

            [DllImport("user32.dll")]
            public static extern int GetWindowRgn(IntPtr hWnd, IntPtr hRgn);

            [DllImport("gdi32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool DeleteObject(IntPtr hObject);

            public const int ERROR = 0;
            public const int NULLREGION = 1;
            public const int SIMPLEREGION = 2;
            public const int COMPLEXREGION = 3;

            public const int PW_RENDERFULLCONTENT = 2;

            [StructLayout(LayoutKind.Sequential)]
            public struct RECT
            {
                public int Left;
                public int Top;
                public int Right;
                public int Bottom;
            }
        }
    }
}
