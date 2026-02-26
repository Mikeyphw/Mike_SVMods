using System.Collections.Generic;
using RoosterControl.Application.FarmAnimals;
using RoosterControl.Framework.Config;
using StardewValley.GameData.FarmAnimals;

namespace RoosterControl.Application;

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
            _i18n,
            ModTextures,
            enableStuckInside: Config.General.EnableStuckInside,
            isStuckInside: isStuckInside,
            blueEggModInstalled: HasMod("6480.blueegg", Helper)
        );
    }
}
