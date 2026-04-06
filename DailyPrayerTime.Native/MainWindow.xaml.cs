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

namespace DailyPrayerTime.Native
{
    public partial class MainWindow : Window
    {
        private const string TimeFmtFull = "hh:mm tt";
        private const string TimeFmtShort = "hh:mm";
        private const string CountdownFmt = "{0:D2}:{1:D2}:{2:D2}";

        private DispatcherTimer? _timer;
        private PrayerTimes? _todayPrayerTimes;
        private PrayerTimes? _tomorrowPrayerTimes;
        private OverlayWindow? _overlay;
        private Prayer _lastPrayer = Prayer.NONE;
        private Prayer _lastJamaatPopupPrayer = Prayer.NONE;
        private Forms.NotifyIcon? _notifyIcon;
        private CongregationTimerPopup? _activeJamaatPopup;

        public MainWindow()
        {
            SettingsManager.Load();
            InitializeComponent();
            ApplySettingsTheme();
            SetupTimer();
            SetupTrayIcon();
            CalculatePrayerTimes();
            ManageOverlay();
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
            cms.Items.Add("Settings", null, (s, e) => { 
                 var sw = new SettingsWindow(_todayPrayerTimes, _tomorrowPrayerTimes);
                 if (sw.ShowDialog() == true) {
                     ApplySettingsTheme();
                     CalculatePrayerTimes();
                     ManageOverlay();
                 }
            });
            var overlayItem = new Forms.ToolStripMenuItem("Show Overlay");
            overlayItem.CheckOnClick = true;
            overlayItem.Checked = SettingsManager.Current.ShowOverlay;
            overlayItem.Click += (s, e) => {
                SettingsManager.Current.ShowOverlay = overlayItem.Checked;
                SettingsManager.Save();
                ManageOverlay();
            };
            cms.Items.Add(overlayItem);
            cms.Items.Add("-");
            cms.Items.Add("Exit", null, (s, e) => System.Windows.Application.Current.Shutdown());
            _notifyIcon.ContextMenuStrip = cms;
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
                CalculatePrayerTimes();
                ManageOverlay();
            }
        }
        
        private void ManageOverlay()
        {
            if (SettingsManager.Current.ShowOverlay && _overlay == null)
            {
                _overlay = new OverlayWindow();
                _overlay.Show();
            }
            else if (!SettingsManager.Current.ShowOverlay && _overlay != null)
            {
                _overlay.Close();
                _overlay = null;
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

        private void CalculatePrayerTimes()
        {
            var s = SettingsManager.Current;
            var coordinates = new Coordinates(s.Latitude, s.Longitude);
            
            CalculationParameters parameters = CalculationMethod.KARACHI.GetParameters();
            try {
                if (s.Method == "MUSLIMWORLDLEAGUE") parameters = CalculationMethod.MUSLIM_WORLD_LEAGUE.GetParameters();
                else if (s.Method == "NORTHAMERICA") parameters = CalculationMethod.NORTH_AMERICA.GetParameters();
                else if (s.Method == "UMMALQURA") parameters = CalculationMethod.UMM_AL_QURA.GetParameters();
                else if (s.Method == "EGYPTIAN") parameters = CalculationMethod.EGYPTIAN.GetParameters();
            } catch (Exception) { /* Fallback to Karachi if format is invalid */ }

            parameters.Madhab = s.School == 1 ? Madhab.HANAFI : Madhab.SHAFI;

            var todayDate = DateTime.Now;
            var tomorrowDate = DateTime.Now.AddDays(1);
            
            var today = new Batoulapps.Adhan.Internal.DateComponents(todayDate.Year, todayDate.Month, todayDate.Day);
            var tomorrow = new Batoulapps.Adhan.Internal.DateComponents(tomorrowDate.Year, tomorrowDate.Month, tomorrowDate.Day);

            _todayPrayerTimes = new PrayerTimes(coordinates, today, parameters);
            _tomorrowPrayerTimes = new PrayerTimes(coordinates, tomorrow, parameters);
            
            HijriDateDisplay.Text = GetHijriDate();
            RefreshUIDisplay();
        }

        private string GetHijriDate()
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

            FajrTimeText.Text = $"{_todayPrayerTimes.Fajr.ToLocalTime().ToString(TimeFmtFull)} - {_todayPrayerTimes.Sunrise.ToLocalTime().ToString(TimeFmtFull)}";
            SunriseTimeText.Text = _todayPrayerTimes.Sunrise.ToLocalTime().ToString(TimeFmtFull);
            DhuhrTimeText.Text = $"{_todayPrayerTimes.Dhuhr.ToLocalTime().ToString(TimeFmtFull)} - {_todayPrayerTimes.Asr.ToLocalTime().ToString(TimeFmtFull)}";
            AsrTimeText.Text = $"{_todayPrayerTimes.Asr.ToLocalTime().ToString(TimeFmtFull)} - {_todayPrayerTimes.Maghrib.ToLocalTime().ToString(TimeFmtFull)}";
            MaghribTimeText.Text = $"{_todayPrayerTimes.Maghrib.ToLocalTime().ToString(TimeFmtFull)} - {_todayPrayerTimes.Isha.ToLocalTime().ToString(TimeFmtFull)}";
            IshaTimeText.Text = $"{_todayPrayerTimes.Isha.ToLocalTime().ToString(TimeFmtFull)} - {_tomorrowPrayerTimes.Fajr.ToLocalTime().ToString(TimeFmtFull)}";
            
            HeroSunriseText.Text = "☀ Sunrise: " + _todayPrayerTimes.Sunrise.ToLocalTime().ToString(TimeFmtFull);
            HeroSunsetText.Text = "🌆 Sunset: " + _todayPrayerTimes.Maghrib.ToLocalTime().ToString(TimeFmtFull);
            
            DateTime sunrise = _todayPrayerTimes.Sunrise.ToLocalTime();
            DateTime dhuhr = _todayPrayerTimes.Dhuhr.ToLocalTime();
            DateTime maghrib = _todayPrayerTimes.Maghrib.ToLocalTime();

            SunriseProhibRange.Text = $"{sunrise.ToString(TimeFmtShort)} - {sunrise.AddMinutes(15).ToString(TimeFmtShort)}";
            ZawalProhibRange.Text = $"{dhuhr.AddMinutes(-30).ToString(TimeFmtShort)} - {dhuhr.ToString(TimeFmtShort)}";
            SunsetProhibRange.Text = $"{maghrib.AddMinutes(-15).ToString(TimeFmtShort)} - {maghrib.ToString(TimeFmtShort)}";

            UpdateCountdown();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            UpdateCountdown();
            CheckProhibitedTimes();
            CheckJamaatAlarms();
        }

        private void CheckJamaatAlarms()
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

            DateTime startTime = _todayPrayerTimes!.TimeForPrayer(p)!.Value.ToLocalTime();
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

        private DateTime? GetJamaatTime(Prayer p, AppSettings s, DateTime now)
        {
            string timeStr = p switch
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

        private DateTime GetPrayerEndTime(Prayer p)
        {
            return p switch
            {
                Prayer.FAJR => _todayPrayerTimes!.Sunrise.ToLocalTime(),
                Prayer.DHUHR => _todayPrayerTimes!.Asr.ToLocalTime(),
                Prayer.ASR => _todayPrayerTimes!.Maghrib.ToLocalTime(),
                Prayer.MAGHRIB => _todayPrayerTimes!.Isha.ToLocalTime(),
                Prayer.ISHA => _tomorrowPrayerTimes!.Fajr.ToLocalTime(),
                _ => DateTime.MaxValue
            };
        }

        private void ShowJamaatPopup(string prayerName, DateTime jamaatTime)
        {
            // Close existing if open
            if (_activeJamaatPopup != null && _activeJamaatPopup.IsVisible)
            {
                _activeJamaatPopup.Close();
            }

            _activeJamaatPopup = new CongregationTimerPopup(prayerName, jamaatTime);
            _activeJamaatPopup.Show();
        }

        private void CheckProhibitedTimes()
        {
            if (_todayPrayerTimes == null) return;

            DateTime now = DateTime.Now;
            DateTime sunrise = _todayPrayerTimes.Sunrise.ToLocalTime();
            DateTime dhuhr = _todayPrayerTimes.Dhuhr.ToLocalTime();
            DateTime maghrib = _todayPrayerTimes.Maghrib.ToLocalTime();

            UpdateProhibCard(SunriseProhibCard, SunriseProhibTimer, now, sunrise, sunrise.AddMinutes(15));
            UpdateProhibCard(ZawalProhibCard, ZawalProhibTimer, now, dhuhr.AddMinutes(-30), dhuhr);
            UpdateProhibCard(SunsetProhibCard, SunsetProhibTimer, now, maghrib.AddMinutes(-15), maghrib);

            // Update main banner
            if (now >= sunrise && now <= sunrise.AddMinutes(15))
            {
                ProhibitedWarning.Visibility = Visibility.Visible;
                TimeSpan rem = sunrise.AddMinutes(15) - now;
                ProhibitedText.Text = $"⚠️ Prohibited: Sunrise Period ({rem.Minutes:D2}:{rem.Seconds:D2})";
            }
            else if (now >= dhuhr.AddMinutes(-30) && now <= dhuhr)
            {
                ProhibitedWarning.Visibility = Visibility.Visible;
                TimeSpan rem = dhuhr - now;
                ProhibitedText.Text = $"⚠️ Prohibited: Zawal Period ({rem.Minutes:D2}:{rem.Seconds:D2})";
            }
            else if (now >= maghrib.AddMinutes(-15) && now <= maghrib)
            {
                ProhibitedWarning.Visibility = Visibility.Visible;
                TimeSpan rem = maghrib - now;
                ProhibitedText.Text = $"⚠️ Prohibited: Sunset Period ({rem.Minutes:D2}:{rem.Seconds:D2})";
            }
            else
            {
                ProhibitedWarning.Visibility = Visibility.Collapsed;
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
            DateTime utcNow = DateTime.UtcNow;

            var nextResult = GetNextPrayerInfo(utcNow);
            Prayer nextPrayer = nextResult.nextPrayer;
            DateTime nextTime = nextResult.nextTime;

            TimeSpan remaining = nextTime - now;
            if (remaining.TotalSeconds < 0)
            {
                 CalculatePrayerTimes();
                 return;
            }

            string countStr = string.Format(CountdownFmt, remaining.Hours, remaining.Minutes, remaining.Seconds);
            var nextName = FormatPrayerName(nextPrayer);
            
            var currentPrayer = _todayPrayerTimes.CurrentPrayer(utcNow);
            Prayer curPrayer = currentPrayer == Prayer.NONE && now > _todayPrayerTimes.Isha.ToLocalTime() ? Prayer.ISHA : currentPrayer;
            string curName = FormatPrayerName(curPrayer);

            UpdateHeroSection(currentPrayer, curName, nextName, countStr);
            UpdateOverlay(currentPrayer, curName, nextName, countStr, nextTime);
            CheckNotifications(currentPrayer);
            UpdateProgressBar(currentPrayer, nextTime, now);
            UpdatePrayerListHighlighting(curPrayer);
        }

        private (Prayer nextPrayer, DateTime nextTime) GetNextPrayerInfo(DateTime utcNow)
        {
            var nextPrayer = _todayPrayerTimes!.NextPrayer(utcNow);
            if (nextPrayer == Prayer.NONE)
            {
                return (Prayer.FAJR, _tomorrowPrayerTimes!.Fajr.ToLocalTime());
            }
            return (nextPrayer, _todayPrayerTimes.TimeForPrayer(nextPrayer)!.Value.ToLocalTime());
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
        }

        private void UpdateOverlay(Prayer currentPrayer, string curName, string nextName, string countStr, DateTime nextTime)
        {
            if (_overlay == null) return;

            _overlay.OverlayNameText.Text = currentPrayer != Prayer.NONE ? $"{curName} ends in:" : $"{nextName} starts in:";
            _overlay.OverlayCountdownText.Text = countStr;
            
            DateTime? currentStart = _todayPrayerTimes?.TimeForPrayer(_todayPrayerTimes.CurrentPrayer(DateTime.UtcNow))?.ToLocalTime() 
                                    ?? _todayPrayerTimes?.Isha.ToLocalTime();
            
            string rangeStr = currentStart.HasValue ? $"{currentStart.Value:hh:mm tt} - {nextTime:hh:mm tt}" : "N/A";
            
            _overlay.ToolTipCurrentText.Text = $"Current: {curName} ({rangeStr})";
            _overlay.ToolTipNextText.Text = $"Next: {nextName} starts at {nextTime:hh:mm tt}";
            _overlay.ForceTopmost();
        }

        private void CheckNotifications(Prayer currentPrayer)
        {
            if (currentPrayer != _lastPrayer && currentPrayer != Prayer.NONE)
            {
                if (SettingsManager.Current.NotificationsEnabled)
                {
                    string pName = FormatPrayerName(currentPrayer);
                    string dateStr = DateTime.Now.ToString("dd MMM yyyy");
                    new ToastContentBuilder()
                        .AddText($"{pName} Time - {dateStr}")
                        .AddText($"It's time for {pName} prayer. {LocationDisplay.Text}")
                        .Show();
                }
                _lastPrayer = currentPrayer;
            }
        }

        private void UpdateProgressBar(Prayer currentPrayer, DateTime nextTime, DateTime now)
        {
            if (currentPrayer != Prayer.NONE)
            {
                DateTime currentPrayerTime = _todayPrayerTimes!.TimeForPrayer(currentPrayer)!.Value.ToLocalTime();
                double totalMs = (nextTime - currentPrayerTime).TotalMilliseconds;
                double elapsedMs = (now - currentPrayerTime).TotalMilliseconds;
                PrayerProgress.Value = Math.Min(100, Math.Max(0, (elapsedMs / totalMs) * 100));
            }
            else
            {
                PrayerProgress.Value = 0;
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
