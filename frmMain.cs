using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Imaging;

namespace QIT
{
	public partial class frmMain : Form
	{
		public frmMain()
		{
			InitializeComponent();

			this.Text = Program.ProductName;

			if (Settings.ImageExt == 0)
				this.btnExtOrig.Checked = true;
			else if (Settings.ImageExt == 1)
				this.btnExtPNG.Checked = true;
			else if (Settings.ImageExt == 2)
				this.btnExtJPG.Checked = true;
			this.btnExtPNGTrans.Checked = Settings.PNGTrans;
		}

		private void btmCopyright_Click(object sender, EventArgs e)
		{
			System.Diagnostics.Process.Start("explorer", "\"http://blog.ryuanerin.kr/\"");
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
			for (int i = 0; i < _lstImages.Count; ++i)
			{
				using (frmUpload frm = new frmUpload())
				{
					frm.AutoStart = this._autoStart;

					frm.Text = String.Format("{0} ({1} / {2})", Program.ProductName, i + 1, _lstImages.Count);

					if (frm.SetImage(_lstImages[i]))
						frm.ShowDialog(this);

					frm.Dispose();

					_lstImages[i].Dispose();
				}
			}

			this._lstImages.Clear();
		}

		private void btnExtOrig_CheckedChanged(object sender, EventArgs e)
		{
			if (this.btnExtOrig.Checked)
			{
				Settings.ImageExt = 0;
				Settings.Save();

				this.btnExtJPG.Checked = this.btnExtPNG.Checked = false;
			}
			else
			{
				if (!this.btnExtJPG.Checked && !this.btnExtPNG.Checked)
					this.btnExtOrig.Checked = true;
			}
		}

		private void btnExtPNG_CheckedChanged(object sender, EventArgs e)
		{
			if (this.btnExtPNG.Checked)
			{
				Settings.ImageExt = 1;
				Settings.Save();

				this.btnExtJPG.Checked = this.btnExtOrig.Checked = false;
			}
			else
			{
				if (!this.btnExtJPG.Checked && !this.btnExtOrig.Checked)
					this.btnExtPNG.Checked = true;
			}
		}

		private void btnExtJPG_CheckedChanged(object sender, EventArgs e)
		{
			if (this.btnExtJPG.Checked)
			{
				Settings.ImageExt = 2;
				Settings.Save();

				this.btnExtOrig.Checked = this.btnExtPNG.Checked = false;
			}
			else
			{
				if (!this.btnExtOrig.Checked && !this.btnExtPNG.Checked)
					this.btnExtJPG.Checked = true;
			}
		}

		private void btnExtPNGTrans_CheckedChanged(object sender, EventArgs e)
		{
			Settings.PNGTrans = this.btnExtPNGTrans.Checked;
			Settings.Save();
		}
	}
}
