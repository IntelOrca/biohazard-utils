using System;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace emdui
{
    public class Settings
    {
        public static Settings Default { get; private set; } = new Settings();

        public string BlenderPath { get; set; }
        public bool ShowFloor { get; set; } = true;

        private static string GetDataPath(string fileName)
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var configPath = Path.Combine(appDataPath, "emdui", fileName);
            return configPath;
        }

        private static string GetConfigPath() => GetDataPath("config.json");

        public static void Load()
        {
            try
            {
                var configPath = GetConfigPath();
                if (!File.Exists(configPath))
                    return;

                var json = File.ReadAllText(configPath);
                Default = JsonSerializer.Deserialize<Settings>(json, new JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Failed to open configuration file.",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        public static void Save()
        {
            var configPath = GetConfigPath();
            try
            {
                var configDirectory = Path.GetDirectoryName(configPath);
                Directory.CreateDirectory(configDirectory);
                var json = JsonSerializer.Serialize(Default, new JsonSerializerOptions()
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });
                File.WriteAllText(configPath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Failed to write configuration file.",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
