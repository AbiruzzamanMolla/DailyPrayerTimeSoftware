using System;
using System.Windows;
using Microsoft.Win32;
using Microsoft.Toolkit.Uwp.Notifications;
using Batoulapps.Adhan;
using System.Linq;
using System.Windows.Controls;
using System.Net.Http;
using System.IO;
using System.Threading.Tasks;

namespace DailyPrayerTime.Native
{
    public partial class SettingsWindow : Window
    {
        private CombinedPrayerTimes? _today;
        private CombinedPrayerTimes? _tomorrow;
        private System.Windows.Media.MediaPlayer _testPlayer = new System.Windows.Media.MediaPlayer();

        public SettingsWindow(CombinedPrayerTimes? today, CombinedPrayerTimes? tomorrow)
        {
            _today = today;
            _tomorrow = tomorrow;
            InitializeComponent();
            InitializeTimeInputs();
            LoadForm();
            
            // Wire up time format change event
            TimeFormatInput.SelectionChanged += TimeFormatInput_SelectionChanged;
        }

        private void InitializeTimeInputs()
        {
            // Initial population is now handled by PopulateHints which is called in LoadForm
            UpdateHourItems();
        }

        private void UpdateHourItems()
        {
            var selectedItem = TimeFormatInput.SelectedItem as System.Windows.Controls.ComboBoxItem;
            bool is24h = selectedItem?.Content.ToString()?.Contains("24") ?? false;
            
            // If we have prayer times, we will filter for each combo specifically in PopulateHints
            // For now, these are the defaults:
            var defaultHours = is24h 
                ? Enumerable.Range(0, 24).Select(i => i.ToString("D2")).ToList()
                : Enumerable.Range(1, 12).Select(i => i.ToString("D2")).ToList();

            foreach (var combo in new[] { FajrHourInput, DhuhrHourInput, AsrHourInput, MaghribHourInput, IshaHourInput })
            {
                if (combo.ItemsSource == null) combo.ItemsSource = defaultHours;
            }

            var visibility = is24h ? Visibility.Collapsed : Visibility.Visible;
            foreach (var combo in new[] { FajrAmPmInput, DhuhrAmPmInput, AsrAmPmInput, MaghribAmPmInput, IshaAmPmInput })
            {
                combo.Visibility = visibility;
                if (combo.SelectedIndex == -1) combo.SelectedIndex = 0;
            }

            PopulateHints();
        }

        private void TimeFormatInput_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = TimeFormatInput.SelectedItem as System.Windows.Controls.ComboBoxItem;
            if (selectedItem == null) return;

            bool to24h = selectedItem.Content.ToString()?.Contains("24") ?? false;
            
            // Simple approach: temporarily store current times as 24h internal
            var fajr = GetTimeFromInputs(FajrHourInput, FajrMinuteInput, FajrAmPmInput, !to24h);
            var dhuhr = GetTimeFromInputs(DhuhrHourInput, DhuhrMinuteInput, DhuhrAmPmInput, !to24h);
            var asr = GetTimeFromInputs(AsrHourInput, AsrMinuteInput, AsrAmPmInput, !to24h);
            var maghrib = GetTimeFromInputs(MaghribHourInput, MaghribMinuteInput, MaghribAmPmInput, !to24h);
            var isha = GetTimeFromInputs(IshaHourInput, IshaMinuteInput, IshaAmPmInput, !to24h);

            UpdateHourItems();
            PopulateHints();

            // Set them back
            SetTimeToInputs(fajr, FajrHourInput, FajrMinuteInput, FajrAmPmInput, to24h);
            SetTimeToInputs(dhuhr, DhuhrHourInput, DhuhrMinuteInput, DhuhrAmPmInput, to24h);
            SetTimeToInputs(asr, AsrHourInput, AsrMinuteInput, AsrAmPmInput, to24h);
            SetTimeToInputs(maghrib, MaghribHourInput, MaghribMinuteInput, MaghribAmPmInput, to24h);
            SetTimeToInputs(isha, IshaHourInput, IshaMinuteInput, IshaAmPmInput, to24h);
        }

        private static string GetTimeFromInputs(System.Windows.Controls.ComboBox h, System.Windows.Controls.ComboBox m, System.Windows.Controls.ComboBox ampm, bool was12h)
        {
            try
            {
                int hh = int.Parse(h.SelectedItem as string ?? "0");
                int mm = int.Parse(m.SelectedItem as string ?? "0");
                if (was12h)
                {
                    string ap = (ampm.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString() ?? "AM";
                    if (ap == "PM" && hh < 12) hh += 12;
                    else if (ap == "AM" && hh == 12) hh = 0;
                }
                return $"{hh:D2}:{mm:D2}";
            }
            catch { return "00:00"; }
        }

        private static void SetTimeToInputs(string time, System.Windows.Controls.ComboBox h, System.Windows.Controls.ComboBox m, System.Windows.Controls.ComboBox ampm, bool isUI24h)
        {
            if (string.IsNullOrEmpty(time) || !time.Contains(":")) return;
            var parts = time.Split(':');
            int hh = int.Parse(parts[0]);
            string mm = parts[1];

            if (isUI24h)
            {
                h.SelectedItem = hh.ToString("D2");
            }
            else
            {
                string ap = hh >= 12 ? "PM" : "AM";
                int h12 = hh % 12;
                if (h12 == 0) h12 = 12;
                h.SelectedItem = h12.ToString("D2");
                foreach (System.Windows.Controls.ComboBoxItem item in ampm.Items)
                {
                    if (item.Content.ToString() == ap) { ampm.SelectedItem = item; break; }
                }
            }
            m.SelectedItem = mm;
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
            UseDeskBandInput.IsChecked = s.UseDeskBand;
            IntegratedTaskbarInput.IsChecked = s.UseIntegratedTaskbar;
            ShowHeroGridInput.IsChecked = s.ShowHeroPrayerGrid;
            NotificationsInput.IsChecked = s.NotificationsEnabled;
            
            GradStartInput.Text = s.GradientStart;
            GradEndInput.Text = s.GradientEnd;
            PrimaryColorInput.Text = s.PrimaryColor;
            SecondaryColorInput.Text = s.SecondaryColor;

            AutoStartInput.IsChecked = s.AutoStart;
            SilentStartInput.IsChecked = s.SilentStart;
            SilentStartInput.IsEnabled = s.AutoStart;
            
            AutoStartInput.Checked += (snd, evt) => SilentStartInput.IsEnabled = true;
            AutoStartInput.Unchecked += (snd, evt) => SilentStartInput.IsEnabled = false;

            ExternalApiInput.IsChecked = s.UseExternalApi;

            bool is24h = s.TimeFormat == "24h";
            TimeFormatInput.SelectedIndex = is24h ? 1 : 0;
            
            SetTimeToInputs(s.FajrJamaatTime, FajrHourInput, FajrMinuteInput, FajrAmPmInput, is24h);
            SetTimeToInputs(s.DhuhrJamaatTime, DhuhrHourInput, DhuhrMinuteInput, DhuhrAmPmInput, is24h);
            SetTimeToInputs(s.AsrJamaatTime, AsrHourInput, AsrMinuteInput, AsrAmPmInput, is24h);
            SetTimeToInputs(s.MaghribJamaatTime, MaghribHourInput, MaghribMinuteInput, MaghribAmPmInput, is24h);
            SetTimeToInputs(s.IshaJamaatTime, IshaHourInput, IshaMinuteInput, IshaAmPmInput, is24h);

            JamaatPopupOffsetInput.Text = s.JamaatPopupOffset.ToString();

            AdhanAlarmEnabledInput.IsChecked = s.AdhanAlarmEnabled;
            AdhanAlarmOffsetInput.Text = s.AdhanAlarmOffset.ToString();
            AdhanSoundPathInput.Text = s.AdhanSoundPath;
            AdhanPopupEnabledInput.IsChecked = s.AdhanPopupEnabled;
            FajrAdhanSoundPathInput.Text = s.FajrAdhanSoundPath;
            TahajjudAdhanEnabledInput.IsChecked = s.TahajjudAdhanEnabled;
            TahajjudAdhanSoundPathInput.Text = s.TahajjudAdhanSoundPath;

            PopulateHints();
        }

        private void PopulateHints()
        {
            if (_today == null || _tomorrow == null) return;
            var selectedItem = TimeFormatInput?.SelectedItem as System.Windows.Controls.ComboBoxItem;
            bool is24h = selectedItem?.Content.ToString()?.Contains("24") ?? false;
            string fmt = is24h ? "HH:mm" : "hh:mm tt";

            FajrRangeHint.Text = $"Today: {_today.Fajr.ToString(fmt)} - {_today.Sunrise.ToString(fmt)}";
            DhuhrRangeHint.Text = $"Today: {_today.Dhuhr.ToString(fmt)} - {_today.Asr.ToString(fmt)}";
            AsrRangeHint.Text = $"Today: {_today.Asr.ToString(fmt)} - {_today.Maghrib.ToString(fmt)}";
            MaghribRangeHint.Text = $"Today: {_today.Maghrib.ToString(fmt)} - {_today.Isha.ToString(fmt)}";
            IshaRangeHint.Text = $"Today: {_today.Isha.ToString(fmt)} - {_tomorrow.Fajr.ToString(fmt)}";

            FilterCombo(FajrHourInput, FajrMinuteInput, FajrAmPmInput, _today.Fajr, _today.Sunrise, is24h);
            FilterCombo(DhuhrHourInput, DhuhrMinuteInput, DhuhrAmPmInput, _today.Dhuhr, _today.Asr, is24h);
            FilterCombo(AsrHourInput, AsrMinuteInput, AsrAmPmInput, _today.Asr, _today.Maghrib, is24h);
            FilterCombo(MaghribHourInput, MaghribMinuteInput, MaghribAmPmInput, _today.Maghrib, _today.Isha, is24h);
            FilterCombo(IshaHourInput, IshaMinuteInput, IshaAmPmInput, _today.Isha, _tomorrow.Fajr, is24h);
        }

        private void FilterCombo(System.Windows.Controls.ComboBox h, System.Windows.Controls.ComboBox m, System.Windows.Controls.ComboBox ampm, DateTime start, DateTime end, bool is24h)
        {
            string? currentH = h.SelectedItem as string;
            string? currentM = m.SelectedItem as string;

            var validHours = GetValidHours(start, end, is24h);
            h.ItemsSource = validHours;
            if (currentH != null && validHours.Contains(currentH)) h.SelectedItem = currentH;
            else h.SelectedIndex = 0;

            UpdateMinuteCombo(h, m, ampm, start, end, is24h);
            if (currentM != null && (m.ItemsSource as List<string>)?.Contains(currentM) == true) m.SelectedItem = currentM;
        }

        private static List<string> GetValidHours(DateTime start, DateTime end, bool is24h)
        {
            var hours = new List<int>();
            DateTime current = new DateTime(start.Year, start.Month, start.Day, start.Hour, 0, 0, DateTimeKind.Local);
            while (current <= end)
            {
                if (!hours.Contains(current.Hour)) hours.Add(current.Hour);
                current = current.AddHours(1);
            }

            if (is24h) return hours.Select(h => h.ToString("D2")).ToList();
            
            return hours.Select(h => {
                int h12 = h % 12;
                if (h12 == 0) h12 = 12;
                return h12.ToString("D2");
            }).Distinct().OrderBy(s => s).ToList();
        }

        private void HourInput_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_today == null || _tomorrow == null) return;
            var selectedItem = TimeFormatInput?.SelectedItem as System.Windows.Controls.ComboBoxItem;
            bool is24h = selectedItem?.Content.ToString()?.Contains("24") ?? false;

            if (sender == FajrHourInput || sender == FajrAmPmInput) UpdateMinuteCombo(FajrHourInput, FajrMinuteInput, FajrAmPmInput, _today.Fajr, _today.Sunrise, is24h);
            else if (sender == DhuhrHourInput || sender == DhuhrAmPmInput) UpdateMinuteCombo(DhuhrHourInput, DhuhrMinuteInput, DhuhrAmPmInput, _today.Dhuhr, _today.Asr, is24h);
            else if (sender == AsrHourInput || sender == AsrAmPmInput) UpdateMinuteCombo(AsrHourInput, AsrMinuteInput, AsrAmPmInput, _today.Asr, _today.Maghrib, is24h);
            else if (sender == MaghribHourInput || sender == MaghribAmPmInput) UpdateMinuteCombo(MaghribHourInput, MaghribMinuteInput, MaghribAmPmInput, _today.Maghrib, _today.Isha, is24h);
            else if (sender == IshaHourInput || sender == IshaAmPmInput) UpdateMinuteCombo(IshaHourInput, IshaMinuteInput, IshaAmPmInput, _today.Isha, _tomorrow.Fajr, is24h);
        }

        private static void UpdateMinuteCombo(System.Windows.Controls.ComboBox h, System.Windows.Controls.ComboBox m, System.Windows.Controls.ComboBox ampm, DateTime start, DateTime end, bool is24h)
        {
            string? selH = h.SelectedItem as string;
            if (selH == null) return;

            int hour24 = int.Parse(selH);
            if (!is24h)
            {
                string ap = (ampm.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString() ?? "AM";
                if (ap == "PM" && hour24 < 12) hour24 += 12;
                else if (ap == "AM" && hour24 == 12) hour24 = 0;
            }

            var minutes = new List<string>();
            for (int min = 0; min < 60; min++)
            {
                DateTime check;
                if (hour24 < start.Hour && hour24 <= end.Hour && start.Hour > end.Hour) 
                    check = new DateTime(end.Year, end.Month, end.Day, hour24, min, 0, DateTimeKind.Local);
                else
                    check = new DateTime(start.Year, start.Month, start.Day, hour24, min, 0, DateTimeKind.Local);

                if (check >= start && check <= end)
                {
                    minutes.Add(min.ToString("D2"));
                }
            }
            m.ItemsSource = minutes;
            if (m.SelectedIndex == -1) m.SelectedIndex = 0;
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
            s.UseDeskBand = UseDeskBandInput.IsChecked ?? false;
            s.UseIntegratedTaskbar = IntegratedTaskbarInput.IsChecked ?? false;
            s.ShowHeroPrayerGrid = ShowHeroGridInput.IsChecked ?? true;
            s.NotificationsEnabled = NotificationsInput.IsChecked ?? true;
            
            bool newAutoStart = AutoStartInput.IsChecked ?? false;
            bool newSilentStart = SilentStartInput.IsChecked ?? false;
            
            if (s.AutoStart != newAutoStart || s.SilentStart != newSilentStart)
            {
                s.AutoStart = newAutoStart;
                s.SilentStart = newSilentStart;
                SetAutoStart(newAutoStart, newSilentStart);
            }

            s.GradientStart = GradStartInput.Text;
            s.GradientEnd = GradEndInput.Text;
            s.PrimaryColor = PrimaryColorInput.Text;
            s.SecondaryColor = SecondaryColorInput.Text;

            s.AutoStart = AutoStartInput.IsChecked ?? false;
            s.SilentStart = SilentStartInput.IsChecked ?? false;
            s.UseExternalApi = ExternalApiInput.IsChecked ?? false;
            
            bool is24h = TimeFormatInput.SelectedIndex == 1;
            s.TimeFormat = is24h ? "24h" : "12h";

            s.FajrJamaatTime = GetTimeFromInputs(FajrHourInput, FajrMinuteInput, FajrAmPmInput, !is24h);
            s.DhuhrJamaatTime = GetTimeFromInputs(DhuhrHourInput, DhuhrMinuteInput, DhuhrAmPmInput, !is24h);
            s.AsrJamaatTime = GetTimeFromInputs(AsrHourInput, AsrMinuteInput, AsrAmPmInput, !is24h);
            s.MaghribJamaatTime = GetTimeFromInputs(MaghribHourInput, MaghribMinuteInput, MaghribAmPmInput, !is24h);
            s.IshaJamaatTime = GetTimeFromInputs(IshaHourInput, IshaMinuteInput, IshaAmPmInput, !is24h);

            if (int.TryParse(JamaatPopupOffsetInput.Text, out int offset)) s.JamaatPopupOffset = offset;

            s.AdhanAlarmEnabled = AdhanAlarmEnabledInput.IsChecked ?? false;
            if (int.TryParse(AdhanAlarmOffsetInput.Text, out int aao)) s.AdhanAlarmOffset = aao;
            s.AdhanSoundPath = AdhanSoundPathInput.Text;
            s.AdhanPopupEnabled = AdhanPopupEnabledInput.IsChecked ?? true;
            s.FajrAdhanSoundPath = FajrAdhanSoundPathInput.Text;
            s.TahajjudAdhanEnabled = TahajjudAdhanEnabledInput.IsChecked ?? false;
            s.TahajjudAdhanSoundPath = TahajjudAdhanSoundPathInput.Text;

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

        private void BrowseAdhan_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "Audio files (*.mp3;*.wav)|*.mp3;*.wav|All files (*.*)|*.*";
            bool? result = openFileDialog.ShowDialog();
            if (result == true)
            {
                AdhanSoundPathInput.Text = openFileDialog.FileName;
            }
        }
        
        public static void SetAutoStart(bool enable, bool silentStart = false)
        {
            try
            {
                using RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true)!;
                if (enable)
                {
                    string? exePath = Environment.ProcessPath;
                    if (string.IsNullOrEmpty(exePath)) return;

                    string args = silentStart ? " -silent" : "";
                    key.SetValue("DailyPrayerTimeNative", $"\"{exePath}\"{args}");
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
                
                var items = Newtonsoft.Json.JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JArray>(response);
                if (items != null && items.Count > 0)
                {
                    var firstItem = items[0];
                    if (firstItem != null)
                    {
                        LocationNameInput.Text = firstItem["display_name"]?.ToString();
                        LatInput.Text = firstItem["lat"]?.ToString();
                        LngInput.Text = firstItem["lon"]?.ToString();
                    }
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

        private void TestPopup_Click(object sender, RoutedEventArgs e)
        {
            var popup = new CongregationTimerPopup("Test Prayer", DateTime.Now.AddMinutes(5));
            popup.Owner = this;
            popup.Show();
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            try {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
                e.Handled = true;
            } catch { /* Handle error */ }
        }
        private void PickColor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is string tag)
            {
                using (var dialog = new System.Windows.Forms.ColorDialog())
                {
                    if (tag == "Primary") dialog.Color = System.Drawing.ColorTranslator.FromHtml(PrimaryColorInput?.Text ?? "#000000");
                    else dialog.Color = System.Drawing.ColorTranslator.FromHtml(SecondaryColorInput?.Text ?? "#000000");

                    if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        string hex = $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}";
                        if (tag == "Primary" && PrimaryColorInput != null) PrimaryColorInput.Text = hex;
                        else if (tag == "Secondary" && SecondaryColorInput != null) SecondaryColorInput.Text = hex;
                        else if (tag == "GradStart" && GradStartInput != null) GradStartInput.Text = hex;
                        else if (tag == "GradEnd" && GradEndInput != null) GradEndInput.Text = hex;
                    }
                }
            }
        }

        private void AdhanPresetCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AdhanPresetCombo.SelectedItem is ComboBoxItem item && item.Tag is string fileName && !string.IsNullOrEmpty(fileName))
            {
                if (fileName == "CUSTOM") return;

                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string targetPath = Path.Combine(baseDir, "Assets", "Adhan", fileName);

                if (File.Exists(targetPath))
                {
                    AdhanSoundPathInput.Text = targetPath;
                    
                    // If it's a Fajr track, set it for Fajr too
                    if (fileName.ToLower().Contains("fajer"))
                    {
                        FajrAdhanSoundPathInput.Text = targetPath;
                    }
                }
                else
                {
                    System.Windows.MessageBox.Show("Selected built-in adhan file not found in Assets/Adhan folder.", "File Missing", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void FajrBrowseAdhan_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "Audio files (*.mp3;*.wav)|*.mp3;*.wav|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true) FajrAdhanSoundPathInput.Text = openFileDialog.FileName;
        }

        private void TahajjudBrowseAdhan_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "Audio files (*.mp3;*.wav)|*.mp3;*.wav|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true) TahajjudAdhanSoundPathInput.Text = openFileDialog.FileName;
        }

        private void TestAdhan_Click(object sender, RoutedEventArgs e)
        {
            PlayTestSound(AdhanSoundPathInput.Text, "General Adhan", "Start - End", "00:00 AM");
        }

        private void TestFajrAdhan_Click(object sender, RoutedEventArgs e)
        {
            PlayTestSound(FajrAdhanSoundPathInput.Text, "Fajr Adhan", "Start - End", "00:00 AM");
        }

        private void TestTahajjudAdhan_Click(object sender, RoutedEventArgs e)
        {
            PlayTestSound(TahajjudAdhanSoundPathInput.Text, "Tahajjud", "Start - End", "N/A");
        }

        private void PlayTestSound(string path, string prayerName = "Test Prayer", string range = "00:00 - 00:00", string jamaat = "00:00")
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                System.Windows.MessageBox.Show("Please select a valid sound file first.", "Test Sound", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (AdhanPopupEnabledInput.IsChecked == true)
            {
                var popup = new AdhanNotificationWindow(prayerName, range, jamaat, path);
                popup.Show();
            }
            else
            {
                try
                {
                    _testPlayer.Open(new Uri(path));
                    _testPlayer.Play();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Failed to play sound: {ex.Message}");
                }
            }
        }

        private async void CheckForUpdates_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as System.Windows.Controls.Button;
            if (btn == null) return;

            btn.Content = "Checking...";
            btn.IsEnabled = false;

            try
            {
                var updateInfo = await UpdateService.CheckForUpdateAsync();
                if (updateInfo.IsUpdateAvailable)
                {
                    var result = System.Windows.MessageBox.Show(
                        $"A new version ({updateInfo.LatestVersion}) is available!\n\nWould you like to open the download page?",
                        "Update Available",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(updateInfo.ReleaseUrl) { UseShellExecute = true });
                    }
                }
                else
                {
                    System.Windows.MessageBox.Show(
                        "You are already using the latest version.",
                        "Check for Updates",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Failed to check for updates: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                btn.Content = "Check for Updates";
                btn.IsEnabled = true;
            }
        }

        private void SupportMe_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://www.supportkori.com/abiruzzaman") { UseShellExecute = true });
            }
            catch { /* Ignore */ }
        }

        private void SettingsTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SettingsButtonsPanel == null) return;
            
            // Index 2 is "Support & Contact"
            if (SettingsTabControl.SelectedIndex == 2)
            {
                SettingsButtonsPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                SettingsButtonsPanel.Visibility = Visibility.Visible;
            }
        }
    }
}
