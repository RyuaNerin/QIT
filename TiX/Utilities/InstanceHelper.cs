using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace TiX.Utilities
{
    internal sealed class InstanceHelper : IDisposable
    {
        private readonly string m_uniqueName;
        
        private int   m_wmMessage;
        private Mutex m_mutex;

        public InstanceHelper(string uniqueName)
        {
            this.m_uniqueName = uniqueName;
        }
        ~InstanceHelper()
        {
            this.Dispose(false);
        }
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
        private bool m_disposed;
        private void Dispose(bool disposing)
        {
            if (this.m_disposed)
                return;
            this.m_disposed = true;

            if (this.m_mutex != null)
            {
                this.m_mutex.Dispose();
                this.m_mutex = null;
            }
        }

        public bool LockOrActivate()
        {
            this.m_wmMessage = NativeMethods.RegisterWindowMessage($"WM_ACTIVATE_{this.m_uniqueName}");
            
            try
            {
                this.m_mutex = new Mutex(true, this.m_uniqueName);
            }
            catch
            {
            }

            if (this.m_mutex == null || !this.m_mutex.WaitOne(TimeSpan.Zero, true))
            {
                NativeMethods.PostMessage(
                    (IntPtr)NativeMethods.HWND_BROADCAST,
                    this.m_wmMessage,
                    IntPtr.Zero,
                    IntPtr.Zero);

                return false;
            }

            return true;
        }

        public void WaitOne()
        {
            if (this.m_mutex != null)
                this.m_mutex.WaitOne();
        }

        public int WMMessage => this.m_wmMessage;

        private static class NativeMethods
        {
            [DllImport("user32")]
            public static extern bool PostMessage(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam);

            [DllImport("user32")]
            public static extern int RegisterWindowMessage(string message);

            public const int HWND_BROADCAST = 0xffff;
        }
    }
}
