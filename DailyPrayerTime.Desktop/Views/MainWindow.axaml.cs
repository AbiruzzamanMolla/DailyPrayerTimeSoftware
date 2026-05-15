using Avalonia.Controls;
using Avalonia.Input;
using System.Collections.Generic;
using System.Linq;
using DailyPrayerTime.Shared.Services;

namespace DailyPrayerTime.Desktop.Views;

public partial class MainWindow : Window
{
    private readonly List<PhraseItem> _phrases = TasbihService.DefaultPhrases;
    private readonly Dictionary<string, int> _counts = new();
    private readonly Dictionary<string, int> _targets = new()
    {
        ["SubhanAllah"] = 33,
        ["Alhamdulillah"] = 33,
        ["AllahuAkbar"] = 34,
        ["LaIlahaIllallah"] = 0,
        ["Astaghfirullah"] = 0,
    };
    private int _currentIdx;
    private PhraseItem Current => _phrases[_currentIdx];
    private int CurrentCount => _counts.GetValueOrDefault(Current.Key, 0);

    public MainWindow()
    {
        InitializeComponent();
        BuildPhraseChips();
        SelectPhrase(0);
        UpdateTasbihUI();
        LoadCounts();
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
                Padding = new Avalonia.Thickness(12, 6),
                Margin = new Avalonia.Thickness(2),
                Cursor = new Cursor(StandardCursorType.Hand),
                Tag = i
            };
            chip.PointerPressed += (s, e) =>
            {
                if (s is Border b && b.Tag is int idx)
                    SelectPhrase(idx);
            };

            chip.Child = new TextBlock
            {
                Text = _phrases[i].Arabic,
                Foreground = Avalonia.Media.Brushes.White,
                FontSize = 14,
                FontFamily = new Avalonia.Media.FontFamily("Traditional Arabic, Segoe UI"),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };
            PhrasePanel.Children.Add(chip);
            UpdateChipStyle(chip, i == _currentIdx);
        }
    }

    private void UpdateChipStyle(Border chip, bool selected)
    {
        if (selected)
        {
            chip.Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromArgb(51, 16, 185, 129));
            chip.BorderBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromArgb(96, 16, 185, 129));
        }
        else
        {
            chip.Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromArgb(26, 255, 255, 255));
            chip.BorderBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromArgb(42, 255, 255, 255));
        }
    }

    private void SelectPhrase(int idx)
    {
        if (idx < 0 || idx >= _phrases.Count) return;
        _currentIdx = idx;
        for (int i = 0; i < PhrasePanel.Children.Count; i++)
            if (PhrasePanel.Children[i] is Border chip)
                UpdateChipStyle(chip, i == idx);
        UpdateTasbihUI();
    }

    private void UpdateTasbihUI()
    {
        var phrase = Current;
        int count = CurrentCount;
        TbCount.Text = count.ToString();
        TbPhrase.Text = phrase.EnTranslit;
        TbLabel.Text = GetLabel(phrase.Key);
        int target = _targets.GetValueOrDefault(phrase.Key, 0);
        TbTarget.Text = target > 0 ? target.ToString() : "\U0001f3af";
        TbTotal.Text = _counts.Values.Sum().ToString();
    }

    private string GetLabel(string key) => key switch
    {
        "SubhanAllah" => "Glory be to Allah",
        "Alhamdulillah" => "Praise be to Allah",
        "AllahuAkbar" => "Allah is the Greatest",
        "LaIlahaIllallah" => "There is no god but Allah",
        "Astaghfirullah" => "I seek forgiveness from Allah",
        _ => key
    };

    private void LoadCounts()
    {
        var saved = TasbihService.Instance.LoadDay(System.DateTime.Today);
        _counts.Clear();
        foreach (var p in _phrases)
            _counts[p.Key] = saved.GetValueOrDefault(p.Key, 0);
        UpdateTasbihUI();
    }

    private void Save() =>
        TasbihService.Instance.SaveDay(System.DateTime.Today, _counts);

    private void Increment()
    {
        _counts[Current.Key] = CurrentCount + 1;
        Save();
        UpdateTasbihUI();
    }

    private void TasbihTap(object? sender, PointerPressedEventArgs e) => Increment();
    private void TasbihDec(object? sender, PointerPressedEventArgs e)
    {
        if (CurrentCount > 0) _counts[Current.Key] = CurrentCount - 1;
        Save(); UpdateTasbihUI();
    }
    private void TasbihReset(object? sender, PointerPressedEventArgs e)
    {
        _counts[Current.Key] = 0; Save(); UpdateTasbihUI();
    }
    private void TasbihTarget(object? sender, PointerPressedEventArgs e)
    {
        int target = _targets.GetValueOrDefault(Current.Key, 0);
        if (target > 0) { _counts[Current.Key] = System.Math.Max(0, CurrentCount - target); Save(); UpdateTasbihUI(); }
    }
    private void ApplySettings(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is ViewModels.MainWindowViewModel vm)
            vm.ApplySettings();
    }
}
