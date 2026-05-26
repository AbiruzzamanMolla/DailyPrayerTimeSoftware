using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace DailyPrayerTime.Native
{
    public static class FontSizeHelper
    {
        private class BoxedDouble
        {
            public double Value { get; set; }
        }

        private static readonly ConditionalWeakTable<DependencyObject, BoxedDouble> _baseSizes = new ConditionalWeakTable<DependencyObject, BoxedDouble>();
        private static double _currentScale = 1.0;

        public static double CurrentScale => _currentScale;

        public static event Action? ScaleChanged;

        static FontSizeHelper()
        {
            // Register a global class handler for the Loaded event of FrameworkElements and TextElements
            EventManager.RegisterClassHandler(typeof(FrameworkElement), FrameworkElement.LoadedEvent, new RoutedEventHandler(OnElementLoaded));
            EventManager.RegisterClassHandler(typeof(TextElement), TextElement.LoadedEvent, new RoutedEventHandler(OnTextElementLoaded));
        }

        private static void OnElementLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is DependencyObject depObj)
            {
                ApplyScaleToElement(depObj);
            }
        }

        private static void OnTextElementLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is DependencyObject depObj)
            {
                ApplyScaleToElement(depObj);
            }
        }

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
            Reapply();
            SettingsManager.Current.FontScale = _currentScale;
            SettingsManager.Save();
            ScaleChanged?.Invoke();
        }

        public static void InitializeFromSettings()
        {
            _currentScale = Math.Clamp(SettingsManager.Current.FontScale, 0.6, 2.0);
        }

        public static void ApplyScale(DependencyObject root)
        {
            if (root == null) return;

            if (root is FrameworkElement fe && !fe.IsLoaded)
            {
                return;
            }

            ApplyScaleToElement(root);

            int childCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < childCount; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(root, i);
                ApplyScale(child);
            }
        }

        private static void ApplyScaleToElement(DependencyObject element)
        {
            double originalSize = 0;
            bool hasFontSize = false;

            if (element is TextBlock tb)
            {
                var boxed = _baseSizes.GetValue(tb, key => new BoxedDouble { Value = ((TextBlock)key).FontSize });
                originalSize = boxed.Value;
                hasFontSize = true;
            }
            else if (element is System.Windows.Controls.Control control)
            {
                var boxed = _baseSizes.GetValue(control, key => new BoxedDouble { Value = ((System.Windows.Controls.Control)key).FontSize });
                originalSize = boxed.Value;
                hasFontSize = true;
            }
            else if (element is TextElement te)
            {
                var boxed = _baseSizes.GetValue(te, key => new BoxedDouble { Value = ((TextElement)key).FontSize });
                originalSize = boxed.Value;
                hasFontSize = true;
            }

            if (hasFontSize)
            {
                double scaled = Math.Round(originalSize * _currentScale, 1);
                if (element is TextBlock t)
                    t.FontSize = scaled;
                else if (element is System.Windows.Controls.Control c)
                    c.FontSize = scaled;
                else if (element is TextElement e)
                    e.FontSize = scaled;
            }
        }

        private static void Reapply()
        {
            foreach (var window in System.Windows.Application.Current.Windows)
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
