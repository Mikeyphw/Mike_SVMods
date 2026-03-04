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

    
    private void TryInstallDesertTombHarmonyPatches(string reason, bool force = false)
    {
        if (_config is null)
            return;

        var cfg = _config.Get();

#if WEGG_DEV
        var debugForce = cfg.DebugForceDesertTombHandler;
        var debugUnrestricted = cfg.DebugForceDesertTombUnrestricted;
#else
        var debugForce = false;
        var debugUnrestricted = false;
#endif

        var shouldInstall = force || cfg.DesertTombModuleEnabled || cfg.DesertTombForcePatch || debugForce || debugUnrestricted;

        Monitor.Log(
            $"[DesertTomb][Harmony] shouldInstall={shouldInstall} force={force} reason={reason} enabled={cfg.DesertTombModuleEnabled} forcePatch={cfg.DesertTombForcePatch} debugForce={debugForce} debugUnrestricted={debugUnrestricted}",
            LogLevel.Info);

        if (!shouldInstall)
            return;

        if (_harmony is null)
        {
            Monitor.Log("[DesertTomb][Harmony] Harmony bootstrap not initialized yet; cannot install patches.", LogLevel.Warn);
            return;
        }

        if (_desertTombHarmonyInstalled)
        {
            Monitor.Log("[DesertTomb][Harmony] Patches already installed (skipping).", LogLevel.Info);
            LogDesertTombHarmonyPatchStatus();
            return;
        }

        var runtime = EnsureDesertTombRuntime();
        if (runtime is null)
        {
            Monitor.Log("[DesertTomb][Harmony] DesertTombRuntime not available; cannot install patches.", LogLevel.Warn);
            return;
        }

        try
        {
            DesertTombMineShaftPatches.Install(_harmony, runtime);
            _desertTombHarmonyInstalled = true;
            Monitor.Log("[DesertTomb][Harmony] Installed DesertTomb Harmony patches successfully.", LogLevel.Info);
        }
        catch (Exception ex)
        {
            Monitor.Log($"[DesertTomb][Harmony] Failed to install DesertTomb Harmony patches: {ex}", LogLevel.Error);
            return;
        }

        LogDesertTombHarmonyPatchStatus();
    }

    private DesertTombRuntime? EnsureDesertTombRuntime()
    {
        if (_desertTombRuntime is not null)
            return _desertTombRuntime;

        if (_config is null)
            return null;

        var log = _contentLog ?? new SmapiLog(Monitor);

        // Ensure data loader exists for JSON-backed definitions.
        _contentDataLoader ??= new ConfiguredModDataLoader(new ModDataLoader(JsonFileReaderFactory.Create(Helper), log), _config!, log);

        var reader = JsonFileReaderFactory.Create(Helper);

        var monsterDefinitions = new TombMonsterDefinitions(reader, log);
        var dropTables = new TombDropTables(reader, log);
        var dropEngine = new TombDropTableEngine();

        var monsterFactory = new DesertTombMonsterFactory(monsterDefinitions, dropTables, dropEngine, log);

        var spawnExecutor = new DesertTombSpawnExecutor(monsterFactory, log);

        // Generator currently doesn't depend on SpawnConfiguration values (kept for future expansion).
        var generator = new DesertTombLevelGenerator(new SpawnConfiguration());

        _desertTombRuntime = new DesertTombRuntime(
            config: _config,
            getStep: () => _contentProgression?.GetCurrentStep() ?? ProgressionRules.MaxStep,
            log: log,
            isMainPlayer: () => Context.IsMainPlayer,
            generator: generator,
            spawnExecutor: spawnExecutor,
            diagnostics: null
        );

        return _desertTombRuntime;
    }

    private void LogDesertTombHarmonyPatchStatus()
    {
        if (_harmony is null)
            return;

        var harmonyId = _harmony.Id;

        LogPatchInfo(harmonyId, typeof(StardewValley.Locations.MineShaft), "loadLevel");
        LogPatchInfo(harmonyId, typeof(StardewValley.Locations.MineShaft), "chooseLevelType");
        LogPatchInfo(harmonyId, typeof(StardewValley.Locations.MineShaft), "populateLevel");
        LogPatchInfo(harmonyId, typeof(StardewValley.Locations.MineShaft), nameof(StardewValley.Locations.MineShaft.monsterDrop));

        LogPatchInfo(harmonyId, typeof(StardewValley.Objects.BreakableContainer), nameof(StardewValley.Objects.BreakableContainer.performToolAction));
        LogPatchInfo(harmonyId, typeof(StardewValley.Game1), nameof(StardewValley.Game1.createItemDebris));
    }

    private void LogPatchInfo(string harmonyId, Type type, string methodName)
    {
        var original = AccessTools.Method(type, methodName);
        if (original is null)
        {
            Monitor.Log($"[DesertTomb][Harmony] PatchStatus: {type.FullName}.{methodName} original=<null>", LogLevel.Info);
            return;
        }

        var info = Harmony.GetPatchInfo(original);
        if (info is null)
        {
            Monitor.Log($"[DesertTomb][Harmony] PatchStatus: {type.FullName}.{methodName} patched=<no>", LogLevel.Info);
            return;
        }

        var byUsPrefix = info.Prefixes?.Count(p => p.owner == harmonyId) ?? 0;
        var byUsPostfix = info.Postfixes?.Count(p => p.owner == harmonyId) ?? 0;
        var byUsTranspiler = info.Transpilers?.Count(p => p.owner == harmonyId) ?? 0;
        var byUsFinalizer = info.Finalizers?.Count(p => p.owner == harmonyId) ?? 0;

        Monitor.Log(
            $"[DesertTomb][Harmony] PatchStatus: {type.FullName}.{methodName} patched=<yes> ours(prefix={byUsPrefix}, postfix={byUsPostfix}, transpiler={byUsTranspiler}, finalizer={byUsFinalizer}) total(prefix={info.Prefixes?.Count ?? 0}, postfix={info.Postfixes?.Count ?? 0}, transpiler={info.Transpilers?.Count ?? 0}, finalizer={info.Finalizers?.Count ?? 0})",
            LogLevel.Info);
    }
}
