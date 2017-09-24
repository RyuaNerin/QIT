using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace TiX.Utilities
{
    internal static class UacIcon
    {
        private static class NativeMethods
        {
            [DllImport("user32")]
            public static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

            public const int BCM_FIRST = 0x1600; //Normal button
            public const int BCM_SETSHIELD = (BCM_FIRST + 0x000C);
        }

        public static void SetUacIcon(bool isAdministratorMode, Control handle, bool enabled)
        {
            if (isAdministratorMode)
                return;

            if (enabled && !handle.Text.StartsWith(" "))
                handle.Text = " " + handle.Text;
            else if (!enabled && handle.Text.StartsWith(" ") && handle.Text.Length > 2)
                handle.Text = handle.Text.Substring(1);

            NativeMethods.SendMessage(handle.Handle, NativeMethods.BCM_SETSHIELD, IntPtr.Zero, new IntPtr(enabled ? 1 : 0));
        }
    }
}
