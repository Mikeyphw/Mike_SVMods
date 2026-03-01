using System;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Monsters;
using EggsInWild.Application.DesertTomb.Runtime;
using EggsInWild.Infrastructure.DesertTomb.Monsters.Definitions;

namespace EggsInWild.Infrastructure.DesertTomb.Harmony;

/// <summary>
/// Harmony hooks for:
/// - forcing DesertTomb TMX map layout during MineShaft.loadLevel,
/// - skipping vanilla MineShaft.populateLevel and running DesertTomb population instead,
/// - spawning the ladder after the Pharaoh boss is defeated.
/// </summary>
internal static class DesertTombMineShaftPatches
{
    internal static DesertTombRuntime? Runtime { get; private set; }

    internal static void Install(HarmonyLib.Harmony harmony, DesertTombRuntime runtime)
    {
        Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));

        harmony.Patch(
            original: AccessTools.Method(typeof(MineShaft), "loadLevel"),
            postfix: new HarmonyMethod(typeof(DesertTombMineShaftPatches), nameof(AfterLoadLevel))
        );

        harmony.Patch(
            original: AccessTools.Method(typeof(MineShaft), "chooseLevelType"),
            postfix: new HarmonyMethod(typeof(DesertTombMineShaftPatches), nameof(AfterChooseLevelType))
        );

        harmony.Patch(
            original: AccessTools.Method(typeof(MineShaft), "populateLevel"),
            prefix: new HarmonyMethod(typeof(DesertTombMineShaftPatches), nameof(BeforePopulateLevel))
        );

        harmony.Patch(
            original: AccessTools.Method(typeof(MineShaft), nameof(MineShaft.monsterDrop)),
            postfix: new HarmonyMethod(typeof(DesertTombMineShaftPatches), nameof(AfterMonsterDrop))
        );
        DesertTombBreakableLootPatches.Install(harmony);
    }

    private static void AfterLoadLevel(MineShaft __instance, int level)
    {
        var rt = Runtime;
        if (rt is null)
            return;

        rt.HookAfterLoadLevel(__instance, level);
    }

    
    private static void AfterChooseLevelType(MineShaft __instance)
    {
        var rt = Runtime;
        if (rt is null)
            return;

        rt.HookAfterChooseLevelType(__instance);
    }

private static bool BeforePopulateLevel(MineShaft __instance)
    {
        var rt = Runtime;
        if (rt is null)
            return true;

        return rt.HookBeforePopulateLevel(__instance);
    }


    private static void AfterMonsterDrop(MineShaft __instance, Monster monster, int x, int y, Farmer who)
{
    try
    {
        var rt = Runtime;
        rt?.TraceHook("monsterDrop", __instance, $"fired monster={monster?.Name ?? "<null>"} at=({x},{y})", StardewModdingAPI.LogLevel.Info);

        if (__instance is null || monster is null)
            return;

        if (!__instance.modData.TryGetValue(DesertTombRuntime.LadderSuppressedKey, out var suppressed) || suppressed != "1")
            return;

        if (!IsPharaoh(monster))
            return;

        // Boss died: spawn ladder at the canonical "beneath ladder" tile.
        // This mirrors vanilla's infested-floor behavior but is keyed specifically to the Pharaoh.
        __instance.createLadderAt(__instance.tileBeneathLadder, "newArtifact");
        __instance.modData.Remove(DesertTombRuntime.LadderSuppressedKey);

        rt?.TraceHook("monsterDrop", __instance, "ladder spawned (pharaoh defeated)", StardewModdingAPI.LogLevel.Info);
    }
    catch (Exception ex)
    {
        Runtime?.TraceHook("monsterDrop", __instance, $"exception: {ex}", StardewModdingAPI.LogLevel.Error);
    }
}


    private static bool IsPharaoh(Monster monster)
        => monster.modData.TryGetValue(DesertTombMonsterModDataKeys.MonsterDefinitionId, out var id)
           && string.Equals(id, "TheTombPharaoh", StringComparison.OrdinalIgnoreCase);

    
}
