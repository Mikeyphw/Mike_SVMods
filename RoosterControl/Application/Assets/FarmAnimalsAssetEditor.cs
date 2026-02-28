using System;
using System.Collections.Generic;
using RoosterControl.Helpers;
using StardewModdingAPI.Events;
using StardewValley.GameData.FarmAnimals;

namespace RoosterControl.Application.Assets;

/// <summary>Applies RoosterControl edits to <c>Data/FarmAnimals</c>.</summary>
internal sealed class FarmAnimalsAssetEditor
{
    private readonly RoosterHelper _roosterHelper;
    private readonly Action<RoosterData, IDictionary<string, FarmAnimalData>> _applyEdits;
    private readonly Func<IDictionary<string, FarmAnimalData>, bool> _tryApplyDailyEdits;

    public FarmAnimalsAssetEditor(
        RoosterHelper roosterHelper,
        Action<RoosterData, IDictionary<string, FarmAnimalData>> applyEdits,
        Func<IDictionary<string, FarmAnimalData>, bool> tryApplyDailyEdits
    )
    {
        _roosterHelper = roosterHelper ?? throw new ArgumentNullException(nameof(roosterHelper));
        _applyEdits = applyEdits ?? throw new ArgumentNullException(nameof(applyEdits));
        _tryApplyDailyEdits = tryApplyDailyEdits ?? throw new ArgumentNullException(nameof(tryApplyDailyEdits));
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

                _ = _tryApplyDailyEdits(data);
            },
            AssetEditPriority.Late + 2000
        );
    }
}
