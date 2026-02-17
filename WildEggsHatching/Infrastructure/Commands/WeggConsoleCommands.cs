using System;
using StardewModdingAPI;

namespace WildEggsHatching.Infrastructure.Commands;

public sealed class WeggConsoleCommands
{
    private readonly IMonitor _monitor;
    private readonly WeggCommandDispatcher _dispatcher;

    public WeggConsoleCommands(IMonitor monitor, WeggCommandDispatcher dispatcher)
    {
        _monitor = monitor;
        _dispatcher = dispatcher;
    }

    public void Register(IModHelper helper)
    {
        helper.ConsoleCommands.Add(
            name: "wegg",
            documentation: "WildEggsHatching subcommands. Type `wegg help`.",
            callback: (_, args) =>
            {
                _monitor.Log($"wegg args={string.Join(" ", args)}", LogLevel.Trace);
                var result = _dispatcher.Execute(args);
                _monitor.Log(result.Message, result.Success ? LogLevel.Info : LogLevel.Warn);
            });
        helper.ConsoleCommands.Add(
            name: "wegg_notes",
            documentation: "WildEggsHatching AnimalNotes commands. Type `wegg_notes dump`.",
            callback: (_, args) =>
            {
                _monitor.Log($"wegg_notes args={string.Join(" ", args)}", LogLevel.Trace);

                var merged = new string[args.Length + 1];
                merged[0] = "notes";
                Array.Copy(args, 0, merged, 1, args.Length);

                var result = _dispatcher.Execute(merged);
                _monitor.Log(result.Message, result.Success ? LogLevel.Info : LogLevel.Warn);
            });
        helper.ConsoleCommands.Add(
            name: "wegg_schema",
            documentation: "WildEggsHatching schema commands. Type `wegg_schema wipe --yes`.",
            callback: (_, args) =>
            {
                _monitor.Log($"wegg_schema args={string.Join(" ", args)}", LogLevel.Trace);

                var merged = new string[args.Length + 1];
                merged[0] = "schema";
                Array.Copy(args, 0, merged, 1, args.Length);

                var result = _dispatcher.Execute(merged);
                _monitor.Log(result.Message, result.Success ? LogLevel.Info : LogLevel.Warn);
            });




    }
}
