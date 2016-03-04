using System;
using System.Runtime.InteropServices;

namespace SimplePsd
{
	/// <summary>
	/// This class shall keep the Win32 APIs being used in 
	/// the program.
	/// </summary>
	internal static class NativeMethods
	{

		#region Class Variables
		public const int BI_RGB = 0;
		public const int WHITE_BRUSH = 0;
		#endregion
		
		#region Class Functions

        [DllImport("gdi32.dll", EntryPoint="DeleteDC")]
        [return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool DeleteDC(IntPtr hDc);

        [DllImport("gdi32.dll", EntryPoint="DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool DeleteObject(IntPtr hDc);

		[DllImport ("gdi32.dll",EntryPoint="CreateCompatibleDC")]
		public static extern IntPtr CreateCompatibleDC(IntPtr hdc);

		[DllImport ("gdi32.dll",EntryPoint="SelectObject")]
		public static extern IntPtr SelectObject(IntPtr hdc,IntPtr bmp);

		[DllImport("gdi32.dll", EntryPoint="CreateDIBSection")]
		public static extern IntPtr CreateDIBSection(IntPtr hDC, [In] ref BITMAPINFO pBitmapInfo, int un, IntPtr lplpVoid, IntPtr handle, int offset);
		
		[DllImport("gdi32.dll", EntryPoint="GetStockObject")]
		public static extern IntPtr  GetStockObject(int fnObject);

		[DllImport("gdi32.dll", EntryPoint="SetPixel")]
		public static extern uint SetPixel(IntPtr hDC, int x, int y, int nColor);

		[DllImport("user32.dll",EntryPoint="GetDC")]
		public static extern IntPtr GetDC(IntPtr ptr);

		[DllImport("user32.dll",EntryPoint="ReleaseDC")]
		public static extern int ReleaseDC(IntPtr hWnd,IntPtr hDc);
	
		[DllImport("user32.dll", EntryPoint="FillRect")]
		public static extern int FillRect(IntPtr hDC, [In] ref RECT lprc, IntPtr hbr);
		
		#endregion

        [StructLayout(LayoutKind.Sequential, Pack=1)]
        public struct BITMAPINFOHEADER
        {
            public void Init()
            {
                this.biSize = (uint)Marshal.SizeOf(this);
            }

            public uint     biSize;
            public int      biWidth;
            public int      biHeight;
            public ushort   biPlanes;
            public ushort   biBitCount;
            public uint     biCompression;
            public uint     biSizeImage;
            public int      biXPelsPerMeter;
            public int      biYPelsPerMeter;
            public uint     biClrUsed;
            public uint     biClrImportant;
        }

        [StructLayout(LayoutKind.Sequential, Pack=1)]
        public struct BITMAPINFO
        {
            public BITMAPINFOHEADER bmiHeader;
            public RGBQUAD[] bmiColors;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RGBQUAD
        {
            public byte rgbBlue;
            public byte rgbGreen;
            public byte rgbRed;
            public byte rgbReserved;
        }

        [StructLayout(LayoutKind.Sequential, Pack=1)]
        public struct RECT
        {
            public int left;
            public int right;
            public int top;
            public int bottom;
        }
	}
}
