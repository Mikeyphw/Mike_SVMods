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
}
