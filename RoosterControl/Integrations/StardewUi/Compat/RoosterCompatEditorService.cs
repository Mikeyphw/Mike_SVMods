using System;
using System.Collections.Generic;
using System.Linq;
using RoosterControl.Application.Compat;
using RoosterControl.Enums;
using StardewValley.GameData.FarmAnimals;

namespace RoosterControl.Integrations.StardewUi.Compat;

/// <summary>
/// Shared service for loading and applying compat editor selections.
/// Used by both GMCM and StardewUI editors to ensure consistent behavior.
/// </summary>
internal sealed class RoosterCompatEditorService
{
    private readonly CompatSelectionService _selectionService;

    public RoosterCompatEditorService(CompatSelectionService selectionService)
    {
        _selectionService = selectionService ?? throw new ArgumentNullException(nameof(selectionService));
    }

    /// <summary>
    /// Loads the current compat selection state and generates a snapshot suitable for UI display.
    /// </summary>
    public RoosterCompatEditorSnapshot LoadSnapshot(CompatSelectionState state, IDictionary<string, FarmAnimalData>? animalData)
    {
        var eggGroups = BuildEggGroups(state, animalData);
        var providerRecords = BuildProviderRecords(state);

        return new RoosterCompatEditorSnapshot(
            EggGroups: eggGroups,
            ProviderRecords: providerRecords
        );
    }

    /// <summary>
    /// Applies changes from a draft back to the selection state.
    /// </summary>
    public void ApplyDraft(CompatSelectionState state, RoosterCompatEditorDraft draft)
    {
        if (draft.EggSelections is not null)
        {
            foreach (var (kind, sourceId) in draft.EggSelections)
            {
                if (_selectionService.TrySetEggSource(state, kind, sourceId, out _, out var changed, out var error) && changed)
                {
                    // Selection changed, state is already updated
                }
            }
        }

        if (draft.UseAllProviders.HasValue)
        {
            if (draft.UseAllProviders.Value)
            {
                _selectionService.EnableAllRoosterProviders(state, out _, out _, out _);
            }
            else if (!string.IsNullOrWhiteSpace(draft.SelectedProviderId))
            {
                _selectionService.TrySetRoosterProviderSingle(state, draft.SelectedProviderId, out _, out _, out _);
            }
        }
    }

    /// <summary>
    /// Builds a summary text describing the current compat selections.
    /// </summary>
    public string BuildSummaryText(CompatSelectionState state, IDictionary<string, FarmAnimalData> data)
    {
        var errors = 0;
        var parts = new List<string>();

        foreach (var kind in CompatSelectionService.ManagedEggKinds)
        {
            if (_selectionService.TryResolveSelectedEggItemId(state, kind, data, out var itemId, out var err))
            {
                parts.Add($"{kind}={itemId}");
            }
            else
            {
                errors++;
                parts.Add($"{kind}=ERR");
            }
        }

        var enabledProviders = state.EnabledRoosterSources?.Count ?? 0;
        var roosterKeys = _selectionService.GetEnabledRoosterFarmAnimalKeys(state);
        var roosterPart = $"roosters=providers:{enabledProviders} keys:{roosterKeys.Count}";
        parts.Add(roosterPart);

        var summary = $"Compat sources: {string.Join(", ", parts)}";
        if (errors > 0)
            summary += $" (errors:{errors})";

        return summary;
    }

    /// <summary>
    /// Builds validation notes for a specific egg kind.
    /// </summary>
    public string BuildEggSelectionValidationNote(CompatSelectionState state, EggKind kind, IDictionary<string, FarmAnimalData> data)
    {
        if (_selectionService.TryResolveSelectedEggItemId(state, kind, data, out var itemId, out var error))
        {
            var sourceId = state.SelectedEggSources.TryGetValue(kind, out var sid) && !string.IsNullOrWhiteSpace(sid)
                ? sid
                : "(default)";
            return $"{kind}: OK ({sourceId} => {itemId})";
        }

        return $"{kind}: ERR ({error ?? "unresolved"})";
    }

    /// <summary>
    /// Builds validation notes for rooster providers.
    /// </summary>
    public string BuildRoosterProvidersValidationNote(CompatSelectionState state, IDictionary<string, FarmAnimalData> data)
    {
        var enabledProviders = state.EnabledRoosterSources?.Count ?? 0;
        var keys = _selectionService.GetEnabledRoosterFarmAnimalKeys(state);

        var missing = keys.Where(k => !data.ContainsKey(k)).ToList();
        if (missing.Count == 0)
            return $"Roosters: OK (providers:{enabledProviders} keys:{keys.Count})";

        return $"Roosters: WARN (providers:{enabledProviders} keys:{keys.Count} missing:{missing.Count})";
    }

    private List<RoosterCompatEggGroupRecord> BuildEggGroups(CompatSelectionState state, IDictionary<string, FarmAnimalData>? animalData)
    {
        var result = new List<RoosterCompatEggGroupRecord>();

        // Group by category
        var standardKinds = new[] { EggKind.BrownEgg, EggKind.WhiteEgg, EggKind.BlueEgg, EggKind.VoidEgg, EggKind.GoldEgg };
        var largeKinds = new[] { EggKind.LargeBrownEgg, EggKind.LargeWhiteEgg, EggKind.LargeBlueEgg, EggKind.LargeVoidEgg, EggKind.LargeGoldEgg };
        var specialKinds = new[] { EggKind.RoosterEgg };

        var catalog = _selectionService.EggCatalog;

        void AddGroup(string category, string section, IEnumerable<EggKind> kinds)
        {
            var records = new List<RoosterCompatEggKindRecord>();
            foreach (var kind in kinds)
            {
                var sources = catalog.Sources
                    .Where(s => s.EggKind == kind && !string.IsNullOrWhiteSpace(s.SourceId))
                    .ToList();

                var selectedId = state.SelectedEggSources.TryGetValue(kind, out var sid) ? sid : null;
                var validationNote = animalData is not null
                    ? BuildEggSelectionValidationNote(state, kind, animalData)
                    : null;

                records.Add(new RoosterCompatEggKindRecord(
                    Kind: kind,
                    DisplayName: kind.ToString(),
                    AvailableSources: sources.Select(s => s.SourceId).ToList(),
                    SelectedSourceId: selectedId,
                    ValidationNote: validationNote
                ));
            }

            result.Add(new RoosterCompatEggGroupRecord(
                Category: category,
                Section: section,
                Kinds: records
            ));
        }

        AddGroup("Standard Eggs", "compat.sources.eggs.standard", standardKinds);
        AddGroup("Large Eggs", "compat.sources.eggs.large", largeKinds);
        AddGroup("Special Eggs", "compat.sources.eggs.special", specialKinds);

        return result;
    }

    private List<RoosterCompatProviderRecord> BuildProviderRecords(CompatSelectionState state)
    {
        var catalog = _selectionService.RoosterCatalog;
        var enabledSet = (state.EnabledRoosterSources ?? new List<string>())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var records = catalog.Sources
            .Where(s => !string.IsNullOrWhiteSpace(s.SourceId))
            .GroupBy(s => s.SourceId, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .Select(s => new RoosterCompatProviderRecord(
                ProviderId: s.SourceId,
                DisplayName: s.DisplayName,
                IsEnabled: enabledSet.Contains(s.SourceId)
            ))
            .OrderBy(r => r.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => r.ProviderId, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return records;
    }
}
