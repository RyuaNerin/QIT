using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;
using TiX.Utilities;

namespace TiX
{
    internal enum InstallerResult : int
    {
        UNKNOWN            = -1,
        SUCCESS            = 0,
        NOT_AUTHORIZED     = 1,
        DLL_CREATAION_FAIL = 3,
        FAIL_DLL_REGIST    = 4,
        FILE_USED          = 5,
    }

    [Flags]
    internal enum OptionInstallation : long
    {
        Install             = 0x01,
        InstallRunas        = 0x02,
        Uninstall           = 0x04,
        UninstallRunas      = 0x08,
        ShoftcutInDesktop   = 0x10,
        ShoftcutInStartMenu = 0x20,
    }

    [Flags]
    internal enum OptionTixSettings : int
    {
        None                       = 0x00,
        ShellExtension_WithText    = 0x01,
        ShellExtension_WithoutText = 0x02,
        StartWithWindows           = 0x04,
    }

    internal static class Installer
    {
        private static InstallerResult Runas(bool isRunas, TiXOption option, Func<InstallerResult> action)
        {
            if (!isRunas)
            {
                var startup = new ProcessStartInfo()
                {
                    WindowStyle      = ProcessWindowStyle.Hidden,
                    UseShellExecute  = true,
                    FileName         = Application.ExecutablePath,
                    Verb             = "runas",
                    Arguments        = option.ToString()
                };

                try
                {
                    using (var proc = Process.Start(startup))
                    {
                        proc.WaitForExit();
                        return (InstallerResult)proc.ExitCode;
                    }
                }
                catch
                {
                    return InstallerResult.NOT_AUTHORIZED;
                }
            }
            else if (!TiXMain.IsAdministratorMode)
            {
                return InstallerResult.NOT_AUTHORIZED;
            }
            else
            {
                return action();
            }
        }

        public static InstallerResult TiXSetting(bool isRunas, OptionTixSettings option)
        {
            var wt  = option.HasFlag(OptionTixSettings.ShellExtension_WithText);
            var wot = option.HasFlag(OptionTixSettings.ShellExtension_WithoutText);

            var exe = Application.ExecutablePath;

            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(@"Software\RyuaNerin"))
                {
                    key.SetValue("TiX",     exe,         RegistryValueKind.ExpandString);
                    key.SetValue("TiX-wt",  wt  ? 1 : 0, RegistryValueKind.DWord);
                    key.SetValue("TiX-wot", wot ? 1 : 0, RegistryValueKind.DWord);
                }
            }
            catch
            {
                return InstallerResult.UNKNOWN;
            }

            try
            {
                using (var run = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\\Run", true))
                {
                    if (option.HasFlag(OptionTixSettings.StartWithWindows))
                        run.SetValue("TiX", exe, RegistryValueKind.String);
                    else
                        run.DeleteValue("TiX", false);
                }
                
            }
            catch
            {
                return InstallerResult.UNKNOWN;
            }

            return InstallerResult.SUCCESS;
        }

        public static InstallerResult Ap_Install(bool isRunas, OptionInstallation option)
        {
            try
            {
                var dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    dir = Path.Combine(dir, "TiX");
                var exe = Path.Combine(dir, "TiX.exe");

                var sid = new NTAccount(WindowsIdentity.GetCurrent().Name).Translate(typeof(SecurityIdentifier)).ToString();
                var runas = Installer.Ap_Install_Runas(isRunas, dir, sid);
                if (runas != InstallerResult.SUCCESS)
                    return runas;

                var asm = Assembly.GetExecutingAssembly();
                var desc = ((AssemblyDescriptionAttribute)asm.GetCustomAttributes(typeof(AssemblyDescriptionAttribute), true)[0]).Description;

                if (option.HasFlag(OptionInstallation.ShoftcutInDesktop))
                    CreateShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "TiX.lnk"), exe, desc);

                if (option.HasFlag(OptionInstallation.ShoftcutInStartMenu))
                    CreateShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "TiX.lnk"), exe, desc);
            }
            catch (Exception ex) when (ex is SecurityException || ex is UnauthorizedAccessException)
            {
                return InstallerResult.NOT_AUTHORIZED;
            }
            catch
            {
                return InstallerResult.UNKNOWN;
            }

            return InstallerResult.SUCCESS;
        }
        public static InstallerResult Ap_Install_Runas(bool isRunas, string dir, string sid)
        {
            return Runas(
                isRunas,
                new TiXOption
                {
                    OptionInstallation = OptionInstallation.InstallRunas,
                    Files = new List<string>
                    {
                        dir,
                        sid
                    }
                },
                () =>
                {
                    try
                    {
                        var exe = Path.Combine(dir, "TiX.exe");

                        if (!Directory.CreateDirectory(dir).Exists)
                            return InstallerResult.NOT_AUTHORIZED;

                        // 파일이 있으면 삭제 시도 후 강제 종료
                        if (!ShutdownTiX(exe))
                            return InstallerResult.FILE_USED;

                        // 복사
                        File.Copy(Application.ExecutablePath, exe);

                        if (!File.Exists(exe))
                            return InstallerResult.UNKNOWN;

                        var asm = Assembly.GetExecutingAssembly();
                        var copyright = ((AssemblyCopyrightAttribute)asm.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), true)[0]).Copyright;
                        
                        using (var currentUser = Registry.Users.CreateSubKey(sid))
                        {
                            // 스키마 등록
                            using (var stix = currentUser.CreateSubKey(@"Software\Classes\tix"))
                            {
                                stix.SetValue(null, "URL:TiX");
                                stix.SetValue("URL Protocol", "", RegistryValueKind.String);
                                using (var cmm = stix.CreateSubKey(@"shell\open\command"))
                                    cmm.SetValue(null, $"\"{exe}\" {new TiXOption { SchemeData = "\"%1\"" }}");
                            }

                            using (var key = currentUser.CreateSubKey(@"Software\RyuaNerin"))
                            {
                                key.SetValue("TiX",     exe, RegistryValueKind.ExpandString);
                                key.SetValue("TiX-wt",  1,   RegistryValueKind.DWord);
                                key.SetValue("TiX-wot", 1,   RegistryValueKind.DWord);
                            }

                            // Shell Extension 등록
                            var dllPath = Path.Combine(dir, string.Format("TiXExt{0}.dll", IntPtr.Size * 8));
                            using (var exp = new ExplorerRestarter())
                            {
                                try
                                {
                                    File.WriteAllBytes(
                                        dllPath,
                                        IntPtr.Size == 8 ?
                                            Properties.Resources.TiXExt64 :
                                            Properties.Resources.TiXExt32
                                        );
                                }
                                catch
                                {
                                    return InstallerResult.DLL_CREATAION_FAIL;
                                }

                                SetShellExtensionApprove(true, sid);
                                DllRegisterServer(currentUser, dllPath);
                            }

                            File.WriteAllText(Path.Combine(dir, "TiX.dat"), sid, Encoding.UTF8);

                            var filesize = new FileInfo(exe).Length + new FileInfo(dllPath).Length;
                            filesize /= 1024;

                            using (var uninstall = currentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"))
                            {
                                using (var tix = uninstall.CreateSubKey($"{{{TiXMain.GUIDApplication}}}"))
                                {
                                    tix.SetValue("DisplayName",         TiXMain.ProductName);
                                    tix.SetValue("DisplayIcon",         exe);
                                    tix.SetValue("DisplayVersion",      Application.ProductVersion);

                                    tix.SetValue("ApplicationVersion",  Application.ProductVersion);

                                    tix.SetValue("Publisher",           copyright);
                                    tix.SetValue("URLInfoAbout",        "https://github.com/RyuaNerin/QIT");
                                    tix.SetValue("Contact",             "https://github.com/RyuaNerin/QIT");

                                    tix.SetValue("InstallDate",         DateTime.Now.ToString("yyyyMMdd"));
                                    tix.SetValue("InstallLocation",     dir);
                                    tix.SetValue("UninstallString",     $"\"{exe}\" {new TiXOption { OptionInstallation = OptionInstallation.Uninstall }}");

                                    tix.SetValue("EstimatedSize", filesize, RegistryValueKind.DWord);
                                    tix.SetValue("NoModify",      1,        RegistryValueKind.DWord);
                                    tix.SetValue("NoRepair",      1,        RegistryValueKind.DWord);
                                }
                            }
                        }
                    }
                    catch (Exception ex) when (ex is SecurityException || ex is UnauthorizedAccessException)
                    {
                        return InstallerResult.NOT_AUTHORIZED;
                    }
                    catch
                    {
                        return InstallerResult.UNKNOWN;
                    }

                    return InstallerResult.SUCCESS;
                });
        }

        public static InstallerResult Ap_Uninstall()
        {
            try
            {
                var tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".exe");

                File.Copy(Application.ExecutablePath, tempPath);

                Process.Start(new ProcessStartInfo
                {
                    FileName    = tempPath,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    Verb        = "runas",
                    Arguments   = new TiXOption
                    {
                        OptionInstallation = OptionInstallation.UninstallRunas,
                        Files = new List<string> { Path.GetDirectoryName(Application.ExecutablePath) },
                    }.ToString(),
                });
                
                return InstallerResult.SUCCESS;
            }
            catch
            {
                return InstallerResult.UNKNOWN;
            }
        }
        public static int Ap_Uninstall_Runas(string dir)
        {
            var exe = Path.Combine(dir, "TiX.exe");

            // sid
            var sid = File.ReadAllText(Path.Combine(dir, "TiX.dat"));
            
            using (var currentUser = Registry.Users.CreateSubKey(sid))
            {
                ShutdownTiX(exe);

                // 개인 설정 제거
                try
                {
                    using (var key = currentUser.CreateSubKey(@"Software\RyuaNerin", RegistryKeyPermissionCheck.ReadWriteSubTree))
                    {
                        key.DeleteValue("TiX",     false);
                        key.DeleteValue("TiX-wt",  false);
                        key.DeleteValue("TiX-wot", false);
                    }
                }
                catch
                {
                }

                // 스키마 삭제
                try
                {
                    currentUser.DeleteSubKeyTree(@"Software\Classes\tix", false);
                }
                catch
                {
                }

                // Shell Extension 삭제
                try
                {
                    using (var exp = new ExplorerRestarter())
                    {
                        DllUnregisterServer(currentUser);
                        
                        SetShellExtensionApprove(false, sid);
                    }
                }
                catch
                {
                }

                // 바로가기 삭제
                try
                {
                    foreach (var path in Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "*.lnk"))
                        RemoveShortcut(path, exe);
                }
                catch
                {
                }
                
                try
                {
                    foreach (var path in Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "*.lnk"))
                    RemoveShortcut(path, exe);
                }
                catch
                {
                }

                // 파일 삭제
                DeleteFiles(dir);

                // 임시 파일 삭제
                NativeMethods.MoveFileEx(Application.ExecutablePath, null, NativeMethods.MoveFileFlags.MOVEFILE_DELAY_UNTIL_REBOOT);

                try
                {
                    currentUser.DeleteSubKeyTree($"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{{{TiXMain.GUIDApplication}}}", false);
                }
                catch
                {
                }
            }

            return 0;
        }

        private static bool ShutdownTiX(string exe)
        {
            if (File.Exists(exe))
            {
                try
                {
                    File.Delete(exe);
                }
                catch
                {
                    try
                    {
                        foreach (var proc in Process.GetProcessesByName(Path.GetFileName(exe)))
                        {
                            using (proc)
                            {
                                proc.Kill();
                                proc.WaitForExit();
                            }
                        }

                        File.Delete(exe);
                    }
                    catch
                    {
                        return false;
                    }
                }

                if (File.Exists(exe))
                    return false;
            }

            return true;
        }

        private static void DeleteFiles(string dir)
        {
            var lst = new List<string>();
            lst.AddRange(Directory.GetFiles(dir));

            int i = 0;

            while (i < lst.Count)
            {
                try
                {
                    File.Delete(lst[i]);
                    lst.RemoveAt(i);
                }
                catch
                {
                    ++i;
                }
            }

            for (i = 0; i < lst.Count; ++i)
                NativeMethods.MoveFileEx(lst[i], null, NativeMethods.MoveFileFlags.MOVEFILE_DELAY_UNTIL_REBOOT);

            try
            {
                Directory.Delete(dir, true);
            }
            catch
            {
                NativeMethods.MoveFileEx(dir, null, NativeMethods.MoveFileFlags.MOVEFILE_DELAY_UNTIL_REBOOT);
            }
        }

        private static void CreateShortcut(string shortcutPath, string targetPath, string description)
        {
            try
            {
                var wsh = new IWshRuntimeLibrary.WshShell();
                var sc = (IWshRuntimeLibrary.IWshShortcut) wsh.CreateShortcut(shortcutPath);
                sc.TargetPath       = targetPath;
                sc.Description      = description;
                sc.WorkingDirectory = Path.GetDirectoryName(targetPath);
                sc.Save();
            }
            catch
            {
            }
        }

        private static void RemoveShortcut(string shortcutPath, string targetPath)
        {
            shortcutPath = Path.GetFullPath(shortcutPath);
            targetPath   = Path.GetFullPath(targetPath);

            try
            {
                var wsh = new IWshRuntimeLibrary.WshShell();
                var sc = (IWshRuntimeLibrary.IWshShortcut)wsh.CreateShortcut(shortcutPath);

                if (Path.GetFullPath(sc.TargetPath) == targetPath)
                        File.Delete(shortcutPath);
            }
            catch
            {
            }
        }

        private static void SetShellExtensionApprove(bool set, string sid)
        {
            var delApprove = false;
            var delSubKey = false;

            using (var reg = Registry.LocalMachine.CreateSubKey("SoftWare\\RyuaNerin"))
            {
                if (set)
                    reg.SetValue($"TiXExt-{sid}", string.Empty, RegistryValueKind.String);
                else
                    reg.DeleteValue($"TiXExt-{sid}", false);

                var subValues = reg.GetValueNames();
                delApprove = !set && subValues.Any(e => e.StartsWith("TiXExt-"));
                delSubKey = subValues.Length == 0;
            }

            if (delSubKey)
                Registry.LocalMachine.DeleteSubKeyTree("Software\\RyuaNerin");

            using (var reg = Registry.LocalMachine.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Approved"))
            {
                if (set)
                    reg.SetValue(TiXMain.GUIDShellExtension, "TiXExt Class", RegistryValueKind.String);
                else if (delApprove)
                    reg.DeleteValue(TiXMain.GUIDShellExtension, false);
            }
        }

        private static void DllRegisterServer(RegistryKey cu, string dllPath)
        {
            RegistryKey reg;

            using (reg = cu.CreateSubKey($"Software\\Classes\\CLSID\\{TiXMain.GUIDShellExtension}"))
            {
                reg.SetValue(null, "TiXExt Class", RegistryValueKind.String);

                using (var inproc = reg.CreateSubKey("InprocServer32"))
                {
                    inproc.SetValue(null, dllPath, RegistryValueKind.String);
                    inproc.SetValue("ThreadingModel", "Apartment", RegistryValueKind.String);
                }
            }

            using (reg = cu.CreateSubKey("Software\\Classes\\*\\ShellEx\\ContextMenuHandlers\\TiXExt"))
                reg.SetValue(null, TiXMain.GUIDShellExtension);

            using (reg = cu.CreateSubKey("Software\\Classes\\Directory\\ShellEx\\ContextMenuHandlers\\TiXExt"))
                reg.SetValue(null, TiXMain.GUIDShellExtension);
        }

        private static void DllUnregisterServer(RegistryKey cu)
        {
            cu.DeleteSubKeyTree($"Software\\Classes\\CLSID\\{TiXMain.GUIDShellExtension}", false);
            cu.DeleteSubKeyTree( "Software\\Classes\\*\\ShellEx\\ContextMenuHandlers\\TiXExt", false);
            cu.DeleteSubKeyTree( "Software\\Classes\\Directory\\ShellEx\\ContextMenuHandlers\\TiXExt", false);
        }

        private static class NativeMethods
        {
            [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool MoveFileEx(
                [MarshalAs(UnmanagedType.LPWStr)] string lpExistingFileName,
                [MarshalAs(UnmanagedType.LPWStr)] string lpNewFileName,
                MoveFileFlags dwFlags);

            [Flags]
            public enum MoveFileFlags
            {
                MOVEFILE_REPLACE_EXISTING = 0x00000001,
                MOVEFILE_COPY_ALLOWED = 0x00000002,
                MOVEFILE_DELAY_UNTIL_REBOOT = 0x00000004,
                MOVEFILE_WRITE_THROUGH = 0x00000008,
                MOVEFILE_CREATE_HARDLINK = 0x00000010,
                MOVEFILE_FAIL_IF_NOT_TRACKABLE = 0x00000020
            }
        }
    }
}
