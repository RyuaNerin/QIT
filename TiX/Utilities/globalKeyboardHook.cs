using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace TiX.Utilities
{
    internal class KeyHookEventArgs : EventArgs
    {
        public KeyHookEventArgs(Keys key)
        {
            this.Keys = key;
        }
        public bool  Handled  { get; set; }
        public Keys  Keys     { get; private set; }
    }

	internal class GlobalKeyboardHook : IDisposable
	{
        private List<Keys> m_down = new List<Keys>();
        public IList<Keys> Down { get { return this.m_down; } }

        private List<Keys> m_up = new List<Keys>();
        public IList<Keys> Up { get { return this.m_up; } }
        
		IntPtr m_hwnd = IntPtr.Zero;
        Keys m_control = Keys.None;
        Keys m_shift   = Keys.None;
        Keys m_alt     = Keys.None;
		
		public event EventHandler<KeyHookEventArgs> KeyDown;
		public event EventHandler<KeyHookEventArgs> KeyUp;

		NativeMethods.keyboardHookProc khp;

		public GlobalKeyboardHook()
		{
			khp = new NativeMethods.keyboardHookProc(hookProc);
			Hook();
		}

		private bool m_disposed = false;
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			if (this.m_disposed) return;
			this.m_disposed = true;

			if (disposing)
			{
				Unhook();
			}
		}

		~GlobalKeyboardHook()
		{
			this.Dispose(false);
		}

        private static IntPtr m_hLibrary = IntPtr.Zero;
		public void Hook()
		{
            if (m_hwnd == IntPtr.Zero)
            {
                if (m_hLibrary == IntPtr.Zero)
                    m_hLibrary = NativeMethods.LoadLibrary("User32");

                m_hwnd = NativeMethods.SetWindowsHookEx(NativeMethods.WH_KEYBOARD_LL, khp, m_hLibrary, 0);
            }
		}

		public void Unhook()
		{
            if (m_hwnd != IntPtr.Zero)
            {
			    NativeMethods.UnhookWindowsHookEx(m_hwnd);
                m_hwnd = IntPtr.Zero;
            }
		}

		private IntPtr hookProc(int code, IntPtr wParam, ref NativeMethods.keyboardHookStruct lParam)
		{
			if (code >= 0)
			{
                var key = (Keys)lParam.vkCode;
				var wparam = wParam.ToInt64();
				bool d = (wparam == NativeMethods.WM_KEYDOWN || wparam == NativeMethods.WM_SYSKEYDOWN);
				bool u = (wparam == NativeMethods.WM_KEYUP   || wparam == NativeMethods.WM_SYSKEYUP);

                if (d ^ u)
                {
                    switch (key)
                    {
                        case Keys.Control:
                        case Keys.ControlKey:
                        case Keys.LControlKey:
                        case Keys.RControlKey:
                            this.m_control = d ? Keys.Control : Keys.None;
                            break;

                        case Keys.Shift:
                        case Keys.ShiftKey:
                        case Keys.LShiftKey:
                        case Keys.RShiftKey:
                            this.m_shift = d ? Keys.Shift : Keys.None;
                            break;

                        case Keys.Alt:
                        case Keys.Menu:
                        case Keys.RMenu:
                        case Keys.LMenu:
                            this.m_alt = d ? Keys.Alt : Keys.None;
                            break;
                    }
                }

                key = key | this.m_control | this.m_shift | this.m_alt;

                if (this.KeyUp != null && this.Up.Contains(key))
                {
                    var arg = new KeyHookEventArgs(key);
                    KeyUp.Invoke(this, arg);
                    if (arg.Handled)
                        return new IntPtr(1);
                }

                if (this.KeyDown != null && this.Down.Contains(key))
                {
                    var arg = new KeyHookEventArgs(key);
                    KeyDown.Invoke(this, arg);
                    if (arg.Handled)
                        return new IntPtr(1);
                }
			}
			return NativeMethods.CallNextHookEx(m_hwnd, code, wParam, ref lParam);
		}

		private static class NativeMethods
		{
            public delegate IntPtr keyboardHookProc(int code, IntPtr wParam, ref keyboardHookStruct lParam);

            [StructLayout(LayoutKind.Sequential)]
            public struct keyboardHookStruct
            {
                public int vkCode;
                public int scanCode;
                public int flags;
                public int time;
                public int dwExtraInfo;
            }

            public const int WH_KEYBOARD_LL = 13;
            public const int WM_KEYDOWN = 0x100;
            public const int WM_KEYUP = 0x101;
            public const int WM_SYSKEYDOWN = 0x104;
            public const int WM_SYSKEYUP = 0x105;
            
			[DllImport("user32.dll")]
			public static extern IntPtr SetWindowsHookEx(int idHook, keyboardHookProc callback, IntPtr hInstance, uint threadId);

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
			public static extern bool UnhookWindowsHookEx(IntPtr hInstance);

			[DllImport("user32.dll")]
			public static extern IntPtr CallNextHookEx(IntPtr idHook, int nCode, IntPtr wParam, ref keyboardHookStruct lParam);

			[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
			public static extern IntPtr LoadLibrary(string lpFileName);
		}
	}
}
