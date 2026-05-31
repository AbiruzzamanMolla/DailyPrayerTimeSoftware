using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DailyPrayerTime.Native.Models;
using DailyPrayerTime.Native.Helpers;

namespace DailyPrayerTime.Native.Services
{
    public class LeaderboardEntry
    {
        public string UserId { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public bool IsAnonymous { get; set; }
        public int TotalPrayersCompleted { get; set; }
        public int TotalDaysTracked { get; set; }
        public double CompletionRate { get; set; }
        public string LastUpdated { get; set; } = "";
        public int Rank { get; set; }
    }

    public class LeaderboardService
    {
        private static readonly Lazy<LeaderboardService> _instance = new(() => new());
        public static LeaderboardService Instance => _instance.Value;

        private List<LeaderboardEntry> _cachedEntries = new();
        private DateTime _lastFetch = DateTime.MinValue;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

        public event Action? LeaderboardUpdated;

        private LeaderboardService() { }

        public List<LeaderboardEntry> CachedEntries => _cachedEntries;

        public void ForceRefresh()
        {
            _cachedEntries.Clear();
            _lastFetch = DateTime.MinValue;
        }

        public async Task<List<LeaderboardEntry>> GetLeaderboardAsync()
        {
            // Use cache if fresh
            if (_cachedEntries.Count > 0 && (DateTime.Now - _lastFetch) < CacheDuration)
                return _cachedEntries;

            var entries = new List<LeaderboardEntry>();

            try
            {
                // Ensure current user's stats are pushed first
                await PushMyStatsAsync();

                // Fetch all leaderboard entries
                var rawEntries = await FirestoreRestHelper.GetCollectionAsync("leaderboard");

                foreach (var (id, data) in rawEntries)
                {
                    var entry = new LeaderboardEntry
                    {
                        UserId = id,
                        DisplayName = data.TryGetValue("displayName", out var dn) ? dn.ToString() ?? "" : "",
                        IsAnonymous = data.TryGetValue("isAnonymous", out var ia) && Convert.ToBoolean(ia),
                        TotalPrayersCompleted = data.TryGetValue("totalPrayersCompleted", out var tp) ? Convert.ToInt32(tp) : 0,
                        TotalDaysTracked = data.TryGetValue("totalDaysTracked", out var td) ? Convert.ToInt32(td) : 0,
                        CompletionRate = data.TryGetValue("completionRate", out var cr) ? Convert.ToDouble(cr) : 0,
                        LastUpdated = data.TryGetValue("lastUpdated", out var lu) ? lu.ToString() ?? "" : ""
                    };
                    entries.Add(entry);
                }

                // Sort by completion rate (descending), then by total prayers
                entries = entries
                    .OrderByDescending(e => e.CompletionRate)
                    .ThenByDescending(e => e.TotalPrayersCompleted)
                    .ToList();

                // Assign ranks
                for (int i = 0; i < entries.Count; i++)
                    entries[i].Rank = i + 1;

                _cachedEntries = entries;
                _lastFetch = DateTime.Now;
                LeaderboardUpdated?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to fetch leaderboard: {ex.Message}");
            }

            return _cachedEntries;
        }

        public async Task PushMyStatsAsync()
        {
            if (!AuthService.Instance.IsSignedIn) return;

            try
            {
                string uid = AuthService.Instance.Uid!;
                bool isAnonymous = SettingsManager.Current.LeaderboardAnonymous;

                // Calculate stats from local tracker data
                int totalPrayersCompleted = 0;
                int totalDaysTracked = 0;
                int totalPrayerOpportunities = 0;

                for (int i = 0; i < 30; i++)
                {
                    DateTime date = DateTime.Today.AddDays(-i);
                    var deeds = TrackerService.Instance.LoadDay(date);

                    bool hasData = deeds.Prayers.Values.Any(p => p.Any(d => d.IsChecked));
                    if (hasData) totalDaysTracked++;

                    foreach (var prayer in deeds.Prayers)
                    {
                        // Skip nafal/adhkar from leaderboard calculation
                        if (prayer.Key == "Tahajjud" || prayer.Key == "Duha" ||
                            prayer.Key == "Awwabin" || prayer.Key == "Adhkar")
                            continue;

                        foreach (var deed in prayer.Value)
                        {
                            if (deed.Type == DeedType.Fard)
                            {
                                totalPrayerOpportunities++;
                                if (deed.IsChecked) totalPrayersCompleted++;
                            }
                        }
                    }
                }

                double completionRate = totalPrayerOpportunities > 0
                    ? Math.Round((double)totalPrayersCompleted / totalPrayerOpportunities * 100, 1)
                    : 0;

                string displayName = isAnonymous
                    ? "Anonymous"
                    : (AuthService.Instance.DisplayName ?? "User");

                var stats = new Dictionary<string, object>
                {
                    ["displayName"] = displayName,
                    ["isAnonymous"] = isAnonymous,
                    ["totalPrayersCompleted"] = totalPrayersCompleted,
                    ["totalDaysTracked"] = totalDaysTracked,
                    ["completionRate"] = completionRate,
                    ["lastUpdated"] = DateTime.UtcNow.ToString("o")
                };

                await FirestoreRestHelper.SetDocumentAsync("leaderboard", uid, stats);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to push leaderboard stats: {ex.Message}");
            }
        }

        public async Task ToggleAnonymousAsync(bool anonymous)
        {
            SettingsManager.Current.LeaderboardAnonymous = anonymous;
            SettingsManager.Save();
            await PushMyStatsAsync();
            _cachedEntries.Clear(); // Force refresh
            await GetLeaderboardAsync();
        }

        public LeaderboardEntry? GetMyRanking()
        {
            if (!AuthService.Instance.IsSignedIn) return null;
            string uid = AuthService.Instance.Uid!;
            return _cachedEntries.FirstOrDefault(e => e.UserId == uid);
        }
    }
}
