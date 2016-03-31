using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;

namespace TiX.ExternalLibrary
{
    internal static class Resolver
    {
        public static void Init(Type resourceType)
        {
            m_rootNamespace = resourceType.Namespace;
            if (m_rootNamespace.Contains("."))
                m_rootNamespace = m_rootNamespace.Substring(0, m_rootNamespace.IndexOf('.'));

            var bytesType = typeof(byte[]);
            m_resourceMethod = resourceType
                               .GetProperties(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.GetProperty)
                               .Where(e => e.PropertyType == bytesType)
                               .ToArray();
            m_resourceName = m_resourceMethod.Select(e => e.Name.Replace('_', '.')).ToArray();

            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        private static string           m_rootNamespace;
        private static string[]         m_resourceName;
        private static PropertyInfo[]   m_resourceMethod;
        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var name = new AssemblyName(args.Name).Name.Replace('-', '.');
            if (name.StartsWith(m_rootNamespace)) return null;
            
            for (int i = 0; i < m_resourceName.Length; ++i)
            {
                if (m_resourceName[i].Contains(name))
                {
                    byte[] buff;

                    using (var comp     = new MemoryStream((byte[])m_resourceMethod[i].GetValue(null, null)))
                    using (var gzip     = new GZipStream(comp, CompressionMode.Decompress))
                    using (var uncomp   = new MemoryStream(4096))
                    {
                        gzip.CopyTo(uncomp);
                        buff = uncomp.ToArray();
                    }

                    return Assembly.Load(buff);
                }
            }

            return null;
        }
    }
}
