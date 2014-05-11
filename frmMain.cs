using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace QIT
{
	public partial class frmMain : Form
	{
		public frmMain()
		{
			InitializeComponent();

			this.Text = Program.ProductName;
		}

		List<string> _lstPaths = new List<string>();
		bool _autoStart;

		private void pnl_DragEnter(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
				if ((e.KeyState & 8) == 8)
					e.Effect = DragDropEffects.Move;
				else
					e.Effect = DragDropEffects.Copy;

			else
				e.Effect = DragDropEffects.None;
		}

		private void pnl_DragDrop(object sender, DragEventArgs e)
		{
			try
			{
				this._autoStart = ((e.KeyState & 8) == 8);

				string[] data = (string[])e.Data.GetData(DataFormats.FileDrop);

				if (data.Length > 10)
					if (MessageBox.Show(
						this,
						String.Format("정말 {0} 개의 이미지를 트윗하시겠습니까?", data.Length),
						Program.ProductName,
						MessageBoxButtons.YesNo,
						MessageBoxIcon.Question) == DialogResult.No)
						return;

				_lstPaths.AddRange(data);
				_lstPaths.Sort();

				this.tmrQueue.Enabled = true;
			}
			catch
			{
				
			}
		}

		private void tmrQueue_Tick(object sender, EventArgs e)
		{
			this.tmrQueue.Enabled = false;
			for (int i = 0; i < _lstPaths.Count; ++i)
			{
				try
				{
					switch (_lstPaths[i].Substring(_lstPaths[i].LastIndexOf('.')).ToLower())
					{
						case ".png":
						case ".jpg":
						case ".jpeg":
						case ".gif":
							using (frmUpload frm = new frmUpload())
							{
								frm.AutoStart = this._autoStart;

								frm.Text = String.Format("{0} ({1} / {2})", Program.ProductName, i + 1, _lstPaths.Count);
								if (this._autoStart)
									frm.txtText.Text = _lstPaths[i].Substring(_lstPaths[i].LastIndexOf('\\') + 1);

								if (frm.SetImage(_lstPaths[i]))
									frm.ShowDialog(this);

								frm.Dispose();
							}
							break;

					default:
							break;
					}
				}
				catch
				{

				}
			}

			this._lstPaths.Clear();
		}
	}
}
