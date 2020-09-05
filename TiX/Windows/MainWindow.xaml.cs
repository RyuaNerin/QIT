using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Microsoft.Win32;
using TiX.Core;
using TiX.Utilities;

using NotifyIcon = System.Windows.Forms.NotifyIcon;
using Screen = System.Windows.Forms.Screen;

namespace TiX.Windows
{
    public partial class MainWindow : Window
    {
        public static MainWindow Instance { get; private set; }

        private readonly GlobalKeyboardHook m_manager = new GlobalKeyboardHook();
        private readonly NotifyIcon m_ntf = new NotifyIcon();
        private readonly int m_wmMessage;

        public MainWindow(int wmMessage)
        {
            this.InitializeComponent();

            Instance = this;

            this.m_ntf.Icon = Properties.Resources.TiX;

            this.m_wmMessage = wmMessage;

            this.Title = TiXMain.ProductName;

            this.m_manager.Down.Add((ModifierKeys.None, Key.PrintScreen));
            this.m_manager.Down.Add((ModifierKeys.Shift, Key.PrintScreen));
            this.m_manager.Down.Add((ModifierKeys.Control, Key.PrintScreen));
            this.m_manager.Down.Add((ModifierKeys.Alt, Key.PrintScreen));
            this.m_manager.KeyDown += this.GlobalKeyboardHook_KeyDown;

            this.m_ntf.Click += this.Notify_Click;
        }

        private void Notify_Click(object sender, EventArgs e)
        {
            this.Activate();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            if (this.m_wmMessage != 0)
            {
                var source = PresentationSource.FromVisual(this) as HwndSource;
                source.AddHook(this.WndProc);
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == this.m_wmMessage)
            {
                this.Activate();
            }

            return IntPtr.Zero;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (Settings.Instance.StartInTray)
            {
                this.m_ntf.Visible = true;
                this.Hide();
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Settings.Instance.Save();
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (Settings.Instance.MinizeToTray)
            {
                if (this.WindowState == WindowState.Minimized)
                {
                    this.m_ntf.Visible = true;
                    this.Hide();
                }
                else
                {
                    this.m_ntf.Visible = false;
                }
            }
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Right)
            {
                var win = new ConfigWindow
                {
                    Owner = this
                };
                win.ShowDialog();
            }
        }

        private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                var ofd = new OpenFileDialog
                {
                    Filter = "지원하는 모든 파일|*.bmp;*.emf;*.exif;*.gif;*.ico;*.jpg;*.jpeg;*.png;*.tif;*.tiff;*.wmf;*.psd",
                    Title = "트윗할 이미지들을 선택해주세요",
                    Multiselect = true,
                };

                if (!ofd.ShowDialog(this) ?? false)
                    return;

                var autoStart = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
                autoStart = (Settings.Instance.ReversedCtrl && !autoStart) || (!Settings.Instance.ReversedCtrl && autoStart);

                TweetModerator.Tweet(ofd.FileNames, new TweetOption { AutoStart = autoStart });
            }
        }

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.None;

            if (ImageSet.IsAvailable(e.Data))
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
                    e.Effects = DragDropEffects.Move | DragDropEffects.Link | DragDropEffects.Copy;
            }
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            //                 A                              *  !B                       +   !A                              *  B
            //                 ==============================    =======================      ===============================    =======================
            //var autoStart = (Settings.Instance.ReversedCtrl && ((e.KeyState & 8) != 8)) || (!Settings.Instance.ReversedCtrl && ((e.KeyState & 8) == 8));

            // (A * !B) + (!A * B) = A ^ B
            var autoStart = Settings.Instance.ReversedCtrl ^ ((e.KeyStates & DragDropKeyStates.ControlKey) == DragDropKeyStates.ControlKey);

            TweetModerator.Tweet(e.Data, new TweetOption { AutoStart = autoStart });
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.V)
                TweetModerator.Tweet(
                    Clipboard.GetDataObject(),
                    new TweetOption
                    {
                        AutoStart = false,
                        WindowTitle = "클립보드 이미지 전송중"
                    });
        }

        private int m_captureing = 0;
        private void GlobalKeyboardHook_KeyDown(object sender, KeyHookEventArgs e)
        {
            this.Dispatcher.Invoke(new Action<ModifierKeys, Key>(this.CaptureStart), e.ModifierKeys, e.Key);

            e.Handled = true;
        }
        private void CaptureStart(ModifierKeys modifierKeys, Key key)
        {
            if (Interlocked.Exchange(ref this.m_captureing, 1) != 0)
                return;

            try
            {
                switch (modifierKeys)
                {
                    case ModifierKeys.Alt:
                        this.CaptureCurrentScreen();
                        break;

                    case ModifierKeys.Control:
                        this.CaptureCurrentWindow();
                        break;

                    case ModifierKeys.Shift:
                        this.CaptureAndClip();
                        break;

                    default:
                        this.CaptureScreen();
                        break;
                }
            }
            catch
            {
                Interlocked.Exchange(ref this.m_captureing, 0);
            }
        }
        private void CaptureEnd()
        {
            Interlocked.Exchange(ref this.m_captureing, 0);
        }

        private void CaptureScreen()
        {
            this.CaptureScreenWithRectangle(System.Windows.Forms.SystemInformation.VirtualScreen);
        }
        private void CaptureCurrentScreen()
        {
            var fg = NativeMethods.GetForegroundWindow();
            if (fg == IntPtr.Zero)
            {
                this.CaptureEnd();
                return;
            }

            var sr = Screen.FromHandle(fg).Bounds;
            this.CaptureScreenWithRectangle(new Rectangle(sr.Location, sr.Size));
        }
        private void CaptureScreenWithRectangle(Rectangle captureRect, bool hideTix = true)
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

            this.CaptureTweetImage(capture);
        }
        private void CaptureCurrentWindow()
        {
            var hwnd = NativeMethods.GetForegroundWindow();
            if (hwnd == IntPtr.Zero)
            {
                this.CaptureEnd();
                return;
            }

            if (!NativeMethods.GetWindowRect(hwnd, out NativeMethods.RECT wRect))
            {
                this.CaptureEnd();
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

            this.CaptureTweetImage(img);
        }
        private void CaptureAndClip()
        {
            this.Opacity = 0;
            this.ShowInTaskbar = false;

            var win = new CaptureWindow
            {
                Owner = this
            };
            var captured = win.ShowDialog() ?? false;

            this.ShowInTaskbar = true;
            this.Opacity = 255;


            if (captured)
            {
                this.CaptureEnd();
                return;
            }

            this.CaptureTweetImage(win.CropedImage);
        }
        private void CaptureTweetImage(Image image)
        {
            this.WindowState = WindowState.Normal;
            this.Show();

            TweetModerator.Tweet(
                image,
                new TweetOption
                {
                    CloseEvent = this.CaptureEnd,
                    AutoStart = false,
                    WindowTitle = "캡처 화면 전송중"
                });
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
