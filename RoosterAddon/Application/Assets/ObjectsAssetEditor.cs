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
