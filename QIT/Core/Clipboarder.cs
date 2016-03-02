using System.Drawing;
using System.Windows.Forms;

namespace Quicx
{
	class Clipboarder
	{
		public static Image getClipboardImage()
		{
            try
            {
                return Clipboard.GetImage();
            }
            catch
            { }
            return null;
		}
	}
}
