using System;
using System.IO;
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
        public string AssetDownloadUrl { get; set; } = "";
        public string AssetName { get; set; } = "";
    }

    public static class UpdateService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private const string RepoUrl = "https://api.github.com/repos/AbiruzzamanMolla/DailyPrayerTimeSoftware/releases/latest";

        public static string? DownloadedUpdatePath { get; private set; }

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
                    bool isUpdateAvailable = latest > current;
                    string assetUrl = "";
                    string assetName = "";

                    if (isUpdateAvailable && latestRelease.assets != null)
                    {
                        bool isPortable = StorageService.IsPortable();
                        bool is64 = Environment.Is64BitOperatingSystem;

                        foreach (var asset in latestRelease.assets)
                        {
                            string name = ((string)asset.name).ToLower();
                            if (isPortable)
                            {
                                if (name.Contains("portable"))
                                {
                                    if (is64 && name.Contains("x64"))
                                    {
                                        assetUrl = asset.browser_download_url;
                                        assetName = asset.name;
                                        break;
                                    }
                                    else if (!is64 && name.Contains("x86"))
                                    {
                                        assetUrl = asset.browser_download_url;
                                        assetName = asset.name;
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                if (name.Contains("setup"))
                                {
                                    if (is64 && name.Contains("x64"))
                                    {
                                        assetUrl = asset.browser_download_url;
                                        assetName = asset.name;
                                        break;
                                    }
                                    else if (!is64 && name.Contains("x86"))
                                    {
                                        assetUrl = asset.browser_download_url;
                                        assetName = asset.name;
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    return new UpdateInfo
                    {
                        LatestVersion = latestVersionStr,
                        ReleaseUrl = htmlUrl,
                        IsUpdateAvailable = isUpdateAvailable,
                        AssetDownloadUrl = assetUrl,
                        AssetName = assetName
                    };
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Update check failed: {ex.Message}");
            }

            return new UpdateInfo();
        }

        public static async Task StartBackgroundDownloadAsync(string downloadUrl, string assetName)
        {
            try
            {
                string tempDir = Path.Combine(Path.GetTempPath(), "DailyPrayerTimeUpdates");
                Directory.CreateDirectory(tempDir);
                string targetPath = Path.Combine(tempDir, assetName);

                if (File.Exists(targetPath))
                {
                    File.Delete(targetPath);
                }

                using (var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                    {
                        await stream.CopyToAsync(fileStream);
                    }
                }

                DownloadedUpdatePath = targetPath;
                Debug.WriteLine($"Update downloaded successfully to: {targetPath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to download update: {ex.Message}");
            }
        }

        public static void InstallUpdate()
        {
            if (string.IsNullOrEmpty(DownloadedUpdatePath) || !File.Exists(DownloadedUpdatePath))
                return;

            try
            {
                bool isPortable = StorageService.IsPortable();
                if (isPortable)
                {
                    string currentExe = Process.GetCurrentProcess().MainModule?.FileName ?? "";
                    if (string.IsNullOrEmpty(currentExe)) return;

                    string tempDir = Path.GetDirectoryName(DownloadedUpdatePath) ?? Path.GetTempPath();
                    string batchPath = Path.Combine(tempDir, "update_portable.bat");

                    string batContent = $@"@echo off
timeout /t 2 /nobreak > nul
copy /y ""{DownloadedUpdatePath}"" ""{currentExe}""
start """" ""{currentExe}""
del ""%~f0""
";
                    File.WriteAllText(batchPath, batContent);

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = batchPath,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        CreateNoWindow = true,
                        UseShellExecute = false
                    });
                }
                else
                {
                    // Installed version (Inno Setup)
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = DownloadedUpdatePath,
                        Arguments = "/VERYSILENT /SUPPRESSMSGBOXES",
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to launch update installer: {ex.Message}");
            }
        }

        private static string GetCurrentVersion()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";
        }
    }
}
