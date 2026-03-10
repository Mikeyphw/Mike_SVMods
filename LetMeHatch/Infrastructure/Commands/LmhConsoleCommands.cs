using System;
using System.Linq;
using LetMeHatch.Application.Coordination;
using Mikew.NestCore.Diagnostics;
using StardewModdingAPI;
using static LetMeHatch.ModEntry;

namespace LetMeHatch.Infrastructure.Commands;

public static class LmhConsoleCommands
{
    public static void Register(IModHelper helper, LmhCoordinator coordinator)
    {
        if (helper is null) throw new ArgumentNullException(nameof(helper));
        if (coordinator is null) throw new ArgumentNullException(nameof(coordinator));

        helper.ConsoleCommands.Add(
            "lmh",
            "LetMeHatch commands. Use: lmh help",
            (_, args) => Execute(coordinator, args));
    }

    private static void Execute(LmhCoordinator coordinator, string[] args)
    {
        if (args.Length == 0 || IsHelp(args[0]))
        {
            MonitorInstance.Log(BuildHelpText(), LogLevel.Info);
            return;
        }

        switch (args[0].ToLowerInvariant())
        {
            case "help":
                MonitorInstance.Log(BuildHelpText(), LogLevel.Info);
                return;
            case "status":
                coordinator.PrintStatus();
                return;
            case "verify":
                HandleVerify(coordinator, args.Skip(1).ToArray());
                return;
            case "reload":
                coordinator.RequestEarlyInit();
                return;
            case "save":
                HandleSave(args.Skip(1).ToArray());
                return;
            case "debug":
#if LMH_DEV
                HandleDebug(coordinator, args.Skip(1).ToArray());
#else
                MonitorInstance.Log("Dev-only commands are unavailable in this build.", LogLevel.Warn);
#endif
                return;
            default:
                MonitorInstance.Log($"Unknown subcommand '{args[0]}'.\n\n{BuildHelpText()}", LogLevel.Warn);
                return;
        }
    }

    private static void HandleVerify(LmhCoordinator coordinator, string[] args)
    {
        var json = args.Any(x => string.Equals(x, "--json", StringComparison.OrdinalIgnoreCase));
        var report = coordinator.BuildVerifyReport();
        var text = json
            ? DiagnosticReportFormatter.FormatJson(report, "lmh verify")
            : DiagnosticReportFormatter.FormatText(report, "lmh verify");
        MonitorInstance.Log(text, report.FailCount > 0 ? LogLevel.Warn : LogLevel.Info);
    }

    private static void HandleSave(string[] args)
    {
        if (args.Length == 0 || IsHelp(args[0]))
        {
            MonitorInstance.Log("Usage: lmh save clear --yes", LogLevel.Info);
            return;
        }

        if (!string.Equals(args[0], "clear", StringComparison.OrdinalIgnoreCase))
        {
            MonitorInstance.Log($"Unknown save subcommand '{args[0]}'.\n\n{BuildHelpText()}", LogLevel.Warn);
            return;
        }

        MonitorInstance.Log("LetMeHatch does not currently persist per-save data, so there is nothing to clear.", LogLevel.Info);
    }

#if LMH_DEV
    private static void HandleDebug(LmhCoordinator coordinator, string[] args)
    {
        if (args.Length == 0 || IsHelp(args[0]))
        {
            MonitorInstance.Log(BuildDebugHelpText(), LogLevel.Info);
            return;
        }

        switch (args[0].ToLowerInvariant())
        {
            case "i18n-check":
                coordinator.CheckI18n();
                return;
            case "content-handlers":
                coordinator.PrintContentHandlers();
                return;
            case "pipeline":
                coordinator.PrintPipelineDiagnostics();
                return;
            case "runtime":
                coordinator.PrintRuntimeDiagnostics();
                return;
            case "invalidate-all":
                coordinator.InvalidateAllContent();
                return;
            case "state-dump":
                coordinator.DebugWriteStateDump(args.Skip(1).ToArray());
                return;
            case "clone-texture":
                HandleCloneTextureDebug(coordinator, args.Skip(1).ToArray());
                return;
            case "breeding":
                HandleBreedingDebug(coordinator, args.Skip(1).ToArray());
                return;
            default:
                MonitorInstance.Log($"Unknown debug subcommand '{args[0]}'.\n\n{BuildDebugHelpText()}", LogLevel.Warn);
                return;
        }
    }

    private static void HandleCloneTextureDebug(LmhCoordinator coordinator, string[] args)
    {
        if (args.Length == 0 || IsHelp(args[0]))
        {
            MonitorInstance.Log(
                "lmh debug clone-texture preview <cloneId>\n" +
                "lmh debug clone-texture healthcheck [count]\n" +
                "lmh debug clone-texture stats [maxKeys]\n" +
                "lmh debug clone-texture clear-cache",
                LogLevel.Info);
            return;
        }

        switch (args[0].ToLowerInvariant())
        {
            case "preview":
                coordinator.DebugCloneTexturePreview(args.Length >= 2 ? args[1] : string.Empty);
                return;
            case "healthcheck":
                coordinator.DebugCloneTextureHealthcheck(args.Length >= 2 ? args[1] : string.Empty);
                return;
            case "stats":
                coordinator.DebugCloneTextureStats(args.Length >= 2 ? args[1] : string.Empty);
                return;
            case "clear-cache":
                coordinator.DebugCloneTextureClearCache();
                return;
            default:
                MonitorInstance.Log($"Unknown clone-texture debug subcommand '{args[0]}'.", LogLevel.Warn);
                return;
        }
    }

    private static void HandleBreedingDebug(LmhCoordinator coordinator, string[] args)
    {
        if (args.Length == 0 || IsHelp(args[0]))
        {
            MonitorInstance.Log(
                "lmh debug breeding dump-plans [path] [femaleKey]\n" +
                "lmh debug breeding dump-coop-index [path]\n" +
                "lmh debug breeding validate-plans [femaleKey]\n" +
                "lmh debug breeding explain [femaleKey] [--json]",
                LogLevel.Info);
            return;
        }

        switch (args[0].ToLowerInvariant())
        {
            case "dump-plans":
                coordinator.DebugBreedingDumpPlans(args.Length >= 2 ? args[1] : string.Empty, args.Length >= 3 ? args[2] : string.Empty);
                return;
            case "dump-coop-index":
                coordinator.DebugBreedingDumpCoopIndex(args.Length >= 2 ? args[1] : string.Empty);
                return;
            case "validate-plans":
                coordinator.DebugBreedingValidatePlans(args.Length >= 2 ? args[1] : string.Empty);
                return;
            case "explain":
                coordinator.DebugBreedingExplain(args.Skip(1).ToArray());
                return;
            default:
                MonitorInstance.Log($"Unknown breeding debug subcommand '{args[0]}'.", LogLevel.Warn);
                return;
        }
    }
#endif

    private static string BuildHelpText()
        => string.Join(
            Environment.NewLine,
            "lmh help",
            "lmh status",
            "lmh verify [--json]",
            "lmh reload",
            "lmh save clear --yes",
#if LMH_DEV
            "lmh debug ...",
#endif
            "",
            "Status prints a concise runtime snapshot.",
            "Verify runs read-only LetMeHatch diagnostics.");

#if LMH_DEV
    private static string BuildDebugHelpText()
        => string.Join(
            Environment.NewLine,
            "lmh debug i18n-check",
            "lmh debug content-handlers",
            "lmh debug pipeline",
            "lmh debug runtime",
            "lmh debug invalidate-all",
            "lmh debug state-dump [path]",
            "lmh debug clone-texture ...",
            "lmh debug breeding ...");
#endif

    private static bool IsHelp(string arg)
        => string.Equals(arg, "help", StringComparison.OrdinalIgnoreCase)
           || string.Equals(arg, "-h", StringComparison.OrdinalIgnoreCase)
           || string.Equals(arg, "--help", StringComparison.OrdinalIgnoreCase);
}
