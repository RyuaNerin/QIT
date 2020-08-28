using System.Globalization;
using System.IO;

namespace TiX.Utilities
{
    internal static class Signatures
    {
        public static readonly short[] WebP = ParseSignature("52494646????????57454250");
        public static readonly short[] Psd  = ParseSignature("38425053");

        public static bool CheckSignature(Stream stream, short[] signature)
        {
            var buff = new byte[signature.Length];
            var read = stream.Read(buff, 0, buff.Length);
            if (signature.Length != read)
                return false;

            var res = CheckSignature(buff, signature);

            return res;
        }
        public static bool CheckSignature(byte[] buff, short[] signature)
        {
            for (int i = 0; i < buff.Length; ++i)
                if (signature[i] != -1 && signature[i] != buff[i])
                    return false;

            return true;
        }

        private static short[] ParseSignature(string signature)
        {
            var buff = new short[signature.Length / 2];
            for (int i = 0; i < buff.Length; ++i)
            {
                var part = signature.Substring(i * 2, 2);
                if (part == "??")
                    buff[i] = -1;
                else
                    buff[i] = byte.Parse(part, NumberStyles.HexNumber);
            }

            return buff;
        }
    }
}
