using System;
using System.IO;
using Newtonsoft.Json;
using System.Diagnostics;

namespace DailyPrayerTime.Native
{
    public static class DeskBandDataWriter
    {
        private static readonly string AppDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DailyPrayerTimeNative");
        private static readonly string DataFile = Path.Combine(AppDataFolder, "deskband_data.json");

        public static void Write(DeskBandData data)
        {
            try
            {
                if (!Directory.Exists(AppDataFolder))
                {
                    Directory.CreateDirectory(AppDataFolder);
                }

                string json = JsonConvert.SerializeObject(data);
                // We use a temporary file and Move to ensure atomic write, 
                // preventing the DeskBand from reading a partially written file.
                string tempFile = DataFile + ".tmp";
                File.WriteAllText(tempFile, json);
                
                if (File.Exists(DataFile))
                {
                    File.Delete(DataFile);
                }
                File.Move(tempFile, DataFile);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to write DeskBand data: " + ex.Message);
            }
        }
    }
}
