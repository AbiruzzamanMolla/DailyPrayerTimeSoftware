using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;

namespace DailyPrayerTime.Native
{
    public static class FontSizeHelper
    {
        private static readonly Dictionary<DependencyObject, double> _baseSizes = new Dictionary<DependencyObject, double>();
        private static double _currentScale = 1.0;

        public static double CurrentScale => _currentScale;

        public static event Action? ScaleChanged;

        public static void Increase()
        {
            double next = Math.Round(_currentScale + 0.1, 1);
            if (next <= 2.0)
            {
                _currentScale = next;
                SettingsManager.Current.FontScale = _currentScale;
                SettingsManager.Save();
                Reapply();
                ScaleChanged?.Invoke();
            }
        }

        public static void Decrease()
        {
            double next = Math.Round(_currentScale - 0.1, 1);
            if (next >= 0.6)
            {
                _currentScale = next;
                SettingsManager.Current.FontScale = _currentScale;
                SettingsManager.Save();
                Reapply();
                ScaleChanged?.Invoke();
            }
        }

        public static void Reset()
        {
            _currentScale = 1.0;
            _baseSizes.Clear();
            SettingsManager.Current.FontScale = _currentScale;
            SettingsManager.Save();
            ScaleChanged?.Invoke();
        }

        public static void InitializeFromSettings()
        {
            _currentScale = Math.Clamp(SettingsManager.Current.FontScale, 0.6, 2.0);
            _baseSizes.Clear();
        }

        public static void ApplyScale(DependencyObject root)
        {
            if (root == null) return;

            ApplyScaleToElement(root);

            int childCount = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                ApplyScale(child);
            }
        }

        private static void ApplyScaleToElement(DependencyObject element)
        {
            double originalSize = 0;
            bool hasFontSize = false;

            if (element is TextBlock tb)
            {
                if (!_baseSizes.ContainsKey(tb))
                {
                    _baseSizes[tb] = tb.FontSize;
                }
                originalSize = _baseSizes[tb];
                hasFontSize = true;
            }
            else if (element is Control control)
            {
                if (!_baseSizes.ContainsKey(control))
                {
                    _baseSizes[control] = control.FontSize;
                }
                originalSize = _baseSizes[control];
                hasFontSize = true;
            }
            else if (element is TextElement te)
            {
                if (!_baseSizes.ContainsKey(te))
                {
                    _baseSizes[te] = te.FontSize;
                }
                originalSize = _baseSizes[te];
                hasFontSize = true;
            }

            if (hasFontSize)
            {
                double scaled = Math.Round(originalSize * _currentScale, 1);
                if (element is TextBlock t)
                    t.FontSize = scaled;
                else if (element is Control c)
                    c.FontSize = scaled;
                else if (element is TextElement e)
                    e.FontSize = scaled;
            }
        }

        private static void Reapply()
        {
            foreach (var window in Application.Current.Windows)
            {
                if (window is Window w)
                {
                    ApplyScale(w);
                }
            }
        }

        public static void AutoScaleOnLoaded(Window window)
        {
            if (window.IsLoaded)
            {
                ApplyScale(window);
            }
            else
            {
                void handler(object? s, RoutedEventArgs e)
                {
                    ApplyScale(window);
                    window.Loaded -= handler;
                }
                window.Loaded += handler;
            }
        }
    }
}
