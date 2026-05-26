using System;
using System.Text.Json.Serialization;

namespace DailyPrayerTime.Native
{
    public class NoticeApiResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("language")]
        public string Language { get; set; }

        [JsonPropertyName("notice")]
        public NoticeData Notice { get; set; }

        [JsonPropertyName("statistics")]
        public NoticeStatistics Statistics { get; set; }
    }

    public class NoticeData
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("notification_message")]
        public string NotificationMessage { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("start_date")]
        public string StartDateString { get; set; }

        [JsonPropertyName("expiry_date")]
        public string ExpiryDateString { get; set; }

        [JsonIgnore]
        public DateTime? StartDate
        {
            get
            {
                if (DateTime.TryParse(StartDateString, out var date))
                    return date;
                return null;
            }
        }

        [JsonIgnore]
        public DateTime? ExpiryDate
        {
            get
            {
                if (DateTime.TryParse(ExpiryDateString, out var date))
                    return date;
                return null;
            }
        }
    }

    public class NoticeStatistics
    {
        [JsonPropertyName("total_api_calls")]
        public int TotalApiCalls { get; set; }

        [JsonPropertyName("unique_ip_count")]
        public int UniqueIpCount { get; set; }
    }
}
