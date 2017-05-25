using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using SHDocVw;
using Shell32;

namespace TiX.Utilities
{
    internal class ExplorerRestarter : IDisposable
    {
        private readonly List<List<string>> m_opened = new List<List<string>>();
        private readonly object m_orignalValue;
        private readonly RegistryValueKind m_orignalKind;

        private bool m_disposed = false;

        public ExplorerRestarter()
        {
            using (var reg = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon", true))
            {
                this.m_orignalValue = reg.GetValue("AutoRestartShell", null);
                if (this.m_orignalValue != null)
                    this.m_orignalKind = reg.GetValueKind("AutoRestartShell");

                reg.SetValue("AutoRestartShell", 0);
            }

            var exp = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe");

            ShellWindows shellWindows = new ShellWindows();
            foreach (InternetExplorer window in shellWindows)
            {
                if (window.FullName == exp)
                {
                    var list = new List<string>()
                    {
                        window.LocationURL
                    };

                    FolderItems items = ((IShellFolderViewDual2)window.Document).SelectedItems();
                    foreach (FolderItem item in items)
                        list.Add(item.Path);

                    this.m_opened.Add(list);
                }
            }

            foreach (var proc in Process.GetProcessesByName("explorer"))
            {
                try
                {
                    using (proc)
                    {
                        proc.Kill();
                        proc.WaitForExit();
                    }
                }
                catch
                {
                }
            }
        }

        ~ExplorerRestarter()
        {
            this.Dispose(false);
        }
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
        private void Dispose(bool disposing)
        {
            if (this.m_disposed)
                return;
            this.m_disposed = true;
            
            Process.Start(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe"));
            
            var lst = new List<IntPtr>();

            int r;

            foreach (var path in this.m_opened)
            {
                if (path.Count == 1)
                {
                    Process.Start(new ProcessStartInfo { FileName = path[0], UseShellExecute = true });
                }
                else if (path.Count > 1)
                {
                    lst.Clear();

                    r = NativeMethods.SHParseDisplayName(path[0], IntPtr.Zero, out IntPtr pidlFolder, 0, out uint psfgaoOut);
                    if (r == NativeMethods.S_OK && pidlFolder != IntPtr.Zero)
                    {
                        try
                        {
                            for (int i = 1; i < path.Count; ++i)
                            {
                                r = NativeMethods.SHParseDisplayName(path[i], IntPtr.Zero, out IntPtr pidlFile, 0, out psfgaoOut);

                                if (r == NativeMethods.S_OK && pidlFile != IntPtr.Zero)
                                    lst.Add(pidlFile);
                            }

                            var arr = lst.ToArray();
                            NativeMethods.SHOpenFolderAndSelectItems(pidlFolder, (uint)arr.Length, arr, 0);
                        }
                        finally
                        {
                            Marshal.FreeCoTaskMem(pidlFolder);
                            lst.ForEach(e => Marshal.FreeCoTaskMem(e));
                        }
                    }
                }
            }
            
            try
            {
                using (var reg = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon", true))
                {
                    if (this.m_orignalValue == null)
                        reg.DeleteValue("AutoRestartShell");
                    else
                        reg.SetValue("AutoRestartShell", this.m_orignalValue, this.m_orignalKind);
                }
            }
            catch
            {
            }

            this.m_opened.Clear();
        }

        private static class NativeMethods
        {
            [DllImport("shell32.dll", SetLastError = true)]
            public static extern int SHParseDisplayName(
                [MarshalAs(UnmanagedType.LPWStr)] string name,
                IntPtr bindingContext,
                [Out] out IntPtr pidl,
                uint sfgaoIn,
                [Out] out uint psfgaoOut);

            [DllImport("shell32.dll", SetLastError = true)]
            public static extern int SHOpenFolderAndSelectItems(
                IntPtr pidlFolder,
                uint cidl,
                [In, MarshalAs(UnmanagedType.LPArray)] IntPtr[] apidl,
                uint dwFlags);

            public const int S_OK = 0;
        }
    }
}
