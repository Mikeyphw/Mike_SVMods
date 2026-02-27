using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using LetMeHatch.Core.Breeding;
using LetMeHatch.Application;
using LetMeHatch.Application.Gateways;
using LetMeHatch.Infrastructure.Gateways;
using LetMeHatch.Infrastructure.ApplicationAdapters;
using System.Collections.Generic;
using LetMeHatch.Config;
using LetMeHatch.Framework;
using LetMeHatch.GameAssetManagement;
using Mikew.NestCore.Helpers.Config;
using Mikew.NestCore.Helpers.Logging;
using LetMeHatch.Infrastructure.Content;
using LetMeHatch.Infrastructure.Content.Loaders;
using Mikew.NestCore.Helpers.Content;
using LetMeHatch.Integrations.Gmcm;
using Mikew.NestCore.Framework;
using Mikew.NestCore.Framework.Random;
using Mikew.NestCore.Helpers;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using static LetMeHatch.EnumsAndStrings.Enums;
using static LetMeHatch.EnumsAndStrings.Strings;
using static Mikew.NestCore.EnumsAndStrings.Strings;
using LogScopedHelper = Mikew.NestCore.Helpers.Logging.LogScopedHelper;
using ScopedLogCatalog = Mikew.NestCore.Helpers.Logging.ScopedLogCatalog;

namespace LetMeHatch.Application.Coordination;

public sealed class LmhCoordinator
{
    private readonly CoreContext _core;
    private readonly SmapiConfigProvider<ModConfig> _config;
    private readonly ILog _log;
    private readonly AssetInvalidationBatcher _invalidation;
    private readonly LetMeHatchApi _api;

    private IRoosterHatchingAPI? _roosterHatching;
    private IWildEggsApi? _wildEggs;

    private GameAssetManager? _assetManager;
    private LmhApplicationServices? _app;
    private AssetPipelineHost<ContentContext>? _assetPipeline;

    private readonly HashSet<string> _addedTokens = new();

    private bool _earlyInitRequested;

    public LmhCoordinator(CoreContext core, SmapiConfigProvider<ModConfig> config, ILog log, LetMeHatchApi api, AssetInvalidationBatcher invalidation)
    {
        _core = core ?? throw new ArgumentNullException(nameof(core));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _invalidation = invalidation ?? throw new ArgumentNullException(nameof(invalidation));
    }

    public void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        InitializeApis();

        LmhGmcmMenu.TryRegister(
            core: _core,
            configProvider: _config,
            onSaved: () =>
            {
                InvalidateAllContent();
                InvalidateGameContentCache();
            }
        );
    }

    public void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        RequestEarlyInit();
    }

    public void OnWarped(object? sender, WarpedEventArgs e)
    {
        _invalidation.Request(AssetNames.Machines);
    }

    public void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        if (_assetManager is null)
        {
            return;
        }

        DynamicCloneObjectTextureLoader.ClearCache();

        if (_config.Get().CloneVanillaEggs && _assetManager.ItemsToAdd.Count <= 0)
        {
            _assetManager.ItemManager.CloneItemsFromGame();
        }

#if MIKE_DEV
        #endif

        _invalidation.Request(AssetNames.FarmAnimals);
        _invalidation.Request(AssetNames.Objects);
        _invalidation.Request(AssetNames.SpringObjects);
    }

    public void OnDayEnding(object? sender, DayEndingEventArgs e)
    {
        _assetManager?.DayEndingReset();
    }

    public void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!_earlyInitRequested)
        {
            return;
        }

        if (!Context.IsWorldReady || e.Ticks < 5)
        {
            return;
        }

        if (_assetManager is not null)
        {
            _core.Helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
            _earlyInitRequested = false;
            return;
        }

        _core.Helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
        _earlyInitRequested = false;

        if (_wildEggs is null)
        {
            LogScopedHelper.Log(_log, ScopedLogCatalog.ModLthteCategory.Runtime, "Skipping early initialization because Wild Eggs API is unavailable.", LogLevel.Warn);
            return;
        }

        InitializeAssetEditor();
        DynamicCloneObjectTextureLoader.ClearCache();
        _api.SetGameAssetManager(_assetManager!);


        InvalidateGameContentCache();
        HandleBreedingData();

        _assetPipeline?.Attach();
        _api.SetFinishedEarlyStuff(true);
    }

    public void RequestEarlyInit()
    {
        _earlyInitRequested = true;

        _core.Helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
        _core.Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
    }

    public void CheckI18n()
    {
        var jsonFile = _core.Helper.Data.ReadJsonFile<Dictionary<string, string>>("i18n/default.json") ?? new();
        _core.Helper.Data.WriteJsonFile("i18n/default.json.bak", jsonFile);

        ModEntry.TranslationHelper.Checki18nKeys(_log);
    }


    public void PrintContentHandlers()
    {
        if (_assetPipeline is null)
        {
            LogScopedHelper.Log(_log, ScopedLogCatalog.ModLthteCategory.ContentPipeline, "Content pipeline is not initialized yet.", LogLevel.Warn);
            return;
        }

        LogScopedHelper.Log(
            _log,
            ScopedLogCatalog.ModLthteCategory.ContentPipeline,
            "NestCore asset pipeline host is initialized and attached on early init.",
            LogLevel.Info);
    }

    public void InvalidateAllContent()
    {
        if (_assetPipeline is null)
        {
            LogScopedHelper.Log(_log, ScopedLogCatalog.ModLthteCategory.ContentPipeline, "Content pipeline is not initialized yet.", LogLevel.Warn);
            return;
        }

        _assetPipeline.RequestInvalidateAll();

        LogScopedHelper.Log(_log, ScopedLogCatalog.ModLthteCategory.ContentPipeline, "Invalidated caches for all registered content handlers.", LogLevel.Info);
    }

    public void PrintStatus()
    {
        var lines = new List<string>
        {
            $"WorldReady={Context.IsWorldReady}",
            $"MainPlayer={Context.IsMainPlayer}",
            $"EarlyInitRequested={_earlyInitRequested}",
            $"WildEggsApiReady={_wildEggs is not null}",
            $"RoosterApiReady={_roosterHatching is not null}",
            $"AssetManagerReady={_assetManager is not null}",
            $"AssetPipelineReady={_assetPipeline is not null}",
            $"ApplicationServicesReady={_app is not null}",
        };

        foreach (var line in lines)
            LogScopedHelper.Log(_log, ScopedLogCatalog.ModLthteCategory.Runtime, line, LogLevel.Info);
    }

    private void InitializeApis()
    {
        _roosterHatching = _core.Helper.ModRegistry.GetApi<IRoosterHatchingAPI>("Mikew.roostersaddon.smapi");
        _wildEggs = _core.Helper.ModRegistry.GetApi<IWildEggsApi>("Mikew.eggsinwild");

        if (_roosterHatching is null)
        {
            throw new InvalidOperationException(
                "LetMeHatch requires Rooster's Addon (Mikew.roostersaddon.smapi). Install/enable it and try again."
            );
        }

        if (_wildEggs is null)
        {
            throw new InvalidOperationException(
                "LetMeHatch requires EggsInWild (Mikew.eggsinwild). Install/enable it and try again."
            );
        }
    }

private void InitializeAssetEditor()
{
    if (_wildEggs is null)
    {
        return;
    }

    _assetManager = new GameAssetManager(_wildEggs);
    ModEntry.CurrentAssetManager = _assetManager;
    _assetManager.TextureManager.SetUpModAssets();

    _app = new LmhApplicationServices(
        new GameRandomProvider(),
        new LmhGateways(
            new SmapiGameDataGateway(_core.Helper.GameContent),
            new SmapiWorldGateway(),
            new SmapiClock(),
            new SmapiAssetGateway(_invalidation)
        ),
        new FarmAnimalsAssetEditsAdapter(_assetManager),
        new ObjectsAssetEditsAdapter(_assetManager),
        new MachinesAssetEditsAdapter(_assetManager),
        new AssetLoadsAdapter(_assetManager)
    );


    _assetPipeline = LetMeHatchContentPipeline.Create(
        _core,
        _config,
        _log,
        ModEntry.AssetHandler,
        _assetManager,
        _app
    );
}

    private void InvalidateGameContentCache()
    {
        if (_assetManager is null)
        {
            return;
        }

        DynamicCloneObjectTextureLoader.ClearCache();

        _invalidation.Request(AssetNames.FarmAnimals);
        _invalidation.Request(AssetNames.Objects);

        _assetManager.AnimalItemRelationsManager.HandleAnimalEggRelations();
        GameAssetManager.SaveData(_assetManager.SavedItemsData);
        _assetManager.ItemManager.CloneItemsFromGame();
    }

    private void HandleBreedingData()
    {
        if (_assetManager is null)
        {
            return;
        }

        DynamicCloneObjectTextureLoader.ClearCache();

        _assetManager.BreedingDataLoader.LoadBreedingData();

        var loadedFemaleBreedingData = _assetManager.LoadedFemaleBreedingData;
        var breedingConfigsByPack = _assetManager.BreedingConfigsByPack;

        foreach (var (contentPackID, breedingDataFemaleDic) in loadedFemaleBreedingData)
        {
            breedingConfigsByPack.TryAdd(contentPackID, new());

            foreach (var (femaleKey, femaleBreedingData) in breedingDataFemaleDic)
            {
                breedingConfigsByPack[contentPackID].TryAdd(femaleKey, new(femaleBreedingData));

                foreach (var male in femaleBreedingData.Males)
                {
                    breedingConfigsByPack[contentPackID][femaleKey].BoostByMale.TryAdd(male.Key, male.Value.BoostByMale);
                    breedingConfigsByPack[contentPackID][femaleKey].MaleChance.TryAdd(male.Key, male.Value.MaleChance);
                    breedingConfigsByPack[contentPackID][femaleKey].Enabled.TryAdd(male.Key, male.Value.Enabled);
                }
            }
        }
    }




public void DebugBreedingDumpPlans(string pathArg, string femaleKeyArg)
{
    if (!Context.IsWorldReady || _assetManager is null)
    {
        _log.Log("[breeding dump plans] World not ready or asset manager not initialized.", LogLevel.Warn);
        return;
    }

    var farmAnimals = Game1.farmAnimalData;
    var femaleKeyFilter = string.IsNullOrWhiteSpace(femaleKeyArg) ? null : femaleKeyArg.Trim();

    var plans = _assetManager.BreedingDataLoader.BuildPlansForToday(
        farmAnimals,
        ModEntry.Config,
        _assetManager.BreedingConfigsByPack,
        femaleKeyFilter
    );

    var dump = plans.Select(ToDump).ToList();
    var json = JsonSerializer.Serialize(dump, new JsonSerializerOptions { WriteIndented = true });

    var defaultName = $"breeding_plans_{Game1.Date.Year}_{Game1.Date.Season}_{Game1.Date.DayOfMonth}.json";
    var path = ResolveOutputPath(pathArg, defaultName);

    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    File.WriteAllText(path, json);

    _log.Log($"[breeding dump plans] Wrote {dump.Count} plan(s) to: {path}", LogLevel.Info);
}

public void DebugBreedingDumpCoopIndex(string pathArg)
{
    if (!Context.IsWorldReady)
    {
        _log.Log("[breeding dump coop index] World not ready.", LogLevel.Warn);
        return;
    }

    var index = CoopBreedingIndex.Build(Game1.getFarm());
    var dump = index.GetAdultCountsByCoop();

    var json = JsonSerializer.Serialize(dump, new JsonSerializerOptions { WriteIndented = true });
    var defaultName = $"breeding_coop_index_{Game1.Date.Year}_{Game1.Date.Season}_{Game1.Date.DayOfMonth}.json";
    var path = ResolveOutputPath(pathArg, defaultName);

    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    File.WriteAllText(path, json);

    _log.Log($"[breeding dump coop index] Wrote coop index (coops={dump.Count}) to: {path}", LogLevel.Info);
}

public void DebugBreedingValidatePlans(string femaleKeyArg)
{
    if (!Context.IsWorldReady || _assetManager is null)
    {
        _log.Log("[breeding validate plans] World not ready or asset manager not initialized.", LogLevel.Warn);
        return;
    }

    var farmAnimals = Game1.farmAnimalData;
    var femaleKeyFilter = string.IsNullOrWhiteSpace(femaleKeyArg) ? null : femaleKeyArg.Trim();

    var plans = _assetManager.BreedingDataLoader.BuildPlansForToday(
        farmAnimals,
        ModEntry.Config,
        _assetManager.BreedingConfigsByPack,
        femaleKeyFilter
    );

    var validator = new BreedingPlanValidator();

    var invalid = 0;
    foreach (var plan in plans)
    {
        var result = validator.Validate(plan);
        if (result.IsValid)
        {
            continue;
        }

        invalid++;
        _log.Log($"[breeding validate plans] INVALID planId={plan.PlanId} errors={result.Errors.Count}", LogLevel.Warn);
        foreach (var err in result.Errors)
        {
            _log.Log($"[breeding validate plans] - {err}", LogLevel.Warn);
        }
    }

    if (invalid == 0)
    {
        _log.Log($"[breeding validate plans] OK ({plans.Count} plan(s)).", LogLevel.Info);
    }
}

private static BreedingPlanDump ToDump(BreedingPlan plan)
{
    return new BreedingPlanDump(
        plan.PlanId,
        plan.InGameDate,
        plan.Seed,
        plan.ScopeAnimalKeys.ToList(),
        plan.Pairs.Select(p => new BreedingPairDump(
            p.FemaleAnimalKey,
            p.MaleAnimalKey,
            p.OtherMaleKeys.ToList(),
            p.Mode.ToString(),
            p.EggToClearItemId,
            p.EggToAssignItemId,
            p.FertilizedSucceeded,
            p.MaleQuantity,
            p.BaseFertilizedChance,
            p.AdjustedFertilizedChance,
            p.MaleChance,
            p.BoostByMale,
            p.ApplyDailyLuckBoost,
            p.SelectedEggType
        )).ToList(),
        plan.Mutations.Select(m => m.ToString() ?? string.Empty).ToList(),
        plan.Diagnostics.ToList()
    );
}

private static string ResolveOutputPath(string pathArg, string defaultFileName)
{
    var modDir = ModEntry.HelperInstance.DirectoryPath;
    var baseDir = Path.Combine(modDir, "data");

    if (string.IsNullOrWhiteSpace(pathArg))
    {
        return Path.Combine(baseDir, defaultFileName);
    }

    var candidate = pathArg.Trim();
    if (!Path.IsPathRooted(candidate))
    {
        candidate = Path.Combine(baseDir, candidate);
    }

    if (Directory.Exists(candidate))
    {
        return Path.Combine(candidate, defaultFileName);
    }

    return candidate;
}

private sealed record BreedingPlanDump(
    string PlanId,
    DateOnly InGameDate,
    int Seed,
    List<string> ScopeAnimalKeys,
    List<BreedingPairDump> Pairs,
    List<string> Mutations,
    List<string> Diagnostics);

private sealed record BreedingPairDump(
    string FemaleAnimalKey,
    string MaleAnimalKey,
    List<string> OtherMaleKeys,
    string Mode,
    string? EggToClearItemId,
    string? EggToAssignItemId,
    bool FertilizedSucceeded,
    int MaleQuantity,
    double BaseFertilizedChance,
    double AdjustedFertilizedChance,
    double MaleChance,
    double BoostByMale,
    bool ApplyDailyLuckBoost,
    string? SelectedEggType);

public void DebugCloneTexturePreview(string cloneId)
{
    if (string.IsNullOrWhiteSpace(cloneId))
    {
        _log.Log("[clone texture preview] Usage: lmh_clone_texture_preview <cloneId>", LogLevel.Info);
        return;
    }

    if (!Core.Cloning.CloneIdService.TryParseCloneId(cloneId, out var animalKey, out var originalItemId, out _))
    {
        _log.Log($"[clone texture preview] Not a recognized clone id: {cloneId}", LogLevel.Warn);
        return;
    }

    if (!StardewValley.Game1.objectData.TryGetValue(originalItemId, out var original))
    {
        _log.Log($"[clone texture preview] Original object not found: {originalItemId}", LogLevel.Warn);
        return;
    }

    StardewValley.Game1.objectData.TryGetValue(cloneId, out var cloneData);

    var (tint, tintSource) = Infrastructure.Content.Loaders.DynamicCloneObjectTextureLoader.ResolveTintWithSource(
        cloneId,
        animalKey,
        originalItemId,
        cloneData,
        original);

    var assetKey = $"Mods/{ModEntry.ModManifestInstance.UniqueID}/Objects/Clones/{cloneId}";

    _log.Log($"[clone texture preview] cloneId={cloneId}", LogLevel.Info);
    _log.Log($"[clone texture preview] originalItemId={originalItemId} name={original.DisplayName ?? original.Name}", LogLevel.Info);
    _log.Log($"[clone texture preview] dynamicEnabled={ModEntry.Config.UseDynamicCloneTextures} tintMode={ModEntry.Config.DynamicCloneTintMode} tint={tint} tintSource={tintSource}", LogLevel.Info);
    _log.Log($"[clone texture preview] assetKey={assetKey}", LogLevel.Info);
}


public void DebugCloneTextureHealthcheck(string countArg)
{
    var count = TryParsePositiveInt(countArg, defaultValue: 25, maxValue: 500);
    if (StardewValley.Game1.objectData is null || StardewValley.Game1.objectData.Count == 0)
    {
        _log.Log("[clone texture healthcheck] Game1.objectData is not ready yet.", LogLevel.Warn);
        return;
    }

    var cloneIds = new List<string>(capacity: Math.Min(count, 256));

    foreach (var id in StardewValley.Game1.objectData.Keys)
    {
        if (!Core.Cloning.CloneIdService.IsClonedItem(id))
            continue;

        cloneIds.Add(id);
        if (cloneIds.Count >= count)
            break;
    }
if (cloneIds.Count == 0)
    {
        _log.Log("[clone texture healthcheck] No clone ids found in object data.", LogLevel.Info);
        return;
    }

    var ok = 0;
    var fail = 0;
    var assetPrefix = $"Mods/{ModEntry.ModManifestInstance.UniqueID}/Objects/Clones/";

    foreach (var cloneId in cloneIds)
    {
        try
        {
            _ = StardewValley.Game1.content.Load<Microsoft.Xna.Framework.Graphics.Texture2D>(assetPrefix + cloneId);
            ok++;
        }
        catch (Exception ex)
        {
            fail++;
            if (fail <= 5)
                _log.Log($"[clone texture healthcheck] FAIL cloneId={cloneId}: {ex.Message}", LogLevel.Warn);
        }
    }

    _log.Log($"[clone texture healthcheck] checked={cloneIds.Count} ok={ok} fail={fail} cacheCount={Infrastructure.Content.Loaders.DynamicCloneObjectTextureLoader.CacheCount}", LogLevel.Info);
}

public void DebugCloneTextureStats(string maxKeysArg)
{
    var maxKeys = TryParsePositiveInt(maxKeysArg, defaultValue: 10, maxValue: 100);
    var keys = Infrastructure.Content.Loaders.DynamicCloneObjectTextureLoader.GetCacheKeys(maxKeys);

    _log.Log($"[clone texture stats] cacheCount={Infrastructure.Content.Loaders.DynamicCloneObjectTextureLoader.CacheCount} dynamicEnabled={ModEntry.Config.UseDynamicCloneTextures} tintMode={ModEntry.Config.DynamicCloneTintMode}", LogLevel.Info);
    if (keys.Length > 0)
        _log.Log($"[clone texture stats] keys={string.Join(", ", keys)}", LogLevel.Info);
}

public void DebugCloneTextureClearCache()
{
    var before = Infrastructure.Content.Loaders.DynamicCloneObjectTextureLoader.CacheCount;
    Infrastructure.Content.Loaders.DynamicCloneObjectTextureLoader.ClearCache();
    _log.Log($"[clone texture clear cache] cleared={before}", LogLevel.Info);
}

private static int TryParsePositiveInt(string? arg, int defaultValue, int maxValue)
{
    if (!string.IsNullOrWhiteSpace(arg) && int.TryParse(arg, out var parsed) && parsed > 0)
        return Math.Min(parsed, maxValue);

    return defaultValue;
}


}
