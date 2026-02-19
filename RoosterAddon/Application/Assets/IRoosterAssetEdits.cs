using System;
using System.Collections.Generic;
using RoosterAddon.Enums;
using RoosterAddon.Framework.Config;
using RoosterAddon.Helpers;
using StardewModdingAPI;
using StardewValley.GameData.FarmAnimals;
using StardewValley.GameData.Objects;

using LuckyTypes = Mikew.BirdsOfAFeather.EnumsAndStrings.Enums.LuckyTypes;

namespace RoosterAddon.Application.Assets;

/// <summary>
/// Narrow coordinator surface required by asset editors.
/// </summary>
internal interface IRoosterAssetEdits
{
    bool HasDayStarted { get; }
    LuckyTypes DailyLuck { get; }
    int LastDebugDumpDay { get; set; }

    string FarmAnimalsAssetName { get; }
    string ObjectsAssetName { get; }

    IModHelper Helper { get; }
    IMonitor Monitor { get; }
    IManifest ModManifest { get; }

    RoosterAddonConfig Config { get; }
    RoosterControl RoosterHelper { get; }

    Dictionary<Textures, Dictionary<TextureDetails, string>> ModTextures { get; }
    bool IsStuckInside { get; }

    bool CloneEggsEnabled { get; }

    void EnsureRoostersSetup();

    void SetRoosterData(RoosterData rooster, IDictionary<string, FarmAnimalData> data);
    void RunHatchingSystem(RoosterData rooster, IDictionary<string, FarmAnimalData> data);
    void GetRoostersFemales(IDictionary<string, FarmAnimalData> animalData, Dictionary<RoosterType, RoosterData> roosters);
    void UpdateRoosterHatchingApi();

    void RequestInvalidate(string assetName);
    void QueueNextTickAction(Action action);
    void WriteStateDump(string fileName, object payload);
}
