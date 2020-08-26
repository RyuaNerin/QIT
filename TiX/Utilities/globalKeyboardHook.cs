using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace TiX.Utilities
{
    internal class KeyHookEventArgs : EventArgs
    {
        public KeyHookEventArgs(ModifierKeys modifierKeys, Key key)
        {
            this.ModifierKeys = modifierKeys;
            this.Key = key;
        }

        public ModifierKeys ModifierKeys { get; }
        public Key Key { get; }

        public bool Handled { get; set; }
    }

	internal class GlobalKeyboardHook : IDisposable
	{
        public IList<(ModifierKeys, Key)> Down { get; } = new List<(ModifierKeys, Key)>();
        public IList<(ModifierKeys, Key)> Up   { get; } = new List<(ModifierKeys, Key)>();
        
		IntPtr m_hwnd = IntPtr.Zero;
		
		public event EventHandler<KeyHookEventArgs> KeyDown;
		public event EventHandler<KeyHookEventArgs> KeyUp;

		private readonly NativeMethods.keyboardHookProc khp;

		public GlobalKeyboardHook()
		{
            this.khp = new NativeMethods.keyboardHookProc(this.HookProc);
        }
        ~GlobalKeyboardHook()
        {
            this.Dispose(false);
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
                this.Unhook();
			}
		}

        private static IntPtr m_hLibrary = IntPtr.Zero;
		public void Hook()
		{
            if (this.m_hwnd == IntPtr.Zero)
            {
                if (m_hLibrary == IntPtr.Zero)
                    m_hLibrary = NativeMethods.LoadLibrary("User32");

                this.m_hwnd = NativeMethods.SetWindowsHookEx(NativeMethods.WH_KEYBOARD_LL, this.khp, m_hLibrary, 0);
            }
		}

		public void Unhook()
		{
            if (this.m_hwnd != IntPtr.Zero)
            {
			    NativeMethods.UnhookWindowsHookEx(this.m_hwnd);
                this.m_hwnd = IntPtr.Zero;
            }
		}

		private IntPtr HookProc(int code, IntPtr wParam, ref NativeMethods.KeyboardHookStruct lParam)
		{
			if (code >= 0)
			{
                var key = KeyInterop.KeyFromVirtualKey(lParam.vkCode);
				var wparam = wParam.ToInt64();

                var modifierKeys = Keyboard.Modifiers;

                if (this.KeyUp != null && this.Up.Contains((modifierKeys, key)))
                {
                    var arg = new KeyHookEventArgs(modifierKeys, key);
                    KeyUp.Invoke(this, arg);
                    if (arg.Handled)
                        return new IntPtr(1);
                }

                if (this.KeyDown != null && this.Down.Contains((modifierKeys, key)))
                {
                    var arg = new KeyHookEventArgs(modifierKeys, key);
                    KeyDown.Invoke(this, arg);
                    if (arg.Handled)
                        return new IntPtr(1);
                }
			}
			return NativeMethods.CallNextHookEx(this.m_hwnd, code, wParam, ref lParam);
		}

		private static class NativeMethods
		{
            public delegate IntPtr keyboardHookProc(int code, IntPtr wParam, ref KeyboardHookStruct lParam);

            [StructLayout(LayoutKind.Sequential)]
            public struct KeyboardHookStruct
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
			public static extern IntPtr CallNextHookEx(IntPtr idHook, int nCode, IntPtr wParam, ref KeyboardHookStruct lParam);

			[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
			public static extern IntPtr LoadLibrary(string lpFileName);
		}
	}
}
