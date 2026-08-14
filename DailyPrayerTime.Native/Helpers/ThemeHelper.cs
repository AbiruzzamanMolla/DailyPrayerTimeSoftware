using System;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace DailyPrayerTime.Native.Helpers
{
    public static class ThemeHelper
    {
        private static bool _listenerInitialized = false;

        public static void InitializeSystemThemeListener()
        {
            if (_listenerInitialized) return;
            _listenerInitialized = true;

            try
            {
                SystemEvents.UserPreferenceChanged += (s, e) =>
                {
                    if (e.Category == UserPreferenceCategory.General || e.Category == UserPreferenceCategory.Color)
                    {
                        if (SettingsManager.Current.ThemePreset == "system")
                        {
                            Application.Current?.Dispatcher?.Invoke(() =>
                            {
                                ApplyTheme();
                            });
                        }
                    }
                };
            }
            catch { }
        }

        public static bool IsWindowsDarkTheme()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                var val = key?.GetValue("AppsUseLightTheme");
                if (val is int intVal)
                {
                    return intVal == 0;
                }
            }
            catch { }
            return true;
        }

        public static Color GetWindowsAccentColor()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\DWM");
                var val = key?.GetValue("AccentColor");
                if (val is int intVal)
                {
                    byte r = (byte)(intVal & 0xFF);
                    byte g = (byte)((intVal >> 8) & 0xFF);
                    byte b = (byte)((intVal >> 16) & 0xFF);
                    return Color.FromArgb(255, r, g, b);
                }
            }
            catch { }
            return SystemParameters.WindowGlassColor;
        }

        public static void ApplyTheme()
        {
            InitializeSystemThemeListener();

            var s = SettingsManager.Current;
            var resources = Application.Current.Resources;

            try
            {
                Color primaryColor;
                Color secondaryColor;
                Color gradientStart;
                Color gradientEnd;
                Color bgColor = ParseColor(s.BgColor, Colors.White);

                if (s.ThemePreset == "system")
                {
                    bool isDark = IsWindowsDarkTheme();
                    if (isDark)
                    {
                        gradientStart = Color.FromRgb(18, 18, 18); // #121212
                        gradientEnd = Color.FromRgb(0, 0, 0);      // #000000
                        primaryColor = Color.FromRgb(255, 255, 255); // #FFFFFF
                        secondaryColor = Color.FromRgb(212, 212, 212); // #D4D4D4
                    }
                    else
                    {
                        gradientStart = Color.FromRgb(255, 255, 255); // #FFFFFF
                        gradientEnd = Color.FromRgb(241, 245, 249);   // #F1F5F9
                        primaryColor = Color.FromRgb(0, 0, 0);        // #000000
                        secondaryColor = Color.FromRgb(51, 65, 85);   // #334155
                    }
                }
                else
                {
                    primaryColor = ParseColor(s.PrimaryColor, Color.FromRgb(16, 185, 129));
                    secondaryColor = ParseColor(s.SecondaryColor, Color.FromRgb(52, 211, 153));
                    gradientStart = ParseColor(s.GradientStart, Color.FromRgb(6, 78, 59));
                    gradientEnd = ParseColor(s.GradientEnd, Color.FromRgb(2, 44, 34));
                }

                // Register colors
                resources["ThemePrimaryColor"] = primaryColor;
                resources["ThemeSecondaryColor"] = secondaryColor;
                resources["ThemeGradientStart"] = gradientStart;
                resources["ThemeGradientEnd"] = gradientEnd;
                resources["ThemeBgColor"] = bgColor;

                // Register standard brushes
                resources["ThemePrimaryBrush"] = new SolidColorBrush(primaryColor);
                resources["ThemeSecondaryBrush"] = new SolidColorBrush(secondaryColor);

                bool isLight = (gradientStart.R * 0.299 + gradientStart.G * 0.587 + gradientStart.B * 0.114) > 150;

                // Translucent brushes
                Color primaryTrans = primaryColor;
                primaryTrans.A = isLight ? (byte)24 : (byte)40;
                resources["ThemePrimaryTranslucentBrush"] = new SolidColorBrush(primaryTrans);

                Color secondaryTrans = secondaryColor;
                secondaryTrans.A = isLight ? (byte)30 : (byte)48;
                resources["ThemeSecondaryTranslucentBrush"] = new SolidColorBrush(secondaryTrans);

                Color secondaryTransHigh = secondaryColor;
                secondaryTransHigh.A = isLight ? (byte)16 : (byte)24;
                resources["ThemeSecondaryTranslucentHighBrush"] = new SolidColorBrush(secondaryTransHigh);

                // Additional Glassmorphic Brushes
                if (isLight)
                {
                    resources["ThemeTextBrush"] = new SolidColorBrush(Color.FromRgb(15, 23, 42)); // Slate-900 (Black text)
                    resources["ThemeTextMutedBrush"] = new SolidColorBrush(Color.FromRgb(71, 85, 105)); // Slate-600
                    resources["ThemeAccentBrush"] = new SolidColorBrush(Color.FromRgb(0, 0, 0));
                    resources["ThemeWarningBrush"] = new SolidColorBrush(Color.FromRgb(217, 119, 6)); // Amber-600
                    resources["ThemeProhibitedBgBrush"] = new SolidColorBrush(Color.FromArgb(60, 220, 38, 38));
                    
                    resources["ThemeCardBgBrush"] = new SolidColorBrush(Color.FromArgb(18, 0, 0, 0)); // Translucent dark card on white
                    resources["ThemeCardBorderBrush"] = new SolidColorBrush(Color.FromArgb(35, 0, 0, 0));

                    resources["ThemeTitleBarBrush"] = new SolidColorBrush(Color.FromRgb(241, 245, 249)); // #F1F5F9 Slate-100
                    resources["ThemeButtonBgBrush"] = new SolidColorBrush(Color.FromRgb(15, 23, 42));
                    resources["ThemeButtonTextBrush"] = Brushes.White;
                    resources["ThemeButtonHoverBgBrush"] = new SolidColorBrush(Color.FromRgb(51, 65, 85));
                }
                else
                {
                    resources["ThemeTextBrush"] = Brushes.White;
                    Color textMuted = Color.FromRgb(167, 243, 208);
                    resources["ThemeTextMutedBrush"] = new SolidColorBrush(textMuted);
                    resources["ThemeAccentBrush"] = new SolidColorBrush(secondaryColor);
                    resources["ThemeWarningBrush"] = new SolidColorBrush(Color.FromRgb(251, 191, 36));
                    resources["ThemeProhibitedBgBrush"] = new SolidColorBrush(Color.FromArgb(77, 255, 0, 0));
                    
                    resources["ThemeCardBgBrush"] = new SolidColorBrush(Color.FromArgb(26, 255, 255, 255));
                    resources["ThemeCardBorderBrush"] = new SolidColorBrush(Color.FromArgb(42, 255, 255, 255));

                    Color titlebarBg = gradientEnd;
                    titlebarBg.R = (byte)(titlebarBg.R * 0.4);
                    titlebarBg.G = (byte)(titlebarBg.G * 0.4);
                    titlebarBg.B = (byte)(titlebarBg.B * 0.4);
                    resources["ThemeTitleBarBrush"] = new SolidColorBrush(titlebarBg);

                    resources["ThemeButtonBgBrush"] = new SolidColorBrush(secondaryColor);
                    resources["ThemeButtonTextBrush"] = new SolidColorBrush(gradientEnd);
                    resources["ThemeButtonHoverBgBrush"] = new SolidColorBrush(primaryColor);
                }

                // Gradient brush calculations
                double angleRad = s.GradientAngle * Math.PI / 180.0;
                double startX = 0.5 - Math.Cos(angleRad) * 0.5;
                double startY = 0.5 - Math.Sin(angleRad) * 0.5;
                double endX = 0.5 + Math.Cos(angleRad) * 0.5;
                double endY = 0.5 + Math.Sin(angleRad) * 0.5;

                var startPoint = new Point(startX, startY);
                var endPoint = new Point(endX, endY);

                Color popupGradStart = gradientStart;
                popupGradStart.A = 248; // High opacity to ensure white text readability
                Color popupGradEnd = gradientEnd;
                popupGradEnd.A = 248;

                var popupGradientBrush = new LinearGradientBrush(popupGradStart, popupGradEnd, startPoint, endPoint);
                resources["ThemePopupBgBrush"] = popupGradientBrush;

                Color glassGradStart = gradientStart;
                glassGradStart.A = 224; // 88% opacity
                Color glassGradEnd = gradientEnd;
                glassGradEnd.A = 224;

                var gradientBrush = new LinearGradientBrush(glassGradStart, glassGradEnd, startPoint, endPoint);
                resources["ThemeGradientBrush"] = gradientBrush;

                // For backward compatibility with elements bound to MainGradient
                if (resources.Contains("MainGradient") && resources["MainGradient"] is LinearGradientBrush mainGradient)
                {
                    mainGradient.GradientStops[0].Color = glassGradStart;
                    mainGradient.GradientStops[1].Color = glassGradEnd;
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
                    Color solidBg = bgColor;
                    solidBg.A = 224; // 88% opacity
                    resources["ThemeBgBrush"] = new SolidColorBrush(solidBg);
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

        public static Color LightenColor(Color color, float factor)
        {
            float r = (255 - color.R) * factor + color.R;
            float g = (255 - color.G) * factor + color.G;
            float b = (255 - color.B) * factor + color.B;
            return Color.FromRgb((byte)Math.Clamp(r, 0, 255), (byte)Math.Clamp(g, 0, 255), (byte)Math.Clamp(b, 0, 255));
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
