// http://www.codeproject.com/csharp/simplepsd.asp

using System;
using System.Drawing;
using System.IO;
using System.Linq;

namespace SimplePsd
{
	/// <summary>
	/// Main class is for opening Adobe Photoshop files
	/// </summary>
	public sealed class CPSD : IDisposable
    {
        private short m_channels;
        private int m_width;
        private int m_height;
        private short m_bpp;
        private short m_colorMode;

        private int m_colorLength;
        private byte[] m_colorData;

        private short n_heightRes = 96;
        private short n_widthRes = 96;

        private short   m_nColourCount = -1;
		private int		m_nCompression = -1;
		private	IntPtr  m_hBitmap = IntPtr.Zero;

        public CPSD(string strPathName)
        {
            using (var file = File.OpenRead(strPathName))
            using (var reader = new BigEndianBinaryReader(file))
                this.Load(reader);
        }
        public CPSD(Stream stream)
        {
            using (var reader = new BigEndianBinaryReader(stream))
                this.Load(reader);
        }

        public void Dispose()
        {
            NativeMethods.DeleteObject(this.m_hBitmap);
            GC.SuppressFinalize(this);
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
			//byte [] Reserved   = stream.ReadBytes(6);
            stream.ReadBytes(6);

			if (Enumerable.SequenceEqual(Signature, Signature_8BPS))
			{
				if (Version[1] == 0x01)
				{
					this.m_channels = stream.ReadInt16();
					this.m_height = stream.ReadInt32();
					this.m_width = stream.ReadInt32();
					this.m_bpp = stream.ReadInt16();
					this.m_colorMode = stream.ReadInt16();
				}
			}
		}

		private void ReadColourModeData(BigEndianBinaryReader stream)
		{
			this.m_colorLength = stream.ReadInt32();

			if (this.m_colorLength > 0)
				this.m_colorData = stream.ReadBytes(this.m_colorLength);
		}

		private static readonly byte[] Signature_8BIM = { 0x38, 0x42, 0x49, 0x4D };
		private void ReadImageResource(BigEndianBinaryReader stream)
		{
			int nTotalBytes = stream.ReadInt32();

			stream.BytesRead = 0;

            short nId;
            int nSize;

			while ((stream.BaseStream.Position < stream.BaseStream.Length) && (stream.BytesRead < nTotalBytes))
			{
				if (Enumerable.SequenceEqual(stream.ReadBytes(4), Signature_8BIM))
				{
					nId = stream.ReadInt16();

					byte SizeOfName = stream.ReadByte();
						
					int nSizeOfName = (int)SizeOfName;
					if(nSizeOfName > 0)
					{
						if((nSizeOfName % 2) != 0)
							SizeOfName = stream.ReadByte();

                        // Name
                        stream.Move(nSizeOfName);
					}

					//SizeOfName = stream.ReadByte();
					stream.Move(1);

					nSize = stream.ReadInt32();

					if ((nSize % 2) != 0)
						nSize++;

					if (nSize > 0)
					{
						switch(nId)
						{
							case 1005:
								this.n_widthRes = stream.ReadInt16();
                                stream.Move(6);
								this.n_heightRes = stream.ReadInt16();
                                stream.Move(6);
								break;

							case 1046:
								this.m_nColourCount = stream.ReadInt16();
								break;

							default:
								stream.Move(nSize);
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
						int nWidth = this.m_width;
						int nHeight = this.m_height;
						int bytesPerPixelPerChannel = this.m_bpp / 8;
						
						int nPixels = nWidth * nHeight;
						int nTotalBytes = nPixels * bytesPerPixelPerChannel * this.m_channels;

						byte [] pData = new byte[nTotalBytes];
						byte [] pImageData = new byte[nTotalBytes];
						
						#region switch(.nColourMode)
						switch(this.m_colorMode)
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
									nTotalBytes = nPixels * nBytesToReadPerPixelPerChannel * this.m_channels;
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
							int ppm_x = (int)Math.Ceiling(this.n_widthRes  * 10000 / 254d);
                            int ppm_y = (int)Math.Ceiling(this.n_heightRes * 10000 / 254d);

							switch(this.m_bpp)
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
						int nWidth = this.m_width;
						int nHeight = this.m_height;
						int bytesPerPixelPerChannel = this.m_bpp / 8;
						
						int nPixels = nWidth * nHeight;
						int nTotalBytes = nPixels * bytesPerPixelPerChannel * this.m_channels;

						byte [] pDest = new byte[nTotalBytes];
						byte [] pData = new byte[nTotalBytes];
						for(long i = 0; i < nTotalBytes; ++i) pData[i] = 254;
						for(long i = 0; i < nTotalBytes; ++i) pDest[i] = 254;

						byte ByteValue = 0x00;

						int Count = 0;

						int nPointer = 0;

						// The RLE-compressed data is proceeded by a 2-byte data count for each row in the data,
						// which we're going to just skip.
						stream.Position += nHeight * this.m_channels * 2;


						for(int channel=0; channel< this.m_channels; ++channel)
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

						for(int nColour = 0; nColour < this.m_channels; ++nColour)
						{
							nPixelCounter = nColour * bytesPerPixelPerChannel;
							for(int nPos = 0; nPos < nPixels; ++nPos)
							{
								for(int j = 0; j < bytesPerPixelPerChannel; ++j)
									pDest[nPixelCounter + j] = pData[nPointer + j];

								nPointer++;

								nPixelCounter += this.m_channels * bytesPerPixelPerChannel;
							}
						}

						for(int i = 0; i < nTotalBytes; i++)
							pData[i] = pDest[i];

                        int ppm_x = (int)Math.Ceiling(this.n_widthRes  * 10000 / 254d);
                        int ppm_y = (int)Math.Ceiling(this.n_heightRes * 10000 / 254d);

						switch (this.m_bpp)
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
			IntPtr hDC = NativeMethods.GetDC(IntPtr.Zero);
		
			if(hDC.Equals(IntPtr.Zero)) return;

			IntPtr pvBits = IntPtr.Zero;
			NativeMethods.BITMAPINFO BitmapInfo = new NativeMethods.BITMAPINFO();
			BitmapInfo.bmiHeader = new NativeMethods.BITMAPINFOHEADER();

            BitmapInfo.bmiHeader.Init();
			BitmapInfo.bmiHeader.biWidth              = cx;
			BitmapInfo.bmiHeader.biHeight             = cy;
			BitmapInfo.bmiHeader.biPlanes             = 1;
			BitmapInfo.bmiHeader.biBitCount           = (ushort)BitCount;
			BitmapInfo.bmiHeader.biCompression        = NativeMethods.BI_RGB;
			BitmapInfo.bmiHeader.biSizeImage          = 0;
			BitmapInfo.bmiHeader.biXPelsPerMeter      = ppm_x; 
			BitmapInfo.bmiHeader.biYPelsPerMeter      = ppm_y;
			BitmapInfo.bmiHeader.biClrImportant       = 0;
			BitmapInfo.bmiHeader.biClrUsed            = 0;

			this.m_hBitmap = NativeMethods.CreateDIBSection(hDC, ref BitmapInfo, 0, pvBits, IntPtr.Zero, 0);

			if(this.m_hBitmap.Equals(IntPtr.Zero))  
			{
				NativeMethods.ReleaseDC(IntPtr.Zero, hDC);
				return;
			}
			else
			{
				IntPtr hdcMemory = NativeMethods.CreateCompatibleDC(hDC);
				IntPtr hbmpOld = NativeMethods.SelectObject( hdcMemory, this.m_hBitmap );
				NativeMethods.RECT rc;
	
				rc.left = rc.top = 0;
				rc.right = cx;
				rc.bottom = cy;
				IntPtr hBrush = NativeMethods.GetStockObject(NativeMethods.WHITE_BRUSH);
				NativeMethods.FillRect(hdcMemory, ref rc, hBrush);
				NativeMethods.SelectObject(hdcMemory, hbmpOld);
				NativeMethods.DeleteDC(hdcMemory);
			}

			NativeMethods.ReleaseDC(IntPtr.Zero, hDC);
		}

		private void ProccessBuffer(byte[] pData)
		{
			if(this.m_hBitmap.Equals(IntPtr.Zero)) return;

			IntPtr hBitmap = this.m_hBitmap;

			IntPtr hdcMemory = IntPtr.Zero;
			IntPtr hbmpOld = IntPtr.Zero;

			short bytesPerPixelPerChannel = (short)(this.m_bpp / 8);
			int nPixels = this.m_width * this.m_height;
			int nTotalBytes = nPixels * bytesPerPixelPerChannel * this.m_channels;
			
			switch (this.m_colorMode)
			{
				case 1:		// Grayscale
				case 8:		// Duotone
					#region
					{
						hdcMemory = NativeMethods.CreateCompatibleDC(IntPtr.Zero);
						hbmpOld = NativeMethods.SelectObject(hdcMemory, hBitmap);

						int nCounter = 0;
						int nValue = 0;
						int nColor = 0;


						byte[] ColorValue = new byte[64];

						for(int nRow = 0; nRow < this.m_height; ++nRow)
						{
							for(int nCol = 0; nCol < this.m_width; ++nCol)
							{
								for(int i=0;i<bytesPerPixelPerChannel;i++)
									ColorValue[i] = pData[nCounter+i];

								BigEndianBinaryReader.SwapBytes(ColorValue, bytesPerPixelPerChannel);
							
								nValue = BitConverter.ToInt32(ColorValue,0);
								if(this.m_bpp == 16)
									nValue = nValue / 256;

								if(nValue > 255) nValue = 255;
								else if(nValue < 0) nValue = 0;

								nColor = ColorTranslator.ToWin32(Color.FromArgb(nValue, nValue, nValue));
								NativeMethods.SetPixel(hdcMemory, nCol, nRow, nColor);
							
								nCounter += bytesPerPixelPerChannel;
							}
						}
						NativeMethods.SelectObject(hdcMemory, hbmpOld);
						NativeMethods.DeleteDC(hdcMemory);
					}
					#endregion
					break;

				case 2: // Indexed
					#region
					{
						hdcMemory = NativeMethods.CreateCompatibleDC(IntPtr.Zero);
						hbmpOld = NativeMethods.SelectObject(hdcMemory, hBitmap);
						// pData holds the indices of loop through the palette and set the correct RGB
						// 8bpp are supported
						if(this.m_colorLength==768 && this.m_nColourCount>0)
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
								nRed = (int)this.m_colorData[nIndex];
								nGreen = (int)this.m_colorData[nIndex + 256];
								nBlue = (int)this.m_colorData[nIndex + 2 * 256];

								nColor = ColorTranslator.ToWin32(Color.FromArgb(nRed, nGreen, nBlue));
								NativeMethods.SetPixel(hdcMemory, nCol, nRow, nColor);
								nCol++;
								if(this.m_width <= nCol)
								{
									nCol = 0;
									nRow++;
								}
							}
						}

						NativeMethods.SelectObject(hdcMemory, hbmpOld);
						NativeMethods.DeleteDC(hdcMemory);
					}
					#endregion
					break;

				case 3:	// RGB
					#region
					{
						hdcMemory = NativeMethods.CreateCompatibleDC(IntPtr.Zero);
						hbmpOld = NativeMethods.SelectObject(hdcMemory, hBitmap);

						int nBytesToRead = this.m_bpp / 8;
						if (nBytesToRead == 2)
							nBytesToRead = 1;

						int nRow = 0;
						int nCol = 0;
						int nRed = 0;
						int nGreen = 0;
						int nBlue = 0;
						int nColor = 0;
						byte [] ColorValue = new byte[8];

						for (int nCounter = 0; nCounter < nTotalBytes; nCounter += this.m_channels * nBytesToRead)
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
							NativeMethods.SetPixel(hdcMemory, nCol, nRow, nColor);
							nCol++;
							if (this.m_width <= nCol)
							{
								nCol = 0;
								nRow++;
							}
						}

						NativeMethods.SelectObject(hdcMemory, hbmpOld);
						NativeMethods.DeleteDC(hdcMemory);
					}
					#endregion
					break;

				case 4:	// CMYK
					#region
					{
						hdcMemory = NativeMethods.CreateCompatibleDC(IntPtr.Zero);
						hbmpOld = NativeMethods.SelectObject(hdcMemory, hBitmap);

						double C, M, Y, K;
						double exC, exM, exY, exK;

						int nRow = 0;
						int nCol = 0;
						int nColor = 0;

						byte [] ColorValue = new byte[8];

						double dMaxColours = Math.Pow(2, this.m_bpp);

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
							NativeMethods.SetPixel(hdcMemory, nCol, nRow, nColor);
							nCol++;
							if (this.m_width <= nCol)
							{
								nCol = 0;
								nRow++;
							}
						}

						NativeMethods.SelectObject(hdcMemory, hbmpOld);
						NativeMethods.DeleteDC(hdcMemory);
					}
					#endregion
					break;

				case 7:		// Multichannel
					#region
					{
						hdcMemory = NativeMethods.CreateCompatibleDC(IntPtr.Zero);
						hbmpOld = NativeMethods.SelectObject(hdcMemory, hBitmap);

						double C, M, Y, K;
						double exC, exM, exY, exK;

						int nRow = 0;
						int nCol = 0;
						int nColor = 0;

						byte [] ColorValue = new byte[8];

						double dMaxColours = Math.Pow(2, this.m_bpp);

						Color crPixel = Color.White;

						// assume format is in either CMY or CMYK
						if(this.m_channels >= 3)
						{
							for(int nCounter = 0; nCounter < nTotalBytes; nCounter += this.m_channels * bytesPerPixelPerChannel)
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
							
								if(this.m_channels == 4)
								{
									Array.Copy(pData,nCounter+3*bytesPerPixelPerChannel, ColorValue, 0, bytesPerPixelPerChannel);
									BigEndianBinaryReader.SwapBytes(ColorValue, bytesPerPixelPerChannel);
									exK = (double)BitConverter.ToUInt32(ColorValue, 0);

									K = (1.0 - exK / dMaxColours);
								}

								crPixel = CMYKToRGB(C, M, Y, K);

								nColor = ColorTranslator.ToWin32(crPixel);
								NativeMethods.SetPixel(hdcMemory, nCol, nRow, nColor);
								nCol++;
								if (this.m_width <= nCol)
								{
									nCol = 0;
									nRow++;
								}
							}
						}

						NativeMethods.SelectObject(hdcMemory, hbmpOld);
						NativeMethods.DeleteDC(hdcMemory);
					}
					#endregion
					break;

				case 9:	// Lab
					#region
					{
						hdcMemory = NativeMethods.CreateCompatibleDC(IntPtr.Zero);
						hbmpOld = NativeMethods.SelectObject(hdcMemory, hBitmap);

						int L, a, b;

						int nRow = 0;
						int nCol = 0;
						int nColor = 0;

						byte [] ColorValue = new byte[64];

						double exL, exA, exB;
						double L_coef, a_coef, b_coef;
						double dMaxColours = Math.Pow(2, this.m_bpp);

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
							NativeMethods.SetPixel(hdcMemory, nCol, nRow, nColor);
							nCol++;
							if(this.m_width <= nCol)
							{
								nCol = 0;
								nRow++;
							}
						}

						NativeMethods.SelectObject(hdcMemory, hbmpOld);
						NativeMethods.DeleteDC(hdcMemory);
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

		public IntPtr HBitmap
		{
			get { return this.m_hBitmap; }
		}
		#endregion
	};

}
