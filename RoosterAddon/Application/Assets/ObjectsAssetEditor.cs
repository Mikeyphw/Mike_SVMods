using System;
using System.Collections.Generic;
using System.Linq;
using RoosterAddon.Constants;
using RoosterAddon.Enums;
using StardewModdingAPI.Events;
using StardewValley.GameData.Objects;

namespace RoosterAddon.Application.Assets;

/// <summary>
/// Applies RoosterAddon edits to <c>Data/Objects</c>.
/// </summary>
internal sealed class ObjectsAssetEditor
{
    private readonly IRoosterAssetEdits _ctx;

    public ObjectsAssetEditor(IRoosterAssetEdits context)
    {
        _ctx = context;
    }

    public void Edit(AssetRequestedEventArgs e)
    {
        if (_ctx.RoosterHelper is null || _ctx.RoosterHelper.Roosters is null)
            return;

        if (_ctx.RoosterHelper.FemaleEggs.Count > 0)
        {
            e.Edit(
                asset =>
                {
                    var dataObject = asset.AsDictionary<string, ObjectData>().Data;

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

                    var originalEggs = dataObject
                        .Where(o => allEggIds.Contains(o.Key))
                        .ToDictionary(o => o.Key, o => o.Value);

                    _ctx.GetAndSetFertilizedEggs(
                        (Dictionary<string, ObjectData>)dataObject,
                        originalEggs
                    );
                },
                AssetEditPriority.Late + 20000
            );
        }

        e.Edit(
            asset =>
            {
                var dataObject = asset.AsDictionary<string, ObjectData>().Data;

                var betterTrufflesIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var produces in _ctx.RoosterHelper.Roosters.Values.Select(r => r.AllProduces))
                {
                    betterTrufflesIds.Add(
                        produces[ProduceTypeEntries.Produce][ProduceEntries.First][RoosterItemDataEntries.Name]
                    );
                    betterTrufflesIds.Add(
                        produces[ProduceTypeEntries.Deluxe][ProduceEntries.First][RoosterItemDataEntries.Name]
                    );
                }

                foreach (var entry in dataObject)
                {
                    if (!betterTrufflesIds.Contains(entry.Key))
                        continue;

                    entry.Value.ContextTags ??= new();
                    entry.Value.ContextTags.Add("temisthem_bettertruffles");
                }
            },
            AssetEditPriority.Late + 10000
        );
    }
}
