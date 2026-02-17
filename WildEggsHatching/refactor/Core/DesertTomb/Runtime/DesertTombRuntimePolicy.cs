using System;
using System.Text.RegularExpressions;
using WildEggsHatching.Core.Enums;
using static WildEggsHatching.DesertTomb.Core.Enums.DesertTombEnums;

namespace WildEggsHatching.DesertTomb.Core.Runtime;

/// <summary>
/// Pure helper policy for deciding whether DesertTomb should activate on a mine floor,
/// and for mapping the current mine map path to a known tomb layout.
/// </summary>
public static class DesertTombRuntimePolicy
{
    private static readonly Regex LevelRegex = new(
        pattern: @"(?i)(?:^|/|\\)(?:level)?(?<n>77377|39|28|19|14)(?:\D|$)",
        options: RegexOptions.Compiled | RegexOptions.CultureInvariant
    );

    public static bool IsEligibleByProgression(bool progressionModeEnabled, ProgressionStep step)
        => !progressionModeEnabled || step >= ProgressionStep.PartIV;

    public static bool IsTombCandidate(int mineLevel, string? mapPath)
        => mineLevel > 120 || TryParseMineLevel(mapPath, out _);

    public static bool TryParseMineLevel(string? mapPath, out MineLevel mineLevel)
    {
        mineLevel = MineLevel.Level77377;

        if (string.IsNullOrWhiteSpace(mapPath))
            return false;

        var normalized = mapPath.Replace('\\', '/');

        var m = LevelRegex.Match(normalized);
        if (!m.Success)
            return false;

        var n = m.Groups["n"].Value;
        mineLevel = n switch
        {
            "77377" => MineLevel.Level77377,
            "39" => MineLevel.Level39,
            "28" => MineLevel.Level28,
            "19" => MineLevel.Level19,
            "14" => MineLevel.Level14,
            _ => MineLevel.Level77377,
        };

        return true;
    }

    public static MineLevel GuessMineLevelOrDefault(string? mapPath)
        => TryParseMineLevel(mapPath, out var level) ? level : MineLevel.Level77377;

    public static int ParseLoadedMapNumber(string mapPath)
    {
        if (string.IsNullOrWhiteSpace(mapPath))
            return 0;

        var normalized = mapPath.Replace('\\', '/');

        var m = LevelRegex.Match(normalized);
        if (!m.Success)
            return 0;

        return int.TryParse(m.Groups["n"].Value, out var n) ? n : 0;
    }
}
