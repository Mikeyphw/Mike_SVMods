using System;
using StardewModdingAPI.Events;
using StardewValley.GameData.FarmAnimals;

namespace RoosterAddon.Application.Assets;

/// <summary>
/// Applies RoosterAddon edits to <c>Data/FarmAnimals</c>.
/// </summary>
internal sealed class FarmAnimalsAssetEditor
{
    private readonly IRoosterAssetEdits _ctx;

    public FarmAnimalsAssetEditor(IRoosterAssetEdits context)
    {
        _ctx = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void Edit(AssetRequestedEventArgs e)
    {
        if (_ctx.RoosterHelper is null || _ctx.RoosterHelper.Roosters is null)
            return;

        e.Edit(
            asset =>
            {
                var data = asset.AsDictionary<string, FarmAnimalData>().Data;

                foreach (var rooster in _ctx.RoosterHelper.Roosters.Values)
                    _ctx.SetRoosterData(rooster, data);
            },
            AssetEditPriority.Late + 2000
        );
    }
}
