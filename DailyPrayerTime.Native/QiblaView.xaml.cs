using System;
using DailyPrayerTime.Native.Services;
using WControls = System.Windows.Controls;

namespace DailyPrayerTime.Native
{
    public partial class QiblaView : WControls.UserControl
    {
        public QiblaView()
        {
            InitializeComponent();
        }

        public void UpdateDirection()
        {
            try
            {
                var s = SettingsManager.Current;
                double lat = s.Latitude;
                double lon = s.Longitude;

                if (Math.Abs(lat) < 0.001 && Math.Abs(lon) < 0.001)
                {
                    BearingText.Text = "--°";
                    DirectionText.Text = LocalizationManager.Instance.GetString("Qibla_NoLocation");
                    LocationText.Text = "";
                    return;
                }

                double bearing = QiblaCalculator.CalculateDirection(lat, lon);
                string dir = GetCompassDirection(bearing);

                ArrowRotation.Angle = bearing;
                ShaftRotation.Angle = bearing;

                BearingText.Text = $"{bearing:F1}°";
                DirectionText.Text = string.Format(LocalizationManager.Instance.GetString("Qibla_Bearing"), dir);
                LocationText.Text = string.Format(LocalizationManager.Instance.GetString("Qibla_FromLocation"), s.LocationName);
            }
            catch
            {
                BearingText.Text = "--°";
                DirectionText.Text = LocalizationManager.Instance.GetString("Label_NA");
            }
        }

        private void RefreshBtn_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            UpdateDirection();
        }

        private static string GetCompassDirection(double bearing)
        {
            string[] dirs = { "N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE",
                              "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW" };
            int index = (int)Math.Round(bearing / 22.5) % 16;
            return dirs[index];
        }
    }
}
