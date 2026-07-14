using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using DailyPrayerTime.Native.Helpers;

namespace DailyPrayerTime.Native.Services
{
    public class AnalyticsService
    {
        private static readonly HttpClient _http = new();
        private static AnalyticsService? _instance;
        public static AnalyticsService Instance => _instance ??= new AnalyticsService();

        private bool _isStarted = false;
        private string ProjectId => FirebaseConfig.ProjectId;
        private string BaseUrl => $"https://firestore.googleapis.com/v1/projects/{ProjectId}/databases/(default)/documents";

        public event Action<int>? UserCountUpdated;

        private AnalyticsService() { }

        public void Start()
        {
            if (_isStarted) return;
            _isStarted = true;

            // Generate Device ID if it doesn't exist
            if (string.IsNullOrEmpty(SettingsManager.Current.AnalyticsDeviceId))
            {
                SettingsManager.Current.AnalyticsDeviceId = Guid.NewGuid().ToString();
                SettingsManager.Save();
            }

            // Start periodic hourly ping in background task
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        await PingActiveAsync();
                    }
                    catch (Exception ex)
                    {
                        AppLogger.Log($"Analytics heartbeat failed: {ex.Message}");
                    }
                    await Task.Delay(TimeSpan.FromHours(1));
                }
            });
        }

        public async Task PingActiveAsync()
        {
            string deviceId = SettingsManager.Current.AnalyticsDeviceId;
            string dateStr = DateTime.UtcNow.ToString("yyyy-MM-dd");
            string docId = $"{dateStr}_{deviceId}";
            string url = $"{BaseUrl}/active_users/{docId}";

            var body = new
            {
                fields = new
                {
                    deviceId = new { stringValue = deviceId },
                    lastActive = new { stringValue = DateTime.UtcNow.ToString("o") },
                    date = new { stringValue = dateStr }
                }
            };

            var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
            var response = await _http.PatchAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                string errMsg = await response.Content.ReadAsStringAsync();
                AppLogger.Log($"Analytics ping failed: {response.StatusCode} - {errMsg}");
            }
            else
            {
                // Retrieve updated count after successful ping
                _ = FetchActiveUserCountAsync();
            }
        }

        public async Task<int> FetchActiveUserCountAsync()
        {
            string dateStr = DateTime.UtcNow.ToString("yyyy-MM-dd");
            string url = $"{BaseUrl}:runQuery";

            var query = new
            {
                structuredQuery = new
                {
                    from = new[]
                    {
                        new { collectionId = "active_users" }
                    },
                    where = new
                    {
                        fieldFilter = new
                        {
                            field = new { fieldPath = "date" },
                            op = "EQUAL",
                            value = new { stringValue = dateStr }
                        }
                    },
                    select = new
                    {
                        fields = Array.Empty<string>()
                    }
                }
            };

            var content = new StringContent(JsonConvert.SerializeObject(query), Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                string errMsg = await response.Content.ReadAsStringAsync();
                AppLogger.Log($"Analytics fetch count failed: {response.StatusCode} - {errMsg}");
                return SettingsManager.Current.LastActiveUserCount;
            }

            string json = await response.Content.ReadAsStringAsync();
            int count = ParseQueryResultsCount(json);

            SettingsManager.Current.LastActiveUserCount = count;
            SettingsManager.Current.LastActiveUserFetchTime = DateTime.Now.ToString("g");
            SettingsManager.Save();

            UserCountUpdated?.Invoke(count);
            return count;
        }

        private int ParseQueryResultsCount(string json)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(json)) return 0;
                var arr = JArray.Parse(json);
                int count = 0;
                foreach (var item in arr)
                {
                    // Each item is an object, but if no documents matched it might be empty or contain a single empty result
                    if (item["document"] != null)
                    {
                        count++;
                    }
                }
                return count;
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Error parsing query count results: {ex.Message}");
                return 0;
            }
        }
    }
}
