using System;
using System.Windows;
using System.Windows.Media;

namespace DailyPrayerTime.Native.Helpers
{
    public static class ThemeHelper
    {
        public static void ApplyTheme()
        {
            var s = SettingsManager.Current;
            var resources = Application.Current.Resources;

            try
            {
                // Parse hex colors safely
                Color primaryColor = ParseColor(s.PrimaryColor, Color.FromRgb(16, 185, 129)); // Default #10b981
                Color secondaryColor = ParseColor(s.SecondaryColor, Color.FromRgb(52, 211, 153)); // Default #34d399
                Color gradientStart = ParseColor(s.GradientStart, Color.FromRgb(6, 78, 59)); // Default #064e3b
                Color gradientEnd = ParseColor(s.GradientEnd, Color.FromRgb(2, 44, 34)); // Default #022c22
                Color bgColor = ParseColor(s.BgColor, Colors.White);

                // Register colors
                resources["ThemePrimaryColor"] = primaryColor;
                resources["ThemeSecondaryColor"] = secondaryColor;
                resources["ThemeGradientStart"] = gradientStart;
                resources["ThemeGradientEnd"] = gradientEnd;
                resources["ThemeBgColor"] = bgColor;

                // Register standard brushes
                resources["ThemePrimaryBrush"] = new SolidColorBrush(primaryColor);
                resources["ThemeSecondaryBrush"] = new SolidColorBrush(secondaryColor);

                // Translucent brushes
                Color primaryTrans = primaryColor;
                primaryTrans.A = 32; // ~12.5% opacity (0x20)
                resources["ThemePrimaryTranslucentBrush"] = new SolidColorBrush(primaryTrans);

                Color secondaryTrans = secondaryColor;
                secondaryTrans.A = 48; // ~18.8% opacity (0x30)
                resources["ThemeSecondaryTranslucentBrush"] = new SolidColorBrush(secondaryTrans);

                Color secondaryTransHigh = secondaryColor;
                secondaryTransHigh.A = 24; // ~9.4% opacity (0x18)
                resources["ThemeSecondaryTranslucentHighBrush"] = new SolidColorBrush(secondaryTransHigh);

                // Gradient brush calculations
                double angleRad = s.GradientAngle * Math.PI / 180.0;
                double startX = 0.5 - Math.Cos(angleRad) * 0.5;
                double startY = 0.5 - Math.Sin(angleRad) * 0.5;
                double endX = 0.5 + Math.Cos(angleRad) * 0.5;
                double endY = 0.5 + Math.Sin(angleRad) * 0.5;

                var startPoint = new Point(startX, startY);
                var endPoint = new Point(endX, endY);

                var gradientBrush = new LinearGradientBrush(gradientStart, gradientEnd, startPoint, endPoint);
                resources["ThemeGradientBrush"] = gradientBrush;

                // For backward compatibility with elements bound to MainGradient
                if (resources.Contains("MainGradient") && resources["MainGradient"] is LinearGradientBrush mainGradient)
                {
                    mainGradient.GradientStops[0].Color = gradientStart;
                    mainGradient.GradientStops[1].Color = gradientEnd;
                    mainGradient.StartPoint = startPoint;
                    mainGradient.EndPoint = endPoint;
                }
                else
                {
                    resources["MainGradient"] = gradientBrush;
                }

                // Background brush according to settings
                if (s.BgType?.Equals("solid", StringComparison.OrdinalIgnoreCase) == true)
                {
                    resources["ThemeBgBrush"] = new SolidColorBrush(bgColor);
                }
                else
                {
                    resources["ThemeBgBrush"] = gradientBrush;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error applying theme colors: " + ex.Message);
            }
        }

        private static Color ParseColor(string hex, Color defaultColor)
        {
            if (string.IsNullOrWhiteSpace(hex)) return defaultColor;
            try
            {
                var val = ColorConverter.ConvertFromString(hex);
                if (val is Color c) return c;
            }
            catch
            {
                // Ignore invalid hex
            }
            return defaultColor;
        }
    }
}
