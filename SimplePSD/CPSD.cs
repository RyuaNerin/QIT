using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Linq;
using System.Runtime.InteropServices;

namespace SimplePsd
{
	/// <summary>
	/// Main class is for opening Adobe Photoshop files
	/// </summary>
	public class CPSD
	{

		private PSDHeaderInfo       m_HeaderInfo;
		private PSDColorModeData    m_ColorModeData;
		private PSDImageResource    m_ImageResource;
		private PSDResolutionInfo   m_ResolutionInfo;
		private PSDDisplayInfo      m_DisplayInfo;
		private PSDThumbNail        m_ThumbNail;

		private bool	m_bResolutionInfoFilled;
		private bool	m_bThumbnailFilled;
		private bool    m_bCopyright;

		private short   m_nColourCount;
		private short   m_nTransparentIndex;
		private int		m_nGlobalAngle;
		private int		m_nCompression;
		private	IntPtr  m_hBitmap;
		
		public CPSD()
		{
			this.m_bResolutionInfoFilled = false;
			this.m_bThumbnailFilled = false;
			this.m_bCopyright = false;
			this.m_nColourCount = -1;
			this.m_nTransparentIndex = -1;
			this.m_nGlobalAngle = 30;
			this.m_nCompression = -1;
			this.m_hBitmap = IntPtr.Zero;
		}

		~CPSD()
		{
			try
			{
				Marshal.FreeHGlobal(this.m_hBitmap);
			}
			catch
			{ }
		}
		
		public void Load(string strPathName)
		{
			this.Load(new BigEndianBinaryReader(new FileStream(strPathName, FileMode.Open, FileAccess.Read, FileShare.Read)));
		}
		public void Load(Stream stream)
		{
			this.Load(new BigEndianBinaryReader(stream));
		}
		private void Load(BigEndianBinaryReader stream)
		{
			stream.BaseStream.Position = 0;

			this.ReadHeader(stream);

			this.ReadColourModeData(stream);

			this.ReadImageResource(stream);

			this.ReadLayerAndMaskInfoSection(stream);

			this.ReadImageData(stream);
		}

		private static readonly byte[] Signature_8BPS = { 0x38, 0x42, 0x50, 0x53 };
		private void ReadHeader(BigEndianBinaryReader stream)
		{
			// Set Position to the beginning of the stream.
			byte [] Signature  = stream.ReadBytes(4);
			byte [] Version    = stream.ReadBytes(2);
			byte [] Reserved   = stream.ReadBytes(6);

			if (Enumerable.SequenceEqual(Signature, Signature_8BPS))
			{
				if (Version[1] == 0x01)
				{
					this.m_HeaderInfo = new PSDHeaderInfo();

					this.m_HeaderInfo.nChannels = stream.ReadInt16();
					this.m_HeaderInfo.nHeight = stream.ReadInt32();
					this.m_HeaderInfo.nWidth = stream.ReadInt32();
					this.m_HeaderInfo.nBitsPerPixel = stream.ReadInt16();
					this.m_HeaderInfo.nColourMode = stream.ReadInt16();
				}
			}
		}

		private void ReadColourModeData(BigEndianBinaryReader stream)
		{
			this.m_ColorModeData = new PSDColorModeData();
			this.m_ColorModeData.nLength = stream.ReadInt32();

			if (this.m_ColorModeData.nLength > 0)
				this.m_ColorModeData.ColourData = stream.ReadBytes(this.m_ColorModeData.nLength);
		}

		private static readonly byte[] Signature_8BIM = { 0x38, 0x42, 0x49, 0x4D };
		private void ReadImageResource(BigEndianBinaryReader stream)
		{
			this.m_ImageResource	= new PSDImageResource();
			this.m_ResolutionInfo	= new PSDResolutionInfo();
			this.m_DisplayInfo		= new PSDDisplayInfo();
			this.m_ThumbNail		= new PSDThumbNail();

			this.m_ImageResource.nLength = stream.ReadInt32();
			int nTotalBytes = this.m_ImageResource.nLength;

			stream.BytesRead = 0;

			while ((stream.BaseStream.Position < stream.BaseStream.Length) && (stream.BytesRead < nTotalBytes))
			{
				this.m_ImageResource.Reset();
				this.m_ImageResource.OSType = stream.ReadBytes(4);

				if (Enumerable.SequenceEqual(this.m_ImageResource.OSType, Signature_8BIM))
				{
					this.m_ImageResource.nID = stream.ReadInt16();

					byte SizeOfName = stream.ReadByte();
						
					int nSizeOfName = (int)SizeOfName;
					if(nSizeOfName > 0)
					{
						if((nSizeOfName % 2) != 0)
							SizeOfName = stream.ReadByte();

						this.m_ImageResource.Name = stream.ReadBytes(nSizeOfName);
					}

					//SizeOfName = stream.ReadByte();
					stream.Move(1);

					this.m_ImageResource.nSize = stream.ReadInt32();

					if ((this.m_ImageResource.nSize % 2) != 0)
						this.m_ImageResource.nSize++;

					if (this.m_ImageResource.nSize > 0)
					{
						switch(this.m_ImageResource.nID)
						{
							case 1005:
								this.m_bResolutionInfoFilled = true;
								this.m_ResolutionInfo.hRes			= stream.ReadInt16();
								this.m_ResolutionInfo.hResUnit		= stream.ReadInt32();
								this.m_ResolutionInfo.widthUnit		= stream.ReadInt16();
								this.m_ResolutionInfo.vRes			= stream.ReadInt16();
								this.m_ResolutionInfo.vResUnit		= stream.ReadInt32();
								this.m_ResolutionInfo.heightUnit	= stream.ReadInt16();
								break;

							case 1007:
								this.m_DisplayInfo.ColourSpace	= stream.ReadInt16();
								this.m_DisplayInfo.Colour[0]	= stream.ReadInt16();
								this.m_DisplayInfo.Colour[1]	= stream.ReadInt16();
								this.m_DisplayInfo.Colour[2]	= stream.ReadInt16();
								this.m_DisplayInfo.Colour[3]	= stream.ReadInt16();
								this.m_DisplayInfo.Opacity		= stream.ReadInt16();
								this.m_DisplayInfo.kind			= (stream.ReadByte() != 0x00);
								this.m_DisplayInfo.padding		= stream.ReadByte();

								if (this.m_DisplayInfo.Opacity < 0 || this.m_DisplayInfo.Opacity > 100)
									this.m_DisplayInfo.Opacity = 100;
								break;

							case 1034:
								this.m_bCopyright = (stream.ReadInt16() > 0);
								break;

							case 1033:
							case 1036:
								this.m_bThumbnailFilled = true;
								this.m_ThumbNail.nFormat			= stream.ReadInt32();
								this.m_ThumbNail.nWidth				= stream.ReadInt32();
								this.m_ThumbNail.nHeight			= stream.ReadInt32();
								this.m_ThumbNail.nWidthBytes		= stream.ReadInt32();
								this.m_ThumbNail.nSize				= stream.ReadInt32();
								this.m_ThumbNail.nCompressedSize	= stream.ReadInt32();
								this.m_ThumbNail.nBitPerPixel		= stream.ReadInt16();
								this.m_ThumbNail.nPlanes			= stream.ReadInt16();
								
								int nTotalData = this.m_ImageResource.nSize - 28;

								// In BGR format
								if (this.m_ImageResource.nID == 1033)
								{
									if (nTotalData % 3 != 0)
										nTotalData += 3 - (nTotalData % 3);

									stream.Move(nTotalData);
								}
								// In RGB format
								else if (this.m_ImageResource.nID == 1036)
								{
									stream.Move(nTotalData);
								}

								break;

							case 1037:
								this.m_nGlobalAngle = stream.ReadInt32();
								break;

							case 1046:
								this.m_nColourCount = stream.ReadInt16();
								break;

							case 1047:
								this.m_nTransparentIndex = stream.ReadInt16();
								break;

							default:
								stream.Move(this.m_ImageResource.nSize);
								break;
						}
					}
				}
			}
		}

		private void ReadLayerAndMaskInfoSection(BigEndianBinaryReader stream)
		{
			int nTotalBytes = stream.ReadInt32();
				
			if(stream.Position + nTotalBytes < stream.Length)
				stream.Move(nTotalBytes);
		}

		private void ReadImageData(BigEndianBinaryReader stream)
		{
			this.m_nCompression = stream.ReadInt16();

			switch(this.m_nCompression)
			{
				case 0:
					#region
					{
						int nWidth = this.m_HeaderInfo.nWidth;
						int nHeight = this.m_HeaderInfo.nHeight;
						int bytesPerPixelPerChannel = this.m_HeaderInfo.nBitsPerPixel / 8;
						
						int nPixels = nWidth * nHeight;
						int nTotalBytes = nPixels * bytesPerPixelPerChannel * this.m_HeaderInfo.nChannels;

						byte [] pData = new byte[nTotalBytes];
						byte [] pImageData = new byte[nTotalBytes];
						
						#region switch(m_HeaderInfo.nColourMode)
						switch(this.m_HeaderInfo.nColourMode)
						{
							case 1:		// Grayscale
							case 2:		// Indexed
							case 8:		// Duotone
							{
								pData = new byte[nTotalBytes];
								pImageData = new byte[bytesPerPixelPerChannel];
								
								for(int i = 0; i < nTotalBytes; i++) pData[i] = 254;
								for(int i = 0; i < bytesPerPixelPerChannel; i++) pImageData[i] = 254;
								
								stream.BytesRead = 0;
								while(stream.BytesRead < nTotalBytes)
								{
									pImageData = stream.ReadBytes(bytesPerPixelPerChannel);
									
									for(int j = 0; j < bytesPerPixelPerChannel; j++)
										pData[stream.BytesRead + j] = pImageData[j];
								}
							}
								break;

							case 3:		// RGB
							{
								int nBytesToReadPerPixelPerChannel = bytesPerPixelPerChannel;
								if (nBytesToReadPerPixelPerChannel == 2)
								{
									nBytesToReadPerPixelPerChannel = 1;
									nTotalBytes = nPixels * nBytesToReadPerPixelPerChannel * this.m_HeaderInfo.nChannels;
								}

								pImageData = new byte[nBytesToReadPerPixelPerChannel];
								pData = new byte[nTotalBytes];

								for(int i = 0; i < nTotalBytes; i++) pData[i] = 254;
								for(int i = 0; i < nBytesToReadPerPixelPerChannel; i++) pImageData[i] = 254;
								
								int nPixelCounter = 0;
								for(int nColour = 0; nColour<3; ++nColour)
								{
									nPixelCounter = nColour;
									for(int nPos=0; nPos<nPixels; ++nPos)
									{
										if(stream.BytesRead < nTotalBytes)
										{
											pImageData = stream.ReadBytes(nBytesToReadPerPixelPerChannel);

											for(int j = 0; j < nBytesToReadPerPixelPerChannel; j++)
												pData[nPixelCounter+j] = pImageData[j];

											nPixelCounter += 3;
											if(bytesPerPixelPerChannel == 2)
												pImageData = stream.ReadBytes(nBytesToReadPerPixelPerChannel);
										}
									}
								}
							}
								break;

							case 4:	// CMYK
							{
								pImageData = new byte[bytesPerPixelPerChannel];
								pData = new byte[nTotalBytes];
								
								for(int i = 0; i < nTotalBytes; i++) pData[i] = 254;
								for(int i = 0; i < bytesPerPixelPerChannel; i++) pImageData[i] = 254;

								int nPixelCounter = 0;
								for(int nColour = 0; nColour<4; ++nColour)
								{
									nPixelCounter = nColour*bytesPerPixelPerChannel;
									for(int nPos=0; nPos<nPixels; ++nPos)
									{
										if(stream.BytesRead < nTotalBytes)
										{
											pImageData = stream.ReadBytes(bytesPerPixelPerChannel);

											for(int j=0;j<bytesPerPixelPerChannel;j++)
												pData[nPixelCounter+j] = pImageData[j];

											nPixelCounter += 4*bytesPerPixelPerChannel;
										}
									}
								}
							}
								break;

							case 9:	// Lab
							{
								pImageData = new byte[bytesPerPixelPerChannel];
								pData = new byte[nTotalBytes];
								for(int i = 0; i < nTotalBytes; i++) pData[i] = 254;
								for(int i = 0; i < bytesPerPixelPerChannel; i++) pImageData[i] = 254;
								
								int nPixelCounter = 0;
								for(int nColour = 0; nColour < 3; ++nColour)
								{
									nPixelCounter = nColour * bytesPerPixelPerChannel;
									
									for(int nPos = 0; nPos < nPixels; ++nPos)
									{
										if(stream.BytesRead < nTotalBytes)
										{
											pImageData = stream.ReadBytes(bytesPerPixelPerChannel);
											
											for(int j = 0; j < bytesPerPixelPerChannel; j++)
												pData[nPixelCounter + j] = pImageData[j];
											
											nPixelCounter += 3*bytesPerPixelPerChannel;
										}
									}
								}
							}
								break;
						}
						#endregion

						if(stream.BytesRead == nTotalBytes)
						{
							int ppm_x = 3780;	// 96 dpi
							int ppm_y = 3780;	// 96 dpi

							if(this.m_bResolutionInfoFilled)
							{
								int nHorzResolution = (int)this.m_ResolutionInfo.hRes;
								int nVertResolution = (int)this.m_ResolutionInfo.vRes;

								ppm_x = (nHorzResolution * 10000) / 254;
								ppm_y = (nVertResolution * 10000) / 254;
							}

							switch(this.m_HeaderInfo.nBitsPerPixel)
							{
								case 1:
									throw new Exception("Not yet implemented");

								case 8:
								case 16:
									CreateDIBSection(nWidth, nHeight, ppm_x, ppm_y, 24);
									break;

								default:
									throw new Exception("Unsupported Format");
							}
							
							if(this.m_hBitmap == IntPtr.Zero)
								throw new Exception("Cannot create hBitmap");

							ProccessBuffer(pData);
						}
					}
					#endregion
					break;

				case 1:	// RLE compression
					#region
					{
						int nWidth = this.m_HeaderInfo.nWidth;
						int nHeight = this.m_HeaderInfo.nHeight;
						int bytesPerPixelPerChannel = this.m_HeaderInfo.nBitsPerPixel / 8;
						
						int nPixels = nWidth * nHeight;
						int nTotalBytes = nPixels * bytesPerPixelPerChannel * this.m_HeaderInfo.nChannels;

						byte [] pDest = new byte[nTotalBytes];
						byte [] pData = new byte[nTotalBytes];
						for(long i = 0; i < nTotalBytes; ++i) pData[i] = 254;
						for(long i = 0; i < nTotalBytes; ++i) pDest[i] = 254;

						byte ByteValue = 0x00;

						int Count = 0;

						int nPointer = 0;

						// The RLE-compressed data is proceeded by a 2-byte data count for each row in the data,
						// which we're going to just skip.
						stream.Position += nHeight * this.m_HeaderInfo.nChannels * 2;


						for(int channel=0; channel< this.m_HeaderInfo.nChannels; ++channel)
						{
							// Read the RLE data.
							Count = 0;
							while(Count < nPixels)
							{
								ByteValue = stream.ReadByte();

								int len = (int)ByteValue;
								if(len < 128)
								{
									len++;
									Count += len;

									while(len != 0)
									{
										ByteValue = stream.ReadByte();

										pData[nPointer] = ByteValue;
										nPointer++;
										len--;
									}
								}
								else if(len > 128)
								{
									// Next -len+1 bytes in the dest are replicated from next source byte.
									// (Interpret len as a negative 8-bit int.)
									len ^= 0x0FF;
									len += 2;
									ByteValue = stream.ReadByte();

									Count += len;

									while(len!=0)
									{
										pData[nPointer] = ByteValue;
										nPointer++;
										len--;
									}
								}
								else if ( 128 == len )
								{
									// Do nothing
								}
							}
						}

						int nPixelCounter = 0;
						nPointer = 0;

						for(int nColour = 0; nColour < this.m_HeaderInfo.nChannels; ++nColour)
						{
							nPixelCounter = nColour * bytesPerPixelPerChannel;
							for(int nPos = 0; nPos < nPixels; ++nPos)
							{
								for(int j = 0; j < bytesPerPixelPerChannel; ++j)
									pDest[nPixelCounter + j] = pData[nPointer + j];

								nPointer++;

								nPixelCounter += this.m_HeaderInfo.nChannels * bytesPerPixelPerChannel;
							}
						}

						for(int i = 0; i < nTotalBytes; i++)
							pData[i] = pDest[i];
						
						int ppm_x = 3780;	// 96 dpi
						int ppm_y = 3780;	// 96 dpi

						if(this.m_bResolutionInfoFilled)
						{
							int nHorResolution = (int)this.m_ResolutionInfo.hRes;
							int nVertResolution = (int)this.m_ResolutionInfo.vRes;

							ppm_x = (nHorResolution * 10000) / 254;
							ppm_y = (nVertResolution * 10000) / 254;
						}

						switch (this.m_HeaderInfo.nBitsPerPixel)
						{
							case 1:
								throw new FormatException("Not yet implemented");

							case 8:
							case 16:
								CreateDIBSection(nWidth, nHeight, ppm_x, ppm_y, 24);
								break;

							default:
								throw new FormatException("Unsupported format");
						}

						if (this.m_hBitmap == IntPtr.Zero)
							throw new FormatException("Cannot create hBitmap");

						ProccessBuffer(pData);
					}
					#endregion
					break;

				case 2:	// ZIP without prediction
					throw new FormatException("ZIP without prediction, no specification");

				case 3:	// ZIP with prediction
					throw new FormatException("ZIP with prediction, no specification");

				default:
					throw new FormatException("Unknown format");
			}
		}

		private void CreateDIBSection(int cx, int cy, int ppm_x, int ppm_y, short BitCount)
		{
			IntPtr hDC = WinInvoke32.GetDC(IntPtr.Zero);
		
			if(hDC.Equals(IntPtr.Zero)) return;

			IntPtr pvBits = IntPtr.Zero;
			BITMAPINFO BitmapInfo = new BITMAPINFO();
			BitmapInfo.bmiHeader = new BITMAPINFOHEADER();

			BitmapInfo.bmiHeader.biSize               = 40;
			BitmapInfo.bmiHeader.biWidth              = cx;
			BitmapInfo.bmiHeader.biHeight             = cy;
			BitmapInfo.bmiHeader.biPlanes             = 1;
			BitmapInfo.bmiHeader.biBitCount           = BitCount;
			BitmapInfo.bmiHeader.biCompression        = WinInvoke32.BI_RGB;
			BitmapInfo.bmiHeader.biSizeImage          = 0;
			BitmapInfo.bmiHeader.biXPelsPerMeter      = ppm_x; 
			BitmapInfo.bmiHeader.biYPelsPerMeter      = ppm_y;
			BitmapInfo.bmiHeader.biClrImportant       = 0;
			BitmapInfo.bmiHeader.biClrUsed            = 0;

			this.m_hBitmap = WinInvoke32.CreateDIBSection(hDC, ref BitmapInfo, 0, pvBits, IntPtr.Zero, 0);

			if(this.m_hBitmap.Equals(IntPtr.Zero))  
			{
				WinInvoke32.ReleaseDC(IntPtr.Zero, hDC);
				return;
			}
			else
			{
				IntPtr hdcMemory = WinInvoke32.CreateCompatibleDC(hDC);
				IntPtr hbmpOld = WinInvoke32.SelectObject( hdcMemory, this.m_hBitmap );
				RECT rc;
	
				rc.left = rc.top = 0;
				rc.right = cx;
				rc.bottom = cy;
				IntPtr hBrush = WinInvoke32.GetStockObject(WinInvoke32.WHITE_BRUSH);
				WinInvoke32.FillRect(hdcMemory, ref rc, hBrush);
				WinInvoke32.SelectObject(hdcMemory, hbmpOld);
				WinInvoke32.DeleteDC(hdcMemory);
			}

			WinInvoke32.ReleaseDC(IntPtr.Zero, hDC);
		}

		private void ProccessBuffer(byte[] pData)
		{
			if(this.m_hBitmap.Equals(IntPtr.Zero)) return;

			IntPtr hBitmap = this.m_hBitmap;

			IntPtr hdcMemory = IntPtr.Zero;
			IntPtr hbmpOld = IntPtr.Zero;

			short bytesPerPixelPerChannel = (short)(this.m_HeaderInfo.nBitsPerPixel / 8);
			int nPixels = this.m_HeaderInfo.nWidth * this.m_HeaderInfo.nHeight;
			int nTotalBytes = nPixels * bytesPerPixelPerChannel * this.m_HeaderInfo.nChannels;
			
			switch (this.m_HeaderInfo.nColourMode)
			{
				case 1:		// Grayscale
				case 8:		// Duotone
					#region
					{
						hdcMemory = WinInvoke32.CreateCompatibleDC(IntPtr.Zero);
						hbmpOld = WinInvoke32.SelectObject(hdcMemory, hBitmap);

						int nCounter = 0;
						int nValue = 0;
						int nColor = 0;


						byte[] ColorValue = new byte[64];

						for(int nRow = 0; nRow < this.m_HeaderInfo.nHeight; ++nRow)
						{
							for(int nCol = 0; nCol < this.m_HeaderInfo.nWidth; ++nCol)
							{
								for(int i=0;i<bytesPerPixelPerChannel;i++)
									ColorValue[i] = pData[nCounter+i];

								BigEndianBinaryReader.SwapBytes(ColorValue, bytesPerPixelPerChannel);
							
								nValue = BitConverter.ToInt32(ColorValue,0);
								if(this.m_HeaderInfo.nBitsPerPixel == 16)
									nValue = nValue / 256;

								if(nValue > 255) nValue = 255;
								else if(nValue < 0) nValue = 0;

								nColor = ColorTranslator.ToWin32(Color.FromArgb(nValue, nValue, nValue));
								WinInvoke32.SetPixel(hdcMemory, nCol, nRow, nColor);
							
								nCounter += bytesPerPixelPerChannel;
							}
						}
						WinInvoke32.SelectObject(hdcMemory, hbmpOld);
						WinInvoke32.DeleteDC(hdcMemory);
					}
					#endregion
					break;

				case 2: // Indexed
					#region
					{
						hdcMemory = WinInvoke32.CreateCompatibleDC(IntPtr.Zero);
						hbmpOld = WinInvoke32.SelectObject(hdcMemory, hBitmap);
						// pData holds the indices of loop through the palette and set the correct RGB
						// 8bpp are supported
						if(this.m_ColorModeData.nLength==768 && this.m_nColourCount>0)
						{
							int nRow = 0;
							int nCol = 0;
							int nRed = 0;
							int nGreen = 0;
							int nBlue = 0;
							int nIndex = 0;
							int nColor = 0;

							for(int nCounter=0; nCounter<nTotalBytes; ++nCounter)
							{
								nIndex = (int)pData[nCounter];
								nRed = (int)this.m_ColorModeData.ColourData[nIndex];
								nGreen = (int)this.m_ColorModeData.ColourData[nIndex + 256];
								nBlue = (int)this.m_ColorModeData.ColourData[nIndex + 2 * 256];

								nColor = ColorTranslator.ToWin32(Color.FromArgb(nRed, nGreen, nBlue));
								WinInvoke32.SetPixel(hdcMemory, nCol, nRow, nColor);
								nCol++;
								if(this.m_HeaderInfo.nWidth <= nCol)
								{
									nCol = 0;
									nRow++;
								}
							}
						}

						WinInvoke32.SelectObject(hdcMemory, hbmpOld);
						WinInvoke32.DeleteDC(hdcMemory);
					}
					#endregion
					break;

				case 3:	// RGB
					#region
					{
						hdcMemory = WinInvoke32.CreateCompatibleDC(IntPtr.Zero);
						hbmpOld = WinInvoke32.SelectObject(hdcMemory, hBitmap);

						int nBytesToRead = this.m_HeaderInfo.nBitsPerPixel / 8;
						if (nBytesToRead == 2)
							nBytesToRead = 1;

						int nRow = 0;
						int nCol = 0;
						int nRed = 0;
						int nGreen = 0;
						int nBlue = 0;
						int nColor = 0;
						byte [] ColorValue = new byte[8];

						for (int nCounter = 0; nCounter < nTotalBytes; nCounter += this.m_HeaderInfo.nChannels * nBytesToRead)
						{
							Array.Copy(pData, nCounter + 0 * nBytesToRead, ColorValue, 0, nBytesToRead);
							BigEndianBinaryReader.SwapBytes(ColorValue, nBytesToRead);
							nRed = BitConverter.ToInt32(ColorValue, 0);

							Array.Copy(pData, nCounter + 1 * nBytesToRead, ColorValue, 0, nBytesToRead);
							BigEndianBinaryReader.SwapBytes(ColorValue, nBytesToRead);
							nGreen = BitConverter.ToInt32(ColorValue, 0);

							Array.Copy(pData, nCounter + 2 * nBytesToRead, ColorValue, 0, nBytesToRead);
							BigEndianBinaryReader.SwapBytes(ColorValue, nBytesToRead);
							nBlue = BitConverter.ToInt32(ColorValue, 0);

							nColor = ColorTranslator.ToWin32(Color.FromArgb(nRed, nGreen, nBlue));
							WinInvoke32.SetPixel(hdcMemory, nCol, nRow, nColor);
							nCol++;
							if (this.m_HeaderInfo.nWidth <= nCol)
							{
								nCol = 0;
								nRow++;
							}
						}

						WinInvoke32.SelectObject(hdcMemory, hbmpOld);
						WinInvoke32.DeleteDC(hdcMemory);
					}
					#endregion
					break;

				case 4:	// CMYK
					#region
					{
						hdcMemory = WinInvoke32.CreateCompatibleDC(IntPtr.Zero);
						hbmpOld = WinInvoke32.SelectObject(hdcMemory, hBitmap);

						double C, M, Y, K;
						double exC, exM, exY, exK;

						int nRow = 0;
						int nCol = 0;
						int nColor = 0;

						byte [] ColorValue = new byte[8];

						double dMaxColours = Math.Pow(2, this.m_HeaderInfo.nBitsPerPixel);

						Color crPixel = Color.White;

						for (int nCounter = 0; nCounter < nTotalBytes; nCounter += 4 * bytesPerPixelPerChannel)
						{
							Array.Copy(pData, nCounter + 0 * bytesPerPixelPerChannel, ColorValue, 0, bytesPerPixelPerChannel);
							BigEndianBinaryReader.SwapBytes(ColorValue, bytesPerPixelPerChannel);
							exC = (double)BitConverter.ToUInt32(ColorValue, 0);

							Array.Copy(pData, nCounter + 1 * bytesPerPixelPerChannel, ColorValue, 0, bytesPerPixelPerChannel);
							BigEndianBinaryReader.SwapBytes(ColorValue, bytesPerPixelPerChannel);
							exM = (double)BitConverter.ToUInt32(ColorValue, 0);

							Array.Copy(pData, nCounter + 2 * bytesPerPixelPerChannel, ColorValue, 0, bytesPerPixelPerChannel);
							BigEndianBinaryReader.SwapBytes(ColorValue, bytesPerPixelPerChannel);
							exY = (double)BitConverter.ToUInt32(ColorValue, 0);

							Array.Copy(pData, nCounter + 3 * bytesPerPixelPerChannel, ColorValue, 0, bytesPerPixelPerChannel);
							BigEndianBinaryReader.SwapBytes(ColorValue, bytesPerPixelPerChannel);
							exK = (double)BitConverter.ToUInt32(ColorValue, 0);

							C = (1.0 - exC / dMaxColours);
							M = (1.0 - exM / dMaxColours);
							Y = (1.0 - exY / dMaxColours);
							K = (1.0 - exK / dMaxColours);

							crPixel = CMYKToRGB(C, M, Y, K);

							nColor = ColorTranslator.ToWin32(crPixel);
							WinInvoke32.SetPixel(hdcMemory, nCol, nRow, nColor);
							nCol++;
							if (this.m_HeaderInfo.nWidth <= nCol)
							{
								nCol = 0;
								nRow++;
							}
						}

						WinInvoke32.SelectObject(hdcMemory, hbmpOld);
						WinInvoke32.DeleteDC(hdcMemory);
					}
					#endregion
					break;

				case 7:		// Multichannel
					#region
					{
						hdcMemory = WinInvoke32.CreateCompatibleDC(IntPtr.Zero);
						hbmpOld = WinInvoke32.SelectObject(hdcMemory, hBitmap);

						double C, M, Y, K;
						double exC, exM, exY, exK;

						int nRow = 0;
						int nCol = 0;
						int nColor = 0;

						byte [] ColorValue = new byte[8];

						double dMaxColours = Math.Pow(2, this.m_HeaderInfo.nBitsPerPixel);

						Color crPixel = Color.White;

						// assume format is in either CMY or CMYK
						if(this.m_HeaderInfo.nChannels >= 3)
						{
							for(int nCounter = 0; nCounter < nTotalBytes; nCounter += this.m_HeaderInfo.nChannels * bytesPerPixelPerChannel)
							{
								Array.Copy(pData, nCounter + 0 * bytesPerPixelPerChannel, ColorValue, 0, bytesPerPixelPerChannel);
								BigEndianBinaryReader.SwapBytes(ColorValue, bytesPerPixelPerChannel);
								exC = (double)BitConverter.ToUInt32(ColorValue, 0);

								Array.Copy(pData, nCounter + 1 * bytesPerPixelPerChannel, ColorValue, 0, bytesPerPixelPerChannel);
								BigEndianBinaryReader.SwapBytes(ColorValue, bytesPerPixelPerChannel);
								exM = (double)BitConverter.ToUInt32(ColorValue, 0);

								Array.Copy(pData, nCounter + 2 * bytesPerPixelPerChannel, ColorValue, 0, bytesPerPixelPerChannel);
								BigEndianBinaryReader.SwapBytes(ColorValue, bytesPerPixelPerChannel);
								exY = (double)BitConverter.ToUInt32(ColorValue, 0);
							
								C = (1.0 - exC / dMaxColours);
								M = (1.0 - exM / dMaxColours);
								Y = (1.0 - exY / dMaxColours);
								K = 0;
							
								if(this.m_HeaderInfo.nChannels == 4)
								{
									Array.Copy(pData,nCounter+3*bytesPerPixelPerChannel, ColorValue, 0, bytesPerPixelPerChannel);
									BigEndianBinaryReader.SwapBytes(ColorValue, bytesPerPixelPerChannel);
									exK = (double)BitConverter.ToUInt32(ColorValue, 0);

									K = (1.0 - exK / dMaxColours);
								}

								crPixel = CMYKToRGB(C, M, Y, K);

								nColor = ColorTranslator.ToWin32(crPixel);
								WinInvoke32.SetPixel(hdcMemory, nCol, nRow, nColor);
								nCol++;
								if (this.m_HeaderInfo.nWidth <= nCol)
								{
									nCol = 0;
									nRow++;
								}
							}
						}

						WinInvoke32.SelectObject(hdcMemory, hbmpOld);
						WinInvoke32.DeleteDC(hdcMemory);
					}
					#endregion
					break;

				case 9:	// Lab
					#region
					{
						hdcMemory = WinInvoke32.CreateCompatibleDC(IntPtr.Zero);
						hbmpOld = WinInvoke32.SelectObject(hdcMemory, hBitmap);

						int L, a, b;

						int nRow = 0;
						int nCol = 0;
						int nColor = 0;

						byte [] ColorValue = new byte[64];

						double exL, exA, exB;
						double L_coef, a_coef, b_coef;
						double dMaxColours = Math.Pow(2, this.m_HeaderInfo.nBitsPerPixel);

						L_coef = dMaxColours / 100.0;
						a_coef = dMaxColours / 256.0;
						b_coef = dMaxColours / 256.0;

						Color crPixel = Color.White;
					
						for(int nCounter = 0; nCounter < nTotalBytes; nCounter += 3 * bytesPerPixelPerChannel)
						{
							Array.Copy(pData, nCounter + 0 * bytesPerPixelPerChannel, ColorValue, 0, bytesPerPixelPerChannel);
							BigEndianBinaryReader.SwapBytes(ColorValue, bytesPerPixelPerChannel);
							exL = (double)BitConverter.ToUInt32(ColorValue,0);

							Array.Copy(pData, nCounter + 1 * bytesPerPixelPerChannel, ColorValue, 0, bytesPerPixelPerChannel);
							BigEndianBinaryReader.SwapBytes(ColorValue, bytesPerPixelPerChannel);
							exA = (double)BitConverter.ToUInt32(ColorValue,0);

							Array.Copy(pData, nCounter + 2 * bytesPerPixelPerChannel, ColorValue, 0, bytesPerPixelPerChannel);
							BigEndianBinaryReader.SwapBytes(ColorValue, bytesPerPixelPerChannel);
							exB = (double)BitConverter.ToUInt32(ColorValue,0);

							L = (int)(exL / L_coef);
							a = (int)(exA / a_coef - 128.0);
							b = (int)(exB / b_coef - 128.0);

							crPixel = LabToRGB(L, a, b);

							nColor = ColorTranslator.ToWin32(crPixel);
							WinInvoke32.SetPixel(hdcMemory, nCol, nRow, nColor);
							nCol++;
							if(this.m_HeaderInfo.nWidth <= nCol)
							{
								nCol = 0;
								nRow++;
							}
						}

						WinInvoke32.SelectObject(hdcMemory, hbmpOld);
						WinInvoke32.DeleteDC(hdcMemory);
					}
					#endregion
					break;
			}
		}

		private Color LabToRGB(int L, int a, int b)
		{
			// For the conversion we first convert values to XYZ and then to RGB
			// Standards used Observer = 2, Illuminant = D65
			
			const double ref_X = 95.047;
			const double ref_Y = 100.000;
			const double ref_Z = 108.883;

			double var_Y = ( (double)L + 16.0 ) / 116.0;
			double var_X = (double)a / 500.0 + var_Y;
			double var_Z = var_Y - (double)b / 200.0;

			if ( Math.Pow(var_Y, 3) > 0.008856 )
			var_Y = Math.Pow(var_Y, 3);
			else
			var_Y = ( var_Y - 16 / 116 ) / 7.787;

			if ( Math.Pow(var_X, 3) > 0.008856 )
			var_X = Math.Pow(var_X, 3);
			else
			var_X = ( var_X - 16 / 116 ) / 7.787;

			if ( Math.Pow(var_Z, 3) > 0.008856 )
			var_Z = Math.Pow(var_Z, 3);
			else
			var_Z = ( var_Z - 16 / 116 ) / 7.787;

			double X = ref_X * var_X;
			double Y = ref_Y * var_Y;
			double Z = ref_Z * var_Z;

			return XYZToRGB(X, Y, Z);
		}

		private Color XYZToRGB(double X, double Y, double Z)
		{
			// Standards used Observer = 2, Illuminant = D65
			// ref_X = 95.047, ref_Y = 100.000, ref_Z = 108.883

			double var_X = X / 100.0;
			double var_Y = Y / 100.0;
			double var_Z = Z / 100.0;

			double var_R = var_X * 3.2406 + var_Y * (-1.5372) + var_Z * (-0.4986);
			double var_G = var_X * (-0.9689) + var_Y * 1.8758 + var_Z * 0.0415;
			double var_B = var_X * 0.0557 + var_Y * (-0.2040) + var_Z * 1.0570;

			if ( var_R > 0.0031308 )
			var_R = 1.055 * ( Math.Pow(var_R, 1/2.4) ) - 0.055;
			else
			var_R = 12.92 * var_R;

			if ( var_G > 0.0031308 )
			var_G = 1.055 * ( Math.Pow(var_G, 1/2.4) ) - 0.055;
			else
			var_G = 12.92 * var_G;

			if ( var_B > 0.0031308 )
			var_B = 1.055 * ( Math.Pow(var_B, 1/2.4) )- 0.055;
			else
			var_B = 12.92 * var_B;

			int nRed = (int)(var_R * 256.0);
			int nGreen = (int)(var_G * 256.0);
			int nBlue = (int)(var_B * 256.0);

			if(nRed<0) nRed = 0;
			else if(nRed>255) nRed = 255;
			if(nGreen<0) nGreen = 0;
			else if(nGreen>255) nGreen = 255;
			if(nBlue<0)	nBlue = 0;
			else if(nBlue>255) nBlue = 255;

			return Color.FromArgb(nRed,nGreen,nBlue);
		}

		private Color CMYKToRGB(double C, double M, double Y, double K)
		{
			int nRed = (int)(( 1.0 - ( C *( 1 - K ) + K ) ) * 255);
			int nGreen = (int)(( 1.0 - ( M *( 1 - K ) + K ) ) * 255);
			int nBlue = (int)(( 1.0 - ( Y *( 1 - K ) + K ) ) * 255);

			if(nRed<0) nRed = 0;
			else if(nRed>255) nRed = 255;
			if(nGreen<0) nGreen = 0;
			else if(nGreen>255) nGreen = 255;
			if(nBlue<0)	nBlue = 0;
			else if(nBlue>255) nBlue = 255;

			return Color.FromArgb(nRed,nGreen,nBlue);
		}

		#region Properies
		public bool IsThumbnailIncluded
		{
			get { return this.m_bThumbnailFilled; }
		}

		public int BitsPerPixel
		{
			get { return this.m_HeaderInfo.nBitsPerPixel; }
		}

		public int GlobalAngle
		{
			get { return this.m_nGlobalAngle; }
		}

		public bool IsCopyrighted
		{
			get { return this.m_bCopyright; }
		}

		public IntPtr HBitmap
		{
			get { return this.m_hBitmap; }
		}

		public int Width
		{
			get { return this.m_HeaderInfo.nWidth; }
		}

		public int Height
		{
			get { return this.m_HeaderInfo.nHeight; }
		}

		public int XResolution
		{
			get { return this.m_ResolutionInfo.hRes; }
		}

		public int YResolution
		{
			get { return this.m_ResolutionInfo.vRes; }
		}

		public int Compression
		{
			get { return this.m_nCompression; }
		}
		#endregion
	};

}
