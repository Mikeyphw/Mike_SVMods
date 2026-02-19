using System;
using System.Collections.Generic;
using RoosterAddon.Helpers;
using StardewModdingAPI.Events;
using StardewValley.GameData.FarmAnimals;

namespace RoosterAddon.Application.Assets;

/// <summary>Applies RoosterAddon edits to <c>Data/FarmAnimals</c>.</summary>
internal sealed class FarmAnimalsAssetEditor
{
    private readonly RoosterControl _roosterHelper;
    private readonly Action<RoosterData, IDictionary<string, FarmAnimalData>> _applyEdits;

    public FarmAnimalsAssetEditor(
        RoosterControl roosterHelper,
        Action<RoosterData, IDictionary<string, FarmAnimalData>> applyEdits
    )
    {
        _roosterHelper = roosterHelper ?? throw new ArgumentNullException(nameof(roosterHelper));
        _applyEdits = applyEdits ?? throw new ArgumentNullException(nameof(applyEdits));
    }

    public void Edit(AssetRequestedEventArgs e)
    {
        if (_roosterHelper.Roosters is null)
            return;

        e.Edit(
            asset =>
            {
                var data = asset.AsDictionary<string, FarmAnimalData>().Data;
                foreach (var rooster in _roosterHelper.Roosters.Values)
                    _applyEdits(rooster, data);
            },
            AssetEditPriority.Late + 2000
        );
    }
}
