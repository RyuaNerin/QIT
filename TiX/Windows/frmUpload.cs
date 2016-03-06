using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using TiX.Core;
using TiX.Utilities;

namespace TiX.Windows
{
    public partial class frmUpload : Form
    {
        private ImageCollection m_ic;
        private int     m_index = -1;
        private float   m_ratio;
        private string  m_extension;
        private Image   m_image;
        private Image   m_imageThumbnail;
        private MemoryStream  m_rawData;

		internal string InReplyToStatusId { get; set; }

        //////////////////////////////////////////////////////////////////////////

        public frmUpload(ImageCollection ic, bool mainWnd = false)
        {
            InitializeComponent();

            this.ajax.Left = this.txtText.Left + this.txtText.Width  / 2 - 16;
            this.ajax.Top  = this.txtText.Top  + this.txtText.Height / 2 - 16;

            this.ShowInTaskbar = mainWnd;

            this.m_ic = ic;
            
            this.Text = String.Format("{0} (1 / {1})", Program.ProductName, ic.Count);
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
            else
            {
                var screen = Screen.FromHandle(this.Handle).Bounds;

                this.Left = screen.Left + (screen.Width  - this.Width)  / 2;
                this.Top  = screen.Top  + (screen.Height - this.Height) / 2;
            }

            this.m_ic.GetImage();
            
            StartNew();
        }

        private void StartNew()
        {
            Clear();

            if (++this.m_index >= this.m_ic.Count)
            {
                this.Close();
                return;
            }

            try
            {
                if (string.IsNullOrEmpty(this.TweetString))
                {
                    this.txtText.Text = this.TweetString;
                }
                else if (!Settings.UniformityText)
                {
                    this.txtText.Text = "";
                }

                this.Text = String.Format("{0} ({1} / {2})", Program.ProductName, this.m_index + 1, this.m_ic.Count);

                this.txtText.Enabled = false;
                this.ajax.Start();
                this.bgwResize.RunWorkerAsync(this.m_index);
            }
            catch
            {
            }
        }
        private void Clear()
        {
            this.m_rawData = null;

            if (this.m_image != null)
            {
                this.m_image.Dispose();
                this.m_image = null;
            }

            if (this.m_imageThumbnail != null)
            {
                this.m_imageThumbnail.Dispose();
                this.m_imageThumbnail = null;
            }

            try
            {
                this.lblImageSize.Text = string.Empty;
                this.picImage.Image = null;
            }
            catch
            {
            }
        }

        private void bgwResize_DoWork(object sender, DoWorkEventArgs e)
        {
            this.m_ic.Get((int)e.Argument, out this.m_image, out this.m_rawData);
            if (this.m_image == null) return;

            var szBefore = this.m_image.Size;

            try
            {
                ImageResize.ResizeImage(ref this.m_image, this.m_rawData, out this.m_extension);
            }
            catch
            {
                return;
            }
            var szAfter = this.m_image.Size;
         
            //////////////////////////////////////////////////
            // Thumbnail
            var ratio = Math.Min(64d / this.m_image.Width, 64d / this.m_image.Height);
            var newWidth  = (int)(this.m_image.Width  * ratio);
            var newHeight = (int)(this.m_image.Height * ratio);

            this.m_imageThumbnail = new Bitmap(newWidth, newHeight);

            using (var graphics = Graphics.FromImage(this.m_imageThumbnail))
                graphics.DrawImage(this.m_image, 0, 0, newWidth, newHeight);
            //////////////////////////////////////////////////
                        
            this.m_ratio = (szBefore.Width * szBefore.Height) * 100f / (szAfter.Width * szAfter.Height);

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
                try
                {
                    this.ajax.Stop();
                    this.txtText.Enabled = true;

                    this.picImage.Image = this.m_imageThumbnail;

                    this.lblImageSize.Text =
                    String.Format(
                            "{0}x{1} ({2:##0.0} %)",
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
                catch
                {
                }
            }
        }

        private void frmUpload_Enter(object sender, EventArgs e)
        {
            try
            {
                this.txtText.Focus();
            }
            catch
            {
            }
        }

        private void frmUpload_Activated(object sender, EventArgs e)
        {
            try
            {
                this.txtText.Focus();
            }
            catch
            {
            }
        }

        //////////////////////////////////////////////////////////////////////////

        private void ShowPreview()
        {
            using (frmPreview frm = new frmPreview(this.m_image))
                frm.ShowDialog(this);
        }

        //////////////////////////////////////////////////////////////////////////

        private bool m_isOver80p = false;
        private bool m_isOver90p = false;
        private bool m_tweetable = true;

        private static Regex m_regex = new Regex(@"https?:\/\/(-\.)?([^\s\/?\.#-]+\.?)+(\/[^\s]*)?", RegexOptions.Compiled | RegexOptions.IgnoreCase);
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
            if (this.IsDisposed) return;

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
                string boundary  = Helper.CreateString();
                string boundary2 = "--" + boundary;

                var req = Program.Twitter.CreateWebRequest("POST", "https://api.twitter.com/1.1/statuses/update_with_media.json");
                req.ContentType = "multipart/form-data; charset=utf-8; boundary=" + boundary;

                var stream = req.GetRequestStream();
                using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true, NewLine = "\r\n" })
				{
					writer.WriteLine( boundary2 );
					writer.WriteLine( "Content-Disposition: form-data; name=\"status\"" );
					writer.WriteLine( );
					writer.WriteLine( e.Argument as string );

                    int i;
					if (!string.IsNullOrEmpty(InReplyToStatusId) && int.TryParse(InReplyToStatusId, out i))
					{
						writer.WriteLine( boundary2 );
						writer.WriteLine( "Content-Disposition: form-data; name=\"in_reply_to_status_id\"" );
						writer.WriteLine( );
						writer.WriteLine(InReplyToStatusId);
					}

					writer.WriteLine(boundary2);
                    writer.WriteLine("Content-Type: application/octet-stream");
                    writer.WriteLine("Content-Disposition: form-data; name=\"media[]\"; filename=\"img{0}\"", this.m_extension);
                    writer.WriteLine();
                    this.m_rawData.CopyTo(stream);
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
            try
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
            catch
            {
            }
        }

        //////////////////////////////////////////////////////////////////////////

        private void picImage_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                var temp = Path.GetTempFileName();
                temp = Path.ChangeExtension(temp, this.m_extension);
                using (var file = File.OpenWrite(temp))
                    this.m_rawData.CopyTo(file);

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
