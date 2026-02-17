using System;
using System.Collections.Generic;
using System.Linq;
using RoosterAddon.Constants;
using RoosterAddon.Enums;
using RoosterAddon.Helpers;
using RoosterAddon.Application.FarmAnimals;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley.GameData.FarmAnimals;

namespace RoosterAddon.Application.Assets;

/// <summary>
/// Applies RoosterAddon edits to <c>Data/FarmAnimals</c>.
/// </summary>
internal sealed class FarmAnimalsAssetEditor
{
    private readonly IRoosterAssetEdits _ctx;
    private readonly FarmAnimalsRoosterEditsService _edits = new();
public FarmAnimalsAssetEditor(IRoosterAssetEdits context)
    {
        _ctx = context;
    }

    public void Edit(AssetRequestedEventArgs e)
    {
        e.Edit(
            asset =>
            {
                var data = asset.AsDictionary<string, FarmAnimalData>().Data;

                _ctx.EnsureRoostersSetup();
                foreach (var rooster in _ctx.RoosterHelper.Roosters.Values)
                    _edits.Apply(rooster, data, _ctx.Config, _ctx.RoosterHelper, _ctx.Helper.Translation, _ctx.ModTextures, _ctx.Config.General.EnableStuckInside, _ctx.IsStuckInside, _ctx.Helper.ModRegistry.IsLoaded("6480.blueegg"));

                foreach (var roosterFarmAnimal in RoosterControl.GetRoostersFromCoops())
                    roosterFarmAnimal.reload(roosterFarmAnimal.home);
            },
            AssetEditPriority.Late + 2000
        );

        e.Edit(
            asset =>
            {
                var data = asset.AsDictionary<string, FarmAnimalData>().Data;

                _ctx.RoosterHelper.UpdateRoosterQuantity_All();
                _ctx.GetRoostersFemales(data, _ctx.RoosterHelper.Roosters);
                _ctx.UpdateRoosterHatchingApi();

                foreach (var rooster in _ctx.RoosterHelper.Roosters.Values)
                    _ctx.RunHatchingSystem(rooster, data);

                if (_ctx.RoosterHelper.FemaleEggs.Count > 0)
                    _ctx.RequestInvalidate(_ctx.ObjectsAssetName);
            },
            AssetEditPriority.Late + 12500
        );

        e.Edit(
            asset =>
            {
                var data = asset.AsDictionary<string, FarmAnimalData>().Data;
                DebugDumpIfEnabled(new Dictionary<string, FarmAnimalData>(data));
            },
            AssetEditPriority.Late + 10000
        );
    }

    private void DebugDumpIfEnabled(Dictionary<string, FarmAnimalData> data)
    {
        if (!_ctx.Config.General.EnableDebug)
            return;

        var today = SDate.Now().DaysSinceStart;
        if (_ctx.LastDebugDumpDay == today)
            return;

        _ctx.LastDebugDumpDay = today;

        var validAnimalKeys = new List<string>();
        foreach (var rooster in _ctx.RoosterHelper.Roosters.Values)
        {
            validAnimalKeys.Add(rooster.FarmAnimalName);
            validAnimalKeys.Add(rooster.Female);
            rooster.Females.ForEach(female => validAnimalKeys.Add(female));
        }

        var farmAnimals = data
            .Where(a => validAnimalKeys.Contains(a.Key))
            .ToDictionary(a => a.Key, a => a.Value);

        var roostersExport = new RoosterDataExport(_ctx.RoosterHelper.Roosters);
        var farmAnimalsExport = new FarmAnimalsExport(farmAnimals);

        _ctx.QueueNextTickAction(() =>
        {
            _ctx.WriteStateDump(
                $"{StardewModdingAPI.Constants.SaveFolderName}_roosters_{today}.json",
                roostersExport
            );
            _ctx.WriteStateDump(
                $"{StardewModdingAPI.Constants.SaveFolderName}_farmAnimals_{today}.json",
                farmAnimalsExport
            );
        });

        Logger.LogDailyLuck(_ctx.DailyLuck, _ctx.Monitor);
        Logger.LogRoosterData(_ctx.RoosterHelper.Roosters, _ctx.Monitor);
        Logger.LogFarmAnimals(farmAnimals, _ctx.Monitor);
    }
}
