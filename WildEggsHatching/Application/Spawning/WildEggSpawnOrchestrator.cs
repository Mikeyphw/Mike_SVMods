using WildEggsHatching.Infrastructure.Game;
using Mikew.BirdsOfAFeather.Helpers.Logging;
using WildEggsHatching.Infrastructure.ModData;

namespace WildEggsHatching.Application.Spawning;

public sealed class WildEggSpawnOrchestrator
{
    private readonly IModDataLoader _data;
    private readonly IGameStateReader _game;
    private readonly SpawnPlanner _planner;
    private readonly EggSpawnService _service;
    private readonly ILog _log;

    public WildEggSpawnOrchestrator(
        IModDataLoader data,
        IGameStateReader game,
        SpawnPlanner planner,
        EggSpawnService service,
        ILog log)
    {
        _data = data;
        _game = game;
        _planner = planner;
        _service = service;
        _log = log;
    }

    public void OnDayStarted()
    {
        var snapshot = _data.Load();

        var saveId = _game.GetSaveUniqueId();
        var dayNumber = _game.GetDayNumber();

        var plan = _planner.BuildPlan(snapshot, saveId, dayNumber);

        _service.SpawnForToday(plan, snapshot.SpawnParameters, saveId, dayNumber);

        _log.Log($"Spawn plan executed: packs={plan.PackEggBudgets.Count}, jobs={plan.Jobs.Count}", StardewModdingAPI.LogLevel.Trace);
    }

    public void OnDayEnding()
    {
        _service.DumpEndOfDayTileSearchFailureSummary();
    }
}
