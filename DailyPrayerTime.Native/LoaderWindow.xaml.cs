using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;

namespace DailyPrayerTime.Native
{
    /// <summary>
    /// Interaction logic for LoaderWindow.xaml
    /// </summary>
    public partial class LoaderWindow : Window
    {
        public LoaderWindow()
        {
            InitializeComponent();
            LoadVersionInfo();
        }

        private void LoadVersionInfo()
        {
            try
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                if (version != null)
                {
                    VersionText.Text = $"v{version.Major}.{version.Minor}.{version.Build}";
                }
            }
            catch
            {
                VersionText.Text = "v2.1.0";
            }
        }

        private void JoinTelegram_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://t.me/dailyprayertimersoftware");
        }

        private void Support_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://www.supportkori.com/abiruzzaman");
        }

        private void Website_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://abiruzzamanmolla.github.io/DailyPrayerTimeSoftware/");
        }

        private void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Could not open link: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
