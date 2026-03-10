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
            name: "we",
            documentation: "EggsInWild commands. Type `we help`.",
            callback: (_, args) =>
            {
                LogScopedHelper.Log(_log, ScopedLogCatalog.ModWeCategory.Commands, $"we args={string.Join(" ", args)}", LogLevel.Trace);
                var result = _dispatcher.Execute(args);
                LogScopedHelper.Log(_log, ScopedLogCatalog.ModWeCategory.Commands, result.Message, result.Success ? LogLevel.Info : LogLevel.Warn);
            });
    }
}
