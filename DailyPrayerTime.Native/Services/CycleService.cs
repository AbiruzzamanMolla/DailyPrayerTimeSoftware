using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DailyPrayerTime.Native.Models;
using Newtonsoft.Json;

namespace DailyPrayerTime.Native.Services
{
    public class CycleService
    {
        private static readonly Lazy<CycleService> _instance = new(() => new());
        public static CycleService Instance => _instance.Value;

        private string CycleDir => Path.Combine(StorageService.GetAppDataPath(), "cycle");
        private string EntriesPath => Path.Combine(CycleDir, "entries.json");
        private string MetaPath => Path.Combine(CycleDir, "meta.json");

        private List<CycleEntry> _entries = new();
        private CycleMeta _meta = new();

        private CycleService()
        {
            if (!Directory.Exists(CycleDir))
                Directory.CreateDirectory(CycleDir);
            LoadAll();
        }

        public List<CycleEntry> Entries => _entries;
        public CycleMeta Meta => _meta;
        public string SelectedMadhab => SettingsManager.Current.SelectedCycleMadhab ?? "Sistani";

        // ─── Persistence ─────────────────────────────────────────
        private void LoadAll()
        {
            if (File.Exists(EntriesPath))
            {
                try
                {
                    _entries = JsonConvert.DeserializeObject<List<CycleEntry>>(File.ReadAllText(EntriesPath)) ?? new();
                }
                catch { _entries = new(); }
            }
            if (File.Exists(MetaPath))
            {
                try
                {
                    _meta = JsonConvert.DeserializeObject<CycleMeta>(File.ReadAllText(MetaPath)) ?? new();
                }
                catch { _meta = new(); }
            }
        }

        public void SaveAll()
        {
            File.WriteAllText(EntriesPath, JsonConvert.SerializeObject(_entries, Formatting.Indented));
            File.WriteAllText(MetaPath, JsonConvert.SerializeObject(_meta, Formatting.Indented));
            _ = CloudSyncService.Instance.PushCycleMetaAsync(_meta);
        }

        // ─── Entry Management ────────────────────────────────────
        public void AddEntry(CycleEntry entry)
        {
            _entries.Add(entry);
            _entries = _entries.OrderBy(e => e.StartDate).ToList();
            UpdateMeta();
            SaveAll();
        }

        public void UpdateEntry(string startDate, CycleEntry updated)
        {
            var idx = _entries.FindIndex(e => e.StartDate == startDate);
            if (idx >= 0)
            {
                _entries[idx] = updated;
                UpdateMeta();
                SaveAll();
            }
        }

        public void DeleteEntry(string startDate)
        {
            _entries.RemoveAll(e => e.StartDate == startDate);
            UpdateMeta();
            SaveAll();
        }

        // ─── Cycle Calculation ───────────────────────────────────
        public CycleDayInfo GetDayInfo(DateTime date)
        {
            string dateStr = date.ToString("yyyy-MM-dd");
            var today = DateTime.Today;
            var info = new CycleDayInfo
            {
                Date = date,
                IsCurrentDay = date.Date == today.Date
            };

            // Find the most recent entry that started on or before this date
            var entry = _entries
                .Where(e => !string.IsNullOrEmpty(e.StartDate) && e.StartDate.CompareTo(dateStr) <= 0)
                .OrderByDescending(e => e.StartDate)
                .FirstOrDefault();

            if (entry == null)
            {
                info.Status = CycleStatus.Unknown;
                info.StatusText = "No data";
                info.StatusTextArabic = "لا توجد بيانات";
                return info;
            }

            var (minHayd, maxHayd, minTuhur) = MadhabRules.GetRules(entry.Madhab);
            DateTime startDate = DateTime.Parse(entry.StartDate);
            int daysSinceStart = (date - startDate).Days;

            if (string.IsNullOrEmpty(entry.EndDate))
            {
                // Period hasn't ended yet — still in Hayd
                info.Status = CycleStatus.Hayd;
                info.StatusText = "Menstruation (Hayd)";
                info.StatusTextArabic = "حيض";
                info.PeriodDayNumber = daysSinceStart + 1;
                info.CycleDayNumber = daysSinceStart + 1;
                info.IsQadaRequired = date.Month == today.Month
                    ? RamadanData.GetCurrentRamadanDay(date) > 0
                    : false;
                return info;
            }

            DateTime endDate = DateTime.Parse(entry.EndDate);
            int periodDays = (endDate - startDate).Days + 1;

            if (date.Date >= startDate.Date && date.Date <= endDate.Date)
            {
                // Within the recorded period
                info.Status = CycleStatus.Hayd;
                info.StatusText = $"Menstruation (Day {daysSinceStart + 1} of {periodDays})";
                info.StatusTextArabic = $"حيض (اليوم {daysSinceStart + 1} من {periodDays})";
                info.PeriodDayNumber = daysSinceStart + 1;
                info.CycleDayNumber = daysSinceStart + 1;
            }
            else if (date.Date > endDate.Date)
            {
                int daysAfterEnd = (date - endDate).Days;
                int daysBetweenEndAndNext = _entries
                    .Where(e => !string.IsNullOrEmpty(e.StartDate) && e.StartDate > entry.EndDate)
                    .OrderBy(e => e.StartDate)
                    .Select(e => DateTime.Parse(e.StartDate))
                    .FirstOrDefault() == default
                        ? 0
                        : (_entries.Where(e => !string.IsNullOrEmpty(e.StartDate) && e.StartDate > entry.EndDate)
                            .OrderBy(e => e.StartDate)
                            .Select(e => (DateTime.Parse(e.StartDate) - endDate).Days)
                            .FirstOrDefault());

                if (daysAfterEnd < minTuhur)
                {
                    // Safe period (Tuhur)
                    info.Status = CycleStatus.Tuhr;
                    info.StatusText = $"Safe period (Tuhur Day {daysAfterEnd + 1})";
                    info.StatusTextArabic = $"طهارة (اليوم {daysAfterEnd + 1})";
                }
                else if (daysAfterEnd >= minTuhur)
                {
                    // Waiting for next cycle — predicted period
                    int predictedCycleEnd = _meta.AverageCycleLength;
                    int cycleDay = daysSinceStart + 1;

                    if (cycleDay <= _meta.AveragePeriodLength)
                    {
                        // Predicted Hayd
                        info.Status = CycleStatus.Hayd;
                        info.StatusText = $"Predicted menstruation (Day {cycleDay})";
                        info.StatusTextArabic = $"حيض متوقع (اليوم {cycleDay})";
                        info.PeriodDayNumber = cycleDay;
                    }
                    else
                    {
                        info.Status = CycleStatus.Tuhr;
                        info.StatusText = $"Safe period (Day {daysAfterEnd + 1} after period)";
                        info.StatusTextArabic = $"طهارة (اليوم {daysAfterEnd + 1} بعد الدورة)";
                    }
                }
            }
            else
            {
                // Before the entry start — shouldn't normally happen, but handle gracefully
                info.Status = CycleStatus.Unknown;
            }

            info.CycleDayNumber = daysSinceStart + 1;
            return info;
        }

        // ─── Meta Update ─────────────────────────────────────────
        private void UpdateMeta()
        {
            if (_entries.Count == 0) return;

            var completed = _entries.Where(e => !string.IsNullOrEmpty(e.EndDate)).ToList();
            _meta.TotalCycles = completed.Count;
            _meta.LastPeriodStart = _entries.Last().StartDate;

            if (completed.Count >= 2)
            {
                var lengths = new List<int>();
                var periodLengths = new List<int>();
                for (int i = 1; i < completed.Count; i++)
                {
                    var prev = DateTime.Parse(completed[i - 1].StartDate);
                    var curr = DateTime.Parse(completed[i].StartDate);
                    lengths.Add((curr - prev).Days);
                    periodLengths.Add((DateTime.Parse(completed[i - 1].EndDate!) - prev).Days + 1);
                }
                _meta.AverageCycleLength = (int)lengths.Average();
                _meta.AveragePeriodLength = (int)periodLengths.Average();
            }
            else if (completed.Count == 1)
            {
                _meta.AveragePeriodLength = (DateTime.Parse(completed[0].EndDate!) - DateTime.Parse(completed[0].StartDate)).Days + 1;
            }
        }

        // ─── Predictions ─────────────────────────────────────────
        public DateTime? GetNextPeriodStart()
        {
            if (_entries.Count == 0 || string.IsNullOrEmpty(_meta.LastPeriodStart)) return null;
            DateTime lastStart = DateTime.Parse(_meta.LastPeriodStart);
            return lastStart.AddDays(_meta.AverageCycleLength);
        }

        public DateTime? GetNextPeriodEnd()
        {
            var nextStart = GetNextPeriodStart();
            if (nextStart == null) return null;
            return nextStart.Value.AddDays(_meta.AveragePeriodLength - 1);
        }

        public int GetDaysUntilNextPeriod()
        {
            var next = GetNextPeriodStart();
            if (next == null) return -1;
            int days = (next.Value - DateTime.Today).Days;
            return days >= 0 ? days : 0;
        }

        // ─── Status Check ────────────────────────────────────────
        public bool IsInHayd(DateTime date)
        {
            return GetDayInfo(date).Status == CycleStatus.Hayd;
        }

        public bool IsInTuhr(DateTime date)
        {
            return GetDayInfo(date).Status == CycleStatus.Tuhr;
        }

        public List<string> GetMissedRamadanFasts()
        {
            var missed = new List<string>();
            int year = DateTime.Today.Year;
            // Check current Ramadan
            for (int day = 1; day <= 30; day++)
            {
                try
                {
                    var umAlQura = new System.Globalization.UmAlQuraCalendar();
                    var ramadanDate = umAlQura.ToDateTime(year, 9, day, 0, 0, 0, 0);
                    if (IsInHayd(ramadanDate))
                        missed.Add(ramadanDate.ToString("yyyy-MM-dd"));
                }
                catch { }
            }
            return missed;
        }
    }
}
