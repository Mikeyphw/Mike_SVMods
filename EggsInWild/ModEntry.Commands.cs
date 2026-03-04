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

    private void RegisterConsoleCommands(IModHelper helper)
    {
        if (_config is null)
            return;

        var log = new SmapiLog(Monitor);
        var i18n = new I18TranslationHelper(helper);
        var saveData = new SmapiSaveDataStore(helper, Monitor);
        var game = new GameStateReader();
        var world = new WorldLocations();
        var items = new GameItemRegistry();
        var tileFinder = new GameTileFinder();
        var placer = new GameObjectPlacer();
        var histogram = new EggsInWild.Application.Spawning.Diagnostics.TileSearchHistogram();

        var quests = new GameQuestService();
        var mail = new GameMailService();
        var recipes = new GameRecipeService();
        var unlockFacts = new GameUnlockFactsProvider(game, mail, quests);

        var progression = new EggsInWild.Application.Progression.ProgressionService(
            store: saveData,
            config: _config,
            facts: unlockFacts,
            quests: quests,
            mail: mail,
            recipes: recipes,
            log: log,
            invalidateAssets: InvalidateGameAssets);

        var dataLoader = new ConfiguredModDataLoader(new ModDataLoader(JsonFileReaderFactory.Create(helper), log), _config!, log);
        var mods = new EggsInWild.Infrastructure.Mods.SmapiModAvailability(helper.ModRegistry);
        var spawnWeather = new EggsInWild.Application.Spawning.SpawnWeatherHistory(saveData, log);
        var conditions = new EggsInWild.Application.Spawning.DefaultAreaConditionsEvaluator(game, spawnWeather);
        var planner = new EggsInWild.Application.Spawning.SpawnPlanner(
            world,
            mods,
            conditions,
            log);
        var spawnService = new EggsInWild.Application.Spawning.EggSpawnService(world, placer, tileFinder, log, histogram);
        var spawner = new EggsInWild.Application.Spawning.WildEggSpawnOrchestrator(dataLoader, game, planner, spawnService, log);

        var animalNotes = new EggsInWild.Application.AnimalNotes.AnimalNotesOrchestrator(
            store: saveData,
            dataLoader: dataLoader,
            game: game,
            i18n: i18n,
            debugMode: _config.Get().AnimalNotesDebugMode,
            warnAfterDays: _config.Get().AnimalNotesWarnAfterDays,
            placementRadius: _config.Get().AnimalNotesPlacementRadius,
            mail: mail,
            quests: quests,
            placer: placer,
            progression: progression,
            log: log,
            getStep: progression.GetCurrentStep,
            isMainPlayer: () => Context.IsMainPlayer,
            sendQueueMailMessage: null);

        var animalNotesValidator = new EggsInWild.Application.AnimalNotes.AnimalNotesStartupValidator(
            dataLoader,
            log,
            progression.GetCurrentStep);

        var verifier = new WeggVerifier(helper, saveData, _config, dataLoader, log);

        var facade = new ModFacade(
            store: saveData,
            config: _config,
            log: log,
            isWorldReady: () => Context.IsWorldReady,
            isMainPlayer: () => Context.IsMainPlayer,
            reloadAll: () =>
            {
                _config.Reload();
                InvalidateGameAssets();
            },
            progression: progression,
            spawner: spawner,
            animalNotes: animalNotes,
            animalNotesValidator: animalNotesValidator,
            world: world,
            items: items,
            game: game,
            tileFinder: tileFinder,
            placer: placer,
            dataLoader: dataLoader,
            mods: mods,
            verifier: verifier,
            histogram: histogram
#if WEGG_DEV
            ,
            forceInstallDesertTombPatches: () =>
            {
                var wasInstalled = _desertTombHarmonyInstalled;
                TryInstallDesertTombHarmonyPatches(reason: "console command wegg tomb force_patches", force: true);
                var isInstalled = _desertTombHarmonyInstalled;

                if (isInstalled && !wasInstalled)
                    return new EggsInWild.Application.Facade.FacadeResult(true, "DesertTomb Harmony patches installed.");
                if (isInstalled && wasInstalled)
                    return new EggsInWild.Application.Facade.FacadeResult(true, "DesertTomb Harmony patches already installed.");
                return new EggsInWild.Application.Facade.FacadeResult(false, "DesertTomb Harmony patches not installed (see log).");
            }
#endif

        );

        var dispatcher = new WeggCommandDispatcher(facade, i18n);
        var commands = new WeggConsoleCommands(log, i18n, dispatcher);
        commands.Register(helper);
    }
}
