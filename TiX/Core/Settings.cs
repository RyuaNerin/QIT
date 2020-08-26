using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace TiX.Core
{
    internal class Settings : INotifyPropertyChanged
    {
        public static Settings Instance { get; } = new Settings();

        private sealed class Attr : Attribute
        {
            public Attr(string name) => this.Name = name;
            public string Name { get; }
        }

        public  const           string FileName = "TiX.ini";
        public  readonly static string DefaultPath;

        private readonly static PropertyInfo[] m_properties;

        public event PropertyChangedEventHandler PropertyChanged;
        private void InvokePropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        [Attr("UToken" )] public string UToken  { get; set; } = null;
        [Attr("USecret")] public string USecret { get; set; } = null;

        private bool m_topmost = true;
        [Attr("Topmost")]
        public bool Topmost
        {
            get => this.m_topmost;
            set
            {
                this.m_topmost = value;
                this.InvokePropertyChanged();
            }
        }

        private bool m_reversedCtrl = false;
        [Attr("ReversedCtrl")]
        public bool ReversedCtrl
        {
            get => this.m_reversedCtrl;
            set
            {
                this.m_reversedCtrl = value;
                this.InvokePropertyChanged();
            }
        }

        [Attr("StartInTray"   )] public bool StartInTray    { get; set; } = false;
        [Attr("MinizeToTray"  )] public bool MinizeToTray   { get; set; } = false;

        [Attr("UniformityText")] public bool UniformityText { get; set; } = false;
        [Attr("EnabledInReply")] public bool EnabledInReply { get; set; } = true;
        [Attr("SEWithText"    )] public bool SEWithText     { get; set; } = true;
        [Attr("SEWithoutText" )] public bool SEWithoutText  { get; set; } = true;

        private bool m_previewHighQuality = true;
        [Attr("PreviewHighQuality")]
        public bool PreviewHighQuality
        {
            get => this.m_previewHighQuality;
            set
            {
                this.m_previewHighQuality = value;
                this.InvokePropertyChanged();
            }
        }

        static Settings()
        {
            Settings.DefaultPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), FileName);
            Settings.m_properties = typeof(Settings).GetProperties().Where(e => e.GetCustomAttributes<Attr>() != null).ToArray();
        }
        private Settings()
        {
        }

        public void Load()
        {
            if (!File.Exists(Settings.DefaultPath))
            {
                return;
            }

            string str;
            foreach (var prop in m_properties)
            {
                str = NativeMethods.Get(Settings.DefaultPath, "TiX", prop.Name);
                if (!string.IsNullOrEmpty(str))
                    prop.SetValue(this, Str2Obj(str, prop.PropertyType), null);
            }
        }

        public void Save()
        {
            string val;
            foreach (var prop in m_properties)
            {
                val = Obj2Str(prop.GetValue(this, null));

                if (val != null)
                    NativeMethods.Set(Settings.DefaultPath, "TiX", prop.GetCustomAttribute<Attr>().Name, val);
            }
        }

        public void ApplyToRegistry()
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(@"Software\RyuaNerin"))
                {
                    key.SetValue("TiX-wt",  this.SEWithText    ? 1 : 0, RegistryValueKind.DWord);
                    key.SetValue("TiX-wot", this.SEWithoutText ? 1 : 0, RegistryValueKind.DWord);
                }
            }
            catch
            {
            }
        }

        private static object Str2Obj(string val, Type toType)
        {
            if (toType == typeof(bool))   return val == "1";
            if (toType == typeof(string))
            {
                var str = val as string;
                return !string.IsNullOrEmpty(str) ? str : null;
            }
            return null;
        }
        private static string Obj2Str(object val)
        {
            if (val is string) return val as string;
            if (val is bool)   return (bool)val ? "1" : "0";
            return "";
        }

        private static class NativeMethods
        {
            [DllImport("kernel32.dll", CharSet=CharSet.Unicode)]
            private static extern uint GetPrivateProfileString(string lpAppName, string lpKeyName, string lpDefault, StringBuilder lpReturnedString, uint nSize, string lpFileName);
            public static string Get(string path, string section, string key)
            {
                var sb = new StringBuilder(64);
                GetPrivateProfileString(section, key, null, sb, (uint)sb.Capacity, path);

                return sb.ToString();
            }

            [DllImport("kernel32.dll", CharSet=CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool WritePrivateProfileString(string lpAppName, string lpKeyName, string lpString, string lpFileName);
            public static void Set(string path, string section, string key, string value)
            {
                WritePrivateProfileString(section, key, value, path);
            }
        }
    }
}
