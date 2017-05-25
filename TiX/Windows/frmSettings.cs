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
            this.Icon = TiX.Properties.Resources.TiX;

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

            this.m_binder.Add(si, e => e.SEWithText,         this.chkEnableShellWT);
            this.m_binder.Add(si, e => e.SEWithoutText,      this.chkEnableShellWoT);

            this.m_binder.FromSetting();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (Settings.Instance.SEWithText       != this.chkEnableShellWT.Checked  ||
                Settings.Instance.SEWithoutText    != this.chkEnableShellWoT.Checked ||
                Settings.Instance.StartWithWindows != this.chkStartWithWindows.Checked)
            {
                var option = OptionTixSettings.None;
                if (this.chkEnableShellWT.Checked)    option |= OptionTixSettings.ShellExtension_WithText;
                if (this.chkEnableShellWoT.Checked)   option |= OptionTixSettings.ShellExtension_WithoutText;
                if (this.chkStartWithWindows.Checked) option |= OptionTixSettings.StartWithWindows;

                switch (Installer.TiXSetting(false, option))
                {
                    case InstallerResult.NOT_AUTHORIZED:
                        this.Error("관리자 권한으로 실행해주세요!");
                        break;

                    case InstallerResult.UNKNOWN:
                        this.Error("알 수 없는 문제가 발생했어요!");
                        break;
                }
            }


            this.m_binder.ToSetting();

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
