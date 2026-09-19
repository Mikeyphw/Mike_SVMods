using LetMeHatch.Infrastructure.Breeding;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using LetMeHatch.Core.Breeding;
using LetMeHatch.Core.Breeding.Diagnostics;
using LetMeHatch.Application;
using LetMeHatch.Application.Gateways;
using LetMeHatch.Infrastructure.Gateways;
using LetMeHatch.Infrastructure.ApplicationAdapters;
using System.Collections.Generic;
using LetMeHatch.Config;
using LetMeHatch.Framework;
using LetMeHatch.GameAssetManagement;
using Mikew.NestCore.Diagnostics;
using Mikew.NestCore.Helpers.Config;
using Mikew.NestCore.Helpers.Logging;
using LetMeHatch.Infrastructure.Content;
using LetMeHatch.Infrastructure.Content.Loaders;
using Mikew.NestCore.Helpers.Content;
using LetMeHatch.Integrations.Gmcm;
using Mikew.NestCore.Framework;
using Mikew.NestCore.Framework.Random;
using Mikew.NestCore.Helpers;
using LetMeHatch.Infrastructure.Content.Editors;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.FarmAnimals;
using static LetMeHatch.EnumsAndStrings.Enums;
using static LetMeHatch.EnumsAndStrings.Strings;
using static Mikew.NestCore.EnumsAndStrings.Strings;
using LogScopedHelper = Mikew.NestCore.Helpers.Logging.LogScopedHelper;
using ScopedLogCatalog = Mikew.NestCore.Helpers.Logging.ScopedLogCatalog;

namespace LetMeHatch.Application.Coordination;

public sealed partial class LmhCoordinator
{
    private readonly CoreContext _core;
    private readonly SmapiConfigProvider<ModConfig> _config;
    private readonly ILog _log;
    private readonly AssetInvalidationBatcher _invalidation;
    private readonly LetMeHatchApi _api;

    private IRoosterControlApi? _roosterControl;
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
            owner: this,
            core: _core,
            configProvider: _config,
            onSaved: () =>
            {
                InvalidateAllContent();
                InvalidateGameContentCache();
            }
        );

        InitializeStardewUi();
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
        PrintPipelineDiagnostics();
    }

    public void PrintPipelineDiagnostics()
    {
        var descriptors = GetContentHandlerDescriptors();

        LogScopedHelper.Log(
            _log,
            ScopedLogCatalog.ModLthteCategory.ContentPipeline,
            $"PipelineReady={_assetPipeline is not null} RegisteredHandlers={descriptors.Count} InvalidationTargets={descriptors.Select(static x => x.Handles).Distinct(StringComparer.OrdinalIgnoreCase).Count()}",
            LogLevel.Info);

        foreach (var descriptor in descriptors)
        {
            LogScopedHelper.Log(
                _log,
                ScopedLogCatalog.ModLthteCategory.ContentPipeline,
                $"[{descriptor.Kind}] {descriptor.Name} order={descriptor.Order} handles={descriptor.Handles} gates={descriptor.Gates}",
                LogLevel.Info);
        }
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

    public string BuildStatusText()
    {
        var lines = new List<string>
        {
            $"WorldReady={Context.IsWorldReady}",
            $"MainPlayer={Context.IsMainPlayer}",
            $"EarlyInitRequested={_earlyInitRequested}",
            $"WildEggsApiReady={_wildEggs is not null}",
            $"RoosterApiReady={_roosterControl is not null}",
            $"AssetManagerReady={_assetManager is not null}",
            $"AssetPipelineReady={_assetPipeline is not null}",
            $"ApplicationServicesReady={_app is not null}",
        };

        return string.Join(Environment.NewLine, lines);
    }

    public void PrintStatus()
    {
        foreach (var line in BuildStatusText().Split(Environment.NewLine))
            LogScopedHelper.Log(_log, ScopedLogCatalog.ModLthteCategory.Runtime, line, LogLevel.Info);
    }

    public void PrintRuntimeDiagnostics()
    {
        foreach (var line in BuildRuntimeDiagnosticsText().Split(Environment.NewLine))
            LogScopedHelper.Log(_log, ScopedLogCatalog.ModLthteCategory.Runtime, line, LogLevel.Info);
    }

    public DiagnosticReport BuildVerifyReport()
    {
        var report = new DiagnosticReport();
        report.Add("WORLD_READY", Context.IsWorldReady ? DiagnosticSeverity.Pass : DiagnosticSeverity.Warn, "runtime", $"WorldReady={Context.IsWorldReady}");
        report.Add("MAIN_PLAYER", Context.IsMainPlayer ? DiagnosticSeverity.Pass : DiagnosticSeverity.Warn, "runtime", $"MainPlayer={Context.IsMainPlayer}");
        report.Add("WE_API", _wildEggs is not null ? DiagnosticSeverity.Pass : DiagnosticSeverity.Fail, "integrations", $"WildEggsApiReady={_wildEggs is not null}", "Install or enable EggsInWild.");
        report.Add("RA_API", _roosterControl is not null ? DiagnosticSeverity.Pass : DiagnosticSeverity.Fail, "integrations", $"RoosterApiReady={_roosterControl is not null}", "Install or enable RoosterControl.");
        report.Add("ASSET_MANAGER", _assetManager is not null ? DiagnosticSeverity.Pass : DiagnosticSeverity.Warn, "runtime", $"AssetManagerReady={_assetManager is not null}", "Load a save and run reload if assets are not initialized.");
        report.Add("PIPELINE", _assetPipeline is not null ? DiagnosticSeverity.Pass : DiagnosticSeverity.Warn, "content-pipeline", $"AssetPipelineReady={_assetPipeline is not null}");
        report.Add("SERVICES", _app is not null ? DiagnosticSeverity.Pass : DiagnosticSeverity.Warn, "runtime", $"ApplicationServicesReady={_app is not null}");
        return report;
    }

    public void DebugWriteStateDump(string[] args)
    {
        if (!Context.IsWorldReady)
        {
            LogScopedHelper.Log(_log, ScopedLogCatalog.ModLthteCategory.Runtime, "Save not loaded; nothing to dump yet.", LogLevel.Info);
            return;
        }

        var defaultName = $"lmh_state_{Game1.Date.Year}_{Game1.Date.Season}_{Game1.Date.DayOfMonth}.json";
        var pathArg = args.FirstOrDefault(static x => !x.StartsWith("--", StringComparison.Ordinal));
        var path = ResolveOutputPath(pathArg ?? string.Empty, Path.Combine("state-dumps", defaultName));
        var runtimeLines = BuildRuntimeDiagnosticsText().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        var payload = new
        {
            GeneratedAtUtc = DateTime.UtcNow,
            SaveId = Game1.uniqueIDForThisGame,
            Date = new
            {
                Game1.Date.Year,
                Season = Game1.Date.Season.ToString(),
                Game1.Date.DayOfMonth
            },
            Status = BuildStatusText().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
            Runtime = runtimeLines,
            Pipeline = GetContentHandlerDescriptors().Select(static x => new
            {
                x.Kind,
                x.Name,
                x.Order,
                x.Handles,
                x.Gates
            }).ToArray()
        };

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
        LogScopedHelper.Log(_log, ScopedLogCatalog.ModLthteCategory.Runtime, $"Wrote state dump: {path}", LogLevel.Info);
    }

    private void InitializeApis()
    {
        _roosterControl = _core.Helper.ModRegistry.GetApi<IRoosterControlApi>("Mikew.roostersaddon.smapi");
        _wildEggs = _core.Helper.ModRegistry.GetApi<IWildEggsApi>("Mikew.eggsinwild");

        if (_roosterControl is null)
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

    _assetManager = new GameAssetManager(_wildEggs, _roosterControl);
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
#if LMH_DEV


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

    var json = BreedingDumpJson.SerializePlans(plans, indented: true);

    var defaultName = $"breeding_plans_{Game1.Date.Year}_{Game1.Date.Season}_{Game1.Date.DayOfMonth}.json";
    var path = ResolveOutputPath(pathArg, defaultName);

    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    File.WriteAllText(path, json);

    _log.Log($"[breeding dump plans] Wrote {plans.Count} plan(s) to: {path}", LogLevel.Info);
}
#endif
#if LMH_DEV


public void DebugBreedingDumpCoopIndex(string pathArg)
{
    if (!Context.IsWorldReady)
    {
        _log.Log("[breeding dump coop index] World not ready.", LogLevel.Warn);
        return;
    }

    var index = SmapiCoopBreedingIndexFactory.Build(Game1.getFarm());
    var json = BreedingDumpJson.SerializeCoopIndex(index, indented: true);
    var defaultName = $"breeding_coop_index_{Game1.Date.Year}_{Game1.Date.Season}_{Game1.Date.DayOfMonth}.json";
    var path = ResolveOutputPath(pathArg, defaultName);

    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    File.WriteAllText(path, json);

    _log.Log($"[breeding dump coop index] Wrote coop index (coops={index.GetAdultCountsByCoop().Count}) to: {path}", LogLevel.Info);
}
#endif
#if LMH_DEV


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
#endif
#if LMH_DEV

    public void DebugBreedingExplain(string[] args)
    {
        if (!Context.IsWorldReady || _assetManager is null)
        {
            _log.Log("[breeding explain] World not ready or asset manager not initialized.", LogLevel.Warn);
            return;
        }

        var json = args.Any(static x => string.Equals(x, "--json", StringComparison.OrdinalIgnoreCase));
        var positional = args.Where(static x => !string.Equals(x, "--json", StringComparison.OrdinalIgnoreCase)).ToArray();
        var femaleKeyFilter = positional.Length >= 1 ? positional[0].Trim() : string.Empty;

        var plans = _assetManager.BreedingDataLoader.BuildPlansForToday(
            Game1.farmAnimalData,
            ModEntry.Config,
            _assetManager.BreedingConfigsByPack,
            string.IsNullOrWhiteSpace(femaleKeyFilter) ? null : femaleKeyFilter);

        if (plans.Count == 0)
        {
            _log.Log(string.IsNullOrWhiteSpace(femaleKeyFilter)
                ? "[breeding explain] No plans available for today."
                : $"[breeding explain] No plan found for female '{femaleKeyFilter}'.",
                LogLevel.Info);
            return;
        }

        if (json)
        {
            _log.Log(BreedingDumpJson.SerializePlans(plans, indented: false), LogLevel.Info);
            return;
        }

        foreach (var plan in plans)
        {
            _log.Log($"[breeding explain] planId={plan.PlanId} date={plan.InGameDate:yyyy-MM-dd} seed={plan.Seed} scope={plan.ScopeAnimalKeys.Count} pairs={plan.Pairs.Count} mutations={plan.Mutations.Count}", LogLevel.Info);

            foreach (var pair in plan.Pairs)
            {
                var otherMales = pair.OtherMaleKeys.Count > 0
                    ? string.Join(", ", pair.OtherMaleKeys)
                    : "<none>";
                _log.Log(
                    $"[breeding explain] pair female={pair.FemaleAnimalKey} male={pair.MaleAnimalKey} others={otherMales} mode={pair.Mode} selectedEggType={pair.SelectedEggType ?? "<none>"} assign={pair.EggToAssignItemId ?? "<none>"} clear={pair.EggToClearItemId ?? "<none>"} fertilized={pair.FertilizedSucceeded} qty={pair.MaleQuantity}",
                    LogLevel.Info);
                _log.Log(
                    $"[breeding explain] chances female={pair.FemaleAnimalKey} base={pair.BaseFertilizedChance:0.###} adjusted={pair.AdjustedFertilizedChance:0.###} maleChance={pair.MaleChance:0.###} boost={pair.BoostByMale:0.###} dailyLuckBoost={pair.ApplyDailyLuckBoost}",
                    LogLevel.Info);
            }

            if (plan.Diagnostics.Count == 0)
            {
                _log.Log("[breeding explain] diagnostics=<none>", LogLevel.Info);
            }
            else
            {
                foreach (var diagnostic in plan.Diagnostics)
                    _log.Log($"[breeding explain] diagnostic: {diagnostic}", LogLevel.Info);
            }
        }
    }
#endif


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
#if LMH_DEV


public void DebugCloneTexturePreview(string cloneId)
{
    if (string.IsNullOrWhiteSpace(cloneId))
    {
        _log.Log("[clone texture preview] Usage: lmh debug clone-texture preview <cloneId>", LogLevel.Info);
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
#endif
#if LMH_DEV


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
#endif
#if LMH_DEV


public void DebugCloneTextureStats(string maxKeysArg)
{
    var maxKeys = TryParsePositiveInt(maxKeysArg, defaultValue: 10, maxValue: 100);
    var keys = Infrastructure.Content.Loaders.DynamicCloneObjectTextureLoader.GetCacheKeys(maxKeys);

    _log.Log($"[clone texture stats] cacheCount={Infrastructure.Content.Loaders.DynamicCloneObjectTextureLoader.CacheCount} dynamicEnabled={ModEntry.Config.UseDynamicCloneTextures} tintMode={ModEntry.Config.DynamicCloneTintMode}", LogLevel.Info);
    if (keys.Length > 0)
        _log.Log($"[clone texture stats] keys={string.Join(", ", keys)}", LogLevel.Info);
}
#endif
#if LMH_DEV


public void DebugCloneTextureClearCache()
{
    var before = Infrastructure.Content.Loaders.DynamicCloneObjectTextureLoader.CacheCount;
    Infrastructure.Content.Loaders.DynamicCloneObjectTextureLoader.ClearCache();
    _log.Log($"[clone texture clear cache] cleared={before}", LogLevel.Info);
}
#endif


private static int TryParsePositiveInt(string? arg, int defaultValue, int maxValue)
{
    if (!string.IsNullOrWhiteSpace(arg) && int.TryParse(arg, out var parsed) && parsed > 0)
        return Math.Min(parsed, maxValue);

    return defaultValue;
}

    private string BuildRuntimeDiagnosticsText()
    {
        var lines = new List<string>(BuildStatusText().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries));

        lines.Add($"Config.BreedingSystemEnabled={ModEntry.Config.BreedingSystemEnabled}");
        lines.Add($"Config.CloneVanillaEggs={ModEntry.Config.CloneVanillaEggs}");
        lines.Add($"Config.CloneMachineRules={ModEntry.Config.CloneMachineRules}");
        lines.Add($"Config.OnlyFertilizedEggs={ModEntry.Config.OnlyFertilizedEggs}");
        lines.Add($"Config.UseDynamicCloneTextures={ModEntry.Config.UseDynamicCloneTextures}");
        lines.Add($"Config.DynamicCloneTintMode={ModEntry.Config.DynamicCloneTintMode}");
        lines.Add($"DynamicTextureCacheCount={DynamicCloneObjectTextureLoader.CacheCount}");

        if (_assetManager is null)
        {
            lines.Add("LoadedContentPacks=0");
            lines.Add("LoadedFemaleEntries=0");
            lines.Add("LoadedMaleEntries=0");
            lines.Add("BreedingConfigPacks=0");
            lines.Add("BreedingConfigEntries=0");
            lines.Add("ClonedAnimals=0");
            lines.Add("ItemsToAdd=0");
            lines.Add("FertilizedEggsToAdd=0");
            lines.Add("OutputItemsToClone=0");
            lines.Add("SavedAnimalItemRelations=0");
            lines.Add("SavedAnimalKeys=0");
            lines.Add("MachineRules=0");
            return string.Join(Environment.NewLine, lines);
        }

        var loadedFemaleEntries = _assetManager.LoadedFemaleBreedingData.Sum(static kvp => kvp.Value.Count);
        var loadedMaleEntries = _assetManager.LoadedFemaleBreedingData.Sum(static kvp => kvp.Value.Sum(static inner => inner.Value.Males.Count));
        var breedingConfigEntries = _assetManager.BreedingConfigsByPack.Sum(static kvp => kvp.Value.Count);
        var itemsToAdd = _assetManager.ItemsToAdd.Sum(static byAnimal => byAnimal.Value.Sum(static byType => byType.Value.Count));
        var foundItemCounts = _assetManager.SavedItemsData.FoundItems
            .Select(kvp => $"{kvp.Key}={kvp.Value.Count}")
            .ToArray();

        lines.Add($"LoadedContentPacks={_assetManager.LoadedContentPacks.Count}");
        lines.Add($"LoadedFemaleEntries={loadedFemaleEntries}");
        lines.Add($"LoadedMaleEntries={loadedMaleEntries}");
        lines.Add($"BreedingConfigPacks={_assetManager.BreedingConfigsByPack.Count}");
        lines.Add($"BreedingConfigEntries={breedingConfigEntries}");
        lines.Add($"ClonedAnimals={_assetManager.ClonedAnimalsInfoList.Count}");
        lines.Add($"ItemsToAdd={itemsToAdd}");
        lines.Add($"FertilizedEggsToAdd={_assetManager.FertilizedEggsToAdd.Count}");
        lines.Add($"OutputItemsToClone={_assetManager.OutpuItemsToClone.Count}");
        lines.Add($"SavedAnimalItemRelations={_assetManager.SavedItemsData.AnimalItemRelation.Count}");
        lines.Add($"SavedAnimalKeys={_assetManager.SavedItemsData.AnimalKeys.Count}");
        lines.Add($"MachineRules={_assetManager.MachineEditor.MachineRules.Count}");
        lines.Add($"FoundItems={string.Join(", ", foundItemCounts)}");

        return string.Join(Environment.NewLine, lines);
    }

    private IReadOnlyList<ContentHandlerDescriptor> GetContentHandlerDescriptors()
    {
        var descriptors = new List<ContentHandlerDescriptor>();

        AddDescriptor(descriptors, "loader", 0, new DynamicCloneObjectTextureLoader(ModEntry.ModManifestInstance.UniqueID));
        AddDescriptor(descriptors, "editor", 10, new FarmAnimalsBreedingSystemEditor());
        AddDescriptor(descriptors, "editor", 20, new FarmAnimalsEggListByNameEditor());
        AddDescriptor(descriptors, "editor", 30, new FarmAnimalsClonesEditor());
        AddDescriptor(descriptors, "editor", 40, new FarmAnimalsBirthTextEditor());
        AddDescriptor(descriptors, "editor", 50, new FarmAnimalsClonePlaceholdersEditor());
        AddDescriptor(descriptors, "editor", 60, new FarmAnimalsReplaceOriginalItemsEditor());
        AddDescriptor(descriptors, "editor", 70, new ObjectsClonedEggsEditor());
        AddDescriptor(descriptors, "editor", 80, new ObjectsFertilizedEggsEditor());
        AddDescriptor(descriptors, "editor", 90, new MachinesRulesEditor());

        return descriptors;
    }

    private static void AddDescriptor(
        ICollection<ContentHandlerDescriptor> descriptors,
        string kind,
        int order,
        object handler)
    {
        if (handler is not IContentHandlerDebugInfo info)
            return;

        var name = handler.GetType().Name;
        descriptors.Add(new ContentHandlerDescriptor(kind, name, order, info.DebugHandles, info.DebugGates));
    }

    private sealed record ContentHandlerDescriptor(string Kind, string Name, int Order, string Handles, string Gates);

}
