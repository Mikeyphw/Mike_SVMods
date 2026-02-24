using System;
using RoosterControl.Application.Objects;
using RoosterControl.Helpers;
using StardewModdingAPI.Events;
using StardewValley.GameData.Objects;

namespace RoosterControl.Application.Assets;

/// <summary>Applies RoosterControl edits to <c>Data/Objects</c>.</summary>
internal sealed class ObjectsAssetEditor
{
    private readonly RoosterControl _roosterHelper;
    private readonly BetterTrufflesContextTagService _betterTrufflesTags;

    public ObjectsAssetEditor(RoosterControl roosterHelper, BetterTrufflesContextTagService betterTrufflesTags)
    {
        _roosterHelper = roosterHelper ?? throw new ArgumentNullException(nameof(roosterHelper));
        _betterTrufflesTags = betterTrufflesTags ?? throw new ArgumentNullException(nameof(betterTrufflesTags));
    }

    public void Edit(AssetRequestedEventArgs e)
    {
        if (_roosterHelper.Roosters is null)
            return;

        e.Edit(
            asset =>
            {
                var dataObject = asset.AsDictionary<string, ObjectData>().Data;
                _betterTrufflesTags.Apply(dataObject, _roosterHelper.Roosters.Values);
            },
            AssetEditPriority.Late + 10000
        );
    }
}
