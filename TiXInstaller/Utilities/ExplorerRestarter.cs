using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using SHDocVw;
using Shell32;

namespace TiX.Utilities
{
    internal class ExplorerRestarter : IDisposable
    {
        private struct ExplorerStatus
        {
            public string       dirPath;
            public int          ShowCmd;
            public List<string> SelectedFiles;
        }
        
        private readonly List<ExplorerStatus> m_opened = new List<ExplorerStatus>();
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

            var wndpl = NativeMethods.WINDOWPLACEMENT.Default;

            ShellWindows shellWindows = new ShellWindows();
            foreach (InternetExplorer window in shellWindows)
            {
                if (window.FullName == exp)
                {
                    var st = new ExplorerStatus();
                    st.dirPath = window.LocationURL;

                    var lst = new List<string>();
                    FolderItems items = ((IShellFolderViewDual2)window.Document).SelectedItems();
                    foreach (FolderItem item in items)
                        lst.Add(item.Path);

                    if (lst.Count > 0)
                        st.SelectedFiles = lst;

                    st.ShowCmd = NativeMethods.GetWindowPlacement(new IntPtr(window.HWND), ref wndpl) ? wndpl.ShowCmd : -1;
                    
                    this.m_opened.Add(st);
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
            
            int result;

            foreach (var path in this.m_opened)
            {
                lst.Clear();

                result = NativeMethods.SHParseDisplayName(path.dirPath, IntPtr.Zero, out IntPtr pidlFolder, 0, out uint psfgaoOut);
                if (result == NativeMethods.S_OK && pidlFolder != IntPtr.Zero)
                {
                    try
                    {
                        if (path.SelectedFiles == null)
                            NativeMethods.SHOpenFolderAndSelectItems(pidlFolder, 0U, null, 0);
                        else
                        {
                            foreach (var filePath in path.SelectedFiles)
                            {
                                result = NativeMethods.SHParseDisplayName(filePath, IntPtr.Zero, out IntPtr pidlFile, 0, out psfgaoOut);

                                if (result == NativeMethods.S_OK && pidlFile != IntPtr.Zero)
                                    lst.Add(pidlFile);
                            }

                            var apidl = lst.ToArray();
                            NativeMethods.SHOpenFolderAndSelectItems(pidlFolder, (uint)apidl.Length, apidl, 0);
                        }
                    }
                    finally
                    {
                        NativeMethods.ILFree(pidlFolder);
                        lst.ForEach(e => NativeMethods.ILFree(e));
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

            [DllImport("shell32.dll")]
            public static extern void ILFree(
                IntPtr pidl);

            [DllImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool GetWindowPlacement(
                IntPtr hWnd,
                ref WINDOWPLACEMENT lpwndpl);

            [Serializable]
            [StructLayout(LayoutKind.Sequential)]
            public struct WINDOWPLACEMENT
            {
                public int       Length;
                public int       Flags;                
                public int       ShowCmd;
                public Point     MinPosition;
                public Point     MaxPosition;
                public Rectangle NormalPosition;

                public static WINDOWPLACEMENT Default
                {
                    get
                    {
                        WINDOWPLACEMENT result = new WINDOWPLACEMENT();
                        result.Length = Marshal.SizeOf(result);
                        return result;
                    }
                }
            }

            public const int S_OK = 0;

        }
    }
}
