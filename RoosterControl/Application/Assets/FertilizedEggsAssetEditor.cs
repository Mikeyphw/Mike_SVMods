using System;
using RoosterControl.Application.FertilizedEggs;
using RoosterControl.Enums;
using RoosterControl.Framework.Config;
using RoosterControl.Helpers;
using StardewModdingAPI.Events;
using StardewValley.GameData.Objects;

namespace RoosterControl.Application.Assets;

/// <summary>Applies fertilized egg variants to <c>Data/Objects</c>.</summary>
internal sealed class FertilizedEggsAssetEditor
{
    private readonly RoosterHelper _roosterHelper;
    private readonly Func<string> _getFertilizedNameSuffix;
    private readonly Func<string> _getFertilizedDescription;
    private readonly Func<bool> _cloneEggsEnabled;

    private readonly FertilizedEggsObjectEditService _service = new();

    public FertilizedEggsAssetEditor(
        RoosterHelper roosterHelper,
        Func<string> getFertilizedNameSuffix,
        Func<string> getFertilizedDescription,
        Func<bool> cloneEggsEnabled
    )
    {
        _roosterHelper = roosterHelper ?? throw new ArgumentNullException(nameof(roosterHelper));
        _getFertilizedNameSuffix = getFertilizedNameSuffix ?? throw new ArgumentNullException(nameof(getFertilizedNameSuffix));
        _getFertilizedDescription = getFertilizedDescription ?? throw new ArgumentNullException(nameof(getFertilizedDescription));
        _cloneEggsEnabled = cloneEggsEnabled ?? throw new ArgumentNullException(nameof(cloneEggsEnabled));
    }

    public void Edit(AssetRequestedEventArgs e)
    {
        if (_roosterHelper.Roosters is null)
            return;

        if (_roosterHelper.FemaleEggs.Count == 0)
            return;

        var enableSynthesis = _roosterHelper.Config.General.HatchingMode == HatchingMode.OnlyFertilizedEggs
            || _cloneEggsEnabled();

        if (!enableSynthesis)
            return;

        e.Edit(
            asset =>
            {
                var objects = asset.AsDictionary<string, ObjectData>().Data;
                _service.Apply(
                    objects: (System.Collections.Generic.Dictionary<string, ObjectData>)objects,
                    femaleEggs: _roosterHelper.FemaleEggs,
                    getFertilizedNameSuffix: _getFertilizedNameSuffix,
                    getFertilizedDescription: _getFertilizedDescription,
                    enableSynthesis: enableSynthesis
                );
            },
            AssetEditPriority.Late + 20000
        );
    }
}
