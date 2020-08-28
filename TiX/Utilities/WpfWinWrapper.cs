using System;
using System.Windows;
using System.Windows.Forms;

namespace TiX.Utilities
{
    internal class WindowWrapper : IWin32Window
    {
        public WindowWrapper(Window window)
        {
            this.Handle = new System.Windows.Interop.WindowInteropHelper(window).Handle;
        }

        public IntPtr Handle { get; }
    }
}
