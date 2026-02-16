using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using WildEggsHatching.DesertTomb.Configuration;
using static WildEggsHatching.ModEntry;

namespace WildEggsHatching.DesertTomb.Configuration
{
    /// <summary>
    /// Handles loading and saving configuration from JSON files using SMAPI's data helper
    /// </summary>
    public static class ConfigurationLoader
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() },
        };

        /// <summary>
        /// Loads configuration from a JSON file using SMAPI's data helper
        /// </summary>
        /// <param name="filePath">Path to the JSON configuration file relative to mod folder</param>
        /// <returns>Loaded configuration or default if file doesn't exist</returns>
        public static SpawnConfiguration LoadConfiguration(string filePath)
        {
            try
            {
                var config = HelperInstance.Data.ReadJsonFile<SpawnConfiguration>(filePath);
                return config ?? SpawnConfiguration.Default;
            }
            catch (Exception ex)
            {
                ModEntry.LoggerHelper.Log(
                    $"Failed to load configuration from {filePath}: {ex.Message}",
                    StardewModdingAPI.LogLevel.Error
                );
                return SpawnConfiguration.Default;
            }
        }

        /// <summary>
        /// Saves configuration to a JSON file using SMAPI's data helper
        /// </summary>
        /// <param name="config">Configuration to save</param>
        /// <param name="filePath">Path to save the JSON file relative to mod folder</param>
        public static void SaveConfiguration(SpawnConfiguration config, string filePath)
        {
            try
            {
                HelperInstance.Data.WriteJsonFile(filePath, config);
            }
            catch (Exception ex)
            {
                ModEntry.LoggerHelper.Log(
                    $"Failed to save configuration to {filePath}: {ex.Message}",
                    StardewModdingAPI.LogLevel.Error
                );
            }
        }

        /// <summary>
        /// Creates a default configuration file if it doesn't exist using SMAPI's data helper
        /// </summary>
        /// <param name="filePath">Path to create the default configuration file relative to mod folder</param>
        public static void CreateDefaultConfiguration(string filePath)
        {
            var existingConfig = HelperInstance.Data.ReadJsonFile<SpawnConfiguration>(filePath);
            if (existingConfig != null)
            {
                return;
            }

            SaveConfiguration(SpawnConfiguration.Default, filePath);
        }

        /// <summary>
        /// Validates a configuration file and returns any issues using SMAPI's data helper
        /// </summary>
        /// <param name="filePath">Path to the configuration file relative to mod folder</param>
        /// <returns>Validation result with any issues found</returns>
        public static ConfigurationValidationResult ValidateConfiguration(string filePath)
        {
            try
            {
                var config = HelperInstance.Data.ReadJsonFile<SpawnConfiguration>(filePath);

                if (config == null)
                {
                    return new ConfigurationValidationResult(
                        false,
                        "Configuration file does not exist or is empty"
                    );
                }

                return new ConfigurationValidationResult(true, "Configuration is valid");
            }
            catch (InvalidOperationException ex)
            {
                return new ConfigurationValidationResult(
                    false,
                    $"JSON parsing error: {ex.Message}"
                );
            }
            catch (Exception ex)
            {
                return new ConfigurationValidationResult(false, $"Validation error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Result of configuration validation
    /// </summary>
    public class ConfigurationValidationResult
    {
        public bool IsValid { get; }
        public string Message { get; }

        public ConfigurationValidationResult(bool isValid, string message)
        {
            IsValid = isValid;
            Message = message;
        }
    }
}
