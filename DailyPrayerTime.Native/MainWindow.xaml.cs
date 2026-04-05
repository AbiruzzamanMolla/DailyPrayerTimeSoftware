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
        private DispatcherTimer? _timer;
        private PrayerTimes? _todayPrayerTimes;
        private PrayerTimes? _tomorrowPrayerTimes;
        private OverlayWindow? _overlay;
        private Prayer _lastPrayer = Prayer.NONE;
        private Forms.NotifyIcon? _notifyIcon;

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
                 var sw = new SettingsWindow();
                 if (sw.ShowDialog() == true) {
                     ApplySettingsTheme();
                     CalculatePrayerTimes();
                     ManageOverlay();
                 }
            });
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
            var sw = new SettingsWindow();
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

            // Update UI list
            // Update UI list with Ranges
            string fmt = "hh:mm tt";
            FajrTimeText.Text = $"{_todayPrayerTimes.Fajr.ToLocalTime().ToString(fmt)} - {_todayPrayerTimes.Sunrise.ToLocalTime().ToString(fmt)}";
            SunriseTimeText.Text = _todayPrayerTimes.Sunrise.ToLocalTime().ToString(fmt);
            DhuhrTimeText.Text = $"{_todayPrayerTimes.Dhuhr.ToLocalTime().ToString(fmt)} - {_todayPrayerTimes.Asr.ToLocalTime().ToString(fmt)}";
            AsrTimeText.Text = $"{_todayPrayerTimes.Asr.ToLocalTime().ToString(fmt)} - {_todayPrayerTimes.Maghrib.ToLocalTime().ToString(fmt)}";
            MaghribTimeText.Text = $"{_todayPrayerTimes.Maghrib.ToLocalTime().ToString(fmt)} - {_todayPrayerTimes.Isha.ToLocalTime().ToString(fmt)}";
            IshaTimeText.Text = $"{_todayPrayerTimes.Isha.ToLocalTime().ToString(fmt)} - {_tomorrowPrayerTimes.Fajr.ToLocalTime().ToString(fmt)}";
            
            HeroSunriseText.Text = "☀ Sunrise: " + _todayPrayerTimes.Sunrise.ToLocalTime().ToString(fmt);
            HeroSunsetText.Text = "🌆 Sunset: " + _todayPrayerTimes.Maghrib.ToLocalTime().ToString(fmt);
            
            // Set Prohibited Ranges (Short format)
            DateTime sunrise = _todayPrayerTimes.Sunrise.ToLocalTime();
            DateTime dhuhr = _todayPrayerTimes.Dhuhr.ToLocalTime();
            DateTime maghrib = _todayPrayerTimes.Maghrib.ToLocalTime();

            SunriseProhibRange.Text = $"{sunrise.ToString("hh:mm")} - {sunrise.AddMinutes(15).ToString("hh:mm")}";
            ZawalProhibRange.Text = $"{dhuhr.AddMinutes(-30).ToString("hh:mm")} - {dhuhr.ToString("hh:mm")}";
            SunsetProhibRange.Text = $"{maghrib.AddMinutes(-15).ToString("hh:mm")} - {maghrib.ToString("hh:mm")}";

            UpdateCountdown();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            UpdateCountdown();
            CheckProhibitedTimes();
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

            var nextPrayer = _todayPrayerTimes.NextPrayer(utcNow);
            DateTime nextTime;
            
            if (nextPrayer == Prayer.NONE)
            {
                // After Isha, next prayer is Fajr tomorrow
                nextPrayer = Prayer.FAJR;
                nextTime = _tomorrowPrayerTimes.Fajr.ToLocalTime();
            }
            else
            {
                nextTime = _todayPrayerTimes.TimeForPrayer(nextPrayer)!.Value.ToLocalTime();
            }

            TimeSpan remaining = nextTime - now;
            // If remaining is negative, something went wrong, let's recalibrate (might happen at midnight)
            if (remaining.TotalSeconds < 0)
            {
                 CalculatePrayerTimes();
                 return;
            }

            var nextName = FormatPrayerName(nextPrayer);
            string countStr = string.Format("{0:D2}:{1:D2}:{2:D2}", remaining.Hours, remaining.Minutes, remaining.Seconds);
            
            // Current Prayer Detection
            var currentPrayer = _todayPrayerTimes.CurrentPrayer(utcNow);
            Prayer curPrayer = currentPrayer;
            
            // Special handling for the night period (After Isha, before Fajr)
            if (curPrayer == Prayer.NONE || (nextPrayer == Prayer.FAJR && now > _todayPrayerTimes.Isha.ToLocalTime())) 
            {
                curPrayer = Prayer.ISHA;
            }
            
            string curName = FormatPrayerName(curPrayer);

            NextPrayerNameText.Text = nextName;
            CountdownText.Text = countStr;
            
            if (_overlay != null)
            {
                _overlay.OverlayNameText.Text = $"{curName} ends in:";
                _overlay.OverlayCountdownText.Text = countStr;
                
                DateTime? currentStart = _todayPrayerTimes.TimeForPrayer(curPrayer)?.ToLocalTime() ?? _todayPrayerTimes.Isha.ToLocalTime();
                string rangeStr = $"{currentStart.Value:hh:mm tt} - {nextTime:hh:mm tt}";
                
                _overlay.ToolTipCurrentText.Text = $"Current: {curName} ({rangeStr})";
                _overlay.ToolTipNextText.Text = $"Next: {nextName} starts at {nextTime:hh:mm tt}";
                _overlay.ForceTopmost();
            }

            // Notification Logic
            if (currentPrayer != _lastPrayer && currentPrayer != Prayer.NONE)
            {
                if (SettingsManager.Current.NotificationsEnabled)
                {
                    string pName = FormatPrayerName(currentPrayer);
                    new ToastContentBuilder()
                        .AddText($"{pName} Time")
                        .AddText($"It's time for {pName} prayer. {LocationDisplay.Text}")
                        .Show();
                }
                _lastPrayer = currentPrayer;
            }

            if (currentPrayer != Prayer.NONE)
            {
                DateTime currentPrayerTime = _todayPrayerTimes.TimeForPrayer(currentPrayer)!.Value.ToLocalTime();
                double totalMs = (nextTime - currentPrayerTime).TotalMilliseconds;
                double elapsedMs = (now - currentPrayerTime).TotalMilliseconds;
                PrayerProgress.Value = Math.Min(100, Math.Max(0, (elapsedMs / totalMs) * 100));
            }
            else
            {
                PrayerProgress.Value = 0;
            }

            // Highlight active prayer card in UI
            ResetHighlighting(this);
            var activeBg = new SolidColorBrush(WColor.FromArgb(40, 255, 255, 255)); // 15% white
            var activeNameBrush = new SolidColorBrush(WColor.FromRgb(52, 211, 153)); // Secondary green (#34d399)
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

        private static string FormatPrayerName(Prayer p)
        {
            if (p == Prayer.SUNRISE) return "Sunrise";
            return char.ToUpper(p.ToString()[0]) + p.ToString().Substring(1).ToLower();
        }

        private static void ResetHighlighting(MainWindow window)
        {
            var transparentBg = new SolidColorBrush(WColor.FromArgb(26, 255, 255, 255)); // 10% white
            var whiteBrush = new SolidColorBrush(Colors.White);
            var greenBrush = new SolidColorBrush(WColor.FromRgb(52, 211, 153)); // #34d399
            
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
