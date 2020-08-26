using System;
using System.Threading.Tasks;
using System.Windows;
using Sentry;

namespace TiX
{
    internal static class CrashReport
    {
        public static void Init()
        {
            SentrySdk.Init(opt =>
            {
                opt.Dsn = new Dsn("https://791134202f414221a4367e80c9bfeb40@sentry.ryuar.in/20");
                opt.IsEnvironmentUser = true;

#if DEBUG
                opt.Release = "Debug";
                opt.Debug = true;
#else
                opt.Release = App.Version;
#endif
            });

            AppDomain.CurrentDomain.UnhandledException += (s, e) => SentrySdk.CaptureException(e.ExceptionObject as Exception);
            TaskScheduler.UnobservedTaskException += (s, e) => SentrySdk.CaptureException(e.Exception);
            Application.Current.DispatcherUnhandledException += (s, e) => SentrySdk.CaptureException(e.Exception);
            Application.Current.Dispatcher.UnhandledException += (s, e) => SentrySdk.CaptureException(e.Exception);
        }
    }
}
