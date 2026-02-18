using System;
using System.Text.RegularExpressions;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace Mikew.BirdsOfAFeather.Helpers.Content
{
    /// <summary>
    /// Common helpers for matching SMAPI asset requests.
    /// </summary>
    public static class AssetNameMatchers
    {
        public static Func<AssetRequestedEventArgs, bool> ForAsset(string assetName)
        {
            if (string.IsNullOrWhiteSpace(assetName))
                throw new ArgumentException("Asset name is required.", nameof(assetName));

            return e => e.NameWithoutLocale.IsEquivalentTo(assetName);
        }

        public static Func<AssetRequestedEventArgs, bool> ForBaseName(string baseName)
        {
            if (string.IsNullOrWhiteSpace(baseName))
                throw new ArgumentException("Base name is required.", nameof(baseName));

            return e => string.Equals(e.NameWithoutLocale.BaseName, baseName, StringComparison.OrdinalIgnoreCase);
        }

        public static Func<AssetRequestedEventArgs, bool> ForPrefix(string baseNamePrefix)
        {
            if (string.IsNullOrWhiteSpace(baseNamePrefix))
                throw new ArgumentException("Prefix is required.", nameof(baseNamePrefix));

            return e => e.NameWithoutLocale.BaseName.StartsWith(baseNamePrefix, StringComparison.OrdinalIgnoreCase);
        }

        public static Func<AssetRequestedEventArgs, bool> ForRegex(Regex regex)
        {
            if (regex == null)
                throw new ArgumentNullException(nameof(regex));

            return e => regex.IsMatch(e.NameWithoutLocale.BaseName);
        }
    }
}
