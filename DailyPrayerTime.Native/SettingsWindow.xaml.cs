using System;
using System.Windows;
using Microsoft.Win32;
using Microsoft.Toolkit.Uwp.Notifications;

namespace DailyPrayerTime.Native
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            LoadForm();
        }

        private void LoadForm()
        {
            var s = SettingsManager.Current;
            LocationNameInput.Text = s.LocationName;
            LatInput.Text = s.Latitude.ToString();
            LngInput.Text = s.Longitude.ToString();

            // Setup method dropdown
            foreach (System.Windows.Controls.ComboBoxItem item in MethodInput.Items)
            {
                if (item.Content.ToString()!.ToUpper() == s.Method.ToUpper())
                {
                    MethodInput.SelectedItem = item;
                }
            }

            MadhabInput.SelectedIndex = s.School == 1 ? 1 : 0;
            OverlayInput.IsChecked = s.ShowOverlay;
            NotificationsInput.IsChecked = s.NotificationsEnabled;
            AutoStartInput.IsChecked = s.AutoStart;

            GradStartInput.Text = s.GradientStart;
            GradEndInput.Text = s.GradientEnd;
            PrimaryColorInput.Text = s.PrimaryColor;
            SecondaryColorInput.Text = s.SecondaryColor;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var s = SettingsManager.Current;

            s.LocationName = LocationNameInput.Text;
            if (double.TryParse(LatInput.Text, out double lat)) s.Latitude = lat;
            if (double.TryParse(LngInput.Text, out double lng)) s.Longitude = lng;

            if (MethodInput.SelectedItem is System.Windows.Controls.ComboBoxItem methodItem)
            {
                s.Method = methodItem.Content.ToString()!.ToUpper();
            }

            s.School = MadhabInput.SelectedIndex; // 0=Shafi, 1=Hanafi
            
            s.ShowOverlay = OverlayInput.IsChecked ?? true;
            s.NotificationsEnabled = NotificationsInput.IsChecked ?? true;
            
            bool newAutoStart = AutoStartInput.IsChecked ?? false;
            if (s.AutoStart != newAutoStart)
            {
                s.AutoStart = newAutoStart;
                SetAutoStart(newAutoStart);
            }

            s.GradientStart = GradStartInput.Text;
            s.GradientEnd = GradEndInput.Text;
            s.PrimaryColor = PrimaryColorInput.Text;
            s.SecondaryColor = SecondaryColorInput.Text;

            SettingsManager.Save();
            this.DialogResult = true;
            this.Close();
        }

        private void TestNotification_Click(object sender, RoutedEventArgs e)
        {
            new Microsoft.Toolkit.Uwp.Notifications.ToastContentBuilder()
                .AddText("Prayer Notification Test")
                .AddText("This is a test notification from Daily Prayer Timer.")
                .Show();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
        
        private void SetAutoStart(bool enable)
        {
            try
            {
                using RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true)!;
                if (enable)
                {
                    string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName;
                    key.SetValue("DailyPrayerTimeNative", "\"" + exePath + "\"");
                }
                else
                {
                    key.DeleteValue("DailyPrayerTimeNative", false);
                }
            }
            catch { /* handle permission error */ }
        }

        private async void SearchLocation_Click(object sender, RoutedEventArgs e)
        {
            string query = LocationNameInput.Text.Trim();
            if (query.Length < 3)
            {
                System.Windows.MessageBox.Show("Please enter at least 3 characters to search.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var btn = sender as System.Windows.Controls.Button;
                if (btn != null) btn.Content = "...";

                string apiKey = "pk.caf5ae7f1137c95c5354d716da66d44d";
                string url = $"https://api.locationiq.com/v1/autocomplete?key={apiKey}&q={Uri.EscapeDataString(query)}&limit=1&dedupe=1";

                using var client = new System.Net.Http.HttpClient();
                var response = await client.GetStringAsync(url);
                
                var items = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(response);
                if (items != null && items.Count > 0)
                {
                    var firstItem = items[0];
                    LocationNameInput.Text = firstItem.display_name;
                    LatInput.Text = firstItem.lat;
                    LngInput.Text = firstItem.lon;
                }
                else
                {
                    System.Windows.MessageBox.Show("No results found.", "Search", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Search failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                var btn = sender as System.Windows.Controls.Button;
                if (btn != null) btn.Content = "Search";
            }
        }
        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            try {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
                e.Handled = true;
            } catch { /* Handle error */ }
        }
    }
}
