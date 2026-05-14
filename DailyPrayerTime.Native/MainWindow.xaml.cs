using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using System.Diagnostics;
using Forms = System.Windows.Forms;
using Batoulapps.Adhan;
using Microsoft.Toolkit.Uwp.Notifications;
using WColor = System.Windows.Media.Color;
using WColorConverter = System.Windows.Media.ColorConverter;
using System.Net.Http;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DailyPrayerTime.Native
{
    using DailyPrayerTime.Native.Models;
    using DailyPrayerTime.Native.Services;
    public partial class MainWindow : Window
    {
        private const string TimeFmtFull = "hh:mm tt";
        private const string TimeFmtShort = "hh:mm";

        private DispatcherTimer? _timer;
        private CombinedPrayerTimes? _todayPrayerTimes;
        private CombinedPrayerTimes? _tomorrowPrayerTimes;
        private OverlayWindow? _overlay;
        private TaskbarWindow? _taskbarWindow;
        private EnhancedTaskbarWindow? _enhancedTaskbarWindow;
        private Prayer _lastJamaatPopupPrayer = Prayer.NONE;
        private Forms.NotifyIcon? _notifyIcon;
        private CongregationTimerPopup? _activeJamaatPopup;
        
        private bool _sunriseProhibActive = false;
        private bool _zawalProhibActive = false;
        private bool _sunsetProhibActive = false;
        private string _prohibNotifyDate = "";

        private static string GetTimeFmt() => SettingsManager.Current.TimeFormat == "24h" ? "HH:mm" : "hh:mm tt";
        private MediaPlayer _adhanPlayer = new MediaPlayer();
        private Prayer _lastAdhanPrayer = Prayer.NONE;
        private string _lastAdhanDate = "";
        private Prayer _lastStartNotificationPrayer = Prayer.NONE;
        private Prayer _lastEndSoundPrayer = Prayer.NONE;
        private string _lastTahajjudSoundDate = "";
        private Prayer _lastJamaatNotificationID = Prayer.NONE;
        private Prayer _lastEndNotificationID = Prayer.NONE;
        private Prayer _lastDeedPopupPrayer = Prayer.NONE;
        private string _lastDeedPopupDate = "";
        private string _lastSummaryPopupDate = "";
        private Prayer _lastPreAdhanNotificationID = Prayer.NONE;
        private string _lastShuruqWarningDate = "";
        private string _lastSunriseNotificationDate = "";
        private string _lastEidTakbeerDate = "";
        private UpdateInfo? _currentUpdate;
        private bool _isZenMode = false;
        private bool _isFullScreen = false;
        private Rect? _savedWindowBounds;
        private bool _isRamadanMode = false;
        private bool _isFastingNoteExpanded = false;
        private bool _isTrackerMode = false;
        private DailyDeeds _currentDeeds;

        private bool IsWindows11 => Environment.OSVersion.Version.Major >= 10 && Environment.OSVersion.Version.Build >= 22000;

        public MainWindow()
        {
            SettingsManager.Load();
            InitializeComponent();
            
            // Apply initial tab state
            if (TabHome.IsChecked == true) TabHome_Checked(null, null);
            else if (TabPrayers.IsChecked == true) TabPrayers_Checked(null, null);
            else if (TabTracker.IsChecked == true) TabTracker_Checked(null, null);

            _currentDeeds = TrackerService.Instance.LoadDay(DateTime.Today);
            this.Height = SystemParameters.WorkArea.Height * 0.85;
            ApplySettingsTheme();
            SetupTimer();
            SetupTrayIcon();
            _ = CalculatePrayerTimes();
            Task.Run(async () => await DownloadDefaultAdhan());
            _ = CheckForUpdates();
            ManageOverlay();
            ManageIntegratedTaskbar();
            ManageEnhancedTaskbar();

            // Refresh AutoStart registry path in case the app was moved (Portable Mode support)
            if (SettingsManager.Current.AutoStart)
            {
                SettingsWindow.SetAutoStart(true, SettingsManager.Current.SilentStart);
            }

            InitBasmalaHeader();
            SyncToolbarIcons();
        }

        private async Task CheckForUpdates()
        {
            try
            {
                _currentUpdate = await UpdateService.CheckForUpdateAsync();
                if (_currentUpdate != null && _currentUpdate.IsUpdateAvailable)
                {
                    Dispatcher.Invoke(() =>
                    {
                        UpdateVersionText.Text = string.Format(LocalizationManager.Instance.GetString("Update_VersionAvailable"), _currentUpdate.LatestVersion);
                        UpdateBanner.Visibility = Visibility.Visible;
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Update check UI error: " + ex.Message);
            }
        }

        private void SetupTrayIcon()
        {
            _notifyIcon = new Forms.NotifyIcon();
            try {
                _notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(Process.GetCurrentProcess().MainModule?.FileName ?? "icon.ico");
            } catch { /* Fallback */ }
            _notifyIcon.Visible = true;
            _notifyIcon.Text = LocalizationManager.Instance.GetString("AppTitle");

            var cms = new Forms.ContextMenuStrip();
            
            cms.Items.Add(LocalizationManager.Instance.GetString("Tray_Open"), null, (s, e) => { Show(); WindowState = WindowState.Normal; Activate(); });
            
            cms.Items.Add(LocalizationManager.Instance.GetString("Tray_Settings"), null, async (s, e) => { 
                var sw = new SettingsWindow(_todayPrayerTimes, _tomorrowPrayerTimes);
                if (sw.ShowDialog() == true) {
                    ApplySettingsTheme();
                    await CalculatePrayerTimes();
                    ManageOverlay();
                    ManageIntegratedTaskbar();
                    ManageEnhancedTaskbar();
                }
            });

            cms.Items.Add(new Forms.ToolStripSeparator());

            var overlayItem = new Forms.ToolStripMenuItem(LocalizationManager.Instance.GetString("Tray_ShowOverlay"));
            overlayItem.CheckOnClick = true;
            overlayItem.Checked = SettingsManager.Current.ShowOverlay;
            overlayItem.Click += (s, e) => {
                SettingsManager.Current.ShowOverlay = overlayItem.Checked;
                SettingsManager.Save();
                ManageOverlay();
            };
            cms.Items.Add(overlayItem);

            var deskbandItem = new Forms.ToolStripMenuItem(LocalizationManager.Instance.GetString("Tray_ShowDeskBand"));
            deskbandItem.CheckOnClick = true;
            deskbandItem.Checked = SettingsManager.Current.UseDeskBand;
            deskbandItem.Click += (s, e) => {
                SettingsManager.Current.UseDeskBand = deskbandItem.Checked;
                SettingsManager.Save();
                // DeskBand is handled by Explorer, we just update data
            };
            cms.Items.Add(deskbandItem);

            var integratedItem = new Forms.ToolStripMenuItem(LocalizationManager.Instance.GetString("Tray_ShowIntegratedTaskbar"));
            integratedItem.CheckOnClick = true;
            integratedItem.Checked = SettingsManager.Current.UseIntegratedTaskbar;
            integratedItem.Click += (s, e) => {
                SettingsManager.Current.UseIntegratedTaskbar = integratedItem.Checked;
                SettingsManager.Save();
                ManageIntegratedTaskbar();
            };
            cms.Items.Add(integratedItem);

            var enhancedItem = new Forms.ToolStripMenuItem(LocalizationManager.Instance.GetString("Tray_ShowEnhancedTaskbar"));
            enhancedItem.CheckOnClick = true;
            enhancedItem.Checked = SettingsManager.Current.UseEnhancedTaskbar;
            enhancedItem.Click += (s, e) => {
                SettingsManager.Current.UseEnhancedTaskbar = enhancedItem.Checked;
                SettingsManager.Save();
                ManageEnhancedTaskbar();
            };
            cms.Items.Add(enhancedItem);

            cms.Items.Add(new Forms.ToolStripSeparator());
            cms.Items.Add(LocalizationManager.Instance.GetString("Tray_Exit"), null, (s, e) => System.Windows.Application.Current.Shutdown());

            _notifyIcon.ContextMenuStrip = cms;

            // Ensure checkmarks stay in sync when menu opens
            cms.Opening += (s, e) => {
                overlayItem.Checked = SettingsManager.Current.ShowOverlay;
                deskbandItem.Checked = SettingsManager.Current.UseDeskBand;
                integratedItem.Checked = SettingsManager.Current.UseIntegratedTaskbar;
                enhancedItem.Checked = SettingsManager.Current.UseEnhancedTaskbar;
            };

            _notifyIcon.DoubleClick += (s, e) => { ShowWindow(); };
            
            this.Closing += (s, e) =>
            {
                e.Cancel = true;
                this.Hide();
            };
        }

        private void ShowWindow()
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                this.Hide();
                if (_notifyIcon != null)
                {
                    _notifyIcon.ShowBalloonTip(2000, LocalizationManager.Instance.GetString("AppTitle"), LocalizationManager.Instance.GetString("Tray_RunningInBackground"), Forms.ToolTipIcon.Info);
                }
            }
            base.OnStateChanged(e);
        }



        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void RamadanMode_Click(object sender, RoutedEventArgs e)
        {
            _isRamadanMode = !_isRamadanMode;
            SyncToolbarIcons();
            HeroRamadanGrid.Visibility = _isRamadanMode ? Visibility.Visible : Visibility.Collapsed;
            HeroDefaultGrid.Visibility = _isRamadanMode ? Visibility.Collapsed : Visibility.Visible;
            RefreshUIDisplay();
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.F11)
            {
                FullScreen_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
        }

        private void FullScreen_Click(object sender, RoutedEventArgs e)
        {
            _isFullScreen = !_isFullScreen;
            
            if (_isFullScreen)
            {
                _savedWindowBounds = new System.Windows.Rect(this.Left, this.Top, this.Width, this.Height);

                this.WindowStyle = WindowStyle.None;
                this.ResizeMode = ResizeMode.NoResize;
                this.Topmost = true;
                this.WindowState = WindowState.Normal;

                this.Left = 0;
                this.Top = 0;
                this.Width = SystemParameters.PrimaryScreenWidth;
                this.Height = SystemParameters.PrimaryScreenHeight;

                MainGrid.RowDefinitions[0].Height = new System.Windows.GridLength(0);
                FooterRow.Height = new System.Windows.GridLength(0);
            }
            else
            {
                this.Topmost = false;
                this.WindowStyle = WindowStyle.None;
                this.ResizeMode = ResizeMode.CanResize;
                this.WindowState = WindowState.Normal;

                if (_savedWindowBounds.HasValue)
                {
                    var b = _savedWindowBounds.Value;
                    this.Left = b.Left;
                    this.Top = b.Top;
                    this.Width = Math.Max(460, b.Width);
                    this.Height = Math.Max(480, b.Height);
                }
                else
                {
                    this.Width = Math.Max(460, this.Width);
                    this.Height = Math.Max(480, this.Height);
                }

                MainGrid.RowDefinitions[0].Height = new System.Windows.GridLength(40);
                FooterRow.Height = System.Windows.GridLength.Auto;
            }
        }

        private void FastingNote_Toggle_Click(object sender, RoutedEventArgs e)
        {
            _isFastingNoteExpanded = !_isFastingNoteExpanded;
            FastingNoteToggleIcon.Text = _isFastingNoteExpanded ? "▲" : "▼";
            FastingNoteExtraContent.Visibility = _isFastingNoteExpanded ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void Settings_Click(object sender, RoutedEventArgs e)
        {
            SettingsIcon.Foreground = new SolidColorBrush((WColor)WColorConverter.ConvertFromString("#34D399"));
            await Task.Delay(50); // Yield cleanly to UI thread to render green color before blocking modal opens
            
            var sw = new SettingsWindow(_todayPrayerTimes, _tomorrowPrayerTimes);
            if (sw.ShowDialog() == true)
            {
                ApplySettingsTheme();
                _ = CalculatePrayerTimes();
                ManageOverlay();
                ManageIntegratedTaskbar();
            }
            SyncToolbarIcons();
        }

        private void ZenMode_Click(object sender, RoutedEventArgs e)
        {
            _isZenMode = !_isZenMode;
            SyncToolbarIcons();

            // 1. Visibility Toggles
            // HeroBorder is now inside PrayerListScroll's MainContentStack
            // Zen mode shows ONLY the HeroBorder in the center
            var otherItemsVisibility = _isZenMode ? Visibility.Collapsed : Visibility.Visible;
            
            HighlightsHeader.Visibility = otherItemsVisibility;
            HighlightsGrid.Visibility = otherItemsVisibility;
            ProhibitedHeader.Visibility = otherItemsVisibility;
            ProhibitedGrid.Visibility = otherItemsVisibility;
            FardHeader.Visibility = Visibility.Collapsed; // Always hidden in zen
            FardCardsPanel.Visibility = Visibility.Collapsed;
            NafalHeader.Visibility = Visibility.Collapsed;
            NafalCardsPanel.Visibility = Visibility.Collapsed;
            
            UpdateBanner.Visibility = Visibility.Collapsed; 

            // 2. Layout Adjustments
            MainContentStack.VerticalAlignment = _isZenMode ? VerticalAlignment.Center : VerticalAlignment.Top;
            HeroBorder.Visibility = Visibility.Visible; // Always show hero in zen
            PrayerListScroll.Visibility = Visibility.Visible; // Must be visible to see hero now

            // 3. Background Transition
            if (_isZenMode)
            {
                // Deep dark navy background from reference image
                MainGrid.Background = new SolidColorBrush(WColor.FromRgb(17, 24, 39));
            }
            else
            {
                // Restore original gradient
                MainGrid.Background = (LinearGradientBrush)FindResource("MainGradient");
            }
        }

        /// <summary>Exits Zen Mode if currently active. Safe to call even when not in zen mode.</summary>
        private void ExitZenMode()
        {
            if (!_isZenMode) return;
            _isZenMode = false;
            SyncToolbarIcons();

            HighlightsHeader.Visibility = Visibility.Visible;
            HighlightsGrid.Visibility = Visibility.Visible;
            ProhibitedHeader.Visibility = Visibility.Visible;
            ProhibitedGrid.Visibility = Visibility.Visible;

            MainContentStack.VerticalAlignment = VerticalAlignment.Top;
            MainGrid.Background = (LinearGradientBrush)FindResource("MainGradient");
        }

        private void ManageOverlay()
        {
            bool shouldShowOverlay = SettingsManager.Current.ShowOverlay;

            if (shouldShowOverlay && _overlay == null)
            {
                _overlay = new OverlayWindow();
                _overlay.Show();
            }
            else if (!shouldShowOverlay && _overlay != null)
            {
                _overlay.Close();
                _overlay = null;
            }
        }

        private void ManageIntegratedTaskbar()
        {
            bool shouldShow = SettingsManager.Current.UseIntegratedTaskbar;

            if (shouldShow && _taskbarWindow == null)
            {
                _taskbarWindow = new TaskbarWindow();
                // Ensure data is pushed immediately as soon as the window handle is ready
                _taskbarWindow.OnReady = () => Dispatcher.Invoke(() => UpdateCountdown());
                _taskbarWindow.Show();
                UpdateCountdown();
            }
            else if (!shouldShow && _taskbarWindow != null)
            {
                _taskbarWindow.Close();
                _taskbarWindow = null;
            }
        }

        public void ManageEnhancedTaskbar()
        {
            bool shouldShow = SettingsManager.Current.UseEnhancedTaskbar;

            if (shouldShow && _enhancedTaskbarWindow == null)
            {
                _enhancedTaskbarWindow = new EnhancedTaskbarWindow();
                _enhancedTaskbarWindow.Show();
                UpdateCountdown();
            }
            else if (!shouldShow && _enhancedTaskbarWindow != null)
            {
                _enhancedTaskbarWindow.Close();
                _enhancedTaskbarWindow = null;
            }
        }

        public void OpenSettings()
        {
            Settings_Click(null, null);
        }

        private void UpdateBanner_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_currentUpdate != null && !string.IsNullOrEmpty(_currentUpdate.ReleaseUrl))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = _currentUpdate.ReleaseUrl,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(
                        string.Format(LocalizationManager.Instance.GetString("Msg_UpdateLinkFailed"), ex.Message), 
                        LocalizationManager.Instance.GetString("Title_Error"), 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Warning);
                }
            }
        }
        
        private void ApplySettingsTheme()
        {
            var s = SettingsManager.Current;
            HeroLocationText.Text = s.LocationName;
            
            var primaryBrush = new SolidColorBrush((WColor)WColorConverter.ConvertFromString(s.PrimaryColor));
            
            // Hero section background
            HeroBorder.Background = primaryBrush;

            try 
            {
                var mainBrush = (LinearGradientBrush)this.Resources["MainGradient"];
                mainBrush.GradientStops[0].Color = (WColor)WColorConverter.ConvertFromString(s.GradientStart);
                mainBrush.GradientStops[1].Color = (WColor)WColorConverter.ConvertFromString(s.GradientEnd);
            } 
            catch (Exception) { /* Invalid color format */ }

            // Refresh basmala translation any time settings (language) change
            InitBasmalaHeader();

            // Enforce tracker visibility centralizing through countdown updates
            UpdateCountdown();
        }

        private void InitBasmalaHeader()
        {
            string lang = LocalizationManager.Instance.CurrentLanguage;
            // Set the translation text from localization
            BasmalaTranslationText.Text = LocalizationManager.Instance.GetString("Header_Basmala_Translation");
            // In Arabic mode, disable hover interaction entirely (text is already Arabic)
            BasmalaHeaderGrid.Cursor = lang == "ar" ? System.Windows.Input.Cursors.Arrow : System.Windows.Input.Cursors.Hand;
            BasmalaHeaderGrid.IsEnabled = lang != "ar";
            // Reset to default state
            BasmalaArabicText.Opacity = 0.8;
            BasmalaTranslationText.Opacity = 0;
        }

        private void BasmalaHeader_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (LocalizationManager.Instance.CurrentLanguage == "ar") return;
            AnimateOpacity(BasmalaArabicText, 0.8, 0.0, 300);
            AnimateOpacity(BasmalaTranslationText, 0.0, 1.0, 300);
        }

        private void BasmalaHeader_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (LocalizationManager.Instance.CurrentLanguage == "ar") return;
            AnimateOpacity(BasmalaArabicText, 0.0, 0.8, 300);
            AnimateOpacity(BasmalaTranslationText, 1.0, 0.0, 300);
        }

        private static void AnimateOpacity(System.Windows.UIElement element, double from, double to, int durationMs)
        {
            var anim = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromMilliseconds(durationMs),
                EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseInOut }
            };
            element.BeginAnimation(System.Windows.UIElement.OpacityProperty, anim);
        }

        private void SetupTimer()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private async Task CalculatePrayerTimes()
        {
            var s = SettingsManager.Current;
            _todayPrayerTimes = await PrayerService.GetPrayerTimesAsync(s.Latitude, s.Longitude, s.Method, s.School);
            
            // Get tomorrow's times (offline calculation is fine for this)
            var coordinates = new Coordinates(s.Latitude, s.Longitude);
            var tomorrowDate = DateTime.Now.AddDays(1);
            var nextDay = new Batoulapps.Adhan.Internal.DateComponents(tomorrowDate.Year, tomorrowDate.Month, tomorrowDate.Day);
            var parameters = CalculationMethodExtensions.GetParameters(CalculationMethod.MUSLIM_WORLD_LEAGUE); // Standard
            _tomorrowPrayerTimes = new CombinedPrayerTimes(new PrayerTimes(coordinates, nextDay, parameters));
            
            Dispatcher.Invoke(() =>
            {
                HeroSubDate.Text = GetLocalizedDate(DateTime.Now);
                HeroSubDay.Text = GetLocalizedDayName(DateTime.Now);
                
                // Force local Hijri calculation for non-English locales (API returns English month names)
                HeroSubHijri.Text = GetHijriDate(_todayPrayerTimes.HijriDay, _todayPrayerTimes.HijriMonth, _todayPrayerTimes.HijriYear, _todayPrayerTimes.HijriWeekday);
                RefreshUIDisplay();
                
                // Force an explicit update to the taskbar window now that data is loaded
                if (_taskbarWindow != null)
                {
                   UpdateCountdown();
                }
            });
        }

        private static async Task DownloadDefaultAdhan()
        {
            var s = SettingsManager.Current;
            if (!string.IsNullOrEmpty(s.AdhanSoundPath)) return;

            string appData = StorageService.GetAppDataPath();
            string assets = Path.Combine(appData, "assets");
            if (!Directory.Exists(assets)) Directory.CreateDirectory(assets);

            string destPath = Path.Combine(assets, "default_adhan.mp3");
            if (File.Exists(destPath)) 
            {
                s.AdhanSoundPath = destPath;
                SettingsManager.Save();
                return;
            }

            try
            {
                using var client = new HttpClient();
                // A reliable public domain adhan MP3
                string adhanUrl = "https://www.islamcan.com/audio/adhan/azan1.mp3"; 
                var data = await client.GetByteArrayAsync(adhanUrl);
                await File.WriteAllBytesAsync(destPath, data);
                
                s.AdhanSoundPath = destPath;
                SettingsManager.Save();
                Debug.WriteLine("Default Adhan downloaded to: " + destPath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to download default Adhan: " + ex.Message);
            }
        }

        private static string GetHijriDate(int d = 0, int m = 0, int y = 0, string wd = "")
        {
            try {
                int day, month, year;

                if (d > 0 && m > 0 && y > 0)
                {
                    // Use API Provided Data
                    day = d;
                    month = m;
                    year = y;
                }
                else
                {
                    // Local Fallback
                    var now = DateTime.Now.AddDays(SettingsManager.Current.HijriAdjustment);
                    var hijri = new System.Globalization.UmAlQuraCalendar();
                    year = hijri.GetYear(now);
                    month = hijri.GetMonth(now);
                    day = hijri.GetDayOfMonth(now);
                }

                string[] months = {
                    LocalizationManager.Instance.GetString("Month_Hijri_1"),
                    LocalizationManager.Instance.GetString("Month_Hijri_2"),
                    LocalizationManager.Instance.GetString("Month_Hijri_3"),
                    LocalizationManager.Instance.GetString("Month_Hijri_4"),
                    LocalizationManager.Instance.GetString("Month_Hijri_5"),
                    LocalizationManager.Instance.GetString("Month_Hijri_6"),
                    LocalizationManager.Instance.GetString("Month_Hijri_7"),
                    LocalizationManager.Instance.GetString("Month_Hijri_8"),
                    LocalizationManager.Instance.GetString("Month_Hijri_9"),
                    LocalizationManager.Instance.GetString("Month_Hijri_10"),
                    LocalizationManager.Instance.GetString("Month_Hijri_11"),
                    LocalizationManager.Instance.GetString("Month_Hijri_12")
                };

                string suffix = LocalizationManager.Instance.GetString("Label_HijriSuffix");
                string datePart = $"{day} {months[month - 1]} {year} {suffix}";
                return string.IsNullOrEmpty(wd) ? datePart : $"{wd}, {datePart}";
            } catch { return "Hijri Date N/A"; }
        }

        private void RefreshUIDisplay()
        {
            if (_todayPrayerTimes == null || _tomorrowPrayerTimes == null) return;
            
            Dispatcher.Invoke(() =>
            {
                string timeFmt = GetTimeFmt();
                FajrTimeText.Text = $"{_todayPrayerTimes.Fajr.ToString(timeFmt)} - {_todayPrayerTimes.Sunrise.ToString(timeFmt)}";
                SunriseTimeText.Text = _todayPrayerTimes.Sunrise.ToString(timeFmt);
                DhuhrTimeText.Text = $"{_todayPrayerTimes.Dhuhr.ToString(timeFmt)} - {_todayPrayerTimes.Asr.ToString(timeFmt)}";
                AsrTimeText.Text = $"{_todayPrayerTimes.Asr.ToString(timeFmt)} - {_todayPrayerTimes.Maghrib.ToString(timeFmt)}";
                MaghribTimeText.Text = $"{_todayPrayerTimes.Maghrib.ToString(timeFmt)} - {_todayPrayerTimes.Isha.ToString(timeFmt)}";
                IshaTimeText.Text = $"{_todayPrayerTimes.Isha.ToString(timeFmt)} - {_tomorrowPrayerTimes.Fajr.ToString(timeFmt)}";

                // Populate Rakat Notes
                FajrRakatNote.Text = LocalizationManager.Instance.GetString("Note_Fajr");
                DhuhrRakatNote.Text = LocalizationManager.Instance.GetString("Note_Dhuhr");
                AsrRakatNote.Text = LocalizationManager.Instance.GetString("Note_Asr");
                MaghribRakatNote.Text = LocalizationManager.Instance.GetString("Note_Maghrib");
                IshaRakatNote.Text = LocalizationManager.Instance.GetString("Note_Isha");

                // Friday Jumu'ah Support
                bool isFriday = DateTime.Now.DayOfWeek == DayOfWeek.Friday;
                bool showJumuah = false;
                if (isFriday)
                {
                    DateTime? jamatTime = GetJamaatTime(Prayer.DHUHR, SettingsManager.Current, DateTime.Now);
                    // If it's Friday and Jumu'ah (Dhuhr) jamaat hasn't passed yet, show Jumu'ah instead of Dhuhr
                    if (jamatTime.HasValue && DateTime.Now < jamatTime.Value)
                    {
                        showJumuah = true;
                    }
                }

                JumuahCard.Visibility = showJumuah ? Visibility.Visible : Visibility.Collapsed;
                DhuhrCard.Visibility = showJumuah ? Visibility.Collapsed : Visibility.Visible;

                if (showJumuah)
                {
                    JumuahTimeText.Text = DhuhrTimeText.Text;
                    JumuahRakatNote.Text = LocalizationManager.Instance.GetString("Note_Jumuah");
                    JumuahJamatText.Text = $"{LocalizationManager.Instance.GetString("Label_Jamaat")}: {FormatJamat(SettingsManager.Current.DhuhrJamaatTime, timeFmt)}";
                }

                // Populate Jamat Times
                FajrJamatText.Text = $"{LocalizationManager.Instance.GetString("Label_Jamaat")}: {FormatJamat(SettingsManager.Current.FajrJamaatTime, timeFmt)}";
                DhuhrJamatText.Text = $"{LocalizationManager.Instance.GetString("Label_Jamaat")}: {FormatJamat(SettingsManager.Current.DhuhrJamaatTime, timeFmt)}";
                AsrJamatText.Text = $"{LocalizationManager.Instance.GetString("Label_Jamaat")}: {FormatJamat(SettingsManager.Current.AsrJamaatTime, timeFmt)}";
                MaghribJamatText.Text = $"{LocalizationManager.Instance.GetString("Label_Jamaat")}: {FormatJamat(SettingsManager.Current.MaghribJamaatTime, timeFmt)}";
                IshaJamatText.Text = $"{LocalizationManager.Instance.GetString("Label_Jamaat")}: {FormatJamat(SettingsManager.Current.IshaJamaatTime, timeFmt)}";
                
                // Hero Section Sub-Card (Slot Swapping & Ranges)
                DateTime now = DateTime.Now;
                DateTime duhaStart = _todayPrayerTimes.Sunrise.AddMinutes(15);
                DateTime duhaEnd = _todayPrayerTimes.Dhuhr.AddMinutes(-15);
                
                // Slot Swapping: Duha (After Fajr, before Dhuhr)
                if (now > _todayPrayerTimes.Sunrise && now < _todayPrayerTimes.Dhuhr)
                {
                    DhuhrLabel.Text = LocalizationManager.Instance.GetString("Prayer_Duha");
                    SubDhuhrTime.Text = $"{duhaStart.ToString(timeFmt)} - {duhaEnd.ToString(timeFmt)}";
                    DhuhrLabel.Foreground = new SolidColorBrush(WColor.FromRgb(251, 191, 36)); // amber-400
                }
                else
                {
                    DhuhrLabel.Text = showJumuah ? LocalizationManager.Instance.GetString("Prayer_Jumuah") : LocalizationManager.Instance.GetString("Prayer_Dhuhr");
                    SubDhuhrTime.Text = $"{_todayPrayerTimes.Dhuhr.ToString(timeFmt)} - {_todayPrayerTimes.Asr.ToString(timeFmt)}";
                    DhuhrLabel.Foreground = System.Windows.Media.Brushes.White;
                }

                // Slot Swapping: Tahajjud (After Midnight)
                DateTime sunset = _todayPrayerTimes.Maghrib;
                DateTime nextSunrise = _tomorrowPrayerTimes.Sunrise;
                TimeSpan nightLength = nextSunrise - sunset;
                DateTime midnight = sunset.AddTicks(nightLength.Ticks / 2);

                if (now >= midnight && now < _tomorrowPrayerTimes.Fajr)
                {
                    IshaLabel.Text = LocalizationManager.Instance.GetString("Prayer_Tahajjud");
                    IshaLabel.Foreground = new SolidColorBrush(WColor.FromRgb(52, 211, 153)); // emerald-400 (Tahajjud)
                    SubIshaTime.Text = $"{midnight.ToString(timeFmt)} - {_tomorrowPrayerTimes.Fajr.ToString(timeFmt)}";
                }
                else
                {
                    IshaLabel.Text = LocalizationManager.Instance.GetString("Prayer_Isha");
                    IshaLabel.Foreground = System.Windows.Media.Brushes.White;
                    SubIshaTime.Text = $"{_todayPrayerTimes.Isha.ToString(timeFmt)} - {_tomorrowPrayerTimes.Fajr.ToString(timeFmt)}";
                }

                SubFajrTime.Text = $"{_todayPrayerTimes.Fajr.ToString(timeFmt)} - {_todayPrayerTimes.Sunrise.ToString(timeFmt)}";
                // SubDhuhr is handled above
                SubAsrTime.Text = $"{_todayPrayerTimes.Asr.ToString(timeFmt)} - {_todayPrayerTimes.Maghrib.ToString(timeFmt)}";
                SubMaghribTime.Text = $"{_todayPrayerTimes.Maghrib.ToString(timeFmt)} - {_todayPrayerTimes.Isha.ToString(timeFmt)}";
                SubIshaTime.Text = $"{_todayPrayerTimes.Isha.ToString(timeFmt)} - {_tomorrowPrayerTimes.Fajr.ToString(timeFmt)}";

                // Ramadan Mode Hero Data
                HeroSuhurTime.Text = _todayPrayerTimes.Suhur.ToString(timeFmt);
                HeroIftarTime.Text = _todayPrayerTimes.Iftar.ToString(timeFmt);
                
                SuhurTimeText.Text = $"{_todayPrayerTimes.Suhur.ToString(timeFmt)}";
                IftarTimeText.Text = $"{_todayPrayerTimes.Iftar.ToString(timeFmt)}";

                // Hide cards if redundant in Ramadan mode
                SuhurCard.Visibility = _isRamadanMode ? Visibility.Collapsed : Visibility.Visible;
                IftarCard.Visibility = _isRamadanMode ? Visibility.Collapsed : Visibility.Visible;

                string todayDateShort = DateTime.Now.ToString("ddd, MMM d");
                SuhurDateText.Text = todayDateShort;
                IftarDateText.Text = todayDateShort;
                
                UpdateFastingHighlights();
                UpdateNafalTimes();
                UpdateCountdown();
            });
        }

        private string FormatJamat(string timeStr, string timeFmt)
        {
            if (string.IsNullOrEmpty(timeStr)) return "--:--";
            try
            {
                var parts = timeStr.Split(':');
                if (parts.Length < 2) return timeStr;
                var dt = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, int.Parse(parts[0]), int.Parse(parts[1]), 0);
                return dt.ToString(timeFmt);
            }
            catch { return timeStr; }
        }

        private (DateTime start, DateTime end) GetNightBoundaries(DateTime now)
        {
            if (_todayPrayerTimes == null || _tomorrowPrayerTimes == null) return (now, now);
            DateTime todayPDate = _todayPrayerTimes.Fajr.Date;
            DateTime maghrib, nextFajr;

            if (todayPDate == now.Date)
            {
                if (now < _todayPrayerTimes.Fajr)
                {
                    maghrib = _todayPrayerTimes.Maghrib.AddDays(-1);
                    nextFajr = _todayPrayerTimes.Fajr;
                }
                else
                {
                    maghrib = _todayPrayerTimes.Maghrib;
                    nextFajr = _tomorrowPrayerTimes.Fajr;
                }
            }
            else if (todayPDate == now.Date.AddDays(-1))
            {
                if (now < _tomorrowPrayerTimes.Fajr)
                {
                    maghrib = _todayPrayerTimes.Maghrib;
                    nextFajr = _tomorrowPrayerTimes.Fajr;
                }
                else
                {
                    maghrib = _tomorrowPrayerTimes.Maghrib;
                    nextFajr = _tomorrowPrayerTimes.Fajr.AddDays(1);
                }
            }
            else
            {
                maghrib = _todayPrayerTimes.Maghrib;
                nextFajr = _tomorrowPrayerTimes.Fajr;
                if (now.Hour < 12) maghrib = maghrib.AddDays(-1);
            }
            return (maghrib, nextFajr);
        }

        private void UpdateNafalTimes()
        {
            if (_todayPrayerTimes == null || _tomorrowPrayerTimes == null) return;
            var s = SettingsManager.Current;
            var timeFmt = GetTimeFmt();

            // 1. Salat al-Duha (Ishraq)
            // Starts ~15-20 mins after sunrise, ends ~15 mins before Dhuhr
            DateTime duhaStart = _todayPrayerTimes.Sunrise.AddMinutes(15);
            DateTime duhaEnd = _todayPrayerTimes.Dhuhr.AddMinutes(-15);
            DuhaTimeText.Text = $"{duhaStart.ToString(timeFmt)} - {duhaEnd.ToString(timeFmt)}";
            DuhaRakatNote.Text = LocalizationManager.Instance.GetString("Note_Duha");

            // 2. Awwabin
            // Between Maghrib and Isha
            AwwabinTimeText.Text = $"{_todayPrayerTimes.Maghrib.ToString(timeFmt)} - {_todayPrayerTimes.Isha.ToString(timeFmt)}";
            AwwabinRakatNote.Text = LocalizationManager.Instance.GetString("Note_Awwabin");

            // 3. Tahajjud
            // After Isha until Fajr. Optimized after 1/2 or 1/3 of the night.
            var (nightStart, nightEnd) = GetNightBoundaries(DateTime.Now);
            DateTime ishaTime = nightStart.AddTicks((_todayPrayerTimes.Isha - _todayPrayerTimes.Maghrib).Ticks);
            TahajjudTimeText.Text = $"{ishaTime.ToString(timeFmt)} - {nightEnd.ToString(timeFmt)}";
            TahajjudRakatNote.Text = LocalizationManager.Instance.GetString("Note_Tahajjud");

            // Last 1/3 calculation
            TimeSpan nightDuration = nightEnd - nightStart;
            TimeSpan oneThird = new TimeSpan(nightDuration.Ticks / 3);
            DateTime lastThirdStart = nightEnd - oneThird;
            LastThirdTimeText.Text = string.Format(LocalizationManager.Instance.GetString("Label_LastThird"), lastThirdStart.ToString(timeFmt));
            NightThirdNote.Text = LocalizationManager.Instance.GetString("Note_NightThird");
        }

        private bool _isFirstBoot = true;

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (_todayPrayerTimes == null) return;
            
            DateTime now = DateTime.Now;
            var currentPrayer = _todayPrayerTimes.CurrentPrayer(now);

            if (_isFirstBoot)
            {
                _lastStartNotificationPrayer = currentPrayer;
                _lastEndSoundPrayer = currentPrayer;
                _lastAdhanPrayer = currentPrayer;
                _lastJamaatNotificationID = currentPrayer;
                _lastEndNotificationID = currentPrayer;
                _lastJamaatPopupPrayer = currentPrayer;
                var nextResult = GetNextPrayerInfo(now);
                _lastPreAdhanNotificationID = nextResult.nextPrayer;
                _isFirstBoot = false;
            }

            UpdateCountdown();
            CheckEnhancedNotifications(now, currentPrayer);
            CheckTahajjudStartSound(now);
            CheckProhibitedTimes();
            CheckAndShowJamaatAlarm();
            CheckAndPlayAdhanAlarm(currentPrayer);
            CheckTahajjudAdhanAlarm(now);
            CheckAutoBackup(now);
            CheckEidTakbeer(now);
        }

        private void CheckAutoBackup(DateTime now)
        {
            var s = SettingsManager.Current;
            if (s.AutoBackupSchedule == "NONE" || string.IsNullOrEmpty(s.AutoBackupLocation)) return;

            bool due = false;
            if (DateTime.TryParse(s.LastAutoBackupDate, out DateTime lastBackup))
            {
                if (s.AutoBackupSchedule == "DAILY" && (now - lastBackup).TotalDays >= 1) due = true;
                else if (s.AutoBackupSchedule == "WEEKLY" && (now - lastBackup).TotalDays >= 7) due = true;
                else if (s.AutoBackupSchedule == "MONTHLY" && now.Month != lastBackup.Month) due = true;
            }
            else
            {
                // Never backed up, do it now
                due = true;
            }

            if (!due) return;

            try
            {
                if (!Directory.Exists(s.AutoBackupLocation)) return;
                string filename = $"DailyPrayerTracker_Backup_{now:yyyyMMdd_HHmm}.zip";
                string dest = Path.Combine(s.AutoBackupLocation, filename);
                TrackerService.Instance.BackupData(dest);

                s.LastAutoBackupDate = now.ToString("o");
                SettingsManager.Save();
            }
            catch { /* Skip and try again later */ }
        }

        private void CheckEidTakbeer(DateTime now)
        {
            if (!SettingsManager.Current.EidTakbeerEnabled) return;
            if (!DailyPrayerTime.Native.Services.RamadanData.IsEid(now.Date)) return;
            string todayKey = now.ToString("yyyy-MM-dd");
            if (_lastEidTakbeerDate == todayKey) return;
            _lastEidTakbeerDate = todayKey;

            try
            {
                ShowNotification(
                    LocalizationManager.Instance.GetString("Ramadan_EidTakbeerTitle"),
                    LocalizationManager.Instance.GetString("Ramadan_EidTakbeerMsg")
                );
            }
            catch { /* Skip */ }
        }

        private void CheckEnhancedNotifications(DateTime now, Prayer currentPrayer)
        {
            var s = SettingsManager.Current;

            // 0. Sound Notification Transitions
            if (s.PrayerSoundEnabled && currentPrayer != _lastEndSoundPrayer)
            {
                if (currentPrayer == Prayer.SUNRISE) 
                    NotificationSoundService.PlayPrayerSound(Prayer.FAJR, "end");
                else if (currentPrayer == Prayer.ASR) 
                    NotificationSoundService.PlayPrayerSound(Prayer.DHUHR, "end");
                else if (currentPrayer == Prayer.MAGHRIB) 
                    NotificationSoundService.PlayPrayerSound(Prayer.ASR, "end");
                else if (currentPrayer == Prayer.ISHA) 
                    NotificationSoundService.PlayPrayerSound(Prayer.MAGHRIB, "end");
                else if (currentPrayer == Prayer.FAJR && _lastEndSoundPrayer == Prayer.ISHA) 
                    NotificationSoundService.PlayPrayerSound(Prayer.ISHA, "end");

                _lastEndSoundPrayer = currentPrayer;
            }

            if (!s.NotificationsEnabled) return;

            // 1. Prayer Start Notification
            if (currentPrayer != _lastStartNotificationPrayer && currentPrayer != Prayer.NONE && currentPrayer != Prayer.SUNRISE)
            {
                if (IsReminderEnabled(currentPrayer, s))
                {
                    ShowNotification(
                        string.Format(LocalizationManager.Instance.GetString("Notify_PrayerStarted"), FormatPrayerName(currentPrayer)),
                        string.Format(LocalizationManager.Instance.GetString("Notify_PrayerStartedMsg"), FormatPrayerName(currentPrayer), now.ToString(GetTimeFmt()))
                    );
                }
                
                // Sound
                if (s.PrayerSoundEnabled)
                {
                    NotificationSoundService.PlayPrayerSound(currentPrayer, "start");
                }

                _lastStartNotificationPrayer = currentPrayer;
            }

            // 2. Pre-Adhan Reminder
            var nextResult = GetNextPrayerInfo(now);
            if (nextResult.nextTime != DateTime.MinValue && _lastPreAdhanNotificationID != nextResult.nextPrayer)
            {
                TimeSpan timeToNext = nextResult.nextTime - now;
                if (timeToNext.TotalMinutes > 0 && timeToNext.TotalMinutes <= s.PreAdhanOffset)
                {
                    if (IsReminderEnabled(nextResult.nextPrayer, s))
                    {
                        ShowNotification(
                            string.Format(LocalizationManager.Instance.GetString("Notify_StartingSoon"), FormatPrayerName(nextResult.nextPrayer)),
                            string.Format(LocalizationManager.Instance.GetString("Notify_StartingSoonMsg"), FormatPrayerName(nextResult.nextPrayer), Math.Ceiling(timeToNext.TotalMinutes)),
                            true
                        );
                    }
                    _lastPreAdhanNotificationID = nextResult.nextPrayer;
                }
            }
            if (currentPrayer == nextResult.nextPrayer) _lastPreAdhanNotificationID = Prayer.NONE;

            // 3. Shuruq / Sunrise Notifications
            string today = now.ToShortDateString();
            if (s.ReminderShuruq)
            {
                // Warning: Fajr ending soon (10 mins before sunrise)
                TimeSpan timeToSunrise = _todayPrayerTimes!.Sunrise - now;
                if (timeToSunrise.TotalMinutes > 0 && timeToSunrise.TotalMinutes <= 10 && _lastShuruqWarningDate != today)
                {
                    ShowNotification(
                        LocalizationManager.Instance.GetString("Notify_FajrEnding"),
                        string.Format(LocalizationManager.Instance.GetString("Notify_FajrEndingMsg"), _todayPrayerTimes.Sunrise.ToString(GetTimeFmt()), Math.Ceiling(timeToSunrise.TotalMinutes))
                    );
                    _lastShuruqWarningDate = today;
                }

                // Notification: Sunrise started
                if (currentPrayer == Prayer.SUNRISE && _lastSunriseNotificationDate != today)
                {
                    ShowNotification(
                        LocalizationManager.Instance.GetString("Notify_SunriseStarted"),
                        string.Format(LocalizationManager.Instance.GetString("Notify_SunriseStartedMsg"), now.ToString(GetTimeFmt()))
                    );
                    _lastSunriseNotificationDate = today;
                }
            }

            // 4. Jamaat Notification
            var jamaatTime = GetJamaatTime(currentPrayer, s, now);
            if (jamaatTime.HasValue && _lastJamaatNotificationID != currentPrayer)
            {
                if (now >= jamaatTime.Value && now < jamaatTime.Value.AddMinutes(1))
                {
                    if (IsEstablishedEnabled(currentPrayer, s))
                    {
                        ShowNotification(
                            string.Format(LocalizationManager.Instance.GetString("Notify_JamaatNow"), FormatPrayerName(currentPrayer)),
                            string.Format(LocalizationManager.Instance.GetString("Notify_JamaatNowMsg"), FormatPrayerName(currentPrayer), jamaatTime.Value.ToString(GetTimeFmt()))
                        );
                    }
                    _lastJamaatNotificationID = currentPrayer;
                }
            }

            // 5. Prayer End Warning (Generic, 10 mins before)
            if (nextResult.nextTime != DateTime.MinValue && _lastEndNotificationID != currentPrayer && currentPrayer != Prayer.NONE)
            {
                TimeSpan timeToNext = nextResult.nextTime - now;
                if (timeToNext.TotalMinutes > 0 && timeToNext.TotalMinutes <= 10)
                {
                    // For Fajr, we already handled Shuruq warning above
                    if (currentPrayer != Prayer.FAJR)
                    {
                        ShowNotification(
                            string.Format(LocalizationManager.Instance.GetString("Notify_EndingSoon"), FormatPrayerName(currentPrayer)),
                            string.Format(LocalizationManager.Instance.GetString("Notify_EndingSoonMsg"), FormatPrayerName(currentPrayer), Math.Ceiling(timeToNext.TotalMinutes))
                        );
                    }
                    _lastEndNotificationID = currentPrayer;
                }
            }
        }

        private static void ShowNotification(string title, string message, bool withSound = false)
        {
            var builder = new ToastContentBuilder()
                .AddText(title)
                .AddText(message);

            if (withSound)
            {
                builder.AddAudio(new Uri("ms-winsoundevent:Notification.Default"));
            }

            builder.Show();
        }

        private void CheckAndPlayAdhanAlarm(Prayer currentPrayer)
        {
            var s = SettingsManager.Current;
            if (string.IsNullOrEmpty(s.AdhanSoundPath)) return;
            if (!IsAdhanEnabled(currentPrayer, s)) return;

            // Only trigger if we haven't played it for this prayer today
            string today = DateTime.Now.ToShortDateString();
            if (_lastAdhanPrayer == currentPrayer && _lastAdhanDate == today) return;

            DateTime? jamaatTime = GetJamaatTime(currentPrayer != Prayer.NONE ? currentPrayer : Prayer.FAJR, s, DateTime.Now);
            if (!jamaatTime.HasValue) return;

            DateTime adhanTriggerTime = jamaatTime.Value.AddMinutes(-s.AdhanAlarmOffset);
            DateTime now = DateTime.Now;

            // Pick the sound path: Fajr path if prayer is Fajr and path exists, otherwise standard
            string soundPath = s.AdhanSoundPath;
            if (currentPrayer == Prayer.FAJR && !string.IsNullOrEmpty(s.FajrAdhanSoundPath))
            {
                soundPath = s.FajrAdhanSoundPath;
            }

            if (string.IsNullOrEmpty(soundPath)) return;

            // Trigger if within a 30-second window of the offset
            if (now >= adhanTriggerTime && now < adhanTriggerTime.AddSeconds(30))
            {
                string prayerName = FormatPrayerName(currentPrayer);
                string timeFmt = GetTimeFmt();
                string range = GetPrayerTimeRange(currentPrayer, _todayPrayerTimes, _tomorrowPrayerTimes, timeFmt);
                string jt = jamaatTime.Value.ToString(timeFmt);

                if (s.AdhanPopupEnabled)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        var popup = new AdhanNotificationWindow(prayerName, range, jt, soundPath);
                        popup.Volume = s.AdhanVolume / 100.0;
                        popup.Show();
                    });
                    _lastAdhanPrayer = currentPrayer;
                    _lastAdhanDate = today;
                }
                else
                {
                    try
                    {
                        if (System.IO.File.Exists(soundPath))
                        {
                            _adhanPlayer.Open(new Uri(soundPath));
                            _adhanPlayer.Volume = s.AdhanVolume / 100.0;
                            _adhanPlayer.Play();
                            _lastAdhanPrayer = currentPrayer;
                            _lastAdhanDate = today;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Adhan play failed: " + ex.Message);
                    }
                }
            }
        }

        private string GetPrayerTimeRange(Prayer p, CombinedPrayerTimes? t, CombinedPrayerTimes? nextDay, string fmt)
        {
            if (t == null) return "";
            return p switch
            {
                Prayer.FAJR => $"{t.Fajr.ToString(fmt)} - {t.Sunrise.ToString(fmt)}",
                Prayer.DHUHR => $"{t.Dhuhr.ToString(fmt)} - {t.Asr.ToString(fmt)}",
                Prayer.ASR => $"{t.Asr.ToString(fmt)} - {t.Maghrib.ToString(fmt)}",
                Prayer.MAGHRIB => $"{t.Maghrib.ToString(fmt)} - {t.Isha.ToString(fmt)}",
                Prayer.ISHA => $"{t.Isha.ToString(fmt)} - {(nextDay != null ? nextDay.Fajr.ToString(fmt) : LocalizationManager.Instance.GetString("Prayer_Fajr"))}",
                _ => ""
            };
        }

        private string _lastTahajjudAdhanDate = "";
        private void CheckTahajjudAdhanAlarm(DateTime now)
        {
            var s = SettingsManager.Current;
            if (!s.TahajjudAdhanEnabled || _todayPrayerTimes == null || _tomorrowPrayerTimes == null) return;

            // Only trigger if we haven't played it tonight
            string today = now.ToShortDateString();
            if (_lastTahajjudAdhanDate == today) return;

            // Best Time calculation (same as UpdateNafalTimes)
            var (nightStart, nightEnd) = GetNightBoundaries(now);
            TimeSpan nightDuration = nightEnd - nightStart;
            TimeSpan oneThird = new TimeSpan(nightDuration.Ticks / 3);
            DateTime lastThirdStart = nightEnd - oneThird;

            // Check if we are in the window (within 30 seconds of Last Third Start)
            if (now >= lastThirdStart && now < lastThirdStart.AddSeconds(30))
            {
                string soundPath = !string.IsNullOrEmpty(s.TahajjudAdhanSoundPath) ? s.TahajjudAdhanSoundPath : s.AdhanSoundPath;
                if (string.IsNullOrEmpty(soundPath) || !System.IO.File.Exists(soundPath)) return;

                if (s.AdhanPopupEnabled)
                {
                    string timeFmt = GetTimeFmt();
                    string range = $"{lastThirdStart.ToString(timeFmt)} - {nightEnd.ToString(timeFmt)}";
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        var popup = new AdhanNotificationWindow(LocalizationManager.Instance.GetString("Prayer_Tahajjud"), range, "N/A", soundPath);
                        popup.Show();
                    });
                    _lastTahajjudAdhanDate = today;
                }
                else
                {
                    try
                    {
                        _adhanPlayer.Open(new Uri(soundPath));
                        _adhanPlayer.Play();
                        _lastTahajjudAdhanDate = today;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Tahajjud Adhan play failed: " + ex.Message);
                    }
                }
            }
        }

        private void CheckTahajjudStartSound(DateTime now)
        {
            var s = SettingsManager.Current;
            if (!s.PrayerSoundEnabled || _todayPrayerTimes == null || _tomorrowPrayerTimes == null) return;

            string today = now.ToShortDateString();
            if (_lastTahajjudSoundDate == today) return;

            // Islamic Midnight (Halfway between Maghrib/Sunset and Fajr)
            var (nightStart, nightEnd) = GetNightBoundaries(now);
            TimeSpan nightDuration = nightEnd - nightStart;
            TimeSpan halfNight = new TimeSpan(nightDuration.Ticks / 2);
            DateTime islamicMidnight = nightStart + halfNight;

            // Trigger at Midnight (within 30s window)
            if (now >= islamicMidnight && now < islamicMidnight.AddSeconds(30))
            {
                NotificationSoundService.PlayTahajjudSound();
                _lastTahajjudSoundDate = today;
            }
        }
    
        private void CheckAndShowJamaatAlarm()
        {
            if (_todayPrayerTimes == null || _tomorrowPrayerTimes == null) return;

            DateTime now = DateTime.Now;
            var s = SettingsManager.Current;

            // List of prayers to check for jamaat
            Prayer[] prayers = { Prayer.FAJR, Prayer.DHUHR, Prayer.ASR, Prayer.MAGHRIB, Prayer.ISHA };

            foreach (var p in prayers)
            {
                if (IsEstablishedEnabled(p, s))
                {
                    if (CheckAndShowJamaatAlarm(p, now, s)) return;
                    if (s.DeedPopupEnabled) CheckAndShowDeedPopup(p, now, s);
                }
            }

            if (s.DailySummaryPopupEnabled) CheckAndShowDailySummary(now, s);
            ResetJamaatAlarmState(now, s);
        }

        private bool CheckAndShowJamaatAlarm(Prayer p, DateTime now, AppSettings s)
        {
            DateTime? jamaatTime = GetJamaatTime(p, s, now);
            if (!jamaatTime.HasValue) return false;
            
            DateTime startTime = _todayPrayerTimes!.TimeForPrayer(p);
            DateTime endTime = GetPrayerEndTime(p);

            // Validation: Ensure Jamaat is between Start and End
            DateTime validatedJamaat = jamaatTime.Value;
            if (validatedJamaat < startTime) validatedJamaat = startTime;
            if (validatedJamaat >= endTime) validatedJamaat = endTime.AddMinutes(-1);

            DateTime popupTriggerTime = validatedJamaat.AddMinutes(-s.JamaatPopupOffset);

            if (now >= popupTriggerTime && now < validatedJamaat)
            {
                if (_lastJamaatPopupPrayer != p)
                {
                    ShowJamaatPopup(FormatPrayerName(p), validatedJamaat);
                    _lastJamaatPopupPrayer = p;
                }
                return true;
            }
            return false;
        }

        private void CheckAndShowDeedPopup(Prayer p, DateTime now, AppSettings s)
        {
            if (_lastDeedPopupPrayer == p && _lastDeedPopupDate == now.ToString("yyyy-MM-dd")) return;

            DateTime? jamaatTime = GetJamaatTime(p, s, now);
            if (!jamaatTime.HasValue) return;

            // Show popup some minutes after jamaat
            DateTime popupTime = jamaatTime.Value.AddMinutes(s.DeedPopupOffsetMinutes);

            if (now >= popupTime && now < popupTime.AddMinutes(30)) // 30 min window
            {
                // Map Prayer enum to title-case keys used in Deeds dictionary
                string pKey = ToPrayerKey(p);
                if (p == Prayer.DHUHR && now.DayOfWeek == DayOfWeek.Friday) pKey = "Jumuah";

                if (_currentDeeds.Prayers.TryGetValue(pKey, out var entries))
                {
                    _lastDeedPopupPrayer = p;
                    _lastDeedPopupDate = now.ToString("yyyy-MM-dd");

                    var popup = new DeedPopup(pKey, entries, _currentDeeds);
                    if (this.IsVisible) popup.Owner = this;
                    popup.Show();
                }
            }
        }

        private void CheckAndShowDailySummary(DateTime now, AppSettings s)
        {
            if (_lastSummaryPopupDate == now.ToString("yyyy-MM-dd")) return;

            if (TimeSpan.TryParse(s.DailySummaryPopupTime, out var summaryTime))
            {
                DateTime target = now.Date.Add(summaryTime);
                if (now >= target && now < target.AddMinutes(30))
                {
                    _lastSummaryPopupDate = now.ToString("yyyy-MM-dd");
                    var summary = new DailySummaryPopup(_currentDeeds);
                    if (this.IsVisible) summary.Owner = this;
                    summary.Show();
                }
            }
        }

        private static DateTime? GetJamaatTime(Prayer p, AppSettings s, DateTime now)
        {
            string? timeStr = p switch
            {
                Prayer.FAJR => s.FajrJamaatTime,
                Prayer.DHUHR => s.DhuhrJamaatTime,
                Prayer.ASR => s.AsrJamaatTime,
                Prayer.MAGHRIB => s.MaghribJamaatTime,
                Prayer.ISHA => s.IshaJamaatTime,
                _ => null
            };

            if (string.IsNullOrEmpty(timeStr)) return null;

            if (TimeSpan.TryParse(timeStr, System.Globalization.CultureInfo.InvariantCulture, out TimeSpan ts))
            {
                DateTime jamaat = now.Date.Add(ts);
                
                // If it's already passed and it's early next morning (common for Isha/Tahajjud area)
                // but usually prayer times follow the sunrise-sunset cycle of the "current Islamic day".
                // We'll just stick to today's date mostly.
                return jamaat;
            }
            return null;
        }

        private void ResetJamaatAlarmState(DateTime now, AppSettings s)
        {
            if (_lastJamaatPopupPrayer == Prayer.NONE) return;

            DateTime? lastJamaatTime = GetJamaatTime(_lastJamaatPopupPrayer, s, now);
            if (lastJamaatTime.HasValue && now > lastJamaatTime.Value.AddMinutes(1))
            {
                _lastJamaatPopupPrayer = Prayer.NONE;
            }
        }


        private static void UpdateProhibCard(System.Windows.Controls.Border card, System.Windows.Controls.TextBlock timerText, System.Windows.Controls.TextBlock? stickyStatus, DateTime now, DateTime start, DateTime end)
        {
            if (now >= start && now <= end)
            {
                card.Background = new SolidColorBrush(WColor.FromArgb(150, 239, 68, 68)); // semi-transparent red
                TimeSpan rem = end - now;
                string t = $"{rem.Hours}h {rem.Minutes}m {rem.Seconds}s";
                timerText.Text = t;
                if (stickyStatus != null)
                {
                    stickyStatus.Text = "ACTIVE";
                    stickyStatus.Foreground = new SolidColorBrush(WColor.FromRgb(248, 113, 113)); // red-400
                }
            }
            else
            {
                card.Background = new SolidColorBrush(WColor.FromArgb(26, 255, 255, 255)); // 10% white (glass)
                if (now < start)
                {
                    TimeSpan rem = start - now;
                    string t = rem.TotalHours < 24 
                        ? $"-{rem.Hours}{LocalizationManager.Instance.GetString("Unit_Hour_Short")} {rem.Minutes}{LocalizationManager.Instance.GetString("Unit_Min_Short")} {rem.Seconds}{LocalizationManager.Instance.GetString("Unit_Sec_Short")}" 
                        : LocalizationManager.Instance.GetString("Label_Upcoming");
                    timerText.Text = t;
                    if (stickyStatus != null)
                    {
                        stickyStatus.Text = rem.TotalHours < 24 
                            ? string.Format(LocalizationManager.Instance.GetString("Label_StartsInShort"), rem.Hours, rem.Minutes) 
                            : LocalizationManager.Instance.GetString("Label_Upcoming");
                        stickyStatus.Foreground = new SolidColorBrush(Colors.White) { Opacity = 0.7 };
                    }
                }
                else
                {
                    timerText.Text = LocalizationManager.Instance.GetString("Label_Passed");
                    if (stickyStatus != null)
                    {
                        stickyStatus.Text = LocalizationManager.Instance.GetString("Label_Passed");
                        stickyStatus.Foreground = new SolidColorBrush(Colors.White) { Opacity = 0.5 };
                    }
                }
            }
        }
        
        private void UpdateCountdown()
        {
            if (_todayPrayerTimes == null || _tomorrowPrayerTimes == null) return;
            
            DateTime now = DateTime.Now;

            var nextResult = GetNextPrayerInfo(now);
            Prayer nextPrayer = nextResult.nextPrayer;
            DateTime nextTime = nextResult.nextTime;

            TimeSpan remaining = nextTime - now;
            if (remaining.TotalSeconds < 0)
            {
                 _ = CalculatePrayerTimes();
                 return;
            }

            string countStr = FormatCountdown(remaining.Hours, remaining.Minutes, remaining.Seconds);
            var nextName = FormatPrayerName(nextPrayer);
            
            var currentPrayer = _todayPrayerTimes.CurrentPrayer(now);
            // If it's before Fajr (early morning), we are in the Isha/Night window of the previous religious day.
            Prayer curPrayer = (currentPrayer == Prayer.NONE && (now > _todayPrayerTimes.Isha || now < _todayPrayerTimes.Fajr)) ? Prayer.ISHA : currentPrayer;
            string curName = FormatPrayerName(curPrayer);

            string heroCountStr = countStr;
            Prayer heroPrayer = curPrayer;

            if (currentPrayer.ToString() == "SUNRISE") // Adhan library enum may have SUNRISE
            {
                heroCountStr = "-" + countStr;
                heroPrayer = Prayer.NONE;
            }

            UpdateHeroSection(heroPrayer, curName, nextName, heroCountStr, nextTime);
            UpdateOverlay(heroPrayer, curName, nextName, heroCountStr, nextTime);
            UpdateProgressBar(currentPrayer, nextTime, now);
            UpdatePrayerListHighlighting(curPrayer);
            UpdateHighlightsCountdown(now);
            UpdateDynamicBackgrounds(now);
        }

        public static class UIPaths
        {
            public const string Sun = "M12 7c-2.76 0-5 2.24-5 5s2.24 5 5 5 5-2.24 5-5-2.24-5-5-5zM2 13h2c.55 0 1-.45 1-1s-.45-1-1-1H2c-.55 0-1 .45-1 1s.45 1 1 1zm18 0h2c.55 0 1-.45 1-1s-.45-1-1-1h-2c-.55 0-1 .45-1 1s.45 1 1 1zM11 2v2c0 .55.45 1 1 1s1-.45 1-1V2c0-.55-.45-1-1-1s-1 .45-1 1zm0 18v2c0 .55.45 1 1 1s1-.45 1-1v-2c0-.55-.45-1-1-1s-1 .45-1 1zM5.99 4.58c-.39-.39-1.03-.39-1.41 0-.39.39-.39 1.03 0 1.41l1.06 1.06c.39.39 1.03.39 1.41 0 .39-.39.39-1.03 0-1.41L5.99 4.58zm12.37 12.37c-.39-.39-1.03-.39-1.41 0-.39.39-.39 1.03 0 1.41l1.06 1.06c.39.39 1.03.39 1.41 0 .39-.39.39-1.03 0-1.41l-1.06-1.06zm1.06-10.96c.39-.39.39-1.03 0-1.41-.39-.39-1.03-.39-1.41 0l-1.06 1.06c-.39.39-.39 1.03 0 1.41.39.39 1.03.39 1.41 0l1.06-1.06zM7.05 18.36c.39-.39.39-1.03 0-1.41-.39-.39-1.03-.39-1.41 0l-1.06 1.06c-.39.39-.39 1.03 0 1.41.39.39 1.03.39 1.41 0l1.06-1.06z";
            public const string Moon = "M12 3c-4.97 0-9 4.03-9 9s4.03 9 9 9 9-4.03 9-9c0-.46-.04-.92-.1-1.36-.98 1.37-2.58 2.26-4.4 2.26-3.03 0-5.5-2.47-5.5-5.5 0-1.82.89-3.42 2.26-4.4C12.92 3.04 12.46 3 12 3z";
        }

        private string _currentBgName = "";

        private void UpdateDynamicBackgrounds(DateTime now)
        {
            if (_todayPrayerTimes == null || _tomorrowPrayerTimes == null) return;

            string pathData;
            string keyName;
            
            DateTime sunriseStart = _todayPrayerTimes.Fajr;
            DateTime sunsetEnd = _todayPrayerTimes.Maghrib;

            if (now >= sunriseStart && now < sunsetEnd)
            {
                pathData = UIPaths.Sun;
                keyName = "Sun";
            }
            else
            {
                pathData = UIPaths.Moon;
                keyName = "Moon";
            }

            if (_currentBgName != keyName)
            {
                _currentBgName = keyName;
                try
                {
                    var geom = System.Windows.Media.Geometry.Parse(pathData);
                    HeroBackgroundPath.Data = geom;
                    if (_overlay != null)
                    {
                        _overlay.OverlayBackgroundPath.Data = geom;
                    }
                }
                catch { }
            }
        }

        private void UpdateHighlightsCountdown(DateTime now)
        {
            if (_todayPrayerTimes == null) return;

            UpdateSingleHighlight(SuhurCountdownText, now, _todayPrayerTimes.Suhur);
            UpdateSingleHighlight(IftarCountdownText, now, _todayPrayerTimes.Iftar);
        }

        private static void UpdateSingleHighlight(System.Windows.Controls.TextBlock textBlock, DateTime now, DateTime target)
        {
            if (now > target)
            {
                textBlock.Text = LocalizationManager.Instance.GetString("Label_Passed");
                textBlock.Foreground = new SolidColorBrush(WColor.FromRgb(156, 163, 175)); // slate-400
            }
            else
            {
                TimeSpan rem = target - now;
                textBlock.Text = $"-{rem.Hours}{LocalizationManager.Instance.GetString("Unit_Hour_Short")} {rem.Minutes}{LocalizationManager.Instance.GetString("Unit_Min_Short")} {rem.Seconds}{LocalizationManager.Instance.GetString("Unit_Sec_Short")}";
                textBlock.Foreground = new SolidColorBrush(WColor.FromRgb(255, 255, 255)); // white
            }
        }

        private (Prayer nextPrayer, DateTime nextTime) GetNextPrayerInfo(DateTime now)
        {
            if (_todayPrayerTimes == null || _tomorrowPrayerTimes == null) return (Prayer.NONE, DateTime.MinValue);

            // Using local times throughout
            if (now < _todayPrayerTimes.Fajr) return (Prayer.FAJR, _todayPrayerTimes.Fajr);
            if (now < _todayPrayerTimes.Sunrise) return (Prayer.SUNRISE, _todayPrayerTimes.Sunrise);
            if (now < _todayPrayerTimes.Dhuhr) return (Prayer.DHUHR, _todayPrayerTimes.Dhuhr);
            if (now < _todayPrayerTimes.Asr) return (Prayer.ASR, _todayPrayerTimes.Asr);
            if (now < _todayPrayerTimes.Maghrib) return (Prayer.MAGHRIB, _todayPrayerTimes.Maghrib);
            if (now < _todayPrayerTimes.Isha) return (Prayer.ISHA, _todayPrayerTimes.Isha);

            return (Prayer.FAJR, _tomorrowPrayerTimes.Fajr);
        }

        private void UpdateHeroSection(Prayer currentPrayer, string curName, string nextName, string countStr, DateTime nextTime)
        {
            HeroTimeUntilLabel.Text = currentPrayer != Prayer.NONE 
                ? string.Format(LocalizationManager.Instance.GetString("Hero_TimeLeft"), curName) 
                : string.Format(LocalizationManager.Instance.GetString("Hero_TimeUntil"), nextName);
            HeroCountdownText.Text = countStr;

            if (_isTrackerMode)
            {
                string miniName = currentPrayer != Prayer.NONE ? curName : nextName;
                TrackerViewControl.UpdateMiniStatus(miniName, countStr);
            }

            SyncTrackerWithHero(currentPrayer);
            
            // Update Rakat Note for Hero Section
            bool isFriday = DateTime.Now.DayOfWeek == DayOfWeek.Friday;
            Prayer noteP = (currentPrayer == Prayer.NONE && nextName == "Shuruq") ? Prayer.FAJR : (currentPrayer != Prayer.NONE ? currentPrayer : Prayer.NONE);
            // If it's Friday and the current/next is Dhuhr, use Jumu'ah notes
            if (isFriday && (noteP == Prayer.DHUHR || (currentPrayer == Prayer.NONE && nextName == "Dhuhr")))
            {
                HeroPrayerNote.Text = LocalizationManager.Instance.GetString("Note_Jumuah");
            }
            else
            {
                HeroPrayerNote.Text = GetRakatNote(noteP);
            }
            HeroPrayerNote.Visibility = string.IsNullOrEmpty(HeroPrayerNote.Text) ? Visibility.Collapsed : Visibility.Visible;

            // Update Progress Row Labels
            DateTime now = DateTime.Now;
            var timeFmt = GetTimeFmt();
            
            // Previous and Next Prayer specifically for the Hero Row
            Prayer prevP = GetPreviousPrayer(now);
            DateTime prevT = _todayPrayerTimes!.TimeForPrayer(prevP);
            // Handling midnight crossover for previous prayer time label
            if (prevP == Prayer.ISHA && now < _todayPrayerTimes.Fajr) prevT = _todayPrayerTimes.Isha.AddDays(-1);

            HeroPrevPrayerName.Text = FormatPrayerName(prevP);
            HeroPrevPrayerTime.Text = prevT.ToString(timeFmt);
            
            HeroNextPrayerName.Text = FormatPrayerName(currentPrayer != Prayer.NONE ? GetNextPrayerAfter(currentPrayer) : _todayPrayerTimes.CurrentPrayer(nextTime));
            // Actually, we already have nextName and nextTime from caller
            HeroNextPrayerName.Text = nextName;
            HeroNextPrayerTime.Text = nextTime.ToString(timeFmt);

            // Progress Bar (Percent)
            TimeSpan total = nextTime - prevT;
            TimeSpan elapsed = now - prevT;
            double progress = (elapsed.Ticks / (double)total.Ticks) * 100;
            HeroProgressBar.Value = Math.Max(0, Math.Min(100, progress));

            // Show Jamaat time for the displayed next prayer
            DateTime? jamaatTime = GetJamaatTime(currentPrayer != Prayer.NONE ? currentPrayer : (nextName == "Fajr" ? Prayer.FAJR : (Prayer)Enum.Parse(typeof(Prayer), nextName.ToUpper())), SettingsManager.Current, DateTime.Now);
            // Fallback for enum parsing if nextName is custom
            try { if (currentPrayer == Prayer.NONE) jamaatTime = GetJamaatTime((Prayer)Enum.Parse(typeof(Prayer), nextName.ToUpper()), SettingsManager.Current, DateTime.Now); } catch { }
            
            // Note: I'll clean up the jamaat display since it's not explicitly in the new user design image 
            // but I can keep it or hide it. The user image doesn't show Jamaat. I'll hide it to be faithful to the image.
            // If the user wants it back, they can ask.

            // Show Context Notes (Makruh, Duha, Tahajjud)
            HeroContextNoteText.Visibility = Visibility.Collapsed;
            TahajjudHeroDisplay.Visibility = Visibility.Collapsed;
            NafalNoticeHero.Visibility = Visibility.Collapsed;
            
            if (_todayPrayerTimes != null && _tomorrowPrayerTimes != null)
            {
                HeroSunriseTime.Text = _todayPrayerTimes.Sunrise.ToString(timeFmt);
                HeroSunsetTime.Text = _todayPrayerTimes.Maghrib.ToString(timeFmt);

                // Visibility of Hero grids based on settings
                bool showGrid = SettingsManager.Current.ShowHeroPrayerGrid;
                HeroDefaultGrid.Visibility = (!_isRamadanMode && showGrid) ? Visibility.Visible : Visibility.Collapsed;
                HeroRamadanGrid.Visibility = (_isRamadanMode && showGrid) ? Visibility.Visible : Visibility.Collapsed;

                // Ramadan Hero Overrides
                if (_isRamadanMode)
                {
                    DateTime suhur = _todayPrayerTimes.Suhur;
                    DateTime iftar = _todayPrayerTimes.Iftar;
                    
                    if (now < suhur)
                    {
                        TimeSpan rem = suhur - now;
                        HeroSuhurStatus.Text = $"-{rem.Hours}{LocalizationManager.Instance.GetString("Unit_Hour_Short")} {rem.Minutes}{LocalizationManager.Instance.GetString("Unit_Min_Short")} {rem.Seconds}{LocalizationManager.Instance.GetString("Unit_Sec_Short")}";
                        HeroIftarStatus.Text = LocalizationManager.Instance.GetString("Status_Upcoming");
                    }
                    else if (now < iftar)
                    {
                        HeroSuhurStatus.Text = LocalizationManager.Instance.GetString("Status_Completed");
                        TimeSpan rem = iftar - now;
                        HeroIftarStatus.Text = $"-{rem.Hours}{LocalizationManager.Instance.GetString("Unit_Hour_Short")} {rem.Minutes}{LocalizationManager.Instance.GetString("Unit_Min_Short")} {rem.Seconds}{LocalizationManager.Instance.GetString("Unit_Sec_Short")}";

                        // NEW: Swap Asr countdown to Iftar if active
                        if (currentPrayer == Prayer.ASR || nextName == "Maghrib")
                        {
                            HeroTimeUntilLabel.Text = string.Format(LocalizationManager.Instance.GetString("Hero_TimeLeft"), LocalizationManager.Instance.GetString("Prayer_Maghrib"));
                            HeroCountdownText.Text = FormatCountdown(rem.Hours, rem.Minutes, rem.Seconds);
                        }
                    }
                    else
                    {
                        HeroSuhurStatus.Text = LocalizationManager.Instance.GetString("Label_Done");
                        HeroIftarStatus.Text = LocalizationManager.Instance.GetString("Label_Current");
                    }
                }

                // Makruh or Tahajjud (During Isha)
                if (curName.Equals("Isha", StringComparison.OrdinalIgnoreCase) || currentPrayer == Prayer.ISHA)
                {
                    var (nightStart, nightEnd) = GetNightBoundaries(now);
                    TimeSpan nightDuration = nightEnd - nightStart;
                    
                    // Islamic Midnight (Halfway)
                    TimeSpan halfNight = new TimeSpan(nightDuration.Ticks / 2);
                    DateTime islamicMidnight = nightStart + halfNight;

                    // Last Third of the Night (Tahajjud)
                    TimeSpan oneThird = new TimeSpan(nightDuration.Ticks / 3);
                    DateTime lastThirdStart = nightEnd - oneThird;
                    
                    DateTime tahajjudWindowEnd = nightEnd.AddMinutes(-10);
                    if (now < islamicMidnight && now >= nightStart)
                    {
                        HeroContextNoteText.Text = string.Format(LocalizationManager.Instance.GetString("Label_MakruhStarts"), islamicMidnight.ToString(GetTimeFmt()));
                        HeroContextNoteText.Visibility = Visibility.Visible;
                    }
                    else if (now >= lastThirdStart && now < tahajjudWindowEnd)
                    {
                        // Dedicated Tahajjud Timer
                        TahajjudHeroDisplay.Visibility = Visibility.Visible;
                        TimeSpan rem = tahajjudWindowEnd - now;
                        TahajjudHeroTimerText.Text = FormatCountdown(rem.Hours, rem.Minutes, rem.Seconds);
                    }
                    else
                    {
                        HeroContextNoteText.Text = LocalizationManager.Instance.GetString("Label_TahajjudActive");
                        HeroContextNoteText.Foreground = new SolidColorBrush(Colors.White);
                        HeroContextNoteText.Visibility = Visibility.Visible;
                    }
                }
                // Salat al-Duha (Notice Card)
                else if (now > _todayPrayerTimes.Sunrise.AddMinutes(15) && now < _todayPrayerTimes.Dhuhr.AddMinutes(-5))
                {
                    NafalNoticeText.Text = string.Format(LocalizationManager.Instance.GetString("Label_DuhaActive"), _todayPrayerTimes.Dhuhr.AddMinutes(-15).ToString(GetTimeFmt()));
                    NafalNoticeHero.Visibility = Visibility.Visible;
                }
                // Salat al-Awwabin (Notice Card)
                else if (now > _todayPrayerTimes.Maghrib.AddMinutes(15) && now < _todayPrayerTimes.Isha.AddMinutes(-15))
                {
                    NafalNoticeText.Text = LocalizationManager.Instance.GetString("Label_AwwabinActive");
                    NafalNoticeHero.Visibility = Visibility.Visible;
                }
                else
                {
                    HeroContextNoteText.Foreground = new SolidColorBrush(WColor.FromRgb(251, 191, 36)); // amber-400 (reset for Makruh)
                }
            }
        }

        private void UpdateOverlay(Prayer currentPrayer, string curName, string nextName, string countStr, DateTime nextTime)
        {
            DateTime? currentStart = _todayPrayerTimes?.TimeForPrayer(_todayPrayerTimes.CurrentPrayer(DateTime.Now));
            if (!currentStart.HasValue) currentStart = _todayPrayerTimes?.Isha;
            string rangeStr = currentStart.HasValue ? $"{currentStart.Value:hh:mm tt} - {nextTime:hh:mm tt}" : "N/A";

            // 1. Sync Data for DeskBand (Background)
            try
            {
                if (SettingsManager.Current.UseDeskBand)
                {
                    var data = new DeskBandData
                    {
                        Label = currentPrayer != Prayer.NONE 
                            ? string.Format(LocalizationManager.Instance.GetString("Label_EndsIn"), curName) 
                            : string.Format(LocalizationManager.Instance.GetString("Label_StartsIn"), nextName),
                        Countdown = countStr,
                        CurrentPrayer = curName,
                        NextPrayer = nextName,
                        NextTime = nextTime.ToString(GetTimeFmt()),
                        PrimaryColor = SettingsManager.Current.PrimaryColor,
                        IsNight = _currentBgName == "Moon",
                        IsActive = true
                    };
                    DeskBandDataWriter.Write(data);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("DeskBand update failed: " + ex.Message);
            }

            // 2. Update Integrated Taskbar Window
            try
            {
                if (_taskbarWindow != null)
                {
                    string label = currentPrayer != Prayer.NONE ? curName : nextName;
                    _taskbarWindow.UpdateData(countStr, label);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Integrated Taskbar update failed: " + ex.Message);
            }

            // 3. Update Enhanced Taskbar Window (TrafficMonitor-style)
            try
            {
                if (_enhancedTaskbarWindow != null)
                {
                    string compact;
                    if (currentPrayer != Prayer.NONE)
                        compact = $"\u25cf {curName} {countStr}  \u25b8 {nextName} {nextTime.ToString(GetTimeFmt())}";
                    else
                        compact = $"\u25b8 {nextName} {nextTime.ToString(GetTimeFmt())}";
                    string colorHex = GetStatusColorHex(currentPrayer, nextTime);
                    _enhancedTaskbarWindow.UpdateData(compact, colorHex);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Enhanced Taskbar update failed: " + ex.Message);
            }

            // 4. Update Overlay Window (UI)
            if (_overlay == null) return;

            _overlay.OverlayNameText.Text = currentPrayer != Prayer.NONE 
                ? string.Format(LocalizationManager.Instance.GetString("Label_EndsIn"), curName)
                : string.Format(LocalizationManager.Instance.GetString("Label_StartsIn"), nextName);
            _overlay.OverlayCountdownText.Text = countStr;
            
            _overlay.ToolTipCurrentText.Text = string.Format(LocalizationManager.Instance.GetString("Label_CurrentWithRange"), curName, rangeStr);
            _overlay.ToolTipNextText.Text = string.Format(LocalizationManager.Instance.GetString("Label_NextStartsAt"), nextName, nextTime.ToString(GetTimeFmt()));
            _overlay.ForceTopmost();
        }

        private double CalcPrayerProgress(Prayer currentPrayer, DateTime nextTime, DateTime now)
        {
            if (_todayPrayerTimes == null) return 0;
            Prayer prevP = GetPreviousPrayer(now);
            DateTime prevT = _todayPrayerTimes.TimeForPrayer(prevP);
            if (prevP == Prayer.ISHA && now < _todayPrayerTimes.Fajr)
                prevT = _todayPrayerTimes.Isha.AddDays(-1);
            TimeSpan total = nextTime - prevT;
            TimeSpan elapsed = now - prevT;
            if (total.TotalMilliseconds <= 0) return 0;
            return Math.Max(0, Math.Min(1.0, elapsed.TotalMilliseconds / total.TotalMilliseconds));
        }

        private string GetStatusColorHex(Prayer currentPrayer, DateTime nextTime)
        {
            if (_todayPrayerTimes == null) return "#10b981";

            DateTime now = DateTime.Now;
            if (_sunriseProhibActive || _zawalProhibActive || _sunsetProhibActive)
                return "#ef4444";

            if (currentPrayer != Prayer.NONE)
            {
                var endTime = GetPrayerEndTime(currentPrayer);
                if (endTime != DateTime.MaxValue)
                {
                    double minsLeft = (endTime - now).TotalMinutes;
                    if (minsLeft < 10 && minsLeft > 0)
                        return "#fbbf24";
                }
                return "#10b981";
            }

            return "#9ca3af";
        }

        private void UpdateProgressBar(Prayer currentPrayer, DateTime nextTime, DateTime now)
        {
            if (_todayPrayerTimes == null || _tomorrowPrayerTimes == null) return;

            // We calculate progress between the "effective previous" and "effective next" prayer times
            // This ensures the progress bar moves even during Shuruq or before Fajr.
            
            Prayer prevP = GetPreviousPrayer(now);
            DateTime prevT = _todayPrayerTimes.TimeForPrayer(prevP);

            // Handle midnight / early morning crossover for Isha -> Fajr interval
            if (prevP == Prayer.ISHA && now < _todayPrayerTimes.Fajr)
            {
                prevT = _todayPrayerTimes.Isha.AddDays(-1);
            }
            else if (prevP == Prayer.ISHA && now >= _todayPrayerTimes.Isha)
            {
                // We are after Isha today, next is Fajr tomorrow
                // prevT is already today's Isha
            }

            TimeSpan total = nextTime - prevT;
            TimeSpan elapsed = now - prevT;

            if (total.TotalMilliseconds > 0)
            {
                double progress = (elapsed.TotalMilliseconds / total.TotalMilliseconds) * 100;
                HeroProgressBar.Value = Math.Max(0, Math.Min(100, progress));
            }
            else
            {
                HeroProgressBar.Value = 0;
            }
        }

        private DateTime GetPrayerEndTime(Prayer p)
        {
            if (_todayPrayerTimes == null || _tomorrowPrayerTimes == null) return DateTime.MaxValue;
            return p switch
            {
                Prayer.FAJR => _todayPrayerTimes.Sunrise,
                Prayer.DHUHR => _todayPrayerTimes.Asr,
                Prayer.ASR => _todayPrayerTimes.Maghrib,
                Prayer.MAGHRIB => _todayPrayerTimes.Isha,
                Prayer.ISHA => _tomorrowPrayerTimes.Fajr,
                _ => _todayPrayerTimes.Sunrise
            };
        }

        private void ShowJamaatPopup(string prayerName, DateTime jamaatTime)
        {
            if (_activeJamaatPopup != null) _activeJamaatPopup.Close();
            _activeJamaatPopup = new CongregationTimerPopup(prayerName, jamaatTime);
            _activeJamaatPopup.Show();
        }

        private void CheckProhibitedTimes()
        {
            if (_todayPrayerTimes == null) return;
            DateTime now = DateTime.Now;
            var s = SettingsManager.Current;
            string today = now.ToShortDateString();
            
            // Reset daily notification state
            if (_prohibNotifyDate != today)
            {
                _sunriseProhibActive = false;
                _zawalProhibActive = false;
                _sunsetProhibActive = false;
                _prohibNotifyDate = today;
            }

            DateTime sunrise = _todayPrayerTimes.Sunrise;
            DateTime dhuhr = _todayPrayerTimes.Dhuhr;
            DateTime maghrib = _todayPrayerTimes.Maghrib;

            DateTime sunrStart = sunrise, sunrEnd = sunrise.AddMinutes(15);
            DateTime zawlStart = dhuhr.AddMinutes(-30), zawlEnd = dhuhr;
            DateTime sunsStart = maghrib.AddMinutes(-15), sunsEnd = maghrib;

            UpdateProhibCard(SunriseProhibCard, SunriseProhibTimer, null, now, sunrStart, sunrEnd);
            UpdateProhibCard(ZawalProhibCard, ZawalProhibTimer, null, now, zawlStart, zawlEnd);
            UpdateProhibCard(SunsetProhibCard, SunsetProhibTimer, null, now, sunsStart, sunsEnd);
            
            // Populate Range Labels
            var timeFmt = GetTimeFmt();
            SunriseProhibRange.Text = $"{sunrStart.ToString(timeFmt)} - {sunrEnd.ToString(timeFmt)}";
            ZawalProhibRange.Text = $"{zawlStart.ToString(timeFmt)} - {zawlEnd.ToString(timeFmt)}";
            SunsetProhibRange.Text = $"{sunsStart.ToString(timeFmt)} - {sunsEnd.ToString(timeFmt)}";

            // Handle Notifications
            if (s.NotificationsEnabled)
            {
                CheckProhibNotify("Prohibited_Sunrise", now, sunrStart, sunrEnd, ref _sunriseProhibActive);
                CheckProhibNotify("Prohibited_Zawl", now, zawlStart, zawlEnd, ref _zawalProhibActive);
                CheckProhibNotify("Prohibited_Sunset", now, sunsStart, sunsEnd, ref _sunsetProhibActive);
            }

            bool isProhib = (now >= sunrStart && now <= sunrEnd) ||
                           (now >= zawlStart && now <= zawlEnd) ||
                           (now >= sunsStart && now <= sunsEnd);

            ProhibitedWarning.Visibility = isProhib ? Visibility.Visible : Visibility.Collapsed;
        }

        private static void CheckProhibNotify(string nameKey, DateTime now, DateTime start, DateTime end, ref bool isActive)
        {
            if (now >= start && now <= end)
            {
                if (!isActive)
                {
                    ShowNotification(
                        string.Format(LocalizationManager.Instance.GetString("Notify_ProhibitedStarted"), LocalizationManager.Instance.GetString(nameKey)),
                        string.Format(LocalizationManager.Instance.GetString("Notify_ProhibitedStartedMsg"), end.ToString(GetTimeFmt()))
                    );
                    isActive = true;
                }
            }
            else if (now > end && isActive)
            {
                ShowNotification(
                    string.Format(LocalizationManager.Instance.GetString("Notify_ProhibitedEnded"), LocalizationManager.Instance.GetString(nameKey)),
                    LocalizationManager.Instance.GetString("Notify_ProhibitedEndedMsg")
                );
                isActive = false;
            }
        }

        private void UpdateFastingHighlights()
        {
            if (_todayPrayerTimes == null) return;

            int hijriDay = _todayPrayerTimes.HijriDay;
            int hijriMonth = _todayPrayerTimes.HijriMonth;

            if (hijriDay == 0 || hijriMonth == 0)
            {
                try {
                    var calendar = new System.Globalization.UmAlQuraCalendar();
                    var hijriNow = DateTime.Now.AddDays(SettingsManager.Current.HijriAdjustment);
                    hijriDay = calendar.GetDayOfMonth(hijriNow);
                    hijriMonth = calendar.GetMonth(hijriNow);
                } catch { }
            }

            var generalHighlights = new System.Collections.Generic.List<string>();
            var details = new System.Collections.Generic.List<string>();
            bool isFriday = DateTime.Now.DayOfWeek == DayOfWeek.Friday;

            // 1. Prohibited Days
            bool isProhibited = false;
            if (hijriMonth == 10 && hijriDay == 1) { generalHighlights.Add(LocalizationManager.Instance.GetString("Fasting_EidFitr")); isProhibited = true; }
            else if (hijriMonth == 12 && hijriDay == 10) { generalHighlights.Add(LocalizationManager.Instance.GetString("Fasting_EidAdha")); isProhibited = true; }
            else if (hijriMonth == 12 && (hijriDay == 11 || hijriDay == 12 || hijriDay == 13)) { generalHighlights.Add(LocalizationManager.Instance.GetString("Fasting_Tashreeq")); isProhibited = true; }

            if (!isProhibited)
            {
                // 2. Weekly Sunnah Fasts
                var dayOfWeek = DateTime.Now.DayOfWeek;
                if (dayOfWeek == DayOfWeek.Monday) generalHighlights.Add(LocalizationManager.Instance.GetString("Fasting_Monday"));
                else if (dayOfWeek == DayOfWeek.Thursday) generalHighlights.Add(LocalizationManager.Instance.GetString("Fasting_Thursday"));

                // 3. Monthly (White Days)
                if (hijriDay == 13 || hijriDay == 14 || hijriDay == 15) generalHighlights.Add(LocalizationManager.Instance.GetString("Fasting_WhiteDays"));

                // 4. Annual
                if (hijriMonth == 10 && hijriDay > 1 && hijriDay <= 30) generalHighlights.Add(LocalizationManager.Instance.GetString("Fasting_Shawwal"));
                if (hijriMonth == 12 && hijriDay == 9) generalHighlights.Add(LocalizationManager.Instance.GetString("Fasting_Arafah"));
                if (hijriMonth == 1 && hijriDay == 10) generalHighlights.Add(LocalizationManager.Instance.GetString("Fasting_Ashura"));
            }

            // Aggregate Titles and Details
            var aggregatedTitles = new System.Collections.Generic.List<string>(generalHighlights);
            
            if (isFriday)
            {
                aggregatedTitles.Insert(0, LocalizationManager.Instance.GetString("Sunnah_Friday_Title"));
                details.Add(LocalizationManager.Instance.GetString("Sunnah_Friday_Header"));
                details.Add(LocalizationManager.Instance.GetString("Sunnah_Ghusl"));
                details.Add(LocalizationManager.Instance.GetString("Sunnah_Cleaning"));
                details.Add(LocalizationManager.Instance.GetString("Sunnah_Miswak"));
                details.Add(LocalizationManager.Instance.GetString("Sunnah_Dress"));
                details.Add(LocalizationManager.Instance.GetString("Sunnah_Attar"));
                details.Add("\n" + LocalizationManager.Instance.GetString("Sunnah_Spiritual_Header"));
                details.Add(LocalizationManager.Instance.GetString("Sunnah_Kahf"));
                details.Add(LocalizationManager.Instance.GetString("Sunnah_Salawat"));
                details.Add(LocalizationManager.Instance.GetString("Sunnah_EarlyArrival"));
                details.Add(LocalizationManager.Instance.GetString("Sunnah_Khutbah"));
                details.Add("\n" + LocalizationManager.Instance.GetString("Sunnah_Dua_Header"));
                details.Add(LocalizationManager.Instance.GetString("Sunnah_PreMaghrib"));
                details.Add(LocalizationManager.Instance.GetString("Sunnah_DuringKhutbah"));
            }

            if (aggregatedTitles.Count > 0)
            {
                FastingNoteText.Text = "✨ " + string.Join(" | ", aggregatedTitles);
                FastingNoteDetailsText.Text = string.Join("\n", details);
                
                // Show toggle only if we have details
                FastingNoteToggleBtn.Visibility = details.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
                FastingNoteBorder.Visibility = Visibility.Visible;
            }
            else
            {
                FastingNoteBorder.Visibility = Visibility.Collapsed;
            }
        }

        private bool IsReminderEnabled(Prayer p, AppSettings s)
        {
            return p switch
            {
                Prayer.FAJR => s.ReminderFajr,
                Prayer.SUNRISE => s.ReminderShuruq,
                Prayer.DHUHR => s.ReminderDhuhr,
                Prayer.ASR => s.ReminderAsr,
                Prayer.MAGHRIB => s.ReminderMaghrib,
                Prayer.ISHA => s.ReminderIsha,
                _ => false
            };
        }

        private bool IsAdhanEnabled(Prayer p, AppSettings s)
        {
            return p switch
            {
                Prayer.FAJR => s.AdhanFajr,
                Prayer.DHUHR => s.AdhanDhuhr,
                Prayer.ASR => s.AdhanAsr,
                Prayer.MAGHRIB => s.AdhanMaghrib,
                Prayer.ISHA => s.AdhanIsha,
                _ => false
            };
        }

        private bool IsEstablishedEnabled(Prayer p, AppSettings s)
        {
            return p switch
            {
                Prayer.FAJR => s.EstablishedFajr,
                Prayer.DHUHR => s.EstablishedDhuhr,
                Prayer.ASR => s.EstablishedAsr,
                Prayer.MAGHRIB => s.EstablishedMaghrib,
                Prayer.ISHA => s.EstablishedIsha,
                _ => false
            };
        }

        private static string FormatPrayerName(Prayer p)
        {
            if (p == Prayer.SUNRISE) return LocalizationManager.Instance.GetString("Prayer_Sunrise");
            if (p == Prayer.FAJR) return LocalizationManager.Instance.GetString("Prayer_Fajr");
            if (p == Prayer.DHUHR) return LocalizationManager.Instance.GetString("Prayer_Dhuhr");
            if (p == Prayer.ASR) return LocalizationManager.Instance.GetString("Prayer_Asr");
            if (p == Prayer.MAGHRIB) return LocalizationManager.Instance.GetString("Prayer_Maghrib");
            if (p == Prayer.ISHA) return LocalizationManager.Instance.GetString("Prayer_Isha");
            return p.ToString();
        }

        private Prayer GetPreviousPrayer(DateTime now)
        {
            if (_todayPrayerTimes == null) return Prayer.ISHA;
            
            if (now < _todayPrayerTimes.Fajr) return Prayer.ISHA; // Was Isha of yesterday
            if (now < _todayPrayerTimes.Sunrise) return Prayer.FAJR;
            if (now < _todayPrayerTimes.Dhuhr) return Prayer.SUNRISE;
            if (now < _todayPrayerTimes.Asr) return Prayer.DHUHR;
            if (now < _todayPrayerTimes.Maghrib) return Prayer.ASR;
            if (now < _todayPrayerTimes.Isha) return Prayer.MAGHRIB;
            
            return Prayer.ISHA;
        }

        private Prayer GetNextPrayerAfter(Prayer p)
        {
            return p switch
            {
                Prayer.FAJR => Prayer.SUNRISE,
                Prayer.SUNRISE => Prayer.DHUHR,
                Prayer.DHUHR => Prayer.ASR,
                Prayer.ASR => Prayer.MAGHRIB,
                Prayer.MAGHRIB => Prayer.ISHA,
                Prayer.ISHA => Prayer.FAJR,
                _ => Prayer.FAJR
            };
        }

        private void UpdatePrayerListHighlighting(Prayer curPrayer)
        {
            ResetHighlighting(this);
            var activeBg = new SolidColorBrush(WColor.FromArgb(40, 255, 255, 255));
            var activeNameBrush = new SolidColorBrush(WColor.FromRgb(52, 211, 153));
            var whiteBrush = new SolidColorBrush(Colors.White);

            switch (curPrayer)
            {
                case Prayer.FAJR: SetCardHighlight(FajrCard, FajrNameText, FajrTimeText, activeBg, activeNameBrush, whiteBrush); break;
                case Prayer.SUNRISE: SetCardHighlight(SunriseCard, SunriseNameText, SunriseTimeText, activeBg, activeNameBrush, whiteBrush); break;
                case Prayer.DHUHR: SetCardHighlight(DhuhrCard, DhuhrNameText, DhuhrTimeText, activeBg, activeNameBrush, whiteBrush); break;
                case Prayer.ASR: SetCardHighlight(AsrCard, AsrNameText, AsrTimeText, activeBg, activeNameBrush, whiteBrush); break;
                case Prayer.MAGHRIB: SetCardHighlight(MaghribCard, MaghribNameText, MaghribTimeText, activeBg, activeNameBrush, whiteBrush); break;
                case Prayer.ISHA: SetCardHighlight(IshaCard, IshaNameText, IshaTimeText, activeBg, activeNameBrush, whiteBrush); break;
            }
        }

        private static void ResetHighlighting(MainWindow window)
        {
            var transparentBg = new SolidColorBrush(WColor.FromArgb(26, 255, 255, 255));
            var whiteBrush = new SolidColorBrush(Colors.White);
            var greenBrush = new SolidColorBrush(WColor.FromRgb(52, 211, 153));
            
            SetCardHighlight(window.FajrCard, window.FajrNameText, window.FajrTimeText, transparentBg, whiteBrush, greenBrush);
            SetCardHighlight(window.SunriseCard, window.SunriseNameText, window.SunriseTimeText, transparentBg, whiteBrush, greenBrush);
            SetCardHighlight(window.DhuhrCard, window.DhuhrNameText, window.DhuhrTimeText, transparentBg, whiteBrush, greenBrush);
            SetCardHighlight(window.AsrCard, window.AsrNameText, window.AsrTimeText, transparentBg, whiteBrush, greenBrush);
            SetCardHighlight(window.MaghribCard, window.MaghribNameText, window.MaghribTimeText, transparentBg, whiteBrush, greenBrush);
            SetCardHighlight(window.IshaCard, window.IshaNameText, window.IshaTimeText, transparentBg, whiteBrush, greenBrush);
        }

        private static void SetCardHighlight(System.Windows.Controls.Border card, System.Windows.Controls.TextBlock name, System.Windows.Controls.TextBlock time, SolidColorBrush bg, SolidColorBrush nameFg, SolidColorBrush timeFg)
        {
            card.Background = bg;
            name.Foreground = nameFg;
            time.Foreground = timeFg;
        }
        private string GetRakatNote(Prayer p)
        {
            return p switch
            {
                Prayer.FAJR => LocalizationManager.Instance.GetString("Note_Fajr"),
                Prayer.DHUHR => LocalizationManager.Instance.GetString("Note_Dhuhr"),
                Prayer.ASR => LocalizationManager.Instance.GetString("Note_Asr"),
                Prayer.MAGHRIB => LocalizationManager.Instance.GetString("Note_Maghrib"),
                Prayer.ISHA => LocalizationManager.Instance.GetString("Note_Isha"),
                _ => ""
            };
        }

        // --- Localization Helpers ---

        private static string FormatCountdown(int hours, int minutes, int seconds)
        {
            var lm = LocalizationManager.Instance;
            return $"{hours}{lm.GetString("Unit_Hour_Short")} {minutes}{lm.GetString("Unit_Min_Short")} {seconds}{lm.GetString("Unit_Sec_Short")}";
        }

        private static string GetLocalizedDate(DateTime dt)
        {
            string month = LocalizationManager.Instance.GetString($"Month_Gregorian_{dt.Month}");
            return $"{dt.Day} {month} {dt.Year}";
        }

        private static string GetLocalizedDayName(DateTime dt)
        {
            return LocalizationManager.Instance.GetString($"Day_{(int)dt.DayOfWeek}");
        }

        // --- Tab Event Handlers ---

        private void TabHome_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsInitialized || HeroBorder == null || TrackerViewControl == null) return;
            _isTrackerMode = false;
            TrackerViewControl.Visibility = Visibility.Collapsed;
            QiblaViewControl.Visibility = Visibility.Collapsed;
            TasbihViewControl.Visibility = Visibility.Collapsed;
            RamadanViewControl.Visibility = Visibility.Collapsed;
            
            MainContentStack.VerticalAlignment = VerticalAlignment.Center;
            HeroBorder.Visibility = Visibility.Visible;
            PrayerListScroll.Visibility = Visibility.Visible;
            
            HighlightsHeader.Visibility = Visibility.Visible;
            HighlightsGrid.Visibility = Visibility.Visible;
            UpdateFastingHighlights(); 
            ProhibitedHeader.Visibility = Visibility.Visible;
            ProhibitedGrid.Visibility = Visibility.Visible;

            FardHeader.Visibility = Visibility.Collapsed;
            FardCardsPanel.Visibility = Visibility.Collapsed;
            NafalHeader.Visibility = Visibility.Collapsed;
            NafalCardsPanel.Visibility = Visibility.Collapsed;

            // Zen Mode is only available on the Home tab — re-enable the button
            ZenModeBtn.IsEnabled = true;
            ZenModeBtn.Opacity = 1.0;
        }

        private void TabPrayers_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsInitialized || HeroBorder == null || TrackerViewControl == null) return;
            _isTrackerMode = false;
            TrackerViewControl.Visibility = Visibility.Collapsed;
            QiblaViewControl.Visibility = Visibility.Collapsed;
            TasbihViewControl.Visibility = Visibility.Collapsed;
            RamadanViewControl.Visibility = Visibility.Collapsed;

            // Zen Mode is Home-tab only — exit it and disable the button
            ExitZenMode();
            ZenModeBtn.IsEnabled = false;
            ZenModeBtn.Opacity = 0.35;
            
            MainContentStack.VerticalAlignment = VerticalAlignment.Top;
            HeroBorder.Visibility = Visibility.Collapsed;
            PrayerListScroll.Visibility = Visibility.Visible;

            HighlightsHeader.Visibility = Visibility.Collapsed;
            HighlightsGrid.Visibility = Visibility.Collapsed;
            FastingNoteBorder.Visibility = Visibility.Collapsed;
            ProhibitedHeader.Visibility = Visibility.Collapsed;
            ProhibitedGrid.Visibility = Visibility.Collapsed;

            FardHeader.Visibility = Visibility.Visible;
            FardCardsPanel.Visibility = Visibility.Visible;
            NafalHeader.Visibility = Visibility.Visible;
            NafalCardsPanel.Visibility = Visibility.Visible;
        }

        private void TabTracker_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsInitialized || HeroBorder == null || TrackerViewControl == null) return;
            _isTrackerMode = true;
            TrackerViewControl.Visibility = Visibility.Visible;
            QiblaViewControl.Visibility = Visibility.Collapsed;
            TasbihViewControl.Visibility = Visibility.Collapsed;
            RamadanViewControl.Visibility = Visibility.Collapsed;
            HeroBorder.Visibility = Visibility.Collapsed;
            PrayerListScroll.Visibility = Visibility.Collapsed;
            UpdateBanner.Visibility = Visibility.Collapsed;

            // Zen Mode is Home-tab only — exit it and disable the button
            ExitZenMode();
            ZenModeBtn.IsEnabled = false;
            ZenModeBtn.Opacity = 0.35;

            var enabledPrayers = GetEnabledTrackerPrayers();
            TrackerViewControl.LoadData(enabledPrayers);
        }

        private void TabQibla_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsInitialized || QiblaViewControl == null) return;
            _isTrackerMode = false;
            TrackerViewControl.Visibility = Visibility.Collapsed;
            TasbihViewControl.Visibility = Visibility.Collapsed;
            RamadanViewControl.Visibility = Visibility.Collapsed;
            PrayerListScroll.Visibility = Visibility.Collapsed;
            QiblaViewControl.Visibility = Visibility.Visible;
            ExitZenMode();
            ZenModeBtn.IsEnabled = false;
            ZenModeBtn.Opacity = 0.35;
            QiblaViewControl.UpdateDirection();
        }

        private void TabTasbih_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsInitialized || TasbihViewControl == null) return;
            _isTrackerMode = false;
            TrackerViewControl.Visibility = Visibility.Collapsed;
            QiblaViewControl.Visibility = Visibility.Collapsed;
            RamadanViewControl.Visibility = Visibility.Collapsed;
            PrayerListScroll.Visibility = Visibility.Collapsed;
            TasbihViewControl.Visibility = Visibility.Visible;
            ExitZenMode();
            ZenModeBtn.IsEnabled = false;
            ZenModeBtn.Opacity = 0.35;
            TasbihViewControl.LoadData();
        }

        private void TabRamadan_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsInitialized || RamadanViewControl == null) return;
            _isTrackerMode = false;
            TrackerViewControl.Visibility = Visibility.Collapsed;
            QiblaViewControl.Visibility = Visibility.Collapsed;
            TasbihViewControl.Visibility = Visibility.Collapsed;
            PrayerListScroll.Visibility = Visibility.Collapsed;
            RamadanViewControl.Visibility = Visibility.Visible;
            ExitZenMode();
            ZenModeBtn.IsEnabled = false;
            ZenModeBtn.Opacity = 0.35;
            RamadanViewControl.LoadData();
        }

        public CombinedPrayerTimes? GetTodayPrayerTimes() => _todayPrayerTimes;

        private void SyncToolbarIcons()
        {
            var activeBg = new SolidColorBrush((WColor)WColorConverter.ConvertFromString("#34D399"));
            var transBg = System.Windows.Media.Brushes.Transparent;
            var darkFg = new SolidColorBrush((WColor)WColorConverter.ConvertFromString("#111827"));
            var whiteFg = System.Windows.Media.Brushes.White;

            // 1. Ramadan Icon
            RamadanBtn.Background = _isRamadanMode ? activeBg : transBg;
            RamadanIcon.Foreground = _isRamadanMode ? darkFg : whiteFg;
            RamadanIcon.Opacity = 1.0;

            // 3. Zen Mode Icon
            ZenModeBtn.Background = _isZenMode ? activeBg : transBg;
            ZenModeIcon.Foreground = _isZenMode ? darkFg : whiteFg;
            ZenModeIcon.Opacity = 1.0;

            // 4. Settings & System Icons (Always White/Visible)
            SettingsIcon.Foreground = whiteFg;
            SettingsIcon.Opacity = 1.0;
        }

        public HashSet<string> GetEnabledTrackerPrayers()
        {
            var enabled = new HashSet<string>();
            if (_todayPrayerTimes == null) return enabled;

            DateTime now = DateTime.Now;

            // Fajr & Adhkar
            if (now >= _todayPrayerTimes.Fajr) 
            {
                enabled.Add("Fajr");
                enabled.Add("Adhkar"); // Card + Morning Adhkar
            }
            
            // Duha / Ishraq
            if (now >= _todayPrayerTimes.Sunrise.AddMinutes(15))
            {
                enabled.Add("Ishraq");
                enabled.Add("Duha");
            }

            // On Fridays, enable "Jumuah" instead of "Dhuhr" — TrackerView shows the Jumuah section on Fridays
            if (now >= _todayPrayerTimes.Dhuhr)
            {
                if (now.DayOfWeek == DayOfWeek.Friday)
                    enabled.Add("Jumuah");
                else
                    enabled.Add("Dhuhr");
            }
            
            // Asr & Evening Adhkar
            if (now >= _todayPrayerTimes.Asr) 
            {
                enabled.Add("Asr");
                enabled.Add("Adhkar_Evening");
            }
            
            // Maghrib & Awwabin
            if (now >= _todayPrayerTimes.Maghrib) 
            {
                enabled.Add("Maghrib");
                enabled.Add("Awwabin");
            }
            
            // Isha & Tahajjud
            if (now >= _todayPrayerTimes.Isha) 
            {
                enabled.Add("Isha");
                enabled.Add("Tahajjud");
            }

            return enabled;
        }

        private void RakatCheck_Changed(object sender, RoutedEventArgs e)
        {
            if (HeroRakatList.ItemsSource is List<DeedEntry>)
            {
                TrackerService.Instance.SaveDay(_currentDeeds);
                UpdateOverallTrackerProgress();
                
                if (TrackerViewControl != null)
                {
                    TrackerViewControl.ReloadCurrentDate();
                }
            }
        }

        public void ReloadHeroTrackerFromDisk()
        {
            if (_currentDeeds == null || HeroTrackerTitle == null || string.IsNullOrEmpty(HeroTrackerTitle.Text)) return;
            
            _currentDeeds = TrackerService.Instance.LoadDay(DateTime.Today);
            string title = HeroTrackerTitle.Text; 
            string pKey = title.Replace(" TRACKER", ""); 
            
            pKey = pKey.ToLower();
            if (pKey == "fajr") pKey = "Fajr";
            else if (pKey == "dhuhr") pKey = "Dhuhr";
            else if (pKey == "jumu'ah" || pKey == "jumuah") pKey = "Jumuah";
            else if (pKey == "asr") pKey = "Asr";
            else if (pKey == "maghrib") pKey = "Maghrib";
            else if (pKey == "isha") pKey = "Isha";

            if (_currentDeeds.Prayers.TryGetValue(pKey, out var deeds))
            {
                HeroRakatList.ItemsSource = null;
                HeroRakatList.ItemsSource = deeds;
                UpdateOverallTrackerProgress();
            }
        }

        private void SawmTrack_Changed(object sender, RoutedEventArgs e)
        {
            if (_currentDeeds == null) return;
            bool isChecked = (sender as System.Windows.Controls.CheckBox)?.IsChecked ?? false;
            
            if (_currentDeeds.Sawm != isChecked)
            {
                _currentDeeds.Sawm = isChecked;
                TrackerService.Instance.SaveDay(_currentDeeds);
                
                // Sync UI
                if (HighlightsSawmTrack != null && HighlightsSawmTrack.IsChecked != isChecked) HighlightsSawmTrack.IsChecked = isChecked;
                
                if (TrackerViewControl != null)
                {
                    TrackerViewControl.SyncSawmRemote(isChecked);
                }
            }
        }

        public void SyncSawmRemote(bool isChecked)
        {
            if (_currentDeeds != null) _currentDeeds.Sawm = isChecked;
            if (HighlightsSawmTrack != null)
            {
                HighlightsSawmTrack.IsChecked = isChecked;
            }
        }

        private string ToPrayerKey(Prayer prayer)
        {
            return prayer switch
            {
                Prayer.FAJR    => "Fajr",
                Prayer.DHUHR   => "Dhuhr",
                Prayer.ASR     => "Asr",
                Prayer.MAGHRIB => "Maghrib",
                Prayer.ISHA    => "Isha",
                _              => "Fajr"
            };
        }

        private void SyncTrackerWithHero(Prayer currentPrayer)
        {
            if (_currentDeeds == null) return;

            // Map Prayer enum to title-case string keys used in the Deeds dictionary
            string pKey = ToPrayerKey(currentPrayer == Prayer.NONE ? Prayer.FAJR : currentPrayer);
            if (currentPrayer == Prayer.DHUHR && DateTime.Now.DayOfWeek == DayOfWeek.Friday) pKey = "Jumuah";

            if (_currentDeeds.Prayers.TryGetValue(pKey, out var deeds) && SettingsManager.Current.TrackerEnabled)
            {
                HeroTrackerBox.Visibility = Visibility.Visible;
                HeroTrackerTitle.Text = $"{pKey.ToUpper()} TRACKER";
                HeroRakatList.ItemsSource = deeds;
                UpdateOverallTrackerProgress();
            }
            else
            {
                HeroTrackerBox.Visibility = Visibility.Collapsed;
            }

            // Fasting / Highlights Section Logic
            bool isSawmDay = TrackerService.Instance.IsSunnahSawmDay(DateTime.Today);
            // In normal mode, we always show the tracker if enabled. 
            // In Ramadan mode, we always show the tracker if enabled.
            bool showHighlightsSawm = SettingsManager.Current.TrackerEnabled;
            
            if (HighlightsSawmTrack != null)
            {
                HighlightsSawmTrack.Visibility = showHighlightsSawm ? Visibility.Visible : Visibility.Collapsed;
                HighlightsSawmTrack.IsChecked = _currentDeeds.Sawm;
            }

            if (HighlightsGrid != null)
            {
                if (_isRamadanMode) 
                {
                    // In Ramadan mode, Hero section already shows Suhur/Iftar
                    // So we only show the Fasting Tracker card here
                    if (SuhurCard != null) SuhurCard.Visibility = Visibility.Collapsed;
                    if (IftarCard != null) IftarCard.Visibility = Visibility.Collapsed;
                    HighlightsGrid.Columns = 1;
                }
                else 
                {
                    // In Normal mode, show all 3 cards
                    if (SuhurCard != null) SuhurCard.Visibility = Visibility.Visible;
                    if (IftarCard != null) IftarCard.Visibility = Visibility.Visible;
                    HighlightsGrid.Columns = showHighlightsSawm ? 3 : 2;
                }
            }
        }

        private void UpdateOverallTrackerProgress()
        {
            if (HeroRakatList.ItemsSource is List<DeedEntry> deeds)
            {
                var fardDeed = deeds.FirstOrDefault(d => d.Type == DeedType.Fard);
                if (fardDeed != null)
                {
                    HeroTrackerProgress.Text = fardDeed.IsChecked ? "1/1 Completed" : "0/1 Completed";
                }
                else
                {
                    int done = deeds.Count(d => d.IsChecked);
                    HeroTrackerProgress.Text = $"{done}/{deeds.Count} Completed";
                }
            }
        }
    }
}
