using System;
using StardewModdingAPI;
using LetMeHatchThisEgg.Application.Coordination;

namespace LetMeHatchThisEgg.Infrastructure.Commands;

public static class LmhConsoleCommands
{
    public static void Register(IModHelper helper, LmhCoordinator coordinator)
    {
        if (helper is null) throw new ArgumentNullException(nameof(helper));
        if (coordinator is null) throw new ArgumentNullException(nameof(coordinator));

        helper.ConsoleCommands.Add(
            "lmh_reloadcp",
            "Reloads the content packs for Let Me Hatch This Egg.

Usage: lmh_reloadcp.",
            (_, _) => coordinator.RequestEarlyInit()
        );

        helper.ConsoleCommands.Add(
            "lmh_checki18n",
            "Checks internationalization keys for the mod.

Usage: lmh_checki18n.",
            (_, _) => coordinator.CheckI18n()
        );


        helper.ConsoleCommands.Add(
            "lmh_content_handlers",
            "Prints the registered content loaders/editors and their priorities.

Usage: lmh_content_handlers.",
            (_, _) => coordinator.PrintContentHandlers()
        );

        helper.ConsoleCommands.Add(
            "lmh_invalidate_all",
            "Invalidates all game content caches touched by registered content handlers.

Usage: lmh_invalidate_all.",
            (_, _) => coordinator.InvalidateAllContent()
        );

    }
}
