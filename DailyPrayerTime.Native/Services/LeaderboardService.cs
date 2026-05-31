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
        public string Month { get; set; } = "";
        public string LastUpdated { get; set; } = "";
        public int Rank { get; set; }
    }

    public class HallOfFameEntry
    {
        public string UserId { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public bool IsAnonymous { get; set; }
        public double CompletionRate { get; set; }
        public int TotalPrayersCompleted { get; set; }
        public int TotalDaysTracked { get; set; }
    }

    public class MonthlyHallOfFame
    {
        public string Month { get; set; } = "";
        public HallOfFameEntry? Top1 { get; set; }
        public HallOfFameEntry? Top2 { get; set; }
        public HallOfFameEntry? Top3 { get; set; }
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
                string currentMonth = DateTime.Today.ToString("yyyy-MM");

                foreach (var (id, data) in rawEntries)
                {
                    // Only show entries from current month
                    string entryMonth = data.TryGetValue("month", out var m) ? m.ToString() ?? "" : "";
                    if (!string.IsNullOrEmpty(entryMonth) && entryMonth != currentMonth)
                        continue;

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

                // Calculate stats from current month only
                int totalPrayersCompleted = 0;
                int totalDaysTracked = 0;
                int totalPrayerOpportunities = 0;

                DateTime now = DateTime.Today;
                DateTime monthStart = new DateTime(now.Year, now.Month, 1);
                DateTime monthEnd = now; // Up to today

                for (DateTime date = monthStart; date <= monthEnd; date = date.AddDays(1))
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
                    ["month"] = now.ToString("yyyy-MM"),
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

        // ─── Hall of Fame ────────────────────────────────────────
        public async Task SaveMonthlyTop3Async(string? month = null)
        {
            month ??= DateTime.Today.AddMonths(-1).ToString("yyyy-MM"); // Previous month

            try
            {
                // Fetch all leaderboard entries for that month
                var rawEntries = await FirestoreRestHelper.GetCollectionAsync("leaderboard");
                var monthEntries = rawEntries
                    .Where(r =>
                    {
                        string entryMonth = r.Data.TryGetValue("m", out var m) ? m.ToString() ?? "" : "";
                        return entryMonth == month;
                    })
                    .Select(r => new HallOfFameEntry
                    {
                        UserId = r.Id,
                        DisplayName = r.Data.TryGetValue("displayName", out var dn) ? dn.ToString() ?? "" : "",
                        IsAnonymous = r.Data.TryGetValue("isAnonymous", out var ia) && Convert.ToBoolean(ia),
                        CompletionRate = r.Data.TryGetValue("completionRate", out var cr) ? Convert.ToDouble(cr) : 0,
                        TotalPrayersCompleted = r.Data.TryGetValue("totalPrayersCompleted", out var tp) ? Convert.ToInt32(tp) : 0,
                        TotalDaysTracked = r.Data.TryGetValue("totalDaysTracked", out var td) ? Convert.ToInt32(td) : 0
                    })
                    .OrderByDescending(e => e.CompletionRate)
                    .ThenByDescending(e => e.TotalPrayersCompleted)
                    .Take(3)
                    .ToList();

                var hallOfFame = new MonthlyHallOfFame
                {
                    Month = month,
                    Top1 = monthEntries.ElementAtOrDefault(0),
                    Top2 = monthEntries.ElementAtOrDefault(1),
                    Top3 = monthEntries.ElementAtOrDefault(2)
                };

                await FirestoreRestHelper.SetDocumentAsync("hall_of_fame", month, hallOfFame);
                Debug.WriteLine($"Saved Hall of Fame for {month}: Top1={hallOfFame.Top1?.DisplayName}, Top2={hallOfFame.Top2?.DisplayName}, Top3={hallOfFame.Top3?.DisplayName}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save Hall of Fame: {ex.Message}");
            }
        }

        public async Task<List<MonthlyHallOfFame>> GetYearHallOfFameAsync(int? year = null)
        {
            year ??= DateTime.Today.Year;
            var results = new List<MonthlyHallOfFame>();

            try
            {
                // Fetch all hall_of_fame documents
                var rawEntries = await FirestoreRestHelper.GetCollectionAsync("hall_of_fame");

                foreach (var (id, data) in rawEntries)
                {
                    if (!id.StartsWith($"{year}-")) continue;

                    var hof = new MonthlyHallOfFame { Month = id };

                    if (data.TryGetValue("top1", out var t1) && t1 is Dictionary<string, object> t1Dict)
                        hof.Top1 = ParseHallOfFameEntry(t1Dict);
                    if (data.TryGetValue("top2", out var t2) && t2 is Dictionary<string, object> t2Dict)
                        hof.Top2 = ParseHallOfFameEntry(t2Dict);
                    if (data.TryGetValue("top3", out var t3) && t3 is Dictionary<string, object> t3Dict)
                        hof.Top3 = ParseHallOfFameEntry(t3Dict);

                    results.Add(hof);
                }

                results = results.OrderByDescending(h => h.Month).ToList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to fetch Hall of Fame: {ex.Message}");
            }

            return results;
        }

        private HallOfFameEntry ParseHallOfFameEntry(Dictionary<string, object> dict)
        {
            return new HallOfFameEntry
            {
                UserId = dict.TryGetValue("userId", out var uid) ? uid.ToString() ?? "" : "",
                DisplayName = dict.TryGetValue("displayName", out var dn) ? dn.ToString() ?? "" : "",
                IsAnonymous = dict.TryGetValue("isAnonymous", out var ia) && Convert.ToBoolean(ia),
                CompletionRate = dict.TryGetValue("completionRate", out var cr) ? Convert.ToDouble(cr) : 0,
                TotalPrayersCompleted = dict.TryGetValue("totalPrayersCompleted", out var tp) ? Convert.ToInt32(tp) : 0,
                TotalDaysTracked = dict.TryGetValue("totalDaysTracked", out var td) ? Convert.ToInt32(td) : 0
            };
        }

        public async Task CheckAndSavePreviousMonthAsync()
        {
            string lastSaved = SettingsManager.Current.LastHallOfFameMonth ?? "";
            string previousMonth = DateTime.Today.AddMonths(-1).ToString("yyyy-MM");

            if (lastSaved != previousMonth)
            {
                await SaveMonthlyTop3Async(previousMonth);
                SettingsManager.Current.LastHallOfFameMonth = previousMonth;
                SettingsManager.Save();
            }
        }
    }
}
