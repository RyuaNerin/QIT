using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Net;

namespace QIT
{
	public class DragDropInfo : IDisposable
	{
		public enum DataTypes { None, String, Stream, Image }

		public static bool isAvailable(DragEventArgs e)
		{
			return
				(
					e.Data.GetDataPresent(DataFormats.FileDrop) ||
					e.Data.GetDataPresent(DataFormats.Bitmap) ||
					e.Data.GetDataPresent(DataFormats.Dib) ||
					(e.Data.GetDataPresent(DataFormats.EnhancedMetafile)  && e.Data.GetDataPresent(DataFormats.MetafilePict)) ||
					e.Data.GetDataPresent("HTML Format")
				);
		}
		public static bool Parse(DragEventArgs e, out DragDropInfo info)
		{
			if (DragDropInfo.isAvailable(e))
			{
				info = new DragDropInfo(e);
				return true;
			}
			else
			{
				info = null;
				return false;
			}
		}

		//////////////////////////////////////////////////////////////////////////

		private bool		_disposed = false;
		private DataTypes	_dataType;
		private object		_data;

		public DragDropInfo(string path)
		{
			this._dataType = DataTypes.String;
			this._data = path;
		}
		public DragDropInfo(Image image)
		{
			this._dataType = DataTypes.Image;
			this._data = image;
		}
		public DragDropInfo(DragEventArgs e)
		{
			// Images
			if (e.Data.GetDataPresent(DataFormats.Bitmap))
			{
				this._data = (Image)e.Data.GetData(DataFormats.Bitmap);
				this._dataType = DataTypes.Image;
			}

			// Specifies the Windows device-independent bitmap
			else if (e.Data.GetDataPresent(DataFormats.Dib))
			{
				this._data = (Image)e.Data.GetData(DataFormats.Dib, true);
				this._dataType = DataTypes.Image;
			}

			// In Program like MS Word
			else if (e.Data.GetDataPresent(DataFormats.EnhancedMetafile) &&
					 e.Data.GetDataPresent(DataFormats.MetafilePict))
			{
// 				using (Stream stream = (Stream)e.Data.GetData(DataFormats.MetafilePict))
// 				{
// 					stream.Seek(0, SeekOrigin.Begin);
// 
// 					Image image = new Metafile(stream);
// 
// 					this._data = image;
// 
// 					stream.Close();
// 					stream.Dispose();
// 				}
				this._data = (Image)e.Data.GetData(DataFormats.Bitmap);

				this._dataType = DataTypes.Image;
			}

			// HTML
			else if (e.Data.GetDataPresent("HTML Format"))
			{
				Uri uri;
				string html;
				string src;
				string srcScheme;

				html = (string)e.Data.GetData("HTML Format");

				src = DragDropInfo.regSrc.Match(html).Groups[1].Value;

				srcScheme = src.Substring(0, 8).ToLower();

				if (!srcScheme.StartsWith("http://") && !srcScheme.StartsWith("https://"))
				{
					int fragmentStart	= int.Parse(DragDropInfo.regFragmentStart.Match(html).Groups[1].Value);
					int fragmentEnd		= int.Parse(DragDropInfo.regFragmentEnd.Match(html).Groups[1].Value);

					string fragment = html.Substring(fragmentStart, fragmentEnd - fragmentStart);
					string baseUrl = DragDropInfo.regBaseUrl.Match(html).Groups[0].Value;

					uri = new Uri(new Uri(baseUrl), src);
				}
				else
				{
					uri = new Uri(src);
				}

				this._data = Image.FromStream(new WebClient().OpenRead(uri));
				this._dataType = DataTypes.Image;
			}

			else
			{

				this._dataType = DataTypes.None;
			}
		}
		private static RegexOptions regISC	= RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase;
		private static RegexOptions regMC	= RegexOptions.Compiled | RegexOptions.Multiline;
		private static Regex regSrc				= new Regex(@"<img.*?src=[""'](.*?)[""'].*>",	regISC);
		private static Regex regFragmentStart	= new Regex(@"^StartFragment:(\d+)",			regMC);
		private static Regex regFragmentEnd		= new Regex(@"^EndFragment:(\d+)",				regMC);
		private static Regex regBaseUrl			= new Regex(@"http://.*?/",						regMC);

		~DragDropInfo()
		{
			Dispose(false);
		}
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			if (!this._disposed)
			{
				this._disposed = true;

				if (disposing)
				{
					if (this._dataType == DataTypes.Image)
						(this._data as Image).Dispose();

					if (this._dataType == DataTypes.Stream)
						(this._data as Stream).Dispose();
				}
			}
		}

		public DataTypes DataType { get { return this._dataType; } }

		public string GetString()
		{
			return this._data as string;
		}
		public Stream GetStream()
		{
			return this._data as Stream;
		}
		public Image GetImage()
		{
			return this._data as Image;
		}


	}
}
