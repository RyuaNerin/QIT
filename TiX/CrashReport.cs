using System;
using System.Windows.Forms;
using SharpRaven;
using SharpRaven.Data;
using TiX.Core;

namespace TiX
{
    internal static class CrashReport
    {
        private static readonly RavenClient ravenClient;

        static CrashReport()
        {
            ravenClient = new RavenClient("https://b2d115a75b1f485a8a0b49cd51aabfc6:b55540eaf7274e549c3f1864694a171b@sentry.io/133868");
            ravenClient.Environment = Application.ProductName;
            ravenClient.Logger = Application.ProductName;
            ravenClient.Release = Application.ProductVersion;
        }

        public static void Init()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) => ShowCrashReport(e.ExceptionObject as Exception);
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (s, e) => ShowCrashReport(e.Exception);
            Application.ThreadException += (s, e) => ShowCrashReport(e.Exception);
        }

        public static void ShowCrashReport(Exception ex)
        {
            if (ex == null)
                return;

            if (Settings.EnabledErrorReport)
                return;

            Error(ex, null);
        }

        public static void Info(object data, string format, params object[] args)
        {
            if (Settings.EnabledErrorReport)
                return;

            SentryEvent ev = new SentryEvent(new SentryMessage(format, args));
            ev.Level = ErrorLevel.Info;
            ev.Extra = data;

            Report(ev);
        }

        public static void Error(Exception ex, object data)
        {
            if (Settings.EnabledErrorReport)
                return;

            var ev = new SentryEvent(ex);
            ev.Level = ErrorLevel.Error;
            ev.Extra = data;

            Report(ev);
        }

        private static void Report(SentryEvent @event)
        {
            @event.Tags.Add("ARCH", Environment.Is64BitOperatingSystem ? "x64" : "x86");
            @event.Tags.Add("OS", Environment.OSVersion.VersionString);
            @event.Tags.Add("NET", Environment.Version.ToString());

            ravenClient.CaptureAsync(@event);
        }
    }
}
