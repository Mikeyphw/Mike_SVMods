using System;
using System.Collections.Generic;
using System.Linq;
using EggsInWild.Application.Animals;
using EggsInWild.Infrastructure.Config;
using EggsInWild.Infrastructure.ModData;
using EggsInWild.Infrastructure.ModData.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mikew.NestCore.Helpers.Logging;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.FarmAnimals;

namespace EggsInWild.Infrastructure.Integrations.StardewUi.WildAnimals;

public sealed class WildAnimalsEditorService
{
    private readonly Func<Dictionary<string, WildAnimalJson>> _loadDefinitions;
    private readonly Action<Dictionary<string, WildAnimalJson>>? _saveDefinitions;
    private readonly Func<ModConfig> _getConfig;
    private readonly Func<IReadOnlyDictionary<string, FarmAnimalData>> _loadFarmAnimals;
    private readonly Func<string, WildAnimalJson, WildAnimalsEditorPreviewRecord?> _resolvePreview;
    private readonly Func<string, Texture2D?> _loadTexture;

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
        _saveDefinitions = definitions => helper.Data.WriteJsonFile(ModDataPaths.WildAnimalsList, definitions);
        _getConfig = getConfig;
        _loadFarmAnimals = () =>
        {
            try
            {
                return helper.GameContent.Load<Dictionary<string, FarmAnimalData>>("Data/FarmAnimals");
            }
            catch (Exception ex)
            {
                var smapiLog = new SmapiLog(monitor);
                smapiLog.LogScoped(ScopedLogCatalog.ModWeCategory.Content, $"Failed to load Data/FarmAnimals for WildAnimals editor previews: {ex}", LogLevel.Warn);
                return new Dictionary<string, FarmAnimalData>(StringComparer.OrdinalIgnoreCase);
            }
        };
        _resolvePreview = (wildAnimalKey, wildAnimal) =>
        {
            var farmAnimals = _loadFarmAnimals();
            var preview = WildAnimalPreviewResolver.TryResolvePreview(wildAnimalKey, wildAnimal, helper.ModRegistry, farmAnimals);
            return preview is null
                ? null
                : new WildAnimalsEditorPreviewRecord(preview.FarmAnimalKey, preview.TexturePath, preview.SourceRect, true, $"Preview: {preview.FarmAnimalKey}");
        };
        _loadTexture = texturePath =>
        {
            try
            {
                return Game1.content.Load<Texture2D>(texturePath);
            }
            catch
            {
                return null;
            }
        };
    }

    public WildAnimalsEditorService(
        Func<Dictionary<string, WildAnimalJson>> loadDefinitions,
        Func<ModConfig> getConfig,
        Func<IReadOnlyDictionary<string, FarmAnimalData>>? loadFarmAnimals = null,
        Func<string, WildAnimalJson, WildAnimalsEditorPreviewRecord?>? resolvePreview = null,
        Func<string, Texture2D?>? loadTexture = null,
        Action<Dictionary<string, WildAnimalJson>>? saveDefinitions = null)
    {
        _loadDefinitions = loadDefinitions ?? throw new ArgumentNullException(nameof(loadDefinitions));
        _getConfig = getConfig ?? throw new ArgumentNullException(nameof(getConfig));
        _saveDefinitions = saveDefinitions;
        _loadFarmAnimals = loadFarmAnimals ?? (() => new Dictionary<string, FarmAnimalData>(StringComparer.OrdinalIgnoreCase));
        _resolvePreview = resolvePreview ?? ((key, _) => new WildAnimalsEditorPreviewRecord(null, null, null, false, $"No preview available for {key}."));
        _loadTexture = loadTexture ?? (_ => null);
    }

    public WildAnimalsEditorCatalog LoadCatalog()
    {
        var definitions = _loadDefinitions();
        var config = _getConfig();
        var farmAnimals = _loadFarmAnimals();
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
            var effectiveDefinition = BuildEffectiveDefinition(definition, overrideConfig);

            var locationTypes = BuildLocationTypes(locationTypeKeys, definition, overrideConfig);
            var modGroups = BuildModGroups(definition, overrideConfig, farmAnimals);
            var preview = ResolvePreview(key, effectiveDefinition);
            var warningMessages = BuildWarnings(key, modGroups, preview);
            var hasValidEffectiveAnimalKeys = preview.HasPreview
                || modGroups.Any(group => group.EffectiveKeys.Count > group.UnknownEffectiveKeys.Count);

            animals.Add(new WildAnimalsEditorAnimalRecord(
                key,
                definition,
                CloneOverride(overrideConfig),
                WildAnimalsEditorHelpers.GetWildAnimalEffectiveEnabled(config, key, definition.Enabled),
                overrideConfig?.LargeEgg ?? definition.LargeEgg,
                overrideConfig?.Price ?? definition.Price,
                overrideConfig?.IsMagic ?? definition.IsMagic,
                locationTypes,
                modGroups,
                preview,
                hasValidEffectiveAnimalKeys,
                HasOverrides(overrideConfig),
                warningMessages.Count > 0,
                warningMessages));
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

    public WildAnimalDraftVisualState DescribeDraft(WildAnimalDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        var definition = BuildDefinitionFromDraft(draft);
        var farmAnimals = _loadFarmAnimals();
        var modGroups = BuildDraftVisualModGroups(draft, farmAnimals);
        var preview = ResolvePreview(draft.Key, definition);
        var warnings = BuildWarnings(draft.Key, modGroups, preview);

        return new WildAnimalDraftVisualState(preview, warnings, modGroups, warnings.Count > 0);
    }

    public Tuple<Texture2D, Rectangle>? TryLoadPreviewSprite(WildAnimalsEditorPreviewRecord preview)
    {
        ArgumentNullException.ThrowIfNull(preview);
        if (!preview.HasPreview || string.IsNullOrWhiteSpace(preview.TexturePath) || preview.SourceRect is null)
            return null;

        var texture = _loadTexture(preview.TexturePath);
        return texture is null ? null : Tuple.Create(texture, preview.SourceRect.Value);
    }

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

    public IReadOnlyList<WildAnimalFarmAnimalPickerItemRecord> LoadFarmAnimalPickerItems(IEnumerable<string>? alreadyReferencedKeys = null)
    {
        var referenced = alreadyReferencedKeys is null
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(alreadyReferencedKeys.Where(key => !string.IsNullOrWhiteSpace(key)), StringComparer.OrdinalIgnoreCase);
        var definitions = _loadDefinitions();
        var farmAnimals = _loadFarmAnimals();

        return farmAnimals
            .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair =>
            {
                var displayName = string.IsNullOrWhiteSpace(pair.Value.DisplayName?.ToString())
                    ? pair.Key
                    : pair.Value.DisplayName!.ToString();
                var suggestedModId = InferSuggestedModId(pair.Key, definitions);
                var tooltip = referenced.Contains(pair.Key)
                    ? $"{displayName}\n{pair.Key}\nAlready referenced"
                    : $"{displayName}\n{pair.Key}";

                return new WildAnimalFarmAnimalPickerItemRecord(
                    pair.Key,
                    displayName,
                    tooltip,
                    suggestedModId,
                    new WildAnimalsEditorPreviewRecord(
                        pair.Key,
                        pair.Value.Texture,
                        new Rectangle(0, 0, pair.Value.SpriteWidth, pair.Value.SpriteHeight),
                        !string.IsNullOrWhiteSpace(pair.Value.Texture) && pair.Value.SpriteWidth > 0 && pair.Value.SpriteHeight > 0,
                        $"Preview: {displayName}"));
            })
            .ToList();
    }

    public void CreateWildAnimalDefinition(string newKey, IReadOnlyList<string> farmAnimalKeys, IReadOnlyList<string> locationTypeKeys)
    {
        if (string.IsNullOrWhiteSpace(newKey))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(newKey));
        ArgumentNullException.ThrowIfNull(farmAnimalKeys);
        ArgumentNullException.ThrowIfNull(locationTypeKeys);
        EnsureCanWriteDefinitions();

        var definitions = _loadDefinitions();
        if (definitions.ContainsKey(newKey))
            throw new InvalidOperationException($"Wild animal '{newKey}' already exists.");

        var normalizedKeys = WildAnimalsEditorHelpers.ParseCsvKeyList(string.Join(", ", farmAnimalKeys)).ToList();
        if (normalizedKeys.Count == 0)
            throw new InvalidOperationException("Select at least one farm animal before creating a wild animal.");

        var modId = InferSuggestedModId(normalizedKeys[0], definitions);
        var locationTypes = locationTypeKeys
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToDictionary(key => key, _ => false, StringComparer.OrdinalIgnoreCase);

        var next = definitions.ToDictionary(pair => pair.Key, pair => CloneDefinition(pair.Value), StringComparer.OrdinalIgnoreCase);
        next[newKey] = new WildAnimalJson
        {
            Enabled = true,
            LargeEgg = false,
            Price = 0,
            IsMagic = false,
            LocationTypes = locationTypes,
            ExtraModAnimalKeys = new Dictionary<string, ExtraModAnimalKeysJson>(StringComparer.OrdinalIgnoreCase)
            {
                [modId] = new() { FarmAnimalKeys = normalizedKeys },
            },
        };

        _saveDefinitions!(next);
    }

    public void AddOrUpdateModGroupDefinition(string wildAnimalKey, string modId, IReadOnlyList<string> farmAnimalKeys)
    {
        if (string.IsNullOrWhiteSpace(wildAnimalKey))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(wildAnimalKey));
        if (string.IsNullOrWhiteSpace(modId))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(modId));
        ArgumentNullException.ThrowIfNull(farmAnimalKeys);
        EnsureCanWriteDefinitions();

        var definitions = _loadDefinitions();
        if (!definitions.TryGetValue(wildAnimalKey, out var definition))
            throw new InvalidOperationException($"Wild animal '{wildAnimalKey}' was not found.");

        var normalizedKeys = WildAnimalsEditorHelpers.ParseCsvKeyList(string.Join(", ", farmAnimalKeys)).ToList();
        if (normalizedKeys.Count == 0)
            throw new InvalidOperationException("Select at least one farm animal before adding a mod group.");

        var next = definitions.ToDictionary(pair => pair.Key, pair => CloneDefinition(pair.Value), StringComparer.OrdinalIgnoreCase);
        next[wildAnimalKey].ExtraModAnimalKeys[modId] = new ExtraModAnimalKeysJson
        {
            FarmAnimalKeys = normalizedKeys,
        };

        _saveDefinitions!(next);
    }

    private IReadOnlyList<WildAnimalsEditorLocationTypeRecord> BuildLocationTypes(
        IReadOnlyList<string> locationTypeKeys,
        WildAnimalJson definition,
        WildAnimalOverrideConfig? overrideConfig)
    {
        var result = new List<WildAnimalsEditorLocationTypeRecord>(locationTypeKeys.Count);
        foreach (var locationTypeKey in locationTypeKeys)
        {
            var baseAllowed = definition.LocationTypes.TryGetValue(locationTypeKey, out var baseValue) && baseValue;
            var overrideAllowed = overrideConfig?.LocationTypes is not null
                && overrideConfig.LocationTypes.TryGetValue(locationTypeKey, out var overrideValue)
                ? overrideValue
                : (bool?)null;

            result.Add(new WildAnimalsEditorLocationTypeRecord(
                locationTypeKey,
                baseAllowed,
                overrideAllowed,
                overrideAllowed ?? baseAllowed));
        }

        return result;
    }

    private static IReadOnlyList<WildAnimalsEditorModGroupRecord> BuildModGroups(
        WildAnimalJson definition,
        WildAnimalOverrideConfig? overrideConfig,
        IReadOnlyDictionary<string, FarmAnimalData> farmAnimals)
    {
        var modIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var modId in definition.ExtraModAnimalKeys.Keys)
            modIds.Add(modId);

        if (overrideConfig?.ExtraModAnimalKeys is not null)
        {
            foreach (var modId in overrideConfig.ExtraModAnimalKeys.Keys)
                modIds.Add(modId);
        }

        return modIds
            .OrderBy(modId => modId, StringComparer.OrdinalIgnoreCase)
            .Select(modId =>
            {
                var baseKeys = definition.ExtraModAnimalKeys.TryGetValue(modId, out var baseBlock)
                    ? baseBlock.GetEffectiveFarmAnimalKeys()
                    : Array.Empty<string>();

                var overrideKeys = overrideConfig?.ExtraModAnimalKeys is not null
                    && overrideConfig.ExtraModAnimalKeys.TryGetValue(modId, out var overrideBlock)
                    && overrideBlock?.FarmAnimalKeys is { Count: > 0 } configuredKeys
                        ? WildAnimalsEditorHelpers.ParseCsvKeyList(string.Join(", ", configuredKeys)).ToList()
                        : null;

                var effectiveKeys = overrideKeys is { Count: > 0 } ? overrideKeys : baseKeys;
                var unknownKeys = effectiveKeys
                    .Where(key => !farmAnimals.ContainsKey(key))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return new WildAnimalsEditorModGroupRecord(modId, baseKeys, overrideKeys, effectiveKeys, unknownKeys, unknownKeys.Count > 0);
            })
            .ToList();
    }

    private static IReadOnlyList<WildAnimalDraftModGroupVisualState> BuildDraftVisualModGroups(
        WildAnimalDraft draft,
        IReadOnlyDictionary<string, FarmAnimalData> farmAnimals)
    {
        return draft.ModGroups
            .Where(group => !string.IsNullOrWhiteSpace(group.ModId))
            .Select(group =>
            {
                var keys = WildAnimalsEditorHelpers.ParseCsvKeyList(string.Join(", ", group.Keys)).ToList();
                var unknownKeys = keys
                    .Where(key => !farmAnimals.ContainsKey(key))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return new WildAnimalDraftModGroupVisualState(group.ModId, keys, unknownKeys, unknownKeys.Count > 0);
            })
            .ToList();
    }

    private WildAnimalsEditorPreviewRecord ResolvePreview(string wildAnimalKey, WildAnimalJson wildAnimal)
    {
        return _resolvePreview(wildAnimalKey, wildAnimal)
            ?? new WildAnimalsEditorPreviewRecord(null, null, null, false, $"No preview available for {wildAnimalKey}.");
    }

    private static IReadOnlyList<string> BuildWarnings(
        string key,
        IEnumerable<WildAnimalsEditorModGroupRecord> modGroups,
        WildAnimalsEditorPreviewRecord preview)
    {
        var warnings = new List<string>();
        foreach (var group in modGroups.Where(group => group.HasWarnings))
            warnings.Add($"{group.ModId}: unknown farm animal keys [{string.Join(", ", group.UnknownEffectiveKeys)}]");

        if (!preview.HasPreview)
            warnings.Add($"No farm-animal sprite preview could be resolved for {key}.");

        return warnings;
    }

    private static IReadOnlyList<string> BuildWarnings(
        string key,
        IEnumerable<WildAnimalDraftModGroupVisualState> modGroups,
        WildAnimalsEditorPreviewRecord preview)
    {
        var warnings = new List<string>();
        foreach (var group in modGroups.Where(group => group.HasWarnings))
            warnings.Add($"{group.ModId}: unknown farm animal keys [{string.Join(", ", group.UnknownKeys)}]");

        if (!preview.HasPreview)
            warnings.Add($"No farm-animal sprite preview could be resolved for {key}.");

        return warnings;
    }

    private static bool HasOverrides(WildAnimalOverrideConfig? overrideConfig)
    {
        return overrideConfig is not null && !WildAnimalsEditorHelpers.IsEmptyOverride(overrideConfig);
    }

    private static WildAnimalJson BuildEffectiveDefinition(WildAnimalJson definition, WildAnimalOverrideConfig? overrideConfig)
    {
        var clone = CloneDefinition(definition);
        if (overrideConfig is null)
            return clone;

        clone.Enabled = overrideConfig.Enabled ?? clone.Enabled;
        clone.LargeEgg = overrideConfig.LargeEgg ?? clone.LargeEgg;
        clone.Price = overrideConfig.Price ?? clone.Price;
        clone.IsMagic = overrideConfig.IsMagic ?? clone.IsMagic;

        if (overrideConfig.LocationTypes is not null)
        {
            foreach (var pair in overrideConfig.LocationTypes)
                clone.LocationTypes[pair.Key] = pair.Value;
        }

        if (overrideConfig.ExtraModAnimalKeys is not null)
        {
            foreach (var pair in overrideConfig.ExtraModAnimalKeys)
            {
                clone.ExtraModAnimalKeys[pair.Key] = new ExtraModAnimalKeysJson
                {
                    FarmAnimalKeys = pair.Value.FarmAnimalKeys is null
                        ? new List<string>()
                        : WildAnimalsEditorHelpers.ParseCsvKeyList(string.Join(", ", pair.Value.FarmAnimalKeys)).ToList(),
                };
            }
        }

        return clone;
    }

    private static WildAnimalJson BuildDefinitionFromDraft(WildAnimalDraft draft)
    {
        return new WildAnimalJson
        {
            Enabled = draft.Enabled,
            LargeEgg = draft.LargeEgg,
            Price = draft.Price,
            IsMagic = draft.IsMagic,
            LocationTypes = draft.LocationTypes.ToDictionary(location => location.Key, location => location.Allowed, StringComparer.OrdinalIgnoreCase),
            ExtraModAnimalKeys = draft.ModGroups
                .Where(group => !string.IsNullOrWhiteSpace(group.ModId))
                .ToDictionary(
                    group => group.ModId,
                    group => new ExtraModAnimalKeysJson
                    {
                        FarmAnimalKeys = WildAnimalsEditorHelpers.ParseCsvKeyList(string.Join(", ", group.Keys)).ToList(),
                    },
                    StringComparer.OrdinalIgnoreCase),
        };
    }

    private static WildAnimalJson CloneDefinition(WildAnimalJson definition)
    {
        return new WildAnimalJson
        {
            Enabled = definition.Enabled,
            LargeEgg = definition.LargeEgg,
            Price = definition.Price,
            IsMagic = definition.IsMagic,
            LocationTypes = definition.LocationTypes.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase),
            ExtraModAnimalKeys = definition.ExtraModAnimalKeys.ToDictionary(
                pair => pair.Key,
                pair => new ExtraModAnimalKeysJson
                {
                    FarmAnimalKeys = pair.Value.GetEffectiveFarmAnimalKeys().ToList(),
                },
                StringComparer.OrdinalIgnoreCase),
        };
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

    private static string InferSuggestedModId(string farmAnimalKey, IReadOnlyDictionary<string, WildAnimalJson> definitions)
    {
        foreach (var definition in definitions.Values)
        {
            foreach (var pair in definition.ExtraModAnimalKeys)
            {
                if (pair.Value.GetEffectiveFarmAnimalKeys().Contains(farmAnimalKey, StringComparer.OrdinalIgnoreCase))
                    return pair.Key;
            }
        }

        return "Vanilla";
    }

    private void EnsureCanWriteDefinitions()
    {
        if (_saveDefinitions is null)
            throw new InvalidOperationException("WildAnimalsList authoring is not available in this editor context.");
    }
}
