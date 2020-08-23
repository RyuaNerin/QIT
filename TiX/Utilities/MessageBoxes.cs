using System.Windows;

namespace TiX.Utilities
{
    internal static class MessageBoxes
    {
        public static MessageBoxResult Error(this Window window, string text)
            => MessageBox.Show(window, text, TiXMain.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);

        public static MessageBoxResult Infomation(this Window window, string text)
            => MessageBox.Show(window, text, TiXMain.ProductName, MessageBoxButton.OK, MessageBoxImage.Information);

        public static MessageBoxResult Question(this Window window, string text)
            => MessageBox.Show(window, text, TiXMain.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
    }
}
