using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using DailyPrayerTime.Native.Services;

namespace DailyPrayerTime.Native
{
    public partial class TasbihView : System.Windows.Controls.UserControl
    {
        private readonly List<PhraseItem> _phrases;
        private readonly Dictionary<string, int> _counts = new();
        private readonly Dictionary<string, int> _targets = new();
        private int _currentIndex = 0;
        private DateTime _currentDate;
        private PhraseItem CurrentPhrase => _phrases[_currentIndex];
        private int CurrentCount => _counts.GetValueOrDefault(CurrentPhrase.Key, 0);

        public TasbihView()
        {
            InitializeComponent();
            _phrases = TasbihService.DefaultPhrases;
            _targets["SubhanAllah"] = 33;
            _targets["Alhamdulillah"] = 33;
            _targets["AllahuAkbar"] = 34;
            _targets["LaIlahaIllallah"] = 0;
            _targets["Astaghfirullah"] = 0;

            BuildPhraseChips();
            SelectPhrase(0);
            SetupKeyboard();
        }

        public void LoadData()
        {
            _currentDate = DateTime.Today;
            var saved = TasbihService.Instance.LoadDay(_currentDate);
            _counts.Clear();
            foreach (var phrase in _phrases)
                _counts[phrase.Key] = saved.GetValueOrDefault(phrase.Key, 0);
            UpdateUI();
        }

        private void BuildPhraseChips()
        {
            for (int i = 0; i < _phrases.Count; i++)
            {
                var chip = new System.Windows.Controls.Border
                {
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(26, 255, 255, 255)),
                    BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(42, 255, 255, 255)),
                    BorderThickness = new System.Windows.Thickness(1.0),
                    CornerRadius = new System.Windows.CornerRadius(20.0),
                    Padding = new System.Windows.Thickness(12.0, 6.0, 12.0, 6.0),
                    Margin = new System.Windows.Thickness(2.0),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Tag = i
                };
                chip.MouseLeftButtonUp += PhraseChip_Click;

                var text = new System.Windows.Controls.TextBlock
                {
                    Text = _phrases[i].Arabic,
                    Foreground = System.Windows.Media.Brushes.White,
                    FontSize = 14.0,
                    FontFamily = new System.Windows.Media.FontFamily("Traditional Arabic, Segoe UI"),
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center
                };
                chip.Child = text;
                PhrasePanel.Children.Add(chip);
                UpdateChipStyle(chip, i == _currentIndex);
            }
        }

        private void PhraseChip_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Border chip && chip.Tag is int index)
                SelectPhrase(index);
        }

        private void SelectPhrase(int index)
        {
            if (index < 0 || index >= _phrases.Count) return;
            _currentIndex = index;

            for (int i = 0; i < PhrasePanel.Children.Count; i++)
            {
                if (PhrasePanel.Children[i] is System.Windows.Controls.Border chip)
                    UpdateChipStyle(chip, i == index);
            }

            UpdateUI();
        }

        private void UpdateChipStyle(System.Windows.Controls.Border chip, bool selected)
        {
            if (selected)
            {
                chip.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(51, 16, 185, 129));
                chip.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(96, 16, 185, 129));
                if (chip.Child is System.Windows.Controls.TextBlock t)
                    t.Foreground = System.Windows.Media.Brushes.White;
            }
            else
            {
                chip.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(26, 255, 255, 255));
                chip.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(42, 255, 255, 255));
                if (chip.Child is System.Windows.Controls.TextBlock t)
                    t.Foreground = System.Windows.Media.Brushes.White;
            }
        }

        private void UpdateUI()
        {
            var phrase = CurrentPhrase;
            int count = CurrentCount;
            string key = phrase.Key;
            int target = _targets.GetValueOrDefault(key, 0);

            CountText.Text = count.ToString();
            PhraseText.Text = phrase.Arabic;
            PhraseNameText.Text = GetPhraseDisplayName(key);

            TargetText.Text = target > 0 ? target.ToString() : "\U0001f3af";

            int total = _counts.Values.Sum();
            TotalText.Text = total.ToString();

            AnimateCount();
        }

        private string GetPhraseDisplayName(string key)
        {
            switch (key)
            {
                case "SubhanAllah": return LocalizationManager.Instance.GetString("Tasbih_SubhanAllah");
                case "Alhamdulillah": return LocalizationManager.Instance.GetString("Tasbih_Alhamdulillah");
                case "AllahuAkbar": return LocalizationManager.Instance.GetString("Tasbih_AllahuAkbar");
                case "LaIlahaIllallah": return LocalizationManager.Instance.GetString("Tasbih_LaIlaha");
                case "Astaghfirullah": return LocalizationManager.Instance.GetString("Tasbih_Astaghfirullah");
                default: return key;
            }
        }

        private void AnimateCount()
        {
            var anim = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 0.6,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(150),
                EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
            };
            var transform = new System.Windows.Media.ScaleTransform(1.0, 1.0);
            CountText.RenderTransform = transform;
            CountText.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
            transform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, anim);
            transform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, anim);
        }

        private void Increment()
        {
            var phrase = CurrentPhrase;
            _counts[phrase.Key] = CurrentCount + 1;
            Save();
            UpdateUI();
        }

        private void TapArea_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Increment();
        }

        private void Decrement_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var phrase = CurrentPhrase;
            if (CurrentCount > 0)
                _counts[phrase.Key] = CurrentCount - 1;
            Save();
            UpdateUI();
        }

        private void Reset_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var phrase = CurrentPhrase;
            _counts[phrase.Key] = 0;
            Save();
            UpdateUI();
        }

        private void Target_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var phrase = CurrentPhrase;
            string key = phrase.Key;
            int target = _targets.GetValueOrDefault(key, 0);
            if (target > 0)
            {
                _counts[phrase.Key] = Math.Max(0, CurrentCount - target);
                Save();
                UpdateUI();
            }
        }

        private void SetupKeyboard()
        {
            Loaded += (s, e) =>
            {
                var win = System.Windows.Window.GetWindow(this);
                if (win != null)
                    win.KeyDown += Window_KeyDown;
            };
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Space || e.Key == System.Windows.Input.Key.Enter)
            {
                if (IsVisible)
                {
                    Increment();
                    e.Handled = true;
                }
            }
        }

        private void Save()
        {
            TasbihService.Instance.SaveDay(_currentDate, _counts);
        }
    }
}
