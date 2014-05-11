using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using Twitter;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;

namespace QIT
{
	public partial class frmUpload : Form
	{
		public frmUpload()
		{
			InitializeComponent();
		}

		protected override void OnFormClosed(FormClosedEventArgs e)
		{
			base.OnFormClosed(e);

			this._img.Dispose();
		}

		private void frmUpload_Load(object sender, EventArgs e)
		{
			try
			{
				this.ajax.Left = this.txtText.Left + this.txtText.Width / 2 - 16;
				this.ajax.Top = this.txtText.Top + this.txtText.Height / 2 - 16;

				if (this.AutoStart)
					this.Tweet();
			}
			catch
			{ }
		}

		private void picImage_Click(object sender, EventArgs e)
		{
			try
			{
				using (frmPreview frm = new frmPreview())
				{
					frm.SetImage(this._img);
					frm.ShowDialog(this);
				}
			}
			catch
			{ }
		}

		//////////////////////////////////////////////////////////////////////////

		public bool AutoStart { get; set; }

		private int		_filesize = (int)(2.8 * 1024 * 1024);
		private string	_path;
		private Image	_img;

		public void SetImage(string path)
		{
			this._path = path;
			this._img = Image.FromFile(path);

			// Resize
			double scale = Math.Min(64.0d / this._img.Width, 64.0d / this._img.Height);
			this.picImage.Image = this.ImageResize(this._img, (int)(this._img.Width * scale), (int)(this._img.Height * scale));

			if (new FileInfo(path).Length > _filesize)
				this._img = this.ImageResize(this._img);
		}

		private Image ImageResize(Image img)
		{
			// Ox : Oy = Rx : Ry
			// Rx * Ry = r * s
			// r = 2.6 : 1

			// Rw = sqrt(Ow * r * s / Oh)
			// Rh = sqrt(Oh * r * s / Ow)

			double w = Math.Sqrt(img.Width * 2.6d * _filesize / img.Height);
			double h = Math.Sqrt(img.Height * 2.6d * _filesize / img.Width);

			return this.ImageResize(img, (int)w, (int)h);
		}
		private Image ImageResize(Image img, int width, int height)
		{
			Image imgResize = new Bitmap(width, height, img.PixelFormat);

			using (Graphics g = Graphics.FromImage(imgResize))
			{
				g.InterpolationMode = InterpolationMode.HighQualityBicubic;
				g.PixelOffsetMode = PixelOffsetMode.HighQuality;
				g.SmoothingMode = SmoothingMode.HighQuality;

				g.DrawImage(img, 0, 0, width, height);
			}

			return imgResize;
		}

		//////////////////////////////////////////////////////////////////////////

		private void frmUpload_Shown(object sender, EventArgs e)
		{
			try
			{
				this.txtText.Focus();
			}
			catch
			{ }
		}

		private void frmUpload_Activated(object sender, EventArgs e)
		{
			try
			{
				this.txtText.Focus();
			}
			catch
			{ }
		}

		private void frmUpload_Enter(object sender, EventArgs e)
		{
			try
			{
				this.txtText.Focus();
			}
			catch
			{ }
		}

		private bool isOver120 = false;
		private bool isOver130 = false;

		private void txtText_TextChanged(object sender, EventArgs e)
		{
			try
			{
				int len = this.txtText.Text.Replace("\r\n", "\n").Length;

				this.lblLength.Text = String.Format("{0} / 130", len);

				if (!this.isOver130 && len >= 130)
				{
					this.lblLength.ForeColor = this.txtText.ForeColor = Color.Red;
					this.isOver130 = true;
				}
				else if (this.isOver130 && len < 130)
				{
					this.lblLength.ForeColor = this.txtText.ForeColor = SystemColors.WindowText;
					this.isOver130 = false;
				}

				if (!this.isOver130 && !this.isOver120 && len >= 120)
				{
					this.lblLength.ForeColor = this.txtText.ForeColor = Color.Brown;
					this.isOver120 = true;
				}
				else if (!this.isOver130 && this.isOver120 && len < 120)
				{
					this.lblLength.ForeColor = this.txtText.ForeColor = SystemColors.WindowText;
					this.isOver120 = false;
				}
			}
			catch
			{ }
		}

		private void txtText_KeyDown(object sender, KeyEventArgs e)
		{
			try
			{
				if (!e.Control && e.KeyData == Keys.Enter)
				{
					if (this.txtText.Text.Replace("\r\n", "\n").Length < 130)
					{
						e.Handled = true;

						this.Tweet();
					}
					else
					{
						System.Media.SystemSounds.Exclamation.Play();
					}
				}
			}
			catch
			{ }
		}

		public void Tweet()
		{
			try
			{
				this.txtText.Enabled = false;
				this.ajax.Start();
				this.bgwTweet.RunWorkerAsync(this.txtText.Text);
			}
			catch
			{ }
		}

		private void bgwTweet_DoWork(object sender, DoWorkEventArgs e)
		{
			try
			{
				byte[] buff;
				string boundary = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");

				using (MemoryStream sBinary = new MemoryStream())
				{
					buff = Encoding.UTF8.GetBytes(
						String.Format(
						"--{0}\r\nContent-Disposition: form-data; name=\"status\"\r\n\r\n{1}\r\n--{0}\r\nContent-Type: application/octet-stream\r\nContent-Disposition: form-data; name=\"media[]\"; filename=\"{2}\"\r\n\r\n",
						boundary,
						(string)e.Argument,
						Path.GetFileName(this._path))
					);
					sBinary.Write(buff, 0, buff.Length);

					using (MemoryStream stmFile = new MemoryStream())
					{
						ImageCodecInfo codecInfo = GetEncoder(ImageFormat.Jpeg);

						EncoderParameters parameters = new EncoderParameters(1);
						parameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 100L);

						this._img.Save(stmFile, codecInfo, parameters);

						buff = stmFile.ToArray();
						sBinary.Write(buff, 0, buff.Length);
					}

					buff = Encoding.UTF8.GetBytes(String.Format("\r\n\r\n--{0}--\r\n", boundary));
					sBinary.Write(buff, 0, buff.Length);

					buff = sBinary.ToArray();

					sBinary.Close();
					sBinary.Dispose();
				}

				//////////////////////////////////////////////////////////////////////////

				string URL, oauth_nonce, oauth_timestamp, hash_parameter, oauth_signature;

				URL = "https://api.twitter.com/1.1/statuses/update_with_media.json";

				oauth_nonce = TwitterAPI.UrlEncode(TwitterAPI.GetNonce());
				oauth_timestamp = TwitterAPI.UrlEncode(TwitterAPI.GenerateTimeStamp());

				hash_parameter = String.Format(
					"oauth_consumer_key={0}&oauth_nonce={1}&oauth_signature_method=HMAC-SHA1&oauth_timestamp={2}&oauth_token={3}&oauth_version=1.0",
					Program.CKey,
					oauth_nonce,
					oauth_timestamp,
					Program.UToken
				);

				using (HMACSHA1 oCrypt = new HMACSHA1())
				{
					oCrypt.Key = Encoding.UTF8.GetBytes(
						string.Format(
							"{0}&{1}",
							TwitterAPI.UrlEncode(Program.CSecret),
							TwitterAPI.UrlEncode(Program.USecret)
						)
					);

					oauth_signature = Convert.ToBase64String(
						oCrypt.ComputeHash(
							Encoding.UTF8.GetBytes(
								String.Format(
									"POST&{0}&{1}",
									TwitterAPI.UrlEncode(URL),
									TwitterAPI.UrlEncode(hash_parameter)
								)
							)
						)
					);

					oCrypt.Clear();
				}

				hash_parameter = String.Format(
					"OAuth oauth_signature=\"{0}\",oauth_nonce=\"{1}\",oauth_timestamp=\"{2}\",oauth_consumer_key=\"{3}\",oauth_token=\"{4}\",oauth_version=\"1.0\",oauth_signature_method=\"HMAC-SHA1\"",
					TwitterAPI.UrlEncode(oauth_signature),
					oauth_nonce,
					oauth_timestamp,
					Program.CKey,
					Program.UToken
				);

				//////////////////////////////////////////////////////////////////////////

				try
				{
					using (WebClient oWeb = new WebClient())
					{
						oWeb.Encoding = Encoding.UTF8;
						oWeb.Headers.Set("Authorization", hash_parameter);
						oWeb.Headers.Set("Content-Type", "multipart/form-data; boundary=" + boundary);
						oWeb.UploadData(URL, "POST", buff);

						e.Result = true;
					}
				}
				catch
				{ }
			}
			catch
			{ }
		}

		private void bgwTweet_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			try
			{
				this.ajax.Stop();
				bool b = (bool)e.Result;

				if (b)
				{
					this.Close();
				}
				else
				{
					this.txtText.Enabled = true;
					this.txtText.Focus();
				}
			}
			catch
			{ }
		}

		private ImageCodecInfo GetEncoder(ImageFormat format)
		{
			ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();

			foreach (ImageCodecInfo codec in codecs)
				if (codec.FormatID == format.Guid)
					return codec;

			return null;
		}
	}
}
