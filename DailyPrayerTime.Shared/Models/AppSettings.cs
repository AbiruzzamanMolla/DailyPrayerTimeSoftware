namespace DailyPrayerTime.Shared.Models
{
    public class AppSettings
    {
        public double Latitude { get; set; } = 23.8103;
        public double Longitude { get; set; } = 90.4125;
        public string LocationName { get; set; } = "Dhaka, Bangladesh";
        public string Method { get; set; } = "KARACHI";
        public int School { get; set; } = 1;
        public string TimeFormat { get; set; } = "12h";
        public string Language { get; set; } = "en";

        public double FajrAngle { get; set; } = 18.0;
        public double IshaAngle { get; set; } = 17.5;
        public int HighLatitudeRule { get; set; } = 0;

        public int SuhurOffset { get; set; } = 0;
        public int IftarOffset { get; set; } = 0;
    }
}
