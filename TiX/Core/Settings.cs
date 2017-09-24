using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace TiX.Core
{
    internal class Settings
    {
        private static readonly Settings m_settings = new Settings();
        public static Settings Instance => m_settings;

        private sealed class Attr : Attribute
        {
        }

        public  const           string FileName = "TiX.ini";
        public  readonly static string DefaultPath;

        private readonly static PropertyInfo[] m_properties;
        
        [Attr] public string UToken             { get; set; } = null;
        [Attr] public string USecret            { get; set; } = null;
        [Attr] public bool   Topmost            { get; set; } = true;
        [Attr] public bool   ReversedCtrl       { get; set; } = false;
        [Attr] public bool   UniformityText     { get; set; } = false;
        [Attr] public bool   EnabledInReply     { get; set; } = true;
        [Attr] public bool   EnabledErrorReport { get; set; } = true;
        [Attr] public bool   StartWithWindows   { get; set; } = false;
        [Attr] public bool   StartInTray        { get; set; } = false;
        [Attr] public bool   MinizeToTray       { get; set; } = false;
        [Attr] public bool   SEWithText         { get; set; } = true;
        [Attr] public bool   SEWithoutText      { get; set; } = true;
        [Attr] public bool   PreviewHighQuality { get; set; } = true;

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
            this.Load(Settings.DefaultPath);
        }
        public void LoadInDirectory(string dir)
        {
            this.Save(Path.Combine(dir, FileName));
        }
        private void Load(string path)
        {
            if (!File.Exists(path))
            {
                return;
            }

            string str;
            foreach (var prop in m_properties)
            {
                str = NativeMethods.Get(path, "TiX", prop.Name);
                if (!string.IsNullOrEmpty(str))
                    prop.SetValue(this, Str2Obj(str, prop.PropertyType), null);
            }
        }

        public void Save()
        {
            this.Save(Settings.DefaultPath);
        }
        public void SaveInDirectory(string dir)
        {
            this.Save(Path.Combine(dir, FileName));
        }
        private void Save(string path)
        {
            string val;
            foreach (var prop in m_properties)
            {
                val = Obj2Str(prop.GetValue(this, null));

                if (val != null)
                    NativeMethods.Set(path, "TiX", prop.Name, val);
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

            try
            {
                using (var run = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\7Run", true))
                {
                    if (this.StartWithWindows)
                        run.SetValue("TiX", Application.ExecutablePath, RegistryValueKind.String);
                    else
                        run.DeleteValue("TiX", false);
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
