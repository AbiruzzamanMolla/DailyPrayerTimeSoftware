using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DailyPrayerTime.Native.Helpers;
using DailyPrayerTime.Native.Models;
using Newtonsoft.Json;

namespace DailyPrayerTime.Native.Services
{
    public class CloudSyncService
    {
        private static readonly Lazy<CloudSyncService> _instance = new(() => new());
        public static CloudSyncService Instance => _instance.Value;

        private static readonly HttpClient _http = new();
        private Timer? _tasbihDebounceTimer;
        private Dictionary<string, int> _pendingTasbihCounts = new();
        private DateTime _pendingTasbihDate;
        private bool _isSyncing;

        public bool IsOnline => true;
        public bool IsSyncing => _isSyncing;
        public event Action? SyncCompleted;

        private CloudSyncService() { }

        private bool IsCloudEnabled =>
            SettingsManager.Current.CloudSyncEnabled &&
            AuthService.Instance.IsSignedIn;

        // ─── Tracker ────────────────────────────────────────────
        public async Task PushTrackerAsync(DailyDeeds deeds)
        {
            if (!IsCloudEnabled) return;
            try
            {
                var data = new Dictionary<string, object>
                {
                    ["prayers"] = deeds.Prayers,
                    ["sawm"] = deeds.Sawm,
                    ["extraAmols"] = deeds.ExtraAmols,
                    ["updatedAt"] = DateTime.UtcNow.ToString("o")
                };
                await FirestoreRestHelper.SetDocumentAsync("tracker", deeds.Date, data);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Cloud push tracker failed: {ex.Message}");
            }
        }

        public async Task<DailyDeeds?> PullTrackerAsync(DateTime date)
        {
            if (!IsCloudEnabled) return null;
            try
            {
                string docId = date.ToString("yyyy-MM-dd");
                var data = await FirestoreRestHelper.GetDocumentAsync("tracker", docId);
                if (data == null) return null;

                var deeds = new DailyDeeds { Date = docId };
                if (data.TryGetValue("sawm", out var sawm))
                    deeds.Sawm = Convert.ToBoolean(sawm);
                if (data.TryGetValue("extraAmols", out var extra) && extra is List<string> extraList)
                    deeds.ExtraAmols = extraList;
                if (data.TryGetValue("prayers", out var prayers) && prayers is Dictionary<string, object> prayerDict)
                {
                    foreach (var kv in prayerDict)
                    {
                        if (kv.Value is List<object> deedList)
                        {
                            var entries = new List<DeedEntry>();
                            foreach (var d in deedList)
                            {
                                if (d is Dictionary<string, object> dDict)
                                {
                                    var entry = new DeedEntry
                                    {
                                        Label = dDict.TryGetValue("Label", out var l) ? l.ToString() ?? "" : "",
                                        Count = dDict.TryGetValue("Count", out var c) ? Convert.ToInt32(c) : 0,
                                        IsChecked = dDict.TryGetValue("IsChecked", out var ic) && Convert.ToBoolean(ic),
                                        Type = dDict.TryGetValue("Type", out var t) && Enum.TryParse<DeedType>(t.ToString(), out var dt) ? dt : DeedType.Fard
                                    };
                                    entries.Add(entry);
                                }
                            }
                            deeds.Prayers[kv.Key] = entries;
                        }
                    }
                }
                return deeds;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Cloud pull tracker failed: {ex.Message}");
                return null;
            }
        }

        public async Task SyncAndMergeTrackerAsync(DateTime date)
        {
            if (!IsCloudEnabled) return;
            try
            {
                _isSyncing = true;
                var local = TrackerService.Instance.LoadDay(date);
                var cloud = await PullTrackerAsync(date);

                if (cloud != null)
                {
                    string localPath = System.IO.Path.Combine(
                        StorageService.GetAppDataPath(), "tracker", $"{date:yyyy-MM-dd}.json");
                    var localTime = System.IO.File.Exists(localPath)
                        ? System.IO.File.GetLastWriteTimeUtc(localPath)
                        : DateTime.MinValue;

                    string? cloudUpdatedStr = null;
                    // Try to get updatedAt from raw cloud data
                    var rawCloud = await FirestoreRestHelper.GetDocumentAsync("tracker", date.ToString("yyyy-MM-dd"));
                    if (rawCloud != null && rawCloud.TryGetValue("updatedAt", out var updatedAt))
                        cloudUpdatedStr = updatedAt.ToString();

                    DateTime cloudTime = DateTime.TryParse(cloudUpdatedStr, out var ct) ? ct : DateTime.MinValue;

                    if (cloudTime > localTime)
                    {
                        // Cloud is newer — overwrite local
                        TrackerService.Instance.SaveDay(cloud);
                        Debug.WriteLine($"Merged cloud → local for {date:yyyy-MM-dd}");
                    }
                    else if (localTime > cloudTime)
                    {
                        // Local is newer — push to cloud
                        await PushTrackerAsync(local);
                        Debug.WriteLine($"Pushed local → cloud for {date:yyyy-MM-dd}");
                    }
                }
                else
                {
                    // No cloud data — push local
                    await PushTrackerAsync(local);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Tracker sync failed: {ex.Message}");
            }
            finally
            {
                _isSyncing = false;
            }
        }

        // ─── Tasbih (debounced) ──────────────────────────────────
        public void PushTasbihDebounced(DateTime date, Dictionary<string, int> counts)
        {
            if (!IsCloudEnabled) return;
            _pendingTasbihDate = date;
            _pendingTasbihCounts = counts;
            _tasbihDebounceTimer?.Dispose();
            _tasbihDebounceTimer = new Timer(async _ => await FlushTasbih(), null, 10000, Timeout.Infinite);
        }

        private async Task FlushTasbih()
        {
            if (!IsCloudEnabled) return;
            try
            {
                string docId = _pendingTasbihDate.ToString("yyyy-MM-dd");
                var data = new Dictionary<string, object>
                {
                    ["counts"] = _pendingTasbihCounts,
                    ["updatedAt"] = DateTime.UtcNow.ToString("o")
                };
                await FirestoreRestHelper.SetDocumentAsync("tasbih", docId, data);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Cloud push tasbih failed: {ex.Message}");
            }
        }

        // ─── Ramadan ─────────────────────────────────────────────
        public async Task PushRamadanAsync(RamadanState state)
        {
            if (!IsCloudEnabled) return;
            try
            {
                var data = new Dictionary<string, object>
                {
                    ["year"] = state.Year,
                    ["prepChecklist"] = state.PrepChecklist,
                    ["dailyGoals"] = state.DailyGoals,
                    ["dailyGoalComplete"] = state.DailyGoalComplete,
                    ["laylatulQadrNights"] = state.LaylatulQadrNights,
                    ["updatedAt"] = DateTime.UtcNow.ToString("o")
                };
                await FirestoreRestHelper.SetDocumentAsync("ramadan", state.Year.ToString(), data);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Cloud push ramadan failed: {ex.Message}");
            }
        }

        public async Task<RamadanState?> PullRamadanAsync(int year)
        {
            if (!IsCloudEnabled) return null;
            try
            {
                var data = await FirestoreRestHelper.GetDocumentAsync("ramadan", year.ToString());
                if (data == null) return null;

                return new RamadanState
                {
                    Year = year,
                    PrepChecklist = data.TryGetValue("prepChecklist", out var pc) && pc is Dictionary<string, object> pcDict
                        ? pcDict.ToDictionary(k => k.Key, k => Convert.ToBoolean(k.Value)) : new(),
                    DailyGoals = data.TryGetValue("dailyGoals", out var dg) && dg is Dictionary<string, object> dgDict
                        ? dgDict.ToDictionary(k => int.Parse(k.Key), k => k.Value.ToString() ?? "") : new(),
                    DailyGoalComplete = data.TryGetValue("dailyGoalComplete", out var dgc) && dgc is Dictionary<string, object> dgcDict
                        ? dgcDict.ToDictionary(k => int.Parse(k.Key), k => Convert.ToBoolean(k.Value)) : new(),
                    LaylatulQadrNights = data.TryGetValue("laylatulQadrNights", out var lq) && lq is Dictionary<string, object> lqDict
                        ? lqDict.ToDictionary(k => int.Parse(k.Key), k => Convert.ToBoolean(k.Value)) : new()
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Cloud pull ramadan failed: {ex.Message}");
                return null;
            }
        }

        // ─── Cycle ──────────────────────────────────────────────
        public async Task PushCycleEntryAsync(string dateKey, CycleEntry entry)
        {
            if (!IsCloudEnabled) return;
            try
            {
                var data = new Dictionary<string, object>
                {
                    ["startDate"] = entry.StartDate,
                    ["endDate"] = entry.EndDate ?? "",
                    ["status"] = entry.Status.ToString(),
                    ["madhab"] = entry.Madhab,
                    ["notes"] = entry.Notes ?? "",
                    ["updatedAt"] = DateTime.UtcNow.ToString("o")
                };
                await FirestoreRestHelper.SetDocumentAsync("cycle", dateKey, data);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Cloud push cycle failed: {ex.Message}");
            }
        }

        public async Task PushCycleMetaAsync(CycleMeta meta)
        {
            if (!IsCloudEnabled) return;
            try
            {
                var data = new Dictionary<string, object>
                {
                    ["averageLength"] = meta.AverageCycleLength,
                    ["averagePeriodLength"] = meta.AveragePeriodLength,
                    ["lastPeriodStart"] = meta.LastPeriodStart ?? "",
                    ["totalCycles"] = meta.TotalCycles,
                    ["updatedAt"] = DateTime.UtcNow.ToString("o")
                };
                await FirestoreRestHelper.SetDocumentAsync("cycle", "meta", data);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Cloud push cycle meta failed: {ex.Message}");
            }
        }

        public async Task<CycleMeta?> PullCycleMetaAsync()
        {
            if (!IsCloudEnabled) return null;
            try
            {
                var data = await FirestoreRestHelper.GetDocumentAsync("cycle", "meta");
                if (data == null) return null;
                return new CycleMeta
                {
                    AverageCycleLength = data.TryGetValue("averageLength", out var al) ? Convert.ToInt32(al) : 28,
                    AveragePeriodLength = data.TryGetValue("averagePeriodLength", out var apl) ? Convert.ToInt32(apl) : 6,
                    LastPeriodStart = data.TryGetValue("lastPeriodStart", out var lps) ? lps.ToString() : "",
                    TotalCycles = data.TryGetValue("totalCycles", out var tc) ? Convert.ToInt32(tc) : 0
                };
            }
            catch { return null; }
        }

        // ─── Leaderboard ─────────────────────────────────────────
        public async Task PushLeaderboardAsync(LeaderboardEntry entry)
        {
            if (!IsCloudEnabled || string.IsNullOrEmpty(entry.UserId)) return;
            try
            {
                var data = new Dictionary<string, object>
                {
                    ["displayName"] = entry.DisplayName,
                    ["isAnonymous"] = entry.IsAnonymous,
                    ["totalPrayersCompleted"] = entry.TotalPrayersCompleted,
                    ["totalDaysTracked"] = entry.TotalDaysTracked,
                    ["completionRate"] = entry.CompletionRate,
                    ["lastUpdated"] = DateTime.UtcNow.ToString("o")
                };
                await FirestoreRestHelper.SetDocumentAsync("leaderboard", entry.UserId, data);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Cloud push leaderboard failed: {ex.Message}");
            }
        }

        public async Task<List<LeaderboardEntry>> PullLeaderboardAsync()
        {
            if (!IsCloudEnabled) return new List<LeaderboardEntry>();
            try
            {
                var rawEntries = await FirestoreRestHelper.GetCollectionAsync("leaderboard");
                var entries = rawEntries.Select(r => new LeaderboardEntry
                {
                    UserId = r.Id,
                    DisplayName = r.Data.TryGetValue("displayName", out var dn) ? dn.ToString() ?? "" : "",
                    IsAnonymous = r.Data.TryGetValue("isAnonymous", out var ia) && Convert.ToBoolean(ia),
                    TotalPrayersCompleted = r.Data.TryGetValue("totalPrayersCompleted", out var tp) ? Convert.ToInt32(tp) : 0,
                    TotalDaysTracked = r.Data.TryGetValue("totalDaysTracked", out var td) ? Convert.ToInt32(td) : 0,
                    CompletionRate = r.Data.TryGetValue("completionRate", out var cr) ? Convert.ToDouble(cr) : 0,
                    LastUpdated = r.Data.TryGetValue("lastUpdated", out var lu) ? lu.ToString() ?? "" : ""
                }).OrderByDescending(e => e.CompletionRate).ToList();

                for (int i = 0; i < entries.Count; i++)
                    entries[i].Rank = i + 1;

                return entries;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Cloud pull leaderboard failed: {ex.Message}");
                return new List<LeaderboardEntry>();
            }
        }

        // ─── Full Sync ───────────────────────────────────────────
        public async Task SyncAllAsync()
        {
            if (!IsCloudEnabled) return;
            try
            {
                _isSyncing = true;
                // Sync today and past 7 days
                for (int i = 0; i < 7; i++)
                {
                    await SyncAndMergeTrackerAsync(DateTime.Today.AddDays(-i));
                }
                SyncCompleted?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Full sync failed: {ex.Message}");
            }
            finally
            {
                _isSyncing = false;
            }
        }
    }
}
