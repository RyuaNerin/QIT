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
using Decchi.Utilities;
using System.Windows.Input;

namespace Quicx
{
    public partial class frmMain : Form
    {
        private static GlobalKeyboardHook manager;
        public frmMain()
        {
            InitializeComponent();

            this.Text = Program.ProductName;
            this.TopMost = Settings.isTopmost;
            if (Settings.isReversedCtrl)
                label2.Text = "Ctrl을 눌러 내용 작성";
            else
                label2.Text = "Ctrl을 눌러 바로 작성";


            manager = new GlobalKeyboardHook();
            manager.KeyDown += manager_KeyDown;

        }

        void manager_KeyDown(object sender, GlobalKeyboardHook.KeyHookEventArgs e)
        {
            if (Keyboard.Modifiers == (System.Windows.Input.ModifierKeys.Control) &&
                e.Key == Key.C)
            {
                this.Hide();
                new Quicx.ScreenCapture.Stasisfield().ShowDialog();
            }
        }

        private IList<DragDropInfo> _lstImages = new List<DragDropInfo>();
        private bool _autoStart;

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

                if (allow) e.Effect = DragDropEffects.Move;
            }
        }

        public static readonly string[] AllowExtension = { ".bmp", ".emf", ".exif", ".gif", ".ico", ".jpg", ".jpeg", ".png", ".tiff", ".wmf", ".psd" };

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
                        if (Settings.isUniformityText && i != 0) frm.SetText(UnifiedStr);
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

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            Settings.Save();
        }

        private void pnl_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {

            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                new frmSettings().ShowDialog();
                this.TopMost = Settings.isTopmost;
                if (Settings.isReversedCtrl)
                    label2.Text = "Ctrl을 눌러 내용 작성";
                else
                    label2.Text = "Ctrl을 눌러 바로 작성";
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Hide();
            new Quicx.ScreenCapture.Stasisfield().ShowDialog();
        }
    }
}
