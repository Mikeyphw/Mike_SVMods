using System;
using System.Collections.Generic;
using System.Linq;
using Mikew.NestCore.Helpers;
using Mikew.NestCore.Helpers.Logging;
using Mikew.NestCore.Integrations.Gmcm;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.FarmAnimals;
using EggsInWild.Infrastructure.Config;
using EggsInWild.Infrastructure.Diagnostics;

namespace EggsInWild;

public sealed partial class ModEntry
{
    private partial void DefineGmcmWildAnimalsPages(GmcmMenuLayout<ModConfig> layout, I18TranslationHelper i18n)
    {
        // ============================
        // Wild Animals (GMCM editable)
        // ============================

        Dictionary<string, EggsInWild.Infrastructure.ModData.Models.WildAnimalJson> wildAnimalsForGmcm;
        try
        {
            var reader = new EggsInWild.Infrastructure.ModData.SmapiJsonFileReader(Helper);
            wildAnimalsForGmcm =
                reader.ReadJsonFile<Dictionary<string, EggsInWild.Infrastructure.ModData.Models.WildAnimalJson>>(EggsInWild.Infrastructure.ModData.ModDataPaths.WildAnimalsList)
                ?? new Dictionary<string, EggsInWild.Infrastructure.ModData.Models.WildAnimalJson>(StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            var smapiLog = new SmapiLog(Monitor);
            smapiLog.LogScoped(ScopedLogCatalog.ModWeCategory.Content, $"Failed to load {EggsInWild.Infrastructure.ModData.ModDataPaths.WildAnimalsList} for GMCM: {ex}", LogLevel.Error);
            wildAnimalsForGmcm = new Dictionary<string, EggsInWild.Infrastructure.ModData.Models.WildAnimalJson>(StringComparer.OrdinalIgnoreCase);
        }

        var wildAnimalKeysForGmcm = wildAnimalsForGmcm.Keys
            .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
            .ToList();

        static int CountWildAnimalKeysBucket(IReadOnlyList<string> keys, char startInclusive, char endInclusive)
        {
            var count = 0;
            foreach (var k in keys)
            {
                if (string.IsNullOrWhiteSpace(k))
                    continue;

                var c = char.ToUpperInvariant(k.Trim()[0]);
                if (c >= startInclusive && c <= endInclusive)
                    count++;
            }

            return count;
        }

        static int CountWildAnimalKeysOther(IReadOnlyList<string> keys)
        {
            var count = 0;
            foreach (var k in keys)
            {
                if (string.IsNullOrWhiteSpace(k))
                    continue;

                var c = char.ToUpperInvariant(k.Trim()[0]);
                if (c < 'A' || c > 'Z')
                    count++;
            }

            return count;
        }

        var wildAnimalsCountAll = wildAnimalKeysForGmcm.Count;
        var wildAnimalsCountAf = CountWildAnimalKeysBucket(wildAnimalKeysForGmcm, 'A', 'F');
        var wildAnimalsCountGl = CountWildAnimalKeysBucket(wildAnimalKeysForGmcm, 'G', 'L');
        var wildAnimalsCountMr = CountWildAnimalKeysBucket(wildAnimalKeysForGmcm, 'M', 'R');
        var wildAnimalsCountSz = CountWildAnimalKeysBucket(wildAnimalKeysForGmcm, 'S', 'Z');
        var wildAnimalsCountOther = CountWildAnimalKeysOther(wildAnimalKeysForGmcm);

        var locationTypeKeysForGmcm = wildAnimalsForGmcm.Values
            .SelectMany(v => v.LocationTypes?.Keys ?? Enumerable.Empty<string>())
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(k => string.Equals(k, "Farm", StringComparison.OrdinalIgnoreCase) ? "" : k, StringComparer.OrdinalIgnoreCase)
            .ToList();

                _knownLocationTypeKeysForNormalization = new HashSet<string>(locationTypeKeysForGmcm, StringComparer.OrdinalIgnoreCase);

        layout.Page("wildAnimals", page =>
        {
            page.ParagraphDynamic(
                () => Translate(i18n, "config.paragraph.wildAnimals.description.text", "Enable/disable wild animals (affects egg assignment and Animal Notes progression)."),
                id: "wildAnimals_description");

            page.ParagraphDynamic(
                () =>
                {
                    var cfg = _config!.Get();
                    var enabled = 0;
                    foreach (var key in wildAnimalKeysForGmcm)
                    {
                        if (!wildAnimalsForGmcm.TryGetValue(key, out var def) || def is null)
                            continue;

                        var defaultEnabled = def.Enabled;
                        var effective = GetWildAnimalEffectiveEnabled(cfg, key, defaultEnabled);
                        if (effective)
                            enabled++;
                    }

                    return Translate(i18n, "config.paragraph.wildAnimals.summary.text", $"Enabled: {enabled}/{wildAnimalKeysForGmcm.Count}", enabled, wildAnimalKeysForGmcm.Count);
                },
                id: "wildAnimals_summary");

            if (wildAnimalKeysForGmcm.Count == 0)
            {
                page.ParagraphDynamic(
                    () => Translate(i18n, "config.paragraph.wildAnimals.empty.text", "No wild animals were found in the WildAnimalsList."),
                    id: "wildAnimals_empty");
                return;
            }

            page.Separator(simple: true);

            page.ParagraphDynamic(
                () => Translate(i18n, "config.paragraph.wildAnimals.indexGroups.note", "Pick a letter group to navigate. (GMCM pages are static; no live search.)"),
                id: "wildAnimals_indexGroups_note");

            page.LinkLabel(
                pageId: "wildAnimals_all",
                text: () => $"{Translate(i18n, "config.wildAnimals.index.group.all.name", "All (A–Z)")} ({wildAnimalsCountAll})",
                tooltip: () => Translate(i18n, "config.wildAnimals.index.group.all.description", "Show all wild animals (long list)."));

            page.LinkLabel(
                pageId: "wildAnimals_index_af",
                text: () => $"{Translate(i18n, "config.wildAnimals.index.group.af.name", "A–F")} ({wildAnimalsCountAf})",
                tooltip: () => Translate(i18n, "config.wildAnimals.index.group.af.description", "Wild animals starting with A–F."));

            page.LinkLabel(
                pageId: "wildAnimals_index_gl",
                text: () => $"{Translate(i18n, "config.wildAnimals.index.group.gl.name", "G–L")} ({wildAnimalsCountGl})",
                tooltip: () => Translate(i18n, "config.wildAnimals.index.group.gl.description", "Wild animals starting with G–L."));

            page.LinkLabel(
                pageId: "wildAnimals_index_mr",
                text: () => $"{Translate(i18n, "config.wildAnimals.index.group.mr.name", "M–R")} ({wildAnimalsCountMr})",
                tooltip: () => Translate(i18n, "config.wildAnimals.index.group.mr.description", "Wild animals starting with M–R."));

            page.LinkLabel(
                pageId: "wildAnimals_index_sz",
                text: () => $"{Translate(i18n, "config.wildAnimals.index.group.sz.name", "S–Z")} ({wildAnimalsCountSz})",
                tooltip: () => Translate(i18n, "config.wildAnimals.index.group.sz.description", "Wild animals starting with S–Z."));

            page.LinkLabel(
                pageId: "wildAnimals_index_other",
                text: () => $"{Translate(i18n, "config.wildAnimals.index.group.other.name", "Other")} ({wildAnimalsCountOther})",
                tooltip: () => Translate(i18n, "config.wildAnimals.index.group.other.description", "Wild animals not starting with A–Z."));

        });

        static string WildAnimalIndexBucket(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return "other";

            var c = char.ToUpperInvariant(key.Trim()[0]);
            if (c is >= 'A' and <= 'F') return "af";
            if (c is >= 'G' and <= 'L') return "gl";
            if (c is >= 'M' and <= 'R') return "mr";
            if (c is >= 'S' and <= 'Z') return "sz";
            return "other";
        }

        var wildAnimalBuckets = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["af"] = new List<string>(),
            ["gl"] = new List<string>(),
            ["mr"] = new List<string>(),
            ["sz"] = new List<string>(),
            ["other"] = new List<string>(),
        };

        foreach (var k in wildAnimalKeysForGmcm)
            wildAnimalBuckets[WildAnimalIndexBucket(k)].Add(k);

        void BuildWildAnimalIndexPage(string pageId, Func<string> title, IEnumerable<string> keys)
        {
            layout.Page(pageId, page =>
            {
                page.LinkLabel(
                    pageId: "wildAnimals",
                    text: () => Translate(i18n, "config.wildAnimals.index.back.name", "Back to WildAnimals index"),
                    tooltip: () => Translate(i18n, "config.wildAnimals.index.back.description", "Return to the WildAnimals index page."));

                page.Separator(simple: true);
                page.SectionLabel(title);

                foreach (var key in keys)
                {
                    var waPageId = BuildWildAnimalPageId(key);
                    page.LinkLabel(
                        pageId: waPageId,
                        text: () =>
                        {
                            var cfg = _config!.Get();
                            var enabled = wildAnimalsForGmcm.TryGetValue(key, out var def) && def is not null
                                ? GetWildAnimalEffectiveEnabled(cfg, key, def.Enabled)
                                : true;

                            var label = enabled ? key : $"{key} (disabled)";

                            if (_extraModAnimalKeysBulkByWildAnimalKey.TryGetValue(key, out var st) && !string.IsNullOrWhiteSpace(st.Raw))
                            {
                                var warnCount = st.Warnings.Count;
                                label += warnCount > 0 ? $" [staged, warn:{warnCount}]" : " [staged]";
                            }

                            return label;
                        },
                        tooltip: () => Translate(i18n, "config.wildAnimals.link.tooltip", "Edit enabled override for '{0}'.", key, key));
                }
            });
        }

        BuildWildAnimalIndexPage("wildAnimals_all", () => Translate(i18n, "config.wildAnimals.index.group.all.name", "All (A–Z)"), wildAnimalKeysForGmcm);
        BuildWildAnimalIndexPage("wildAnimals_index_af", () => Translate(i18n, "config.wildAnimals.index.group.af.name", "A–F"), wildAnimalBuckets["af"]);
        BuildWildAnimalIndexPage("wildAnimals_index_gl", () => Translate(i18n, "config.wildAnimals.index.group.gl.name", "G–L"), wildAnimalBuckets["gl"]);
        BuildWildAnimalIndexPage("wildAnimals_index_mr", () => Translate(i18n, "config.wildAnimals.index.group.mr.name", "M–R"), wildAnimalBuckets["mr"]);
        BuildWildAnimalIndexPage("wildAnimals_index_sz", () => Translate(i18n, "config.wildAnimals.index.group.sz.name", "S–Z"), wildAnimalBuckets["sz"]);
        BuildWildAnimalIndexPage("wildAnimals_index_other", () => Translate(i18n, "config.wildAnimals.index.group.other.name", "Other"), wildAnimalBuckets["other"]);


        foreach (var key in wildAnimalKeysForGmcm)
        {
            var pageId = BuildWildAnimalPageId(key);

            layout.Page(
                pageId: pageId,
                title: () => key,
                build: page =>
                {
                    var definition = wildAnimalsForGmcm.TryGetValue(key, out var def) ? def : null;

                    var defaultEnabled = definition?.Enabled ?? true;
                    var defaultLargeEgg = definition?.LargeEgg ?? false;
                    var defaultPrice = definition?.Price ?? 0;
                    var defaultIsMagic = definition?.IsMagic ?? false;

                    var modAvailability = new EggsInWild.Infrastructure.Mods.SmapiModAvailability(Helper.ModRegistry);

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

                    page.ParagraphDynamic(
                        () =>
                        {
                            var cfg = _config!.Get();
                            var source = GetWildAnimalEnabledSource(cfg, key, defaultEnabled);
                            var effective = GetWildAnimalEffectiveEnabled(cfg, key, defaultEnabled);

                            var state = effective
                                ? Translate(i18n, "config.wildAnimals.state.enabled", "Enabled")
                                : Translate(i18n, "config.wildAnimals.state.disabled", "Disabled");

                            return Translate(i18n, "config.paragraph.wildAnimals.status.text", $"Effective: {state} (source: {source})", state, source);
                        },
                        id: $"wildAnimals_status_{Mikew.NestCore.Integrations.Gmcm.GmcmFieldIds.Normalize(key)}");

                    page.ParagraphDynamic(
                        () =>
                        {
                            var cfg = _config!.Get();
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

                            var yes = Translate(i18n, "config.wildAnimals.bool.yes", "Yes");
                            var no = Translate(i18n, "config.wildAnimals.bool.no", "No");

                            string BoolToYesNo(bool v) => v ? yes : no;

                            return Translate(
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
                        id: $"wildAnimals_scalars_{Mikew.NestCore.Integrations.Gmcm.GmcmFieldIds.Normalize(key)}");

                    page.ParagraphDynamic(
                        () =>
                        {
                            var cfg = _config!.Get();
                            var ov = GetOverride(cfg);

                            var yes = Translate(i18n, "config.wildAnimals.bool.yes", "Yes");
                            var no = Translate(i18n, "config.wildAnimals.bool.no", "No");
                            var unset = Translate(i18n, "config.wildAnimals.value.unset", "(unset)");

                            string BoolToYesNo(bool v) => v ? yes : no;
                            string FormatOverrideBool(bool? v) => v is null ? unset : BoolToYesNo(v.Value);
                            string FormatOverrideInt(int? v) => v is null ? unset : v.Value.ToString();

                            var enabledText = FormatOverrideBool(ov?.Enabled);
                            var largeEggText = FormatOverrideBool(ov?.LargeEgg);
                            var priceText = FormatOverrideInt(ov?.Price);
                            var isMagicText = FormatOverrideBool(ov?.IsMagic);

                            return Translate(
                                i18n,
                                "config.paragraph.wildAnimals.overrides.text",
                                $"Overrides: Enabled={enabledText}; LargeEgg={largeEggText}; Price={priceText}; IsMagic={isMagicText}.",
                                enabledText,
                                largeEggText,
                                priceText,
                                isMagicText);
                        },
                        id: $"wildAnimals_overrides_{Mikew.NestCore.Integrations.Gmcm.GmcmFieldIds.Normalize(key)}");


                    page.Separator(simple: true);

                    page.BoolLabel(
                        elementId: $"wildAnimals.{key}.enabled",
                        name: () => Translate(i18n, "config.wildAnimals.enabled.name", "Enabled"),
                        tooltip: () =>
                        {
                            var defState = defaultEnabled
                                ? Translate(i18n, "config.wildAnimals.state.enabled", "Enabled")
                                : Translate(i18n, "config.wildAnimals.state.disabled", "Disabled");

                            return Translate(
                                i18n,
                                "config.wildAnimals.enabled.tooltip",
                                $"Override enabled state for '{key}'. Default: {defState}.",
                                key,
                                defState);
                        },
                        getValue: () =>
                        {
                            var cfg = _config!.Get();
                            return GetWildAnimalEffectiveEnabled(cfg, key, defaultEnabled);
                        },
                        setValue: value =>
                        {
                            var cfg = _config!.Get();
                            cfg.WildAnimalOverrides ??= new Dictionary<string, WildAnimalOverrideConfig>(StringComparer.OrdinalIgnoreCase);

                            if (!cfg.WildAnimalOverrides.TryGetValue(key, out var ov) || ov is null)
                                ov = new WildAnimalOverrideConfig();

                            ov.Enabled = value == defaultEnabled ? null : value;

                            if (IsEmptyOverride(ov))
                                cfg.WildAnimalOverrides.Remove(key);
                            else
                                cfg.WildAnimalOverrides[key] = ov;
                        });

                    page.BoolLabel(
                        elementId: $"wildAnimals.{key}.largeEgg",
                        name: () => Translate(i18n, "config.wildAnimals.largeEgg.name", "Large egg"),
                        tooltip: () =>
                        {
                            var yes = Translate(i18n, "config.wildAnimals.bool.yes", "Yes");
                            var no = Translate(i18n, "config.wildAnimals.bool.no", "No");
                            var defVal = defaultLargeEgg ? yes : no;

                            return Translate(
                                i18n,
                                "config.wildAnimals.largeEgg.tooltip",
                                $"Override LargeEgg flag for '{key}'. Default: {defVal}.",
                                key,
                                defVal);
                        },
                        getValue: () =>
                        {
                            var cfg = _config!.Get();
                            var ov = GetOverride(cfg);
                            return ov?.LargeEgg ?? defaultLargeEgg;
                        },
                        setValue: value =>
                        {
                            var cfg = _config!.Get();
                            cfg.WildAnimalOverrides ??= new Dictionary<string, WildAnimalOverrideConfig>(StringComparer.OrdinalIgnoreCase);

                            if (!cfg.WildAnimalOverrides.TryGetValue(key, out var ov) || ov is null)
                                ov = new WildAnimalOverrideConfig();

                            ov.LargeEgg = value == defaultLargeEgg ? null : value;

                            if (IsEmptyOverride(ov))
                                cfg.WildAnimalOverrides.Remove(key);
                            else
                                cfg.WildAnimalOverrides[key] = ov;
                        });

                    page.IntLabel(
                        elementId: $"wildAnimals.{key}.price",
                        name: () => Translate(i18n, "config.wildAnimals.price.name", "Price"),
                        tooltip: () => Translate(
                            i18n,
                            "config.wildAnimals.price.tooltip",
                            $"Override price for '{key}'. Default: {defaultPrice}.",
                            key,
                            defaultPrice),
                        getValue: () =>
                        {
                            var cfg = _config!.Get();
                            var ov = GetOverride(cfg);
                            return ov?.Price ?? defaultPrice;
                        },
                        setValue: value =>
                        {
                            var cfg = _config!.Get();
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

                    page.BoolLabel(
                        elementId: $"wildAnimals.{key}.isMagic",
                        name: () => Translate(i18n, "config.wildAnimals.isMagic.name", "Magic"),
                        tooltip: () =>
                        {
                            var yes = Translate(i18n, "config.wildAnimals.bool.yes", "Yes");
                            var no = Translate(i18n, "config.wildAnimals.bool.no", "No");
                            var defVal = defaultIsMagic ? yes : no;

                            return Translate(
                                i18n,
                                "config.wildAnimals.isMagic.tooltip",
                                $"Override IsMagic flag for '{key}'. Default: {defVal}.",
                                key,
                                defVal);
                        },
                        getValue: () =>
                        {
                            var cfg = _config!.Get();
                            var ov = GetOverride(cfg);
                            return ov?.IsMagic ?? defaultIsMagic;
                        },
                        setValue: value =>
                        {
                            var cfg = _config!.Get();
                            cfg.WildAnimalOverrides ??= new Dictionary<string, WildAnimalOverrideConfig>(StringComparer.OrdinalIgnoreCase);

                            if (!cfg.WildAnimalOverrides.TryGetValue(key, out var ov) || ov is null)
                                ov = new WildAnimalOverrideConfig();

                            ov.IsMagic = value == defaultIsMagic ? null : value;

                            if (IsEmptyOverride(ov))
                                cfg.WildAnimalOverrides.Remove(key);
                            else
                                cfg.WildAnimalOverrides[key] = ov;
                        });


                    page.Separator(simple: true);
                    page.SectionLabel(() => Translate(i18n, "config.section.wildAnimals.locationTypes.label", "Location types"));

                    page.ParagraphDynamic(
                        () => Translate(
                            i18n,
                            "config.paragraph.wildAnimals.locationTypes.description.text",
                            "Override eligibility per location type. Use Default to inherit from WildAnimalsList."),
                        id: $"wildAnimals_locationTypes_{Mikew.NestCore.Integrations.Gmcm.GmcmFieldIds.Normalize(key)}");

                    var allowedValues = new[] { "default", "allow", "deny" };

                    foreach (var locationTypeKey in locationTypeKeysForGmcm)
                    {
                        var baseAllowed = definition?.LocationTypes is not null
                            && definition.LocationTypes.TryGetValue(locationTypeKey, out var baseVal)
                            && baseVal;

                        var elementId = $"wildAnimals.{key}.locationTypes.{locationTypeKey}";

                        page.TextLabel(
                            elementId: elementId,
                            name: () =>
                            {
                                var typeName = Translate(
                                    i18n,
                                    $"config.wildAnimals.locationTypes.type.{locationTypeKey}.name",
                                    locationTypeKey);

                                return typeName;
                            },
                            tooltip: () =>
                            {
                                var defState = baseAllowed
                                    ? Translate(i18n, "config.wildAnimals.state.enabled", "Enabled")
                                    : Translate(i18n, "config.wildAnimals.state.disabled", "Disabled");

                                var typeName = Translate(
                                    i18n,
                                    $"config.wildAnimals.locationTypes.type.{locationTypeKey}.name",
                                    locationTypeKey);

                                return Translate(
                                    i18n,
                                    "config.wildAnimals.locationTypes.type.tooltip",
                                    $"Override eligibility for location type '{typeName}'. Default: {defState}.",
                                    typeName,
                                    defState);
                            },
                            getValue: () =>
                            {
                                var cfg = _config!.Get();
                                var ov = GetOverride(cfg);

                                if (ov?.LocationTypes is not null && ov.LocationTypes.TryGetValue(locationTypeKey, out var o))
                                    return o ? "allow" : "deny";

                                return "default";
                            },
                            setValue: value =>
                            {
                                var cfg = _config!.Get();
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
                                    return Translate(i18n, "config.wildAnimals.locationTypes.choice.allow", "Allow");

                                if (string.Equals(v, "deny", StringComparison.OrdinalIgnoreCase))
                                    return Translate(i18n, "config.wildAnimals.locationTypes.choice.deny", "Deny");

                                var defState = baseAllowed
                                    ? Translate(i18n, "config.wildAnimals.locationTypes.choice.default.enabled", "Default (Enabled)")
                                    : Translate(i18n, "config.wildAnimals.locationTypes.choice.default.disabled", "Default (Disabled)");

                                return defState;
                            });
                    }



                    page.Separator(simple: true);
                    page.SectionLabel(() => Translate(i18n, "config.section.wildAnimals.extraModAnimalKeys.label", "Farm animal keys"));

                    page.ParagraphDynamic(
                        () => Translate(
                            i18n,
                            "config.paragraph.wildAnimals.extraModAnimalKeys.description.text",
                            "Edit ExtraModAnimalKeys per mod group. Enter a comma-separated list of FarmAnimalKeys. Blank means default (inherit from WildAnimalsList)."),
                        id: $"wildAnimals_extraModAnimalKeys_{Mikew.NestCore.Integrations.Gmcm.GmcmFieldIds.Normalize(key)}");

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
                        var cfg = _config!.Get();
                        var ov = GetOverride(cfg);
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
                            var data = StardewValley.Game1.content.Load<Dictionary<string, FarmAnimalData>>("Data/FarmAnimals");
                            cachedFarmAnimalIds = new HashSet<string>(data.Keys, StringComparer.OrdinalIgnoreCase);
                        }
                        catch
                        {
                            cachedFarmAnimalIds = null;
                        }

                        return cachedFarmAnimalIds;
                    }

                    page.TextLabel(
                        elementId: $"wildAnimals.{key}.extraModAnimalKeys.customUpsert",
                        name: () => Translate(i18n, "config.wildAnimals.extraModAnimalKeys.customUpsert.name", "Add/update custom modId block"),
                        tooltip: () => Translate(
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

                            var parsed = ParseCsvKeyList(keysRaw);
                            if (parsed.Count == 0)
                                return;

                            var fixedKeys = parsed;
                            if (_config!.Get().GmcmExtraModAnimalKeysBulkFixDropUnknownFarmAnimalKeys && Context.IsWorldReady && parsed.Count > 0)
                            {
                                try
                                {
                                    var data = StardewValley.Game1.content.Load<Dictionary<string, FarmAnimalData>>("Data/FarmAnimals");
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

                            var cfg = _config!.Get();
                            cfg.WildAnimalOverrides ??= new Dictionary<string, WildAnimalOverrideConfig>(StringComparer.OrdinalIgnoreCase);

                            if (!cfg.WildAnimalOverrides.TryGetValue(key, out var ov) || ov is null)
                                ov = new WildAnimalOverrideConfig();

                            ov.ExtraModAnimalKeys ??= new Dictionary<string, ExtraModAnimalKeysOverrideConfig>(StringComparer.OrdinalIgnoreCase);

                            if (SequenceEqualIgnoreCase(fixedKeys, baseKeys))
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

                    page.TextLabel(
                        elementId: $"wildAnimals.{key}.extraModAnimalKeys.customRemove",
                        name: () => Translate(i18n, "config.wildAnimals.extraModAnimalKeys.customRemove.name", "Remove modId override block"),
                        tooltip: () => Translate(
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

                            var cfg = _config!.Get();
                            var ov = GetOverride(cfg);
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

                    page.Bool(
                        optionId: Translate(i18n, "config.wildAnimals.extraModAnimalKeys.bulkClear.name", "Clear staged bulk input"),
                        getValue: () => false,
                        setValue: v =>
                        {
                            if (!v)
                                return;

                            _extraModAnimalKeysBulkByWildAnimalKey.Remove(key);
                        },
                        tooltip: true,
                        fieldId: $"wildAnimals_extraModAnimalKeys_bulkClear_{Mikew.NestCore.Integrations.Gmcm.GmcmFieldIds.Normalize(key)}");

                    page.BoolLabel(
                        elementId: $"wildAnimals.{key}.extraModAnimalKeys.exportEffective",
                        name: () => Translate(i18n, "config.wildAnimals.extraModAnimalKeys.exportEffective.name", "Export effective blocks to SMAPI log"),
                        tooltip: () => Translate(
                            i18n,
                            "config.wildAnimals.extraModAnimalKeys.exportEffective.tooltip",
                            "Write one line per mod-group to SMAPI log: modId | csvKeys (effective after overrides)."),
                        getValue: () => false,
                        setValue: v =>
                        {
                            if (!v)
                                return;

                            var cfg = _config!.Get();
                            var ov = GetOverride(cfg);

                            var outLines = new List<string>
                            {
                                $"[GMCM][ExtraModAnimalKeys] Export effective blocks for '{key}':",
                            };

                            var bodyLines = BuildExtraModAnimalKeysEffectiveExportBodyLines(extraModIdsOrdered, definition, ov);
                            outLines.AddRange(bodyLines);

                            if (bodyLines.Count == 0)
                                outLines.Add(Translate(i18n, "config.wildAnimals.value.none", "(none)"));

                            Monitor.Log(string.Join(Environment.NewLine, outLines), LogLevel.Info);
                        });

                    page.BoolLabel(
                        elementId: $"wildAnimals.{key}.extraModAnimalKeys.copyExportToBulkOverwrite",
                        name: () => Translate(i18n, "config.wildAnimals.extraModAnimalKeys.copyExportToBulkOverwrite.name", "Copy export into bulk stage (overwrite)"),
                        tooltip: () => Translate(
                            i18n,
                            "config.wildAnimals.extraModAnimalKeys.copyExportToBulkOverwrite.tooltip",
                            "Overwrite the staged bulk input with the exported effective blocks (one line per mod-group: modId | csvKeys)."),
                        getValue: () => false,
                        setValue: v =>
                        {
                            if (!v)
                                return;

                            var cfg = _config!.Get();
                            var ov = GetOverride(cfg);

                            var bodyLines = BuildExtraModAnimalKeysEffectiveExportBodyLines(extraModIdsOrdered, definition, ov);
                            var raw = string.Join(Environment.NewLine, bodyLines);
                            if (string.IsNullOrWhiteSpace(raw))
                            {
                                _extraModAnimalKeysBulkByWildAnimalKey.Remove(key);
                                return;
                            }

                            _extraModAnimalKeysBulkByWildAnimalKey[key] = ParseExtraModAnimalKeysBulk(raw, extraModIdsForGmcm, GetKnownFarmAnimalIds());
                        });

                    page.BoolLabel(
                        elementId: $"wildAnimals.{key}.extraModAnimalKeys.copyExportToBulkAppend",
                        name: () => Translate(i18n, "config.wildAnimals.extraModAnimalKeys.copyExportToBulkAppend.name", "Copy export into bulk stage (append)"),
                        tooltip: () => Translate(
                            i18n,
                            "config.wildAnimals.extraModAnimalKeys.copyExportToBulkAppend.tooltip",
                            "Append the exported effective blocks to the staged bulk input (one line per mod-group: modId | csvKeys)."),
                        getValue: () => false,
                        setValue: v =>
                        {
                            if (!v)
                                return;

                            var cfg = _config!.Get();
                            var ov = GetOverride(cfg);

                            var bodyLines = BuildExtraModAnimalKeysEffectiveExportBodyLines(extraModIdsOrdered, definition, ov);
                            var exportRaw = string.Join(Environment.NewLine, bodyLines);
                            if (string.IsNullOrWhiteSpace(exportRaw))
                                return;

                            var existing = _extraModAnimalKeysBulkByWildAnimalKey.TryGetValue(key, out var st) ? st.Raw : "";
                            existing ??= string.Empty;

                            var combined = existing.TrimEnd();
                            if (combined.Length > 0)
                                combined += Environment.NewLine;
                            combined += exportRaw;

                            _extraModAnimalKeysBulkByWildAnimalKey[key] = ParseExtraModAnimalKeysBulk(combined, extraModIdsForGmcm, GetKnownFarmAnimalIds());
                        });


                    page.ParagraphDynamic(
                        () => Translate(
                            i18n,
                            "config.wildAnimals.extraModAnimalKeys.customRefreshNote.text",
                            "After adding/removing blocks, close and reopen this page to refresh the list."),
                        id: $"wildAnimals_extraModAnimalKeys_customRefresh_{Mikew.NestCore.Integrations.Gmcm.GmcmFieldIds.Normalize(key)}");

                    page.Separator(simple: true);
                    page.SectionLabel(() => Translate(i18n, "config.section.wildAnimals.extraModAnimalKeys.bulk.label", "Bulk edit (stage)"));

                    page.ParagraphDynamic(
                        () =>
                        {
                            if (!_extraModAnimalKeysBulkByWildAnimalKey.TryGetValue(key, out var st) || string.IsNullOrWhiteSpace(st.Raw))
                                return Translate(i18n, "config.paragraph.wildAnimals.extraModAnimalKeys.bulkHeader.empty", "Bulk: (none staged).");

                            var cfg = _config!.Get();

                            var fixTrim = cfg.GmcmExtraModAnimalKeysBulkFixTrimWhitespace;
                            var fixDedupe = cfg.GmcmExtraModAnimalKeysBulkFixRemoveDuplicates;
                            var fixDropUnknown = cfg.GmcmExtraModAnimalKeysBulkFixDropUnknownFarmAnimalKeys;

                            var knownFarmAnimalIds = fixDropUnknown ? GetKnownFarmAnimalIds() : null;

                            var enabledFixes = new List<string>();
                            if (fixTrim) enabledFixes.Add("trim");
                            if (fixDedupe) enabledFixes.Add("dedupe");
                            if (fixDropUnknown) enabledFixes.Add("drop-unknown");
                            var enabledFixesText = enabledFixes.Count == 0 ? "(none)" : string.Join(", ", enabledFixes);

                            var ovNow = GetOverride(cfg);

                            var plan = ComputeExtraModAnimalKeysBulkPlanCounts(
                                st,
                                extraModIdsForGmcm,
                                definition,
                                ovNow,
                                fixTrim,
                                fixDedupe,
                                knownFarmAnimalIds);

                            return Translate(
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
                        id: $"wildAnimals_extraModAnimalKeys_bulkHeader_{Mikew.NestCore.Integrations.Gmcm.GmcmFieldIds.Normalize(key)}");


                    page.TextLabel(
                        elementId: $"wildAnimals.{key}.extraModAnimalKeys.bulkInput",
                        name: () => Translate(i18n, "config.wildAnimals.extraModAnimalKeys.bulkInput.name", "Bulk paste (stage)"),
                        tooltip: () => Translate(
                            i18n,
                            "config.wildAnimals.extraModAnimalKeys.bulkInput.tooltip",
                            "Paste one line per mod-group: modId | csvKeys. You may comma-separate modIds (A,B | Cow). Use '*' (or blank) to reset/remove a block. This only stages changes."),
                        getValue: () =>
                            _extraModAnimalKeysBulkByWildAnimalKey.TryGetValue(key, out var st) ? st.Raw : "",
                        setValue: rawValue =>
                        {
                            var st = ParseExtraModAnimalKeysBulk(rawValue, extraModIdsForGmcm, GetKnownFarmAnimalIds());
                            _extraModAnimalKeysBulkByWildAnimalKey[key] = st;
                        });

                    page.ParagraphDynamic(
                        () =>
                        {
                            if (!_extraModAnimalKeysBulkByWildAnimalKey.TryGetValue(key, out var st) || string.IsNullOrWhiteSpace(st.Raw))
                                return Translate(i18n, "config.paragraph.wildAnimals.extraModAnimalKeys.bulkPreview.empty", "Bulk preview: (none)");

                            var cfg = _config!.Get();

                            var fixTrim = cfg.GmcmExtraModAnimalKeysBulkFixTrimWhitespace;
                            var fixDedupe = cfg.GmcmExtraModAnimalKeysBulkFixRemoveDuplicates;
                            var fixDropUnknown = cfg.GmcmExtraModAnimalKeysBulkFixDropUnknownFarmAnimalKeys;

                            var knownFarmAnimalIds = fixDropUnknown ? GetKnownFarmAnimalIds() : null;

                            var enabledFixes = new List<string>();
                            if (fixTrim) enabledFixes.Add("trim");
                            if (fixDedupe) enabledFixes.Add("dedupe");
                            if (fixDropUnknown) enabledFixes.Add("drop-unknown");
                            var enabledFixesText = enabledFixes.Count == 0 ? "(none)" : string.Join(", ", enabledFixes);

                            var ovNow = GetOverride(cfg);

                            var plan = ComputeExtraModAnimalKeysBulkPlanCounts(
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

                            var plannedSummary = Translate(i18n, "config.paragraph.wildAnimals.extraModAnimalKeys.bulkPreview.plan", "Planned: set {0}, clear {1}, no-op {2}.", plan.PlannedSet, plan.PlannedClear, plan.PlannedNoop);
                            var plannedIsNoop = !plan.AnyChangePlanned;
                            var lines = new List<string>
                            {
                                Translate(i18n, "config.paragraph.wildAnimals.extraModAnimalKeys.bulkPreview.header", "Bulk preview (not applied yet):"),
                                Translate(i18n, "config.paragraph.wildAnimals.extraModAnimalKeys.bulkPreview.note", "This is a preview only. Use the toggle below to apply staged changes."),
                                Translate(i18n, "config.paragraph.wildAnimals.extraModAnimalKeys.bulkPreview.summary", "Staged: {0} upserts, {1} resets, {2} parse warnings.", st.Upserts.Count, st.Resets.Count, st.Warnings.Count),
                                plannedSummary,
                                Translate(i18n, "config.paragraph.wildAnimals.extraModAnimalKeys.bulkPreview.fixes", "Quick-fixes enabled for apply: {0}.", enabledFixesText),
                                "",
                            };

                            if (plannedIsNoop)
                            {
                                lines.Add(Translate(i18n, "config.paragraph.wildAnimals.extraModAnimalKeys.bulkPreview.noop", "No config changes would be applied by Bulk Apply."));
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

                            var fixPreview = BuildExtraModAnimalKeysBulkFixPreviewLines(st.Raw, fixTrim, fixDedupe, fixDropUnknown, knownFarmAnimalIds);
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
                                    var canonicalModId = CanonicalizeModId(modId, extraModIdsForGmcm);
                                    var displayModId = string.Equals(modId, canonicalModId, StringComparison.OrdinalIgnoreCase)
                                        ? modId
                                        : $"{modId} => {canonicalModId}";

                                    var fixedKeys = ApplyFarmAnimalKeyFixes(
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

                                    var matchesBase = fixedKeys.Count > 0 && SequenceEqualIgnoreCase(fixedKeys, baseKeys);

                                    ExtraModAnimalKeysOverrideConfig? existingBlock = null;
                                    var hasOverrideNow = ovNow?.ExtraModAnimalKeys is not null
                                        && ovNow.ExtraModAnimalKeys.TryGetValue(canonicalModId, out existingBlock)
                                        && existingBlock is not null;

                                    var existingKeys = hasOverrideNow && existingBlock!.FarmAnimalKeys is { } ovKeys
                                        ? ovKeys
                                        : new List<string>();

                                    var willClear = fixedKeys.Count == 0 || matchesBase;
                                    var alreadySet = !willClear && hasOverrideNow && SequenceEqualIgnoreCase(existingKeys, fixedKeys);

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
                                    var canonicalModId = CanonicalizeModId(modIdRaw, extraModIdsForGmcm);
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
                        id: $"wildAnimals_extraModAnimalKeys_bulkPreview_{Mikew.NestCore.Integrations.Gmcm.GmcmFieldIds.Normalize(key)}");
        page.ParagraphDynamic(
                        () => Translate(
                            i18n,
                            "config.paragraph.wildAnimals.extraModAnimalKeys.bulkApply.note",
                            "Toggle on to apply staged bulk changes. This writes overrides immediately and clears the staged text."),
                        id: $"wildAnimals_extraModAnimalKeys_bulkApplyNote_{Mikew.NestCore.Integrations.Gmcm.GmcmFieldIds.Normalize(key)}");

                    page.Bool(cfg => cfg.GmcmExtraModAnimalKeysBulkApplySkipNoop, optionId: "wildAnimals.extraModAnimalKeys.bulkSkipNoop", tooltip: true);



                    page.Separator(simple: true);
                    page.SectionLabel(() => Translate(i18n, "config.section.wildAnimals.extraModAnimalKeys.bulkFixes.label", "Bulk apply quick-fixes"));

                    page.Bool(cfg => cfg.GmcmExtraModAnimalKeysBulkFixTrimWhitespace, optionId: "wildAnimals.extraModAnimalKeys.bulkFixes.trim", tooltip: true);
                    page.Bool(cfg => cfg.GmcmExtraModAnimalKeysBulkFixRemoveDuplicates, optionId: "wildAnimals.extraModAnimalKeys.bulkFixes.dedupe", tooltip: true);
                    page.Bool(cfg => cfg.GmcmExtraModAnimalKeysBulkFixDropUnknownFarmAnimalKeys, optionId: "wildAnimals.extraModAnimalKeys.bulkFixes.dropUnknown", tooltip: true);

        page.Bool(
                        optionId: Translate(i18n, "config.wildAnimals.extraModAnimalKeys.bulkApply.name", "Apply staged bulk changes"),
                        getValue: () => false,
                        setValue: v =>
                        {
                            if (!v)
                                return;

                            if (!_extraModAnimalKeysBulkByWildAnimalKey.TryGetValue(key, out var st) || string.IsNullOrWhiteSpace(st.Raw))
                                return;

                            var cfg = _config!.Get();
                            var skipNoop = cfg.GmcmExtraModAnimalKeysBulkApplySkipNoop;

                            var fixTrim = cfg.GmcmExtraModAnimalKeysBulkFixTrimWhitespace;
                            var fixDedupe = cfg.GmcmExtraModAnimalKeysBulkFixRemoveDuplicates;
                            var fixDropUnknown = cfg.GmcmExtraModAnimalKeysBulkFixDropUnknownFarmAnimalKeys;

                            var knownFarmAnimalIds = fixDropUnknown ? GetKnownFarmAnimalIds() : null;

                            var ovNow = GetOverride(cfg);

                            var plan = ComputeExtraModAnimalKeysBulkPlanCounts(
                                st,
                                extraModIdsForGmcm,
                                definition,
                                ovNow,
                                fixTrim,
                                fixDedupe,
                                knownFarmAnimalIds);

                            if (skipNoop && !plan.AnyChangePlanned)
                            {
                                Monitor.Log($"[GMCM][ExtraModAnimalKeys] Bulk Apply skipped (no-op) for '{key}'.", LogLevel.Trace);
                                return;
                            }
                            cfg.WildAnimalOverrides ??= new Dictionary<string, WildAnimalOverrideConfig>(StringComparer.OrdinalIgnoreCase);

                            var ov = ovNow ?? new WildAnimalOverrideConfig();
                            ov.ExtraModAnimalKeys ??= new Dictionary<string, ExtraModAnimalKeysOverrideConfig>(StringComparer.OrdinalIgnoreCase);

                            foreach (var modIdRaw in st.Resets)
                            {
                                var modId = CanonicalizeModId(modIdRaw, extraModIdsForGmcm);
                                ov.ExtraModAnimalKeys.Remove(modId);
                            }

                            foreach (var (modIdRaw, keys) in st.Upserts)
                            {
                                var modId = CanonicalizeModId(modIdRaw, extraModIdsForGmcm);
                                var finalKeys = ApplyFarmAnimalKeyFixes(keys, fixTrim, fixDedupe, knownFarmAnimalIds);

                                var baseKeys = definition?.ExtraModAnimalKeys is not null
                                    && definition.ExtraModAnimalKeys.TryGetValue(modId, out var baseBlock)
                                    ? baseBlock.GetEffectiveFarmAnimalKeys()
                                    : Array.Empty<string>();

                                if (finalKeys.Count == 0 || SequenceEqualIgnoreCase(finalKeys, baseKeys))
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

                            _extraModAnimalKeysBulkByWildAnimalKey.Remove(key);
                        },
                        tooltip: false,
                        fieldId: $"wildAnimals_extraModAnimalKeys_bulkApply_{Mikew.NestCore.Integrations.Gmcm.GmcmFieldIds.Normalize(key)}");


                    page.Separator(simple: true);
                    page.SectionLabel(() => Translate(i18n, "config.section.wildAnimals.extraModAnimalKeys.validation.label", "Validation warnings"));

                    page.ParagraphDynamic(
                        () =>
                        {
                            static HashSet<string>? TryLoadKnownFarmAnimalIdsOrNull()
                            {
                                try
                                {
                                    if (!Context.IsWorldReady)
                                        return null;

                                    var data = StardewValley.Game1.content.Load<Dictionary<string, FarmAnimalData>>("Data/FarmAnimals");
                                    return new HashSet<string>(data.Keys, StringComparer.OrdinalIgnoreCase);
                                }
                                catch
                                {
                                    return null;
                                }
                            }

                            var cfg = _config!.Get();
                            var ov = GetOverride(cfg);

                            var groups = new Dictionary<string, ExtraModAnimalKeysJson>(StringComparer.OrdinalIgnoreCase);
                            foreach (var modId in extraModIdsForGmcm)
                            {
                                List<string> raw;

                                if (ov?.ExtraModAnimalKeys is not null
                                    && ov.ExtraModAnimalKeys.TryGetValue(modId, out var blockOv)
                                    && blockOv?.FarmAnimalKeys is { Count: > 0 })
                                {
                                    raw = blockOv.FarmAnimalKeys;
                                }
                                else if (definition?.ExtraModAnimalKeys is not null
                                    && definition.ExtraModAnimalKeys.TryGetValue(modId, out var baseBlock)
                                    && baseBlock?.FarmAnimalKeys is { Count: > 0 })
                                {
                                    raw = baseBlock.FarmAnimalKeys;
                                }
                                else
                                {
                                    raw = new List<string>();
                                }

                                groups[modId] = new ExtraModAnimalKeysJson { FarmAnimalKeys = raw.ToList() };
                            }

                            var knownIds = TryLoadKnownFarmAnimalIdsOrNull();
                            var warnings = ExtraModAnimalKeysWarnings.Build(groups, modAvailability, knownIds);

                            if (warnings.Count == 0)
                            {
                                var none = Translate(i18n, "config.paragraph.wildAnimals.extraModAnimalKeys.validation.none", "Validation warnings: (none)");
                                if (knownIds is null)
                                {
                                    var note = Translate(
                                        i18n,
                                        "config.paragraph.wildAnimals.extraModAnimalKeys.validation.noteFarmAnimalsUnavailable",
                                        "Note: FarmAnimalKeys validation against Data/FarmAnimals is only available when a save is loaded.");
                                    return $"{none}{Environment.NewLine}{note}";
                                }

                                return none;
                            }

                            var lines = new List<string>
                            {
                                Translate(i18n, "config.paragraph.wildAnimals.extraModAnimalKeys.validation.header", "Validation warnings:"),
                            };

                            foreach (var w in warnings.Take(12))
                                lines.Add($" - {w}");

                            if (warnings.Count > 12)
                                lines.Add($" - ... (+{warnings.Count - 12} more)");

                            if (knownIds is null)
                                lines.Add(Translate(
                                    i18n,
                                    "config.paragraph.wildAnimals.extraModAnimalKeys.validation.noteFarmAnimalsUnavailable",
                                    "Note: FarmAnimalKeys validation against Data/FarmAnimals is only available when a save is loaded."));

                            return string.Join(Environment.NewLine, lines);
                        },
                        id: $"wildAnimals_extraModAnimalKeys_validation_{Mikew.NestCore.Integrations.Gmcm.GmcmFieldIds.Normalize(key)}");






        foreach (var modId in extraModIdsOrdered)
                    {
                        var elementId = $"wildAnimals.{key}.extraModAnimalKeys.{Mikew.NestCore.Integrations.Gmcm.GmcmFieldIds.Normalize(modId)}";

                        page.TextLabel(
                            elementId: elementId,
                            name: () => modId == "Vanilla"
                                ? Translate(i18n, "config.wildAnimals.extraModAnimalKeys.modId.vanilla.name", "Vanilla")
                                : modId,
                            tooltip: () =>
                            {
                                var loaded = modAvailability.IsLoaded(modId)
                                    ? Translate(i18n, "config.wildAnimals.state.loaded", "Loaded")
                                    : Translate(i18n, "config.wildAnimals.state.notLoaded", "Not loaded");

                                return Translate(
                                    i18n,
                                    "config.wildAnimals.extraModAnimalKeys.modId.tooltip",
                                    "FarmAnimalKeys for mod-group '{0}'. Mod is {1}.",
                                    modId,
                                    loaded);
                            },
                            getValue: () =>
                            {
                                var cfg = _config!.Get();
                                var ov = GetOverride(cfg);

                                if (ov?.ExtraModAnimalKeys is not null
                                    && ov.ExtraModAnimalKeys.TryGetValue(modId, out var blockOv)
                                    && blockOv?.FarmAnimalKeys is { Count: > 0 })
                                {
                                    return string.Join(", ", blockOv.FarmAnimalKeys);
                                }

                                var baseKeys = definition?.ExtraModAnimalKeys is not null
                                    && definition.ExtraModAnimalKeys.TryGetValue(modId, out var baseBlock)
                                    ? baseBlock.GetEffectiveFarmAnimalKeys()
                                    : Array.Empty<string>();

                                return baseKeys.Count == 0 ? "" : string.Join(", ", baseKeys);
                            },
                            setValue: rawValue =>
                            {
                                var parsed = ParseCsvKeyList(rawValue);
                                var fixedKeys = parsed;
                                if (_config!.Get().GmcmExtraModAnimalKeysBulkFixDropUnknownFarmAnimalKeys && Context.IsWorldReady && parsed.Count > 0)
                                {
                                    try
                                    {
                                        var data = StardewValley.Game1.content.Load<Dictionary<string, FarmAnimalData>>("Data/FarmAnimals");
                                        var known = new HashSet<string>(data.Keys, StringComparer.OrdinalIgnoreCase);
                                        fixedKeys = parsed.Where(x => known.Contains(x)).ToList();
                                    }
                                    catch
                                    {
                                    }
                                }


                                var baseKeys = definition?.ExtraModAnimalKeys is not null
                                    && definition.ExtraModAnimalKeys.TryGetValue(modId, out var baseBlock)
                                    ? baseBlock.GetEffectiveFarmAnimalKeys()
                                    : Array.Empty<string>();

                                var cfg = _config!.Get();
                                cfg.WildAnimalOverrides ??= new Dictionary<string, WildAnimalOverrideConfig>(StringComparer.OrdinalIgnoreCase);

                                if (!cfg.WildAnimalOverrides.TryGetValue(key, out var ov) || ov is null)
                                    ov = new WildAnimalOverrideConfig();

                                ov.ExtraModAnimalKeys ??= new Dictionary<string, ExtraModAnimalKeysOverrideConfig>(StringComparer.OrdinalIgnoreCase);

                                if (fixedKeys.Count == 0 || SequenceEqualIgnoreCase(fixedKeys, baseKeys))
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


                        page.BoolLabel(
                            elementId: $"{elementId}.reset",
                            name: () => Translate(i18n, "config.wildAnimals.extraModAnimalKeys.resetBlock.name", "Reset block to default"),
                            tooltip: () => Translate(
                                i18n,
                                "config.wildAnimals.extraModAnimalKeys.resetBlock.tooltip",
                                "Remove the override for mod-group '{0}'. For custom blocks this deletes the block. Close and reopen this page to refresh the list.",
                                modId),
                            getValue: () => false,
                            setValue: reset =>
                            {
                                if (!reset)
                                    return;

                                var cfg = _config!.Get();
                                var ov = GetOverride(cfg);
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
                    }

                    page.Separator(simple: true);
                    page.SectionLabel(() => Translate(i18n, "config.section.wildAnimals.diagnostics.label", "Diagnostics"));

                    page.ParagraphDynamic(
                        () =>
                        {
                            if (definition?.ExtraModAnimalKeys is null || definition.ExtraModAnimalKeys.Count == 0)
                                return Translate(i18n, "config.paragraph.wildAnimals.diagnostics.noSources.text", "ExtraModAnimalKeys: (none)");

                            var ordered = definition.ExtraModAnimalKeys
                                .OrderBy(kvp => kvp.Key == "Vanilla" ? "" : kvp.Key, StringComparer.OrdinalIgnoreCase);

                            var lines = new List<string>();
                            foreach (var (modId, keys) in ordered)
                            {
                                var loaded = modAvailability.IsLoaded(modId);
                                var raw = keys?.FarmAnimalKeys ?? new List<string>();
                                var effective = keys?.GetEffectiveFarmAnimalKeys() ?? Array.Empty<string>();

                                var rawJoined = raw.Count == 0 ? "" : string.Join(", ", raw);
                                var effJoined = effective.Count == 0 ? "" : string.Join(", ", effective);

                                var legacyNote = raw.Count == 0 && effective.Count > 0
                                    ? Translate(i18n, "config.wildAnimals.diagnostics.note.legacy", " (legacy fields)")
                                    : "";

                                lines.Add($"{modId}: loaded={loaded}; FarmAnimalKeys=[{rawJoined}]{legacyNote}; effective=[{effJoined}]");
                            }

                            var joined = string.Join(Environment.NewLine, lines);
                            return Translate(i18n, "config.paragraph.wildAnimals.diagnostics.sources.text", "ExtraModAnimalKeys sources:\n{0}", joined);
                        },
                        id: $"wildAnimals_diag_sources_{Mikew.NestCore.Integrations.Gmcm.GmcmFieldIds.Normalize(key)}");

                    page.ParagraphDynamic(
                        () =>
                        {
                            if (definition?.ExtraModAnimalKeys is null || definition.ExtraModAnimalKeys.Count == 0)
                                return Translate(i18n, "config.paragraph.wildAnimals.diagnostics.resolvedIds.empty.text", "Resolved FarmAnimalIds: (none)");

                            var ids = new List<string>();
                            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                            foreach (var (modId, keys) in definition.ExtraModAnimalKeys
                                .OrderBy(kvp => kvp.Key == "Vanilla" ? "" : kvp.Key, StringComparer.OrdinalIgnoreCase))
                            {
                                if (!modAvailability.IsLoaded(modId))
                                    continue;

                                foreach (var id in keys.GetEffectiveFarmAnimalKeys())
                                {
                                    if (seen.Add(id))
                                        ids.Add(id);
                                }
                            }

                            var joined = ids.Count == 0
                                ? Translate(i18n, "config.wildAnimals.value.none", "(none)")
                                : string.Join(", ", ids);

                            return Translate(i18n, "config.paragraph.wildAnimals.diagnostics.resolvedIds.text", "Resolved FarmAnimalIds (loaded mods): {0}", joined);
                        },
                        id: $"wildAnimals_diag_resolved_{Mikew.NestCore.Integrations.Gmcm.GmcmFieldIds.Normalize(key)}");

                    page.BoolLabel(
                        elementId: $"wildAnimals.{key}.reset",
                        name: () => Translate(i18n, "config.wildAnimals.reset.name", "Reset override"),
                        tooltip: () => Translate(i18n, "config.wildAnimals.reset.tooltip", "Clear configured override and use the JSON default for this animal."),
                        getValue: () => false,
                        setValue: value =>
                        {
                            if (!value)
                                return;

                            var cfg = _config!.Get();
                            cfg.WildAnimalOverrides?.Remove(key);
                        });
                });
        }

    }
}
