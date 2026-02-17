using System.Collections.Generic;
using RoosterAddon.Application.FarmAnimals;
using RoosterAddon.Framework.Config;
using StardewValley.GameData.FarmAnimals;

namespace RoosterAddon.Application;

internal sealed partial class RoosterCoordinator
{
    private readonly FarmAnimalsRoosterEditsService _farmAnimalsRoosterEdits = new();

    private void SetRoosterData(RoosterData rooster, IDictionary<string, FarmAnimalData> data)
    {
        _farmAnimalsRoosterEdits.Apply(
            rooster,
            data,
            Config,
            RoosterHelper,
            Helper.Translation,
            ModTextures,
            enableStuckInside: Config.General.EnableStuckInside,
            isStuckInside: isStuckInside,
            blueEggModInstalled: HasMod("6480.blueegg", Helper)
        );
    }
}
