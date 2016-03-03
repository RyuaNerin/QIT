using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.Win32;

namespace TiX
{
    public static class ShellExtension
    {
        public static void Install(bool isAdmin)
        {
            string shells = null;
            if (isAdmin)
            {
                shells = Install();
            }
            else
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.UseShellExecute = true;
                startInfo.WorkingDirectory = Environment.CurrentDirectory;
                startInfo.FileName = Application.ExecutablePath;
                startInfo.Arguments = "\"install\"";
                startInfo.Verb = "runas";

                try
                {
                    using (var proc = Process.Start(startInfo))
                    {
                        shells = proc.StandardOutput.ReadToEnd();
                        proc.WaitForExit();
                    }
                }
                catch
                { }
            }

            Settings.Shells = shells;
            Settings.Save();
        }
        public static void Uninstall(bool isAdmin)
        {
            if (isAdmin)
            {
                Uninstall(Settings.Shells);
            }
            else
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.UseShellExecute = true;
                startInfo.WorkingDirectory = Environment.CurrentDirectory;
                startInfo.FileName = Application.ExecutablePath;
                startInfo.Arguments = string.Format("\"uninstall\" \"{0}\"", Settings.Shells);
                startInfo.Verb = "runas";

                try
                {
                    using (var proc = Process.Start(startInfo))
                        proc.WaitForExit();
                }
                catch
                { }
            }

            Settings.EnabledShell = false;
            Settings.Shells = null;
            Settings.Save();
        }

        public static string Install()
        {
            var lst = new List<string>();
            string shell   = null;
            string regPath = null;

            var iconPath  = string.Format("\"{0}\"", Application.ExecutablePath);
            var shellPath = string.Format("\"{0}\" \"shell\" \"%1\"", Application.ExecutablePath);

            foreach (var type in Program.AllowExtension)
            {
                try
                {
                    using (var key = Registry.ClassesRoot.CreateSubKey(type))
                        shell = key.GetValue(null, null) as string;
                }
                catch
                { }

                if (string.IsNullOrEmpty(shell)) continue;

                regPath = string.Format(@"{0}\shell\TiXShell", shell);

                try
                {
                    using (var key = Registry.ClassesRoot.CreateSubKey(regPath))
                    {
                        key.SetValue(null, "TiX 로 트윗하기", RegistryValueKind.ExpandString);
                        key.SetValue("Icon", iconPath, RegistryValueKind.ExpandString);

                        using (var command = key.CreateSubKey("command"))
                            command.SetValue(null, shellPath, RegistryValueKind.ExpandString);
                    }
                }
                catch
                { }

                lst.Add(shell);
            }

            return string.Join(",", lst.ToArray());
        }
        public static void Uninstall(string shells)
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
