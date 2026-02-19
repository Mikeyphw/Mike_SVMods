using System;
using Mikew.BirdsOfAFeather.Helpers.Logging;
using StardewModdingAPI;

namespace WildEggsHatching.Infrastructure.Commands;

public sealed class WeggConsoleCommands
{
    private readonly ILog _log;
    private readonly WeggCommandDispatcher _dispatcher;

    public WeggConsoleCommands(ILog log, WeggCommandDispatcher dispatcher)
    {
        _log = log;
        _dispatcher = dispatcher;
    }

    public void Register(IModHelper helper)
    {
        helper.ConsoleCommands.Add(
            name: "wegg",
            documentation: "WildEggsHatching subcommands. Type `wegg help`.",
            callback: (_, args) =>
            {
                _log.Log($"wegg args={string.Join(" ", args)}", LogLevel.Trace);
                var result = _dispatcher.Execute(args);
                _log.Log(result.Message, result.Success ? LogLevel.Info : LogLevel.Warn);
            });

        helper.ConsoleCommands.Add(
            name: "wegg_notes",
            documentation: "WildEggsHatching AnimalNotes commands. Type `wegg_notes dump`.",
            callback: (_, args) =>
            {
                _log.Log($"wegg_notes args={string.Join(" ", args)}", LogLevel.Trace);

                var merged = new string[args.Length + 1];
                merged[0] = "notes";
                Array.Copy(args, 0, merged, 1, args.Length);

                var result = _dispatcher.Execute(merged);
                _log.Log(result.Message, result.Success ? LogLevel.Info : LogLevel.Warn);
            });

        helper.ConsoleCommands.Add(
            name: "wegg_schema",
            documentation: "WildEggsHatching schema commands. Type `wegg_schema wipe --yes`.",
            callback: (_, args) =>
            {
                _log.Log($"wegg_schema args={string.Join(" ", args)}", LogLevel.Trace);

                var merged = new string[args.Length + 1];
                merged[0] = "schema";
                Array.Copy(args, 0, merged, 1, args.Length);

                var result = _dispatcher.Execute(merged);
                _log.Log(result.Message, result.Success ? LogLevel.Info : LogLevel.Warn);
            });
    }
}
