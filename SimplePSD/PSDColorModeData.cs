using System;

namespace SimplePsd
{
	/// <summary>
	/// ColorModeData class
	/// </summary>
	public class PSDColorModeData
	{
		public int nLength;
		public byte[] ColourData;

		public PSDColorModeData()
		{
			nLength = -1;
		}
	}
}
