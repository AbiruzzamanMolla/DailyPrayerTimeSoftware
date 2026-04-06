using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Reflection;
using System.Diagnostics;

namespace DailyPrayerTime.Native
{
    public class UpdateInfo
    {
        public string LatestVersion { get; set; } = "";
        public string ReleaseUrl { get; set; } = "";
        public bool IsUpdateAvailable { get; set; }
    }

    public static class UpdateService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private const string RepoUrl = "https://api.github.com/repos/AbiruzzamanMolla/DailyPrayerTimeSoftware/releases/latest";

        static UpdateService()
        {
            // GitHub API requires a User-Agent header
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "DailyPrayerTime-Native-Updater");
        }

        public static async Task<UpdateInfo> CheckForUpdateAsync()
        {
            try
            {
                var response = await _httpClient.GetStringAsync(RepoUrl);
                var latestRelease = JsonConvert.DeserializeObject<dynamic>(response);

                if (latestRelease == null) return new UpdateInfo();

                string tagName = latestRelease.tag_name; // e.g., "v1.5.4"
                string htmlUrl = latestRelease.html_url;

                string currentVersion = GetCurrentVersion();
                string latestVersionStr = tagName.TrimStart('v');

                if (Version.TryParse(latestVersionStr, out Version? latest) && 
                    Version.TryParse(currentVersion, out Version? current))
                {
                    return new UpdateInfo
                    {
                        LatestVersion = latestVersionStr,
                        ReleaseUrl = htmlUrl,
                        IsUpdateAvailable = latest > current
                    };
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Update check failed: {ex.Message}");
            }

            return new UpdateInfo();
        }

        private static string GetCurrentVersion()
        {
            // Try to get version from the executing assembly
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";
        }
    }
}
