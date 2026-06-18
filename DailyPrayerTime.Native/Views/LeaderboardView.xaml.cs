using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DailyPrayerTime.Native.Services;
using Color = System.Windows.Media.Color;
using MessageBox = System.Windows.MessageBox;
using Button = System.Windows.Controls.Button;
using UserControl = System.Windows.Controls.UserControl;

namespace DailyPrayerTime.Native.Views
{
    public partial class LeaderboardView : System.Windows.Controls.UserControl
    {
        private bool _isHallOfFameView = false;

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

            if (_isHallOfFameView)
            {
                await LoadHallOfFameAsync();
            }
            else
            {
                await LoadMonthlyLeaderboardAsync();
            }
        }

        private async System.Threading.Tasks.Task LoadMonthlyLeaderboardAsync()
        {
            var entries = await LeaderboardService.Instance.GetLeaderboardAsync();
            DisplayLeaderboard(entries);
        }

        private async System.Threading.Tasks.Task LoadHallOfFameAsync()
        {
            var hallOfFame = await LeaderboardService.Instance.GetYearHallOfFameAsync();
            DisplayHallOfFame(hallOfFame);
        }

        private void DisplayLeaderboard(List<LeaderboardEntry> entries)
        {
            ContentPanel.Children.Clear();
            LoadingText.Visibility = Visibility.Collapsed;

            if (entries.Count == 0)
            {
                ContentPanel.Children.Add(new TextBlock
                {
                    Text = LocalizationManager.Instance.GetString("Leaderboard_Empty") ?? "No data yet. Start tracking to appear here!",
                    Foreground = new SolidColorBrush(Colors.White),
                    Opacity = 0.5,
                    FontSize = 12,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 20, 0, 0)
                });
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
            else
            {
                MyRankCard.Visibility = Visibility.Collapsed;
            }

            // Show top entries
            int displayCount = Math.Min(entries.Count, 50);
            for (int i = 0; i < displayCount; i++)
            {
                var entry = entries[i];
                bool isMe = entry.UserId == AuthService.Instance.Uid;
                ContentPanel.Children.Add(CreateLeaderboardRow(entry, isMe));
            }
        }

        private void DisplayHallOfFame(List<MonthlyHallOfFame> hallOfFame)
        {
            ContentPanel.Children.Clear();
            LoadingText.Visibility = Visibility.Collapsed;
            MyRankCard.Visibility = Visibility.Collapsed;

            if (hallOfFame.Count == 0)
            {
                ContentPanel.Children.Add(new TextBlock
                {
                    Text = "No champions yet. Complete a full month to appear here!",
                    Foreground = new SolidColorBrush(Colors.White),
                    Opacity = 0.5,
                    FontSize = 12,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 20, 0, 0)
                });
                return;
            }

            foreach (var month in hallOfFame)
            {
                ContentPanel.Children.Add(CreateMonthHeader(month.Month));
                if (month.Top1 != null) ContentPanel.Children.Add(CreateChampionRow(month.Top1, "🥇", "#fbbf24"));
                if (month.Top2 != null) ContentPanel.Children.Add(CreateChampionRow(month.Top2, "🥈", "#C0C0C0"));
                if (month.Top3 != null) ContentPanel.Children.Add(CreateChampionRow(month.Top3, "🥉", "#CD7F32"));
                ContentPanel.Children.Add(new Border { Margin = new Thickness(0, 0, 0, 12) });
            }
        }

        private Border CreateMonthHeader(string month)
        {
            DateTime monthDate = DateTime.ParseExact(month + "-01", "yyyy-MM-dd", null);
            string displayMonth = monthDate.ToString("MMMM yyyy");

            return new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(0x33, 0xF5, 0x9E, 0x0B)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 0, 0, 8),
                Child = new TextBlock
                {
                    Text = $"👑 {displayMonth}",
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xFB, 0xBF, 0x24)),
                    HorizontalAlignment = HorizontalAlignment.Center
                }
            };
        }

        private Border CreateChampionRow(HallOfFameEntry entry, string medal, string medalColor)
        {
            string displayName = entry.IsAnonymous ? "Anonymous" : entry.DisplayName;

            var medalText = new TextBlock
            {
                Text = medal,
                FontSize = 18,
                VerticalAlignment = VerticalAlignment.Center,
                Width = 30,
                TextAlignment = TextAlignment.Center
            };

            var nameBlock = new TextBlock
            {
                Text = displayName,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Colors.White),
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            var statsBlock = new TextBlock
            {
                Text = $"{entry.TotalPrayersCompleted} prayers · {entry.TotalDaysTracked} days",
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
                Foreground = new SolidColorBrush(Color.FromRgb(0x34, 0xD3, 0x99)),
                VerticalAlignment = VerticalAlignment.Center
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(35) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Grid.SetColumn(medalText, 0);
            Grid.SetColumn(namePanel, 1);
            Grid.SetColumn(rateBlock, 2);
            grid.Children.Add(medalText);
            grid.Children.Add(namePanel);
            grid.Children.Add(rateBlock);

            return new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(0x15, 0xFF, 0xFF, 0xFF)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(0x25, 0xFF, 0xFF, 0xFF)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 8, 12, 8),
                Margin = new Thickness(0, 0, 0, 4),
                Child = grid
            };
        }

        private Border CreateLeaderboardRow(LeaderboardEntry entry, bool isMe)
        {
            var bg = isMe ? (Brush)Application.Current.Resources["ThemePrimaryTranslucentBrush"] : new SolidColorBrush(Color.FromArgb(0x1A, 0xFF, 0xFF, 0xFF));
            var borderBrush = isMe ? (Brush)Application.Current.Resources["ThemePrimaryBrush"] : new SolidColorBrush(Color.FromArgb(0x2A, 0xFF, 0xFF, 0xFF));

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

        private void TabMonthly_Click(object sender, RoutedEventArgs e)
        {
            _isHallOfFameView = false;
            TabMonthly.Background = (Brush)Application.Current.Resources["ThemePrimaryBrush"];
            TabMonthly.BorderBrush = (Brush)Application.Current.Resources["ThemeSecondaryBrush"];
            
            TabHallOfFame.Background = new SolidColorBrush(Color.FromArgb(0x10, 0xFF, 0xFF, 0xFF));
            TabHallOfFame.BorderBrush = new SolidColorBrush(Color.FromArgb(0x20, 0xFF, 0xFF, 0xFF));
            
            MonthLabel.Text = DateTime.Today.ToString("MMMM yyyy");
            LoadData();
        }
 
        private void TabHallOfFame_Click(object sender, RoutedEventArgs e)
        {
            _isHallOfFameView = true;
            TabHallOfFame.Background = (Brush)Application.Current.Resources["ThemePrimaryBrush"];
            TabHallOfFame.BorderBrush = (Brush)Application.Current.Resources["ThemeSecondaryBrush"];
            
            TabMonthly.Background = new SolidColorBrush(Color.FromArgb(0x10, 0xFF, 0xFF, 0xFF));
            TabMonthly.BorderBrush = new SolidColorBrush(Color.FromArgb(0x20, 0xFF, 0xFF, 0xFF));
            
            MonthLabel.Text = DateTime.Today.Year.ToString();
            LoadData();
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LeaderboardService.Instance.ForceRefresh();
            LoadData();
        }
    }
}
