using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace TiX.Core
{
	public static class Settings
	{
        private class Attr : Attribute
        { }

		public readonly static string FilePath = Path.Combine(Application.StartupPath, "TiX.ini");
        
		public const  string    CKey	= "lQJwJWJoFlbvr2UQnDbg";
		public const  string    CSecret	= "DsuIRA1Ak9mmSCGl9wnNvjhmWJTmb9vZlRdQ7sMqXww";

        [Attr] public static string UToken              { get; set; }
        [Attr] public static string	USecret             { get; set; }
        [Attr] public static bool   Topmost             { get; set; }
        [Attr] public static bool   ReversedCtrl        { get; set; }
        [Attr] public static bool   UniformityText      { get; set; }
        [Attr] public static bool   EnabledShell        { get; set; }
        [Attr] public static string Shells              { get; set; }

        public readonly static PropertyInfo[] m_properties;
        static Settings()
        {
            Twitter.TwitterAPI11.consumerToken  = Settings.CKey;
            Twitter.TwitterAPI11.consumerSecret = Settings.CSecret;

            Settings.m_properties = typeof(Settings).GetProperties().Where(e => e.GetCustomAttributes(false).Any(ee => ee is Attr)).ToArray();
        }

		public static void Load()
		{
			if (!File.Exists(Settings.FilePath))
				return;

            string str;
            foreach (var prop in m_properties)
            {
                str = NativeMethods.Get(FilePath, "TiX", prop.Name);
                if (!string.IsNullOrEmpty(str))
                    prop.SetValue(null, Str2Obj(str, prop.PropertyType), null);
            }
		}

		public static void Save()
        {
            string val;
            foreach (var prop in m_properties)
            {
                val = Obj2Str(prop.GetValue(null, null));

                if (val != null)
                    NativeMethods.Set(FilePath, "TiX", prop.Name, val);
            }
		}

        public static object Str2Obj(string val, Type toType)
        {
            if (toType == typeof(string)) return val;
            if (toType == typeof(bool))   return val == "1";
            return "";
        }
        public static string Obj2Str(object val)
        {
            if (val is string) return val as string;
            if (val is bool)   return (bool)val ? "1" : "0";
            return null;
        }

        private class NativeMethods
        {
            [DllImport("kernel32.dll", CharSet=CharSet.Unicode)]
            private static extern uint GetPrivateProfileString(string lpAppName, string lpKeyName, string lpDefault, StringBuilder lpReturnedString, uint nSize, string lpFileName);

            public static string Get(string path, string section, string key)
            {
                var sb = new StringBuilder(64);
                GetPrivateProfileString(section, key, null, sb, 64, path);

                return sb.ToString();
            }

            [DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool WritePrivateProfileString(string lpAppName, string lpKeyName, string lpString, string lpFileName);
            public static void Set(string path, string section, string key, string value)
            {
                WritePrivateProfileString(section, key, value, path);
            }
        }
	}
}
