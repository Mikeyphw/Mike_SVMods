using System;
using System.Collections.Generic;
using System.Linq;
using RoosterAddon.Application.FertilizedEggs;
using RoosterAddon.Constants;
using RoosterAddon.Enums;
using StardewModdingAPI.Events;
using StardewValley.GameData.Objects;

namespace RoosterAddon.Application.Assets;

/// <summary>
/// Applies fertilized egg variants to <c>Data/Objects</c>.
/// </summary>
internal sealed class FertilizedEggsAssetEditor
{
    private readonly IRoosterAssetEdits _ctx;
    private readonly FertilizedEggsObjectEditService _service = new();

    public FertilizedEggsAssetEditor(IRoosterAssetEdits context)
    {
        _ctx = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void Edit(AssetRequestedEventArgs e)
    {
        if (_ctx.RoosterHelper is null || _ctx.RoosterHelper.Roosters is null)
            return;

        if (_ctx.RoosterHelper.FemaleEggs.Count == 0)
            return;

        e.Edit(
            asset =>
            {
                var objects = asset.AsDictionary<string, ObjectData>().Data;

                var mearasFertilizedEggs = new List<string>
                {
                    ItemIds.CustomItems.FertilizedBlueEgg,
                    ItemIds.CustomItems.FertilizedBrownEgg,
                    ItemIds.CustomItems.FertilizedGoldEgg,
                    ItemIds.CustomItems.FertilizedVoidEgg,
                    ItemIds.CustomItems.FertilizedWhiteEgg
                };

                var eggIdsFromFemaleEggs = _ctx.RoosterHelper
                    .FemaleEggs.SelectMany(e => e.Value)
                    .Where(e => e.Key != EggTypeEntries.Fertilized)
                    .Select(e => e.Value)
                    .Distinct()
                    .ToList();

                var allEggIds = eggIdsFromFemaleEggs.Union(mearasFertilizedEggs).ToList();

                var originalEggs = objects
                    .Where(o => allEggIds.Contains(o.Key))
                    .ToDictionary(o => o.Key, o => o.Value);

                var defaultFemales = _ctx.RoosterHelper.Roosters.Select(r => r.Value.Female).ToList();

                _service.Apply(
                    objects: (Dictionary<string, ObjectData>)objects,
                    originalEggs: originalEggs,
                    femaleEggs: _ctx.RoosterHelper.FemaleEggs,
                    defaultFemales: defaultFemales,
                    getFertilizedNameSuffix: () => _ctx.Helper.Translation.Get("egg.fertilized.name"),
                    getFertilizedDescription: () => _ctx.Helper.Translation.Get("egg.fertilized.description"),
                    cloneEggsEnabled: _ctx.CloneEggsEnabled,
                    includeMearasEggs: false
                );
            },
            AssetEditPriority.Late + 20000
        );
    }
}
