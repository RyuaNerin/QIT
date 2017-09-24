using System;
using System.Windows.Forms;
using TiX.Core;
using TiX.Utilities;

namespace TiX.Windows
{
    internal partial class frmSettings : Form
    {
        private readonly SettingBinder m_binder = new SettingBinder();

        public frmSettings()
        {
            InitializeComponent();
            this.Icon = TiX.Resources.TiX;

            var si = Settings.Instance;

            this.TopMost = si.Topmost;

            this.m_binder.Add(si, e => e.EnabledErrorReport, this.chkErrorReport);
            this.m_binder.Add(si, e => e.EnabledInReply,     this.chkInreply);
            this.m_binder.Add(si, e => e.ReversedCtrl,       this.chkReversedCtrl);
            this.m_binder.Add(si, e => e.Topmost,            this.chkTopMost);
            this.m_binder.Add(si, e => e.UniformityText,     this.chkUniformity);

            this.m_binder.Add(si, e => e.MinizeToTray,       this.chkMinizeToTray);
            this.m_binder.Add(si, e => e.StartInTray,        this.chkStartInTray);
            this.m_binder.Add(si, e => e.StartWithWindows,   this.chkStartWithWindows);

            if (TiXMain.IsInstalled)
            {
                this.m_binder.Add(si, e => e.SEWithText,    this.chkEnableShellWT);
                this.m_binder.Add(si, e => e.SEWithoutText, this.chkEnableShellWoT);
            }
            else
            {
                this.chkEnableShellWT.Enabled  = false;
                this.chkEnableShellWoT.Enabled = false;
            }

            this.m_binder.FromSetting();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            this.m_binder.ToSetting();

            Settings.Instance.ApplyToRegistry();
            Settings.Instance.Save();
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void lblCopyRight_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { UseShellExecute = true, FileName = "\"https://github.com/RyuaNerin/QIT\"" }).Dispose();
        }
    }
}
