using System.Drawing;
using System.Windows.Forms;

namespace TiX
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
            { 
				return null;
			}
		}
	}
}
