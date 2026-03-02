using System;
using System.Linq;
using StardewModdingAPI;
using RoosterControl.Application.Compat;
using Mikew.NestCore.Helpers.Logging;

using static Mikew.NestCore.Helpers.Logging.ScopedLogCatalog;

namespace RoosterControl.Application;

internal sealed partial class RoosterCoordinator
{
    private void CompatCommand(string command, string[] args)
    {
        var verb = args.FirstOrDefault()?.Trim() ?? "help";

        if (verb.Equals("help", StringComparison.OrdinalIgnoreCase) || verb.Equals("-h", StringComparison.OrdinalIgnoreCase))
        {
            LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands,
                "ra_compat usage (Phase 9A):\n" +
                "  ra_compat list\n" +
                "    - prints current compat catalogs and per-save selections.\n" +
                "\n" +
                "Note: cycling and runtime application are Phase 9B.",
                LogLevel.Info);
            return;
        }

        if (!Context.IsWorldReady)
        {
            LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands,
                "ra_compat is only available after a save is loaded.",
                LogLevel.Info);
            return;
        }

        if (verb.Equals("list", StringComparison.OrdinalIgnoreCase))
        {
            EnsureCompatInitialized();
            LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands, _compatSelection.BuildStatusReport(_saveState.Compat), LogLevel.Info);
            return;
        }

        LogScopedHelper.Log(_log, ModRoosterControlCategory.Commands,
            $"Unknown ra_compat verb '{verb}'. Use: ra_compat help",
            LogLevel.Info);
    }
}
