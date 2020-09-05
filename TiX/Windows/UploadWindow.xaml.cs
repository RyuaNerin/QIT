using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Linq;
using TiX.Core;

namespace TiX.Windows
{
    internal partial class UploadWindow : Window
    {
        private const int UrlTCoLength = 24;
        private const int MaxStatusLength = 280 - UrlTCoLength;

        private readonly ImageCollection m_imageCollection;

        public UploadWindow(ImageCollection imageCollection, bool isInstance)
        {
            this.InitializeComponent();

            this.ShowInTaskbar = isInstance;
            this.WindowStyle = isInstance ? WindowStyle.SingleBorderWindow : WindowStyle.ToolWindow;

            this.m_imageCollection = imageCollection;

            this.InputText_TextChanged(null, null);
        }

        private static readonly Regex regUrl = new Regex(@"https?:\/\/(-\.)?([^\s\/?\.#-]+\.?)+(\/[^\s]*)?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private void InputText_TextChanged(object sender, TextChangedEventArgs e)
        {
            var str = this.InputText.Text;
            var len = str.Length - regUrl.Matches(str)?.OfType<Match>().Select(m => m.Length - UrlTCoLength).Sum();
        }

        private void InputText_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && (Keyboard.Modifiers != ModifierKeys.None))
            {
                this.TweetButton_Click(sender, new RoutedEventArgs());
            }
        }

        private void TweetButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
