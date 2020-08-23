using System;
using System.Security.Principal;
using System.Windows.Forms;
using Limitation;
using TiX.Utilities;
using TiX.Windows;

namespace TiX
{
    static class TiXMain
    {
        public static readonly string ProductName        = $"TiX rev.{new Version(Application.ProductVersion).Revision}";
        public const           string GUIDInstaller      = "{9CE5906A-DFBB-4A5A-9EBF-9D262E5D29B8}";
        public const           string GUIDShellExtension = "{9CE5906A-DFBB-4A5A-9EBF-9D262E5D29B9}";

        public static readonly bool IsAdministratorMode;

        public static readonly OAuth Twitter = new OAuth("lQJwJWJoFlbvr2UQnDbg", "DsuIRA1Ak9mmSCGl9wnNvjhmWJTmb9vZlRdQ7sMqXww");

        static TiXMain()
        {
            IsAdministratorMode = IsRunningAsAdministrator();
        }

        public static bool IsRunningAsAdministrator()
        {
            using (var cur = WindowsIdentity.GetCurrent())
            {
                foreach (var role in cur.Groups)
                {
                    if (role.IsValidTargetType(typeof(SecurityIdentifier)))
                    {
                        var sid = (SecurityIdentifier)role.Translate(typeof(SecurityIdentifier));

                        if (sid.IsWellKnown(WellKnownSidType.AccountAdministratorSid) ||
                            sid.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid))
                            return true;
                    }
                }
            }

            return false;
        }

        [STAThread]
        static int Main(string[] argsArray)
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            var args   = Args.Parse(argsArray);
            var option = (OptionInstallation)args.OptionInstallation;
            var cmd    = option & OptionInstallation.Cmd;

            switch (cmd)
            {
                case OptionInstallation.InstallRunas:
                    return (int)Installer.Ap_Install_Runas(true, option, args.Files[0], args.Files[1]);

                case OptionInstallation.UninstallRunas:
                    return (int)Installer.Ap_Uninstall_Runas(args.Files[0]);

                case OptionInstallation.InstallOrNone:
                case OptionInstallation.Uninstall:
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);

                    using (var instance = new InstanceHelper(TiXMain.GUIDInstaller))
                    {
                        if (!instance.LockOrActivate())
                            return 0;

                        if (option == OptionInstallation.InstallOrNone)
                            Application.Run(new frmInstall(instance.WMMessage));
                        else
                            Application.Run(new frmUninstall(instance.WMMessage));
                    }

                    break;
            }


            return 0;
        }
    }
}
