using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using StardewModdingAPI;
using Mikew.NestCore.Helpers.Logging;

namespace Mikew.NestCore.Helpers
{
    /// <summary>
    /// Enhanced translation helper with caching, thread safety, and convenience methods.
    /// </summary>
    public class I18TranslationHelper
    {
        public sealed record MissingValueEntry(string Key, string Issue);

        public sealed record ModI18nMissingValueReport(
            string ModName,
            string UniqueId,
            string ModDirectory,
            IReadOnlyList<MissingValueEntry> MissingValues);

        private readonly IModHelper _helper;
        private readonly ConcurrentDictionary<string, string> _cache = new();
        private readonly ConcurrentDictionary<string, string> _usedKeys = new();
        private readonly ConcurrentDictionary<string, int> _pluralCounts = new();
        private const string EnI18n = "i18n/default.json";

        // Regex to find {0}, {1}, etc. placeholders
        private static readonly Regex TokenPattern = new(@"\{(\d+)\}", RegexOptions.Compiled);
        // Regex to find {{key}} nested keys
        private static readonly Regex NestedKeyPattern = new(@"\{\{(\w+)\}\}", RegexOptions.Compiled);
        // Regexes for static GMCM ids in code (cross-mod i18n scan).
        private static readonly Regex GmcmOptionIdPattern = new(@"optionId\s*:\s*""([^""]+)""", RegexOptions.Compiled);
        private static readonly Regex GmcmPagePattern = new(@"layout\.Page\(\s*""([^""]+)""", RegexOptions.Compiled);
        private static readonly Regex GmcmLinkPattern = new(@"\.Link\(\s*""([^""]+)""", RegexOptions.Compiled);
        private static readonly Regex GmcmSectionPattern = new(@"(?:^|[\s.;])page\.Section\(\s*""([^""]+)""", RegexOptions.Compiled);

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
        /// Gets a translation by key with named tokens.
        /// </summary>
        public Translation Get(string key, object? tokens)
        {
            RecordKey(key);
            return _helper.Translation.Get(key, tokens);
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
        /// Gets a translation with named token interpolation.
        /// </summary>
        public string GetString(string key, object? tokens)
        {
            RecordKey(key);
            var translation = _helper.Translation.Get(key, tokens);
            return translation.ToString();
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
            
            if (_cache.TryGetValue(key, out var cached) && cached is not null)
            {
                result = cached;
                return true;
            }

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

        #region Cross-Mod Missing-Value Scan

        /// <summary>
        /// Scans sibling mod folders for NestCore-dependent mods and reports i18n keys
        /// that are missing or have empty/non-string values.
        /// </summary>
        public static IReadOnlyList<ModI18nMissingValueReport> ScanNestCoreDependentModsForMissingValues(string nestCoreDirectoryPath)
        {
            var reports = new List<ModI18nMissingValueReport>();
            if (string.IsNullOrWhiteSpace(nestCoreDirectoryPath) || !Directory.Exists(nestCoreDirectoryPath))
                return reports;

            var modsRoot = Directory.GetParent(nestCoreDirectoryPath)?.FullName;
            if (string.IsNullOrWhiteSpace(modsRoot) || !Directory.Exists(modsRoot))
                return reports;

            foreach (var modDir in Directory.EnumerateDirectories(modsRoot))
            {
                var manifestPath = Path.Combine(modDir, "manifest.json");
                if (!File.Exists(manifestPath))
                    continue;

                if (!TryReadManifest(manifestPath, out var modName, out var uniqueId, out var usesNestCore))
                    continue;
                if (!usesNestCore)
                    continue;

                var missing = FindMissingValuesForMod(modDir);
                reports.Add(new ModI18nMissingValueReport(modName, uniqueId, modDir, missing));
            }

            return reports
                .OrderBy(r => r.ModName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static bool TryReadManifest(
            string manifestPath,
            out string modName,
            out string uniqueId,
            out bool usesNestCore)
        {
            modName = Path.GetFileName(Path.GetDirectoryName(manifestPath) ?? manifestPath);
            uniqueId = string.Empty;
            usesNestCore = false;

            try
            {
                using var stream = File.OpenRead(manifestPath);
                using var doc = JsonDocument.Parse(stream);
                var root = doc.RootElement;

                if (root.TryGetProperty("Name", out var nameProp) && nameProp.ValueKind == JsonValueKind.String)
                    modName = nameProp.GetString() ?? modName;

                if (root.TryGetProperty("UniqueID", out var uniqueIdProp) && uniqueIdProp.ValueKind == JsonValueKind.String)
                    uniqueId = uniqueIdProp.GetString() ?? string.Empty;

                if (string.Equals(uniqueId, "Mikew.nestcore", StringComparison.OrdinalIgnoreCase))
                    usesNestCore = true;

                if (!usesNestCore
                    && root.TryGetProperty("Dependencies", out var depsProp)
                    && depsProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var dep in depsProp.EnumerateArray())
                    {
                        if (dep.ValueKind != JsonValueKind.Object)
                            continue;
                        if (!dep.TryGetProperty("UniqueID", out var depIdProp) || depIdProp.ValueKind != JsonValueKind.String)
                            continue;

                        var depId = depIdProp.GetString() ?? string.Empty;
                        if (string.Equals(depId, "Mikew.nestcore", StringComparison.OrdinalIgnoreCase))
                        {
                            usesNestCore = true;
                            break;
                        }
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static IReadOnlyList<MissingValueEntry> FindMissingValuesForMod(string modDir)
        {
            var issues = new List<MissingValueEntry>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var i18nDir = Path.Combine(modDir, "i18n");

            if (!Directory.Exists(i18nDir))
            {
                AddIssue(issues, seen, "<i18n>", "missing i18n folder");
                return issues;
            }

            var defaultPath = Path.Combine(i18nDir, "default.json");
            var defaultMap = TryReadI18nMap(defaultPath, out var defaultIssues);
            foreach (var issue in defaultIssues)
                AddIssue(issues, seen, issue.Key, issue.Issue);

            if (!File.Exists(defaultPath))
                AddIssue(issues, seen, "<default.json>", "missing default locale file");

            var defaultKeys = defaultMap.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var referencedKey in CollectReferencedGmcmI18nKeys(modDir))
            {
                if (!defaultKeys.Contains(referencedKey))
                    AddIssue(issues, seen, referencedKey, "missing key in default.json (referenced by GMCM code)");
            }

            foreach (var filePath in Directory.EnumerateFiles(i18nDir, "*.json"))
            {
                var fileName = Path.GetFileName(filePath);
                var map = TryReadI18nMap(filePath, out var fileIssues);
                foreach (var issue in fileIssues)
                    AddIssue(issues, seen, issue.Key, issue.Issue);

                if (string.Equals(fileName, "default.json", StringComparison.OrdinalIgnoreCase))
                    continue;

                foreach (var key in defaultKeys)
                {
                    if (!map.ContainsKey(key))
                        AddIssue(issues, seen, key, $"missing key in {fileName}");
                }
            }

            return issues
                .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Issue, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static Dictionary<string, string> TryReadI18nMap(string filePath, out List<MissingValueEntry> issues)
        {
            issues = new List<MissingValueEntry>();
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var fileName = Path.GetFileName(filePath);

            if (!File.Exists(filePath))
                return map;

            try
            {
                using var stream = File.OpenRead(filePath);
                using var doc = JsonDocument.Parse(stream);
                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                {
                    issues.Add(new MissingValueEntry($"<{fileName}>", $"invalid JSON root (expected object) in {fileName}"));
                    return map;
                }

                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.String)
                    {
                        var value = prop.Value.GetString() ?? string.Empty;
                        map[prop.Name] = value;

                        if (string.IsNullOrWhiteSpace(value))
                            issues.Add(new MissingValueEntry(prop.Name, $"empty value in {fileName}"));
                    }
                    else if (prop.Value.ValueKind == JsonValueKind.Null)
                    {
                        issues.Add(new MissingValueEntry(prop.Name, $"null value in {fileName}"));
                    }
                    else
                    {
                        issues.Add(new MissingValueEntry(prop.Name, $"non-string value in {fileName}"));
                    }
                }
            }
            catch
            {
                issues.Add(new MissingValueEntry($"<{fileName}>", $"invalid JSON in {fileName}"));
            }

            return map;
        }

        private static void AddIssue(
            List<MissingValueEntry> issues,
            HashSet<string> seen,
            string key,
            string issue)
        {
            var dedupeKey = $"{key}\n{issue}";
            if (!seen.Add(dedupeKey))
                return;

            issues.Add(new MissingValueEntry(key, issue));
        }

        private static IReadOnlyCollection<string> CollectReferencedGmcmI18nKeys(string modDir)
        {
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            IEnumerable<string> files;

            try
            {
                files = Directory.EnumerateFiles(modDir, "*.cs", SearchOption.AllDirectories);
            }
            catch
            {
                return keys;
            }

            foreach (var file in files)
            {
                string text;
                try
                {
                    text = File.ReadAllText(file);
                }
                catch
                {
                    continue;
                }

                foreach (Match match in GmcmOptionIdPattern.Matches(text))
                {
                    var id = match.Groups[1].Value;
                    if (string.IsNullOrWhiteSpace(id))
                        continue;

                    keys.Add($"config.{id}.name");
                    keys.Add($"config.{id}.description");
                }

                foreach (Match match in GmcmPagePattern.Matches(text))
                {
                    var pageId = match.Groups[1].Value;
                    if (string.IsNullOrWhiteSpace(pageId))
                        continue;

                    keys.Add($"config.page.{pageId}.name");
                    keys.Add($"config.page.{pageId}.description");
                }

                foreach (Match match in GmcmLinkPattern.Matches(text))
                {
                    var pageId = match.Groups[1].Value;
                    if (string.IsNullOrWhiteSpace(pageId))
                        continue;

                    keys.Add($"config.page.{pageId}.name");
                }

                foreach (Match match in GmcmSectionPattern.Matches(text))
                {
                    var sectionId = match.Groups[1].Value;
                    if (string.IsNullOrWhiteSpace(sectionId))
                        continue;

                    keys.Add($"config.section.{sectionId}.text");
                    keys.Add($"config.section.{sectionId}.description");
                }
            }

            return keys;
        }

        #endregion
    }
}
