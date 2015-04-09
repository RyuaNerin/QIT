using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace QIT
{
	public partial class frmMain : Form
	{
		public frmMain()
		{
			InitializeComponent();

			this.Text = Program.ProductName;
            this.TopMost = Settings.isTopmost;

            _buttPosition.Size = new Size(SystemInformation.CaptionButtonSize.Height, SystemInformation.CaptionButtonSize.Height);
		}

		private IList<DragDropInfo>	_lstImages = new List<DragDropInfo>();
		private bool		_autoStart;

		private void pnl_DragOver(object sender, DragEventArgs e)
		{
			e.Effect = DragDropEffects.None;

			if (DragDropInfo.isAvailable(e))
			{
				bool allow = true;

				if (e.Data.GetDataPresent(DataFormats.FileDrop))
				{
					string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop);

					for (int i = 0; i < paths.Length; ++i)
					{
						if (Array.IndexOf<string>(frmMain.AllowExtension, Path.GetExtension(paths[i]).ToLower()) < 0)
						{
							allow = false;
							break;
						}
					}
				}

				if (allow)
					e.Effect = DragDropEffects.Move;
			}
		}

		private static readonly string[] AllowExtension =
		{ ".bmp", ".emf", ".exif", ".gif", ".ico", ".jpg", ".jpeg", ".png", ".tiff" , ".wmf", ".psd" };
		private void pnl_DragDrop(object sender, DragEventArgs e)
		{
			try
			{
				this._autoStart = ((e.KeyState & 8) == 8);

				// Local Files
				if (e.Data.GetDataPresent(DataFormats.FileDrop))
				{
					string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop);

					if (paths.Length > 10)
						if (MessageBox.Show(
							this,
							String.Format("정말 {0} 개의 이미지를 트윗하시겠습니까?", paths.Length),
							Program.ProductName,
							MessageBoxButtons.YesNo,
							MessageBoxIcon.Question) == DialogResult.No)
							return;

					Array.Sort<string>(paths);

					for (int i = 0; i < paths.Length; ++i)
						if (Array.IndexOf<string>(frmMain.AllowExtension, Path.GetExtension(paths[i]).ToLower()) >= 0)
							this._lstImages.Add(new DragDropInfo(paths[i]));
				}
				else
				{
					DragDropInfo info = new DragDropInfo(e);
					if (info.DataType != DragDropInfo.DataTypes.None)
						this._lstImages.Add(info);
					else
						info.Dispose();
				}

				this.tmrQueue.Enabled = true;

			}
			catch
			{
				
			}
		}

		private void tmrQueue_Tick(object sender, EventArgs e)
		{
			this.tmrQueue.Enabled = false;
            string UnifiedStr = string.Empty;
			for (int i = 0; i < _lstImages.Count; ++i)
			{
				using (frmUpload frm = new frmUpload())
				{
					frm.AutoStart = this._autoStart;

					frm.Text = String.Format("{0} ({1} / {2})", Program.ProductName, i + 1, _lstImages.Count);

                    if (frm.SetImage(_lstImages[i]))
                    {
                        frm.Index = i;
                        if (Settings.isUniformityText && i!=0) frm.SetText(UnifiedStr);
                        frm.ShowDialog(this);
                        if (Settings.isUniformityText && i == 0) UnifiedStr = frm.GetText();
                    }

					frm.Dispose();

					_lstImages[i].Dispose();
				}
			}

			this._lstImages.Clear();
		}
        
        private void pnl_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.None;

            if (DragDropInfo.isAvailable(e))
            {
                bool allow = true;

                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop);

                    for (int i = 0; i < paths.Length; ++i)
                    {
                        if (Array.IndexOf<string>(frmMain.AllowExtension, Path.GetExtension(paths[i]).ToLower()) < 0)
                        {
                            allow = false;
                            break;
                        }
                    }
                }

                if (allow)
                    e.Effect = DragDropEffects.Move;
            }
        }

        void OpenSettingWindow()
        {
            new frmSettings().ShowDialog();
            this.TopMost = Settings.isTopmost;
        }

        /////////////////////////////////////////////////////////////////////////////////
        // Title bar hooking
        // http://www.experts-exchange.com/Programming/Languages/.NET/Q_25353633.html
        /////////////////////////////////////////////////////////////////////////////////


        // The state of our little button
        ButtonState _buttState = ButtonState.Normal;
        Rectangle _buttPosition = new Rectangle();

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern int GetWindowRect(IntPtr hWnd,
                                                ref Rectangle lpRect);
        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
        protected override void WndProc(ref Message m)
        {
            int x, y;
            Rectangle windowRect = new Rectangle();
            GetWindowRect(m.HWnd, ref windowRect);

            switch (m.Msg)
            {
                // WM_NCPAINT
                case 0x85:
                // WM_PAINT
                case 0x0A:
                    base.WndProc(ref m);

                    DrawButton(m.HWnd);

                    m.Result = IntPtr.Zero;

                    break;

                // WM_ACTIVATE
                case 0x86:
                    base.WndProc(ref m);
                    DrawButton(m.HWnd);

                    break;

                // WM_NCMOUSEMOVE
                case 0xA0:
                    // Extract the least significant 16 bits
                    x = ((int)m.LParam << 16) >> 16;
                    // Extract the most significant 16 bits
                    y = (int)m.LParam >> 16;

                    x -= windowRect.Left;
                    y -= windowRect.Top;

                    base.WndProc(ref m);

                    if (!_buttPosition.Contains(new Point(x, y)) &&
                        _buttState == ButtonState.Pushed)
                    {
                        _buttState = ButtonState.Normal;
                        DrawButton(m.HWnd);
                    }

                    break;

                // WM_NCLBUTTONDOWN
                case 0xA1:
                    // Extract the least significant 16 bits
                    x = ((int)m.LParam << 16) >> 16;
                    // Extract the most significant 16 bits
                    y = (int)m.LParam >> 16;

                    x -= windowRect.Left;
                    y -= windowRect.Top;

                    if (_buttPosition.Contains(new Point(x, y)))
                    {
                        _buttState = ButtonState.Pushed;
                        DrawButton(m.HWnd);
                    }
                    else
                        base.WndProc(ref m);

                    break;

                // WM_NCLBUTTONUP
                case 0xA2:
                    // Extract the least significant 16 bits
                    x = ((int)m.LParam << 16) >> 16;
                    // Extract the most significant 16 bits
                    y = (int)m.LParam >> 16;

                    x -= windowRect.Left;
                    y -= windowRect.Top;

                    if (_buttPosition.Contains(new Point(x, y)) &&
                        _buttState == ButtonState.Pushed)
                    {
                        _buttState = ButtonState.Normal;
                        OpenSettingWindow();

                        DrawButton(m.HWnd);
                    }
                    else
                        base.WndProc(ref m);

                    break;

                // WM_NCHITTEST
                case 0x84:
                    // Extract the least significant 16 bits
                    x = ((int)m.LParam << 16) >> 16;
                    // Extract the most significant 16 bits
                    y = (int)m.LParam >> 16;

                    x -= windowRect.Left;
                    y -= windowRect.Top;

                    if (_buttPosition.Contains(new Point(x, y)))
                        m.Result = (IntPtr)18; // HTBORDER
                    else
                        base.WndProc(ref m);

                    break;

                default:
                    base.WndProc(ref m);
                    break;
            }
        }

        private void DrawButton(IntPtr hwnd)
        {

            IntPtr hDC = GetWindowDC(hwnd);
            int x, y;

            try
            {
                using (Graphics g = Graphics.FromHdc(hDC))
                {
                    // Work out size and positioning
                    int CaptionHeight = Bounds.Height - ClientRectangle.Height;
                    Size ButtonSize = SystemInformation.CaptionButtonSize;
                    x = Bounds.Width - 4 * ButtonSize.Width;
                    y = (CaptionHeight - ButtonSize.Height) / 2 - 2;
                    _buttPosition.Location = new Point(x, y);

                    // Draw our "button"
                    g.DrawImage(Bitmap.FromFile(_buttState == ButtonState.Pushed ? "c:/setting.png" : "c:/settingp.png"), _buttPosition);
                }

                ReleaseDC(hwnd, hDC);
            }
            catch { }
        }

        private void pnl_MouseEnter(object sender, EventArgs e)
        {
            if (_buttState == ButtonState.Pushed)
            {
                _buttState = ButtonState.Normal;
                DrawButton(this.Handle);
            }
        }


        /////////////////////////////////////////////////////////////////////////////////

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            Settings.Save();
        }

    }
}
