using System.Windows.Forms;

namespace TiX.Utilities
{
    internal static class MessageBoxes
    {
        public static DialogResult Error(this Form form, string text)
        {
            return MessageBox.Show(form, text, TiXMain.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public static DialogResult Infomation(this Form form, string text)
        {
            return MessageBox.Show(form, text, TiXMain.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public static DialogResult Question(this Form form, string text)
        {
            return MessageBox.Show(form, text, TiXMain.ProductName, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
        }
    }
}
