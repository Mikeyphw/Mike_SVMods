using System;
using System.Collections.Generic;
using StardewModdingAPI;

namespace Mikew.NestCore.Helpers.Logging;

public static class ScopedLogCatalog
{
    // Mod identifiers (unchanged)
    public const string ModNestCore = "NestCore";
    public const string ModRoosterControl = "RoosterControl";
    public const string ModWe = "WE";
    public const string ModLthte = "LTHTE";

    public const string CategoryGeneral = "general";

    // Level constants (unchanged)
    public const string LevelAll = "all";
    public const string LevelTrace = "trace";
    public const string LevelDebug = "debug";
    public const string LevelInfo = "info";
    public const string LevelWarn = "warn";
    public const string LevelError = "error";
    public const string LevelNone = "none";

    #region Category Enums

    /// <summary>Categories for Wild Eggs (WE) / EggsInWild mod.</summary>
    public enum ModWeCategory
    {
        General,
        Progression,
        Spawning,
        AnimalNotes,
        DesertTomb,
        Content,
        Verify,
        Commands,
        SaveData,
        Integrations,
    }

    /// <summary>Categories for RoosterControl mod.</summary>
    public enum ModRoosterControlCategory
    {
        General,
        Catalogs,
        Hatching,
        Planning,
        FarmAnimals,
        Assets,
        Gmcm,
        Commands,
        SaveData,
        Integrations,
    }

    /// <summary>Categories for LetMeHatch (LTHTE) mod.</summary>
    public enum ModLthteCategory
    {
        General,
        Cloning,
        Breeding,
        Assets,
        ContentPipeline,
        Gmcm,
        Commands,
        Integrations,
        Runtime,
    }

    /// <summary>Categories for NestCore mod.</summary>
    public enum ModNestCoreCategory
    {
        General,
        Gmcm,
        Config,
        Content,
        SaveData,
        Integrations,
        Runtime,
    }

    #endregion

    #region Enum to String Conversion

    /// <summary>Converts a ModWeCategory enum value to its string representation.</summary>
    public static string ToString(ModWeCategory category) => category switch
    {
        ModWeCategory.General => CategoryGeneral,
        ModWeCategory.Progression => "progression",
        ModWeCategory.Spawning => "spawning",
        ModWeCategory.AnimalNotes => "animal-notes",
        ModWeCategory.DesertTomb => "desert-tomb",
        ModWeCategory.Content => "content",
        ModWeCategory.Verify => "verify",
        ModWeCategory.Commands => "commands",
        ModWeCategory.SaveData => "save-data",
        ModWeCategory.Integrations => "integrations",
        _ => CategoryGeneral,
    };

    /// <summary>Converts a ModRoosterControlCategory enum value to its string representation.</summary>
    public static string ToString(ModRoosterControlCategory category) => category switch
    {
        ModRoosterControlCategory.General => CategoryGeneral,
        ModRoosterControlCategory.Catalogs => "catalogs",
        ModRoosterControlCategory.Hatching => "hatching",
        ModRoosterControlCategory.Planning => "planning",
        ModRoosterControlCategory.FarmAnimals => "farmanimals",
        ModRoosterControlCategory.Assets => "assets",
        ModRoosterControlCategory.Gmcm => "gmcm",
        ModRoosterControlCategory.Commands => "commands",
        ModRoosterControlCategory.SaveData => "save-data",
        ModRoosterControlCategory.Integrations => "integrations",
        _ => CategoryGeneral,
    };

    /// <summary>Converts a ModLthteCategory enum value to its string representation.</summary>
    public static string ToString(ModLthteCategory category) => category switch
    {
        ModLthteCategory.General => CategoryGeneral,
        ModLthteCategory.Cloning => "cloning",
        ModLthteCategory.Breeding => "breeding",
        ModLthteCategory.Assets => "assets",
        ModLthteCategory.ContentPipeline => "content-pipeline",
        ModLthteCategory.Gmcm => "gmcm",
        ModLthteCategory.Commands => "commands",
        ModLthteCategory.Integrations => "integrations",
        ModLthteCategory.Runtime => "runtime",
        _ => CategoryGeneral,
    };

    /// <summary>Converts a ModNestCoreCategory enum value to its string representation.</summary>
    public static string ToString(ModNestCoreCategory category) => category switch
    {
        ModNestCoreCategory.General => CategoryGeneral,
        ModNestCoreCategory.Gmcm => "gmcm",
        ModNestCoreCategory.Config => "config",
        ModNestCoreCategory.Content => "content",
        ModNestCoreCategory.SaveData => "save-data",
        ModNestCoreCategory.Integrations => "integrations",
        ModNestCoreCategory.Runtime => "runtime",
        _ => CategoryGeneral,
    };

    #endregion

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
