using System;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DailyPrayerTime.Shared.Services
{
    public class UpdateInfo
    {
        public string LatestVersion { get; set; } = "";
        public string ReleaseUrl { get; set; } = "";
        public bool IsUpdateAvailable { get; set; }
    }

    public static class UpdateService
    {
        private static readonly HttpClient Client = new();
        private const string RepoUrl = "https://api.github.com/repos/AbiruzzamanMolla/DailyPrayerTimeSoftware/releases/latest";

        static UpdateService()
        {
            Client.DefaultRequestHeaders.Add("User-Agent", "DailyPrayerTime-Avalonia-Updater");
        }

        public static string AppVersion { get; set; } = "2.4.0";

        public static async Task<UpdateInfo> CheckForUpdateAsync()
        {
            try
            {
                var response = await Client.GetStringAsync(RepoUrl);
                var latestRelease = JsonConvert.DeserializeObject<dynamic>(response);
                if (latestRelease == null) return new UpdateInfo();

                string tagName = latestRelease.tag_name;
                string htmlUrl = latestRelease.html_url;
                string latestVersionStr = tagName.TrimStart('v');

                if (Version.TryParse(latestVersionStr, out var latest) &&
                    Version.TryParse(AppVersion, out var current))
                {
                    return new UpdateInfo
                    {
                        LatestVersion = latestVersionStr,
                        ReleaseUrl = htmlUrl,
                        IsUpdateAvailable = latest > current
                    };
                }
            }
            catch { }

            return new UpdateInfo();
        }
    }
}
