using Mikew.BirdsOfAFeather.Helpers;
using StardewModdingAPI.Events;
using StardewValley;
using static WildEggsHatching.EnumsAndStrings.Enums;
using static WildEggsHatching.EnumsAndStrings.Enums.TextOptionValues;

namespace WildEggsHatching.DesertTomb.Assets;

/// <summary>
/// Owns the "AdditionalMineMaps" entries that turn Skull Cavern floors into Desert Tomb floors.
/// </summary>
internal static class DesertTombMineMapRegistry
{
    internal static void EditAdditionalMineMaps(
        AssetRequestedEventArgs e,
        TombChance tombChance,
        ModAssetHandler assetHandler
    )
    {
        int quantity = tombChance switch
        {
            TombChance.Low => 5,
            TombChance.Medium => 10,
            TombChance.High => 15,
            TombChance.Always => 39,
            _ => 1,
        };

        e.Edit(
            asset =>
            {
                var data = asset.AsDictionary<string, AdditionalMineMaps.MapData>().Data;

                for (int i = 0; i < quantity; i++)
                {
                    data[$"DesertTomb_{i}"] = new AdditionalMineMaps.MapData()
                    {
                        forceLevel = -1,
                        mapPath = assetHandler.GetAssetPath(Assets.MineLevel.Level77377, AssetPathType.GamePath),
                        mapType = "M",
                        maxLevel = -1,
                        minLevel = 121,
                    };
                }
            },
            AssetEditPriority.Default
        );
    }
}
