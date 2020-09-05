using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Limitation;
using Newtonsoft.Json;
using TiX.Core;
using TiX.Utilities;

namespace TiX.Windows
{
    internal partial class frmUpload : Form
    {
        private ImageCollection m_ic;
        private readonly Action m_closeEvent;
        
        private int m_uploadIndex = 0;
        private int m_uploadRange = 0;
        private int m_viewIndex   = 0;

        private int m_tweetUploadedCount = 0;

        private bool m_tweetUploaded = false;

        internal string InReplyToStatusId { get; set; }

        private const int MaxUploadPerTweet = 4;
        private const int TextLength = 140 - UrlLength;
        private const int UrlLength = 25;
        private static readonly Color[] TextColors;
        static frmUpload()
        {
            double d = 255.0 / Math.Pow(TextLength, 1.3);

            TextColors = new Color[TextLength + 1];
            for (int i = 0; i <= TextLength; ++i)
                TextColors[i] = Color.FromArgb((int)(Math.Pow(i, 1.3) * d), 0, 0);
        }

        //////////////////////////////////////////////////////////////////////////

        public frmUpload(ImageCollection ic, Action closeEvent, bool isInstance)
        {
            InitializeComponent();
            this.Icon = TiX.Resources.TiX;

            this.m_closeEvent = closeEvent;

            this.ShowInTaskbar   = isInstance;
            this.FormBorderStyle = isInstance ? FormBorderStyle.SizableToolWindow : FormBorderStyle.Sizable;

            this.m_ic = ic;
            
            this.Text = $"{TiXMain.ProductName} (1-1 / {ic.Count})";
            this.lblLength.Text = $"0 / {TextLength}";
        }

        private bool m_autoStart;
        public bool AutoStart
        {
            get { return this.m_autoStart; }
            set
            {
                this.m_autoStart = value;
                this.txtText.Enabled = !value;
            }
        }
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
			if (this.txtText.Text.Length > 0) this.txtText.SelectionStart = this.txtText.TextLength;
			StartNew();
        }

        private void StartProgress(int value = -1)
        {
            this.picImage.Visible = false;
            this.progress.Visible = true;
            this.progress.Value   = value;
            this.progress.Start();
        }

        private void StopProgress()
        {
            this.picImage.Visible = true;
            this.progress.Visible = false;
            this.progress.Stop();
        }

        private void StartNew()
        {            
            if (this.m_uploadRange > 0)
                for (int i = 0; i < this.m_uploadRange && this.m_uploadIndex + i < this.m_ic.Count; ++i)
                    this.m_ic[this.m_uploadIndex + i].Dispose();

            this.m_uploadIndex += this.m_uploadRange;
            if (this.m_uploadIndex >= this.m_ic.Count)
            {
                this.DialogResult = DialogResult.OK;
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
                else if (!Settings.Instance.UniformityText)
                {
                    this.txtText.Text = "";
                }

                this.SetTitle();

                this.lblRange.Text = "1 / " + this.m_uploadRange;

                this.txtText.Enabled = !this.AutoStart || (Settings.Instance.UniformityText && this.m_uploadIndex == 0);

                this.StartProgress();
                
                for (int i = 0; i < this.m_uploadRange; ++i)
                    this.m_ic[this.m_uploadIndex + i].StartLoad();
            }
            catch (Exception ex)
            {
                CrashReport.Error(ex, null);
            }
        }

        private void SetTitle()
        {
            this.Text = string.Format("{0} ({1}-{2} / {3})", TiXMain.ProductName, this.m_uploadIndex + 1, this.m_uploadIndex + this.m_uploadRange, this.m_ic.Count);
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
            lock (this.m_rangeSync)
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
                this.picImage.Image = TiX.Resources.refresh;
                break;

            case ImageSet.Statues.Error:
                this.picImage.Image = TiX.Resources.error;
                break;

            case ImageSet.Statues.Success:
                this.picImage.Image = sender.Thumbnail;
                break;
            }
        }
        private void SetThumbnail()
        {
            if (this.m_viewIndex >= this.m_uploadIndex + this.m_uploadRange)
                this.m_viewIndex  = this.m_uploadIndex + this.m_uploadRange - 1;
            
            this.lblRange.Text = string.Format("{0} / {1}", this.m_viewIndex - this.m_uploadIndex + 1, this.m_uploadRange);

            this.SetThumbnailByImageSet(this.m_ic[this.m_viewIndex]);
        }

        private void CheckAllImageLoaded(ImageSet sender)
        {
            for (int i = 0; i < this.m_uploadRange; ++i)
                if (this.m_ic[this.m_uploadIndex + i].Status == ImageSet.Statues.None)
                    return;

            // 모든 이미지 로딩이 실패하면 다음 이미지로 넘어간다
            bool skip = true;
            for (int i = 0; i < this.m_uploadRange; ++i)
            {
                if (this.m_ic[this.m_uploadIndex + i].Status != ImageSet.Statues.Error)
                {
                    skip = false;
                    break;
                }
            }

            if (skip)
            {
                if (this.m_uploadIndex + this.m_uploadRange < this.m_ic.Count)
                    this.StartNew();
                else
                {
                    if (this.m_tweetUploaded)
                        this.Close();
                    else
                    {
                        this.Error("지원되는 형식의 이미지가 없어요!");
                        this.Close();
                    }

                    return;
                }
            }

            // 자동 트윗이 아니면 자동시작 안함
            if (this.AutoStart)
            {
                // 내용 통일이면 두번재 트윗부터, 내용 통일이 아니면 첫번째 트윗부터 자동 트윗
                if (Settings.Instance.UniformityText)
                {
                    if (this.m_tweetUploaded)
                    {
                        this.Tweet();
                        return;
                    }
                }
                else
                {
                    this.Tweet();
                    return;
                }
            }

            this.StopProgress();
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

        private static Regex regUrl = new Regex(@"https?:\/\/(-\.)?([^\s\/?\.#-]+\.?)+(\/[^\s]*)?", RegexOptions.IgnoreCase);
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

                this.lblLength.Text = $"{len} / {TextLength}";

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
                this.MoveImagePrevious();
            }
            else if (e.Control && e.KeyCode == Keys.Right)
            {
                this.MoveImageNext();
            }
            else
            {
                this.m_handledText = false;
            }
        }
        private void txtText_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (this.m_handledText == true)
                e.Handled = true;
        }

        private object m_tweetSync = new object();
        private volatile bool m_tweetSyncb;
        public async void Tweet()
        {
            for (int i = 0; i < this.m_uploadRange; ++i)
                if (this.m_ic[i].Status == ImageSet.Statues.None)
                    return;

            lock (this.m_tweetSync)
            {
                if (this.m_tweetSyncb)
                    return;
                this.m_tweetSyncb = true;
            }

            this.txtText.Enabled = false;

            this.StartProgress(0);

            var result = await Task.Factory.StartNew<object>(this.TweetTask,
                Regex.Replace(this.txtText.Text, @"^\\N\\|[^\\]\\N\\$|[^\\]\\N\\[^\\]", this.m_tweetUploadedCount.ToString()));

            this.StopProgress();

            if (result is int)
            {
                this.m_tweetUploadedCount++;
                this.m_tweetUploaded = true;
                this.StartNew();
            }
            else
            {
                System.Media.SystemSounds.Asterisk.Play();

                if (result is string err)
                    this.Error(err);
                
                this.txtText.Enabled = true;
                this.txtText.Focus();
            }

            lock (this.m_tweetSync)
                this.m_tweetSyncb = false;
        }
        
        private class ErrorObject
        {
            [JsonProperty("message")]
            public string Message { get; set; }
        }
        private class TweetObject
        {
            [JsonProperty("id")]
            public string Id { get; set; }
        }
        private class MediaObject
        {
            [JsonProperty("media_id_string")]
            public string MediaId { get; set; }
        }
        
        private class ImageSetUpload
        {
            public int      Index          { get; set; }
            public int[]    ProgressValue  { get; set; }
            public string   TwitterMediaId { get; set; }
            public ImageSet ImageSet       { get; set; }
        }
        private object TweetTask(object textObject)
        {
            int i;

            this.m_uploadValue = new int[4];

            var lst = new ImageSetUpload[this.m_uploadRange];
            for (i = 0; i < this.m_uploadRange; ++i)
                lst[i] = new ImageSetUpload
                {
                    Index         = i,
                    ImageSet      = this.m_ic[this.m_uploadIndex + i],
                    ProgressValue = this.m_uploadValue
                };
            
            this.m_uploadCount = lst.Length;
            
            var tasks = new Task[lst.Length];
            for (i = 0; i < this.m_uploadRange; ++i)
                tasks[i] = Task.Factory.StartNew(new Action<object>(this.UploadImage), lst[i]);

            Task.WaitAll(tasks);

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
                    return 0;

                sb.Remove(sb.Length - 1, 1);

                media_ids = sb.ToString();
            }

            var obj = new
            {
                status                = (string)textObject,
                media_ids             = media_ids,
                in_reply_to_status_id = this.InReplyToStatusId
            };

            var json = new JsonSerializer();
            try
            {
                var buff = Encoding.UTF8.GetBytes(OAuth.ToString(obj));
                var req = TiXMain.Twitter.CreateWebRequest("POST", "https://api.twitter.com/1.1/statuses/update.json", obj);
                req.GetRequestStream().Write(buff, 0, buff.Length);

                using (var res = req.GetResponse())
                {
                    if (Settings.Instance.EnabledInReply)
                    {
                        using (var rStream = res.GetResponseStream())
                        using (var sReader = new StreamReader(rStream))
                        using (var jReader = new JsonTextReader(sReader))
                        {
                            this.InReplyToStatusId = json.Deserialize<TweetObject>(jReader).Id;
                        }
                    }
                }
            }
            catch (WebException ex)
            {
                if (ex.Response != null)
                {
                    using (var res = ex.Response)
                    using (var rStream = res.GetResponseStream())
                    using (var sReader = new StreamReader(rStream))
                    using (var jReader = new JsonTextReader(sReader))
                    {
                        return json.Deserialize<ErrorObject>(jReader).Message;
                    }
                }
            }
            catch (Exception ex)
            {
                CrashReport.Error(ex, null);
                return ex.Message;
            }

            return 0;
        }

        private int[] m_uploadValue = new int[4];
        private int m_uploadCount;
        private void UpdateProgress()
        {
            if (this.IsDisposed)
                return;

            if (this.InvokeRequired)
                this.Invoke(new Action(this.UpdateProgress));
            else
                lock (this.m_uploadValue)
                    this.progress.Value =
                        (
                            this.m_uploadValue[0] +
                            this.m_uploadValue[1] +
                            this.m_uploadValue[2] +
                            this.m_uploadValue[3]
                        ) / this.m_uploadCount;
        }

        private void UploadImage(object rparam)
        {
            var param = (ImageSetUpload)rparam;
            try
            {
                param.ImageSet.Wait();

                if (param.ImageSet.Status == ImageSet.Statues.Error)
                    return;

                var boundary = new Guid().ToString("N");

                var req = (HttpWebRequest)TiXMain.Twitter.CreateWebRequest("POST", "https://upload.twitter.com/1.1/media/upload.json");
                req.AllowWriteStreamBuffering = false;
                req.SendChunked = false;
                req.ContentType = "multipart/form-data; charset=utf-8; boundary=" + boundary;
                
                using (var memory = new MemoryStream((int)param.ImageSet.RawStream.Length + 4096))
                using (var writer = new StreamWriter(memory, Encoding.UTF8) { AutoFlush = true, NewLine = "\r\n" })
                {
                    boundary = "--" + boundary;
                    writer.WriteLine(boundary);
                    writer.WriteLine("Content-Type: application/octet-stream");
                    writer.WriteLine("Content-Disposition: form-data; name=\"media\"; filename=\"img{0}\"", param.ImageSet.Extension);
                    writer.WriteLine();
                    param.ImageSet.RawStream.Position = 0;
                    param.ImageSet.RawStream.CopyTo(memory);
                    writer.WriteLine();
                    writer.WriteLine(boundary + "--");

                    memory.Position = 0;

                    req.ContentLength = memory.Length;
                    var upload = req.GetRequestStream();
                    int read;
                    var buff = new byte[40960];

                    while (memory.Position < memory.Length)
                    {
                        read = memory.Read(buff, 0, 40960);
                        if (read == 0)
                            break;
                        
                        upload.Write(buff, 0, read);
                        
                        lock (param.ProgressValue)
                            param.ProgressValue[param.Index] = (int)((double)memory.Position / memory.Length * 100);

                        this.UpdateProgress();
                    }
                }

                var json = new JsonSerializer();
                using (var res = req.GetResponse())
                using (var rStream = res.GetResponseStream())
                using (var sReader = new StreamReader(rStream))
                using (var jReader = new JsonTextReader(sReader))
                {
                    param.TwitterMediaId = json.Deserialize<MediaObject>(jReader).MediaId;
                }
            }
            catch (WebException)
            {
            }
            catch (Exception ex)
            {
                CrashReport.Error(ex, null);
            }
        }

        //////////////////////////////////////////////////////////////////////////

        private bool  m_picImage_MouseDown         = false;
        private Point m_picImage_MouseDownLocation = Point.Empty;

        private void picImage_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.m_picImage_MouseDown && this.m_picImage_MouseDownLocation != e.Location)
            {
                this.m_picImage_MouseDown = false;

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
            else
            {
                var rx = (float)e.X / this.picImage.Width;

                     if (rx < 0.2) this.picImage.Cursor = Cursors.PanWest;
                else if (0.8 < rx) this.picImage.Cursor = Cursors.PanEast;
                else               this.picImage.Cursor = Cursors.Default;
            }

            this.m_picImage_MouseDownLocation = new Point(-1, -1);
        }

        private void picImage_MouseDown(object sender, MouseEventArgs e)
        {
            this.m_picImage_MouseDownLocation = e.Location;

            if (e.Button == MouseButtons.Left)
            {
                this.m_picImage_MouseDown = true;
            }
            else if (e.Button == MouseButtons.Right)
            {
                this.ShowPreview();
            }
        }

        private void picImage_MouseUp(object sender, MouseEventArgs e)
        {
            this.m_picImage_MouseDown = false;

            if (e.Location != this.m_picImage_MouseDownLocation)
                return;

            if (e.Button == MouseButtons.Right)
                this.ShowPreview();
            else if (e.Button == MouseButtons.Left)
            {                
                var rx = (float)e.X / this.picImage.Width;

                     if (rx < 0.2) this.MoveImagePrevious();
                else if (0.8 < rx) this.MoveImageNext();
            }
        }

        private void ShowPreview()
        {
            var cur = this.m_ic[this.m_viewIndex];
            if (cur.Status == ImageSet.Statues.Success)
                using (frmPreview frm = new frmPreview(cur))
                    frm.ShowDialog(this);
        }

        private void MoveImagePrevious()
        {
            if (this.m_uploadIndex < this.m_viewIndex)
                this.m_viewIndex--;
            else
                this.m_viewIndex = this.m_uploadIndex + this.m_uploadRange - 1;
            this.SetThumbnail();
        }

        private void MoveImageNext()
        {
            if (this.m_viewIndex < this.m_uploadIndex + this.m_uploadRange - 1)
                this.m_viewIndex++;
            else
                this.m_viewIndex = this.m_uploadIndex;
            this.SetThumbnail();
        }

        private void frmUpload_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.m_closeEvent?.Invoke();
            this.m_ic.Dispose();
        }
    }
}
