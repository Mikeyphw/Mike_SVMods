using System;
using HarmonyLib;
using StardewValley;

namespace WildEggsHatching.Infrastructure.Harmony;

internal static class AnimalNotesItemPatches
{
    public static void Install(Harmony harmony)
    {
        harmony.Patch(
            original: AccessTools.Method(typeof(Item), nameof(Item.canBeTrashed)),
            postfix: new HarmonyMethod(typeof(AnimalNotesItemPatches), nameof(AfterCanBeTrashed))
        );

        harmony.Patch(
            original: AccessTools.Method(typeof(Item), nameof(Item.canBeDropped)),
            postfix: new HarmonyMethod(typeof(AnimalNotesItemPatches), nameof(AfterCanBeDropped))
        );

        harmony.Patch(
            original: AccessTools.Method(typeof(Item), nameof(Item.canBeShipped)),
            postfix: new HarmonyMethod(typeof(AnimalNotesItemPatches), nameof(AfterCanBeShipped))
        );
    }

    private static void AfterCanBeTrashed(Item __instance, ref bool __result)
    {
        if (!__result)
            return;

        if (IsJunimoNote(__instance))
            __result = false;
    }

    private static void AfterCanBeDropped(Item __instance, ref bool __result)
    {
        if (!__result)
            return;

        if (IsJunimoNote(__instance))
            __result = false;
    }

    private static void AfterCanBeShipped(Item __instance, ref bool __result)
    {
        if (!__result)
            return;

        if (IsJunimoNote(__instance))
            __result = false;
    }

    private static bool IsJunimoNote(Item item)
        => item is not null
           && !string.IsNullOrWhiteSpace(item.ItemId)
           && item.ItemId.StartsWith($"{Constants.ModId}_junimoNote_", StringComparison.OrdinalIgnoreCase);
}
