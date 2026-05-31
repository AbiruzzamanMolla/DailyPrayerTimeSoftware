using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DailyPrayerTime.Native.Services;

namespace DailyPrayerTime.Native.Views
{
    public partial class LeaderboardView : UserControl
    {
        public LeaderboardView()
        {
            InitializeComponent();
        }

        public async void LoadData()
        {
            if (!AuthService.Instance.IsSignedIn)
            {
                LoadingText.Text = LocalizationManager.Instance.GetString("Leaderboard_SignInRequired") ?? "Sign in to view leaderboard";
                LoadingText.Visibility = Visibility.Visible;
                return;
            }

            // Show current month
            MonthLabel.Text = DateTime.Today.ToString("MMMM yyyy");

            LoadingText.Visibility = Visibility.Visible;
            LoadingText.Text = LocalizationManager.Instance.GetString("Leaderboard_Loading") ?? "Loading...";

            var entries = await LeaderboardService.Instance.GetLeaderboardAsync();
            DisplayLeaderboard(entries);
        }

        private void DisplayLeaderboard(List<LeaderboardEntry> entries)
        {
            LeaderboardList.Children.Clear();
            LoadingText.Visibility = Visibility.Collapsed;

            if (entries.Count == 0)
            {
                LoadingText.Text = LocalizationManager.Instance.GetString("Leaderboard_Empty") ?? "No data yet. Start tracking to appear here!";
                LoadingText.Visibility = Visibility.Visible;
                return;
            }

            // Show my ranking card
            var myEntry = entries.FirstOrDefault(e => e.UserId == AuthService.Instance.Uid);
            if (myEntry != null)
            {
                MyRankCard.Visibility = Visibility.Visible;
                MyRankText.Text = $"#{myEntry.Rank}";
                MyNameText.Text = myEntry.IsAnonymous ? "You (Anonymous)" : $"You ({myEntry.DisplayName})";
                MyStatsText.Text = $"{myEntry.TotalPrayersCompleted} prayers · {myEntry.TotalDaysTracked} days tracked";
                MyRateText.Text = $"{myEntry.CompletionRate}%";
            }

            // Show top entries
            int displayCount = Math.Min(entries.Count, 50);
            for (int i = 0; i < displayCount; i++)
            {
                var entry = entries[i];
                bool isMe = entry.UserId == AuthService.Instance.Uid;
                LeaderboardList.Children.Add(CreateEntryRow(entry, isMe));
            }
        }

        private Border CreateEntryRow(LeaderboardEntry entry, bool isMe)
        {
            var bg = isMe ? new SolidColorBrush(Color.FromArgb(0x33, 0x10, 0xB9, 0x81)) : new SolidColorBrush(Color.FromArgb(0x1A, 0xFF, 0xFF, 0xFF));
            var borderBrush = isMe ? new SolidColorBrush(Color.FromArgb(0x60, 0x10, 0xB9, 0x81)) : new SolidColorBrush(Color.FromArgb(0x2A, 0xFF, 0xFF, 0xFF));

            string rankEmoji = entry.Rank switch
            {
                1 => "🥇",
                2 => "🥈",
                3 => "🥉",
                _ => $"#{entry.Rank}"
            };

            string displayName = entry.IsAnonymous ? "Anonymous" : entry.DisplayName;
            if (isMe && !entry.IsAnonymous) displayName = $"{entry.DisplayName} (You)";
            else if (isMe && entry.IsAnonymous) displayName = "Anonymous (You)";

            var rankText = new TextBlock
            {
                Text = entry.Rank <= 3 ? rankEmoji : rankEmoji,
                FontSize = entry.Rank <= 3 ? 18 : 11,
                FontWeight = entry.Rank <= 3 ? FontWeights.Bold : FontWeights.Normal,
                Foreground = entry.Rank <= 3 ? new SolidColorBrush(Color.FromRgb(0xFB, 0xBF, 0x24)) : new SolidColorBrush(Colors.White),
                VerticalAlignment = VerticalAlignment.Center,
                Width = 35,
                TextAlignment = TextAlignment.Center
            };

            var nameBlock = new TextBlock
            {
                Text = displayName,
                FontSize = 12,
                FontWeight = isMe ? FontWeights.SemiBold : FontWeights.Normal,
                Foreground = new SolidColorBrush(Colors.White),
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            var statsBlock = new TextBlock
            {
                Text = $"{entry.TotalPrayersCompleted} prayers · {entry.TotalDaysTracked}d",
                FontSize = 9,
                Foreground = new SolidColorBrush(Color.FromArgb(0x99, 0xFF, 0xFF, 0xFF)),
                VerticalAlignment = VerticalAlignment.Center
            };

            var namePanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            namePanel.Children.Add(nameBlock);
            namePanel.Children.Add(statsBlock);

            var rateBlock = new TextBlock
            {
                Text = $"{entry.CompletionRate}%",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = entry.CompletionRate >= 80 ? new SolidColorBrush(Color.FromRgb(0x34, 0xD3, 0x99))
                           : entry.CompletionRate >= 50 ? new SolidColorBrush(Color.FromRgb(0xFB, 0xBF, 0x24))
                           : new SolidColorBrush(Color.FromRgb(0xF8, 0x71, 0x71)),
                VerticalAlignment = VerticalAlignment.Center
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Grid.SetColumn(rankText, 0);
            Grid.SetColumn(namePanel, 1);
            Grid.SetColumn(rateBlock, 2);
            grid.Children.Add(rankText);
            grid.Children.Add(namePanel);
            grid.Children.Add(rateBlock);

            return new Border
            {
                Background = bg,
                BorderBrush = borderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 8, 12, 8),
                Margin = new Thickness(0, 0, 0, 4),
                Child = grid
            };
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LeaderboardService.Instance.ForceRefresh();
            LoadData();
        }
    }
}
