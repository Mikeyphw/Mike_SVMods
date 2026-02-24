using System;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Monsters;
using EggsInWild.DesertTomb.Core.Spawning;
using EggsInWild.Infrastructure.Config;
using Mikew.NestCore.Helpers.Logging;
using Mikew.NestCore.Helpers.Config;
using LogScopedHelper = Mikew.NestCore.Helpers.Logging.LogScopedHelper;
using static Mikew.NestCore.Helpers.Logging.ScopedLogCatalog;

namespace EggsInWild.Infrastructure.DesertTomb.Diagnostics;

/// <summary>
/// Optional diagnostics surface for DesertTomb (Phase 9):
/// - Pharaoh HUD tracker arrow,
/// - extra activation/spawn logging.
/// </summary>
public sealed class DesertTombDiagnostics
{
    private readonly SmapiConfigProvider<ModConfig> _config;
    private readonly ILog _log;
    private readonly PharaohTrackerOverlay _overlay;

    public DesertTombDiagnostics(IModHelper helper, SmapiConfigProvider<ModConfig> config, ILog log)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _overlay = new PharaohTrackerOverlay(helper, config);
    }

    public void RenderHud(SpriteBatch spriteBatch) => _overlay.Render(spriteBatch);

    public void OnActivated(int layoutLevel, string mapPath, MineShaft mineShaft, TombSpawnPlan plan, int spawned)
    {
        if (!_config.Get().DesertTombDebugLogging)
            return;

        var sb = new StringBuilder();
        sb.AppendLine("[DesertTomb] Activation diagnostics:");
        sb.AppendLine($"  MineLevel={mineShaft.mineLevel} Layout={layoutLevel} MapPath='{mapPath}'");
        var plannedTotal = plan.Areas.Sum(a => a.Spawns.Sum(s => s.Count));
        sb.AppendLine($"  PlannedTotal={plannedTotal} Spawned={spawned}");

        var byType = plan.Areas
            .SelectMany(a => a.Spawns)
            .GroupBy(s => s.Type)
            .OrderByDescending(g => g.Sum(x => x.Count))
            .Select(g => (Type: g.Key, Count: g.Sum(x => x.Count)));

        foreach (var row in byType)
            sb.AppendLine($"  - {row.Type}: {row.Count}");

        LogScopedHelper.Log(_log, ScopedLogCatalog.ModWeCategory.DesertTomb, sb.ToString().TrimEnd(), LogLevel.Trace);

        LogMonsters(mineShaft);
    }

    public void LogMonsters(GameLocation location)
    {
        if (!_config.Get().DesertTombDebugLogging)
            return;

        var monsters = location.characters
            .OfType<Monster>()
            .GroupBy(m => m.Name)
            .OrderByDescending(g => g.Count())
            .Select(g => (Name: g.Key, Count: g.Count()))
            .ToList();

        if (monsters.Count == 0)
        {
            LogScopedHelper.Log(_log, ScopedLogCatalog.ModWeCategory.DesertTomb, "[DesertTomb] Monsters: (none)", LogLevel.Trace);
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("[DesertTomb] Monsters:");
        foreach (var m in monsters)
            sb.AppendLine($"  - {m.Name}: {m.Count}");

        LogScopedHelper.Log(_log, ScopedLogCatalog.ModWeCategory.DesertTomb, sb.ToString().TrimEnd(), LogLevel.Trace);
    }
}
