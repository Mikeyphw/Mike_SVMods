using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Mikew.NestCore.Helpers;
using Mikew.NestCore.Helpers.Content;
using Mikew.NestCore.Helpers.Config;
using Mikew.NestCore.Helpers.Logging;
using Mikew.NestCore.Helpers.SaveData;
using Mikew.NestCore.Integrations.Gmcm;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.FarmAnimals;
using EggsInWild.Application.Animals;
using EggsInWild.Application.Facade;
using EggsInWild.Application.Progression;
using EggsInWild.Core.Enums;
using EggsInWild.Core.ShopProgression;
using EggsInWild.Infrastructure.Api;
using EggsInWild.Infrastructure.Content;
using EggsInWild.Infrastructure.Content.Editors;
using EggsInWild.Infrastructure.Commands;
using EggsInWild.Infrastructure.Config;
using EggsInWild.Infrastructure.Diagnostics;
using EggsInWild.Infrastructure.Game;
using EggsInWild.Infrastructure.DesertTomb.Monsters.Definitions;
using EggsInWild.Application.DesertTomb.Runtime;
using EggsInWild.Application.DesertTomb.Generation;
using EggsInWild.Application.DesertTomb.Spawning;
using EggsInWild.Infrastructure.DesertTomb.Harmony;
using EggsInWild.Infrastructure.DesertTomb.Loot;
using EggsInWild.Infrastructure.DesertTomb.Loot.Definitions;
using EggsInWild.Infrastructure.DesertTomb.Monsters;
using EggsInWild.DesertTomb.Core.Configuration;
using EggsInWild.Infrastructure.ModData;
using EggsInWild.Infrastructure.ModData.Models;
using EggsInWild.Infrastructure.Assets;

namespace EggsInWild;

public sealed partial class ModEntry
{

#if WEGG_DEV
    private DevJsonTuningService? _devJsonTuning;
    private SpawnParametersJson? _devSpawnParametersJson;
    private Dictionary<string, TombMonsterDefinitionJson>? _devMonsterDefinitionsJson;

    private SpawnParametersJson DevSpawnParameters
    {
        get
        {
            if (_devSpawnParametersJson is not null)
                return _devSpawnParametersJson;

            var reader = new SmapiJsonFileReader(Helper);
            _devSpawnParametersJson = reader.ReadJsonFile<SpawnParametersJson>(ModDataPaths.SpawnParameters) ?? new SpawnParametersJson();
            return _devSpawnParametersJson;
        }
    }

    private void MutateDevSpawnParameters(Action<SpawnParametersJson> mutate)
    {
        mutate(DevSpawnParameters);
        Helper.Data.WriteJsonFile(ModDataPaths.SpawnParameters, DevSpawnParameters);
    }


    private Dictionary<string, TombMonsterDefinitionJson> DevMonsterDefinitions
    {
        get
        {
            if (_devMonsterDefinitionsJson is not null)
                return _devMonsterDefinitionsJson;

            var reader = new SmapiJsonFileReader(Helper);
            _devMonsterDefinitionsJson = reader.ReadJsonFile<Dictionary<string, TombMonsterDefinitionJson>>(DesertTombDefinitionPaths.MonsterDefinitions)
                ?? new Dictionary<string, TombMonsterDefinitionJson>(StringComparer.OrdinalIgnoreCase);

            return _devMonsterDefinitionsJson;
        }
    }

    private void MutateDevMonsterDefinitions(Action mutate)
    {
        mutate();
        Helper.Data.WriteJsonFile(DesertTombDefinitionPaths.MonsterDefinitions, DevMonsterDefinitions);
    }

    private static string NormalizeDevJsonKey(string raw)
        => string.IsNullOrWhiteSpace(raw) ? "empty" : new string(raw.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray());
#endif
}
