using System;
using System.Collections.Generic;
using RoosterControl.Application.Catalogs;
using RoosterControl.Application.Planning;
using RoosterControl.Constants;

namespace RoosterControl.Application.Produces;

/// <summary>Resolves produce/egg choice keys into concrete item IDs, supporting catalog-driven random groups.</summary>
public interface IProduceResolver
{
    /// <summary>Resolve an item choice key (or random group key) into an item ID.</summary>
    string Resolve(string choiceKey);
}

/// <summary>
/// Extension of <see cref="IProduceResolver"/> for deterministic resolution.
/// </summary>
public interface IDeterministicProduceResolver : IProduceResolver
{
    /// <summary>
    /// Resolve a choice key deterministically using a caller-provided base seed.
    /// </summary>
    string Resolve(string choiceKey, int baseSeed);
}

/// <summary>
/// Random group -> choice resolver. Random groups are provided by <see cref="ProduceCatalog.RandomGroups"/>.
/// </summary>
public sealed class RandomGroupProduceResolver : IDeterministicProduceResolver
{
    internal const int DefaultResolveCacheCapacity = 2048;
    internal const int DefaultTraceCacheCapacity = 512;

    private readonly IDictionary<string, List<string>> _groups;
    private readonly Random? _random;

    private readonly LruCache<CacheKey, string> _resolveCache;
    private readonly LruCache<CacheKey, ProduceResolutionTrace> _traceCache;

    private long _resolveCacheHits;
    private long _resolveCacheMisses;
    private long _traceCacheHits;
    private long _traceCacheMisses;

    public RandomGroupProduceResolver(ProduceCatalog? produceCatalog)
        : this(produceCatalog, DefaultResolveCacheCapacity, DefaultTraceCacheCapacity, random: null)
    {
    }

    public RandomGroupProduceResolver(ProduceCatalog? produceCatalog, Random random)
        : this(
            produceCatalog,
            DefaultResolveCacheCapacity,
            DefaultTraceCacheCapacity,
            random ?? throw new ArgumentNullException(nameof(random))
        )
    {
    }

    internal RandomGroupProduceResolver(ProduceCatalog? produceCatalog, int resolveCacheCapacity, int traceCacheCapacity)
        : this(produceCatalog, resolveCacheCapacity, traceCacheCapacity, random: null)
    {
    }

    internal RandomGroupProduceResolver(
        ProduceCatalog? produceCatalog,
        int resolveCacheCapacity,
        int traceCacheCapacity,
        Random? random
    )
    {
        _groups =
            (produceCatalog?.RandomGroups as IDictionary<string, List<string>>)
            ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        _random = random;

        var comparer = new CacheKeyComparer();
        _resolveCache = new LruCache<CacheKey, string>(resolveCacheCapacity, comparer);
        _traceCache = new LruCache<CacheKey, ProduceResolutionTrace>(traceCacheCapacity, comparer);
    }
/// <summary>Clear deterministic resolution caches (safe to call any time).</summary>
    internal void ClearCaches()
    {
        _resolveCache.Clear();
        _traceCache.Clear();

        _resolveCacheHits = 0;
        _resolveCacheMisses = 0;
        _traceCacheHits = 0;
        _traceCacheMisses = 0;
    }

    internal ProduceResolverCacheStats GetCacheStats()
        => new(
            ResolveCacheEntries: _resolveCache.Count,
            ResolveCacheCapacity: _resolveCache.Capacity,
            ResolveCacheHits: _resolveCacheHits,
            ResolveCacheMisses: _resolveCacheMisses,
            ResolveCacheEvictions: _resolveCache.Evictions,
            TraceCacheEntries: _traceCache.Count,
            TraceCacheCapacity: _traceCache.Capacity,
            TraceCacheHits: _traceCacheHits,
            TraceCacheMisses: _traceCacheMisses,
            TraceCacheEvictions: _traceCache.Evictions
        );
public string Resolve(string choiceKey)
    {
        if (string.IsNullOrWhiteSpace(choiceKey))
            return string.Empty;

        var trimmed = choiceKey.Trim();

        if (_random is null)
            return Resolve(trimmed, baseSeed: 0);

        return ResolveInnerRandom(trimmed, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
    }

    public string Resolve(string choiceKey, int baseSeed)
    {
        if (string.IsNullOrWhiteSpace(choiceKey))
            return string.Empty;

        var trimmed = choiceKey.Trim();

        var cacheKey = new CacheKey(trimmed, baseSeed);
        if (_resolveCache.TryGetValue(cacheKey, out var cached))
        {
            _resolveCacheHits++;
            return cached;
        }

        _resolveCacheMisses++;

        var resolved = ResolveInnerDeterministic(
            trimmed,
            baseSeed,
            depth: 0,
            seen: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        );

        _resolveCache.Set(cacheKey, resolved);
        return resolved;
    }

    internal ProduceTraceResult ResolveWithTraceCached(string choiceKey, int baseSeed)
    {
        if (string.IsNullOrWhiteSpace(choiceKey))
            return new ProduceTraceResult(
                new ProduceResolutionTrace(string.Empty, baseSeed, Array.Empty<ProduceResolutionStep>(), string.Empty, CycleDetected: false),
                CacheHit: true
            );

        var trimmed = choiceKey.Trim();
        var cacheKey = new CacheKey(trimmed, baseSeed);

        if (_traceCache.TryGetValue(cacheKey, out var cached))
        {
            _traceCacheHits++;
            return new ProduceTraceResult(cached, CacheHit: true);
        }

        _traceCacheMisses++;

        var steps = new List<ProduceResolutionStep>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var resolved = ResolveInnerDeterministicWithTrace(trimmed, baseSeed, depth: 0, seen, steps, out var cycleDetected);
        var trace = new ProduceResolutionTrace(trimmed, baseSeed, steps.ToArray(), resolved, cycleDetected);

        _traceCache.Set(cacheKey, trace);
        return new ProduceTraceResult(trace, CacheHit: false);
    }

    internal ProduceResolutionTrace ResolveWithTrace(string choiceKey, int baseSeed)
        => ResolveWithTraceCached(choiceKey, baseSeed).Trace;

    private string ResolveInnerDeterministicWithTrace(
        string key,
        int baseSeed,
        int depth,
        HashSet<string> seen,
        List<ProduceResolutionStep> steps,
        out bool cycleDetected
    )
    {
        cycleDetected = false;

        if (string.IsNullOrWhiteSpace(key))
            return string.Empty;

        if (!seen.Add(key))
        {
            cycleDetected = true;
            return Translate(key);
        }

        if (_groups.TryGetValue(key, out var options) && options is { Count: > 0 })
        {
            var pickSeed = RoosterSeedKey.ForRandomGroupPick(baseSeed, key, depth);
            var index = (int)((uint)pickSeed % (uint)options.Count);

            var picked = options[index]?.Trim() ?? string.Empty;
            steps.Add(new ProduceResolutionStep(depth, key, pickSeed, index, picked));

            var resolved = ResolveInnerDeterministicWithTrace(picked, baseSeed, depth + 1, seen, steps, out var innerCycle);
            cycleDetected = innerCycle;
            return resolved;
        }

        return Translate(key);
    }

    private string ResolveInnerRandom(string key, HashSet<string> seen)
    {
        if (string.IsNullOrWhiteSpace(key))
            return string.Empty;

        if (!seen.Add(key))
            return Translate(key);

        if (_groups.TryGetValue(key, out var options) && options is { Count: > 0 })
        {
            var picked = options[_random!.Next(options.Count)];
            return ResolveInnerRandom(picked?.Trim() ?? string.Empty, seen);
        }

        return Translate(key);
    }

    private string ResolveInnerDeterministic(string key, int baseSeed, int depth, HashSet<string> seen)
    {
        if (string.IsNullOrWhiteSpace(key))
            return string.Empty;

        if (!seen.Add(key))
            return Translate(key);

        if (_groups.TryGetValue(key, out var options) && options is { Count: > 0 })
        {
            var pickSeed = RoosterSeedKey.ForRandomGroupPick(baseSeed, key, depth);
            var index = (int)((uint)pickSeed % (uint)options.Count);

            var picked = options[index];
            return ResolveInnerDeterministic(picked?.Trim() ?? string.Empty, baseSeed, depth + 1, seen);
        }

        return Translate(key);
    }

    private static string Translate(string key)
        => ItemIds.VariableValues.TryGetValue(key, out var translated) ? translated : key;

    private readonly record struct CacheKey(string Key, int BaseSeed);

    private sealed class CacheKeyComparer : IEqualityComparer<CacheKey>
    {
        public bool Equals(CacheKey x, CacheKey y)
            => x.BaseSeed == y.BaseSeed
               && StringComparer.OrdinalIgnoreCase.Equals(x.Key, y.Key);

        public int GetHashCode(CacheKey obj)
            => HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Key ?? string.Empty),
                obj.BaseSeed
            );
    }
}

internal readonly record struct ProduceTraceResult(ProduceResolutionTrace Trace, bool CacheHit);

internal readonly record struct ProduceResolverCacheStats(
    int ResolveCacheEntries,
    int ResolveCacheCapacity,
    long ResolveCacheHits,
    long ResolveCacheMisses,
    long ResolveCacheEvictions,
    int TraceCacheEntries,
    int TraceCacheCapacity,
    long TraceCacheHits,
    long TraceCacheMisses,
    long TraceCacheEvictions
);
internal sealed record ProduceResolutionTrace(
    string InputKey,
    int BaseSeed,
    IReadOnlyList<ProduceResolutionStep> Steps,
    string ResolvedKey,
    bool CycleDetected
);

internal sealed record ProduceResolutionStep(
    int Depth,
    string GroupKey,
    int PickSeed,
    int Index,
    string Picked
);
