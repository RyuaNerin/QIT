using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Limitation;
using TiX.Core;

namespace TiX.Windows
{
    internal partial class frmUpload : Form
    {
        private ImageCollection m_ic;
        
        private int m_uploadIndex = 0;
        private int m_uploadRange = 0;
        private int m_viewIndex   = 0;

        private int m_tweeted = 0;

        internal string InReplyToStatusId { get; set; }

        private const int MaxUploadPerTweet = 4;
        private const int TextLength = 140 - UrlLength;
        private const int UrlLength = 24;
        private static readonly Color[] TextColors;
        static frmUpload()
        {
            double d = 255.0 / Math.Pow(TextLength, 1.3);

            TextColors = new Color[TextLength + 1];
            for (int i = 0; i <= TextLength; ++i)
                TextColors[i] = Color.FromArgb((int)(Math.Pow(i, 1.3) * d), 0, 0);
        }

        //////////////////////////////////////////////////////////////////////////

        public frmUpload(ImageCollection ic, bool mainWnd = false)
        {
            InitializeComponent();
            this.Icon = TiX.Properties.Resources.TiX;

            this.ShowInTaskbar = mainWnd;

            this.m_ic = ic;
            
            this.Text = String.Format("{0} (1-1 / {1})", TiXMain.ProductName, ic.Count);
            this.lblLength.Text = String.Format("0 / {0}", TextLength);
        }

        public bool AutoStart { get; set; }
        private string m_tweetString;
        public string TweetString
        {
            get { return this.m_tweetString; }
            set { this.txtText.Text = this.m_tweetString = value; }
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

            this.m_ic.LoadedImage += this.LoadedImage;
			if ( txtText.Text.Length > 0 ) this.txtText.SelectionStart = this.txtText.TextLength;
			StartNew();
		}

        private void StartNew()
        {            
            if (this.m_uploadRange > 0)
                for (int i = 0; i < this.m_uploadRange && this.m_uploadIndex + i < this.m_ic.Count; ++i)
                    this.m_ic[this.m_uploadIndex + i].Dispose();

            this.m_uploadIndex += this.m_uploadRange;
            if (this.m_uploadIndex >= this.m_ic.Count)
            {
                this.Close();
                return;
            }

            this.m_viewIndex = this.m_uploadIndex;
            this.m_uploadRange = Math.Min(MaxUploadPerTweet, this.m_ic.Count - this.m_uploadIndex);
            this.SetRange();

            this.SetThumbnail();
            
            try
            {
                if (!string.IsNullOrWhiteSpace(this.TweetString))
                {
                    this.txtText.Text = this.TweetString;
                }
                else if (!Settings.UniformityText)
                {
                    this.txtText.Text = "";
                }

                this.SetTitle();

                this.lblRange.Text = "1 / " + this.m_uploadRange;

                this.txtText.Enabled = true;
                this.ajax.Start();
                
                for (int i = 0; i < this.m_uploadRange; ++i)
                    this.m_ic[this.m_uploadIndex + i].StartLoad();
            }
            catch
            {
            }
        }

        private void SetTitle()
        {
            this.Text = String.Format("{0} ({1}-{2} / {3})", TiXMain.ProductName, this.m_uploadIndex + 1, this.m_uploadIndex + this.m_uploadRange, this.m_ic.Count);
        }

        private void LoadedImage(object sender ,EventArgs e)
        {
            var imageSet = sender as ImageSet;

            this.SetRange();

            if (this.m_viewIndex == imageSet.Index)
                this.Invoke(new Action<ImageSet>(this.SetThumbnailByImageSet), imageSet);

            this.Invoke(new Action<ImageSet>(this.CheckAllImageLoaded), imageSet);
        }

        private object m_rangeSync = new object();
        private void SetRange()
        {
            lock (m_rangeSync)
            {
                for (int i = 0; i < this.m_uploadRange; ++i)
                {
                    if (this.m_ic[this.m_uploadIndex + i].Status == ImageSet.Statues.Success &&
                        this.m_ic[this.m_uploadIndex + i].GifFrames != null)
                    {
                        this.m_uploadRange = i == 0 ? 1 : i;

                        this.Invoke(new Action(this.SetTitle));
                        this.Invoke(new Action(this.SetThumbnail));
                        
                        break;
                    }
                }
            }
        }

        private void SetThumbnailByImageSet(ImageSet sender)
        {
            switch (sender.Status)
            {
            case ImageSet.Statues.None:
                this.picImage.Image = Properties.Resources.refresh;
                break;

            case ImageSet.Statues.Error:
                this.picImage.Image = Properties.Resources.error;
                break;

            case ImageSet.Statues.Success:
                this.picImage.Image = sender.Thumbnail;
                break;
            }
        }
        private void SetThumbnail()
        {
            if (this.m_viewIndex >= this.m_uploadIndex + this.m_uploadRange)
                this.m_viewIndex = this.m_uploadIndex + this.m_uploadRange - 1;
            
            this.lblRange.Text = string.Format("{0} / {1}", this.m_viewIndex - this.m_uploadIndex + 1, this.m_uploadRange);

            this.SetThumbnailByImageSet(this.m_ic[this.m_viewIndex]);
        }

        private void CheckAllImageLoaded(ImageSet sender)
        {
            for (int i = 0; i < this.m_uploadRange; ++i)
                if (this.m_ic[this.m_uploadIndex + i].Status == ImageSet.Statues.None)
                    return;

            this.ajax.Stop();

            if (this.AutoStart && (!Settings.UniformityText || this.m_uploadIndex > 0))
            {
                // 자동 트윗이거나,
                // 내용 통일이 아니거나,
                // 내용 통일이고 index 가 0 이 아닐때 자동트윗
                this.Tweet();
            }
            else if (Settings.UniformityText && this.m_uploadIndex > 0)
                this.Tweet();

            this.txtText.Focus();
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

        private static Regex regUrl = new Regex(@"https?:\/\/(-\.)?([^\s\/?\.#-]+\.?)+(\/[^\s]*)?", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private void txtText_TextChanged(object sender, EventArgs e)
        {
            try
            {
                var str = this.txtText.Text;
                var len = str.Length;

                var m = regUrl.Match(str);
                while (m.Success)
                {
                    len = len - m.Length + UrlLength;
                    m = m.NextMatch();
                }

                this.lblLength.Text = String.Format("{0} / {1}", len, TextLength);

                if (len > TextLength)
                    len = TextLength;

                this.lblLength.ForeColor = TextColors[len];
            }
            catch
            { }
        }

        private bool m_handledText = false;
        private void txtText_KeyDown(object sender, KeyEventArgs e)
        {
            if (this.IsDisposed) return;

            if (!e.Control && (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Return))
            {
                this.Tweet();

                this.m_handledText = true;
            }
            else if (e.Control && e.KeyCode == Keys.A)
            {
                this.txtText.SelectAll();

                this.m_handledText = true;
            }
            else if (e.Control && e.KeyCode == Keys.R)
            {
                this.ShowPreview();

                this.m_handledText = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                this.StartNew();

                this.m_handledText = true;
            }
            else if (e.Control && e.KeyCode == Keys.Left)
            {
                if (this.m_uploadIndex < this.m_viewIndex)
                    this.m_viewIndex--;
                else
                    this.m_viewIndex = this.m_uploadIndex + this.m_uploadRange - 1;
                this.SetThumbnail();
            }
            else if (e.Control && e.KeyCode == Keys.Right)
            {
                if (this.m_viewIndex < this.m_uploadIndex + this.m_uploadRange - 1)
                    this.m_viewIndex++;
                else
                    this.m_viewIndex = this.m_uploadIndex;
                this.SetThumbnail();
            }
            else
            {
                this.m_handledText = false;
            }
        }
        private void txtText_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (m_handledText == true)
                e.Handled = true;
        }

        public async void Tweet()
        {
            for (int i = 0; i < this.m_uploadRange; ++i)
                if (this.m_ic[i].Status == ImageSet.Statues.None)
                    return;

            this.txtText.Enabled = false;
            this.ajax.Start();

            var result = await Task.Factory.StartNew<object>(this.TweetTask, this.txtText.Text.Replace("/N/", (this.m_tweeted + 1).ToString()));


            if (result is int)
            {
                this.ajax.Stop();
                this.StartNew();
            }
            else
            {
                System.Media.SystemSounds.Asterisk.Play();

                var err = result as string;
                if (err != null)
                    MessageBox.Show(this, err, TiXMain.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);

                this.ajax.Stop();
                this.txtText.Enabled = true;
                this.txtText.Focus();
            }
        }

        private static Regex regError = new Regex("\"message\"[ \t]*:[ \t]*\"([^\"]+)\"", RegexOptions.IgnoreCase);
        private static Regex regTweetId = new Regex("\"id_str\"[ \t]*:[ \t]*\"([^\"]+)\"", RegexOptions.IgnoreCase);
        private object TweetTask(object textObject)
        {
            int i;

            var lst = new ImageSet[this.m_uploadRange];
            for (i = 0; i < this.m_uploadRange; ++i)
                lst[i] = this.m_ic[this.m_uploadIndex + i];

            var option = new ParallelOptions();
            option.MaxDegreeOfParallelism = 4;
            Parallel.ForEach(lst, option, UploadImage);
            
            string media_ids;
            {
                var sb = new StringBuilder(32);

                int k = 0;
                for (i = 0; i < this.m_uploadRange; ++i)
                {
                    if (lst[i].TwitterMediaId != null)
                    {
                        k++;
                        sb.AppendFormat(lst[i].TwitterMediaId);
                        sb.AppendFormat(",");
                    }
                }

                if (k == 0)
                {
                    return 0;
                }

                sb.Remove(sb.Length - 1, 1);

                media_ids = sb.ToString();
            }
            var obj = new { status = (string)textObject, media_ids = media_ids, in_reply_to_status_id = this.InReplyToStatusId };

            try
            {
                var buff = Encoding.UTF8.GetBytes(OAuth.ToString(obj));
                var req = TiXMain.Twitter.CreateWebRequest("POST", "https://api.twitter.com/1.1/statuses/update.json", obj);
                req.GetRequestStream().Write(buff, 0, buff.Length);

                using (var res = req.GetResponse())
                {
                    using (var stream = res.GetResponseStream())
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        if (Settings.EnabledInReply)
                        {
                            var m = regError.Match(reader.ReadToEnd());
                            if (m.Success)
                                this.InReplyToStatusId = m.Groups[1].Value;
                        }
                    }
                }
            }
            catch (WebException ex)
            {
                if (ex.Response != null)
                {
                    using (var res = ex.Response)
                    using (var stream = res.GetResponseStream())
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        var m = regError.Match(reader.ReadToEnd());
                        if (m.Success)
                            return m.Groups[1].Value;
                    }
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

            return 0;
        }

        private static Regex regMedia = new Regex("\"media_id_string\"[ \t]*:[ \t]*\"([^\"]+)\"", RegexOptions.IgnoreCase);
        private void UploadImage(ImageSet imageSet)
        {
            try
            {
                if (imageSet.Status == ImageSet.Statues.None)
                    imageSet.IsLoading.WaitOne();

                if (imageSet.Status == ImageSet.Statues.Error)
                    return;

                var boundary  = Helper.CreateRandomString();

                var req = TiXMain.Twitter.CreateWebRequest("POST", "https://upload.twitter.com/1.1/media/upload.json");
                req.ContentType = "multipart/form-data; charset=utf-8; boundary=" + boundary;
                boundary = "--" + boundary;

                var stream = req.GetRequestStream();
                using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true, NewLine = "\r\n" })
                {
                    writer.WriteLine(boundary);
                    writer.WriteLine("Content-Type: application/octet-stream");
                    writer.WriteLine("Content-Disposition: form-data; name=\"media\"; filename=\"img{0}\"", imageSet.Extension);
                    writer.WriteLine();
                    imageSet.RawStream.Position = 0;
                    imageSet.RawStream.CopyTo(stream);
                    writer.WriteLine();
                    writer.WriteLine(boundary + "--");
                }

                using (var res = req.GetResponse())
                using (var reader = new StreamReader(res.GetResponseStream()))
                    imageSet.TwitterMediaId = regMedia.Match(reader.ReadToEnd()).Groups[1].Value;
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
                var cur = this.m_ic[this.m_viewIndex];

                if (cur.Status != ImageSet.Statues.Success)
                    return;

                var temp = Path.GetTempFileName();
                temp = Path.ChangeExtension(temp, cur.Extension);

                cur.RawStream.Position = 0;
                using (var file = File.OpenWrite(temp))
                    cur.RawStream.CopyTo(file);

                var dataObject = new DataObject();
                dataObject.SetData(DataFormats.FileDrop, new string[] { temp });

                this.picImage.DoDragDrop(dataObject, DragDropEffects.Copy | DragDropEffects.Move);
            }
            else if (e.Button == MouseButtons.Right)
            {
                this.ShowPreview();
            }
        }

        private void ShowPreview()
        {
            var cur = this.m_ic[this.m_viewIndex];
            if (cur.Status == ImageSet.Statues.Success)
                using (frmPreview frm = new frmPreview(cur))
                    frm.ShowDialog(this);
        }
    }
}
