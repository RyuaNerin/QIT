using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace QIT
{
	static class Settings
	{
		public static string	FilePath	= Path.Combine(Application.StartupPath, "QIT.info");

		public static string	CKey		= null;
		public static string	CSecret		= null;
		public static string	UToken		= null;
		public static string	USecret		= null;

		// 0 : Auto
		// 1 : PNG
		// 2 : JPEG
		public static int		ImageExt	= 0;
		public static bool		PNGTrans	= true;

		public static void Load()
		{
			if (!File.Exists(Settings.FilePath))
				return;

			int		key;
			int		len;
			byte[]	buff;

			try
			{
				using (Stream stream = new FileStream(Settings.FilePath, FileMode.Open, FileAccess.Read))
				{
					while (stream.Position < stream.Length)
					{
						key = stream.ReadByte();
						len = stream.ReadByte() << 8 | stream.ReadByte();

						if (len > 0)
						{
							buff = new byte[len];

							stream.Read(buff, 0, len);

							switch (key)
							{
								case 0:
									Settings.UToken = Encoding.UTF8.GetString(buff);
									break;

								case 1:
									Settings.USecret = Encoding.UTF8.GetString(buff);
									break;

								case 2:
									Settings.ImageExt = buff[0];
									break;

								case 3:
									Settings.PNGTrans = (buff[0] == 1);
									break;
							}
						}
					}
				}
			}
			catch
			{ }
		}

		public static void Save()
		{
			using (Stream stream = new FileStream(Settings.FilePath, FileMode.Create, FileAccess.Write))
			{
				Settings.Save(stream, 0, Settings.UToken);
				Settings.Save(stream, 1, Settings.USecret);
				Settings.Save(stream, 2, (byte)Settings.ImageExt);
				Settings.Save(stream, 3, (byte)(Settings.PNGTrans ? 1 : 0));
			}
		}
		private static void Save(Stream stream, int key, string str)
		{
			if (!String.IsNullOrEmpty(str))
			{
				byte[] buff = Encoding.UTF8.GetBytes(str);
				Settings.Save(stream, key, buff.Length, buff);
			}
			else
			{
				Settings.Save(stream, key, 0, null);
			}
		}
		private static void Save(Stream stream, int key, byte buff)
		{
			Settings.Save(stream, key, 1, new byte[] { buff });
		}
		private static void Save(Stream stream, int key, int len, byte[] buff)
		{
			byte len0 = (byte)((len >> 8) & 0xFF);
			byte len1 = (byte)((len >> 0) & 0xFF);

			stream.WriteByte((byte)key);
			stream.WriteByte(len0);
			stream.WriteByte(len1);

			if (buff != null && buff.Length > 0)
				stream.Write(buff, 0, buff.Length);
		}
	}
}
