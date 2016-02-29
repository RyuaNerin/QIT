using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Threading;
using System.ComponentModel;
using Twitter;

namespace Quicx
{
    public partial class frmUpload : Form
    {
        public frmUpload()
        {
            InitializeComponent();

            this.ajax.Left = this.txtText.Left + this.txtText.Width / 2 - 16;
            this.ajax.Top = this.txtText.Top + this.txtText.Height / 2 - 16;

            this._tempPath = Path.Combine(Application.StartupPath, String.Format("{0}.{1}", Helper.CreateString(), (this._isJPG ? "jpg" : "png")));
        }

        public bool AutoStart { get; set; }

        private void frmUpload_FormClosed(object sender, FormClosedEventArgs e)
        {
            this._image.Dispose();

            File.Delete(this._tempPath);
        }

        bool b = true;
        public int Index { get; set; }

        private void frmUpload_Shown(object sender, EventArgs e)
        {
            try
            {
                if (this.b)
                {
                    if (Settings.isReversedCtrl ? !this.AutoStart : this.AutoStart)
                        this.Tweet();

                    this.b = false;
                }

                if (Settings.isUniformityText)
                    if (Index>0)
                        this.Tweet();
                    else
                        this.txtText.Text = "";

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

        private void frmUpload_Activated(object sender, EventArgs e)
        {
            try
            {
                this.txtText.Focus();
            }
            catch
            { }
        }

        //////////////////////////////////////////////////////////////////////////

        private void ShowPreview()
        {
            try
            {
                using (frmPreview frm = new frmPreview())
                {
                    frm.Text = String.Format("미리보기 {0} ({1} x {2})", (this._isJPG ? "JPG" : "PNG"), this._image.Width, this._image.Height);
                    frm.SetImage(this._image);
                    frm.ShowDialog(this);
                }
            }
            catch
            { }
        }

        //////////////////////////////////////////////////////////////////////////

        private const int MaxFileSize = (int)(2.5 * 1024 * 1024);

        private double _resizedRatio;
        private Image _image;
        private byte[] _rawData;
        private bool _isJPG;
        private bool _isPSD;
        private string _tempPath;

        public bool SetImage(DragDropInfo info)
        {
            try
            {
                switch (info.DataType)
                {
                    case DragDropInfo.DataTypes.String:
                        {
                            string path = info.GetString();

                            if (Path.GetExtension(path).ToLower() == ".psd")
                            {
                                this._isPSD = true;
                                this._isJPG = false;

                                SimplePsd.CPSD psd = new SimplePsd.CPSD();
                                psd.Load(path);

                                this._image = Image.FromHbitmap(psd.HBitmap);
                            }
                            else
                            {
                                this._isPSD = false;
                                this._isJPG = (Path.GetExtension(path).ToLower() == ".jpg");

                                this._image = Image.FromFile(path, true);
                            }

                            return this.ResizeImage(new FileInfo(path).Length);
                        }

                    case DragDropInfo.DataTypes.Image:
                        {
                            this._image = info.GetImage();

                            return this.ResizeImage(long.MaxValue);
                        }

                    case DragDropInfo.DataTypes.Stream:
                        {
                            Stream stream = info.GetStream();

                            this._image = Image.FromStream(stream);

                            return this.ResizeImage(stream.Length);
                        }
                }
            }
            catch
            { }

            return false;
        }

		public bool SetImage(Image image)
		{
			try
			{

				this._isJPG = true;
				this._isPSD = false;
				this._image = image;
				return this.ResizeImage( long.MaxValue );
			}
			catch
			{ }
			return false;
		}

        public void SetText(string str)
        {
            this.txtText.Text = str;
        }

        public string GetText()
        {
            return this.txtText.Text;
        }

        private bool ResizeImage(long origFileSize)
        {
            //////////////////////////////////////////////////////////////////////////

            long origPixels = this._image.Width * this._image.Height;
            long resizedPixels;

            this._isJPG = (this._image.RawFormat.Guid == ImageFormat.Jpeg.Guid);

            switch (Settings.ImageExt)
            {
                case 0:
                    if (origFileSize > MaxFileSize)
                        this._isJPG = true;
                    break;

                case 1:
                    this._isJPG = true;
                    break;

                case 2:
                    this._isJPG = false;
                    break;
            }

            //////////////////////////////////////////////////////////////////////////

            ImageCodecInfo codecInfo;
            EncoderParameters parameters;

            double d = 1.1d;

            if (!this._isPSD && this._isJPG)
            {
                codecInfo = GetEncoder(ImageFormat.Jpeg);
                parameters = new EncoderParameters(1);
                parameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 100L);
            }
            else if (!this._isPSD)
            {
                codecInfo = GetEncoder(ImageFormat.Png);
                parameters = new EncoderParameters(1);

                long depth = 24L;

                if (Settings.PNGTrans)
                {
                    switch (this._image.PixelFormat)
                    {
                        case PixelFormat.Format16bppArgb1555:
                        case PixelFormat.Format32bppArgb:
                        case PixelFormat.Format32bppPArgb:
                        case PixelFormat.Format64bppArgb:
                        case PixelFormat.Format64bppPArgb:
                            depth = 32L;
                            break;
                    }
                }

                parameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.ColorDepth, depth);

                d = d / (depth / 8);
            }
            else
            {
                codecInfo = GetEncoder(ImageFormat.Png);
                parameters = new EncoderParameters(1);
                parameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.ColorDepth, 32);
            }

            //////////////////////////////////////////////////////////////////////////

            try
            {
                // Make Thumbnail
                double scale = Math.Min((64.0d / this._image.Width), (64.0d / this._image.Height));
                this.picImage.Image =
                    this.ResizeBySize(
                        this._image,
                        (int)(scale * this._image.Width),
                        (int)(scale * this._image.Height),
                        true);

                if (origFileSize <= MaxFileSize)
                {
                    using (MemoryStream stmFile = new MemoryStream())
                    {
                        this._image.Save(stmFile, codecInfo, parameters);
                        this._rawData = stmFile.ToArray();
                        stmFile.Dispose();
                    }
                }
                else
                {
                    Image img;

                    do
                    {
                        img = this.ResizeByCapacity(this._image, this._isJPG, d);
                        this._image.Dispose();
                        this._image = img;

                        using (MemoryStream stmFile = new MemoryStream())
                        {
                            this._image.Save(stmFile, codecInfo, parameters);
                            this._rawData = stmFile.ToArray();
                            stmFile.Dispose();
                        }

                        d *= 0.9d;
                    }
                    while (this._rawData.Length > frmUpload.MaxFileSize);
                }

                resizedPixels = this._image.Width * this._image.Height;

                _resizedRatio = (100.0d * resizedPixels / origPixels);

                this.lblImageSize.Text =
                    String.Format(
                        "{0} {1} x {2} ({3:##0.0} %)",
                        (this._isJPG ? "JPG" : "PNG"),
                        this._image.Width,
                        this._image.Height,
                        _resizedRatio);

                return true;
            }
            catch
            {
                return false;
            }
        }

        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();

            foreach (ImageCodecInfo codec in codecs)
                if (codec.FormatID == format.Guid)
                    return codec;

            return null;
        }

        private Image ResizeByCapacity(Image img, bool isJPG, double c)
        {
            // Ox : Oy = Rx : Ry
            // Rx * Ry = r
            // Rw = sqrt(Ow * r / Oh)
            // Rh = sqrt(Oh * r / Ow)

            double d;

            if (isJPG)
                d = frmUpload.MaxFileSize * 2.6d * c;
            else
                d = frmUpload.MaxFileSize * 2.0d * c; // Pixel Depth

            int w = (int)Math.Ceiling(Math.Sqrt(d * img.Width / img.Height));
            int h = (int)Math.Ceiling(Math.Sqrt(d * img.Height / img.Width));

            if (w > img.Width || h > img.Height)
            {
                w = img.Width;
                h = img.Height;
            }

            return this.ResizeBySize(img, w, h, false);
        }

        private Image ResizeBySize(Image img, int width, int height, bool FillBackground)
        {
            PixelFormat pixelFormat = PixelFormat.Format24bppRgb;

            if (!this._isJPG && Settings.PNGTrans)
            {
                switch (img.PixelFormat)
                {
                    case PixelFormat.Format16bppArgb1555:
                    case PixelFormat.Format32bppArgb:
                    case PixelFormat.Format32bppPArgb:
                    case PixelFormat.Format64bppArgb:
                    case PixelFormat.Format64bppPArgb:
                        pixelFormat = PixelFormat.Format32bppArgb;
                        break;
                }
            }

            Image imgResize = new Bitmap(width, height, pixelFormat);

            using (Graphics g = Graphics.FromImage(imgResize))
            {
                foreach (PropertyItem propertyItem in this._image.PropertyItems)
                    imgResize.SetPropertyItem(propertyItem);

                if (FillBackground)
                    g.Clear(Color.White);

                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.SmoothingMode = SmoothingMode.HighQuality;

                g.DrawImage(img, 0, 0, width, height);
            }

            return imgResize;
        }

        //////////////////////////////////////////////////////////////////////////

        private bool isOver110 = false;
        private bool isOver120 = false;

        private void txtText_TextChanged(object sender, EventArgs e)
        {
            try
            {
                int len = this.txtText.Text.Replace("\r\n", "\n").Length;

                this.lblLength.Text = String.Format("{0} / 130", len);

                if (!this.isOver120 && len > 120)
                {
                    this.lblLength.ForeColor = this.txtText.ForeColor = Color.Red;
                    this.isOver120 = true;
                }
                else if (this.isOver120 && len <= 120)
                {
                    this.lblLength.ForeColor = this.txtText.ForeColor = SystemColors.WindowText;
                    this.isOver120 = false;
                }
                else if (!this.isOver120 && !this.isOver110 && len > 110)
                {
                    this.lblLength.ForeColor = this.txtText.ForeColor = Color.Brown;
                    this.isOver110 = true;
                }
                else if (!this.isOver120 && this.isOver110 && len <= 110)
                {
                    this.lblLength.ForeColor = this.txtText.ForeColor = SystemColors.WindowText;
                    this.isOver110 = false;
                }
            }
            catch
            { }
        }

        private void txtText_KeyDown(object sender, KeyEventArgs e)
        {
            bool isHandled = false;
            if (!e.Control && (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Return))
            {
                if (this.txtText.Text.Replace("\r\n", "\n").Length < 130)
                    this.Tweet();

                else
                    System.Media.SystemSounds.Exclamation.Play();

                isHandled = true;
            }
            else if (e.Control && e.KeyCode == Keys.A)
            {
                this.txtText.SelectAll();

                isHandled = true;
            }
            else if (e.Control && e.KeyCode == Keys.R)
            {
                this.ShowPreview();

                isHandled = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                this.Close();

                isHandled = true;
            }

            if (isHandled)
            {
                e.SuppressKeyPress = false;
                e.Handled = true;
            }
        }

        public void Tweet()
        {
            try
            {
                this.txtText.Enabled = false;
                this.ajax.Start();
                this.bgwTweet.RunWorkerAsync(this.txtText.Text.Replace("/N/", string.Format("{0}", this.Index+1)));
            }
            catch
            { }
        }

        private void bgwTweet_DoWork(object sender, DoWorkEventArgs e)
        {
            e.Result = false;

            try
            {
                byte[] buff;
                string boundary = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");

                using (MemoryStream stream = new MemoryStream())
                {
                    buff = Encoding.UTF8.GetBytes(
                        String.Format(
                            "--{0}\r\nContent-Disposition: form-data; name=\"status\"\r\n\r\n{1}\r\n--{0}\r\nContent-Type: application/octet-stream\r\nContent-Disposition: form-data; name=\"media[]\"; filename=\"img.{2}\"\r\n\r\n",
                            boundary,
                            (string)e.Argument,
                            (this._isJPG ? "jpg" : "png")));
                    stream.Write(buff, 0, buff.Length);

                    stream.Write(this._rawData, 0, this._rawData.Length);

                    buff = Encoding.UTF8.GetBytes(String.Format("\r\n\r\n--{0}--\r\n", boundary));
                    stream.Write(buff, 0, buff.Length);

                    buff = stream.ToArray();

                    stream.Flush();
                    stream.Close();
                    stream.Dispose();
                }

                //////////////////////////////////////////////////////////////////////////

                string URL, oauth_nonce, oauth_timestamp, hash_parameter, oauth_signature;

                URL = "https://api.twitter.com/1.1/statuses/update_with_media.json";

                oauth_nonce = TwitterAPI.UrlEncode(TwitterAPI.GetNonce());
                oauth_timestamp = TwitterAPI.UrlEncode(TwitterAPI.GenerateTimeStamp());

                hash_parameter = String.Format(
                    "oauth_consumer_key={0}&oauth_nonce={1}&oauth_signature_method=HMAC-SHA1&oauth_timestamp={2}&oauth_token={3}&oauth_version=1.0",
                    Settings.CKey,
                    oauth_nonce,
                    oauth_timestamp,
                    Settings.UToken
                );

                using (HMACSHA1 oCrypt = new HMACSHA1())
                {
                    oCrypt.Key = Encoding.UTF8.GetBytes(
                        string.Format(
                            "{0}&{1}",
                            TwitterAPI.UrlEncode(Settings.CSecret),
                            TwitterAPI.UrlEncode(Settings.USecret)
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
                    Settings.CKey,
                    Settings.UToken
                );

                //////////////////////////////////////////////////////////////////////////

                try
                {
                    using (WebClient oWeb = new WebClient())
                    {
                        oWeb.Encoding = Encoding.UTF8;
                        oWeb.Headers.Set("Authorization", hash_parameter);
                        oWeb.Headers.Set("Content-Type", "multipart/form-data; boundary=" + boundary);
                        oWeb.UploadData(new Uri(URL), "POST", buff);

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

        //////////////////////////////////////////////////////////////////////////

        private void picImage_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (!File.Exists(this._tempPath))
                    File.WriteAllBytes(this._tempPath, this._rawData);

                DataObject dataObject = new DataObject();
                dataObject.SetData(DataFormats.FileDrop, new string[] { this._tempPath });

                this.picImage.DoDragDrop(dataObject, DragDropEffects.All);
            }
            else if (e.Button == MouseButtons.Right)
            {
                this.ShowPreview();
            }
        }
    }
}
