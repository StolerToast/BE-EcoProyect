using System;
using System.IO;
using System.Text.Json;

namespace smartbin.Config
{
    public static class AppConfigManager
    {
        private static Config? _configuration;

        public static Config Configuration
        {
            get
            {
                if (_configuration == null)
                {
                    LoadConfiguration();
                }
                return _configuration!;
            }
        }

        private static void LoadConfiguration()
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "config.json");
                string jsonString = File.ReadAllText(configPath);
                _configuration = JsonSerializer.Deserialize<Config>(jsonString);

                if (_configuration == null)
                {
                    throw new Exception("Failed to deserialize configuration");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading configuration: {ex.Message}", ex);
            }
        }
    }
}
