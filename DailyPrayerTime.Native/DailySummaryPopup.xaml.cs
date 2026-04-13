using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using DailyPrayerTime.Native.Models;

namespace DailyPrayerTime.Native
{
    public partial class DailySummaryPopup : Window
    {
        public DailySummaryPopup(DailyDeeds deeds)
        {
            InitializeComponent();
            var summary = deeds.Prayers.Select(p => new SummaryItem 
            { 
                Label = p.Key.ToUpper(), 
                StatusText = $"{p.Value.Count(d => d.IsChecked)}/{p.Value.Count} Done" 
            }).ToList();
            
            // Add Sawm status
            summary.Add(new SummaryItem { Label = "SAWM (FASTING)", StatusText = deeds.Sawm ? "COMPLETED" : "NOT TRACKED" });

            SummaryList.ItemsSource = summary;
        }

        private void Finalize_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    public class SummaryItem
    {
        public string Label { get; set; } = "";
        public string StatusText { get; set; } = "";
    }
}
