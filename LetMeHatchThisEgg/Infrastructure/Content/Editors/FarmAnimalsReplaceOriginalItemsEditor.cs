using LetMeHatchThisEgg.Cloning;
using LetMeHatchThisEgg.GameAssetManagement;
using StardewModdingAPI.Events;
using StardewValley.GameData.FarmAnimals;
using static LetMeHatchThisEgg.EnumsAndStrings.Strings;

namespace LetMeHatchThisEgg.Infrastructure.Content.Editors;

/// <summary>Ensures clone items are associated with the correct original items for egg colors and relations.</summary>
public sealed class FarmAnimalsReplaceOriginalItemsEditor : AssetEditorBase
{
    private readonly GameAssetManager _assets;

    public FarmAnimalsReplaceOriginalItemsEditor(GameAssetManager assets)
    {
        _assets = assets;
    }

    public override string AssetName => GameAssets.FarmAnimals;

    public override AssetEditPriority Priority => AssetEditPriority.Late + NEW_EGGS_ADD_PRIORITY;

        public override string DebugGates => "ItemsToAdd.Count > 0.";

public override void Edit(IAssetData asset, ContentContext context)
    {
        if (_assets.ItemsToAdd.Count < 1)
            return;

        var data = asset.AsDictionary<string, FarmAnimalData>().Data;
        foreach (var cloneItemType in AnimalItemRelationsManager.GetCloneItemTypes())
        {
            _assets.ItemManager.ReplaceOriginalItemForClone(data, cloneItemType);
        }
    }
}
