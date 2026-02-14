using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WildEggsHatching.Json.Models
{
    /// <summary>
    /// Centralized JSON model management for JSON-serializable objects
    /// </summary>
    public static class JsonModelManager
    {
        /// <summary>
        /// Standard JSON serialization options used throughout the application
        /// </summary>
        public static readonly JsonSerializerOptions DefaultOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() },
        };

        /// <summary>
        /// Validates if an object can be properly serialized to JSON
        /// </summary>
        /// <typeparam name="T">Type of object to validate</typeparam>
        /// <param name="obj">Object to validate</param>
        /// <returns>True if object can be serialized, false otherwise</returns>
        public static bool ValidateSerialization<T>(T obj)
            where T : class
        {
            try
            {
                JsonSerializer.Serialize(obj, DefaultOptions);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Creates a deep clone of an object using JSON serialization
        /// </summary>
        /// <typeparam name="T">Type of object to clone</typeparam>
        /// <param name="obj">Object to clone</param>
        /// <returns>Deep cloned object</returns>
        public static T? DeepClone<T>(T obj)
            where T : class
        {
            var json = JsonSerializer.Serialize(obj, DefaultOptions);
            return JsonSerializer.Deserialize<T>(json, DefaultOptions);
        }
    }
}
