using System;
using System.Runtime.InteropServices;
using System.Windows;
using CSDeskBand;

namespace DailyPrayerTime.DeskBand
{
    [ComVisible(true)]
    [Guid("D8A1B2C3-E4F5-46A7-9B0C-1D2E3F4A5B6C")] // Unique GUID for this DeskBand
    [CSDeskBandRegistration(Name = "Daily Prayer Timer", ShowDeskBand = true)]
    public class Deskband : CSDeskBandWpf
    {
        private PrayerTimerControl _control;

        protected override UIElement UIElement => _control ?? (_control = new PrayerTimerControl());

        public Deskband()
        {
            // Configure sizing
            Options.MinHorizontalSize = new DeskBandSize(180, 40);
            Options.HorizontalSize = new DeskBandSize(200, 40);
            Options.Title = "Daily Prayer Timer";
        }

        protected override void DeskbandOnClosed()
        {
            _control = null;
            base.DeskbandOnClosed();
        }
    }
}
