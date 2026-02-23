using System;
using HarmonyLib;
using StardewValley;
using StardewValley.Objects;
using WildEggsHatching.Application.DesertTomb.Runtime;

namespace WildEggsHatching.Infrastructure.DesertTomb.Harmony;

/// <summary>
/// Applies TombDifficulty loot multipliers to breakable containers (barrels/urns) on active DesertTomb floors.
/// This is implemented by:
/// - setting a thread-local context during <see cref="BreakableContainer.performToolAction"/>,
/// - scaling the stack (or skipping the drop) inside <see cref="Game1.createItemDebris"/> calls made during that context.
/// </summary>
internal static class DesertTombBreakableLootPatches
{
    [ThreadStatic] private static double _dropRateMultiplier;
    [ThreadStatic] private static bool _inBreakableContext;
    [ThreadStatic] private static bool _reentryGuard;

    internal static void Install(HarmonyLib.Harmony harmony)
    {
        if (harmony is null)
            throw new ArgumentNullException(nameof(harmony));

        var toolAction = AccessTools.Method(typeof(BreakableContainer), nameof(BreakableContainer.performToolAction));
        if (toolAction is not null)
        {
            harmony.Patch(
                original: toolAction,
                prefix: new HarmonyMethod(typeof(DesertTombBreakableLootPatches), nameof(BeforeBreakableToolAction)),
                postfix: new HarmonyMethod(typeof(DesertTombBreakableLootPatches), nameof(AfterBreakableToolAction))
            );
        }

        // Patch all createItemDebris overloads that include (Item item, GameLocation location).
        foreach (var m in AccessTools.GetDeclaredMethods(typeof(Game1)))
        {
            if (!string.Equals(m.Name, nameof(Game1.createItemDebris), StringComparison.Ordinal))
                continue;

            var ps = m.GetParameters();
            if (ps.Length < 2)
                continue;

            var hasItem = ps.Length > 0 && ps[0].ParameterType == typeof(Item);
            var hasLocation = ps.Any(p => p.ParameterType == typeof(GameLocation));
            if (!hasItem || !hasLocation)
                continue;

            harmony.Patch(
                original: m,
                prefix: new HarmonyMethod(typeof(DesertTombBreakableLootPatches), nameof(BeforeCreateItemDebris))
            );
        }
}

    private static void BeforeBreakableToolAction(BreakableContainer __instance, GameLocation location)
    {
        _inBreakableContext = false;
        _dropRateMultiplier = 1.0;

        if (location is not StardewValley.Locations.MineShaft mineShaft)
            return;

        if (!mineShaft.modData.TryGetValue(DesertTombRuntime.BreakableDropRateMultiplierKey, out var raw))
            return;

        if (!double.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var mult))
            return;

        // Clamp to avoid pathological values.
        _dropRateMultiplier = Math.Clamp(mult, 0.0, 10.0);
        _inBreakableContext = true;
    }

    private static void AfterBreakableToolAction()
    {
        _inBreakableContext = false;
        _dropRateMultiplier = 1.0;
    }

    private static bool BeforeCreateItemDebris(Item item, GameLocation location)
    {
        if (!_inBreakableContext || _reentryGuard)
            return true;

        if (_dropRateMultiplier == 1.0)
            return true;

        if (item is null)
            return true;

        // Scale the final stack probabilistically so multipliers like 1.15 still have effect for stack=1.
        var baseStack = Math.Max(1, item.Stack);
        var target = baseStack * _dropRateMultiplier;

        var floored = (int)Math.Floor(target);
        var frac = target - floored;

        var stack = floored;
        if (frac > 0 && Game1.random.NextDouble() < frac)
            stack += 1;

        // If multiplier < 1, this can go to 0; treat that as "no drop".
        if (stack <= 0)
            return false;

        item.Stack = stack;
        return true;
    }
}
