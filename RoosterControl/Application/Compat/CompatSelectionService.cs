using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mikew.NestCore.Helpers.Logging;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.FarmAnimals;

using static Mikew.NestCore.Helpers.Logging.ScopedLogCatalog;

namespace RoosterControl.Application.Compat;

/// <summary>
/// Loads CP-patchable compat catalogs and maintains per-save selection state.
/// Phase 9A: read-only (list + repair). Phase 9B wires application + cycling.
/// </summary>
internal sealed class CompatSelectionService
{
    private readonly IModHelper _helper;
    private readonly IManifest _manifest;
    private readonly ILog _log;

    private EggSourcesCatalog? _eggCatalog;
    private RoosterSourcesCatalog? _roosterCatalog;

    public bool IsInitialized => _eggCatalog is not null && _roosterCatalog is not null;

    public EggSourcesCatalog EggCatalog => _eggCatalog ?? throw new InvalidOperationException("CompatSelectionService not initialized yet.");
    public RoosterSourcesCatalog RoosterCatalog => _roosterCatalog ?? throw new InvalidOperationException("CompatSelectionService not initialized yet.");

    public CompatSelectionService(IModHelper helper, IManifest manifest, ILog log)
    {
        _helper = helper;
        _manifest = manifest;
        _log = log;
    }

    public bool InitializeOrRepair(CompatSelectionState state)
    {
        _eggCatalog = LoadCatalog<EggSourcesCatalog>(GetEggCatalogAssetName());
        _roosterCatalog = LoadCatalog<RoosterSourcesCatalog>(GetRoosterCatalogAssetName());

        var changed = false;

        changed |= RepairEggSelections(state);
        changed |= RepairRoosterSelections(state);

        return changed;
    }

    public string BuildStatusReport(CompatSelectionState state)
    {
        if (!IsInitialized)
            throw new InvalidOperationException("CompatSelectionService not initialized yet.");

        var sb = new StringBuilder();

        sb.AppendLine("RoosterControl compat status (Phase 9A: selections + catalog loading)");
        sb.AppendLine();
        sb.AppendLine($"Egg sources catalog: {GetEggCatalogAssetName()} (schema v{EggCatalog.SchemaVersion})");
        sb.AppendLine($"Rooster sources catalog: {GetRoosterCatalogAssetName()} (schema v{RoosterCatalog.SchemaVersion})");
        sb.AppendLine();

        sb.AppendLine("EggKind selections:");
        foreach (var kind in Enum.GetValues(typeof(EggKind)).Cast<EggKind>())
        {
            var sources = EggCatalog.Sources
                .Where(s => s.EggKind == kind && !string.IsNullOrWhiteSpace(s.SourceId))
                .OrderBy(s => s.SourceId, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var selected = state.SelectedEggSources.TryGetValue(kind, out var sid) ? sid : "(unset)";
            sb.AppendLine($"  - {kind}: {selected}");

            foreach (var src in sources)
            {
                var refText = src.ItemId is not null
                    ? $"ItemId={src.ItemId}"
                    : src.FarmAnimalRef is not null
                        ? $"FarmAnimalRef={src.FarmAnimalRef.FarmAnimalKey}/{src.FarmAnimalRef.List}[{src.FarmAnimalRef.Index}]"
                        : "INVALID(ref)";

                sb.AppendLine($"      * {src.SourceId} :: {src.DisplayName} :: {refText}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("Rooster providers:");
        var enabled = new HashSet<string>(state.EnabledRoosterSources ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
        foreach (var src in RoosterCatalog.Sources.OrderBy(s => s.SourceId, StringComparer.OrdinalIgnoreCase))
        {
            var on = enabled.Contains(src.SourceId) ? "ENABLED" : "disabled";
            sb.AppendLine($"  - {src.SourceId} :: {src.DisplayName} :: {on}");

            if (!string.IsNullOrWhiteSpace(src.RoosterEggItemId))
                sb.AppendLine($"      RoosterEggItemId: {src.RoosterEggItemId}");

            foreach (var key in src.FarmAnimalKeys ?? new List<RoosterFarmAnimalKeyDef>())
            {
                sb.AppendLine($"      * {key.Key} :: breeding={key.BreedingRole} :: eggLayer={key.EggLayerRole}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("Note: cycling/apply behavior is Phase 9B (not yet enabled).");
        return sb.ToString();
    }

    private string GetEggCatalogAssetName() => $"Mods/{_manifest.UniqueID}/compat/egg_sources";
    private string GetRoosterCatalogAssetName() => $"Mods/{_manifest.UniqueID}/compat/rooster_sources";

    private T LoadCatalog<T>(string assetName) where T : class, new()
    {
        try
        {
            return _helper.GameContent.Load<T>(assetName) ?? new T();
        }
        catch (Exception ex)
        {
            LogScopedHelper.Log(_log, ModRoosterControlCategory.Assets,
                $"Failed to load compat catalog '{assetName}'. Using empty catalog. Error: {ex.Message}",
                LogLevel.Warn);

            return new T();
        }
    }

    private bool RepairEggSelections(CompatSelectionState state)
    {
        var changed = false;

        foreach (var kind in Enum.GetValues(typeof(EggKind)).Cast<EggKind>())
        {
            var options = EggCatalog.Sources
                .Where(s => s.EggKind == kind && !string.IsNullOrWhiteSpace(s.SourceId) && IsEggSourceValid(s))
                .OrderBy(s => s.SourceId, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (options.Count == 0)
                continue;

            if (!state.SelectedEggSources.TryGetValue(kind, out var selected) || string.IsNullOrWhiteSpace(selected))
            {
                state.SelectedEggSources[kind] = options[0].SourceId;
                changed = true;
                continue;
            }

            if (!options.Any(o => o.SourceId.Equals(selected, StringComparison.OrdinalIgnoreCase)))
            {
                state.SelectedEggSources[kind] = options[0].SourceId;
                changed = true;
            }
        }

        return changed;
    }

    private bool RepairRoosterSelections(CompatSelectionState state)
    {
        var changed = false;

        var allSourceIds = RoosterCatalog.Sources
            .Select(s => s.SourceId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Default behavior: enable all providers so multiple rooster mods can coexist.
        if (state.EnabledRoosterSources is null || state.EnabledRoosterSources.Count == 0)
        {
            state.EnabledRoosterSources = allSourceIds.ToList();
            return true;
        }

        var cleaned = state.EnabledRoosterSources
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(id => allSourceIds.Contains(id, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (cleaned.Count != state.EnabledRoosterSources.Count)
        {
            state.EnabledRoosterSources = cleaned;
            changed = true;
        }

        // Ensure at least one enabled provider if any exist.
        if (state.EnabledRoosterSources.Count == 0 && allSourceIds.Count > 0)
        {
            state.EnabledRoosterSources = allSourceIds.ToList();
            changed = true;
        }

        return changed;
    }

    private bool IsEggSourceValid(EggSourceDef src)
    {
        if (!string.IsNullOrWhiteSpace(src.ItemId))
            return true;

        if (src.FarmAnimalRef is null)
            return false;

        if (!Context.IsWorldReady)
            return true; // can't validate without game data yet; repair will still pick a deterministic value

        try
        {
            var data = _helper.GameContent.Load<Dictionary<string, FarmAnimalData>>("Data/FarmAnimals");
            if (!data.TryGetValue(src.FarmAnimalRef.FarmAnimalKey, out var fa))
                return false;

            var idx = src.FarmAnimalRef.Index;
            return src.FarmAnimalRef.List switch
            {
                EggSourceRefList.EggItemIds =>
                    fa.EggItemIds is not null
                    && idx >= 0
                    && idx < fa.EggItemIds.Count
                    && !string.IsNullOrWhiteSpace(fa.EggItemIds[idx]),

                EggSourceRefList.ProduceItemIds =>
                    fa.ProduceItemIds is not null
                    && idx >= 0
                    && idx < fa.ProduceItemIds.Count
                    && !string.IsNullOrWhiteSpace(fa.ProduceItemIds[idx]?.ItemId),

                EggSourceRefList.DeluxeProduceItemIds =>
                    fa.DeluxeProduceItemIds is not null
                    && idx >= 0
                    && idx < fa.DeluxeProduceItemIds.Count
                    && !string.IsNullOrWhiteSpace(fa.DeluxeProduceItemIds[idx]?.ItemId),

                _ => false,
            };
        }
        catch
        {
            return true;
        }
    }

    public bool TryCycleEggSource(CompatSelectionState state, EggKind kind, bool prev, out EggSourceDef? selected, out string? error)
    {
        selected = null;
        error = null;

        if (!IsInitialized)
        {
            error = "CompatSelectionService not initialized yet.";
            return false;
        }

        var options = EggCatalog.Sources
            .Where(s => s.EggKind == kind && !string.IsNullOrWhiteSpace(s.SourceId) && IsEggSourceValid(s))
            .OrderBy(s => s.SourceId, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (options.Count == 0)
        {
            error = $"No valid egg sources are available for EggKind '{kind}'.";
            return false;
        }

        state.SelectedEggSources.TryGetValue(kind, out var currentId);
        currentId ??= string.Empty;

        var currentIdx = options.FindIndex(o => o.SourceId.Equals(currentId, StringComparison.OrdinalIgnoreCase));
        if (currentIdx < 0)
            currentIdx = 0;

        var nextIdx = options.Count == 1
            ? 0
            : prev
                ? (currentIdx - 1 + options.Count) % options.Count
                : (currentIdx + 1) % options.Count;

        var next = options[nextIdx];
        selected = next;

        var changed = !next.SourceId.Equals(currentId, StringComparison.OrdinalIgnoreCase);
        state.SelectedEggSources[kind] = next.SourceId;
        return changed;
    }

    public bool TryCycleRoosterProviderSingle(CompatSelectionState state, bool prev, out RoosterSourceDef? selected, out string? error)
    {
        selected = null;
        error = null;

        if (!IsInitialized)
        {
            error = "CompatSelectionService not initialized yet.";
            return false;
        }

        var options = RoosterCatalog.Sources
            .Where(s => !string.IsNullOrWhiteSpace(s.SourceId))
            .GroupBy(s => s.SourceId, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(s => s.SourceId, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (options.Count == 0)
        {
            error = "No rooster providers are available in the current rooster sources catalog.";
            return false;
        }

        var currentId = state.EnabledRoosterSources.Count switch
        {
            1 => state.EnabledRoosterSources[0],
            > 1 => options.Select(o => o.SourceId).FirstOrDefault(id => state.EnabledRoosterSources.Contains(id, StringComparer.OrdinalIgnoreCase)) ?? options[0].SourceId,
            _ => options[0].SourceId
        };

        var currentIdx = options.FindIndex(o => o.SourceId.Equals(currentId, StringComparison.OrdinalIgnoreCase));
        if (currentIdx < 0)
            currentIdx = 0;

        var nextIdx = options.Count == 1
            ? 0
            : prev
                ? (currentIdx - 1 + options.Count) % options.Count
                : (currentIdx + 1) % options.Count;

        var next = options[nextIdx];
        selected = next;

        var changed = state.EnabledRoosterSources.Count != 1 || !state.EnabledRoosterSources[0].Equals(next.SourceId, StringComparison.OrdinalIgnoreCase);
        state.EnabledRoosterSources = new List<string> { next.SourceId };
        return changed;
    }

    public bool EnableAllRoosterProviders(CompatSelectionState state, out int enabledCount, out string? error)
    {
        enabledCount = 0;
        error = null;

        if (!IsInitialized)
        {
            error = "CompatSelectionService not initialized yet.";
            return false;
        }

        var all = RoosterCatalog.Sources
            .Select(s => s.SourceId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToList();

        enabledCount = all.Count;

        var current = state.EnabledRoosterSources
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var changed = current.Count != all.Count || !current.SequenceEqual(all, StringComparer.OrdinalIgnoreCase);

        state.EnabledRoosterSources = all;
        return changed;
    }


    public bool TryResolveSelectedEggItemId(
        CompatSelectionState state,
        EggKind kind,
        IDictionary<string, FarmAnimalData> animalData,
        out string? itemId,
        out string? error
    )
    {
        itemId = null;
        error = null;

        if (!IsInitialized)
        {
            error = "CompatSelectionService not initialized yet.";
            return false;
        }

        if (animalData is null)
        {
            error = "Data/FarmAnimals not loaded.";
            return false;
        }

        state.SelectedEggSources.TryGetValue(kind, out var selectedId);
        if (string.IsNullOrWhiteSpace(selectedId))
        {
            error = $"No egg source is selected for EggKind '{kind}'.";
            return false;
        }

        var src = EggCatalog.Sources.FirstOrDefault(s =>
            s.EggKind == kind && s.SourceId.Equals(selectedId, StringComparison.OrdinalIgnoreCase));

        if (src is null)
        {
            error = $"Selected egg source '{selectedId}' for EggKind '{kind}' does not exist in the catalog.";
            return false;
        }

        if (!IsEggSourceValid(src))
        {
            error = $"Selected egg source '{selectedId}' for EggKind '{kind}' is invalid.";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(src.ItemId))
        {
            itemId = src.ItemId.Trim();
            return true;
        }

        if (src.FarmAnimalRef is null)
        {
            error = $"Selected egg source '{selectedId}' for EggKind '{kind}' has no FarmAnimalRef.";
            return false;
        }

        if (!animalData.TryGetValue(src.FarmAnimalRef.FarmAnimalKey, out var farmAnimal))
        {
            error = $"Farm animal key '{src.FarmAnimalRef.FarmAnimalKey}' not found in Data/FarmAnimals.";
            return false;
        }

        var list = src.FarmAnimalRef.List switch
        {
            EggSourceRefList.EggItemIds => farmAnimal.EggItemIds,
            EggSourceRefList.ProduceItemIds => farmAnimal.ProduceItemIds,
            EggSourceRefList.DeluxeProduceItemIds => farmAnimal.DeluxeProduceItemIds,
            _ => null
        };

        if (list is null)
        {
            error = $"Farm animal '{src.FarmAnimalRef.FarmAnimalKey}' has no list '{src.FarmAnimalRef.List}'.";
            return false;
        }

        var index = src.FarmAnimalRef.Index;
        if (index < 0 || index >= list.Count)
        {
            error = $"Farm animal '{src.FarmAnimalRef.FarmAnimalKey}' list '{src.FarmAnimalRef.List}' index {index} is out of range (count={list.Count}).";
            return false;
        }

        var resolved = list[index];
        if (string.IsNullOrWhiteSpace(resolved))
        {
            error = $"Farm animal '{src.FarmAnimalRef.FarmAnimalKey}' list '{src.FarmAnimalRef.List}' index {index} resolved to an empty item id.";
            return false;
        }

        itemId = resolved.Trim();
        return true;
    }

    public HashSet<string> GetEnabledRoosterFarmAnimalKeys(CompatSelectionState state)
    {
        if (!IsInitialized)
            throw new InvalidOperationException("CompatSelectionService not initialized yet.");

        var enabledSources = state.EnabledRoosterSources
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var src in RoosterCatalog.Sources.Where(s => enabledSources.Contains(s.SourceId)))
        {
            if (src.FarmAnimalKeys is null)
                continue;

            foreach (var keyDef in src.FarmAnimalKeys.Where(k => !string.IsNullOrWhiteSpace(k.Key)))
                keys.Add(keyDef.Key.Trim());
        }

        return keys;
    }

}
