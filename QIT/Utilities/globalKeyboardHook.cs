using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace TiX.Utilities
{
    public delegate void KeyHookEvent(object sender, KeyHookEventArgs e);
    
    public class KeyHookEventArgs : EventArgs
    {
        public KeyHookEventArgs(HookKey hookkey)
        {
            this.HookKey = hookkey;
        }
        public bool     Handled  { get; set; }
        public HookKey  HookKey  { get; private set; }
    }

    public struct HookKey
    {
        public HookKey(Keys key)
        {
            this.Key = key;
            this.Control = this.Shift = this.Alt = this.Window = false;
        }
        public HookKey(Keys key, bool control, bool shift, bool alt, bool window)
        {
            this.Key = key;
            this.Control = control;
            this.Shift = shift;
            this.Alt = alt;
            this.Window = window;
        }
        public Keys Key;
        public bool Control;
        public bool Shift;
        public bool Alt;
        public bool Window;

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj is HookKey ? this == (HookKey)obj : false;
        }

        public static bool operator ==(HookKey a, HookKey b)
        {
            return a.Key == b.Key && a.Control == b.Control && a.Shift == b.Shift && a.Alt == b.Alt && a.Window == b.Window;
        }
        public static bool operator !=(HookKey a, HookKey b)
        {
            return a.Key != b.Key || a.Control != b.Control || a.Shift != b.Shift || a.Alt != b.Alt || a.Window != b.Window;
        }
    }

	public class GlobalKeyboardHook : IDisposable
	{
        private List<HookKey> m_down = new List<HookKey>();
        public IList<HookKey> Down { get { return this.m_down; } }

        private List<HookKey> m_up = new List<HookKey>();
        public IList<HookKey> Up { get { return this.m_up; } }
        
		IntPtr m_hwnd = IntPtr.Zero;
        bool m_control = false;
        bool m_shift = false;
        bool m_alt = false;
        bool m_window = false;
		
		public event KeyHookEvent KeyDown;
		public event KeyHookEvent KeyUp;

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
                            this.m_control = d;
                            break;

                        case Keys.Shift:
                        case Keys.ShiftKey:
                        case Keys.LShiftKey:
                        case Keys.RShiftKey:
                            this.m_shift = d;
                            break;

                        case Keys.Alt:
                        case Keys.Menu:
                        case Keys.RMenu:
                        case Keys.LMenu:
                            this.m_alt = d;
                            break;
                                                        
                        case Keys.LWin:
                        case Keys.RWin:
                            this.m_window = d;
                            break;
                    }
                }

                var hkey = new HookKey(key, this.m_control, this.m_shift, this.m_alt, this.m_window);

                if (this.KeyUp != null && this.Up.Contains(hkey))
                {
                    var arg = new KeyHookEventArgs(hkey);
                    KeyUp.Invoke(this, arg);
                    if (arg.Handled)
                        return new IntPtr(1);
                }

                if (this.KeyDown != null && this.Down.Contains(hkey))
                {
                    var arg = new KeyHookEventArgs(hkey);
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
			public static extern bool UnhookWindowsHookEx(IntPtr hInstance);

			[DllImport("user32.dll")]
			public static extern IntPtr CallNextHookEx(IntPtr idHook, int nCode, IntPtr wParam, ref keyboardHookStruct lParam);

			[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
			public static extern IntPtr LoadLibrary(string lpFileName);
		}
	}
}
