using System.Collections.Generic;

namespace WildEggsHatching.Application.Spawning.Diagnostics;

public interface ITileSearchHistogram
{
    void Record(string mapName, string eggItemId, TileSearchStats stats, bool foundTile);

    /// <summary>
    /// Return the top rejection reasons for failures for a (map, eggItemId) key.
    /// </summary>
    IReadOnlyList<(string Reason, int Count)> GetTopRejections(string mapName, string eggItemId, int topN = 3);

    /// <summary>Formats a compact summary like "terrain=12, occupied=8, water=3".</summary>
    string FormatTopRejections(string mapName, string eggItemId, int topN = 3);

    /// <summary>
    /// Return the top rejection reasons aggregated across all failures (all keys).
    /// </summary>
    IReadOnlyList<(string Reason, int Count)> GetTopRejectionsAcrossFailures(int topN = 3);

    /// <summary>Formats a compact summary like "terrain=120, occupied=87, water=44".</summary>
    string FormatTopRejectionsAcrossFailures(int topN = 3);

    /// <summary>Clear all in-memory aggregation.</summary>
    void Reset();
}

public sealed class NullTileSearchHistogram : ITileSearchHistogram
{
    public static readonly NullTileSearchHistogram Instance = new();

    public void Record(string mapName, string eggItemId, TileSearchStats stats, bool foundTile) { }

    public IReadOnlyList<(string Reason, int Count)> GetTopRejections(string mapName, string eggItemId, int topN = 3)
        => System.Array.Empty<(string, int)>();

    public string FormatTopRejections(string mapName, string eggItemId, int topN = 3) => "";

    public IReadOnlyList<(string Reason, int Count)> GetTopRejectionsAcrossFailures(int topN = 3)
        => System.Array.Empty<(string, int)>();

    public string FormatTopRejectionsAcrossFailures(int topN = 3) => "";

    public void Reset() { }
}
