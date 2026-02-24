using System;
using System.Collections.Generic;
using StardewModdingAPI;

namespace Mikew.NestCore.Helpers.Logging;

public static class ScopedLogCatalog
{
    public const string ModNestCore = "NestCore";
    public const string ModRoosterControl = "RoosterControl";
    public const string ModWe = "WE";
    public const string ModLthte = "LTHTE";

    public const string CategoryGeneral = "general";

    public const string LevelAll = "all";
    public const string LevelTrace = "trace";
    public const string LevelDebug = "debug";
    public const string LevelInfo = "info";
    public const string LevelWarn = "warn";
    public const string LevelError = "error";
    public const string LevelNone = "none";

    public static readonly string[] AllowedLevels =
    {
        LevelAll,
        LevelTrace,
        LevelDebug,
        LevelInfo,
        LevelWarn,
        LevelError,
        LevelNone
    };

    private static readonly IReadOnlyDictionary<string, string[]> CategoriesByMod =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [ModNestCore] = new[]
            {
                CategoryGeneral,
                "gmcm",
                "config",
                "content",
                "save-data",
                "integrations",
                "runtime",
            },
            [ModRoosterControl] = new[]
            {
                CategoryGeneral,
                "catalogs",
                "hatching",
                "planning",
                "farmanimals",
                "assets",
                "gmcm",
                "commands",
                "save-data",
                "integrations",
            },
            [ModWe] = new[]
            {
                CategoryGeneral,
                "progression",
                "spawning",
                "animal-notes",
                "desert-tomb",
                "content",
                "verify",
                "commands",
                "save-data",
                "integrations",
            },
            [ModLthte] = new[]
            {
                CategoryGeneral,
                "cloning",
                "breeding",
                "assets",
                "content-pipeline",
                "gmcm",
                "commands",
                "integrations",
                "runtime",
            },
        };

    private static readonly Dictionary<string, int> LevelRanks = new(StringComparer.OrdinalIgnoreCase)
    {
        [LevelTrace] = 0,
        [LevelDebug] = 1,
        [LevelInfo] = 2,
        [LevelWarn] = 3,
        [LevelError] = 4,
    };

    public static IReadOnlyList<string> OrderedMods { get; } = new[] { ModNestCore, ModRoosterControl, ModWe, ModLthte };

    public static IReadOnlyList<string> GetCategories(string mod)
    {
        var normalized = NormalizeModTag(mod);
        return CategoriesByMod.TryGetValue(normalized, out var categories)
            ? categories
            : new[] { CategoryGeneral };
    }

    public static string NormalizeModTag(string? mod)
    {
        if (string.IsNullOrWhiteSpace(mod))
            return ModNestCore;

        if (mod.Equals(ModNestCore, StringComparison.OrdinalIgnoreCase) || mod.Contains("nestcore", StringComparison.OrdinalIgnoreCase))
            return ModNestCore;

        if (mod.Equals(ModRoosterControl, StringComparison.OrdinalIgnoreCase) || mod.Contains("rooster", StringComparison.OrdinalIgnoreCase))
            return ModRoosterControl;

        if (mod.Equals(ModWe, StringComparison.OrdinalIgnoreCase) || mod.Contains("wildegg", StringComparison.OrdinalIgnoreCase))
            return ModWe;

        if (mod.Equals(ModLthte, StringComparison.OrdinalIgnoreCase) || mod.Contains("letmehatch", StringComparison.OrdinalIgnoreCase) || mod.Contains("lmh", StringComparison.OrdinalIgnoreCase))
            return ModLthte;

        return mod.Trim();
    }

    public static string NormalizeCategory(string? category)
        => string.IsNullOrWhiteSpace(category) ? CategoryGeneral : category.Trim().ToLowerInvariant();

    public static bool IsAllowedLevel(string? level)
    {
        if (string.IsNullOrWhiteSpace(level))
            return false;

        var normalized = level.Trim().ToLowerInvariant();
        foreach (var allowed in AllowedLevels)
        {
            if (string.Equals(allowed, normalized, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    public static string NormalizeLevel(string? level)
        => IsAllowedLevel(level) ? level!.Trim().ToLowerInvariant() : LevelAll;

    public static bool ShouldEmit(string configuredLevel, LogLevel messageLevel)
    {
        var normalized = NormalizeLevel(configuredLevel);
        if (normalized == LevelAll)
            return true;

        if (normalized == LevelNone)
            return false;

        var msgToken = ToLevelToken(messageLevel);
        if (!LevelRanks.TryGetValue(msgToken, out var msgRank))
            return true;

        if (!LevelRanks.TryGetValue(normalized, out var minRank))
            return true;

        return msgRank >= minRank;
    }

    public static string ToLevelToken(LogLevel level)
        => level switch
        {
            LogLevel.Trace => LevelTrace,
            LogLevel.Debug => LevelDebug,
            LogLevel.Info => LevelInfo,
            LogLevel.Warn => LevelWarn,
            LogLevel.Error => LevelError,
            LogLevel.Alert => LevelError,
            _ => LevelInfo,
        };
}
