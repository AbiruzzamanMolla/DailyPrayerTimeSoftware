using Avalonia.Controls;
using Avalonia.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using DailyPrayerTime.Shared.Models;
using DailyPrayerTime.Shared.Services;
using Newtonsoft.Json;

namespace DailyPrayerTime.Desktop.Views;

public class DuaCardItem : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public int Index { get; set; }
    public string Name { get; set; } = "";
    public string Arabic { get; set; } = "";
    public string Reference { get; set; } = "";
    public string Transliteration { get; set; } = "";
    public string Translation { get; set; } = "";
    private bool _expanded;
    public bool IsExpanded { get => _expanded; set { _expanded = value; OnChanged(); OnChanged(nameof(Arrow)); OnChanged(nameof(ContentVisible)); } }
    public string Arrow => IsExpanded ? "\u25b2" : "\u25bc";
    public bool ContentVisible => IsExpanded;
    private void OnChanged([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new(n ?? ""));
}

public class DuaRaw { public int Index { get; set; } public string Name { get; set; } = ""; public string Arabic { get; set; } = ""; public string Reference { get; set; } = ""; public DuaLang? En { get; set; } public DuaLang? Bn { get; set; } }
public class DuaLang { public string? Name { get; set; } public string? Transliteration { get; set; } public string? Translation { get; set; } }

public partial class MainWindow : Window
{
    private readonly List<PhraseItem> _phrases = TasbihService.DefaultPhrases;
    private readonly Dictionary<string, int> _counts = new();
    private readonly Dictionary<string, int> _targets = new()
    {
        ["SubhanAllah"] = 33, ["Alhamdulillah"] = 33, ["AllahuAkbar"] = 34,
        ["LaIlahaIllallah"] = 0, ["Astaghfirullah"] = 0,
    };
    private int _currentIdx;
    private PhraseItem Current => _phrases[_currentIdx];
    private int CurrentCount => _counts.GetValueOrDefault(Current.Key, 0);
    private ObservableCollection<DuaCardItem> _duaCards = new();
    private DailyDeeds _todayDeeds = new();
    private bool _sawmTracked;
    private bool _notificationsEnabled = true;

    public MainWindow()
    {
        InitializeComponent();
        BuildPhraseChips();
        SelectPhrase(0);
        UpdateTasbihUI();
        LoadCounts();
        DuasList.ItemsSource = _duaCards;
        LoadDuas();
        LoadTracker();
    }

    private void BuildPhraseChips()
    {
        for (int i = 0; i < _phrases.Count; i++)
        {
            var chip = new Border
            {
                Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromArgb(26, 255, 255, 255)),
                BorderBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromArgb(42, 255, 255, 255)),
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(20),
                Padding = new Avalonia.Thickness(12, 6), Margin = new Avalonia.Thickness(2),
                Cursor = new Cursor(StandardCursorType.Hand), Tag = i
            };
            chip.PointerPressed += (s, e) => { if (s is Border b && b.Tag is int idx) SelectPhrase(idx); };
            chip.Child = new TextBlock { Text = _phrases[i].Arabic, Foreground = Avalonia.Media.Brushes.White, FontSize = 14, FontFamily = new Avalonia.Media.FontFamily("Traditional Arabic, Segoe UI"), HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };
            PhrasePanel.Children.Add(chip);
            UpdateChipStyle(chip, i == _currentIdx);
        }
    }

    private void UpdateChipStyle(Border chip, bool selected)
    {
        if (selected) { chip.Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromArgb(51, 16, 185, 129)); chip.BorderBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromArgb(96, 16, 185, 129)); }
        else { chip.Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromArgb(26, 255, 255, 255)); chip.BorderBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromArgb(42, 255, 255, 255)); }
    }

    private void SelectPhrase(int idx)
    {
        if (idx < 0 || idx >= _phrases.Count) return;
        _currentIdx = idx;
        for (int i = 0; i < PhrasePanel.Children.Count; i++)
            if (PhrasePanel.Children[i] is Border chip) UpdateChipStyle(chip, i == idx);
        UpdateTasbihUI();
    }

    private void UpdateTasbihUI()
    {
        var phrase = Current; int count = CurrentCount;
        TbCount.Text = count.ToString(); TbPhrase.Text = phrase.EnTranslit;
        TbLabel.Text = GetLabel(phrase.Key);
        int target = _targets.GetValueOrDefault(phrase.Key, 0);
        TbTarget.Text = target > 0 ? target.ToString() : "\U0001f3af";
        TbTotal.Text = _counts.Values.Sum().ToString();
    }

    private string GetLabel(string key) => key switch
    {
        "SubhanAllah" => "Glory be to Allah", "Alhamdulillah" => "Praise be to Allah",
        "AllahuAkbar" => "Allah is the Greatest", "LaIlahaIllallah" => "There is no god but Allah",
        "Astaghfirullah" => "I seek forgiveness from Allah", _ => key
    };

    private void LoadCounts()
    {
        var saved = TasbihService.Instance.LoadDay(System.DateTime.Today);
        _counts.Clear();
        foreach (var p in _phrases) _counts[p.Key] = saved.GetValueOrDefault(p.Key, 0);
        UpdateTasbihUI();
    }

    private void Save() => TasbihService.Instance.SaveDay(System.DateTime.Today, _counts);
    private void Increment() { _counts[Current.Key] = CurrentCount + 1; Save(); UpdateTasbihUI(); }
    private void TasbihTap(object? s, PointerPressedEventArgs e) => Increment();
    private void TasbihDec(object? s, PointerPressedEventArgs e) { if (CurrentCount > 0) _counts[Current.Key] = CurrentCount - 1; Save(); UpdateTasbihUI(); }
    private void TasbihReset(object? s, PointerPressedEventArgs e) { _counts[Current.Key] = 0; Save(); UpdateTasbihUI(); }
    private void TasbihTarget(object? s, PointerPressedEventArgs e)
    {
        int t = _targets.GetValueOrDefault(Current.Key, 0);
        if (t > 0) { _counts[Current.Key] = System.Math.Max(0, CurrentCount - t); Save(); UpdateTasbihUI(); }
    }
    private void ApplySettings(object? s, PointerPressedEventArgs e)
    {
        if (DataContext is ViewModels.MainWindowViewModel vm) vm.ApplySettings();
    }

    private void LoadDuas()
    {
        try
        {
            string path = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "i18n", "duas.json");
            if (!File.Exists(path)) return;
            var raw = JsonConvert.DeserializeObject<List<DuaRaw>>(File.ReadAllText(path));
            if (raw == null) return;
            _duaCards.Clear();
            foreach (var r in raw)
            {
                var lang = r.En;
                _duaCards.Add(new DuaCardItem
                {
                    Index = r.Index, Name = lang?.Name ?? r.Name, Arabic = r.Arabic, Reference = r.Reference,
                    Transliteration = lang?.Transliteration ?? "", Translation = lang?.Translation ?? ""
                });
            }
        }
        catch { }
    }

    private void ToggleDuaCard(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Avalonia.Controls.Control c && c.DataContext is DuaCardItem item)
            item.IsExpanded = !item.IsExpanded;
    }

    private void LoadTracker()
    {
        _todayDeeds = TrackerService.Instance.LoadDay(System.DateTime.Today);
        _sawmTracked = _todayDeeds.Sawm;
        RenderTrackerItems();
        UpdateSawmUI();
    }

    private void RenderTrackerItems()
    {
        var items = new List<DeedEntry>();
        string[] prayers = { "Fajr", "Dhuhr", "Asr", "Maghrib", "Isha" };
        foreach (var p in prayers)
            if (_todayDeeds.Prayers.TryGetValue(p, out var entries))
                items.AddRange(entries);
        TrackPrayerList.ItemsSource = items;
    }

    private void ToggleTrackItem(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Avalonia.Controls.Control c && c.DataContext is DeedEntry entry)
        {
            entry.IsChecked = !entry.IsChecked;
            TrackerService.Instance.SaveDay(_todayDeeds);
            RenderTrackerItems();
        }
    }

    private void ToggleSawm(object? sender, PointerPressedEventArgs e)
    {
        _sawmTracked = !_sawmTracked;
        _todayDeeds.Sawm = _sawmTracked;
        TrackerService.Instance.SaveDay(_todayDeeds);
        UpdateSawmUI();
    }

    private void UpdateSawmUI()
    {
        if (_sawmTracked)
        {
            SawmCheck.Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromArgb(255, 16, 185, 129));
            SawmCheck.BorderBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromArgb(255, 16, 185, 129));
            SawmLabel.Text = "Fasting today ✓";
        }
        else
        {
            SawmCheck.Background = Avalonia.Media.Brushes.Transparent;
            SawmCheck.BorderBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromArgb(175, 255, 255, 255));
            SawmLabel.Text = "Mark today as fasting";
        }
    }

    private void TrackShowDay(object? sender, PointerPressedEventArgs e)
    {
        TrackDayPanel.IsVisible = true;
        TrackWeekPanel.IsVisible = false;
        TrackDayBtn.Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromArgb(255, 16, 185, 129));
        TrackWeekBtn.Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromArgb(26, 255, 255, 255));
    }

    private void TrackShowWeek(object? sender, PointerPressedEventArgs e)
    {
        TrackDayPanel.IsVisible = false;
        TrackWeekPanel.IsVisible = true;
        TrackDayBtn.Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromArgb(26, 255, 255, 255));
        TrackWeekBtn.Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromArgb(255, 16, 185, 129));

        int total = 0, done = 0;
        for (int i = 0; i < 7; i++)
        {
            var day = TrackerService.Instance.LoadDay(System.DateTime.Today.AddDays(-i));
            foreach (var prayer in day.Prayers.Values)
                foreach (var entry in prayer) { total++; if (entry.IsChecked) done++; }
        }
        WeekSummaryText.Text = total > 0 ? $"{done}/{total} deeds completed this week ({done * 100 / total}%)" : "No data this week";
    }

    private void ToggleNotify(object? sender, PointerPressedEventArgs e)
    {
        _notificationsEnabled = !_notificationsEnabled;
        ViewModels.MainWindowViewModel.NotificationsEnabled = _notificationsEnabled;
        if (_notificationsEnabled)
        {
            NotifyToggle.Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromArgb(255, 16, 185, 129));
            NotifyKnob.Fill = Avalonia.Media.Brushes.White;
            NotifyKnob.Margin = new Avalonia.Thickness(21, 0, 0, 0);
        }
        else
        {
            NotifyToggle.Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromArgb((byte)42, (byte)255, (byte)255, (byte)255));
            NotifyKnob.Fill = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb((byte)156, (byte)163, (byte)175));
            NotifyKnob.Margin = new Avalonia.Thickness(3, 0, 0, 0);
        }
    }

    public bool NotificationsEnabled => _notificationsEnabled;
}
