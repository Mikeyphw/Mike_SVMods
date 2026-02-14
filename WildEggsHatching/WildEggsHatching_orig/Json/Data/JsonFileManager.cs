using System;
using System.Collections.Generic;
using StardewModdingAPI;
using static WildEggsHatching.ModEntry;

namespace WildEggsHatching.Json.Data
{
    /// <summary>
    /// Centralized JSON file management using SMAPI's data helper
    /// </summary>
    public static class JsonFileManager
    {
        /// <summary>
        /// Reads a JSON file from the mod's data folder
        /// </summary>
        /// <typeparam name="T">Type to deserialize to</typeparam>
        /// <param name="filePath">Relative path to the JSON file</param>
        /// <returns>Deserialized object or default(T) if file doesn't exist</returns>
        public static T ReadJsonFile<T>(string filePath)
            where T : class
        {
            try
            {
                return HelperInstance.Data.ReadJsonFile<T>(filePath);
            }
            catch (Exception ex)
            {
                ModEntry.LoggerHelper.Log(
                    $"Failed to read JSON file {filePath}: {ex.Message}",
                    LogLevel.Error
                );
                return null;
            }
        }

        /// <summary>
        /// Writes an object to a JSON file in the mod's data folder
        /// </summary>
        /// <typeparam name="T">Type of object to serialize</typeparam>
        /// <param name="filePath">Relative path to the JSON file</param>
        /// <param name="data">Object to serialize</param>
        public static void WriteJsonFile<T>(string filePath, T data)
            where T : class
        {
            try
            {
                HelperInstance.Data.WriteJsonFile(filePath, data);
            }
            catch (Exception ex)
            {
                ModEntry.LoggerHelper.Log(
                    $"Failed to write JSON file {filePath}: {ex.Message}",
                    LogLevel.Error
                );
            }
        }

        /// <summary>
        /// Creates a default JSON file if it doesn't exist
        /// </summary>
        /// <typeparam name="T">Type of default object</typeparam>
        /// <param name="filePath">Relative path to the JSON file</param>
        /// <param name="defaultData">Default data to write</param>
        public static void CreateDefaultJsonFile<T>(string filePath, T defaultData)
            where T : class
        {
            var existing = ReadJsonFile<T>(filePath);
            if (existing == null)
            {
                WriteJsonFile(filePath, defaultData);
            }
        }

        /// <summary>
        /// Checks if a JSON file exists
        /// </summary>
        /// <param name="filePath">Relative path to the JSON file</param>
        /// <returns>True if file exists, false otherwise</returns>
        public static bool FileExists(string filePath)
        {
            return ReadJsonFile<object>(filePath) != null;
        }
    }
}
