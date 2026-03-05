using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mikew.NestCore.Integrations.Gmcm;
using RoosterControl.Application.Compat;
using RoosterControl.Application.Hatching;
using RoosterControl.Constants;
using RoosterControl.Enums;
using RoosterControl.Framework.Config;
using RoosterControl.Integrations.Gmcm.Pages;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.FarmAnimals;
using StardewValley.ItemTypeDefinitions;
using Mikew.NestCore.Helpers.Logging;

using ILetMeHatchApi = Mikew.NestCore.Framework.ILetMeHatchApi;
using static Mikew.NestCore.Helpers.Logging.ScopedLogCatalog;

namespace RoosterControl.Application;

internal sealed partial class RoosterCoordinator
{
    private GmcmConfigHost<RoosterControlConfig>? _gmcmHost;
    private bool _gmcmRebuildQueued;
    private string _compatSourcesSummary = string.Empty;
    private string _compatValidationSummary = string.Empty;
    private DateTimeOffset? _compatValidationLastRunAt;
    private int _compatValidationWarnings;
    private int _compatValidationErrors;
    private string _compatValidationLastReason = string.Empty;
    private bool _gmcmResetAllArmed;
    private bool _gmcmResetEggSourcesArmed;
    private bool _gmcmResetRoosterProvidersArmed;

    public void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        HatchApi =
            Helper.ModRegistry.GetApi<ILetMeHatchApi>("Mikew.lmh")
            ?? Helper.ModRegistry.GetApi<ILetMeHatchApi>("Mikew.letmehatch.smapi");

        if (!Helper.ModRegistry.IsLoaded("Mikew.nestcore"))
            throw new InvalidOperationException("NestCore is required but was not found.");

        _gmcmHost = GmcmConfigHost<RoosterControlConfig>.TryCreate(
            helper: Helper,
            manifest: ModManifest,
            getConfig: () => Config,
            reset: ResetConfig,
            save: SaveConfig
        );

        if (_gmcmHost is null)
            return;

        _gmcmHost.RebuildLayout(
            define: DefineGmcmLayout,
            fieldIdPrefix: ModManifest.UniqueID,
            titleScreenOnly: false,
            force: true,
            attachLocaleHandler: true
        );
    }

    public void OnLocaleChanged(object? sender, LocaleChangedEventArgs e)
    {
        // GMCM rebuild is handled by NestCore when attachLocaleHandler=true.
        RequestInvalidate(FarmAnimalsAssetName);
        RequestInvalidate(ObjectsAssetName);
    }

    private void ResetConfig()
    {
        Config = RoosterControlConfig.CreateDefault();

        // Treat a GMCM reset as "new config" for catalog seeding.
        _catalogs = _catalogManager.LoadOrCreateAll(Helper, Monitor);
        var changed = _catalogManager.ApplyToConfig(Config, _catalogs, isNewConfig: true, Config.General.CatalogApplyMode, Monitor);
        if (changed)
            ConfigManager.Save(Helper, Config);

        if (Context.IsWorldReady && TryLoadFarmAnimals(out var farmAnimals, out _))
            EnsureCompatValidationState(farmAnimals, reason: "gmcm save", log: true, category: ModRoosterControlCategory.Gmcm);

        InvalidateDailyPlanCache("config reset");
        _needsRoosterSetup = true;
        RequestInvalidate(FarmAnimalsAssetName);
        RequestInvalidate(ObjectsAssetName);

        RequestGmcmRebuild();
    }

    private void SaveConfig()
    {
        var didReset = TryApplyGmcmResetsOnSave();

        ConfigManager.Save(Helper, Config);

        if (Context.IsWorldReady && TryLoadFarmAnimals(out var farmAnimals, out _))
            EnsureCompatValidationState(farmAnimals, reason: "gmcm save", log: true, category: ModRoosterControlCategory.Gmcm);

        InvalidateDailyPlanCache(didReset ? "config saved (after reset)" : "config saved");
        _needsRoosterSetup = true;
        RequestInvalidate(FarmAnimalsAssetName);
        RequestInvalidate(ObjectsAssetName);

        if (didReset)
            RequestGmcmRebuild();
    }

    private void DefineGmcmLayout(GmcmMenuLayout<RoosterControlConfig> layout)
    {
        layout.Root(RoosterRootPage.Build);

        layout.Page("compat", BuildCompatPage);

        layout.Page("probabilities", RoosterProbabilitiesPage.Build);

        layout.Page("females", BuildFemalesPage);

        layout.Page("debug", RoosterDebugPage.Build);

        RoosterTypePages.Define(layout, this);
    }

private const string AllRoosterProvidersValue = "__ALL__";

private void BuildCompatPage(GmcmPageBuilder<RoosterControlConfig> b)
{
    b.Section("compat.settings");
    b.Bool(cfg => cfg.General.BetterTrufflesCompat, optionId: "compat.betterTrufflesCompat", tooltip: true);
    b.Bool(cfg => cfg.General.EnableStuckInside, optionId: "compat.enableStuckInside", tooltip: true);

    b.Separator(simple: true);

    b.Section("compat.sources");

    if (!Context.IsWorldReady)
    {
        b.Paragraph("compat.sources.not_ready");
        return;
    }

    EnsureCompatInitialized();
    EnsureRoostersSetup();

    _invalidations.Flush();

    Dictionary<string, FarmAnimalData> data;
    try
    {
        data = Helper.GameContent.Load<Dictionary<string, FarmAnimalData>>(FarmAnimalsAssetName);
    }
    catch (Exception ex)
    {
        LogScopedHelper.Log(_log, ModRoosterControlCategory.Gmcm, $"Failed to load {FarmAnimalsAssetName} for GMCM compat sources page: {ex}", LogLevel.Error);
        b.Paragraph("compat.sources.error");
        return;
    }

    b.Paragraph("compat.sources.help");

    b.ParagraphGmcmOptionsStatus();
    if (!b.SupportsExtraOptions)
    {
        b.Paragraph("compat.sources.fallback_help");
        b.Separator(id: "compat_sources_fallback_sep", simple: true);
    }

    _compatSourcesSummary = BuildCompatSourcesSummary(data);


    b.Separator(simple: true);

    b.Section("compat.validation");
    b.Paragraph("compat.validation.help");
    EnsureCompatValidationState(data, reason: "gmcm page", log: false, category: ModRoosterControlCategory.Gmcm);
    b.ParagraphDynamic(() => _compatValidationSummary, styled: true, id: "compat_validation_summary");


    b.Separator(simple: true);

    b.Section("compat.reset");
    b.Paragraph("compat.reset.help");

    b.Bool(
        "compat.reset.resetAll",
        () => _gmcmResetAllArmed,
        v => _gmcmResetAllArmed = v,
        tooltip: true,
        fieldId: $"{ModManifest.UniqueID}.compat.reset.all"
    );

    b.Bool(
        "compat.reset.resetEggSources",
        () => _gmcmResetEggSourcesArmed,
        v => _gmcmResetEggSourcesArmed = v,
        tooltip: true,
        fieldId: $"{ModManifest.UniqueID}.compat.reset.eggs"
    );

    b.Bool(
        "compat.reset.resetRoosterProviders",
        () => _gmcmResetRoosterProvidersArmed,
        v => _gmcmResetRoosterProvidersArmed = v,
        tooltip: true,
        fieldId: $"{ModManifest.UniqueID}.compat.reset.roosters"
    );
    b.ParagraphDynamic(() => _compatSourcesSummary, styled: true, id: "compat_sources_summary");

    b.Separator("compat_sources_sep_eggs", simple: true);
    b.Section("compat.sources.eggs");

    var eggGroups = new (string section, EggKind[] kinds)[]
    {
        ("compat.sources.eggs.standard", new[]
        {
            EggKind.BrownEgg, EggKind.WhiteEgg, EggKind.BlueEgg, EggKind.VoidEgg, EggKind.GoldEgg
        }),
        ("compat.sources.eggs.large", new[]
        {
            EggKind.LargeBrownEgg, EggKind.LargeWhiteEgg, EggKind.LargeBlueEgg, EggKind.LargeVoidEgg, EggKind.LargeGoldEgg
        }),
        ("compat.sources.eggs.fertilized", new[]
        {
            EggKind.FertilizedBrownEgg, EggKind.FertilizedWhiteEgg, EggKind.FertilizedBlueEgg, EggKind.FertilizedVoidEgg, EggKind.FertilizedGoldEgg
        }),
        ("compat.sources.eggs.special", new[]
        {
            EggKind.RoosterEgg
        }),
    };

    var totalKinds = eggGroups.Sum(g => g.kinds.Length);
    var kindIndex = 0;

    void AddEggKindPicker(EggKind kind, bool addSeparatorAfter)
    {
        var allowed = _compatSelection.EggCatalog.Sources
            .Where(s => s.EggKind == kind && !string.IsNullOrWhiteSpace(s.SourceId))
            .Where(s => _compatSelection.TryResolveEggSourceItemId(s, data, out _, out _))
            .Select(s => s.SourceId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var displayNames = _compatSelection.EggCatalog.Sources
            .Where(s => s.EggKind == kind && !string.IsNullOrWhiteSpace(s.SourceId))
            .GroupBy(s => s.SourceId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().DisplayName ?? string.Empty, StringComparer.OrdinalIgnoreCase);

        allowed = allowed
            .OrderBy(id => displayNames.TryGetValue(id, out var name) && !string.IsNullOrWhiteSpace(name) ? name : id, StringComparer.OrdinalIgnoreCase)
            .ThenBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (allowed.Length == 0)
        {
            b.ParagraphDynamic(() => _i18n.GetString("config.compatSources.egg.noValidSourcesFound", new { eggKind = kind }));
            if (addSeparatorAfter)
                b.Separator(id: $"compat_egg_sep_{kind}", simple: true);
            return;
        }

        if (b.SupportsExtraOptions && TryBuildEggSourceChoices(kind, allowed, data, out var eggChoices))
        {
            b.ImagePicker(
                elementId: $"compat_sources_egg_{kind}",
                name: () => _i18n.GetString($"config.compatSources.egg.{kind}.name"),
                tooltip: () => _i18n.GetString($"config.compatSources.egg.{kind}.description"),
                getValue: () => GetSelectedEggSourceIndex(kind, allowed[0], allowed),
                setValue: v => SetSelectedEggSourceIndex(kind, allowed, v),
                choices: () => eggChoices,
                fieldId: $"{ModManifest.UniqueID}.compat.sources.egg.{kind}"
            );
        }
        else
        {
            b.TextLabel(
                elementId: $"compat_sources_egg_{kind}",
                name: () => _i18n.GetString($"config.compatSources.egg.{kind}.name"),
                tooltip: () => _i18n.GetString($"config.compatSources.egg.{kind}.description"),
                getValue: () => GetSelectedEggSourceId(kind, allowed[0]),
                setValue: v => SetSelectedEggSourceId(kind, v),
                allowedValues: allowed,
                formatValue: FormatEggSourceValue,
                fieldId: $"{ModManifest.UniqueID}.compat.sources.egg.{kind}"
            );
        }

        b.ParagraphDynamic(
            text: () => BuildEggSelectionValidationNote(kind, data),
            styled: true,
            id: $"compat_egg_note_{kind}"
        );

        if (addSeparatorAfter)
            b.Separator(id: $"compat_egg_sep_{kind}", simple: true);
    }

    foreach (var group in eggGroups)
    {
        b.Section(group.section);

        foreach (var kind in group.kinds)
        {
            kindIndex++;
            AddEggKindPicker(kind, addSeparatorAfter: kindIndex < totalKinds);
        }

        if (kindIndex < totalKinds)
            b.Separator(id: $"compat_egg_group_sep_{group.section}", simple: true);
    }

    b.Separator("compat_sources_sep_roosters", simple: true);
    b.Section("compat.sources.roosters");

    var roosterProviderIds = _compatSelection.RoosterCatalog.Sources
        .Where(s => !string.IsNullOrWhiteSpace(s.SourceId))
        .Select(s => s.SourceId)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();

    var roosterDisplayNames = _compatSelection.RoosterCatalog.Sources
        .Where(s => !string.IsNullOrWhiteSpace(s.SourceId))
        .GroupBy(s => s.SourceId, StringComparer.OrdinalIgnoreCase)
        .ToDictionary(g => g.Key, g => g.First().DisplayName ?? string.Empty, StringComparer.OrdinalIgnoreCase);

    roosterProviderIds = roosterProviderIds
        .OrderBy(id => roosterDisplayNames.TryGetValue(id, out var name) && !string.IsNullOrWhiteSpace(name) ? name : id, StringComparer.OrdinalIgnoreCase)
        .ThenBy(id => id, StringComparer.OrdinalIgnoreCase)
        .ToList();

    if (roosterProviderIds.Count == 0)
    {
        b.Paragraph("compat.sources.roosters.noProvidersFound");
    }
    else
    {
        b.Bool(
            "compatSources.roosters.useAll",
            GetUseAllRoosterProviders,
            SetUseAllRoosterProviders,
            tooltip: true,
            fieldId: $"{ModManifest.UniqueID}.compat.sources.roosters.useAll"
        );

        var roosterAllowedArray = roosterProviderIds.ToArray();

        if (b.SupportsExtraOptions && TryBuildRoosterProviderChoices(roosterAllowedArray, out var roosterChoices))
        {
            b.ImagePicker(
                elementId: "compat_sources_roosters",
                name: () => _i18n.GetString("config.compatSources.roosters.name"),
                tooltip: () => _i18n.GetString("config.compatSources.roosters.description"),
                getValue: () => GetSelectedRoosterProviderIndex(roosterAllowedArray),
                setValue: v => SetSelectedRoosterProviderIndex(roosterAllowedArray, v),
                choices: () => roosterChoices,
                fieldId: $"{ModManifest.UniqueID}.compat.sources.roosters"
            );
        }
        else
        {
            b.TextLabel(
                elementId: "compat_sources_roosters",
                name: () => _i18n.GetString("config.compatSources.roosters.name"),
                tooltip: () => _i18n.GetString("config.compatSources.roosters.description"),
                getValue: () => GetSelectedRoosterProviderValue(roosterAllowedArray[0]),
                setValue: SetSelectedRoosterProviderValue,
                allowedValues: roosterAllowedArray,
                formatValue: FormatRoosterProviderValue,
                fieldId: $"{ModManifest.UniqueID}.compat.sources.roosters"
            );
        }
    }

    b.ParagraphDynamic(
        text: () => BuildRoosterProvidersValidationNote(data),
        styled: true,
        id: "compat_rooster_note"
    );
}



private string BuildCompatSourcesSummary(IDictionary<string, FarmAnimalData> data)
{
    var errors = 0;
    var parts = new List<string>();

    foreach (var kind in Enum.GetValues(typeof(EggKind)).Cast<EggKind>())
    {
        if (_compatSelection.TryResolveSelectedEggItemId(_saveState.Compat, kind, data, out var itemId, out var err))
        {
            parts.Add($"{kind}={itemId}");
        }
        else
        {
            errors++;
            parts.Add($"{kind}=ERR");
        }
    }

    var enabledProviders = _saveState.Compat.EnabledRoosterSources?.Count ?? 0;
    var roosterKeys = _compatSelection.GetEnabledRoosterFarmAnimalKeys(_saveState.Compat);
    var roosterPart = $"roosters=providers:{enabledProviders} keys:{roosterKeys.Count}";
    parts.Add(roosterPart);

    var summary = $"Compat sources: {string.Join(", ", parts)}";
    if (errors > 0)
        summary += $" (errors:{errors})";

    return summary;
}


private string BuildEggSelectionValidationNote(EggKind kind, IDictionary<string, FarmAnimalData> data)
{
    if (_compatSelection.TryResolveSelectedEggItemId(_saveState.Compat, kind, data, out var itemId, out var error))
    {
        var sourceId = _saveState.Compat.SelectedEggSources.TryGetValue(kind, out var sid) && !string.IsNullOrWhiteSpace(sid)
            ? sid
            : "(default)";
        return $"{kind}: OK ({sourceId} => {itemId})";
    }

    return $"{kind}: ERR ({error ?? "unresolved"})";
}

private string BuildRoosterProvidersValidationNote(IDictionary<string, FarmAnimalData> data)
{
    var enabledProviders = _saveState.Compat.EnabledRoosterSources?.Count ?? 0;
    var keys = _compatSelection.GetEnabledRoosterFarmAnimalKeys(_saveState.Compat);

    var missing = keys.Where(k => !data.ContainsKey(k)).ToList();
    if (missing.Count == 0)
        return $"Roosters: OK (providers:{enabledProviders} keys:{keys.Count})";

    return $"Roosters: WARN (providers:{enabledProviders} keys:{keys.Count} missing:{missing.Count})";
}

private uint GetSelectedEggSourceIndex(EggKind kind, string fallback, string[] allowed)
{
    var selected = GetSelectedEggSourceId(kind, fallback);
    var idx = Array.FindIndex(allowed, v => v.Equals(selected, StringComparison.OrdinalIgnoreCase));
    return (uint)(idx < 0 ? 0 : idx);
}

private void SetSelectedEggSourceIndex(EggKind kind, string[] allowed, uint index)
{
    var i = (int)index;
    if (i < 0 || i >= allowed.Length)
        i = 0;

    SetSelectedEggSourceId(kind, allowed[i]);
}

private bool TryBuildEggSourceChoices(
    EggKind kind,
    string[] allowed,
    IDictionary<string, FarmAnimalData> data,
    out (Func<string?> label, Texture2D sheet, Rectangle? sourceRect)[] choices)
{
    var list = new List<(Func<string?> label, Texture2D sheet, Rectangle? sourceRect)>();

    foreach (var sid in allowed)
    {
        var src = _compatSelection.EggCatalog.Sources
            .FirstOrDefault(s => s.EggKind == kind && s.SourceId.Equals(sid, StringComparison.OrdinalIgnoreCase));

        if (src is null || !_compatSelection.TryResolveEggSourceItemId(src, data, out var itemId, out _))
        {
            list.Add((() => FormatEggSourceValue(sid), Game1.mouseCursors, new Rectangle(0, 0, 1, 1)));
            continue;
        }

        try
        {
            var itemData = ItemRegistry.GetDataOrErrorItem(itemId);
            list.Add((() => FormatEggSourceValue(sid), itemData.GetTexture(), itemData.GetSourceRect()));
        }
        catch
        {
            list.Add((() => FormatEggSourceValue(sid), Game1.mouseCursors, new Rectangle(0, 0, 1, 1)));
        }
    }

    choices = list.ToArray();
    return choices.Length > 0;
}

private string GetSelectedEggSourceId(EggKind kind, string fallback)
    => _saveState.Compat.SelectedEggSources.TryGetValue(kind, out var sid) && !string.IsNullOrWhiteSpace(sid)
        ? sid
        : fallback;

private void SetSelectedEggSourceId(EggKind kind, string sourceId)
{
    EnsureCompatInitialized();

    if (!_compatSelection.TrySetEggSource(_saveState.Compat, kind, sourceId, out _, out var changed, out var error))
    {
        if (!string.IsNullOrWhiteSpace(error))
            LogScopedHelper.Log(_log, ModRoosterControlCategory.Gmcm, $"GMCM: failed to set egg source '{sourceId}' for {kind}: {error}", LogLevel.Warn);
        return;
    }

    if (!changed)
        return;

    PersistAndApplyCompatChange($"gmcm set egg source {kind}");
}


private string FormatEggSourceValue(string sourceId)
{
    var src = _compatSelection.EggCatalog.Sources
        .FirstOrDefault(s => s.SourceId.Equals(sourceId, StringComparison.OrdinalIgnoreCase));

    if (src is null)
        return sourceId;

    // Keep labels short for the common case; include IDs only when the display name is ambiguous.
    var isAmbiguous = _compatSelection.EggCatalog.Sources.Any(s =>
        !s.SourceId.Equals(src.SourceId, StringComparison.OrdinalIgnoreCase)
        && string.Equals(s.DisplayName, src.DisplayName, StringComparison.OrdinalIgnoreCase));

    return isAmbiguous ? $"{src.DisplayName} ({src.SourceId})" : src.DisplayName;
}


private bool GetUseAllRoosterProviders()
{
    var enabled = _saveState.Compat.EnabledRoosterSources
        .Where(id => !string.IsNullOrWhiteSpace(id))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();

    return enabled.Count != 1;
}

private void SetUseAllRoosterProviders(bool useAll)
{
    EnsureCompatInitialized();

    var changed = false;

    if (useAll)
    {
        if (!_compatSelection.EnableAllRoosterProviders(_saveState.Compat, out _, out changed, out var error))
        {
            if (!string.IsNullOrWhiteSpace(error))
                LogScopedHelper.Log(_log, ModRoosterControlCategory.Gmcm, $"GMCM: failed to enable all rooster providers: {error}", LogLevel.Warn);
            return;
        }
    }
    else
    {
        var enabled = _saveState.Compat.EnabledRoosterSources
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (enabled.Count == 1)
            return;

        var fallback = _compatSelection.RoosterCatalog.Sources
            .Where(s => !string.IsNullOrWhiteSpace(s.SourceId))
            .GroupBy(s => s.SourceId, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(s => s.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(s => s.SourceId, StringComparer.OrdinalIgnoreCase)
            .Select(s => s.SourceId)
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(fallback))
            return;

        if (!_compatSelection.TrySetRoosterProviderSingle(_saveState.Compat, fallback, out _, out changed, out var error))
        {
            if (!string.IsNullOrWhiteSpace(error))
                LogScopedHelper.Log(_log, ModRoosterControlCategory.Gmcm, $"GMCM: failed to set rooster provider '{fallback}': {error}", LogLevel.Warn);
            return;
        }
    }

    if (!changed)
        return;

    PersistAndApplyCompatChange("gmcm set rooster providers mode");
}

private bool TryBuildRoosterProviderChoices(
    string[] allowed,
    out (Func<string?> label, Texture2D sheet, Rectangle? sourceRect)[] choices)
{
    var list = new List<(Func<string?> label, Texture2D sheet, Rectangle? sourceRect)>();

    foreach (var value in allowed)
    {
        var captured = value;

        var src = _compatSelection.RoosterCatalog.Sources
            .FirstOrDefault(s => s.SourceId.Equals(captured, StringComparison.OrdinalIgnoreCase));

        if (src is not null && !string.IsNullOrWhiteSpace(src.RoosterEggItemId))
        {
            try
            {
                var itemData = ItemRegistry.GetDataOrErrorItem(src.RoosterEggItemId);
                list.Add((() => FormatRoosterProviderValue(captured), itemData.GetTexture(), itemData.GetSourceRect()));
                continue;
            }
            catch
            {
                // fall through
            }
        }

        list.Add((() => FormatRoosterProviderValue(captured), Game1.mouseCursors, new Rectangle(0, 0, 1, 1)));
    }

    choices = list.ToArray();
    return choices.Length > 0;
}

private uint GetSelectedRoosterProviderIndex(string[] allowed)
{
    var selected = GetSelectedRoosterProviderValue(allowed.Length > 0 ? allowed[0] : string.Empty);
    var idx = Array.FindIndex(allowed, v => v.Equals(selected, StringComparison.OrdinalIgnoreCase));
    return (uint)(idx < 0 ? 0 : idx);
}

private void SetSelectedRoosterProviderIndex(string[] allowed, uint index)
{
    var i = (int)index;
    if (i < 0 || i >= allowed.Length)
        i = 0;

    if (allowed.Length == 0)
        return;

    SetSelectedRoosterProviderValue(allowed[i]);
}

private string GetSelectedRoosterProviderValue(string fallback)
{
    var enabled = _saveState.Compat.EnabledRoosterSources
        .Where(id => !string.IsNullOrWhiteSpace(id))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();

    return enabled.Count == 1 ? enabled[0] : fallback;
}

private void SetSelectedRoosterProviderValue(string value)
{
    EnsureCompatInitialized();

    if (!_compatSelection.TrySetRoosterProviderSingle(_saveState.Compat, value, out _, out var changed, out var error))
    {
        if (!string.IsNullOrWhiteSpace(error))
            LogScopedHelper.Log(_log, ModRoosterControlCategory.Gmcm, $"GMCM: failed to set rooster provider '{value}': {error}", LogLevel.Warn);
        return;
    }

    if (!changed)
        return;

    PersistAndApplyCompatChange("gmcm set rooster provider");
}


private string FormatRoosterProviderValue(string value)
{
    if (value.Equals(AllRoosterProvidersValue, StringComparison.OrdinalIgnoreCase))
        return _i18n.GetString("config.compatSources.roosters.allowed.all");

    var src = _compatSelection.RoosterCatalog.Sources
        .FirstOrDefault(s => s.SourceId.Equals(value, StringComparison.OrdinalIgnoreCase));

    if (src is null)
        return value;

    var isAmbiguous = _compatSelection.RoosterCatalog.Sources.Any(s =>
        !s.SourceId.Equals(src.SourceId, StringComparison.OrdinalIgnoreCase)
        && string.Equals(s.DisplayName, src.DisplayName, StringComparison.OrdinalIgnoreCase));

    return isAmbiguous ? $"{src.DisplayName} ({src.SourceId})" : src.DisplayName;
}


private bool TryApplyGmcmResetsOnSave()
{
    if (!_gmcmResetAllArmed && !_gmcmResetEggSourcesArmed && !_gmcmResetRoosterProvidersArmed)
        return false;

    var resetAll = _gmcmResetAllArmed;
    var resetEggs = resetAll || _gmcmResetEggSourcesArmed;
    var resetRoosters = resetAll || _gmcmResetRoosterProvidersArmed;

    _gmcmResetAllArmed = false;
    _gmcmResetEggSourcesArmed = false;
    _gmcmResetRoosterProvidersArmed = false;

    if (!Context.IsWorldReady)
    {
        LogScopedHelper.Log(_log, ModRoosterControlCategory.Gmcm,
            "GMCM reset requested, but a save is not loaded yet. No changes were applied.",
            LogLevel.Info);
        return true;
    }

    if (!Context.IsMainPlayer)
    {
        LogScopedHelper.Log(_log, ModRoosterControlCategory.Gmcm,
            "GMCM reset requested, but only the host can modify per-save compat selections.",
            LogLevel.Info);
        return true;
    }

    EnsureCompatInitialized();

    var reason = resetAll
        ? "gmcm reset compat: all"
        : resetEggs && resetRoosters
            ? "gmcm reset compat: eggs + roosters"
            : resetEggs
                ? "gmcm reset compat: eggs"
                : "gmcm reset compat: roosters";

    if (TryResetCompatSelections(resetEggs, resetRoosters, reason, out var summary))
    {
        LogScopedHelper.Log(_log, ModRoosterControlCategory.Gmcm,
            summary is null
                ? "GMCM reset applied: compat selections were reset to defaults."
                : $"GMCM reset applied:\n{summary}",
            LogLevel.Info);
    }

    return true;
}

private bool TryResetCompatSelections(bool resetEggSources, bool resetRoosterProviders, string reason, out string? summary)
{
    summary = null;

    if (!Context.IsWorldReady)
    {
        summary = "Reset skipped: no save loaded.";
        return false;
    }

    EnsureCompatInitialized();

    if (resetEggSources)
        _saveState.Compat.SelectedEggSources.Clear();

    if (resetRoosterProviders)
        _saveState.Compat.EnabledRoosterSources.Clear();

    // Re-run deterministic repair to seed defaults.
    _compatSelection.InitializeOrRepair(_saveState.Compat, out summary);

    PersistAndApplyCompatChange(reason);
    return true;
}

private bool TryResetCompatEggKind(EggKind kind, string reason, out string? summary)
{
    summary = null;

    if (!Context.IsWorldReady)
    {
        summary = "Reset skipped: no save loaded.";
        return false;
    }

    EnsureCompatInitialized();

    _saveState.Compat.SelectedEggSources.Remove(kind);

    _compatSelection.InitializeOrRepair(_saveState.Compat, out summary);

    PersistAndApplyCompatChange(reason);
    return true;
}



private void PersistAndApplyCompatChange(string reason)
{
    _saveData.Write(SaveStateKey, _saveState);

    InvalidateDailyPlanCache(reason);
    _needsRoosterSetup = true;

    EnsureRoostersSetup();

    RequestInvalidate(FarmAnimalsAssetName);
    if (RoosterHelper.FemaleEggs.Count > 0)
        RequestInvalidate(ObjectsAssetName);

    _invalidations.Flush();
    try
    {
        var data = Helper.GameContent.Load<Dictionary<string, FarmAnimalData>>(FarmAnimalsAssetName);
        ApplyCompatSelectionsToRuntime(data);
        _compatSourcesSummary = BuildCompatSourcesSummary(data);
    }
    catch (Exception ex)
    {
        LogScopedHelper.Log(_log, ModRoosterControlCategory.Gmcm, $"GMCM: failed to apply compat selections after change: {ex}", LogLevel.Error);
    }

    RequestGmcmRebuild();
}



    private void BuildFemalesPage(GmcmPageBuilder<RoosterControlConfig> b)
    {
        if (!Context.IsWorldReady)
        {
            b.Paragraph("females.placeholder");
            return;
        }

        EnsureRoostersSetup();

        _invalidations.Flush();

        Dictionary<string, FarmAnimalData> data;
        try
        {
            data = Helper.GameContent.Load<Dictionary<string, FarmAnimalData>>(FarmAnimalsAssetName);
        }
        catch (Exception ex)
        {
            LogScopedHelper.Log(_log, ModRoosterControlCategory.Gmcm, $"Failed to load {FarmAnimalsAssetName} for GMCM females page: {ex}", LogLevel.Error);
            b.Paragraph("females.error");
            return;
        }


        ApplyCompatSelectionsToRuntime(data);

        var plan = FemaleAssignmentService.ComputeFemaleAssignmentsPlan(
            config: Config,
            animalData: data,
            roosters: RoosterHelper.Roosters,
            animalExclusions: _animalExclusions,
            hatchApi: HatchApi
        );

        // Keep the config dictionary aligned with the game-derived list to avoid missing entries.
        if (plan.ConfigChanged || Config.FemalesOptions is null)
        {
            Config.FemalesOptions = new Dictionary<string, FemaleRoosterAssignment>(plan.FemalesOptions, StringComparer.OrdinalIgnoreCase);
            _femaleAssignmentsDirty = true;
        }

        b.Section("females.assignments");

        foreach (var femaleKey in plan.FemalesOptions.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            var elementId = GmcmDynamicIds.ElementId("females.assignment", femaleKey);

            b.EnumLabel<FemaleRoosterAssignment>(
                elementId: elementId,
                name: () => GetFemaleLabel(femaleKey, data),
                getValue: () => GetFemaleAssignment(femaleKey, plan),
                setValue: v => SetFemaleAssignment(femaleKey, v),
                formatAllowedValue: FormatFemaleAssignmentAllowedValue,
                tooltip: () => GetFemaleTooltip(femaleKey, plan)
            );
        }

        b.Paragraph("females.note");
    }

    private FemaleRoosterAssignment GetFemaleAssignment(string femaleKey, FemaleAssignmentsPlanResult plan)
    {
        if (Config.FemalesOptions is not null && Config.FemalesOptions.TryGetValue(femaleKey, out var current))
            return current;

        if (plan.FemalesOptions.TryGetValue(femaleKey, out var planned))
            return planned;

        return FemaleRoosterAssignment.Off;
    }

    private void SetFemaleAssignment(string femaleKey, FemaleRoosterAssignment value)
    {
        Config.FemalesOptions ??= new Dictionary<string, FemaleRoosterAssignment>(StringComparer.OrdinalIgnoreCase);
        Config.FemalesOptions[femaleKey] = value;
    }

    private string GetFemaleLabel(string femaleKey, IDictionary<string, FarmAnimalData> data)
    {
        if (!data.TryGetValue(femaleKey, out var entry) || entry is null)
            return femaleKey;

        var displayName = entry.DisplayName?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(displayName) || string.Equals(displayName, femaleKey, StringComparison.OrdinalIgnoreCase))
            return femaleKey;

        return _i18n.GetString("config.females.dynamic.label", new { displayName, femaleKey });
    }

    private string GetFemaleTooltip(string femaleKey, FemaleAssignmentsPlanResult plan)
    {
        var normal = "";
        var large = "";

        if (plan.FemaleEggs.TryGetValue(femaleKey, out var eggs))
        {
            if (eggs.TryGetValue(EggTypeEntries.Normal, out var n) && !string.IsNullOrWhiteSpace(n))
                normal = n;
            if (eggs.TryGetValue(EggTypeEntries.Large, out var l) && !string.IsNullOrWhiteSpace(l))
                large = l;
        }

        return _i18n.GetString(
            "config.females.dynamic.tooltip",
            new
            {
                femaleKey,
                normalEgg = string.IsNullOrWhiteSpace(normal) ? "-" : normal,
                largeEgg = string.IsNullOrWhiteSpace(large) ? "-" : large
            }
        );
    }

    private string FormatFemaleAssignmentAllowedValue(string raw)
    {
        var key = $"config.females.assignment.allowed.{raw}";
        return _i18n.GetString(key);
    }


    private void RequestGmcmRebuild()
    {
        if (_gmcmHost is null)
            return;

        if (_gmcmRebuildQueued)
            return;

        _gmcmRebuildQueued = true;
        _nextTick.Enqueue(() =>
        {
            _gmcmRebuildQueued = false;
            _gmcmHost?.RebuildLayout(
                define: DefineGmcmLayout,
                fieldIdPrefix: ModManifest.UniqueID,
                titleScreenOnly: false,
                force: true,
                attachLocaleHandler: true
            );
        });
    }
}
