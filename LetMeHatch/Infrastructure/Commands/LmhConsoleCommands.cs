using System;
using StardewModdingAPI;
using LetMeHatch.Application.Coordination;

namespace LetMeHatch.Infrastructure.Commands;

public static class LmhConsoleCommands
{
    public static void Register(IModHelper helper, LmhCoordinator coordinator)
    {
        if (helper is null) throw new ArgumentNullException(nameof(helper));
        if (coordinator is null) throw new ArgumentNullException(nameof(coordinator));

        helper.ConsoleCommands.Add(
            "lmh_reloadcp",
            "Reloads the content packs for Let Me Hatch.\n\nUsage: lmh_reloadcp.",
            (_, _) => coordinator.RequestEarlyInit()
        );

        helper.ConsoleCommands.Add(
            "lmh_checki18n",
            "Checks internationalization keys for the mod.\n\nUsage: lmh_checki18n.",
            (_, _) => coordinator.CheckI18n()
        );


        helper.ConsoleCommands.Add(
            "lmh_content_handlers",
            "Prints the registered content loaders/editors and their priorities.\n\nUsage: lmh_content_handlers.",
            (_, _) => coordinator.PrintContentHandlers()
        );

        helper.ConsoleCommands.Add(
            "lmh_invalidate_all",
            "Invalidates all game content caches touched by registered content handlers.\n\nUsage: lmh_invalidate_all.",
            (_, _) => coordinator.InvalidateAllContent()
        );

        helper.ConsoleCommands.Add(
            "lmh_status",
            "Print LetMeHatch runtime/integration status.\n\nUsage: lmh_status.",
            (_, _) => coordinator.PrintStatus()
        );

        helper.ConsoleCommands.Add(
            "lmh_clone_texture_preview",
            "Preview dynamic clone texture details.\n\nUsage: lmh_clone_texture_preview <cloneId>.",
            (_, args) => coordinator.DebugCloneTexturePreview(args.Length >= 1 ? args[0] : string.Empty)
        );
        helper.ConsoleCommands.Add(
            "lmh_clone_texture_healthcheck",
            "Healthcheck dynamic clone texture generation by attempting to load N clone textures.\n\nUsage: lmh_clone_texture_healthcheck [count].",
            (_, args) => coordinator.DebugCloneTextureHealthcheck(args.Length >= 1 ? args[0] : string.Empty)
        );

        helper.ConsoleCommands.Add(
            "lmh_clone_texture_stats",
            "Print dynamic clone texture cache stats.\n\nUsage: lmh_clone_texture_stats [maxKeys].",
            (_, args) => coordinator.DebugCloneTextureStats(args.Length >= 1 ? args[0] : string.Empty)
        );

        helper.ConsoleCommands.Add(
            "lmh_clone_texture_clear_cache",
            "Clear the dynamic clone texture cache.\n\nUsage: lmh_clone_texture_clear_cache.",
            (_, _) => coordinator.DebugCloneTextureClearCache()
        );


    }
}
