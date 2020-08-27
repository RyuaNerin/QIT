using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using CoreTweet;
using TiX.Core;
using TiX.Utilities;

namespace TiX.Windows
{
    public partial class PinWindow : Window
    {
        private readonly int m_wmMessage;
        public PinWindow(int wmMessage)
        {
            this.InitializeComponent();

            this.m_wmMessage = wmMessage;
        }

        private IntPtr m_handle;

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var source = PresentationSource.FromVisual(this) as HwndSource;
            source.AddHook(this.WndProc);
            this.m_handle = source.Handle;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            NativeMethods.RemoveClipboardFormatListener(this.m_handle);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (this.m_wmMessage != 0 && msg == this.m_wmMessage)
            {
                this.WindowState = WindowState.Normal;
                this.Activate();
            }
            else if (msg == 0x031D) // WM_CLIPBOARDUPDATE
            {
                var str = Clipboard.GetText();
                if (!string.IsNullOrWhiteSpace(str) && str.Length == 7 && int.TryParse(str, out int i))
                {
                    this.InputPin.Text = str;

                    this.WindowState = WindowState.Normal;
                    this.Activate();
                }
            }

            return IntPtr.Zero;
        }

        private void Window_GotFocus(object sender, RoutedEventArgs e)
        {
            this.InputPin.Focus();
        }

        private OAuth.OAuthSession m_oauthSession;
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.m_oauthSession = await OAuth.AuthorizeAsync("consumer_key", "consumer_secret");
            if (this.m_oauthSession == null)
            {
                this.Error("문제가 발생했어요 :(");
                this.Close();
                return;
            }
            Process.Start(new ProcessStartInfo { FileName = this.m_oauthSession.AuthorizeUri.ToString(), UseShellExecute = true })?.Dispose();

            NativeMethods.AddClipboardFormatListener(this.m_handle);

            this.ProgressRing.IsActive = false;
            this.InputPin.IsEnabled = true;
        }

        private async void InputPin_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    NativeMethods.RemoveClipboardFormatListener(this.m_handle);
                    this.InputPin.IsEnabled = false;


                    var tokens = await this.m_oauthSession.GetTokensAsync(this.InputPin.Text);
                    if (tokens == null)
                    {
                        this.Error("핀번호를 확인해주세요 :(");
                        this.InputPin.IsEnabled = true;
                        return;
                    }

                    Settings.Instance.UToken = tokens.AccessToken;
                    Settings.Instance.USecret = tokens.AccessTokenSecret;
                    Settings.Instance.Save();

                    this.DialogResult = true;
                    this.Hide();
                    TiXMain.AppMain();
                    this.Close();

                    break;

                case Key.Escape:
                    this.Close();
                    break;

                case Key.Back:
                case Key.Delete:
                case Key k when (Key.D0 <= k && k <= Key.D9) || (Key.NumPad0 <= k && k <= Key.NumPad9):
                    return;

                case Key.C when (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control:
                    var str = Clipboard.GetText();
                    if (str.Length == 7 && int.TryParse(str, out _)) return;

                    break;
            }

            e.Handled = true;
        }

        private static class NativeMethods
        {
            [DllImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool AddClipboardFormatListener(IntPtr hwnd);

            [DllImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool RemoveClipboardFormatListener(IntPtr hwnd);
        }
    }
}
