using System;
using System.Text;

namespace TiX.Core
{
	public static class Helper
	{
		private readonly static Random rnd = new Random(DateTime.Now.Millisecond);

		private readonly static char[] chars =
		{
 			'A', 'B', 'C', 'D', 'E',
 			'F', 'G', 'H', 'I', 'J',
 			'K', 'L', 'M', 'N', 'O',
            'P', 'Q', 'R', 'S', 'T',
            'U', 'V', 'W', 'X', 'Y',
            'Z',
			'a', 'b', 'c', 'd', 'e',
			'f', 'g', 'h', 'i', 'j',
			'k', 'l', 'm', 'n', 'o',
			'p', 'q', 'r', 's', 't',
			'u', 'v', 'w', 'x', 'y',
			'z',
			'0', '1', '2', '3', '4',
			'5', '6', '7', '8', '9'
		};

		public static string CreateString(int length = 40)
		{
			StringBuilder stringBuilder = new StringBuilder(length + 1);

			for (int i = 0; i < length; i++)
				stringBuilder.Append(Helper.chars[Helper.rnd.Next(0, chars.Length)]);

			return stringBuilder.ToString();
		}

        public static string ToCapString(long size)
        {
            if (size < 1000)
                return string.Format("{0:##0} B", size);

            if (size < 1000 * 1024)
                return string.Format("{0:##0.0} KiB", size / 1024d);

            if (size < 1000 * 1024 * 1024)
                return string.Format("{0:##0.0} MiB", size / 1024d / 1024d);

            return string.Format("{0:##0.0} GiB", size / 1024d / 1024d / 1024d);
        }
	}
}
