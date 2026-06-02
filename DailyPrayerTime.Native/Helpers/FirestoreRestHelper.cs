using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using DailyPrayerTime.Native.Services;

namespace DailyPrayerTime.Native.Helpers
{
    public static class FirestoreRestHelper
    {
        private static readonly HttpClient _http = new();

        private static string BaseUrl => FirebaseConfig.FirestoreBaseUrl;

        private static async Task<string?> GetTokenAsync()
        {
            return await AuthService.Instance.GetIdTokenAsync();
        }

        private static string GetUrl(string collection, string? documentId = null)
        {
            string baseUrl = BaseUrl;
            bool isGlobal = collection == "leaderboard" || collection == "hall_of_fame" || collection == "contact_messages" || collection == "mail";

            if (isGlobal)
            {
                return string.IsNullOrEmpty(documentId)
                    ? $"{baseUrl}/{collection}"
                    : $"{baseUrl}/{collection}/{documentId}";
            }
            else
            {
                string uid = SettingsManager.Current.FirebaseUid ?? "";
                return string.IsNullOrEmpty(documentId)
                    ? $"{baseUrl}/users/{uid}/{collection}"
                    : $"{baseUrl}/users/{uid}/{collection}/{documentId}";
            }
        }

        public static async Task<Dictionary<string, object>?> GetDocumentAsync(string collection, string documentId)
        {
            var token = await GetTokenAsync();
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(SettingsManager.Current.FirebaseUid))
                return null;

            string url = GetUrl(collection, documentId);

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Bearer {token}");

            var response = await _http.SendAsync(request);
            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            if (!response.IsSuccessStatusCode)
            {
                var errMsg = await response.Content.ReadAsStringAsync();
                string errorLog = $"Firestore read failed: {response.StatusCode} - {errMsg}";
                AppLogger.Log(errorLog);
                throw new HttpRequestException(errorLog);
            }

            var json = await response.Content.ReadAsStringAsync();
            return ParseFirestoreDocument(json);
        }

        public static async Task<List<(string Id, Dictionary<string, object> Data)>> GetCollectionAsync(string collection)
        {
            var token = await GetTokenAsync();
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(SettingsManager.Current.FirebaseUid))
                return new List<(string, Dictionary<string, object>)>();

            string url = GetUrl(collection);

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Bearer {token}");

            var response = await _http.SendAsync(request);
            if (response.StatusCode == HttpStatusCode.NotFound)
                return new List<(string, Dictionary<string, object>)>();

            if (!response.IsSuccessStatusCode)
            {
                var errMsg = await response.Content.ReadAsStringAsync();
                string errorLog = $"Firestore collection query failed: {response.StatusCode} - {errMsg}";
                AppLogger.Log(errorLog);
                throw new HttpRequestException(errorLog);
            }

            var json = await response.Content.ReadAsStringAsync();
            return ParseFirestoreCollection(json);
        }

        public static async Task<bool> SetDocumentAsync(string collection, string documentId, object data)
        {
            var token = await GetTokenAsync();
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(SettingsManager.Current.FirebaseUid))
                return false;

            string url = GetUrl(collection, documentId);

            var firestoreData = ConvertToFirestoreFields(data);
            var body = new { fields = firestoreData };
            var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Patch, url);
            request.Headers.Add("Authorization", $"Bearer {token}");
            request.Content = content;

            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errMsg = await response.Content.ReadAsStringAsync();
                string errorLog = $"Firestore write failed: {response.StatusCode} - {errMsg}";
                AppLogger.Log(errorLog);
                throw new HttpRequestException(errorLog);
            }
            return true;
        }

        public static async Task<bool> DeleteDocumentAsync(string collection, string documentId)
        {
            var token = await GetTokenAsync();
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(SettingsManager.Current.FirebaseUid))
                return false;

            string url = GetUrl(collection, documentId);

            var request = new HttpRequestMessage(HttpMethod.Delete, url);
            request.Headers.Add("Authorization", $"Bearer {token}");

            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errMsg = await response.Content.ReadAsStringAsync();
                string errorLog = $"Firestore delete failed: {response.StatusCode} - {errMsg}";
                AppLogger.Log(errorLog);
                throw new HttpRequestException(errorLog);
            }
            return true;
        }

        private static Dictionary<string, object> ConvertToFirestoreFields(object obj)
        {
            var json = JsonConvert.SerializeObject(obj);
            var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json) ?? new();
            var fields = new Dictionary<string, object>();

            foreach (var kvp in dict)
            {
                fields[kvp.Key] = ConvertToFirestoreValue(kvp.Value);
            }
            return fields;
        }

        private static object ConvertToFirestoreValue(object? value)
        {
            if (value == null)
                return new { nullValue = (object?)null };

            if (value is JToken token)
            {
                if (token.Type == JTokenType.Null)
                    return new { nullValue = (object?)null };
                if (token.Type == JTokenType.Boolean)
                    return new { booleanValue = (bool)token };
                if (token.Type == JTokenType.Integer)
                    return new { integerValue = (long)token };
                if (token.Type == JTokenType.Float)
                    return new { doubleValue = (double)token };
                if (token.Type == JTokenType.String)
                    return new { stringValue = token.ToString() };
                if (token.Type == JTokenType.Date)
                    return new { stringValue = ((DateTime)token).ToString("o") };
                if (token.Type == JTokenType.Array)
                {
                    var values = new List<object>();
                    foreach (var item in token)
                        values.Add(ConvertToFirestoreValue(item));
                    return new { arrayValue = new { values } };
                }
                if (token.Type == JTokenType.Object)
                {
                    var nestedFields = new Dictionary<string, object>();
                    foreach (var prop in ((JObject)token).Properties())
                    {
                        nestedFields[prop.Name] = ConvertToFirestoreValue(prop.Value);
                    }
                    return new { mapValue = new { fields = nestedFields } };
                }
            }

            if (value is bool b)
                return new { booleanValue = b };
            if (value is int i)
                return new { integerValue = i };
            if (value is long l)
                return new { integerValue = l };
            if (value is double d)
                return new { doubleValue = d };
            if (value is string s)
                return new { stringValue = s };
            if (value is DateTime dt)
                return new { stringValue = dt.ToString("o") };

            // For arrays/lists
            if (value is System.Collections.IList list)
            {
                var values = new List<object>();
                foreach (var item in list)
                    values.Add(ConvertToFirestoreValue(item));
                return new { arrayValue = new { values } };
            }

            // For dictionaries/objects
            var nested = ConvertToFirestoreFields(value);
            return new { mapValue = new { fields = nested } };
        }

        private static Dictionary<string, object> ParseFirestoreDocument(string json)
        {
            var result = new Dictionary<string, object>();
            var doc = JObject.Parse(json);
            if (doc["fields"] is JObject fields)
            {
                foreach (var prop in fields.Properties())
                {
                    result[prop.Name] = ParseFirestoreValue(prop.Value);
                }
            }
            return result;
        }

        private static List<(string Id, Dictionary<string, object> Data)> ParseFirestoreCollection(string json)
        {
            var results = new List<(string, Dictionary<string, object>)>();
            try
            {
                if (string.IsNullOrWhiteSpace(json)) return results;
                var root = JObject.Parse(json);
                if (root["documents"] is JArray jArray)
                {
                    foreach (var doc in jArray)
                    {
                        var name = doc["name"]?.ToString() ?? "";
                        var docId = name.Contains('/') ? name.Split('/')[^1] : name;
                        var data = new Dictionary<string, object>();
                        if (doc["fields"] is JObject fields)
                        {
                            foreach (var prop in fields.Properties())
                            {
                                data[prop.Name] = ParseFirestoreValue(prop.Value);
                            }
                        }
                        results.Add((docId, data));
                    }
                }
            }
            catch (Exception ex)
            {
                string errorLog = $"Error parsing firestore collection: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(errorLog);
                AppLogger.Log(errorLog + Environment.NewLine + ex.StackTrace);
            }
            return results;
        }

        private static object ParseFirestoreValue(JToken token)
        {
            if (token["stringValue"] != null) return token["stringValue"]!.ToString();
            if (token["integerValue"] != null) return int.Parse(token["integerValue"]!.ToString());
            if (token["doubleValue"] != null) return double.Parse(token["doubleValue"]!.ToString());
            if (token["booleanValue"] != null) return (bool)token["booleanValue"]!;
            if (token["arrayValue"]?["values"] is JArray arr)
            {
                var list = new List<object>();
                foreach (var item in arr)
                    list.Add(ParseFirestoreValue(item));
                return list;
            }
            if (token["mapValue"]?["fields"] is JObject mapFields)
            {
                var dict = new Dictionary<string, object>();
                foreach (var prop in mapFields.Properties())
                    dict[prop.Name] = ParseFirestoreValue(prop.Value);
                return dict;
            }
            return "";
        }
    }
}
