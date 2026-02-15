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
    }
}
