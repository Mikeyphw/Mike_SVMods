using System;
using System.Collections.Generic;
using Mikew.NestCore.Helpers.Logging;

namespace Mikew.NestCore.Framework
{
    public class CoreModConfig
    {
        public ScopedLoggingConfig ScopedLogging { get; set; }

        public CoreModConfig()
        {
            ScopedLogging = ScopedLoggingConfig.CreateDefault();
        }

        public bool EnsureScopedLoggingDefaults()
        {
            ScopedLogging ??= ScopedLoggingConfig.CreateDefault();
            return ScopedLogging.EnsureDefaults();
        }
    }

    public sealed class ScopedLoggingConfig
    {
        public ModScopedLoggingConfig NestCore { get; set; } = ModScopedLoggingConfig.CreateDefault(ScopedLogCatalog.ModNestCore);
        public ModScopedLoggingConfig RoosterControl { get; set; } = ModScopedLoggingConfig.CreateDefault(ScopedLogCatalog.ModRoosterControl);
        public ModScopedLoggingConfig WE { get; set; } = ModScopedLoggingConfig.CreateDefault(ScopedLogCatalog.ModWe);
        public ModScopedLoggingConfig LTHTE { get; set; } = ModScopedLoggingConfig.CreateDefault(ScopedLogCatalog.ModLthte);

        public static ScopedLoggingConfig CreateDefault()
        {
            var cfg = new ScopedLoggingConfig();
            cfg.EnsureDefaults();
            return cfg;
        }

        public bool EnsureDefaults()
        {
            var changed = false;

            NestCore ??= ModScopedLoggingConfig.CreateDefault(ScopedLogCatalog.ModNestCore);
            RoosterControl ??= ModScopedLoggingConfig.CreateDefault(ScopedLogCatalog.ModRoosterControl);
            WE ??= ModScopedLoggingConfig.CreateDefault(ScopedLogCatalog.ModWe);
            LTHTE ??= ModScopedLoggingConfig.CreateDefault(ScopedLogCatalog.ModLthte);

            changed |= NestCore.EnsureDefaults(ScopedLogCatalog.ModNestCore);
            changed |= RoosterControl.EnsureDefaults(ScopedLogCatalog.ModRoosterControl);
            changed |= WE.EnsureDefaults(ScopedLogCatalog.ModWe);
            changed |= LTHTE.EnsureDefaults(ScopedLogCatalog.ModLthte);

            return changed;
        }

        public ModScopedLoggingConfig GetByMod(string mod)
        {
            var normalized = ScopedLogCatalog.NormalizeModTag(mod);
            return normalized switch
            {
                ScopedLogCatalog.ModNestCore => NestCore,
                ScopedLogCatalog.ModRoosterControl => RoosterControl,
                ScopedLogCatalog.ModWe => WE,
                ScopedLogCatalog.ModLthte => LTHTE,
                _ => NestCore,
            };
        }
    }

    public sealed class ModScopedLoggingConfig
    {
        public string AllLevel { get; set; } = ScopedLogCatalog.LevelAll;
        public Dictionary<string, string> CategoryLevels { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public static ModScopedLoggingConfig CreateDefault(string mod)
        {
            var cfg = new ModScopedLoggingConfig();
            cfg.EnsureDefaults(mod);
            return cfg;
        }

        public bool EnsureDefaults(string mod)
        {
            var changed = false;

            if (!ScopedLogCatalog.IsAllowedLevel(AllLevel))
            {
                AllLevel = ScopedLogCatalog.LevelAll;
                changed = true;
            }
            else
            {
                var normalizedAll = ScopedLogCatalog.NormalizeLevel(AllLevel);
                if (!string.Equals(normalizedAll, AllLevel, StringComparison.Ordinal))
                {
                    AllLevel = normalizedAll;
                    changed = true;
                }
            }

            if (CategoryLevels is null)
            {
                CategoryLevels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                changed = true;
            }
            else if (CategoryLevels.Comparer != StringComparer.OrdinalIgnoreCase)
            {
                CategoryLevels = new Dictionary<string, string>(CategoryLevels, StringComparer.OrdinalIgnoreCase);
                changed = true;
            }

            foreach (var category in ScopedLogCatalog.GetCategories(mod))
            {
                if (!CategoryLevels.TryGetValue(category, out var level))
                {
                    CategoryLevels[category] = ScopedLogCatalog.LevelAll;
                    changed = true;
                    continue;
                }

                if (!ScopedLogCatalog.IsAllowedLevel(level))
                {
                    CategoryLevels[category] = ScopedLogCatalog.LevelAll;
                    changed = true;
                    continue;
                }

                var normalizedLevel = ScopedLogCatalog.NormalizeLevel(level);
                if (!string.Equals(normalizedLevel, level, StringComparison.Ordinal))
                {
                    CategoryLevels[category] = normalizedLevel;
                    changed = true;
                }
            }

            if (!CategoryLevels.ContainsKey(ScopedLogCatalog.CategoryGeneral))
            {
                CategoryLevels[ScopedLogCatalog.CategoryGeneral] = ScopedLogCatalog.LevelAll;
                changed = true;
            }

            return changed;
        }

        public string GetCategoryLevel(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
                category = ScopedLogCatalog.CategoryGeneral;

            if (CategoryLevels.TryGetValue(category, out var level))
                return ScopedLogCatalog.NormalizeLevel(level);

            return ScopedLogCatalog.LevelAll;
        }

        public void SetCategoryLevel(string category, string level)
        {
            if (string.IsNullOrWhiteSpace(category))
                category = ScopedLogCatalog.CategoryGeneral;

            CategoryLevels[category] = ScopedLogCatalog.NormalizeLevel(level);
        }

        public string ResolveEffectiveLevel(string category)
        {
            if (CategoryLevels.TryGetValue(category, out var specific) && ScopedLogCatalog.IsAllowedLevel(specific))
                return ScopedLogCatalog.NormalizeLevel(specific);

            return ScopedLogCatalog.NormalizeLevel(AllLevel);
        }
    }
}
