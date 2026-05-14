using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using DailyPrayerTime.Native.Services;
using Newtonsoft.Json;

namespace DailyPrayerTime.Native
{
    public class DuaCardItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public int Id { get; set; }
        public string Name { get; set; } = "";
        public List<DuaSegment> Segments { get; set; } = new();

        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded == value) return;
                _isExpanded = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Arrow));
                OnPropertyChanged(nameof(ContentVisibility));
            }
        }

        public string Arrow => IsExpanded ? "\u25b2" : "\u25bc";
        public Visibility ContentVisibility => IsExpanded ? Visibility.Visible : Visibility.Collapsed;

        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name ?? ""));
    }

    public class DuaSegment
    {
        public string Arabic { get; set; } = "";
        public string Transliteration { get; set; } = "";
        public string Translation { get; set; } = "";
        public string Reference { get; set; } = "";
    }

    public partial class TasbihView : System.Windows.Controls.UserControl
    {
        private readonly List<PhraseItem> _phrases;
        private readonly Dictionary<string, int> _counts = new();
        private readonly Dictionary<string, int> _targets = new();
        private int _currentIndex = 0;
        private DateTime _currentDate;
        private PhraseItem CurrentPhrase => _phrases[_currentIndex];
        private int CurrentCount => _counts.GetValueOrDefault(CurrentPhrase.Key, 0);
        private ObservableCollection<DuaCardItem> _duaCards = new();

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
            DuasList.ItemsSource = _duaCards;
        }

        public void LoadData()
        {
            _currentDate = DateTime.Today;
            var saved = TasbihService.Instance.LoadDay(_currentDate);
            _counts.Clear();
            foreach (var phrase in _phrases)
                _counts[phrase.Key] = saved.GetValueOrDefault(phrase.Key, 0);
            UpdateUI();
            LoadDuas();
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
                if (IsVisible && TasbihPanel.Visibility == Visibility.Visible)
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

        private void SwitchToTasbih(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            TasbihPanel.Visibility = Visibility.Visible;
            DuasPanel.Visibility = Visibility.Collapsed;
            TabTasbih.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(51, 16, 185, 129));
            TabDuas.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(26, 255, 255, 255));
        }

        private void SwitchToDuas(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            TasbihPanel.Visibility = Visibility.Collapsed;
            DuasPanel.Visibility = Visibility.Visible;
            TabTasbih.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(26, 255, 255, 255));
            TabDuas.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(51, 16, 185, 129));
        }

        private static readonly HashSet<int> _englishIds = new()
        {
            63, 64, 65, 68, 70, 100,
            200, 201, 202, 203, 204, 205, 206, 207, 208, 209, 210
        };

        private static readonly HashSet<int> _bengaliIds = new()
        {
            360, 361, 362, 363, 364, 365, 366, 367, 368, 369,
            370, 371, 372, 373, 374, 375, 376, 377, 378, 379,
            380, 381, 382
        };

        private List<DuaRawData>? _allDuas;

        private void LoadDuas()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "i18n", "duas.json");
                if (!File.Exists(path)) return;

                string json = File.ReadAllText(path);
                _allDuas = JsonConvert.DeserializeObject<List<DuaRawData>>(json);
                if (_allDuas == null) return;

                ApplyLanguageFilter();
            }
            catch { }
        }

        private void ApplyLanguageFilter()
        {
            if (_allDuas == null) return;

            bool isEnglish = true;
            if (DuaLangSelector.SelectedItem is System.Windows.Controls.ComboBoxItem item && item.Tag is string tag)
                isEnglish = tag == "en";

            var allowed = isEnglish ? _englishIds : _bengaliIds;

            _duaCards.Clear();
            foreach (var r in _allDuas)
            {
                if (!allowed.Contains(r.Id)) continue;
                _duaCards.Add(new DuaCardItem
                {
                    Id = r.Id,
                    Name = r.Name,
                    Segments = r.Segments,
                    IsExpanded = false
                });
            }
        }

        private void DuaLangSelector_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            ApplyLanguageFilter();
        }

        private void ToggleDuaCard(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is System.Windows.FrameworkElement fe && fe.DataContext is DuaCardItem item)
                item.IsExpanded = !item.IsExpanded;
        }
    }

    public class DuaRawData
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public List<DuaSegment> Segments { get; set; } = new();
    }
}
