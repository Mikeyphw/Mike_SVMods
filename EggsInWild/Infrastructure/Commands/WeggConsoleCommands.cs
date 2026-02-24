using System;
using Mikew.NestCore.Helpers;
using Mikew.NestCore.Helpers.Logging;
using StardewModdingAPI;
using SmapiLog = Mikew.NestCore.Helpers.Logging.SmapiLog;

namespace EggsInWild.Infrastructure.Commands;

public sealed class WeggConsoleCommands
{
    private readonly ILog _log;
    private readonly I18TranslationHelper _i18n;
    private readonly WeggCommandDispatcher _dispatcher;

    /// <summary>Helper to emit scoped log messages, falling back to basic log if not a SmapiLog instance.</summary>
    private void LogScoped(string category, string message, LogLevel level)
    {
        if (_log is SmapiLog slog)
            slog.LogScoped(ScopedLogCatalog.ModWe, category, message, level);
        else
            _log.Log(message, level);
    }

    public WeggConsoleCommands(ILog log, I18TranslationHelper i18n, WeggCommandDispatcher dispatcher)
    {
        _log = log;
        _i18n = i18n;
        _dispatcher = dispatcher;
    }

    public void Register(IModHelper helper)
    {
        helper.ConsoleCommands.Add(
            name: "wegg",
            documentation: _i18n.GetOrDefault("command.wegg.documentation", "EggsInWild subcommands. Type `wegg help`."),
            callback: (_, args) =>
            {
                LogScoped("commands", $"wegg args={string.Join(" ", args)}", LogLevel.Trace);
                var result = _dispatcher.Execute(args);
                LogScoped("commands", result.Message, result.Success ? LogLevel.Info : LogLevel.Warn);
            });

        helper.ConsoleCommands.Add(
            name: "wegg_notes",
            documentation: "EggsInWild AnimalNotes commands. Type `wegg_notes dump`.",
            callback: (_, args) =>
            {
                LogScoped("commands", $"wegg_notes args={string.Join(" ", args)}", LogLevel.Trace);

                var merged = new string[args.Length + 1];
                merged[0] = "notes";
                Array.Copy(args, 0, merged, 1, args.Length);

                var result = _dispatcher.Execute(merged);
                LogScoped("commands", result.Message, result.Success ? LogLevel.Info : LogLevel.Warn);
            });

        helper.ConsoleCommands.Add(
            name: "wegg_schema",
            documentation: "EggsInWild schema commands. Type `wegg_schema wipe --yes`.",
            callback: (_, args) =>
            {
                LogScoped("commands", $"wegg_schema args={string.Join(" ", args)}", LogLevel.Trace);

                var merged = new string[args.Length + 1];
                merged[0] = "schema";
                Array.Copy(args, 0, merged, 1, args.Length);

                var result = _dispatcher.Execute(merged);
                LogScoped("commands", result.Message, result.Success ? LogLevel.Info : LogLevel.Warn);
            });
    }
}
