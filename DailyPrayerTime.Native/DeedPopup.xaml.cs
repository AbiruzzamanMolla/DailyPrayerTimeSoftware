using System;
using System.Collections.Generic;
using System.Windows;
using DailyPrayerTime.Native.Models;
using DailyPrayerTime.Native.Services;

namespace DailyPrayerTime.Native
{
    public partial class DeedPopup : Window
    {
        private DailyDeeds _deeds;
        private string _prayerName;

        public DeedPopup(string prayerName, List<DeedEntry> entries, DailyDeeds deeds)
        {
            InitializeComponent();
            FontSizeHelper.AutoScaleOnLoaded(this);
            _prayerName = prayerName;
            _deeds = deeds;
            TitleText.Text = $"DID YOU PRAY {prayerName.ToUpper()}?";
            DeedCheckList.ItemsSource = entries;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            TrackerService.Instance.SaveDay(_deeds);
            this.Close();
        }
    }
}
