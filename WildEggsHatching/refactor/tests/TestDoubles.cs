using System;
using System.Collections.Generic;
using WildEggsHatching.Application.Spawning;
using WildEggsHatching.Infrastructure.Game;
using WildEggsHatching.Infrastructure.Logging;
using WildEggsHatching.Infrastructure.Mods;

namespace WildEggsHatching.Refactor.Tests;

internal sealed class FakeLog : ILog
{
    public List<string> Messages { get; } = new();
    public void Log(string message, StardewModdingAPI.LogLevel level = StardewModdingAPI.LogLevel.Debug)
        => Messages.Add($"{level}:{message}");
}

internal sealed class FakeMods : IModAvailability
{
    private readonly HashSet<string> _loaded;

    public FakeMods(IEnumerable<string> loaded)
        => _loaded = new HashSet<string>(loaded, StringComparer.OrdinalIgnoreCase);

    public bool IsLoaded(string uniqueId) => _loaded.Contains(uniqueId);
}

internal sealed class FakeWorld : IWorldLocations
{
    private readonly HashSet<string> _names;

    public FakeWorld(IEnumerable<string> names)
        => _names = new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<StardewValley.GameLocation> GetAll() => Array.Empty<StardewValley.GameLocation>();

    public StardewValley.GameLocation? TryGetByName(string name) => null;

    public bool Exists(string name) => _names.Contains(name);
}

internal sealed class AlwaysTrueConditions : IAreaConditionsEvaluator
{
    public bool AreMet(WildEggsHatching.Infrastructure.ModData.Models.ExtraConditionsJson conditions) => true;
}

internal sealed class AlwaysTrueGsq : IGameStateQueryEvaluator
{
    public bool Check(string expression) => true;
}
