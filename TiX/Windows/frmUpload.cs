using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using TiX.Utilities;
using Twitter;

namespace TiX.Windows
{
    public partial class frmUpload : Form
    {
        private IList<DragDropInfo> m_infos;
        private int     m_index = -1;
        private double  m_ratio;
        private string  m_extension;
        private Image   m_image;
        private Image   m_imageThumbnail;
        private byte[]  m_rawData;

		internal string InReplyToStatusId { get; set; }

        //////////////////////////////////////////////////////////////////////////

        private frmUpload(bool mainWnd)
        {
            InitializeComponent();

            this.ajax.Left = this.txtText.Left + this.txtText.Width  / 2 - 16;
            this.ajax.Top  = this.txtText.Top  + this.txtText.Height / 2 - 16;

            this.ShowInTaskbar = mainWnd;
        }
        public frmUpload(IList<DragDropInfo> infos, bool mainWnd = false) : this(mainWnd)
        {
            this.m_infos = infos;
            
            this.Text = String.Format("{0} (1 / {1})", Program.ProductName, this.m_infos.Count);
        }
        public frmUpload(DragDropInfo info, bool mainWnd = false) : this(mainWnd)
        {
            this.m_infos = new List<DragDropInfo>();
            this.m_infos.Add(info);

            this.Text = String.Format("{0} (1 / {1})", Program.ProductName, this.m_infos.Count);
        }

        public bool AutoStart { get; set; }
        public string TweetString
        {
            get { return this.txtText.Text; }
            set { this.txtText.Text = value; }
        }

        private void frmUpload_Shown(object sender, EventArgs e)
        {
            if (frmMain.Instance != null)
            {
                this.Left = frmMain.Instance.Left + (frmMain.Instance.Width  - this.Width)  / 2;
                this.Top  = frmMain.Instance.Top  + (frmMain.Instance.Height - this.Height) / 2;
            }

            StartNew();
        }

        private void StartNew()
        {
            Clear();

            if (++this.m_index >= this.m_infos.Count)
            {
                this.Close();
                return;
            }

            this.Text = String.Format("{0} ({1} / {2})", Program.ProductName, this.m_index + 1, this.m_infos.Count);

            this.txtText.Enabled = false;
            this.ajax.Start();
            this.bgwResize.RunWorkerAsync();
        }
        private void Clear()
        {
            this.m_rawData = null;

            if (this.m_image != null)
                this.m_image.Dispose();

            if (this.m_imageThumbnail != null)
                this.m_imageThumbnail.Dispose();

            this.lblImageSize.Text = string.Empty;
            this.picImage.Image = null;
        }

        private void bgwResize_DoWork(object sender, DoWorkEventArgs e)
        {
            this.m_image = this.m_infos[this.m_index].GetImage();
            if (this.m_image == null) return;

            ImageResize.ResizeImage(ref this.m_image, ref this.m_rawData, ref this.m_ratio, ref this.m_extension);
         
            //////////////////////////////////////////////////
            // Thumbnail
            var ratio = Math.Min(64d / this.m_image.Width, 64d / this.m_image.Height);
            var newWidth  = (int)(this.m_image.Width  * ratio);
            var newHeight = (int)(this.m_image.Height * ratio);

            var newImage = new Bitmap(newWidth, newHeight);

            using (var graphics = Graphics.FromImage(newImage))
                graphics.DrawImage(this.m_image, 0, 0, newWidth, newHeight);

            this.m_imageThumbnail = newImage;
            //////////////////////////////////////////////////

            e.Result = 0;
        }

        private void bgwResize_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Result == null)
            {
                this.Clear();
                this.StartNew();
            }
            else
            {
                this.ajax.Stop();
                this.txtText.Enabled = true;

                this.picImage.Image = this.m_imageThumbnail;

                this.lblImageSize.Text =
                    String.Format(
                        "{0} {1} x {2} ({3:##0.0} %)",
                        this.m_extension.ToUpper(),
                        this.m_image.Width,
                        this.m_image.Height,
                        this.m_ratio);

                if (this.AutoStart && (!Settings.UniformityText || this.m_index > 0))
                {
                    // 자동 트윗이거나,
                    // 내용 통일이 아니거나,
                    // 내용 통일이고 index 가 0 이 아닐때 자동트윗
                    this.Tweet();
                }
                else if (Settings.UniformityText && this.m_index > 0)
                    this.Tweet();

                this.txtText.Focus();
            }
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
                    frm.Text = String.Format("미리보기 {0} ({1} x {2})", (this.m_extension.ToUpper()), this.m_image.Width, this.m_image.Height);
                    frm.SetImage(this.m_image);
                    frm.ShowDialog(this);
                }
            }
            catch
            { }
        }

        private void frmUpload_FormClosed(object sender, FormClosedEventArgs e)
        {
            Clear();
            this.Dispose();
        }

        //////////////////////////////////////////////////////////////////////////

        private bool m_isOver80p = false;
        private bool m_isOver90p = false;
        private bool m_tweetable = true;

        private static Regex m_regex = new Regex("/^(https?:\\/\\/)?([\\da-z\\.-]+)\\.([a-z\\.]{2,6})([\\/\\w \\.-]*)*\\/?$/", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private void txtText_TextChanged(object sender, EventArgs e)
        {
            try
            {
                var len = m_regex.Replace(this.txtText.Text.Replace("\r\n", "\n"), "12345678901234567890123").Length;

                this.lblLength.Text = String.Format("{0} / 117", len);
                this.m_tweetable = len <= 117;

                if (!this.m_isOver90p && len > 105)
                {
                    this.lblLength.ForeColor = this.txtText.ForeColor = Color.Red;
                    this.m_isOver90p = true;
                }
                else if (this.m_isOver90p && len <= 105)
                {
                    this.lblLength.ForeColor = this.txtText.ForeColor = SystemColors.WindowText;
                    this.m_isOver90p = false;
                }
                else if (!this.m_isOver90p && !this.m_isOver80p && len > 93)
                {
                    this.lblLength.ForeColor = this.txtText.ForeColor = Color.Brown;
                    this.m_isOver80p = true;
                }
                else if (!this.m_isOver90p && this.m_isOver80p && len <= 93)
                {
                    this.lblLength.ForeColor = this.txtText.ForeColor = SystemColors.WindowText;
                    this.m_isOver80p = false;
                }
            }
            catch
            { }
        }

        private bool m_handledText = false;
        private void txtText_KeyDown(object sender, KeyEventArgs e)
        {
            bool isHandled = false;
            if (!e.Control && (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Return))
            {
                if (this.m_tweetable)
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
                this.StartNew();

                isHandled = true;
            }

            m_handledText = isHandled;
//             if (isHandled)
//             {
//                 e.SuppressKeyPress = false;
//                 e.Handled = true;
//             }
        }
        private void txtText_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (m_handledText == true)
                e.Handled = true;
        }

        public void Tweet()
        {
            this.txtText.Enabled = false;
            this.ajax.Start();
            this.bgwTweet.RunWorkerAsync(this.txtText.Text.Replace("/N/", string.Format("{0}", this.m_index + 1)));
        }

        private void bgwTweet_DoWork(object sender, DoWorkEventArgs e)
        {
            e.Result = false;

            try
            {
                string boundary  = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
                string boundary2 = "--" + boundary;

                //////////////////////////////////////////////////

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

                var req = WebRequest.Create(URL) as HttpWebRequest;
                req.ContentType = "multipart/form-data; charset=utf-8; boundary=" + boundary;
                req.Headers.Set("Authorization", hash_parameter);
                req.Method = "POST";

                var stream = req.GetRequestStream();
                using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true, NewLine = "\r\n" })
				{
					writer.WriteLine( boundary2 );
					writer.WriteLine( "Content-Disposition: form-data; name=\"status\"" );
					writer.WriteLine( );
					writer.WriteLine( e.Argument as string );

					if(!string.IsNullOrEmpty(InReplyToStatusId))
					{
						writer.WriteLine( boundary2 );
						writer.WriteLine( "Content-Disposition: form-data; name=\"in_reply_to_status_id\"" );
						writer.WriteLine( );
						writer.WriteLine(InReplyToStatusId);
					}

					writer.WriteLine(boundary2);
                    writer.WriteLine("Content-Type: application/octet-stream");
                    writer.WriteLine("Content-Disposition: form-data; name=\"media[]\"; filename=\"img.{0}\"", this.m_extension);
                    writer.WriteLine();
                    stream.Write(this.m_rawData, 0, this.m_rawData.Length);
                    writer.WriteLine();

                    writer.WriteLine(boundary2 + "--");
                }

                using (var res = req.GetResponse())
                { }

                e.Result = true;
            }
            catch
            {
            }
        }

        private void bgwTweet_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.ajax.Stop();

            if ((bool)e.Result == false)
            {
                this.txtText.Enabled = true;
                this.txtText.Focus();
            }
            else
            {
                this.StartNew();
            }
        }

        //////////////////////////////////////////////////////////////////////////

        private void picImage_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                var temp = Path.GetTempFileName();
                temp = Path.ChangeExtension(temp, "." + this.m_extension);
                File.WriteAllBytes(temp, this.m_rawData);

                DataObject dataObject = new DataObject();
                dataObject.SetData(DataFormats.FileDrop, new string[] { temp });

                this.picImage.DoDragDrop(dataObject, DragDropEffects.All);
            }
            else if (e.Button == MouseButtons.Right)
            {
                this.ShowPreview();
            }
        }
    }
}
