using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
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

				_lstPaths.AddRange(data);

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
								frm.SetImage(_lstPaths[i]);

								frm.AutoStart = this._autoStart;

								frm.Text = String.Format("QIT BETA 2. ({0} / {1})", i + 1, _lstPaths.Count);
								if (this._autoStart)
									frm.txtText.Text = _lstPaths[i].Substring(_lstPaths[i].LastIndexOf('\\') + 1);

								frm.ShowDialog(this);
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
