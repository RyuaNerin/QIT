using System;
using System.IO;

namespace SimplePsd
{
	internal class BigEndianBinaryReader : BinaryReader
	{
		public BigEndianBinaryReader(Stream stream)
			: base (stream)
		{
		}

		public int BytesRead { get; set; }

		public long Position
		{
			get { return this.BaseStream.Position; }
			set { this.BaseStream.Position = value; }
		}
		public long Length
		{
			get { return this.BaseStream.Length; }
		}

		public void Move(int count)
		{
			this.BytesRead += count;
			base.BaseStream.Seek(count, SeekOrigin.Current);
		}
		public override byte ReadByte()
		{
			this.BytesRead += 1;
			return base.ReadByte();
		}
		public override byte[] ReadBytes(int offset)
		{
			this.BytesRead += offset;
			return base.ReadBytes(offset);
		}
		public override short ReadInt16()
		{
			this.BytesRead += 2;
			return BitConverter.ToInt16(BigEndianBinaryReader.SwapBytes(base.ReadBytes(2)), 0);
		}
		public override ushort ReadUInt16()
		{
			this.BytesRead += 2;
			return BitConverter.ToUInt16(BigEndianBinaryReader.SwapBytes(base.ReadBytes(2)), 0);
		}
		public override int ReadInt32()
		{
			this.BytesRead += 4;
			return BitConverter.ToInt32(BigEndianBinaryReader.SwapBytes(base.ReadBytes(4)), 0);
		}
		public override uint ReadUInt32()
		{
			this.BytesRead += 4;
			return BitConverter.ToUInt32(BigEndianBinaryReader.SwapBytes(base.ReadBytes(4)), 0);
		}
		public override long ReadInt64()
		{
			this.BytesRead += 8;
			return BitConverter.ToInt64(BigEndianBinaryReader.SwapBytes(base.ReadBytes(8)), 0);
		}
		public override ulong ReadUInt64()
		{
			this.BytesRead += 8;
			return BitConverter.ToUInt64(BigEndianBinaryReader.SwapBytes(base.ReadBytes(8)), 0);
		}

		public static byte[] SwapBytes(byte[] array)
		{
			return BigEndianBinaryReader.SwapBytes(array, array.Length);
		}
		public static byte[] SwapBytes(byte[] array, int length)
		{
			int i = 0;
			int j = length / 2;
			int k;
			byte t;

			for (i = 0; i < j; ++i)
			{
				k = length - i - 1;

				t			= array[i];
				array[i]	= array[k];
				array[k]	= t;
			}

			return array;
		}
	}
}
