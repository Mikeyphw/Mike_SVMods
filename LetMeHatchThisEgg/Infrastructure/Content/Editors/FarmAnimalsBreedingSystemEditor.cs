using LetMeHatchThisEgg.Breeding;
using LetMeHatchThisEgg.GameAssetManagement;
using StardewModdingAPI.Events;
using StardewValley.GameData.FarmAnimals;
using static LetMeHatchThisEgg.EnumsAndStrings.Strings;

namespace LetMeHatchThisEgg.Infrastructure.Content.Editors;

/// <summary>Runs the breeding system logic to enable hatching/male clone behavior.</summary>
public sealed class FarmAnimalsBreedingSystemEditor : AssetEditorBase
{
    private readonly GameAssetManager _assets;

    public FarmAnimalsBreedingSystemEditor(GameAssetManager assets)
    {
        _assets = assets;
    }

    public override string AssetName => GameAssets.FarmAnimals;

    public override AssetEditPriority Priority => AssetEditPriority.Late + BREEDING_SYSTEM_PRIORITY;

        public override string DebugGates => "Config.BreedingSystemEnabled == true and HasAddedMaleClones == true.";

public override void Edit(IAssetData asset, ContentContext context)
    {
        if (!context.Config.BreedingSystemEnabled || !_assets.HasAddedMaleClones)
            return;

        var data = asset.AsDictionary<string, FarmAnimalData>().Data;
        BreedingDataLoader.RunHatchingSystem(
            data,
            context.Config,
            _assets.BreedingConfigsByPack
        );
    }
}
