using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
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
            ravenClient = new RavenClient("https://b2d115a75b1f485a8a0b49cd51aabfc6:b55540eaf7274e549c3f1864694a171b@sentry.io/133868")
            {
                Environment = Application.ProductName,
                Logger      = Application.ProductName,
                Release     = Application.ProductVersion
            };
        }

        public static void Init()
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            AppDomain.CurrentDomain.UnhandledException += (s, e) => Error(e.ExceptionObject as Exception, e);
            TaskScheduler.UnobservedTaskException      += (s, e) => Error(e.Exception, null);
            Application.ThreadException                += (s, e) => Error(e.Exception, null);
        }

        public static void Error(Exception ex, object data = null)
        {
            if (ex == null
#if !TiXInstaller
                || Settings.Instance.EnabledErrorReport
#endif
                )
                return;

            var ev = new SentryEvent(ex)
            {
                Level = ErrorLevel.Error
            };

            if (data != null)
            {
                var dic = new Dictionary<string, string>();
                foreach (var prop in data.GetType().GetProperties())
                {
                    var value = prop.GetValue(data);
                    string svalue;
                    if (value.GetType().IsGenericType)
                    {
                        var sb = new StringBuilder(1024);
                        using (var writer = new StringWriter(sb))
                        {
                            var serializer = new JsonSerializer();
                            serializer.Serialize(writer, value);
                        }

                        svalue = sb.ToString();
                    }
                    else
                    {
                        svalue = Convert.ToString(value);
                    }

                    dic.Add($"{GetFriendlyName(prop.PropertyType)} {prop.Name}", svalue);
                }

                ev.Extra = dic;
            }

            ev.Tags.Add("ARCH", Environment.Is64BitOperatingSystem ? "x64" : "x86");
            ev.Tags.Add("OS",   Environment.OSVersion.VersionString);
            ev.Tags.Add("NET",  Environment.Version.ToString());

            ravenClient.CaptureAsync(ev);
        }

        private static string GetFriendlyName(Type type)
        {
            var name = type.FullName;
            if (!type.IsGenericType)
                return name;

            int i;

            var sb = new StringBuilder();
            i = name.IndexOf('`');
            if (i != -1)
                sb.Append(name, 0, i);
            else
                sb.Append(name);

            sb.Append('<');

            var args = type.GetGenericArguments();
            for (i = 0; i < args.Length; ++i)
            {
                if (i > 0)
                    sb.Append(", ");

                sb.Append(GetFriendlyName(args[i]));
            }

            sb.Append('>');

            return sb.ToString();
        }
    }
}
