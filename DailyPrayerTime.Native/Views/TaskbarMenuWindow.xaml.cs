using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Batoulapps.Adhan;
using DailyPrayerTime.Native.Models;
using DailyPrayerTime.Native.Services;

namespace DailyPrayerTime.Native.Views
{
    public partial class TaskbarMenuWindow : Window
    {
        private CombinedPrayerTimes _todayTimes;
        private CombinedPrayerTimes _tomorrowTimes;
        private MainWindow _mainWindow;
        private DispatcherTimer _timer;

        public bool IsClosing { get; private set; }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            IsClosing = true;
            base.OnClosing(e);
        }

        public TaskbarMenuWindow(CombinedPrayerTimes todayTimes, CombinedPrayerTimes tomorrowTimes, MainWindow mainWindow)
        {
            InitializeComponent();
            _todayTimes = todayTimes;
            _tomorrowTimes = tomorrowTimes;
            _mainWindow = mainWindow;

            LoadLocalizedStrings();
            UpdateData();

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += (s, e) => UpdateCountdown();
            _timer.Start();

            PositionWindow();
        }

        private void LoadLocalizedStrings()
        {
            try
            {
                // Localization strings
                var dt = DateTime.Now;
                string dayName = LocalizationManager.Instance.GetString($"Day_{(int)dt.DayOfWeek}");
                string monthName = LocalizationManager.Instance.GetString($"Month_Gregorian_{dt.Month}");
                DateText.Text = $"{dayName}, {dt.Day} {monthName}, {dt.Year}".ToUpper();
                
                if (_todayTimes != null)
                {
                    HijriDateText.Text = MainWindow.GetHijriDate(_todayTimes.HijriDay, _todayTimes.HijriMonth, _todayTimes.HijriYear, _todayTimes.HijriWeekday).ToUpper();
                }
                else
                {
                    HijriDateText.Text = "";
                }

                // Location & method info
                var s = SettingsManager.Current;
                InfoMethodText.Text = LocalizationManager.Instance.GetString("Method_" + s.Method.ToString()) ?? s.Method.ToString();
                InfoCoordsText.Text = $"{s.Latitude:F4}, {s.Longitude:F4}";
                InfoLocationText.Text = s.LocationName;
            }
            catch (Exception)
            {
                DateText.Text = DateTime.Now.ToLongDateString().ToUpper();
            }
        }

        private void PositionWindow()
        {
            try
            {
                var mousePos = System.Windows.Forms.Control.MousePosition;
                double sx = 1.0, sy = 1.0;
                var src = PresentationSource.FromVisual(this);
                if (src?.CompositionTarget != null)
                {
                    sx = src.CompositionTarget.TransformToDevice.M11;
                    sy = src.CompositionTarget.TransformToDevice.M22;
                }

                double logicalMouseX = mousePos.X / sx;
                double logicalMouseY = mousePos.Y / sy;

                IntPtr trayHandle = NativeMethods.FindWindow("Shell_TrayWnd", null);
                if (trayHandle != IntPtr.Zero)
                {
                    NativeMethods.GetWindowRect(trayHandle, out NativeMethods.RECT tr);
                    double trayTop = tr.Top / sy;
                    double trayBottom = tr.Bottom / sy;

                    // If taskbar is at bottom
                    if (tr.Height < tr.Width && tr.Top > 0)
                    {
                        this.Left = logicalMouseX - (this.Width / 2);
                        this.Top = trayTop - this.Height - 6;
                    }
                    // If taskbar is at top
                    else if (tr.Height < tr.Width && tr.Top == 0)
                    {
                        this.Left = logicalMouseX - (this.Width / 2);
                        this.Top = trayBottom + 6;
                    }
                    else
                    {
                        this.Left = logicalMouseX - (this.Width / 2);
                        this.Top = logicalMouseY - (this.Height / 2);
                    }
                }
                else
                {
                    this.Left = logicalMouseX - (this.Width / 2);
                    this.Top = logicalMouseY - this.Height - 10;
                }

                // Clamp to screen bounds
                double screenWidth = SystemParameters.WorkArea.Width;
                double screenHeight = SystemParameters.WorkArea.Height;
                if (this.Left < 6) this.Left = 6;
                if (this.Left + this.Width > screenWidth) this.Left = screenWidth - this.Width - 6;
                if (this.Top < 6) this.Top = 6;
                if (this.Top + this.Height > screenHeight) this.Top = screenHeight - this.Height - 6;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TaskbarMenuWindow] Position failed: {ex.Message}");
            }
        }

        private string GetTimeFmt() => SettingsManager.Current.TimeFormat == "24h" ? "HH:mm" : "hh:mm tt";

        private void UpdateData()
        {
            if (_todayTimes == null || _tomorrowTimes == null) return;
            string timeFmt = GetTimeFmt();

            Time_Fajr.Text = $"{_todayTimes.Fajr.ToString(timeFmt)} - {_todayTimes.Sunrise.ToString(timeFmt)}";
            Time_Dhuhr.Text = $"{_todayTimes.Dhuhr.ToString(timeFmt)} - {_todayTimes.Asr.ToString(timeFmt)}";
            Time_Asr.Text = $"{_todayTimes.Asr.ToString(timeFmt)} - {_todayTimes.Maghrib.ToString(timeFmt)}";
            Time_Maghrib.Text = $"{_todayTimes.Maghrib.ToString(timeFmt)} - {_todayTimes.Isha.ToString(timeFmt)}";
            Time_Isha.Text = $"{_todayTimes.Isha.ToString(timeFmt)} - {_tomorrowTimes.Fajr.ToString(timeFmt)}";

            UpdateCountdown();
        }

        private void UpdateCountdown()
        {
            if (_todayTimes == null) return;

            DateTime now = DateTime.Now;
            var currentPrayer = _todayTimes.CurrentPrayer(now);
            var nextResult = GetNextPrayerInfo(now);

            string activePrayerName = "";
            string countdownText = "";
            string activePrayerTimeStr = "";

            var lm = LocalizationManager.Instance;

            if (currentPrayer != Prayer.NONE && currentPrayer != Prayer.SUNRISE)
            {
                activePrayerName = FormatPrayerName(currentPrayer);
                activePrayerTimeStr = _todayTimes.TimeForPrayer(currentPrayer).ToString(GetTimeFmt());
                
                DateTime currentEndTime = nextResult.nextTime;
                if (currentEndTime != DateTime.MinValue)
                {
                    TimeSpan remaining = currentEndTime - now;
                    string timeStr = $"{remaining.Hours}{lm.GetString("Unit_Hour_Short")} {remaining.Minutes}{lm.GetString("Unit_Min_Short")} {remaining.Seconds}{lm.GetString("Unit_Sec_Short")}";
                    string endsInFormat = lm.GetString("Label_EndsIn") ?? "{0} ends in:";
                    string endsInPrefix = string.Format(endsInFormat, "").Replace(":", "").Trim();
                    countdownText = $"{endsInPrefix} {timeStr}";
                }
            }
            else
            {
                activePrayerName = FormatPrayerName(nextResult.nextPrayer);
                activePrayerTimeStr = nextResult.nextTime.ToString(GetTimeFmt());
                
                if (nextResult.nextTime != DateTime.MinValue)
                {
                    TimeSpan remaining = nextResult.nextTime - now;
                    string timeStr = $"{remaining.Hours}{lm.GetString("Unit_Hour_Short")} {remaining.Minutes}{lm.GetString("Unit_Min_Short")} {remaining.Seconds}{lm.GetString("Unit_Sec_Short")}";
                    string startsInFormat = lm.GetString("Label_StartsIn") ?? "{0} starts in:";
                    string startsInPrefix = string.Format(startsInFormat, "").Replace(":", "").Trim();
                    countdownText = $"{startsInPrefix} {timeStr}";
                }
            }

            ActivePrayerNameText.Text = activePrayerName;
            ActivePrayerCountdownText.Text = countdownText;
            ActivePrayerTimeText.Text = activePrayerTimeStr;

            HighlightCurrentPrayer(currentPrayer);
        }

        private void HighlightCurrentPrayer(Prayer currentPrayer)
        {
            // Reset all backgrounds and chips
            ResetHighlights();

            // Determine which border / chip to highlight
            Border? targetBorder = null;
            Border? targetChip = null;
            TextBlock? targetChipText = null;

            switch (currentPrayer)
            {
                case Prayer.FAJR:
                    targetBorder = Border_Fajr;
                    break;
                case Prayer.DHUHR:
                    targetBorder = Border_Dhuhr;
                    targetChip = Chip_Dhuhr;
                    targetChipText = ChipText_Dhuhr;
                    break;
                case Prayer.ASR:
                    targetBorder = Border_Asr;
                    targetChip = Chip_Asr;
                    targetChipText = ChipText_Asr;
                    break;
                case Prayer.MAGHRIB:
                    targetBorder = Border_Maghrib;
                    targetChip = Chip_Maghrib;
                    targetChipText = ChipText_Maghrib;
                    break;
                case Prayer.ISHA:
                    targetBorder = Border_Isha;
                    targetChip = Chip_Isha;
                    targetChipText = ChipText_Isha;
                    break;
            }

            if (targetBorder != null)
            {
                // Translucent theme green background highlight
                targetBorder.Background = new SolidColorBrush(Color.FromArgb(40, 52, 211, 153)); // 15% opacity of #34D399
                
                // Show relative countdown inside list row if possible
                var nextResult = GetNextPrayerInfo(DateTime.Now);
                if (targetChip != null && targetChipText != null && nextResult.nextTime != DateTime.MinValue)
                {
                    TimeSpan remaining = nextResult.nextTime - DateTime.Now;
                    var lm = LocalizationManager.Instance;
                    targetChipText.Text = $"{remaining.Hours}{lm.GetString("Unit_Hour_Short")} {remaining.Minutes}{lm.GetString("Unit_Min_Short")}";
                    targetChip.Visibility = Visibility.Visible;
                }
            }
        }

        private void ResetHighlights()
        {
            Brush transparent = Brushes.Transparent;
            
            Border_Fajr.Background = transparent;
            Border_Dhuhr.Background = transparent;
            Border_Asr.Background = transparent;
            Border_Maghrib.Background = transparent;
            Border_Isha.Background = transparent;

            Chip_Dhuhr.Visibility = Visibility.Collapsed;
            Chip_Asr.Visibility = Visibility.Collapsed;
            Chip_Maghrib.Visibility = Visibility.Collapsed;
            Chip_Isha.Visibility = Visibility.Collapsed;
        }

        private (Prayer nextPrayer, DateTime nextTime) GetNextPrayerInfo(DateTime now)
        {
            if (_todayTimes == null || _tomorrowTimes == null) return (Prayer.NONE, DateTime.MinValue);

            if (now < _todayTimes.Fajr) return (Prayer.FAJR, _todayTimes.Fajr);
            if (now < _todayTimes.Sunrise) return (Prayer.SUNRISE, _todayTimes.Sunrise);
            if (now < _todayTimes.Dhuhr) return (Prayer.DHUHR, _todayTimes.Dhuhr);
            if (now < _todayTimes.Asr) return (Prayer.ASR, _todayTimes.Asr);
            if (now < _todayTimes.Maghrib) return (Prayer.MAGHRIB, _todayTimes.Maghrib);
            if (now < _todayTimes.Isha) return (Prayer.ISHA, _todayTimes.Isha);

            return (Prayer.FAJR, _tomorrowTimes.Fajr);
        }

        private string FormatPrayerName(Prayer p)
        {
            return p switch
            {
                Prayer.FAJR => LocalizationManager.Instance.GetString("Prayer_Fajr"),
                Prayer.SUNRISE => LocalizationManager.Instance.GetString("Prayer_Sunrise"),
                Prayer.DHUHR => LocalizationManager.Instance.GetString("Prayer_Dhuhr"),
                Prayer.ASR => LocalizationManager.Instance.GetString("Prayer_Asr"),
                Prayer.MAGHRIB => LocalizationManager.Instance.GetString("Prayer_Maghrib"),
                Prayer.ISHA => LocalizationManager.Instance.GetString("Prayer_Isha"),
                _ => p.ToString()
            };
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            if (!IsClosing)
            {
                try
                {
                    this.Close();
                }
                catch { }
            }
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.Dispatcher.Invoke(() =>
            {
                _mainWindow.Show();
                _mainWindow.WindowState = WindowState.Normal;
                _mainWindow.Activate();
                _mainWindow.OpenSettings();
            });
            this.Close();
        }

        private void OpenHome_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.Dispatcher.Invoke(() =>
            {
                _mainWindow.Show();
                _mainWindow.WindowState = WindowState.Normal;
                _mainWindow.Activate();
            });
            this.Close();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        protected override void OnClosed(EventArgs e)
        {
            _timer.Stop();
            base.OnClosed(e);
        }
    }
}
