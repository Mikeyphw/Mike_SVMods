using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.GameData.FarmAnimals;
using static LetMeHatchThisEgg.EnumsAndStrings.Strings;

namespace LetMeHatchThisEgg.Infrastructure.Content.Editors;

/// <summary>Runs the breeding system logic to enable hatching/male clone behavior.</summary>
public sealed class FarmAnimalsBreedingSystemEditor : LmhAssetEditorBase
{
    public override string AssetName => Mikew.BirdsOfAFeather.EnumsAndStrings.Strings.AssetNames.FarmAnimals;

    public override AssetEditPriority Priority => AssetEditPriority.Late + BREEDING_SYSTEM_PRIORITY;

    public override string DebugGates => "Config.BreedingSystemEnabled == true.";

    public override void Edit(IAssetData asset, ContentContext context)
    {
        var data = asset.AsDictionary<string, FarmAnimalData>().Data;
        context.App.FarmAnimalsEdits.TryRunBreedingSystem(
            data: (System.Collections.Generic.Dictionary<string, FarmAnimalData>)data,
            config: context.Config,
            enabled: context.Config.BreedingSystemEnabled
        );
    }
}
