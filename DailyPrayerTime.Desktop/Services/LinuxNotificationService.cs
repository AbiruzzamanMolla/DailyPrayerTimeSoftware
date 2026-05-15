using System.Diagnostics;

namespace DailyPrayerTime.Desktop.Services
{
    public static class LinuxNotificationService
    {
        public static void Show(string title, string message)
        {
            try
            {
                var psi = new ProcessStartInfo("notify-send", $"\"{Sanitize(title)}\" \"{Sanitize(message)}\"")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process.Start(psi);
            }
            catch
            {
                // notify-send not available — silently ignore
            }
        }

        private static string Sanitize(string text)
        {
            return text.Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", "");
        }
    }
}
