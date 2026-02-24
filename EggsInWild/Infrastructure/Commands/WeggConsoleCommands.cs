using System;
using Mikew.NestCore.Helpers;
using Mikew.NestCore.Helpers.Logging;
using StardewModdingAPI;
using LogScopedHelper = Mikew.NestCore.Helpers.Logging.LogScopedHelper;
using static Mikew.NestCore.Helpers.Logging.ScopedLogCatalog;

namespace EggsInWild.Infrastructure.Commands;

public sealed class WeggConsoleCommands
{
    private readonly ILog _log;
    private readonly I18TranslationHelper _i18n;
    private readonly WeggCommandDispatcher _dispatcher;

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
                LogScopedHelper.Log(_log, ScopedLogCatalog.ModWeCategory.Commands, $"wegg args={string.Join(" ", args)}", LogLevel.Trace);
                var result = _dispatcher.Execute(args);
                LogScopedHelper.Log(_log, ScopedLogCatalog.ModWeCategory.Commands, result.Message, result.Success ? LogLevel.Info : LogLevel.Warn);
            });

        helper.ConsoleCommands.Add(
            name: "wegg_notes",
            documentation: "EggsInWild AnimalNotes commands. Type `wegg_notes dump`.",
            callback: (_, args) =>
            {
                LogScopedHelper.Log(_log, ScopedLogCatalog.ModWeCategory.Commands, $"wegg_notes args={string.Join(" ", args)}", LogLevel.Trace);

                var merged = new string[args.Length + 1];
                merged[0] = "notes";
                Array.Copy(args, 0, merged, 1, args.Length);

                var result = _dispatcher.Execute(merged);
                LogScopedHelper.Log(_log, ScopedLogCatalog.ModWeCategory.Commands, result.Message, result.Success ? LogLevel.Info : LogLevel.Warn);
            });

        helper.ConsoleCommands.Add(
            name: "wegg_schema",
            documentation: "EggsInWild schema commands. Type `wegg_schema wipe --yes`.",
            callback: (_, args) =>
            {
                LogScopedHelper.Log(_log, ScopedLogCatalog.ModWeCategory.Commands, $"wegg_schema args={string.Join(" ", args)}", LogLevel.Trace);

                var merged = new string[args.Length + 1];
                merged[0] = "schema";
                Array.Copy(args, 0, merged, 1, args.Length);

                var result = _dispatcher.Execute(merged);
                LogScopedHelper.Log(_log, ScopedLogCatalog.ModWeCategory.Commands, result.Message, result.Success ? LogLevel.Info : LogLevel.Warn);
            });
    }
}
