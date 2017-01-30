﻿using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

namespace TiX
{
    internal static class ShellExtension
    {
        public enum Result : int
        {
            UNKNOWN = -1,
            NO_ERROR = 0,
            NOT_AUTHORIZED = 1,
            DLL_NOT_EXITED = 2,
            DLL_CREATAION_FAIL = 3,
            FAIL_REG = 4,
            FILE_USED = 5
        }
        public static Result Install(bool withText, bool withoutText, string oldShells, bool runas = false)
        {
            var startup = new ProcessStartInfo();
            startup.WindowStyle = ProcessWindowStyle.Hidden;
            startup.UseShellExecute = true;
            startup.WorkingDirectory = Environment.CurrentDirectory;

            if (TiXMain.IsAdministratorMode)
            {
                try
                {
                    using (var key = Registry.LocalMachine.CreateSubKey(@"Software\RyuaNerin"))
                    {
                        key.SetValue("TiX", Application.ExecutablePath, RegistryValueKind.ExpandString);
                        key.SetValue("TiX-Option", (withText ? 1 : 0) | (withoutText ? 2 : 0), RegistryValueKind.DWord);
                    }
                }
                catch
                {
                    return Result.NOT_AUTHORIZED;
                }

                if (oldShells != null)
                    UninstallOldShells(oldShells);

                bool installedDll = false;
                try
                {
                	using (var key = Registry.ClassesRoot.OpenSubKey("*\\ShellEx\\ContextMenuHandlers\\00TiXExt"))
                        installedDll = key.GetValue(null, null) != null;
                }
                catch
                {
                }

                if (installedDll)
                    return Result.NO_ERROR;

                var dllPath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), string.Format("TiXExt{0}.dll", IntPtr.Size == 8 ? 64 : 32));
                try
                {
                    File.WriteAllBytes(dllPath, IntPtr.Size == 8 ? Properties.Resources.TiXExt64 : Properties.Resources.TiXExt32);
                }
                catch (IOException)
                {
                    return Result.FILE_USED;
                }
                catch
                {
                    return Result.DLL_CREATAION_FAIL;
                }

                startup.FileName = "regsvr32";
                startup.Arguments = string.Format("/s \"{0}\"", dllPath);

                using (var proc = Process.Start(startup))
                {
                    proc.WaitForExit();
                    return proc.ExitCode == 0 ? Result.NO_ERROR : Result.FAIL_REG;
                }
            }
            else if (runas)
            {
                // 관리자로 켰는데 관리자가 아닌 경우
                return Result.NOT_AUTHORIZED;
            }
            else
            {
                startup.FileName = Application.ExecutablePath;
                startup.Arguments = string.Format("--install \"{0}\" \"{1}\"", withText ? 1 : 0, withoutText ? 1 : 0, oldShells);
                startup.Verb = "runas";

                using (var proc = Process.Start(startup))
                {
                    proc.WaitForExit();
                    return (Result)proc.ExitCode;
                }
            }
        }
        public static Result Uninstall(bool runas = false)
        {
            var startup = new ProcessStartInfo();
            startup.WindowStyle = ProcessWindowStyle.Hidden;
            startup.UseShellExecute = true;
            startup.WorkingDirectory = Environment.CurrentDirectory;

            if (TiXMain.IsAdministratorMode)
            {
                var dllPath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), string.Format("TiXExt{0}.dll", IntPtr.Size == 8 ? 64 : 32));
                if (!File.Exists(dllPath))
                    return Result.DLL_NOT_EXITED;

                startup.FileName = "regsvr32";
                startup.Arguments = string.Format("/s /u \"{0}\"", dllPath);

                Result r;
                using (var proc = Process.Start(startup))
                {
                    proc.WaitForExit();
                    r =  proc.ExitCode == 0 ? Result.NO_ERROR : Result.FAIL_REG;
                }

                return r;
            }
            else if (runas)
            {
                // 관리자로 켰는데 관리자가 아닌 경우
                return Result.NOT_AUTHORIZED;
            }
            else
            {
                startup.FileName = Application.ExecutablePath;
                startup.Arguments = "--unisntall";
                startup.Verb = "runas";

                using (var proc = Process.Start(startup))
                {
                    proc.WaitForExit();
                    return (Result)proc.ExitCode;
                }
            }
        }

        public static void UninstallOldShells(string shells)
        {
            string regPath;

            foreach (var shell in shells.Split(','))
            {
                regPath = string.Format(@"{0}\shell\TiXShell", shell);

                try
                {
                    Registry.ClassesRoot.DeleteSubKeyTree(regPath);
                }
                catch
                { }
            }
        }
    }
}
