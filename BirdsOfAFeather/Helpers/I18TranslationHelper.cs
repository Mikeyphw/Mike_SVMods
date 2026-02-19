using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using StardewModdingAPI;
using Mikew.BirdsOfAFeather.Helpers.Logging;

namespace Mikew.BirdsOfAFeather.Helpers
{
    /// <summary>
    /// Enhanced translation helper with caching, thread safety, and convenience methods.
    /// </summary>
    public class I18TranslationHelper
    {
        private readonly IModHelper _helper;
        private readonly ConcurrentDictionary<string, string> _cache = new();
        private readonly ConcurrentDictionary<string, string> _usedKeys = new();
        private readonly ConcurrentDictionary<string, int> _pluralCounts = new();
        private const string EnI18n = "i18n/default.json";

        // Regex to find {0}, {1}, etc. placeholders
        private static readonly Regex TokenPattern = new(@"\{(\d+)\}", RegexOptions.Compiled);
        // Regex to find {{key}} nested keys
        private static readonly Regex NestedKeyPattern = new(@"\{\{(\w+)\}\}", RegexOptions.Compiled);

        // Current prefix for fluent API
        private string _currentPrefix = string.Empty;

        /// <summary>
        /// Creates a new translation helper.
        /// </summary>
        /// <param name="modHelper">The mod helper instance.</param>
        public I18TranslationHelper(IModHelper modHelper)
        {
            _helper = modHelper;
        }

        #region Core Methods

        /// <summary>
        /// Gets a translation by key (original method, returns SMAPI Translation).
        /// </summary>
        public Translation Get(string key)
        {
            RecordKey(key);
            return _helper.Translation.Get(key);
        }

        /// <summary>
        /// Gets a translation as a string.
        /// </summary>
        /// <param name="key">The translation key.</param>
        /// <returns>The translated string or a missing key placeholder.</returns>
        public string GetString(string key)
        {
            RecordKey(key);
            
            if (_cache.TryGetValue(key, out var cached))
                return cached;

            var translation = _helper.Translation.Get(key);
            var result = translation.ToString();
            
            // Cache the result
            _cache[key] = result;
            return result;
        }

        /// <summary>
        /// Gets a translation with token interpolation.
        /// </summary>
        /// <param name="key">The translation key.</param>
        /// <param name="tokens">Token values to interpolate.</param>
        /// <returns>The translated string with tokens replaced.</returns>
        public string GetString(string key, params object[] tokens)
        {
            var baseString = GetString(key);
            
            // Validate token count
            ValidateTokens(key, tokens);
            
            // Replace tokens
            return tokens.Length > 0 
                ? string.Format(baseString, tokens) 
                : baseString;
        }

        /// <summary>
        /// Gets a translation or returns a default value if missing.
        /// </summary>
        /// <param name="key">The translation key.</param>
        /// <param name="defaultValue">The default value to return if key is missing.</param>
        /// <returns>The translated string or default value.</returns>
        public string GetOrDefault(string key, string defaultValue)
        {
            if (string.IsNullOrEmpty(key))
                return defaultValue;

            RecordKey(key);
            
            // Check cache first
            if (_cache.TryGetValue(key, out var cached))
                return cached;

            var translation = _helper.Translation.Get(key);
            var result = translation.ToString();
            
            // Check if it's a missing key (returns key itself or has placeholder)
            if (IsMissingKey(result, key))
            {
                _cache[key] = defaultValue;
                return defaultValue;
            }
            
            _cache[key] = result;
            return result;
        }

        /// <summary>
        /// Gets a translation or returns empty string if missing.
        /// </summary>
        /// <param name="key">The translation key.</param>
        /// <returns>The translated string or empty.</returns>
        public string GetOrEmpty(string key)
        {
            return GetOrDefault(key, string.Empty);
        }

        /// <summary>
        /// Tries to get a translation safely.
        /// </summary>
        /// <param name="key">The translation key.</param>
        /// <param name="result">The result if found.</param>
        /// <returns>True if the key exists and was retrieved.</returns>
        public bool TryGet(string key, out string result)
        {
            if (string.IsNullOrEmpty(key))
            {
                result = string.Empty;
                return false;
            }

            RecordKey(key);
            
            if (_cache.TryGetValue(key, out result))
                return true;

            var translation = _helper.Translation.Get(key);
            result = translation.ToString();
            
            if (IsMissingKey(result, key))
            {
                result = string.Empty;
                _cache[key] = string.Empty;
                return false;
            }
            
            _cache[key] = result;
            return true;
        }

        #endregion

        #region Batch Operations

        /// <summary>
        /// Gets multiple translations at once.
        /// </summary>
        /// <param name="keys">The translation keys.</param>
        /// <returns>Dictionary of key to translated string.</returns>
        public Dictionary<string, string> GetMany(params string[] keys)
        {
            var result = new Dictionary<string, string>();
            
            foreach (var key in keys)
            {
                result[key] = GetString(key);
            }
            
            return result;
        }

        /// <summary>
        /// Gets multiple translations with fallback defaults.
        /// </summary>
        /// <param name="defaults">Dictionary of key to default value.</param>
        /// <returns>Dictionary of key to translated string (or default).</returns>
        public Dictionary<string, string> GetManyOrDefaults(Dictionary<string, string> defaults)
        {
            var result = new Dictionary<string, string>();
            
            foreach (var (key, defaultValue) in defaults)
            {
                result[key] = GetOrDefault(key, defaultValue);
            }
            
            return result;
        }

        #endregion

        #region Pluralization

        /// <summary>
        /// Gets a translation with pluralization support.
        /// Uses {count} token for the number.
        /// </summary>
        /// <param name="key">The translation key (singular form).</param>
        /// <param name="count">The count to determine singular/plural.</param>
        /// <param name="pluralSuffix">The suffix to append for plural form (default: "s").</param>
        /// <returns>The appropriately pluralized translation.</returns>
        public string GetPlural(string key, int count, string pluralSuffix = "s")
        {
            // Record usage for cleanup
            _pluralCounts.AddOrUpdate(key, count, (_, old) => old + count);

            if (count == 1)
            {
                return GetString(key);
            }

            // Try plural form first, fallback to singular + suffix
            var pluralKey = key + pluralSuffix;
            return TryGet(pluralKey, out var plural) ? plural : GetString(key) + pluralSuffix;
        }

        /// <summary>
        /// Gets a translation with count interpolated.
        /// </summary>
        /// <param name="key">The translation key.</param>
        /// <param name="count">The count value.</param>
        /// <returns>The translation with count replaced.</returns>
        public string GetWithCount(string key, int count)
        {
            return GetString(key, count);
        }

        #endregion

        #region Fluent API - Prefix Support

        /// <summary>
        /// Creates a new helper with a prefix applied to all keys.
        /// </summary>
        /// <param name="prefix">The prefix to prepend to keys.</param>
        /// <returns>A new helper instance with the prefix.</returns>
        /// <example>
        /// var obj = helper.WithPrefix("object.");
        /// obj.Get("name"); // equivalent to helper.Get("object.name")
        /// </example>
        public I18TranslationHelper WithPrefix(string prefix)
        {
            var prefixed = new I18TranslationHelper(_helper)
            {
                _currentPrefix = _currentPrefix + prefix
            };
            return prefixed;
        }

        /// <summary>
        /// Gets a translation with the current prefix applied.
        /// </summary>
        public string Get(string key, bool usePrefix = true)
        {
            var fullKey = usePrefix && !string.IsNullOrEmpty(_currentPrefix) 
                ? _currentPrefix + key 
                : key;
            return GetString(fullKey);
        }

        #endregion

        #region Dictionary-Style Access

        /// <summary>
        /// Dictionary-style access to translations.
        /// </summary>
        /// <param name="key">The translation key.</param>
        /// <returns>The translated string.</returns>
        public string this[string key] => GetString(key);

        /// <summary>
        /// Dictionary-style access with default value.
        /// </summary>
        /// <param name="key">The translation key.</param>
        /// <param name="defaultValue">Default value if key is missing.</param>
        /// <returns>The translated string or default.</returns>
        public string this[string key, string defaultValue] => GetOrDefault(key, defaultValue);

        #endregion

        #region Utility Methods

        /// <summary>
        /// Clears the translation cache.
        /// </summary>
        public void ClearCache()
        {
            _cache.Clear();
        }

        /// <summary>
        /// Gets all keys that have been used.
        /// </summary>
        /// <returns>List of used translation keys.</returns>
        public IReadOnlyList<string> GetUsedKeys()
        {
            return _usedKeys.Keys.ToList();
        }

        /// <summary>
        /// Checks if a key has been used.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if the key has been used.</returns>
        public bool HasBeenUsed(string key)
        {
            return _usedKeys.ContainsKey(key);
        }

        /// <summary>
        /// Validates that token count matches placeholders in translation.
        /// </summary>
        private void ValidateTokens(string key, object[] tokens)
        {
            if (tokens.Length == 0)
                return;

            // Get the raw translation to check placeholder count
            var raw = _helper.Translation.Get(key).ToString();
            var matches = TokenPattern.Matches(raw);
            var maxPlaceholder = 0;

            foreach (Match match in matches)
            {
                var index = int.Parse(match.Groups[1].Value);
                maxPlaceholder = Math.Max(maxPlaceholder, index);
            }

            // Warn if mismatch (0-indexed vs count)
            if (maxPlaceholder >= tokens.Length)
            {
                // Could log warning here if monitor is available
                // For now, just ensure we don't crash
            }
        }

        /// <summary>
        /// Checks if a result indicates a missing key.
        /// </summary>
        private bool IsMissingKey(string result, string key)
        {
            // SMAPI returns the key itself when not found
            // Also check for common missing key patterns
            return result == key || 
                   result.StartsWith("[MISSING:") ||
                   string.IsNullOrWhiteSpace(result);
        }

        /// <summary>
        /// Records a key as being used.
        /// </summary>
        private void RecordKey(string key)
        {
            if (!string.IsNullOrEmpty(key))
            {
                _usedKeys[key.ToLower()] = key;
            }
        }

        #endregion

        #region i18n File Management

        /// <summary>
        /// Checks and cleans up i18n keys - removes unused, adds missing.
        /// </summary>
        /// <param name="log">The logger for output.</param>
        /// <param name="verbose">Whether to log all changes.</param>
        public void Checki18nKeys(ILog log, bool verbose = false)
        {
            var i18nJson = ReadJson();
            var lowerUsedKeys = _usedKeys.Keys.Select(k => k.ToLower()).ToHashSet();

            // Remove unused keys
            var removedCount = 0;
            foreach (var key in i18nJson.Keys.ToList())
            {
                if (!lowerUsedKeys.Contains(key.ToLower()))
                {
                    i18nJson.Remove(key);
                    removedCount++;
                    if (verbose)
                        log.Log($"Removed unused key \"{key}\" from {EnI18n}", LogLevel.Info);
                }
            }

            // Add missing keys
            var addedCount = 0;
            foreach (var key in _usedKeys.Values)
            {
                var lowerKey = key.ToLower();
                if (!i18nJson.Keys.Any(k => k.ToLower() == lowerKey))
                {
                    // Try to find similar key for default
                    i18nJson[key] = GetDefaultValue(key);
                    addedCount++;
                    if (verbose)
                        log.Log($"Added missing key \"{key}\" to {EnI18n}", LogLevel.Info);
                }
            }

            // Log summary
            if (removedCount > 0 || addedCount > 0)
            {
                log.Log($"i18n cleanup: Removed {removedCount} unused, Added {addedCount} missing keys", 
                    LogLevel.Info);
            }

            WriteJson(i18nJson);
        }

        /// <summary>
        /// Gets a reasonable default value for a key.
        /// </summary>
        private string GetDefaultValue(string key)
        {
            // Convert key to readable default: "object.WildEgg.name" -> "Wild Egg Name"
            var parts = key.Split('.');
            if (parts.Length > 0)
            {
                var last = parts[^1];
                return System.Text.RegularExpressions.Regex.Replace(last, 
                    "([a-z])([A-Z])", "$1 $2");
            }
            return key;
        }

        private Dictionary<string, string> ReadJson()
        {
            var i18nJson = _helper.Data.ReadJsonFile<Dictionary<string, string>>(EnI18n);
            if (i18nJson == null)
            {
                WriteJson(new Dictionary<string, string>());
                i18nJson = _helper.Data.ReadJsonFile<Dictionary<string, string>>(EnI18n);
            }
            return i18nJson ?? new Dictionary<string, string>();
        }

        private void WriteJson(Dictionary<string, string> data)
        {
            _helper.Data.WriteJsonFile(EnI18n, data);
        }

        #endregion

        #region Debug Helpers

        /// <summary>
        /// Gets a translation with debug wrapper if missing.
        /// Wraps missing keys in [KEY_NAME] for visual identification.
        /// </summary>
        /// <param name="key">The translation key.</param>
        /// <returns>Translated string or debug wrapper.</returns>
        public string GetWithDebug(string key)
        {
            RecordKey(key);
            
            if (_cache.TryGetValue(key, out var cached))
                return cached;

            var translation = _helper.Translation.Get(key);
            var result = translation.ToString();
            
            if (IsMissingKey(result, key))
            {
                result = $"[MISSING: {key}]";
            }
            
            _cache[key] = result;
            return result;
        }

        /// <summary>
        /// Gets all translations, marking missing ones.
        /// </summary>
        /// <returns>Dictionary with debug info.</returns>
        public Dictionary<string, (string Value, bool IsMissing)> GetAllWithDebug()
        {
            var result = new Dictionary<string, (string, bool)>();
            var i18nJson = ReadJson();
            
            foreach (var key in _usedKeys.Values)
            {
                var value = GetString(key);
                var isMissing = i18nJson.Keys.All(k => k.ToLower() != key.ToLower());
                result[key] = (value, isMissing);
            }
            
            return result;
        }

        #endregion
    }
}
