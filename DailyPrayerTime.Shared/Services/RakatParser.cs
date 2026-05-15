using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DailyPrayerTime.Shared.Models;

namespace DailyPrayerTime.Shared.Services
{
    public static class RakatParser
    {
        public static List<DeedEntry> Parse(string note)
        {
            var entries = new List<DeedEntry>();
            if (string.IsNullOrEmpty(note)) return entries;

            string cleanNote = note;
            int totalIdx = note.LastIndexOf('(');
            if (totalIdx != -1 && note.ToLower().Contains("total"))
                cleanNote = note.Substring(0, totalIdx);

            var parts = cleanNote.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                string p = part.Trim();
                var match = Regex.Match(p, @"(\d+)\s+(Sunnah|Fard|Nafl|Witr|Wajib)", RegexOptions.IgnoreCase);

                if (match.Success)
                {
                    int count = int.Parse(match.Groups[1].Value);
                    string typeStr = match.Groups[2].Value.ToLower();
                    DeedType type = DeedType.Custom;
                    if (typeStr == "sunnah") type = DeedType.Sunnah;
                    else if (typeStr == "fard") type = DeedType.Fard;
                    else if (typeStr == "nafl") type = DeedType.Nafl;
                    else if (typeStr == "witr" || typeStr == "wajib") type = DeedType.Witr;

                    entries.Add(new DeedEntry { Label = p, Count = count, Type = type, IsChecked = false });
                }
                else if (p.ToLower().Contains("min") || p.ToLower().Contains("rakat"))
                {
                    entries.Add(new DeedEntry { Label = p, Type = DeedType.Nafl, Value = 2, IsChecked = false });
                }
            }

            entries.Add(new DeedEntry { Label = "Adhkar", Type = DeedType.Adhkar, IsChecked = false });
            return entries;
        }
    }
}
