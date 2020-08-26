using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using TiX.Core;
using TiX.Utilities;

namespace TiX.Windows
{
    public partial class ConfigWindow : Window
    {
        private readonly SettingBinder m_binder = new SettingBinder();

        public ConfigWindow()
        {
            this.InitializeComponent();

            var si = Settings.Instance;

            this.m_binder.Add(si, e => e.UniformityText, this.ConfigUniformityText);

            this.m_binder.Add(si, e => e.Topmost, this.ConfigTopMost);
            this.m_binder.Add(si, e => e.StartInTray, this.ConfigStartInTray);
            this.m_binder.Add(si, e => e.MinizeToTray, this.ConfigMinizeToTray);

            this.m_binder.Add(si, e => e.ReversedCtrl, this.ConfigReversedCtrl);
            this.m_binder.Add(si, e => e.UniformityText, this.ConfigUniformityText);
            this.m_binder.Add(si, e => e.EnabledInReply, this.ConfigEnabledInReply);

            if (TiXMain.IsInstalled)
            {
                this.m_binder.Add(si, e => e.SEWithText, this.ConfigSEWithText);
                this.m_binder.Add(si, e => e.SEWithoutText, this.ConfigSEWithoutText);
            }
            else
            {
                this.ConfigSEWithText.IsEnabled = false;
                this.ConfigSEWithoutText.IsEnabled = false;
            }

            this.m_binder.FromSetting();
        }

        private void CopyRight_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo { UseShellExecute = true, FileName = "\"https://github.com/RyuaNerin/QIT\"" })?.Dispose();
            }
            catch
            {
            }
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            this.m_binder.ToSetting();

            Settings.Instance.ApplyToRegistry();
            Settings.Instance.Save();
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
