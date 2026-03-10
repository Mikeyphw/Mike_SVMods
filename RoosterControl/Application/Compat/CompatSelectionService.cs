using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
    public static readonly IReadOnlyList<EggKind> ManagedEggKinds = new[]
    {
        EggKind.BrownEgg,
        EggKind.LargeBrownEgg,
        EggKind.WhiteEgg,
        EggKind.LargeWhiteEgg,
        EggKind.VoidEgg,
        EggKind.GoldEgg,
        EggKind.LargeVoidEgg,
        EggKind.LargeGoldEgg,
        EggKind.BlueEgg,
        EggKind.LargeBlueEgg,
        EggKind.RoosterEgg,
    };

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

    private static bool NormalizeState(CompatSelectionState state)
    {
        var changed = false;

        if (state.SelectedEggSources is null)
        {
            state.SelectedEggSources = new Dictionary<EggKind, string>();
            changed = true;
        }
        else
        {
            foreach (var unsupported in state.SelectedEggSources.Keys.Except(ManagedEggKinds).ToList())
            {
                state.SelectedEggSources.Remove(unsupported);
                changed = true;
            }
        }

        if (state.EnabledRoosterSources is null)
        {
            state.EnabledRoosterSources = new List<string>();
            changed = true;
        }

        return changed;
    }


    public bool InitializeOrRepair(CompatSelectionState state, out string? repairSummary)
    {
        _eggCatalog = NormalizeEggCatalog(LoadCatalog<EggSourcesCatalog>(GetEggCatalogAssetName()));
        _roosterCatalog = NormalizeRoosterCatalog(LoadCatalog<RoosterSourcesCatalog>(GetRoosterCatalogAssetName()));

        var notes = new List<string>();
        var changed = NormalizeState(state);

        changed |= RepairEggSelections(state, notes);
        changed |= RepairRoosterSelections(state, notes);

        repairSummary = notes.Count == 0
            ? null
            : string.Join("\n", notes);

        return changed;
    }


    public string BuildStatusReport(CompatSelectionState state)
    {
        if (!IsInitialized)
            throw new InvalidOperationException("CompatSelectionService not initialized yet.");

        var sb = new StringBuilder();

        sb.AppendLine("RoosterControl compat status");
        sb.AppendLine();
        sb.AppendLine($"Egg sources catalog: {GetEggCatalogAssetName()} (schema v{EggCatalog.SchemaVersion})");
        sb.AppendLine($"Rooster sources catalog: {GetRoosterCatalogAssetName()} (schema v{RoosterCatalog.SchemaVersion})");
        sb.AppendLine();

        sb.AppendLine("EggKind selections:");
        foreach (var kind in ManagedEggKinds)
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
            if (!string.IsNullOrWhiteSpace(src.ModId))
                sb.AppendLine($"      ModId: {src.ModId}");

            foreach (var key in src.FarmAnimalKeys ?? new List<RoosterFarmAnimalKeyDef>())
            {
                sb.AppendLine($"      * {key.Key} :: breeding={key.BreedingRole} :: eggLayer={key.EggLayerRole}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("Note: selections are applied at runtime; use ra verify compat for detailed diagnostics.");
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

    private EggSourcesCatalog NormalizeEggCatalog(EggSourcesCatalog? catalog)
    {
        catalog ??= new EggSourcesCatalog();

        var sources = BuildDefaultEggSources().ToList();

        if (catalog.Sources is not null)
            sources.AddRange(catalog.Sources);

        if (catalog.Eggs is not null)
        {
            foreach (var group in catalog.Eggs)
            {
                foreach (var egg in group.Eggs ?? new List<EggSourceOptionDef>())
                {
                    sources.Add(new EggSourceDef
                    {
                        SourceId = egg.SourceId,
                        ModId = egg.ModId,
                        DisplayName = egg.DisplayName,
                        EggKind = group.EggKind,
                        ItemId = egg.ItemId,
                        FarmAnimalRef = egg.FarmAnimalRef
                    });
                }
            }
        }

        var normalized = sources
            .Where(s => ManagedEggKinds.Contains(s.EggKind))
            .Where(IsModInstalled)
            .Select(NormalizeEggSource)
            .Where(s => !string.IsNullOrWhiteSpace(s.SourceId))
            .GroupBy(s => $"{s.EggKind}|{s.SourceId}", StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(s => s.EggKind)
            .ThenBy(s => s.SourceId, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new EggSourcesCatalog
        {
            SchemaVersion = Math.Max(catalog.SchemaVersion, 2),
            Sources = normalized
        };
    }

    private IEnumerable<EggSourceDef> BuildDefaultEggSources()
    {
        foreach (var def in GetDefaultFarmAnimalEggMappings())
            yield return def;

        foreach (var def in GetDefaultCompatEggMappings())
            yield return def;
    }

    private static IEnumerable<EggSourceDef> GetDefaultFarmAnimalEggMappings()
    {
        yield return CreateDefaultFarmAnimalEggSource(EggKind.BrownEgg, "Brown Chicken", EggSourceRefList.EggItemIds, 0);
        yield return CreateDefaultFarmAnimalEggSource(EggKind.LargeBrownEgg, "Brown Chicken", EggSourceRefList.EggItemIds, 1);
        yield return CreateDefaultFarmAnimalEggSource(EggKind.WhiteEgg, "White Chicken", EggSourceRefList.EggItemIds, 0);
        yield return CreateDefaultFarmAnimalEggSource(EggKind.LargeWhiteEgg, "White Chicken", EggSourceRefList.EggItemIds, 1);
        yield return CreateDefaultFarmAnimalEggSource(EggKind.BlueEgg, "Blue Chicken", EggSourceRefList.EggItemIds, 0);
        yield return CreateDefaultFarmAnimalEggSource(EggKind.LargeBlueEgg, "Blue Chicken", EggSourceRefList.EggItemIds, 1);
        yield return CreateDefaultFarmAnimalEggSource(EggKind.VoidEgg, "Void Chicken", EggSourceRefList.EggItemIds, 0);
        yield return CreateDefaultFarmAnimalEggSource(EggKind.LargeVoidEgg, "Void Chicken", EggSourceRefList.EggItemIds, 1);
        yield return CreateDefaultFarmAnimalEggSource(EggKind.GoldEgg, "Golden Chicken", EggSourceRefList.EggItemIds, 0);
        yield return CreateDefaultFarmAnimalEggSource(EggKind.LargeGoldEgg, "Golden Chicken", EggSourceRefList.EggItemIds, 1);
    }

    private static IEnumerable<EggSourceDef> GetDefaultCompatEggMappings()
    {
        yield return new EggSourceDef
        {
            ModId = "Meara.Roosters",
            DisplayName = "Meara Roosters: Blue Egg",
            EggKind = EggKind.BlueEgg,
            ItemId = "Meara.RoostersBlueEgg"
        };
        yield return new EggSourceDef
        {
            ModId = "6480.blueegg",
            DisplayName = "6480's Blue Egg",
            EggKind = EggKind.BlueEgg,
            ItemId = "6480.blueegg_BlueEgg"
        };
        yield return new EggSourceDef
        {
            ModId = "Meara.Roosters",
            DisplayName = "Meara Roosters: Large Blue Egg",
            EggKind = EggKind.LargeBlueEgg,
            ItemId = "Meara.RoostersLargeBlueEgg"
        };
        yield return new EggSourceDef
        {
            ModId = "Meara.Roosters",
            DisplayName = "Meara Roosters: Large Void Egg",
            EggKind = EggKind.LargeVoidEgg,
            ItemId = "Meara.RoostersLargevoidEgg"
        };
        yield return new EggSourceDef
        {
            ModId = "Meara.Roosters",
            DisplayName = "Meara Roosters: Large Gold Egg",
            EggKind = EggKind.LargeGoldEgg,
            ItemId = "Meara.RoostersLargegoldEgg"
        };
        yield return new EggSourceDef
        {
            ModId = "Blaklyte.CP.Rooster",
            DisplayName = "Blaklyte Roosters: Rooster Egg",
            EggKind = EggKind.RoosterEgg,
            ItemId = "Blaklyte.CP.Rooster_rooster_egg"
        };
    }

    private static EggSourceDef CreateDefaultFarmAnimalEggSource(EggKind kind, string farmAnimalKey, EggSourceRefList list, int index)
        => new()
        {
            SourceId = string.Empty,
            DisplayName = $"{farmAnimalKey} ({list}[{index}])",
            EggKind = kind,
            FarmAnimalRef = new FarmAnimalEggRef(farmAnimalKey, list, index)
        };

    private RoosterSourcesCatalog NormalizeRoosterCatalog(RoosterSourcesCatalog? catalog)
    {
        catalog ??= new RoosterSourcesCatalog();

        var sources = BuildDefaultRoosterSources().ToList();
        if (catalog.Sources is not null)
            sources.AddRange(catalog.Sources);

        var normalized = sources
            .Where(IsModInstalled)
            .Select(NormalizeRoosterSource)
            .Where(s => !string.IsNullOrWhiteSpace(s.SourceId))
            .GroupBy(s => s.SourceId, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(s => s.SourceId, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new RoosterSourcesCatalog
        {
            SchemaVersion = Math.Max(catalog.SchemaVersion, 2),
            Sources = normalized
        };
    }

    private static IEnumerable<RoosterSourceDef> BuildDefaultRoosterSources()
    {
        yield return new RoosterSourceDef
        {
            ModId = "Meara.Roosters",
            DisplayName = "Meara Roosters",
            FarmAnimalKeys = new List<RoosterFarmAnimalKeyDef>
            {
                new() { Key = "cockbrown", BreedingRole = BreedingRole.Male, EggLayerRole = EggLayerRole.InheritFromFarmAnimalData },
                new() { Key = "cockwhite", BreedingRole = BreedingRole.Male, EggLayerRole = EggLayerRole.InheritFromFarmAnimalData },
                new() { Key = "cockblue", BreedingRole = BreedingRole.Male, EggLayerRole = EggLayerRole.InheritFromFarmAnimalData },
                new() { Key = "cockvoid", BreedingRole = BreedingRole.Male, EggLayerRole = EggLayerRole.InheritFromFarmAnimalData },
                new() { Key = "cockgold", BreedingRole = BreedingRole.Male, EggLayerRole = EggLayerRole.InheritFromFarmAnimalData },
            }
        };

        yield return new RoosterSourceDef
        {
            ModId = "Blaklyte.CP.Rooster",
            DisplayName = "Blaklyte Roosters",
            FarmAnimalKeys = new List<RoosterFarmAnimalKeyDef>
            {
                new() { Key = "Rooster", BreedingRole = BreedingRole.Male, EggLayerRole = EggLayerRole.InheritFromFarmAnimalData },
                new() { Key = "White Rooster", BreedingRole = BreedingRole.Male, EggLayerRole = EggLayerRole.InheritFromFarmAnimalData },
                new() { Key = "Blue Rooster", BreedingRole = BreedingRole.Male, EggLayerRole = EggLayerRole.InheritFromFarmAnimalData },
                new() { Key = "Desert Rooster", BreedingRole = BreedingRole.Male, EggLayerRole = EggLayerRole.InheritFromFarmAnimalData },
                new() { Key = "Trans Rooster", BreedingRole = BreedingRole.None, EggLayerRole = EggLayerRole.InheritFromFarmAnimalData },
                new() { Key = "Cockatrice", BreedingRole = BreedingRole.Male, EggLayerRole = EggLayerRole.InheritFromFarmAnimalData },
            }
        };
    }

    private bool IsModInstalled(EggSourceDef source) => IsModInstalled(source.ModId);

    private bool IsModInstalled(RoosterSourceDef source) => IsModInstalled(source.ModId);

    private bool IsModInstalled(string? modId)
        => string.IsNullOrWhiteSpace(modId) || _helper.ModRegistry.IsLoaded(modId.Trim());

    private EggSourceDef NormalizeEggSource(EggSourceDef source)
        => source with
        {
            SourceId = NormalizeEggSourceId(source),
            DisplayName = NormalizeDisplayName(source.DisplayName, source.ModId, source.ItemId, source.FarmAnimalRef)
        };

    private RoosterSourceDef NormalizeRoosterSource(RoosterSourceDef source)
        => source with
        {
            SourceId = NormalizeRoosterSourceId(source),
            DisplayName = NormalizeDisplayName(source.DisplayName, source.ModId, null, null)
        };

    private static string NormalizeDisplayName(string? displayName, string? modId, string? itemId, FarmAnimalEggRef? farmAnimalRef)
    {
        if (!string.IsNullOrWhiteSpace(displayName))
            return displayName.Trim();

        if (!string.IsNullOrWhiteSpace(itemId))
            return itemId.Trim();

        if (farmAnimalRef is not null)
            return $"{farmAnimalRef.FarmAnimalKey} ({farmAnimalRef.List}[{farmAnimalRef.Index}])";

        if (!string.IsNullOrWhiteSpace(modId))
            return modId.Trim();

        return "Unknown";
    }

    private static string NormalizeEggSourceId(EggSourceDef source)
    {
        if (!string.IsNullOrWhiteSpace(source.SourceId))
            return source.SourceId.Trim();

        if (source.FarmAnimalRef is not null)
        {
            var listAlias = source.FarmAnimalRef.List switch
            {
                EggSourceRefList.EggItemIds => $"egg{source.FarmAnimalRef.Index}",
                EggSourceRefList.ProduceItemIds => $"produce{source.FarmAnimalRef.Index}",
                EggSourceRefList.DeluxeProduceItemIds => $"deluxe{source.FarmAnimalRef.Index}",
                _ => $"{source.FarmAnimalRef.List}:{source.FarmAnimalRef.Index}"
            };

            return $"farmanimal:{Slugify(source.FarmAnimalRef.FarmAnimalKey)}:{listAlias}";
        }

        var modSegment = !string.IsNullOrWhiteSpace(source.ModId)
            ? Slugify(source.ModId)
            : "vanilla";
        var itemSegment = !string.IsNullOrWhiteSpace(source.ItemId)
            ? Slugify(source.ItemId)
            : "unknown";

        return $"{modSegment}:{itemSegment}";
    }

    private static string NormalizeRoosterSourceId(RoosterSourceDef source)
    {
        if (!string.IsNullOrWhiteSpace(source.SourceId))
            return source.SourceId.Trim();

        if (!string.IsNullOrWhiteSpace(source.ModId))
            return source.ModId.Trim();

        if (!string.IsNullOrWhiteSpace(source.DisplayName))
            return Slugify(source.DisplayName);

        return "provider";
    }

    private static string Slugify(string value)
    {
        var slug = Regex.Replace(value.Trim().ToLowerInvariant(), @"[^a-z0-9._-]+", "-");
        return slug.Trim('-');
    }

        private bool RepairEggSelections(CompatSelectionState state, ICollection<string>? notes)
    {
        var changed = false;

        foreach (var kind in ManagedEggKinds)
        {
            var options = EggCatalog.Sources
                .Where(s => s.EggKind == kind && !string.IsNullOrWhiteSpace(s.SourceId) && IsEggSourceValid(s))
                .OrderBy(s => s.SourceId, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (options.Count == 0)
            {
                if (state.SelectedEggSources.TryGetValue(kind, out var existing) && !string.IsNullOrWhiteSpace(existing))
                {
                    state.SelectedEggSources.Remove(kind);
                    notes?.Add($"EggKind {kind}: '{existing}' removed (no valid sources available)");
                    changed = true;
                }

                continue;
            }

            if (!state.SelectedEggSources.TryGetValue(kind, out var selected) || string.IsNullOrWhiteSpace(selected))
            {
                state.SelectedEggSources[kind] = options[0].SourceId;
                notes?.Add($"EggKind {kind}: (unset) -> '{options[0].SourceId}'");
                changed = true;
                continue;
            }

            if (!options.Any(o => o.SourceId.Equals(selected, StringComparison.OrdinalIgnoreCase)))
            {
                state.SelectedEggSources[kind] = options[0].SourceId;
                notes?.Add($"EggKind {kind}: '{selected}' invalid -> '{options[0].SourceId}'");
                changed = true;
            }
        }

        return changed;
    }


        private bool RepairRoosterSelections(CompatSelectionState state, ICollection<string>? notes)
    {
        var changed = false;

        var allSourceIds = RoosterCatalog.Sources
            .Select(s => s.SourceId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Default behavior: enable all providers so multiple rooster mods can coexist.
        if (state.EnabledRoosterSources is null)
        {
            state.EnabledRoosterSources = allSourceIds.ToList();
            notes?.Add(allSourceIds.Count == 0
                ? "Rooster providers: (unset) -> (none available)"
                : $"Rooster providers: (unset) -> all ({allSourceIds.Count})");
            return true;
        }

        if (state.EnabledRoosterSources.Count == 0)
        {
            if (allSourceIds.Count == 0)
                return false; // nothing to enable

            state.EnabledRoosterSources = allSourceIds.ToList();
            notes?.Add($"Rooster providers: empty -> all ({allSourceIds.Count})");
            return true;
        }

        var before = state.EnabledRoosterSources
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var cleaned = before
            .Where(id => allSourceIds.Contains(id, StringComparer.OrdinalIgnoreCase))
            .ToList();

        var removed = before.Except(cleaned, StringComparer.OrdinalIgnoreCase).ToList();

        if (removed.Count > 0)
        {
            notes?.Add($"Rooster providers: removed invalid ids [{string.Join(", ", removed)}]");
            changed = true;
        }

        if (cleaned.Count != state.EnabledRoosterSources.Count || removed.Count > 0)
        {
            state.EnabledRoosterSources = cleaned;
            changed = true;
        }

        // Ensure at least one enabled provider if any exist.
        if (state.EnabledRoosterSources.Count == 0 && allSourceIds.Count > 0)
        {
            state.EnabledRoosterSources = allSourceIds.ToList();
            notes?.Add($"Rooster providers: empty -> all ({allSourceIds.Count})");
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

        public bool TryCycleEggSource(
        CompatSelectionState state,
        EggKind kind,
        bool prev,
        out EggSourceDef? selected,
        out bool changed,
        out string? error)
    {
        selected = null;
        changed = false;
        error = null;

        NormalizeState(state);

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

        changed = !next.SourceId.Equals(currentId, StringComparison.OrdinalIgnoreCase);
        state.SelectedEggSources[kind] = next.SourceId;

        return true;
    }


        public bool TryCycleRoosterProviderSingle(
        CompatSelectionState state,
        bool prev,
        out RoosterSourceDef? selected,
        out bool changed,
        out string? error)
    {
        selected = null;
        changed = false;
        error = null;

        NormalizeState(state);

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

        changed = state.EnabledRoosterSources.Count != 1
                  || !state.EnabledRoosterSources[0].Equals(next.SourceId, StringComparison.OrdinalIgnoreCase);

        state.EnabledRoosterSources = new List<string> { next.SourceId };

        return true;
    }


        public bool EnableAllRoosterProviders(
        CompatSelectionState state,
        out int enabledCount,
        out bool changed,
        out string? error)
    {
        enabledCount = 0;
        changed = false;
        error = null;

        NormalizeState(state);

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

        changed = current.Count != all.Count || !current.SequenceEqual(all, StringComparer.OrdinalIgnoreCase);

        state.EnabledRoosterSources = all;

        return true;
    }



    public bool TrySetEggSource(
        CompatSelectionState state,
        EggKind kind,
        string sourceId,
        out EggSourceDef? selected,
        out bool changed,
        out string? error)
    {
        selected = null;
        changed = false;
        error = null;

        NormalizeState(state);

        if (!IsInitialized)
        {
            error = "CompatSelectionService not initialized yet.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(sourceId))
        {
            error = "SourceId is required.";
            return false;
        }

        var options = EggCatalog.Sources
            .Where(s => s.EggKind == kind && !string.IsNullOrWhiteSpace(s.SourceId) && IsEggSourceValid(s))
            .OrderBy(s => s.SourceId, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (options.Count == 0)
        {
            error = $"No valid egg sources were found for EggKind={kind}.";
            return false;
        }

        selected = options.FirstOrDefault(o => o.SourceId.Equals(sourceId, StringComparison.OrdinalIgnoreCase));
        if (selected is null)
        {
            error = $"Unknown egg source '{sourceId}' for EggKind={kind}.";
            return false;
        }

        state.SelectedEggSources.TryGetValue(kind, out var current);
        changed = current is null || !current.Equals(selected.SourceId, StringComparison.OrdinalIgnoreCase);

        state.SelectedEggSources[kind] = selected.SourceId;

        return true;
    }


    public bool TrySetRoosterProviderSingle(
        CompatSelectionState state,
        string sourceId,
        out RoosterSourceDef? selected,
        out bool changed,
        out string? error)
    {
        selected = null;
        changed = false;
        error = null;

        NormalizeState(state);

        if (!IsInitialized)
        {
            error = "CompatSelectionService not initialized yet.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(sourceId))
        {
            error = "SourceId is required.";
            return false;
        }

        var options = RoosterCatalog.Sources
            .Where(s => !string.IsNullOrWhiteSpace(s.SourceId))
            .OrderBy(s => s.SourceId, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (options.Count == 0)
        {
            error = "No rooster providers were found in the catalog.";
            return false;
        }

        selected = options.FirstOrDefault(o => o.SourceId.Equals(sourceId, StringComparison.OrdinalIgnoreCase));
        if (selected is null)
        {
            error = $"Unknown rooster provider '{sourceId}'.";
            return false;
        }

        var current = state.EnabledRoosterSources
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToList();

        changed = current.Count != 1 || !current[0].Equals(selected.SourceId, StringComparison.OrdinalIgnoreCase);

        state.EnabledRoosterSources = new List<string> { selected.SourceId };

        return true;
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

        NormalizeState(state);

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

var index = src.FarmAnimalRef.Index;
switch (src.FarmAnimalRef.List)
{
    case EggSourceRefList.EggItemIds:
    {
        var list = farmAnimal.EggItemIds;
        if (list is null)
        {
            error = $"Farm animal '{src.FarmAnimalRef.FarmAnimalKey}' has no list '{src.FarmAnimalRef.List}'.";
            return false;
        }

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
    case EggSourceRefList.ProduceItemIds:
    {
        var list = farmAnimal.ProduceItemIds;
        if (list is null)
        {
            error = $"Farm animal '{src.FarmAnimalRef.FarmAnimalKey}' has no list '{src.FarmAnimalRef.List}'.";
            return false;
        }

        if (index < 0 || index >= list.Count)
        {
            error = $"Farm animal '{src.FarmAnimalRef.FarmAnimalKey}' list '{src.FarmAnimalRef.List}' index {index} is out of range (count={list.Count}).";
            return false;
        }

        var resolved = list[index]?.ItemId;
        if (string.IsNullOrWhiteSpace(resolved))
        {
            error = $"Farm animal '{src.FarmAnimalRef.FarmAnimalKey}' list '{src.FarmAnimalRef.List}' index {index} resolved to an empty item id.";
            return false;
        }

        itemId = resolved.Trim();
        return true;
    }
    case EggSourceRefList.DeluxeProduceItemIds:
    {
        var list = farmAnimal.DeluxeProduceItemIds;
        if (list is null)
        {
            error = $"Farm animal '{src.FarmAnimalRef.FarmAnimalKey}' has no list '{src.FarmAnimalRef.List}'.";
            return false;
        }

        if (index < 0 || index >= list.Count)
        {
            error = $"Farm animal '{src.FarmAnimalRef.FarmAnimalKey}' list '{src.FarmAnimalRef.List}' index {index} is out of range (count={list.Count}).";
            return false;
        }

        var resolved = list[index]?.ItemId;
        if (string.IsNullOrWhiteSpace(resolved))
        {
            error = $"Farm animal '{src.FarmAnimalRef.FarmAnimalKey}' list '{src.FarmAnimalRef.List}' index {index} resolved to an empty item id.";
            return false;
        }

        itemId = resolved.Trim();
        return true;
    }
    default:
        error = $"Farm animal '{src.FarmAnimalRef.FarmAnimalKey}' has no list '{src.FarmAnimalRef.List}'.";
        return false;
}
    }

public bool TryResolveEggSourceItemId(
    EggSourceDef src,
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

    if (!IsEggSourceValid(src))
    {
        error = $"Egg source '{src.SourceId}' is invalid.";
        return false;
    }

    if (!string.IsNullOrWhiteSpace(src.ItemId))
    {
        itemId = src.ItemId.Trim();
        return true;
    }

    if (src.FarmAnimalRef is null)
    {
        error = $"Egg source '{src.SourceId}' has no FarmAnimalRef.";
        return false;
    }

    if (!animalData.TryGetValue(src.FarmAnimalRef.FarmAnimalKey, out var farmAnimal))
    {
        error = $"Farm animal key '{src.FarmAnimalRef.FarmAnimalKey}' not found in Data/FarmAnimals.";
        return false;
    }

    var index = src.FarmAnimalRef.Index;
    switch (src.FarmAnimalRef.List)
    {
        case EggSourceRefList.EggItemIds:
        {
            var list = farmAnimal.EggItemIds;
            if (list is null)
            {
                error = $"Farm animal '{src.FarmAnimalRef.FarmAnimalKey}' has no list '{src.FarmAnimalRef.List}'.";
                return false;
            }

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
        case EggSourceRefList.ProduceItemIds:
        {
            var list = farmAnimal.ProduceItemIds;
            if (list is null)
            {
                error = $"Farm animal '{src.FarmAnimalRef.FarmAnimalKey}' has no list '{src.FarmAnimalRef.List}'.";
                return false;
            }

            if (index < 0 || index >= list.Count)
            {
                error = $"Farm animal '{src.FarmAnimalRef.FarmAnimalKey}' list '{src.FarmAnimalRef.List}' index {index} is out of range (count={list.Count}).";
                return false;
            }

            var resolved = list[index]?.ItemId;
            if (string.IsNullOrWhiteSpace(resolved))
            {
                error = $"Farm animal '{src.FarmAnimalRef.FarmAnimalKey}' list '{src.FarmAnimalRef.List}' index {index} resolved to an empty item id.";
                return false;
            }

            itemId = resolved.Trim();
            return true;
        }
        case EggSourceRefList.DeluxeProduceItemIds:
        {
            var list = farmAnimal.DeluxeProduceItemIds;
            if (list is null)
            {
                error = $"Farm animal '{src.FarmAnimalRef.FarmAnimalKey}' has no list '{src.FarmAnimalRef.List}'.";
                return false;
            }

            if (index < 0 || index >= list.Count)
            {
                error = $"Farm animal '{src.FarmAnimalRef.FarmAnimalKey}' list '{src.FarmAnimalRef.List}' index {index} is out of range (count={list.Count}).";
                return false;
            }

            var resolved = list[index]?.ItemId;
            if (string.IsNullOrWhiteSpace(resolved))
            {
                error = $"Farm animal '{src.FarmAnimalRef.FarmAnimalKey}' list '{src.FarmAnimalRef.List}' index {index} resolved to an empty item id.";
                return false;
            }

            itemId = resolved.Trim();
            return true;
        }
        default:
            error = $"Farm animal '{src.FarmAnimalRef.FarmAnimalKey}' has no list '{src.FarmAnimalRef.List}'.";
            return false;
    }
}

    public HashSet<string> GetEnabledRoosterFarmAnimalKeys(CompatSelectionState state)
    {
        if (!IsInitialized)
            throw new InvalidOperationException("CompatSelectionService not initialized yet.");

        var enabledList = state.EnabledRoosterSources ?? new List<string>();
        var enabledSources = enabledList
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
