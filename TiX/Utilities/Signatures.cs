using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TiX.Utilities
{
    internal class Signatures
    {
        private static readonly IList<Signature> SignatureList = new List<Signature>
        {
            new Signature(FileType.WebP, "52 49 46 46 ?? ?? ?? ?? 57 45 42 50"),
            new Signature(FileType.Psd,  "38 42 50 53"),
            new Signature(FileType.Gif,  "47 49 46 38 37 61"),
            new Signature(FileType.Gif,  "47 49 46 38 39 61"),
            new Signature(FileType.Png,  "89 50 4E 47 0D 0A 1A 0A"),
            new Signature(FileType.Jpeg, "FF D8 FF"),
        };

        public enum FileType
        {
            Unknown,
            WebP,
            Psd,
            Gif,
            Png,
            Jpeg,
        };

        public struct Signature
        {
            public FileType Type { get; }
            public short[] Match { get; }

            public Signature(FileType type, string str)
            {
                this.Type = type;
                this.Match = new short[str.Length / 2];

                var i = 0;
                var mi = 0;
                short v = 0;
                do
                {
                    if (str[i] == ' ' || i == str.Length)
                    {
                        this.Match[mi++] = v;
                    }
                    else if (str[i] == '?')
                    {
                        this.Match[mi++] = v;
                        i++;
                    }
                    else
                    {
                        v = (short)(v * 10 + str[i] - '0');
                    }
                }
                while (++i <= str.Length);
            }
        }

        public static int MaxSignatureLength { get; }

        static Signatures()
        {
            MaxSignatureLength = SignatureList.Max(le => le.Match.Length);
        }

        public static FileType CheckSignature(Stream stream)
        {
            var buff = new byte[MaxSignatureLength];
            return CheckSignature(buff);
        }
        public static FileType CheckSignature(byte[] buff)
        {
            foreach (var sig in SignatureList)
            {
                for (int i = 0; i < buff.Length && i < sig.Match.Length; ++i)
                    if (sig.Match[i] != -1 && sig.Match[i] != buff[i])
                        return sig.Type;
            }

            return FileType.Unknown;
        }
    }
}
