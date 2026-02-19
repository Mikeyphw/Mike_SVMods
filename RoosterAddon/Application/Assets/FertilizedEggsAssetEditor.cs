using System;
using System.Collections.Generic;
using System.Linq;
using RoosterAddon.Application.FertilizedEggs;
using RoosterAddon.Constants;
using RoosterAddon.Enums;
using RoosterAddon.Helpers;
using StardewModdingAPI.Events;
using StardewValley.GameData.Objects;

namespace RoosterAddon.Application.Assets;

/// <summary>Applies fertilized egg variants to <c>Data/Objects</c>.</summary>
internal sealed class FertilizedEggsAssetEditor
{
    private readonly RoosterControl _roosterHelper;
    private readonly Func<string> _getFertilizedNameSuffix;
    private readonly Func<string> _getFertilizedDescription;
    private readonly Func<bool> _cloneEggsEnabled;
    private readonly bool _includeMearasEggs;

    private readonly FertilizedEggsObjectEditService _service = new();

    public FertilizedEggsAssetEditor(
        RoosterControl roosterHelper,
        Func<string> getFertilizedNameSuffix,
        Func<string> getFertilizedDescription,
        Func<bool> cloneEggsEnabled,
        bool includeMearasEggs = false
    )
    {
        _roosterHelper = roosterHelper ?? throw new ArgumentNullException(nameof(roosterHelper));
        _getFertilizedNameSuffix = getFertilizedNameSuffix ?? throw new ArgumentNullException(nameof(getFertilizedNameSuffix));
        _getFertilizedDescription = getFertilizedDescription ?? throw new ArgumentNullException(nameof(getFertilizedDescription));
        _cloneEggsEnabled = cloneEggsEnabled ?? throw new ArgumentNullException(nameof(cloneEggsEnabled));
        _includeMearasEggs = includeMearasEggs;
    }

    public void Edit(AssetRequestedEventArgs e)
    {
        if (_roosterHelper.Roosters is null)
            return;

        if (_roosterHelper.FemaleEggs.Count == 0)
            return;

        e.Edit(
            asset =>
            {
                var objects = asset.AsDictionary<string, ObjectData>().Data;

                var mearasFertilizedEggs = _includeMearasEggs
                    ? new List<string>
                    {
                        ItemIds.CustomItems.FertilizedBlueEgg,
                        ItemIds.CustomItems.FertilizedBrownEgg,
                        ItemIds.CustomItems.FertilizedGoldEgg,
                        ItemIds.CustomItems.FertilizedVoidEgg,
                        ItemIds.CustomItems.FertilizedWhiteEgg
                    }
                    : new List<string>();

                var eggIdsFromFemaleEggs = _roosterHelper
                    .FemaleEggs.SelectMany(e => e.Value)
                    .Where(e => e.Key != EggTypeEntries.Fertilized)
                    .Select(e => e.Value)
                    .Distinct()
                    .ToList();

                var allEggIds = eggIdsFromFemaleEggs.Union(mearasFertilizedEggs).ToList();

                var originalEggs = objects
                    .Where(o => allEggIds.Contains(o.Key))
                    .ToDictionary(o => o.Key, o => o.Value);

                var defaultFemales = _roosterHelper.Roosters.Select(r => r.Value.Female).ToList();

                _service.Apply(
                    objects: (Dictionary<string, ObjectData>)objects,
                    originalEggs: originalEggs,
                    femaleEggs: _roosterHelper.FemaleEggs,
                    defaultFemales: defaultFemales,
                    getFertilizedNameSuffix: _getFertilizedNameSuffix,
                    getFertilizedDescription: _getFertilizedDescription,
                    cloneEggsEnabled: _cloneEggsEnabled(),
                    includeMearasEggs: _includeMearasEggs
                );
            },
            AssetEditPriority.Late + 20000
        );
    }
}
