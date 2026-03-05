using System;
using System.Collections.Generic;
using System.Linq;
using Mikew.NestCore.Helpers;
using Mikew.NestCore.Integrations.Gmcm;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.FarmAnimals;
using EggsInWild.Infrastructure.Config;
using EggsInWild.Infrastructure.ModData;
using EggsInWild.Infrastructure.ModData.Models;
using EggsInWild.Infrastructure.Mods;
using EggsInWild.Infrastructure.Diagnostics;

namespace EggsInWild.Infrastructure.Integrations.Gmcm.Pages.WildAnimals;

/// <summary>
/// GMCM pages for Wild Animals detail pages (per-animal configuration).
/// </summary>
public static class WildAnimalDetailPages
{
    /// <summary>
    /// Define detail pages for each wild animal.
    /// </summary>
    public static void Define(
        EiwGmcmContext ctx,
        GmcmMenuLayout<ModConfig> layout,
        I18TranslationHelper i18n,
        Dictionary<string, WildAnimalJson> wildAnimalsForGmcm,
        List<string> wildAnimalKeysForGmcm,
        List<string> locationTypeKeysForGmcm)
    {
        foreach (var key in wildAnimalKeysForGmcm)
        {
            var pageId = ModEntry.BuildWildAnimalPageId(key);

            layout.Page(
                pageId: pageId,
                title: () => key,
                build: page =>
                {
                    DefineWildAnimalDetailPage(ctx, layout, page, i18n, key, wildAnimalsForGmcm, locationTypeKeysForGmcm);
                });
        }
    }

    private static void DefineWildAnimalDetailPage(
        EiwGmcmContext ctx,
        GmcmMenuLayout<ModConfig> layout,
        GmcmPageBuilder<ModConfig> page,
        I18TranslationHelper i18n,
        string key,
        Dictionary<string, WildAnimalJson> wildAnimalsForGmcm,
        List<string> locationTypeKeysForGmcm)
    {
        var definition = wildAnimalsForGmcm.TryGetValue(key, out var def) ? def : null;

        var defaultEnabled = definition?.Enabled ?? true;
        var defaultLargeEgg = definition?.LargeEgg ?? false;
        var defaultPrice = definition?.Price ?? 0;
        var defaultIsMagic = definition?.IsMagic ?? false;

        var modAvailability = new SmapiModAvailability(ctx.Helper.ModRegistry);

        static bool IsEmptyOverride(WildAnimalOverrideConfig? ov)
            => ov is null
                || (ov.Enabled is null
                    && ov.LargeEgg is null
                    && ov.Price is null
                    && ov.IsMagic is null
                    && (ov.LocationTypes is null || ov.LocationTypes.Count == 0)
                    && (ov.ExtraModAnimalKeys is null || ov.ExtraModAnimalKeys.Count == 0));

        WildAnimalOverrideConfig? GetOverride(ModConfig cfg)
        {
            if (cfg.WildAnimalOverrides is null)
                return null;

            cfg.WildAnimalOverrides.TryGetValue(key, out var ov);
            return ov;
        }

        // Status paragraph
        page.ParagraphDynamic(
            () =>
            {
                var cfg = ctx.GetConfig();
                var source = ModEntry.GetWildAnimalEnabledSource(cfg, key, defaultEnabled);
                var effective = ModEntry.GetWildAnimalEffectiveEnabled(cfg, key, defaultEnabled);

                var state = effective
                    ? ModEntry.Translate(i18n, "config.wildAnimals.state.enabled", "Enabled")
                    : ModEntry.Translate(i18n, "config.wildAnimals.state.disabled", "Disabled");

                return ModEntry.Translate(i18n, "config.paragraph.wildAnimals.status.text", $"Effective: {state} (source: {source})", state, source);
            },
            id: $"wildAnimals_status_{GmcmFieldIds.Normalize(key)}");

        // Scalars paragraph
        page.ParagraphDynamic(
            () =>
            {
                var cfg = ctx.GetConfig();
                var ov = GetOverride(cfg);

                var largeEggOverride = ov?.LargeEgg;
                var priceOverride = ov?.Price;
                var isMagicOverride = ov?.IsMagic;

                var effectiveLargeEgg = largeEggOverride ?? defaultLargeEgg;
                var effectivePrice = priceOverride ?? defaultPrice;
                var effectiveIsMagic = isMagicOverride ?? defaultIsMagic;

                var largeEggSource = largeEggOverride is null ? "default" : "override";
                var priceSource = priceOverride is null ? "default" : "override";
                var isMagicSource = isMagicOverride is null ? "default" : "override";

                var yes = ModEntry.Translate(i18n, "config.wildAnimals.bool.yes", "Yes");
                var no = ModEntry.Translate(i18n, "config.wildAnimals.bool.no", "No");

                string BoolToYesNo(bool v) => v ? yes : no;

                return ModEntry.Translate(
                    i18n,
                    "config.paragraph.wildAnimals.scalars.text",
                    $"Effective scalars: LargeEgg={BoolToYesNo(effectiveLargeEgg)} (source: {largeEggSource}); Price={effectivePrice} (source: {priceSource}); IsMagic={BoolToYesNo(effectiveIsMagic)} (source: {isMagicSource}).",
                    BoolToYesNo(effectiveLargeEgg),
                    largeEggSource,
                    effectivePrice,
                    priceSource,
                    BoolToYesNo(effectiveIsMagic),
                    isMagicSource);
            },
            id: $"wildAnimals_scalars_{GmcmFieldIds.Normalize(key)}");

        // Overrides summary paragraph
        page.ParagraphDynamic(
            () =>
            {
                var cfg = ctx.GetConfig();
                var ov = GetOverride(cfg);

                var yes = ModEntry.Translate(i18n, "config.wildAnimals.bool.yes", "Yes");
                var no = ModEntry.Translate(i18n, "config.wildAnimals.bool.no", "No");
                var unset = ModEntry.Translate(i18n, "config.wildAnimals.value.unset", "(unset)");

                string BoolToYesNo(bool v) => v ? yes : no;
                string FormatOverrideBool(bool? v) => v is null ? unset : BoolToYesNo(v.Value);
                string FormatOverrideInt(int? v) => v is null ? unset : v.Value.ToString();

                var enabledText = FormatOverrideBool(ov?.Enabled);
                var largeEggText = FormatOverrideBool(ov?.LargeEgg);
                var priceText = FormatOverrideInt(ov?.Price);
                var isMagicText = FormatOverrideBool(ov?.IsMagic);

                return ModEntry.Translate(
                    i18n,
                    "config.paragraph.wildAnimals.overrides.text",
                    $"Overrides: Enabled={enabledText}; LargeEgg={largeEggText}; Price={priceText}; IsMagic={isMagicText}.",
                    enabledText,
                    largeEggText,
                    priceText,
                    isMagicText);
            },
            id: $"wildAnimals_overrides_{GmcmFieldIds.Normalize(key)}");

        page.Separator(simple: true);

        // Enabled toggle
        page.BoolLabel(
            elementId: $"wildAnimals.{key}.enabled",
            name: () => ModEntry.Translate(i18n, "config.wildAnimals.enabled.name", "Enabled"),
            tooltip: () =>
            {
                var defState = defaultEnabled
                    ? ModEntry.Translate(i18n, "config.wildAnimals.state.enabled", "Enabled")
                    : ModEntry.Translate(i18n, "config.wildAnimals.state.disabled", "Disabled");

                return ModEntry.Translate(
                    i18n,
                    "config.wildAnimals.enabled.tooltip",
                    $"Override enabled state for '{key}'. Default: {defState}.",
                    key,
                    defState);
            },
            getValue: () =>
            {
                var cfg = ctx.GetConfig();
                return ModEntry.GetWildAnimalEffectiveEnabled(cfg, key, defaultEnabled);
            },
            setValue: value =>
            {
                var cfg = ctx.GetConfig();
                cfg.WildAnimalOverrides ??= new Dictionary<string, WildAnimalOverrideConfig>(StringComparer.OrdinalIgnoreCase);

                if (!cfg.WildAnimalOverrides.TryGetValue(key, out var ov) || ov is null)
                    ov = new WildAnimalOverrideConfig();

                ov.Enabled = value == defaultEnabled ? null : value;

                if (IsEmptyOverride(ov))
                    cfg.WildAnimalOverrides.Remove(key);
                else
                    cfg.WildAnimalOverrides[key] = ov;
            });

        // LargeEgg toggle
        page.BoolLabel(
            elementId: $"wildAnimals.{key}.largeEgg",
            name: () => ModEntry.Translate(i18n, "config.wildAnimals.largeEgg.name", "Large egg"),
            tooltip: () =>
            {
                var yes = ModEntry.Translate(i18n, "config.wildAnimals.bool.yes", "Yes");
                var no = ModEntry.Translate(i18n, "config.wildAnimals.bool.no", "No");
                var defVal = defaultLargeEgg ? yes : no;

                return ModEntry.Translate(
                    i18n,
                    "config.wildAnimals.largeEgg.tooltip",
                    $"Override LargeEgg flag for '{key}'. Default: {defVal}.",
                    key,
                    defVal);
            },
            getValue: () =>
            {
                var cfg = ctx.GetConfig();
                var ov = GetOverride(cfg);
                return ov?.LargeEgg ?? defaultLargeEgg;
            },
            setValue: value =>
            {
                var cfg = ctx.GetConfig();
                cfg.WildAnimalOverrides ??= new Dictionary<string, WildAnimalOverrideConfig>(StringComparer.OrdinalIgnoreCase);

                if (!cfg.WildAnimalOverrides.TryGetValue(key, out var ov) || ov is null)
                    ov = new WildAnimalOverrideConfig();

                ov.LargeEgg = value == defaultLargeEgg ? null : value;

                if (IsEmptyOverride(ov))
                    cfg.WildAnimalOverrides.Remove(key);
                else
                    cfg.WildAnimalOverrides[key] = ov;
            });

        // Price IntLabel
        page.IntLabel(
            elementId: $"wildAnimals.{key}.price",
            name: () => ModEntry.Translate(i18n, "config.wildAnimals.price.name", "Price"),
            tooltip: () => ModEntry.Translate(
                i18n,
                "config.wildAnimals.price.tooltip",
                $"Override price for '{key}'. Default: {defaultPrice}.",
                key,
                defaultPrice),
            getValue: () =>
            {
                var cfg = ctx.GetConfig();
                var ov = GetOverride(cfg);
                return ov?.Price ?? defaultPrice;
            },
            setValue: value =>
            {
                var cfg = ctx.GetConfig();
                cfg.WildAnimalOverrides ??= new Dictionary<string, WildAnimalOverrideConfig>(StringComparer.OrdinalIgnoreCase);

                if (!cfg.WildAnimalOverrides.TryGetValue(key, out var ov) || ov is null)
                    ov = new WildAnimalOverrideConfig();

                ov.Price = value == defaultPrice ? null : value;

                if (IsEmptyOverride(ov))
                    cfg.WildAnimalOverrides.Remove(key);
                else
                    cfg.WildAnimalOverrides[key] = ov;
            },
            min: 0,
            max: 20000,
            interval: 25);

        // IsMagic toggle
        page.BoolLabel(
            elementId: $"wildAnimals.{key}.isMagic",
            name: () => ModEntry.Translate(i18n, "config.wildAnimals.isMagic.name", "Magic"),
            tooltip: () =>
            {
                var yes = ModEntry.Translate(i18n, "config.wildAnimals.bool.yes", "Yes");
                var no = ModEntry.Translate(i18n, "config.wildAnimals.bool.no", "No");
                var defVal = defaultIsMagic ? yes : no;

                return ModEntry.Translate(
                    i18n,
                    "config.wildAnimals.isMagic.tooltip",
                    $"Override IsMagic flag for '{key}'. Default: {defVal}.",
                    key,
                    defVal);
            },
            getValue: () =>
            {
                var cfg = ctx.GetConfig();
                var ov = GetOverride(cfg);
                return ov?.IsMagic ?? defaultIsMagic;
            },
            setValue: value =>
            {
                var cfg = ctx.GetConfig();
                cfg.WildAnimalOverrides ??= new Dictionary<string, WildAnimalOverrideConfig>(StringComparer.OrdinalIgnoreCase);

                if (!cfg.WildAnimalOverrides.TryGetValue(key, out var ov) || ov is null)
                    ov = new WildAnimalOverrideConfig();

                ov.IsMagic = value == defaultIsMagic ? null : value;

                if (IsEmptyOverride(ov))
                    cfg.WildAnimalOverrides.Remove(key);
                else
                    cfg.WildAnimalOverrides[key] = ov;
            });

        // Location Types section
        page.Separator(simple: true);
        page.SectionLabel(() => ModEntry.Translate(i18n, "config.section.wildAnimals.locationTypes.label", "Location types"));

        page.ParagraphDynamic(
            () => ModEntry.Translate(
                i18n,
                "config.paragraph.wildAnimals.locationTypes.description.text",
                "Override eligibility per location type. Use Default to inherit from WildAnimalsList."),
            id: $"wildAnimals_locationTypes_{GmcmFieldIds.Normalize(key)}");

        var allowedValues = new[] { "default", "allow", "deny" };

        foreach (var locationTypeKey in locationTypeKeysForGmcm)
        {
            var baseAllowed = definition?.LocationTypes is not null
                && definition.LocationTypes.TryGetValue(locationTypeKey, out var baseVal)
                && baseVal;

            var elementId = $"wildAnimals.{key}.locationTypes.{locationTypeKey}";

            page.TextLabel(
                elementId: elementId,
                name: () => ModEntry.Translate(
                    i18n,
                    $"config.wildAnimals.locationTypes.type.{locationTypeKey}.name",
                    locationTypeKey),
                tooltip: () =>
                {
                    var defState = baseAllowed
                        ? ModEntry.Translate(i18n, "config.wildAnimals.state.enabled", "Enabled")
                        : ModEntry.Translate(i18n, "config.wildAnimals.state.disabled", "Disabled");

                    var typeName = ModEntry.Translate(
                        i18n,
                        $"config.wildAnimals.locationTypes.type.{locationTypeKey}.name",
                        locationTypeKey);

                    return ModEntry.Translate(
                        i18n,
                        "config.wildAnimals.locationTypes.type.tooltip",
                        $"Override eligibility for location type '{typeName}'. Default: {defState}.",
                        typeName,
                        defState);
                },
                getValue: () =>
                {
                    var cfg = ctx.GetConfig();
                    var ov = GetOverride(cfg);

                    if (ov?.LocationTypes is not null && ov.LocationTypes.TryGetValue(locationTypeKey, out var o))
                        return o ? "allow" : "deny";

                    return "default";
                },
                setValue: value =>
                {
                    var cfg = ctx.GetConfig();
                    cfg.WildAnimalOverrides ??= new Dictionary<string, WildAnimalOverrideConfig>(StringComparer.OrdinalIgnoreCase);

                    if (!cfg.WildAnimalOverrides.TryGetValue(key, out var ov) || ov is null)
                        ov = new WildAnimalOverrideConfig();

                    ov.LocationTypes ??= new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

                    if (string.Equals(value, "default", StringComparison.OrdinalIgnoreCase)
                        || (string.Equals(value, "allow", StringComparison.OrdinalIgnoreCase) && baseAllowed)
                        || (string.Equals(value, "deny", StringComparison.OrdinalIgnoreCase) && !baseAllowed))
                    {
                        ov.LocationTypes.Remove(locationTypeKey);
                    }
                    else if (string.Equals(value, "allow", StringComparison.OrdinalIgnoreCase))
                    {
                        ov.LocationTypes[locationTypeKey] = true;
                    }
                    else if (string.Equals(value, "deny", StringComparison.OrdinalIgnoreCase))
                    {
                        ov.LocationTypes[locationTypeKey] = false;
                    }

                    if (ov.LocationTypes.Count == 0)
                        ov.LocationTypes = null;

                    if (IsEmptyOverride(ov))
                        cfg.WildAnimalOverrides.Remove(key);
                    else
                        cfg.WildAnimalOverrides[key] = ov;
                },
                allowedValues: allowedValues,
                formatValue: v =>
                {
                    if (string.Equals(v, "allow", StringComparison.OrdinalIgnoreCase))
                        return ModEntry.Translate(i18n, "config.wildAnimals.locationTypes.choice.allow", "Allow");

                    if (string.Equals(v, "deny", StringComparison.OrdinalIgnoreCase))
                        return ModEntry.Translate(i18n, "config.wildAnimals.locationTypes.choice.deny", "Deny");

                    var defState = baseAllowed
                        ? ModEntry.Translate(i18n, "config.wildAnimals.locationTypes.choice.default.enabled", "Default (Enabled)")
                        : ModEntry.Translate(i18n, "config.wildAnimals.locationTypes.choice.default.disabled", "Default (Disabled)");

                    return defState;
                });
        }

        // ExtraModAnimalKeys section - using ModEntry bulk helpers
        DefineExtraModAnimalKeysSection(ctx, page, i18n, key, definition, locationTypeKeysForGmcm);
    }

    private static void DefineExtraModAnimalKeysSection(
        EiwGmcmContext ctx,
        GmcmPageBuilder<ModConfig> page,
        I18TranslationHelper i18n,
        string key,
        WildAnimalJson? definition,
        List<string> locationTypeKeysForGmcm)
    {
        // ExtraModAnimalKeys section
        page.Separator(simple: true);
        page.SectionLabel(() => ModEntry.Translate(i18n, "config.section.wildAnimals.extraModAnimalKeys.label", "Farm animal keys"));

        page.ParagraphDynamic(
            () => ModEntry.Translate(
                i18n,
                "config.paragraph.wildAnimals.extraModAnimalKeys.description.text",
                "Edit ExtraModAnimalKeys per mod group. Enter a comma-separated list of FarmAnimalKeys. Blank means default (inherit from WildAnimalsList)."),
            id: $"wildAnimals_extraModAnimalKeys_{GmcmFieldIds.Normalize(key)}");

        var extraModIdsForGmcm = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Vanilla",
        };

        if (definition?.ExtraModAnimalKeys is not null)
        {
            foreach (var modId in definition.ExtraModAnimalKeys.Keys)
                extraModIdsForGmcm.Add(modId);
        }

        {
            var cfg = ctx.GetConfig();
            var ov = GetOverrideForKey(cfg, key);
            if (ov?.ExtraModAnimalKeys is not null)
            {
                foreach (var modId in ov.ExtraModAnimalKeys.Keys)
                    extraModIdsForGmcm.Add(modId);
            }
        }

        var extraModIdsOrdered = extraModIdsForGmcm
            .OrderBy(mid => mid == "Vanilla" ? "" : mid, StringComparer.OrdinalIgnoreCase)
            .ToList();

        HashSet<string>? cachedFarmAnimalIds = null;
        var cachedFarmAnimalIdsLoaded = false;

        HashSet<string>? GetKnownFarmAnimalIds()
        {
            if (cachedFarmAnimalIdsLoaded)
                return cachedFarmAnimalIds;

            cachedFarmAnimalIdsLoaded = true;

            if (!Context.IsWorldReady)
                return null;

            try
            {
                var data = Game1.content.Load<Dictionary<string, FarmAnimalData>>("Data/FarmAnimals");
                cachedFarmAnimalIds = new HashSet<string>(data.Keys, StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                cachedFarmAnimalIds = null;
            }

            return cachedFarmAnimalIds;
        }

        // Custom upsert
        page.TextLabel(
            elementId: $"wildAnimals.{key}.extraModAnimalKeys.customUpsert",
            name: () => ModEntry.Translate(i18n, "config.wildAnimals.extraModAnimalKeys.customUpsert.name", "Add/update custom modId block"),
            tooltip: () => ModEntry.Translate(
                i18n,
                "config.wildAnimals.extraModAnimalKeys.customUpsert.tooltip",
                "Create or update a custom mod-group block not present in WildAnimalsList. Format: modId | csvKeys (example: Author.ModName | Pig, Cow). Close and reopen this page to refresh the list."),
            getValue: () => "",
            setValue: rawValue =>
            {
                var input = (rawValue ?? string.Empty).Trim();
                if (input.Length == 0)
                    return;

                var sepIndex = input.IndexOf('|');
                if (sepIndex < 0) sepIndex = input.IndexOf(':');
                if (sepIndex < 0) sepIndex = input.IndexOf('=');

                if (sepIndex <= 0 || sepIndex >= input.Length - 1)
                    return;

                var modIdRaw = input[..sepIndex].Trim();
                var keysRaw = input[(sepIndex + 1)..].Trim();

                if (modIdRaw.Length == 0 || string.Equals(modIdRaw, "Vanilla", StringComparison.OrdinalIgnoreCase))
                    return;

                var parsed = ModEntry.ParseCsvKeyList(keysRaw);
                if (parsed.Count == 0)
                    return;

                var fixedKeys = parsed;
                if (ctx.GetConfig().GmcmExtraModAnimalKeysBulkFixDropUnknownFarmAnimalKeys && Context.IsWorldReady && parsed.Count > 0)
                {
                    try
                    {
                        var data = Game1.content.Load<Dictionary<string, FarmAnimalData>>("Data/FarmAnimals");
                        var known = new HashSet<string>(data.Keys, StringComparer.OrdinalIgnoreCase);
                        fixedKeys = parsed.Where(x => known.Contains(x)).ToList();
                    }
                    catch
                    {
                    }
                }

                if (fixedKeys.Count == 0)
                    return;

                var modId = modIdRaw;
                foreach (var existing in extraModIdsForGmcm)
                {
                    if (string.Equals(existing, modIdRaw, StringComparison.OrdinalIgnoreCase))
                    {
                        modId = existing;
                        break;
                    }
                }

                var baseKeys = definition?.ExtraModAnimalKeys is not null
                    && definition.ExtraModAnimalKeys.TryGetValue(modId, out var baseBlock)
                    ? baseBlock.GetEffectiveFarmAnimalKeys()
                    : Array.Empty<string>();

                var cfg = ctx.GetConfig();
                cfg.WildAnimalOverrides ??= new Dictionary<string, WildAnimalOverrideConfig>(StringComparer.OrdinalIgnoreCase);

                if (!cfg.WildAnimalOverrides.TryGetValue(key, out var ov) || ov is null)
                    ov = new WildAnimalOverrideConfig();

                ov.ExtraModAnimalKeys ??= new Dictionary<string, ExtraModAnimalKeysOverrideConfig>(StringComparer.OrdinalIgnoreCase);

                if (ModEntry.SequenceEqualIgnoreCase(fixedKeys, baseKeys))
                {
                    ov.ExtraModAnimalKeys.Remove(modId);
                }
                else
                {
                    ov.ExtraModAnimalKeys[modId] = new ExtraModAnimalKeysOverrideConfig
                    {
                        FarmAnimalKeys = fixedKeys.ToList(),
                    };
                }

                if (ov.ExtraModAnimalKeys.Count == 0)
                    ov.ExtraModAnimalKeys = null;

                if (IsEmptyOverride(ov))
                    cfg.WildAnimalOverrides.Remove(key);
                else
                    cfg.WildAnimalOverrides[key] = ov;
            });

        // Custom remove
        page.TextLabel(
            elementId: $"wildAnimals.{key}.extraModAnimalKeys.customRemove",
            name: () => ModEntry.Translate(i18n, "config.wildAnimals.extraModAnimalKeys.customRemove.name", "Remove modId override block"),
            tooltip: () => ModEntry.Translate(
                i18n,
                "config.wildAnimals.extraModAnimalKeys.customRemove.tooltip",
                "Remove an ExtraModAnimalKeys override block by modId. This resets to default for base blocks, and deletes custom blocks. Enter: modId. Close and reopen this page to refresh the list."),
            getValue: () => "",
            setValue: rawValue =>
            {
                var modIdRaw = (rawValue ?? string.Empty).Trim();
                if (modIdRaw.Length == 0 || string.Equals(modIdRaw, "Vanilla", StringComparison.OrdinalIgnoreCase))
                    return;

                var modId = modIdRaw;
                foreach (var existing in extraModIdsForGmcm)
                {
                    if (string.Equals(existing, modIdRaw, StringComparison.OrdinalIgnoreCase))
                    {
                        modId = existing;
                        break;
                    }
                }

                var cfg = ctx.GetConfig();
                var ov = GetOverrideForKey(cfg, key);
                if (ov?.ExtraModAnimalKeys is null)
                    return;

                ov.ExtraModAnimalKeys.Remove(modId);

                if (ov.ExtraModAnimalKeys.Count == 0)
                    ov.ExtraModAnimalKeys = null;

                if (IsEmptyOverride(ov))
                    cfg.WildAnimalOverrides.Remove(key);
                else
                    cfg.WildAnimalOverrides[key] = ov;
            });

        // Clear staged bulk
        page.Bool(
            optionId: ModEntry.Translate(i18n, "config.wildAnimals.extraModAnimalKeys.bulkClear.name", "Clear staged bulk input"),
            getValue: () => false,
            setValue: v =>
            {
                if (!v)
                    return;

                ctx.ExtraModAnimalKeysBulkByWildAnimalKey.Remove(key);
            },
            tooltip: true,
            fieldId: $"wildAnimals_extraModAnimalKeys_bulkClear_{GmcmFieldIds.Normalize(key)}");

        // Export to SMAPI log
        page.BoolLabel(
            elementId: $"wildAnimals.{key}.extraModAnimalKeys.exportEffective",
            name: () => ModEntry.Translate(i18n, "config.wildAnimals.extraModAnimalKeys.exportEffective.name", "Export effective blocks to SMAPI log"),
            tooltip: () => ModEntry.Translate(
                i18n,
                "config.wildAnimals.extraModAnimalKeys.exportEffective.tooltip",
                "Write one line per mod-group to SMAPI log: modId | csvKeys (effective after overrides)."),
            getValue: () => false,
            setValue: v =>
            {
                if (!v)
                    return;

                var cfg = ctx.GetConfig();
                var ov = GetOverrideForKey(cfg, key);

                var outLines = new List<string>
                {
                    $"[GMCM][ExtraModAnimalKeys] Export effective blocks for '{key}':",
                };

                var bodyLines = ModEntry.BuildExtraModAnimalKeysEffectiveExportBodyLines(extraModIdsOrdered, definition, ov);
                outLines.AddRange(bodyLines);

                if (bodyLines.Count == 0)
                    outLines.Add(ModEntry.Translate(i18n, "config.wildAnimals.value.none", "(none)"));

                ctx.Monitor.Log(string.Join(Environment.NewLine, outLines), LogLevel.Info);
            });

        // Copy export to bulk (overwrite)
        page.BoolLabel(
            elementId: $"wildAnimals.{key}.extraModAnimalKeys.copyExportToBulkOverwrite",
            name: () => ModEntry.Translate(i18n, "config.wildAnimals.extraModAnimalKeys.copyExportToBulkOverwrite.name", "Copy export into bulk stage (overwrite)"),
            tooltip: () => ModEntry.Translate(
                i18n,
                "config.wildAnimals.extraModAnimalKeys.copyExportToBulkOverwrite.tooltip",
                "Overwrite the staged bulk input with the exported effective blocks (one line per mod-group: modId | csvKeys)."),
            getValue: () => false,
            setValue: v =>
            {
                if (!v)
                    return;

                var cfg = ctx.GetConfig();
                var ov = GetOverrideForKey(cfg, key);

                var bodyLines = ModEntry.BuildExtraModAnimalKeysEffectiveExportBodyLines(extraModIdsOrdered, definition, ov);
                var raw = string.Join(Environment.NewLine, bodyLines);
                if (string.IsNullOrWhiteSpace(raw))
                {
                    ctx.ExtraModAnimalKeysBulkByWildAnimalKey.Remove(key);
                    return;
                }

                ctx.ExtraModAnimalKeysBulkByWildAnimalKey[key] = ModEntry.ParseExtraModAnimalKeysBulk(raw, extraModIdsForGmcm, GetKnownFarmAnimalIds());
            });

        // Copy export to bulk (append)
        page.BoolLabel(
            elementId: $"wildAnimals.{key}.extraModAnimalKeys.copyExportToBulkAppend",
            name: () => ModEntry.Translate(i18n, "config.wildAnimals.extraModAnimalKeys.copyExportToBulkAppend.name", "Copy export into bulk stage (append)"),
            tooltip: () => ModEntry.Translate(
                i18n,
                "config.wildAnimals.extraModAnimalKeys.copyExportToBulkAppend.tooltip",
                "Append the exported effective blocks to the staged bulk input (one line per mod-group: modId | csvKeys)."),
            getValue: () => false,
            setValue: v =>
            {
                if (!v)
                    return;

                var cfg = ctx.GetConfig();
                var ov = GetOverrideForKey(cfg, key);

                var bodyLines = ModEntry.BuildExtraModAnimalKeysEffectiveExportBodyLines(extraModIdsOrdered, definition, ov);
                var exportRaw = string.Join(Environment.NewLine, bodyLines);
                if (string.IsNullOrWhiteSpace(exportRaw))
                    return;

                var existing = ctx.ExtraModAnimalKeysBulkByWildAnimalKey.TryGetValue(key, out var st) ? st.Raw : "";
                existing ??= string.Empty;

                var combined = existing.TrimEnd();
                if (combined.Length > 0)
                    combined += Environment.NewLine;
                combined += exportRaw;

                ctx.ExtraModAnimalKeysBulkByWildAnimalKey[key] = ModEntry.ParseExtraModAnimalKeysBulk(combined, extraModIdsForGmcm, GetKnownFarmAnimalIds());
            });

        // Refresh note
        page.ParagraphDynamic(
            () => ModEntry.Translate(
                i18n,
                "config.wildAnimals.extraModAnimalKeys.customRefreshNote.text",
                "After adding/removing blocks, close and reopen this page to refresh the list."),
            id: $"wildAnimals_extraModAnimalKeys_customRefresh_{GmcmFieldIds.Normalize(key)}");

        // Bulk edit section
        page.Separator(simple: true);
        page.SectionLabel(() => ModEntry.Translate(i18n, "config.section.wildAnimals.extraModAnimalKeys.bulk.label", "Bulk edit (stage)"));

        DefineBulkStageSection(ctx, page, i18n, key, definition, extraModIdsForGmcm, GetKnownFarmAnimalIds);
    }

    private static void DefineBulkStageSection(
        EiwGmcmContext ctx,
        GmcmPageBuilder<ModConfig> page,
        I18TranslationHelper i18n,
        string key,
        WildAnimalJson? definition,
        HashSet<string> extraModIdsForGmcm,
        Func<HashSet<string>?> getKnownFarmAnimalIds)
    {
        // Bulk header paragraph
        page.ParagraphDynamic(
            () =>
            {
                if (!ctx.ExtraModAnimalKeysBulkByWildAnimalKey.TryGetValue(key, out var st) || string.IsNullOrWhiteSpace(st.Raw))
                    return ModEntry.Translate(i18n, "config.paragraph.wildAnimals.extraModAnimalKeys.bulkHeader.empty", "Bulk: (none staged).");

                var cfg = ctx.GetConfig();

                var fixTrim = cfg.GmcmExtraModAnimalKeysBulkFixTrimWhitespace;
                var fixDedupe = cfg.GmcmExtraModAnimalKeysBulkFixRemoveDuplicates;
                var fixDropUnknown = cfg.GmcmExtraModAnimalKeysBulkFixDropUnknownFarmAnimalKeys;

                var knownFarmAnimalIds = fixDropUnknown ? getKnownFarmAnimalIds() : null;

                var enabledFixes = new List<string>();
                if (fixTrim) enabledFixes.Add("trim");
                if (fixDedupe) enabledFixes.Add("dedupe");
                if (fixDropUnknown) enabledFixes.Add("drop-unknown");
                var enabledFixesText = enabledFixes.Count == 0 ? "(none)" : string.Join(", ", enabledFixes);

                var ovNow = GetOverrideForKey(cfg, key);

                var plan = ModEntry.ComputeExtraModAnimalKeysBulkPlanCounts(
                    st,
                    extraModIdsForGmcm,
                    definition,
                    ovNow,
                    fixTrim,
                    fixDedupe,
                    knownFarmAnimalIds);

                return ModEntry.Translate(
                    i18n,
                    "config.paragraph.wildAnimals.extraModAnimalKeys.bulkHeader.text",
                    "Bulk staged: {0} upserts, {1} resets, {2} warnings | Planned: set {3}, clear {4}, no-op {5} | Fixes: {6}.",
                    st.Upserts.Count,
                    st.Resets.Count,
                    st.Warnings.Count,
                    plan.PlannedSet,
                    plan.PlannedClear,
                    plan.PlannedNoop,
                    enabledFixesText);
            },
            id: $"wildAnimals_extraModAnimalKeys_bulkHeader_{GmcmFieldIds.Normalize(key)}");

        // Bulk input
        page.TextLabel(
            elementId: $"wildAnimals.{key}.extraModAnimalKeys.bulkInput",
            name: () => ModEntry.Translate(i18n, "config.wildAnimals.extraModAnimalKeys.bulkInput.name", "Bulk paste (stage)"),
            tooltip: () => ModEntry.Translate(
                i18n,
                "config.wildAnimals.extraModAnimalKeys.bulkInput.tooltip",
                "Paste one line per mod-group: modId | csvKeys. You may comma-separate modIds (A,B | Cow). Use '*' (or blank) to reset/remove a block. This only stages changes."),
            getValue: () =>
                ctx.ExtraModAnimalKeysBulkByWildAnimalKey.TryGetValue(key, out var st) ? st.Raw : "",
            setValue: rawValue =>
            {
                var st = ModEntry.ParseExtraModAnimalKeysBulk(rawValue, extraModIdsForGmcm, getKnownFarmAnimalIds());
                ctx.ExtraModAnimalKeysBulkByWildAnimalKey[key] = st;
            });

        // Bulk preview
        DefineBulkPreviewParagraph(ctx, page, i18n, key, definition, extraModIdsForGmcm, getKnownFarmAnimalIds);

        // Bulk apply note
        page.ParagraphDynamic(
            () => ModEntry.Translate(
                i18n,
                "config.paragraph.wildAnimals.extraModAnimalKeys.bulkApply.note",
                "Toggle on to apply staged bulk changes. This writes overrides immediately and clears the staged text."),
            id: $"wildAnimals_extraModAnimalKeys_bulkApplyNote_{GmcmFieldIds.Normalize(key)}");

        // Skip noop toggle
        page.Bool(cfg => cfg.GmcmExtraModAnimalKeysBulkApplySkipNoop, optionId: "wildAnimals.extraModAnimalKeys.bulkSkipNoop", tooltip: true);

        // Quick fixes section
        page.Separator(simple: true);
        page.SectionLabel(() => ModEntry.Translate(i18n, "config.section.wildAnimals.extraModAnimalKeys.bulkFixes.label", "Bulk apply quick-fixes"));

        page.Bool(cfg => cfg.GmcmExtraModAnimalKeysBulkFixTrimWhitespace, optionId: "wildAnimals.extraModAnimalKeys.bulkFixes.trim", tooltip: true);
        page.Bool(cfg => cfg.GmcmExtraModAnimalKeysBulkFixRemoveDuplicates, optionId: "wildAnimals.extraModAnimalKeys.bulkFixes.dedupe", tooltip: true);
        page.Bool(cfg => cfg.GmcmExtraModAnimalKeysBulkFixDropUnknownFarmAnimalKeys, optionId: "wildAnimals.extraModAnimalKeys.bulkFixes.dropUnknown", tooltip: true);

        // Bulk apply toggle
        DefineBulkApplyToggle(ctx, page, i18n, key, definition, extraModIdsForGmcm, getKnownFarmAnimalIds);
    }

    private static void DefineBulkPreviewParagraph(
        EiwGmcmContext ctx,
        GmcmPageBuilder<ModConfig> page,
        I18TranslationHelper i18n,
        string key,
        WildAnimalJson? definition,
        HashSet<string> extraModIdsForGmcm,
        Func<HashSet<string>?> getKnownFarmAnimalIds)
    {
        page.ParagraphDynamic(
            () =>
            {
                if (!ctx.ExtraModAnimalKeysBulkByWildAnimalKey.TryGetValue(key, out var st) || string.IsNullOrWhiteSpace(st.Raw))
                    return ModEntry.Translate(i18n, "config.paragraph.wildAnimals.extraModAnimalKeys.bulkPreview.empty", "Bulk preview: (none)");

                var cfg = ctx.GetConfig();

                var fixTrim = cfg.GmcmExtraModAnimalKeysBulkFixTrimWhitespace;
                var fixDedupe = cfg.GmcmExtraModAnimalKeysBulkFixRemoveDuplicates;
                var fixDropUnknown = cfg.GmcmExtraModAnimalKeysBulkFixDropUnknownFarmAnimalKeys;

                var knownFarmAnimalIds = fixDropUnknown ? getKnownFarmAnimalIds() : null;

                var ovNow = GetOverrideForKey(cfg, key);

                var plan = ModEntry.ComputeExtraModAnimalKeysBulkPlanCounts(
                    st,
                    extraModIdsForGmcm,
                    definition,
                    ovNow,
                    fixTrim,
                    fixDedupe,
                    knownFarmAnimalIds);

                static string FormatKeySample(IReadOnlyList<string> keys, int cap = 8)
                {
                    if (keys.Count == 0)
                        return "(empty)";

                    var sample = keys.Take(cap).ToList();
                    var joined = string.Join(", ", sample);
                    return keys.Count > cap ? $"{joined}, ... (+{keys.Count - cap} more)" : joined;
                }

                var enabledFixes = new List<string>();
                if (fixTrim) enabledFixes.Add("trim");
                if (fixDedupe) enabledFixes.Add("dedupe");
                if (fixDropUnknown) enabledFixes.Add("drop-unknown");
                var enabledFixesText = enabledFixes.Count == 0 ? "(none)" : string.Join(", ", enabledFixes);

                var plannedSummary = ModEntry.Translate(i18n, "config.paragraph.wildAnimals.extraModAnimalKeys.bulkPreview.plan", "Planned: set {0}, clear {1}, no-op {2}.", plan.PlannedSet, plan.PlannedClear, plan.PlannedNoop);
                var plannedIsNoop = !plan.AnyChangePlanned;
                var lines = new List<string>
                {
                    ModEntry.Translate(i18n, "config.paragraph.wildAnimals.extraModAnimalKeys.bulkPreview.header", "Bulk preview (not applied yet):"),
                    ModEntry.Translate(i18n, "config.paragraph.wildAnimals.extraModAnimalKeys.bulkPreview.note", "This is a preview only. Use the toggle below to apply staged changes."),
                    ModEntry.Translate(i18n, "config.paragraph.wildAnimals.extraModAnimalKeys.bulkPreview.summary", "Staged: {0} upserts, {1} resets, {2} parse warnings.", st.Upserts.Count, st.Resets.Count, st.Warnings.Count),
                    plannedSummary,
                    ModEntry.Translate(i18n, "config.paragraph.wildAnimals.extraModAnimalKeys.bulkPreview.fixes", "Quick-fixes enabled for apply: {0}.", enabledFixesText),
                    "",
                };

                if (plannedIsNoop)
                {
                    lines.Add(ModEntry.Translate(i18n, "config.paragraph.wildAnimals.extraModAnimalKeys.bulkPreview.noop", "No config changes would be applied by Bulk Apply."));
                    lines.Add("");
                }

                if (st.Warnings.Count > 0)
                {
                    lines.Add("Warnings:");
                    foreach (var w in st.Warnings.Take(10))
                        lines.Add($" - {w}");
                    if (st.Warnings.Count > 10)
                        lines.Add($" - ... (+{st.Warnings.Count - 10} more)");
                    lines.Add("");
                }

                var fixPreview = ModEntry.BuildExtraModAnimalKeysBulkFixPreviewLines(st.Raw, fixTrim, fixDedupe, fixDropUnknown, knownFarmAnimalIds);
                if (fixPreview.Count > 0)
                {
                    lines.AddRange(fixPreview);
                    lines.Add("");
                }

                if (st.Upserts.Count > 0)
                {
                    lines.Add("Upserts:");
                    foreach (var (modId, rawKeys) in st.Upserts
                        .OrderBy(kvp => kvp.Key == "Vanilla" ? "" : kvp.Key, StringComparer.OrdinalIgnoreCase)
                        .Take(12))
                    {
                        var canonicalModId = ModEntry.CanonicalizeModId(modId, extraModIdsForGmcm);
                        var displayModId = string.Equals(modId, canonicalModId, StringComparison.OrdinalIgnoreCase)
                            ? modId
                            : $"{modId} => {canonicalModId}";

                        var fixedKeys = ModEntry.ApplyFarmAnimalKeyFixes(
                            rawKeys,
                            fixTrim,
                            fixDedupe,
                            knownFarmAnimalIds,
                            out var fixStats);

                        var blanksRemoved = fixStats.BlanksRemoved;
                        var trimmedCount = fixStats.TrimmedCount;
                        var dupesRemoved = fixStats.DuplicatesRemoved;
                        var unknownRemoved = fixStats.UnknownRemoved;

                        var deltaParts = new List<string>();
                        if (fixTrim && trimmedCount > 0) deltaParts.Add($"trim:{trimmedCount}");
                        if (blanksRemoved > 0) deltaParts.Add($"blank:{blanksRemoved}");
                        if (fixDedupe && dupesRemoved > 0) deltaParts.Add($"dupe:{dupesRemoved}");
                        if (fixDropUnknown && unknownRemoved > 0) deltaParts.Add($"unknown:{unknownRemoved}");

                        var delta = deltaParts.Count == 0 ? "" : $" ({string.Join(", ", deltaParts)})";

                        var baseKeys = definition?.ExtraModAnimalKeys is not null
                            && definition.ExtraModAnimalKeys.TryGetValue(canonicalModId, out var baseBlock)
                            ? baseBlock.GetEffectiveFarmAnimalKeys()
                            : Array.Empty<string>();

                        var matchesBase = fixedKeys.Count > 0 && ModEntry.SequenceEqualIgnoreCase(fixedKeys, baseKeys);

                        ExtraModAnimalKeysOverrideConfig? existingBlock = null;
                        var hasOverrideNow = ovNow?.ExtraModAnimalKeys is not null
                            && ovNow.ExtraModAnimalKeys.TryGetValue(canonicalModId, out existingBlock)
                            && existingBlock is not null;

                        var existingKeys = hasOverrideNow && existingBlock!.FarmAnimalKeys is { } ovKeys
                            ? ovKeys
                            : new List<string>();

                        var willClear = fixedKeys.Count == 0 || matchesBase;
                        var alreadySet = !willClear && hasOverrideNow && ModEntry.SequenceEqualIgnoreCase(existingKeys, fixedKeys);

                        if (willClear)
                        {
                            var action = hasOverrideNow ? "[will remove override]" : "[no-op; no override]";
                            if (matchesBase)
                            {
                                lines.Add($" - {displayModId}: {rawKeys.Count} -> {fixedKeys.Count} keys{delta}  [matches base] {action}");
                                lines.Add($"   final: [{FormatKeySample(fixedKeys)}]");
                            }
                            else
                            {
                                lines.Add($" - {displayModId}: {rawKeys.Count} -> 0 keys{delta}  {action}");
                            }
                        }
                        else if (alreadySet)
                        {
                            lines.Add($" - {displayModId}: {rawKeys.Count} -> {fixedKeys.Count} keys{delta}  [no-op; already set]");
                            lines.Add($"   final: [{FormatKeySample(fixedKeys)}]");
                        }
                        else
                        {
                            lines.Add($" - {displayModId}: {rawKeys.Count} -> {fixedKeys.Count} keys{delta}  [will set override]");
                            lines.Add($"   final: [{FormatKeySample(fixedKeys)}]");
                        }
                    }

                    if (st.Upserts.Count > 12)
                        lines.Add($" - ... (+{st.Upserts.Count - 12} more)");
                    lines.Add("");
                }

                if (st.Resets.Count > 0)
                {
                    lines.Add("Resets:");

                    foreach (var modIdRaw in st.Resets
                        .OrderBy(mid => mid == "Vanilla" ? "" : mid, StringComparer.OrdinalIgnoreCase)
                        .Take(12))
                    {
                        var canonicalModId = ModEntry.CanonicalizeModId(modIdRaw, extraModIdsForGmcm);
                        var displayModId = string.Equals(modIdRaw, canonicalModId, StringComparison.OrdinalIgnoreCase)
                            ? modIdRaw
                            : $"{modIdRaw} => {canonicalModId}";

                        var willRemove = ovNow?.ExtraModAnimalKeys is not null && ovNow.ExtraModAnimalKeys.ContainsKey(canonicalModId);
                        lines.Add(willRemove ? $" - {displayModId}  [will remove override]" : $" - {displayModId}  [no-op; no override]");
                    }

                    if (st.Resets.Count > 12)
                        lines.Add($" - ... (+{st.Resets.Count - 12} more)");
                }

                return string.Join(Environment.NewLine, lines);
            },
            id: $"wildAnimals_extraModAnimalKeys_bulkPreview_{GmcmFieldIds.Normalize(key)}");
    }

    private static void DefineBulkApplyToggle(
        EiwGmcmContext ctx,
        GmcmPageBuilder<ModConfig> page,
        I18TranslationHelper i18n,
        string key,
        WildAnimalJson? definition,
        HashSet<string> extraModIdsForGmcm,
        Func<HashSet<string>?> getKnownFarmAnimalIds)
    {
        page.Bool(
            optionId: ModEntry.Translate(i18n, "config.wildAnimals.extraModAnimalKeys.bulkApply.name", "Apply staged bulk changes"),
            getValue: () => false,
            setValue: v =>
            {
                if (!v)
                    return;

                if (!ctx.ExtraModAnimalKeysBulkByWildAnimalKey.TryGetValue(key, out var st) || string.IsNullOrWhiteSpace(st.Raw))
                    return;

                var cfg = ctx.GetConfig();
                var skipNoop = cfg.GmcmExtraModAnimalKeysBulkApplySkipNoop;

                var fixTrim = cfg.GmcmExtraModAnimalKeysBulkFixTrimWhitespace;
                var fixDedupe = cfg.GmcmExtraModAnimalKeysBulkFixRemoveDuplicates;
                var fixDropUnknown = cfg.GmcmExtraModAnimalKeysBulkFixDropUnknownFarmAnimalKeys;

                var knownFarmAnimalIds = fixDropUnknown ? getKnownFarmAnimalIds() : null;

                var ovNow = GetOverrideForKey(cfg, key);

                var plan = ModEntry.ComputeExtraModAnimalKeysBulkPlanCounts(
                    st,
                    extraModIdsForGmcm,
                    definition,
                    ovNow,
                    fixTrim,
                    fixDedupe,
                    knownFarmAnimalIds);

                if (skipNoop && !plan.AnyChangePlanned)
                {
                    ctx.Monitor.Log($"[GMCM][ExtraModAnimalKeys] Bulk Apply skipped (no-op) for '{key}'.", LogLevel.Trace);
                    return;
                }
                cfg.WildAnimalOverrides ??= new Dictionary<string, WildAnimalOverrideConfig>(StringComparer.OrdinalIgnoreCase);

                var ov = ovNow ?? new WildAnimalOverrideConfig();
                ov.ExtraModAnimalKeys ??= new Dictionary<string, ExtraModAnimalKeysOverrideConfig>(StringComparer.OrdinalIgnoreCase);

                foreach (var modIdRaw in st.Resets)
                {
                    var modId = ModEntry.CanonicalizeModId(modIdRaw, extraModIdsForGmcm);
                    ov.ExtraModAnimalKeys.Remove(modId);
                }

                foreach (var (modIdRaw, keys) in st.Upserts)
                {
                    var modId = ModEntry.CanonicalizeModId(modIdRaw, extraModIdsForGmcm);
                    var finalKeys = ModEntry.ApplyFarmAnimalKeyFixes(keys, fixTrim, fixDedupe, knownFarmAnimalIds);

                    var baseKeys = definition?.ExtraModAnimalKeys is not null
                        && definition.ExtraModAnimalKeys.TryGetValue(modId, out var baseBlock)
                        ? baseBlock.GetEffectiveFarmAnimalKeys()
                        : Array.Empty<string>();

                    if (finalKeys.Count == 0 || ModEntry.SequenceEqualIgnoreCase(finalKeys, baseKeys))
                    {
                        ov.ExtraModAnimalKeys.Remove(modId);
                    }
                    else
                    {
                        ov.ExtraModAnimalKeys[modId] = new ExtraModAnimalKeysOverrideConfig
                        {
                            FarmAnimalKeys = finalKeys.ToList(),
                        };
                    }
                }

                if (ov.ExtraModAnimalKeys.Count == 0)
                    ov.ExtraModAnimalKeys = null;

                if (IsEmptyOverride(ov))
                    cfg.WildAnimalOverrides.Remove(key);
                else
                    cfg.WildAnimalOverrides[key] = ov;

                ctx.ExtraModAnimalKeysBulkByWildAnimalKey.Remove(key);
            },
            tooltip: false,
            fieldId: $"wildAnimals_extraModAnimalKeys_bulkApply_{GmcmFieldIds.Normalize(key)}");
    }

    private static WildAnimalOverrideConfig? GetOverrideForKey(ModConfig cfg, string key)
    {
        if (cfg.WildAnimalOverrides is null)
            return null;

        cfg.WildAnimalOverrides.TryGetValue(key, out var ov);
        return ov;
    }

    private static bool IsEmptyOverride(WildAnimalOverrideConfig? ov)
        => ov is null
            || (ov.Enabled is null
                && ov.LargeEgg is null
                && ov.Price is null
                && ov.IsMagic is null
                && (ov.LocationTypes is null || ov.LocationTypes.Count == 0)
                && (ov.ExtraModAnimalKeys is null || ov.ExtraModAnimalKeys.Count == 0));
}
