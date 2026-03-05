using System;
using System.Collections.Generic;
using System.Linq;
using EggsInWild.Infrastructure.Config;
using EggsInWild.Infrastructure.ModData;
using EggsInWild.Infrastructure.ModData.Models;
using Mikew.NestCore.Helpers.Logging;
using StardewModdingAPI;

namespace EggsInWild.Infrastructure.Integrations.StardewUi.WildAnimals;

public sealed class WildAnimalsEditorService
{
    private readonly Func<Dictionary<string, WildAnimalJson>> _loadDefinitions;
    private readonly Func<ModConfig> _getConfig;

    public WildAnimalsEditorService(IModHelper helper, IMonitor monitor, Func<ModConfig> getConfig)
    {
        ArgumentNullException.ThrowIfNull(helper);
        ArgumentNullException.ThrowIfNull(monitor);
        ArgumentNullException.ThrowIfNull(getConfig);

        _loadDefinitions = () =>
        {
            try
            {
                var reader = new SmapiJsonFileReader(helper);
                return reader.ReadJsonFile<Dictionary<string, WildAnimalJson>>(ModDataPaths.WildAnimalsList)
                    ?? new Dictionary<string, WildAnimalJson>(StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                var smapiLog = new SmapiLog(monitor);
                smapiLog.LogScoped(ScopedLogCatalog.ModWeCategory.Content, $"Failed to load {ModDataPaths.WildAnimalsList} for WildAnimals editor: {ex}", LogLevel.Error);
                return new Dictionary<string, WildAnimalJson>(StringComparer.OrdinalIgnoreCase);
            }
        };
        _getConfig = getConfig;
    }

    public WildAnimalsEditorService(Func<Dictionary<string, WildAnimalJson>> loadDefinitions, Func<ModConfig> getConfig)
    {
        _loadDefinitions = loadDefinitions ?? throw new ArgumentNullException(nameof(loadDefinitions));
        _getConfig = getConfig ?? throw new ArgumentNullException(nameof(getConfig));
    }

    public WildAnimalsEditorCatalog LoadCatalog()
    {
        var definitions = _loadDefinitions();

        var config = _getConfig();
        var keys = definitions.Keys
            .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var locationTypeKeys = definitions.Values
            .SelectMany(definition => definition.LocationTypes?.Keys ?? Enumerable.Empty<string>())
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(key => string.Equals(key, "Farm", StringComparison.OrdinalIgnoreCase) ? "" : key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var animals = new List<WildAnimalsEditorAnimalRecord>(keys.Count);
        foreach (var key in keys)
        {
            var definition = definitions[key];
            config.WildAnimalOverrides.TryGetValue(key, out var overrideConfig);

            var modIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var modId in definition.ExtraModAnimalKeys.Keys)
                modIds.Add(modId);

            if (overrideConfig?.ExtraModAnimalKeys is not null)
            {
                foreach (var modId in overrideConfig.ExtraModAnimalKeys.Keys)
                    modIds.Add(modId);
            }

            var locationTypes = new List<WildAnimalsEditorLocationTypeRecord>(locationTypeKeys.Count);
            foreach (var locationTypeKey in locationTypeKeys)
            {
                var baseAllowed = definition.LocationTypes.TryGetValue(locationTypeKey, out var baseValue) && baseValue;
                var overrideAllowed = overrideConfig?.LocationTypes is not null
                    && overrideConfig.LocationTypes.TryGetValue(locationTypeKey, out var overrideValue)
                    ? overrideValue
                    : (bool?)null;

                locationTypes.Add(new WildAnimalsEditorLocationTypeRecord(
                    locationTypeKey,
                    baseAllowed,
                    overrideAllowed,
                    overrideAllowed ?? baseAllowed));
            }

            var modGroups = modIds
                .OrderBy(modId => modId, StringComparer.OrdinalIgnoreCase)
                .Select(modId =>
                {
                    var baseKeys = definition.ExtraModAnimalKeys.TryGetValue(modId, out var baseBlock)
                        ? baseBlock.GetEffectiveFarmAnimalKeys()
                        : Array.Empty<string>();

                    var overrideKeys = overrideConfig?.ExtraModAnimalKeys is not null
                        && overrideConfig.ExtraModAnimalKeys.TryGetValue(modId, out var overrideBlock)
                        && overrideBlock?.FarmAnimalKeys is { } configuredKeys
                        ? configuredKeys
                        : null;

                    var effectiveKeys = overrideKeys is { Count: > 0 } ? overrideKeys : baseKeys;
                    return new WildAnimalsEditorModGroupRecord(modId, baseKeys, overrideKeys, effectiveKeys);
                })
                .ToList();

            animals.Add(new WildAnimalsEditorAnimalRecord(
                key,
                definition,
                CloneOverride(overrideConfig),
                WildAnimalsEditorHelpers.GetWildAnimalEffectiveEnabled(config, key, definition.Enabled),
                overrideConfig?.LargeEgg ?? definition.LargeEgg,
                overrideConfig?.Price ?? definition.Price,
                overrideConfig?.IsMagic ?? definition.IsMagic,
                locationTypes,
                modGroups));
        }

        return new WildAnimalsEditorCatalog(definitions, animals, locationTypeKeys);
    }

    public WildAnimalDraft CreateDraft(string wildAnimalKey)
    {
        var catalog = LoadCatalog();
        var animal = catalog.Animals.FirstOrDefault(entry => string.Equals(entry.Key, wildAnimalKey, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Unknown WildAnimals editor key '{wildAnimalKey}'.");

        return new WildAnimalDraft(
            animal.Key,
            animal.Definition.Enabled,
            animal.EffectiveEnabled,
            animal.Definition.LargeEgg,
            animal.EffectiveLargeEgg,
            animal.Definition.Price,
            animal.EffectivePrice,
            animal.Definition.IsMagic,
            animal.EffectiveIsMagic,
            animal.LocationTypes.Select(record => new WildAnimalDraftLocationType(record.Key, record.BaseAllowed, record.EffectiveAllowed)).ToList(),
            animal.ModGroups.Select(record => new WildAnimalDraftModGroup(record.ModId, record.BaseKeys.ToList(), record.EffectiveKeys.ToList())).ToList());
    }

    public WildAnimalDraft DiscardDraft(string wildAnimalKey)
        => CreateDraft(wildAnimalKey);

    public void ApplyDraft(ModConfig cfg, WildAnimalDraft draft)
    {
        ArgumentNullException.ThrowIfNull(cfg);
        ArgumentNullException.ThrowIfNull(draft);

        cfg.WildAnimalOverrides ??= new Dictionary<string, WildAnimalOverrideConfig>(StringComparer.OrdinalIgnoreCase);
        cfg.WildAnimalOverrides.TryGetValue(draft.Key, out var overrideConfig);
        overrideConfig ??= new WildAnimalOverrideConfig();

        overrideConfig.Enabled = draft.Enabled == draft.BaseEnabled ? null : draft.Enabled;
        overrideConfig.LargeEgg = draft.LargeEgg == draft.BaseLargeEgg ? null : draft.LargeEgg;
        overrideConfig.Price = draft.Price == draft.BasePrice ? null : draft.Price;
        overrideConfig.IsMagic = draft.IsMagic == draft.BaseIsMagic ? null : draft.IsMagic;

        var locationOverrides = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        foreach (var locationType in draft.LocationTypes)
        {
            if (locationType.Allowed == locationType.BaseAllowed)
                continue;

            locationOverrides[locationType.Key] = locationType.Allowed;
        }

        overrideConfig.LocationTypes = locationOverrides.Count == 0 ? null : locationOverrides;

        var modGroupOverrides = new Dictionary<string, ExtraModAnimalKeysOverrideConfig>(StringComparer.OrdinalIgnoreCase);
        foreach (var modGroup in draft.ModGroups)
        {
            var finalKeys = WildAnimalsEditorHelpers.ParseCsvKeyList(string.Join(", ", modGroup.Keys));
            if (finalKeys.Count == 0 || WildAnimalsEditorHelpers.SequenceEqualIgnoreCase(finalKeys, modGroup.BaseKeys))
                continue;

            modGroupOverrides[modGroup.ModId] = new ExtraModAnimalKeysOverrideConfig
            {
                FarmAnimalKeys = finalKeys.ToList(),
            };
        }

        overrideConfig.ExtraModAnimalKeys = modGroupOverrides.Count == 0 ? null : modGroupOverrides;

        if (WildAnimalsEditorHelpers.IsEmptyOverride(overrideConfig))
        {
            cfg.WildAnimalOverrides.Remove(draft.Key);
            return;
        }

        cfg.WildAnimalOverrides[draft.Key] = overrideConfig;
    }

    private static WildAnimalOverrideConfig? CloneOverride(WildAnimalOverrideConfig? source)
    {
        if (source is null)
            return null;

        return new WildAnimalOverrideConfig
        {
            Enabled = source.Enabled,
            LargeEgg = source.LargeEgg,
            Price = source.Price,
            IsMagic = source.IsMagic,
            LocationTypes = source.LocationTypes is null
                ? null
                : new Dictionary<string, bool>(source.LocationTypes, StringComparer.OrdinalIgnoreCase),
            ExtraModAnimalKeys = source.ExtraModAnimalKeys is null
                ? null
                : source.ExtraModAnimalKeys.ToDictionary(
                    pair => pair.Key,
                    pair => new ExtraModAnimalKeysOverrideConfig
                    {
                        FarmAnimalKeys = pair.Value.FarmAnimalKeys is null ? null : new List<string>(pair.Value.FarmAnimalKeys),
                    },
                    StringComparer.OrdinalIgnoreCase),
        };
    }
}
