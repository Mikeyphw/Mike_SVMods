using System.Collections.Generic;
using System.Linq;
using RoosterControl.Helpers;
using StardewModdingAPI.Utilities;
using StardewValley.GameData.FarmAnimals;

namespace RoosterControl.Application;

internal sealed partial class RoosterCoordinator
{
    private void DebugDumpFarmAnimalsIfEnabled(Dictionary<string, FarmAnimalData> data)
    {
        if (!Config.General.EnableDebug)
            return;

        var today = SDate.Now().DaysSinceStart;
        if (_lastDebugDumpDay == today)
            return;

        _lastDebugDumpDay = today;

        var validAnimalKeys = new List<string>();
        foreach (var rooster in RoosterHelper.Roosters.Values)
        {
            validAnimalKeys.Add(rooster.FarmAnimalName);
            validAnimalKeys.Add(rooster.Female);
            rooster.Females.ForEach(female => validAnimalKeys.Add(female));
        }

        var farmAnimals = data
            .Where(a => validAnimalKeys.Contains(a.Key))
            .ToDictionary(a => a.Key, a => a.Value);

        var roostersExport = new RoosterDataExport(RoosterHelper.Roosters);
        var farmAnimalsExport = new FarmAnimalsExport(farmAnimals);

        QueueNextTickAction(() =>
        {
            WriteStateDump(
                $"{StardewModdingAPI.Constants.SaveFolderName}_roosters_{today}.json",
                roostersExport
            );
            WriteStateDump(
                $"{StardewModdingAPI.Constants.SaveFolderName}_farmAnimals_{today}.json",
                farmAnimalsExport
            );
        });

        Logger.LogDailyLuck(DailyLuck, Monitor);
        Logger.LogRoosterData(RoosterHelper.Roosters, Monitor);
        Logger.LogFarmAnimals(farmAnimals, Monitor);
    }
}
