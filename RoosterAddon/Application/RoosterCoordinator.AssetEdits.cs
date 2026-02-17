using System;
using System.Collections.Generic;
using RoosterAddon.Application.Assets;
using RoosterAddon.Enums;
using RoosterAddon.Framework.Config;
using RoosterAddon.Helpers;
using StardewModdingAPI;
using StardewValley.GameData.FarmAnimals;
using StardewValley.GameData.Objects;

using LuckyTypes = Mikew.BirdsOfAFeather.EnumsAndStrings.Enums.LuckyTypes;

namespace RoosterAddon.Application;

internal sealed partial class RoosterCoordinator : IRoosterAssetEdits
{
    bool IRoosterAssetEdits.HasDayStarted => HasDayStarted;
    LuckyTypes IRoosterAssetEdits.DailyLuck => DailyLuck;
    int IRoosterAssetEdits.LastDebugDumpDay
    {
        get => _lastDebugDumpDay;
        set => _lastDebugDumpDay = value;
    }

    string IRoosterAssetEdits.FarmAnimalsAssetName => FarmAnimalsAssetName;
    string IRoosterAssetEdits.ObjectsAssetName => ObjectsAssetName;

    IModHelper IRoosterAssetEdits.Helper => _host.Helper;
    IMonitor IRoosterAssetEdits.Monitor => _host.Monitor;
    IManifest IRoosterAssetEdits.ModManifest => _host.ModManifest;

    RoosterAddonConfig IRoosterAssetEdits.Config => Config;
    RoosterControl IRoosterAssetEdits.RoosterHelper => RoosterHelper;

    Dictionary<Textures, Dictionary<TextureDetails, string>> IRoosterAssetEdits.ModTextures => ModTextures;
    bool IRoosterAssetEdits.IsStuckInside => isStuckInside;

    bool IRoosterAssetEdits.CloneEggsEnabled => HatchApi is not null && HatchApi.GetClonedAnimalsInfo().Count > 0;

    void IRoosterAssetEdits.EnsureRoostersSetup() => EnsureRoostersSetup();

    void IRoosterAssetEdits.SetRoosterData(RoosterData rooster, IDictionary<string, FarmAnimalData> data) =>
        SetRoosterData(rooster, data);

    void IRoosterAssetEdits.RunHatchingSystem(RoosterData rooster, IDictionary<string, FarmAnimalData> data) =>
        RunHatchingSystem(rooster, data);

    void IRoosterAssetEdits.GetRoostersFemales(
        IDictionary<string, FarmAnimalData> animalData,
        Dictionary<RoosterType, RoosterData> roosters
    ) => GetRoostersFemales(animalData, roosters);

    void IRoosterAssetEdits.UpdateRoosterHatchingApi() => UpdateRoosterHatchingApi();

    void IRoosterAssetEdits.RequestInvalidate(string assetName) => RequestInvalidate(assetName);

    void IRoosterAssetEdits.QueueNextTickAction(Action action) => QueueNextTickAction(action);

    void IRoosterAssetEdits.WriteStateDump(string fileName, object payload) => WriteStateDump(fileName, payload);
}
