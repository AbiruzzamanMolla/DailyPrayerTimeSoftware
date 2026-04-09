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
    public partial class MainWindow : Window
    {
        private const string TimeFmtFull = "hh:mm tt";
        private const string TimeFmtShort = "hh:mm";
        private const string CountdownFmt = "{0:D2}:{1:D2}:{2:D2}";

        private DispatcherTimer? _timer;
        private CombinedPrayerTimes? _todayPrayerTimes;
        private CombinedPrayerTimes? _tomorrowPrayerTimes;
        private OverlayWindow? _overlay;
        private TaskbarWindow? _taskbarWindow;
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
        private Prayer _lastJamaatNotificationID = Prayer.NONE;
        private Prayer _lastEndNotificationID = Prayer.NONE;
        private UpdateInfo? _currentUpdate;
        
        private bool IsWindows11 => Environment.OSVersion.Version.Major >= 10 && Environment.OSVersion.Version.Build >= 22000;

        public MainWindow()
        {
            SettingsManager.Load();
            InitializeComponent();
            ApplySettingsTheme();
            SetupTimer();
            SetupTrayIcon();
            _ = CalculatePrayerTimes();
            Task.Run(async () => await DownloadDefaultAdhan());
            _ = CheckForUpdates();
            ManageOverlay();
            ManageIntegratedTaskbar();
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
                        UpdateVersionText.Text = $"Version v{_currentUpdate.LatestVersion} is now available.";
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
            _notifyIcon.Text = "Daily Prayer Timer";

            var cms = new Forms.ContextMenuStrip();
            
            cms.Items.Add("Open", null, (s, e) => { Show(); WindowState = WindowState.Normal; Activate(); });
            
            cms.Items.Add("Settings", null, async (s, e) => { 
                var sw = new SettingsWindow(_todayPrayerTimes, _tomorrowPrayerTimes);
                if (sw.ShowDialog() == true) {
                    ApplySettingsTheme();
                    await CalculatePrayerTimes();
                    ManageOverlay();
                    ManageIntegratedTaskbar();
                }
            });

            cms.Items.Add(new Forms.ToolStripSeparator());

            var overlayItem = new Forms.ToolStripMenuItem("Show Floating Overlay");
            overlayItem.CheckOnClick = true;
            overlayItem.Checked = SettingsManager.Current.ShowOverlay;
            overlayItem.Click += (s, e) => {
                SettingsManager.Current.ShowOverlay = overlayItem.Checked;
                SettingsManager.Save();
                ManageOverlay();
            };
            cms.Items.Add(overlayItem);

            var deskbandItem = new Forms.ToolStripMenuItem("Show DeskBand (Legacy)");
            deskbandItem.CheckOnClick = true;
            deskbandItem.Checked = SettingsManager.Current.UseDeskBand;
            deskbandItem.Click += (s, e) => {
                SettingsManager.Current.UseDeskBand = deskbandItem.Checked;
                SettingsManager.Save();
                // DeskBand is handled by Explorer, we just update data
            };
            cms.Items.Add(deskbandItem);

            var integratedItem = new Forms.ToolStripMenuItem("Show Integrated Taskbar Timer (Win 11 Source)");
            integratedItem.CheckOnClick = true;
            integratedItem.Checked = SettingsManager.Current.UseIntegratedTaskbar;
            integratedItem.Click += (s, e) => {
                SettingsManager.Current.UseIntegratedTaskbar = integratedItem.Checked;
                SettingsManager.Save();
                ManageIntegratedTaskbar();
            };
            cms.Items.Add(integratedItem);

            cms.Items.Add(new Forms.ToolStripSeparator());
            cms.Items.Add("Exit", null, (s, e) => System.Windows.Application.Current.Shutdown());

            _notifyIcon.ContextMenuStrip = cms;

            // Ensure checkmarks stay in sync when menu opens
            cms.Opening += (s, e) => {
                overlayItem.Checked = SettingsManager.Current.ShowOverlay;
                deskbandItem.Checked = SettingsManager.Current.UseDeskBand;
                integratedItem.Checked = SettingsManager.Current.UseIntegratedTaskbar;
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
                    _notifyIcon.ShowBalloonTip(2000, "Daily Prayer Timer", "App is running in the background.", Forms.ToolTipIcon.Info);
                }
            }
            base.OnStateChanged(e);
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            var sw = new SettingsWindow(_todayPrayerTimes, _tomorrowPrayerTimes);
            if (sw.ShowDialog() == true)
            {
                ApplySettingsTheme();
                _ = CalculatePrayerTimes();
                ManageOverlay();
                ManageIntegratedTaskbar();
            }
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
                    System.Windows.MessageBox.Show("Could not open the update link: " + ex.Message);
                }
            }
        }
        
        private void ApplySettingsTheme()
        {
            var s = SettingsManager.Current;
            LocationDisplay.Text = "📍 " + s.LocationName;
            
            var primaryBrush = new SolidColorBrush((WColor)WColorConverter.ConvertFromString(s.PrimaryColor));
            LocationDisplay.Foreground = primaryBrush;
            
            // Hero section background
            HeroBorder.Background = primaryBrush;
            FooterBorder.Background = primaryBrush;
            HijriDateDisplay.Foreground = primaryBrush;

            try 
            {
                var mainBrush = (LinearGradientBrush)this.Resources["MainGradient"];
                mainBrush.GradientStops[0].Color = (WColor)WColorConverter.ConvertFromString(s.GradientStart);
                mainBrush.GradientStops[1].Color = (WColor)WColorConverter.ConvertFromString(s.GradientEnd);
            } 
            catch (Exception) { /* Invalid color format */ }
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
                EnglishDateDisplay.Text = DateTime.Now.ToString("dddd, MMM d");
                HijriDateDisplay.Text = string.IsNullOrEmpty(_todayPrayerTimes.HijriDate) ? GetHijriDate() : _todayPrayerTimes.HijriDate;
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

        private static string GetHijriDate()
        {
            try {
                var now = DateTime.Now;
                var hijri = new System.Globalization.UmAlQuraCalendar();
                int year = hijri.GetYear(now);
                int month = hijri.GetMonth(now);
                int day = hijri.GetDayOfMonth(now);

                string[] months = {
                    "Muharram", "Safar", "Rabi' al-Awwal", "Rabi' al-Thani",
                    "Jumada al-Awwal", "Jumada al-Thani", "Rajab", "Sha'ban",
                    "Ramadan", "Shawwal", "Dhu al-Qi'dah", "Dhu al-Hijjah"
                };

                return $"{day} {months[month - 1]} {year} AH";
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
                
                SuhurTimeText.Text = $"{_todayPrayerTimes.Suhur.ToString(timeFmt)}";
                IftarTimeText.Text = $"{_todayPrayerTimes.Iftar.ToString(timeFmt)}";

                string todayDateShort = DateTime.Now.ToString("ddd, MMM d");
                SuhurDateText.Text = todayDateShort;
                IftarDateText.Text = todayDateShort;
                
                UpdateFastingHighlights();
                
                HeroSunriseText.Text = "☀ Sunrise: " + _todayPrayerTimes.Sunrise.ToString(timeFmt);
                HeroSunsetText.Text = "🌆 Sunset: " + _todayPrayerTimes.Maghrib.ToString(timeFmt);
                
                DateTime sunrise = _todayPrayerTimes.Sunrise;
                DateTime dhuhr = _todayPrayerTimes.Dhuhr;
                DateTime maghrib = _todayPrayerTimes.Maghrib;

                SunriseProhibRange.Text = $"{sunrise.ToString(TimeFmtShort)} - {sunrise.AddMinutes(15).ToString(TimeFmtShort)}";
                ZawalProhibRange.Text = $"{dhuhr.AddMinutes(-30).ToString(TimeFmtShort)} - {dhuhr.ToString(TimeFmtShort)}";
                SunsetProhibRange.Text = $"{maghrib.AddMinutes(-15).ToString(TimeFmtShort)} - {maghrib.ToString(TimeFmtShort)}";

                // Nafal Prayers Calculations
                DateTime duhaStart = sunrise.AddMinutes(15);
                DateTime duhaEnd = dhuhr.AddMinutes(-15);
                DuhaTimeText.Text = $"{duhaStart.ToString(timeFmt)} - {duhaEnd.ToString(timeFmt)}";

                DateTime awwabinStart = maghrib.AddMinutes(15);
                DateTime awwabinEnd = _todayPrayerTimes.Isha.AddMinutes(-15);
                AwwabinTimeText.Text = $"{awwabinStart.ToString(timeFmt)} - {awwabinEnd.ToString(timeFmt)}";

                DateTime tahajjudStart = _todayPrayerTimes.Isha.AddMinutes(15);
                DateTime tahajjudEnd = _tomorrowPrayerTimes.Fajr.AddMinutes(-10);
                TahajjudTimeText.Text = $"{tahajjudStart.ToString(timeFmt)} - {tahajjudEnd.ToString(timeFmt)}";

                TimeSpan nightDuration = _tomorrowPrayerTimes.Fajr - maghrib;
                TimeSpan oneThird = new TimeSpan(nightDuration.Ticks / 3);
                DateTime lastThirdStart = _tomorrowPrayerTimes.Fajr - oneThird;
                LastThirdTimeText.Text = $"Last 1/3 begins: {lastThirdStart.ToString(timeFmt)}";

                UpdateCountdown();
            });
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (_todayPrayerTimes == null) return;
            
            DateTime now = DateTime.Now;
            var currentPrayer = _todayPrayerTimes.CurrentPrayer(now);

            UpdateCountdown();
            CheckEnhancedNotifications(now, currentPrayer);
            CheckProhibitedTimes();
            CheckAndShowJamaatAlarm();
            CheckAndPlayAdhanAlarm(currentPrayer);
        }

        private void CheckEnhancedNotifications(DateTime now, Prayer currentPrayer)
        {
            if (!SettingsManager.Current.NotificationsEnabled) return;

            // 1. Prayer Start Notification
            if (currentPrayer != _lastStartNotificationPrayer && currentPrayer != Prayer.NONE)
            {
                ShowNotification($"{FormatPrayerName(currentPrayer)} time started", $"The time for {FormatPrayerName(currentPrayer)} has begun ({now.ToString(GetTimeFmt())}).");
                _lastStartNotificationPrayer = currentPrayer;
            }

            // 2. Jamaat Notification
            var jamaatTime = GetJamaatTime(currentPrayer, SettingsManager.Current, now);
            if (jamaatTime.HasValue && _lastJamaatNotificationID != currentPrayer)
            {
                // Trigger exactly at jamaat time or within 1 minute
                if (now >= jamaatTime.Value && now < jamaatTime.Value.AddMinutes(1))
                {
                    ShowNotification($"{FormatPrayerName(currentPrayer)} Jamaat Now", $"Congregation for {FormatPrayerName(currentPrayer)} is starting at {jamaatTime.Value.ToString(GetTimeFmt())}.");
                    _lastJamaatNotificationID = currentPrayer;
                }
            }

            // 3. Prayer End Warning (10 mins before)
            var nextResult = GetNextPrayerInfo(now);
            if (nextResult.nextTime != DateTime.MinValue && _lastEndNotificationID != currentPrayer)
            {
                TimeSpan timeToNext = nextResult.nextTime - now;
                if (timeToNext.TotalMinutes > 0 && timeToNext.TotalMinutes <= 10)
                {
                    ShowNotification($"{FormatPrayerName(currentPrayer)} ending soon", $"{FormatPrayerName(currentPrayer)} time will end in {Math.Ceiling(timeToNext.TotalMinutes)} minutes.");
                    _lastEndNotificationID = currentPrayer;
                }
            }
            
            // Reset "End Notification" state when prayer changes to allow next one
            if (currentPrayer != _lastEndNotificationID && currentPrayer != Prayer.NONE)
            {
                // This logic is handled by the ID check above, but we ensure consistency
            }
        }

        private static void ShowNotification(string title, string message)
        {
            new ToastContentBuilder()
                .AddText(title)
                .AddText(message)
                .Show();
        }

        private void CheckAndPlayAdhanAlarm(Prayer currentPrayer)
        {
            var s = SettingsManager.Current;
            if (!s.AdhanAlarmEnabled || string.IsNullOrEmpty(s.AdhanSoundPath)) return;

            // Only trigger if we haven't played it for this prayer today
            string today = DateTime.Now.ToShortDateString();
            if (_lastAdhanPrayer == currentPrayer && _lastAdhanDate == today) return;

            DateTime? jamaatTime = GetJamaatTime(currentPrayer != Prayer.NONE ? currentPrayer : Prayer.FAJR, s, DateTime.Now);
            if (!jamaatTime.HasValue) return;

            DateTime adhanTriggerTime = jamaatTime.Value.AddMinutes(-s.AdhanAlarmOffset);
            DateTime now = DateTime.Now;

            // Trigger if within a 30-second window of the offset
            if (now >= adhanTriggerTime && now < adhanTriggerTime.AddSeconds(30))
            {
                try
                {
                    if (System.IO.File.Exists(s.AdhanSoundPath))
                    {
                        _adhanPlayer.Open(new Uri(s.AdhanSoundPath));
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
    
        private void CheckAndShowJamaatAlarm()
        {
            if (_todayPrayerTimes == null || _tomorrowPrayerTimes == null) return;

            DateTime now = DateTime.Now;
            var s = SettingsManager.Current;

            // List of prayers to check for jamaat
            Prayer[] prayers = { Prayer.FAJR, Prayer.DHUHR, Prayer.ASR, Prayer.MAGHRIB, Prayer.ISHA };

            foreach (var p in prayers)
            {
                if (CheckAndShowJamaatAlarm(p, now, s)) return;
            }

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


        private static void UpdateProhibCard(System.Windows.Controls.Border card, System.Windows.Controls.TextBlock timerText, DateTime now, DateTime start, DateTime end)
        {
            if (now >= start && now <= end)
            {
                card.Background = new SolidColorBrush(WColor.FromArgb(150, 239, 68, 68)); // semi-transparent red
                TimeSpan rem = end - now;
                timerText.Text = $"{rem.Minutes:D2}:{rem.Seconds:D2}";
            }
            else
            {
                card.Background = new SolidColorBrush(WColor.FromArgb(26, 255, 255, 255)); // 10% white (glass)
                if (now < start)
                {
                    TimeSpan rem = start - now;
                    timerText.Text = rem.TotalHours < 24 ? $"-{rem.Hours:D2}:{rem.Minutes:D2}" : "Upcoming";
                }
                else
                {
                    timerText.Text = "Passed";
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

            string countStr = string.Format(CountdownFmt, remaining.Hours, remaining.Minutes, remaining.Seconds);
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

            UpdateHeroSection(heroPrayer, curName, nextName, heroCountStr);
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

                    // Update Hero Status Vector Icon (Truly Transparent)
                    HeroStatusIconPath.Data = geom;
                    
                    // Dynamic Colors for the status icon
                    if (now >= _todayPrayerTimes.Fajr && now < _todayPrayerTimes.Sunrise)
                        HeroStatusIconPath.Fill = new SolidColorBrush(WColor.FromRgb(251, 146, 60)); // orange-400
                    else if (now >= _todayPrayerTimes.Sunrise && now < _todayPrayerTimes.Dhuhr)
                        HeroStatusIconPath.Fill = new SolidColorBrush(WColor.FromRgb(251, 191, 36)); // amber-400
                    else if (now >= _todayPrayerTimes.Dhuhr && now < _todayPrayerTimes.Asr)
                        HeroStatusIconPath.Fill = new SolidColorBrush(WColor.FromRgb(252, 211, 77)); // amber-300
                    else if (now >= _todayPrayerTimes.Asr && now < _todayPrayerTimes.Maghrib)
                        HeroStatusIconPath.Fill = new SolidColorBrush(WColor.FromRgb(248, 113, 113)); // red-400
                    else
                        HeroStatusIconPath.Fill = new SolidColorBrush(WColor.FromRgb(226, 232, 240)); // slate-200 (Moon)
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
                textBlock.Text = "Passed";
                textBlock.Foreground = new SolidColorBrush(WColor.FromRgb(156, 163, 175)); // slate-400
            }
            else
            {
                TimeSpan rem = target - now;
                textBlock.Text = $"-{rem.Hours:D2}:{rem.Minutes:D2}:{rem.Seconds:D2}";
                textBlock.Foreground = new SolidColorBrush(WColor.FromRgb(255, 255, 255)); // white
            }
        }

        private (Prayer nextPrayer, DateTime nextTime) GetNextPrayerInfo(DateTime now)
        {
            if (_todayPrayerTimes == null || _tomorrowPrayerTimes == null) return (Prayer.NONE, DateTime.MinValue);

            // Using local times throughout
            if (now < _todayPrayerTimes.Fajr) return (Prayer.FAJR, _todayPrayerTimes.Fajr);
            if (now < _todayPrayerTimes.Dhuhr) return (Prayer.DHUHR, _todayPrayerTimes.Dhuhr);
            if (now < _todayPrayerTimes.Asr) return (Prayer.ASR, _todayPrayerTimes.Asr);
            if (now < _todayPrayerTimes.Maghrib) return (Prayer.MAGHRIB, _todayPrayerTimes.Maghrib);
            if (now < _todayPrayerTimes.Isha) return (Prayer.ISHA, _todayPrayerTimes.Isha);

            return (Prayer.FAJR, _tomorrowPrayerTimes.Fajr);
        }

        private void UpdateHeroSection(Prayer currentPrayer, string curName, string nextName, string countStr)
        {
            if (currentPrayer != Prayer.NONE)
            {
                NextPrayerNameText.Text = curName;
                HeroStatusText.Text = "ends in";
            }
            else
            {
                NextPrayerNameText.Text = nextName;
                HeroStatusText.Text = "starts in";
            }
            CountdownText.Text = countStr;
            
            // Show Jamaat time for the displayed prayer
            DateTime? jamaatTime = GetJamaatTime(currentPrayer != Prayer.NONE ? currentPrayer : Prayer.FAJR, SettingsManager.Current, DateTime.Now);
            if (jamaatTime.HasValue)
            {
                HeroJamaatText.Text = "Jamaat: " + jamaatTime.Value.ToString(TimeFmtFull);
                HeroJamaatText.Visibility = Visibility.Visible;
            }
            else
            {
                HeroJamaatText.Visibility = Visibility.Collapsed;
            }

            // Show Context Notes (Makruh, Duha, Tahajjud)
            HeroContextNoteText.Visibility = Visibility.Collapsed;
            TahajjudHeroDisplay.Visibility = Visibility.Collapsed;
            NafalNoticeHero.Visibility = Visibility.Collapsed;
            
            if (_todayPrayerTimes != null && _tomorrowPrayerTimes != null)
            {
                DateTime now = DateTime.Now;
                
                // Makruh or Tahajjud (During Isha)
                if (curName.Equals("Isha", StringComparison.OrdinalIgnoreCase) || currentPrayer == Prayer.ISHA)
                {
                    // Correcting Night duration for crossover
                    DateTime nightStart = now.Hour < 12 ? _todayPrayerTimes.Maghrib.AddDays(-1) : _todayPrayerTimes.Maghrib;
                    DateTime nightEnd = now.Hour < 12 ? _todayPrayerTimes.Fajr : _tomorrowPrayerTimes.Fajr;
                    
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
                        HeroContextNoteText.Text = $"⚠️ Makruh begins at {islamicMidnight.ToString(GetTimeFmt())}";
                        HeroContextNoteText.Visibility = Visibility.Visible;
                    }
                    else if (now >= lastThirdStart && now < tahajjudWindowEnd)
                    {
                        // Dedicated Tahajjud Timer
                        TahajjudHeroDisplay.Visibility = Visibility.Visible;
                        TimeSpan rem = tahajjudWindowEnd - now;
                        TahajjudHeroTimerText.Text = string.Format(CountdownFmt, rem.Hours, rem.Minutes, rem.Seconds);
                    }
                    else
                    {
                        HeroContextNoteText.Text = "✨ Tahajjud time is currently active";
                        HeroContextNoteText.Foreground = new SolidColorBrush(WColor.FromRgb(96, 165, 250)); // blue-400
                        HeroContextNoteText.Visibility = Visibility.Visible;
                    }
                }
                // Salat al-Duha (Notice Card)
                else if (now > _todayPrayerTimes.Sunrise.AddMinutes(15) && now < _todayPrayerTimes.Dhuhr.AddMinutes(-5))
                {
                    NafalNoticeText.Text = $"Salat al-Duha is active until {_todayPrayerTimes.Dhuhr.AddMinutes(-15).ToString(GetTimeFmt())}";
                    NafalNoticeHero.Visibility = Visibility.Visible;
                }
                // Salat al-Awwabin (Notice Card)
                else if (now > _todayPrayerTimes.Maghrib.AddMinutes(15) && now < _todayPrayerTimes.Isha.AddMinutes(-15))
                {
                    NafalNoticeText.Text = "Salat al-Awwabin time is currently active";
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
                        Label = currentPrayer != Prayer.NONE ? $"{curName} ends in:" : $"{nextName} starts in:",
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

            // 3. Update Overlay Window (UI)
            if (_overlay == null) return;

            _overlay.OverlayNameText.Text = currentPrayer != Prayer.NONE ? $"{curName} ends in:" : $"{nextName} starts in:";
            _overlay.OverlayCountdownText.Text = countStr;
            
            _overlay.ToolTipCurrentText.Text = $"Current: {curName} ({rangeStr})";
            _overlay.ToolTipNextText.Text = $"Next: {nextName} starts at {nextTime:hh:mm tt}";
            _overlay.ForceTopmost();
        }

        private void UpdateProgressBar(Prayer currentPrayer, DateTime nextTime, DateTime now)
        {
            if (_todayPrayerTimes != null && currentPrayer != Prayer.NONE)
            {
                DateTime currentPrayerTime = _todayPrayerTimes.TimeForPrayer(currentPrayer);
                double totalMs = (nextTime - currentPrayerTime).TotalMilliseconds;
                double elapsedMs = (now - currentPrayerTime).TotalMilliseconds;
                PrayerProgress.Value = Math.Min(100, Math.Max(0, (elapsedMs / totalMs) * 100));
            }
            else
            {
                PrayerProgress.Value = 0;
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

            UpdateProhibCard(SunriseProhibCard, SunriseProhibTimer, now, sunrStart, sunrEnd);
            UpdateProhibCard(ZawalProhibCard, ZawalProhibTimer, now, zawlStart, zawlEnd);
            UpdateProhibCard(SunsetProhibCard, SunsetProhibTimer, now, sunsStart, sunsEnd);

            // Handle Notifications
            if (s.NotificationsEnabled)
            {
                CheckProhibNotify("Sunrise Prohibited", now, sunrStart, sunrEnd, ref _sunriseProhibActive);
                CheckProhibNotify("Zawal Prohibited", now, zawlStart, zawlEnd, ref _zawalProhibActive);
                CheckProhibNotify("Sunset Prohibited", now, sunsStart, sunsEnd, ref _sunsetProhibActive);
            }

            bool isProhib = (now >= sunrStart && now <= sunrEnd) ||
                           (now >= zawlStart && now <= zawlEnd) ||
                           (now >= sunsStart && now <= sunsEnd);

            ProhibitedWarning.Visibility = isProhib ? Visibility.Visible : Visibility.Collapsed;
        }

        private static void CheckProhibNotify(string name, DateTime now, DateTime start, DateTime end, ref bool isActive)
        {
            if (now >= start && now <= end)
            {
                if (!isActive)
                {
                    ShowNotification($"{name} Started", $"Prayer is prohibited until {end.ToString(GetTimeFmt())}.");
                    isActive = true;
                }
            }
            else if (now > end && isActive)
            {
                ShowNotification($"{name} Ended", $"Prohibited time has passed. You can pray now.");
                isActive = false;
            }
        }

        private void UpdateFastingHighlights()
        {
            if (_todayPrayerTimes == null) return;

            int hijriDay = _todayPrayerTimes.HijriDay;
            int hijriMonth = _todayPrayerTimes.HijriMonth;

            // Fallback if structured data is 0 (offline or API failed)
            if (hijriDay == 0 || hijriMonth == 0)
            {
                try {
                    var calendar = new System.Globalization.UmAlQuraCalendar();
                    var now = DateTime.Now;
                    hijriDay = calendar.GetDayOfMonth(now);
                    hijriMonth = calendar.GetMonth(now);
                } catch { /* Silently fail and hide */ }
            }

            var highlights = new System.Collections.Generic.List<string>();

            // 1. Prohibited Days (Check these first as they override Sunnah recommendations)
            bool isProhibited = false;
            if (hijriMonth == 10 && hijriDay == 1) 
            {
                highlights.Add("Prohibited: Eid-ul-Fitr (First of Shawwal) - Forbidden to fast today.");
                isProhibited = true;
            }
            else if (hijriMonth == 12 && hijriDay == 10)
            {
                highlights.Add("Prohibited: Eid-ul-Adha (10th of Dhul-Hijjah) - Forbidden to fast today.");
                isProhibited = true;
            }
            else if (hijriMonth == 12 && (hijriDay == 11 || hijriDay == 12 || hijriDay == 13))
            {
                highlights.Add("Prohibited: Day of Tashreeq (Following Eid-ul-Adha) - Forbidden to fast today.");
                isProhibited = true;
            }

            if (isProhibited)
            {
                FastingNoteText.Text = string.Join("\n\n", highlights);
                FastingNoteBorder.Visibility = Visibility.Visible;
                return;
            }

            // 2. Weekly Sunnah Fasts
            var dayOfWeek = DateTime.Now.DayOfWeek;
            if (dayOfWeek == DayOfWeek.Monday) highlights.Add("Monday Fast: The day the Prophet ﷺ was born and received revelation.");
            else if (dayOfWeek == DayOfWeek.Thursday) highlights.Add("Thursday Fast: The day deeds are presented to Allah.");

            // 3. Monthly Sunnah Fasts (Ayyam al-Bidh) - 13, 14, 15
            if (hijriDay == 13 || hijriDay == 14 || hijriDay == 15)
            {
                 highlights.Insert(0, "Ayyam al-Bidh: The monthly Sunnah 'White Days' (13th, 14th, & 15th).");
            }

            // 4. Annual Special Fasts
            if (hijriMonth == 10 && hijriDay > 1 && hijriDay <= 30) highlights.Add("6 Days of Shawwal: Sunnah to fast any six days in the month following Ramadan.");
            if (hijriMonth == 12 && hijriDay == 9) highlights.Add("Day of Arafah: Highly recommended fast for those not performing Hajj.");
            if (hijriMonth == 1 && hijriDay == 10) highlights.Add("Day of Ashura: Highly recommended fast. (Note: Recommend adding 9th or 11th).");
            if (hijriMonth == 12 && hijriDay >= 1 && hijriDay <= 9) highlights.Add("Virtuous Days: Sunnah to fast during the first nine days of Dhul-Hijjah.");
            if (hijriMonth == 8) highlights.Add("Month of Sha'ban: Increasing voluntary fasts is recommended this month.");

            if (highlights.Count > 0)
            {
                FastingNoteText.Text = string.Join("\n\n", highlights);
                FastingNoteBorder.Visibility = Visibility.Visible;
            }
            else
            {
                FastingNoteBorder.Visibility = Visibility.Collapsed;
            }
        }

        private static string FormatPrayerName(Prayer p)
        {
            if (p == Prayer.SUNRISE) return "Sunrise";
            return char.ToUpper(p.ToString()[0]) + p.ToString().Substring(1).ToLower();
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
    }
}
