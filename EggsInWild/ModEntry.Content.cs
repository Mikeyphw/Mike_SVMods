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

private void InstallHarmonyBootstrap()
    {
        if (_harmony is not null)
            return;

        _harmony = new Harmony($"{Constants.ModId}.harmony");
    }

    private void InitializeModAssetHandler()
    {
        _assetHandler = new ModAssetHandler(Constants.ModId, Monitor);
        _assetHandler.AddAssets(
            new()
            {
                new(
                    enumAssetType: typeof(WeggObjectTexture),
                    enumAssetFolder: WeggAssetFolder.objects,
                    fileExt: ModAssetHandler.FileExtension.Png,
                    gamePath: $"Mods/{Constants.ModId}/objects"
                ),
                new(
                    enumAssetType: typeof(WeggBigCraftableTexture),
                    enumAssetFolder: WeggAssetFolder.bigcraftables,
                    fileExt: ModAssetHandler.FileExtension.Png,
                    gamePath: $"Mods/{Constants.ModId}/bigcraftables"
                ),
                new(
                    enumAssetType: typeof(WeggMiscTexture),
                    enumAssetFolder: WeggAssetFolder.misc,
                    fileExt: ModAssetHandler.FileExtension.Png,
                    gamePath: $"Mods/{Constants.ModId}/misc"
                ),
                new(
                    enumAssetType: typeof(WeggMineMap),
                    enumAssetFolder: WeggAssetFolder.mines,
                    fileExt: ModAssetHandler.FileExtension.Tmx,
                    gamePath: "Maps/Mines"
                ),
                new(
                    enumAssetType: typeof(WeggMineTexture),
                    enumAssetFolder: WeggAssetFolder.mines,
                    fileExt: ModAssetHandler.FileExtension.Png,
                    gamePath: "Maps/Mines"
                )
            }
        );

        _assetHandler.SetUpAssets();
    }

    private void InitializeContentPipeline()
    {
        if (_config is null)
            return;

        _contentLog = new SmapiLog(Monitor);
        _contentDataLoader = new ConfiguredModDataLoader(new ModDataLoader(JsonFileReaderFactory.Create(Helper), _contentLog), _config!, _contentLog);

        var saveData = new SmapiSaveDataStore(Helper, Monitor);
        var game = new GameStateReader();
        var quests = new GameQuestService();
        var mail = new GameMailService();
        var recipes = new GameRecipeService();
        var unlockFacts = new GameUnlockFactsProvider(game, mail, quests);

        _contentProgression = new ProgressionService(
            store: saveData,
            config: _config,
            facts: unlockFacts,
            quests: quests,
            mail: mail,
            recipes: recipes,
            log: _contentLog,
            invalidateAssets: InvalidateGameAssets);

        var chosenEggAssignments = new ChosenEggAssignments(_contentDataLoader, Helper.ModRegistry, _contentLog);
        _contentPipeline = new AssetPipelineHost<ContentContext>(Helper, Monitor, CreateContentContext);
        if (_assetHandler is not null)
            _contentPipeline.RegisterModAssets(_assetHandler, order: -100);

        _contentPipeline.RegisterEditor(new MonstersEditor(), order: 10, shouldApply: (ctx, _) => ctx.Step >= ProgressionStep.PartI);
        _contentPipeline.RegisterEditor(new FarmAnimalsEditor(chosenEggAssignments), order: 20, shouldApply: (ctx, _) => ctx.Step >= ProgressionStep.PartI);
        _contentPipeline.RegisterEditor(new ObjectsEditor(), order: 30, shouldApply: (ctx, _) => ctx.Step >= ProgressionStep.PartI);
        _contentPipeline.RegisterEditor(new BigCraftablesEditor(), order: 40, shouldApply: (ctx, _) => ctx.Step >= ProgressionStep.PartI);
        _contentPipeline.RegisterEditor(new MachinesEditor(), order: 50, shouldApply: (ctx, _) => ctx.Step >= ProgressionStep.PartI);
        _contentPipeline.RegisterEditor(new CraftingRecipesEditor(), order: 60, shouldApply: (ctx, _) => ctx.Step >= ProgressionStep.PartI);
        _contentPipeline.RegisterEditor(new MailEditor(), order: 70, shouldApply: (ctx, _) => ctx.Step >= ProgressionStep.PartI);
        _contentPipeline.RegisterEditor(new QuestEditor(), order: 80, shouldApply: (ctx, _) => ctx.Step >= ProgressionStep.PartI);
        _contentPipeline.RegisterEditor(new SpecialOrdersEditor(), order: 90, shouldApply: (ctx, _) => ctx.Step >= ProgressionStep.PartI);
        _contentPipeline.RegisterEditor(new WildEggIncubatorTextureEditor(), order: 100, shouldApply: (ctx, _) => ctx.Step >= ProgressionStep.PartI);
        _contentPipeline.Attach();
    }

    private ContentContext CreateContentContext()
    {
        var config = _config?.Get() ?? new ModConfig();
        var log = _contentLog ?? new SmapiLog(Monitor);
        var i18n = new I18TranslationHelper(Helper);
        var configProvider = _config ?? new SmapiConfigProvider<ModConfig>(Helper, Monitor);
        var dataLoader = _contentDataLoader ?? new ConfiguredModDataLoader(new ModDataLoader(JsonFileReaderFactory.Create(Helper), log), configProvider, log);
        return new ContentContext(
            helper: Helper,
            log: log,
            i18n: i18n,
            config: config,
            step: GetContentProgressionStep(),
            modData: dataLoader.Load());
    }

    private ProgressionStep GetContentProgressionStep()
    {
        if (_config is null)
            return ProgressionStep.PartI;

        if (_contentProgression is not null)
            return _contentProgression.GetCurrentStep();

        return _config.Get().ProgressionMode ? ProgressionStep.PartI : ProgressionRules.MaxStep;
    }

    private void InvalidateGameAssets()
    {
        Helper.GameContent.InvalidateCache("Data/FarmAnimals");
        Helper.GameContent.InvalidateCache("Data/Objects");
        Helper.GameContent.InvalidateCache("Data/Machines");
        Helper.GameContent.InvalidateCache("Data/Mail");
        Helper.GameContent.InvalidateCache("Data/Quests");
        Helper.GameContent.InvalidateCache("Data/CraftingRecipes");
        Helper.GameContent.InvalidateCache("Data/SpecialOrders");
    }
}
