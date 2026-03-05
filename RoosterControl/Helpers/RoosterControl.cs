using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using Mikew.NestCore.Framework.Random;
using RoosterControl.Application.Catalogs;
using RoosterControl.Application.FarmAnimals;
using RoosterControl.Application.Planning;
using RoosterControl.Application.Produces;
using RoosterControl.Constants;
using RoosterControl.Enums;
using RoosterControl.Framework.Config;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.GameData.FarmAnimals;

using LuckyTypes = Mikew.NestCore.EnumsAndStrings.Enums.LuckyTypes;

namespace RoosterControl.Helpers;

/// <summary>
/// Runtime rooster model builder and utilities.
/// </summary>
public sealed partial class RoosterHelper
{
    private readonly FarmAnimalRatioWeightingService _weighting = new();
    private readonly IModHelper _helper;
    private readonly IMonitor _monitor;

    private readonly IDeterministicProduceResolver _produceResolver;
    private readonly ProduceCatalog? _produceCatalog;
    private readonly RoosterCatalog? _roosterCatalog;

    private readonly HashSet<string> _dailyOnceKeys = new(StringComparer.OrdinalIgnoreCase);

    public RoosterControlConfig Config { get; private set; }
    public LuckyTypes DailyLuck { private get; set; } = LuckyTypes.SuperUnlucky;

    public Dictionary<RoosterType, RoosterData> Roosters { get; private set; } = new();
    public Dictionary<string, Dictionary<EggTypeEntries, string>> FemaleEggs { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    public IDeterministicProduceResolver ProduceResolver => _produceResolver;

    public RoosterHelper(
        RoosterControlConfig config,
        IModHelper helper,
        IMonitor monitor,
        RoosterCatalog? roosterCatalog = null,
        ProduceCatalog? produceCatalog = null
    )
    {
        Config = config ?? throw new ArgumentNullException(nameof(config));
        _helper = helper ?? throw new ArgumentNullException(nameof(helper));
        _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        _roosterCatalog = roosterCatalog;
        _produceCatalog = produceCatalog;
        _produceResolver = new RandomGroupProduceResolver(produceCatalog);
    }

    public void UpdateConfig(RoosterControlConfig config)
    {
        Config = config ?? throw new ArgumentNullException(nameof(config));
        InvalidateProduceResolverCaches();
    }


    internal void InvalidateProduceResolverCaches()
    {
        if (_produceResolver is RandomGroupProduceResolver rg)
            rg.ClearCaches();
    }

    public static bool UpdatePercentage(int chance)
    {
        if (chance <= 0)
            return false;
        if (chance >= 100)
            return true;

        return GetPercentageRng().Next(100) < chance;
    }

    public static bool UpdatePercentage(float chance)
    {
        if (chance <= 0)
            return false;
        if (chance >= 100)
            return true;

        return GetPercentageRng().NextDouble() * 100.0 < chance;
    }

    private static IRandomProvider GetPercentageRng()
        => Context.IsWorldReady ? new GameRandomProvider() : new SeededRandomProvider(0);

    public void SetUpRoosters()
    {
        EnsureRoosterDefaults(Config);

        var normalEggProbability = ApplyLuckAdjustments(Config.General.EggProbability);
        var largeEggProbability = ApplyLuckAdjustments(Config.General.LargeEggProbability);
        var fertilizedEggProbability = Config.General.HatchingMode == HatchingMode.OnlyFertilizedEggs
            ? ApplyLuckAdjustments(Config.General.FertilizedEggProbability)
            : 0;

        var defaultCatalog = CatalogDefaults.CreateRoosterCatalogDefaults();
        var (farmId, day, locationName) = GetWorldSeedInputs();

        var roosterMap = new Dictionary<RoosterType, RoosterData>();
        foreach (var roosterType in RoosterControlConfig.CreateDefaultRoosters().Keys)
        {
            var settings = Config.Roosters[roosterType];

            var farmAnimalName = GetFarmAnimalKey(roosterType);
            var femaleKey = GetDefaultFemaleKey(roosterType);

            var seedCtx = new RoosterSeedKey.Context(
                FarmId: farmId,
                Day: day,
                LocationName: locationName,
                BuildingType: settings.BuildingRequired.ToString(),
                FarmAnimalName: farmAnimalName,
                RoosterType: roosterType
            );

            var eggs = BuildEggsForRooster(
                roosterType,
                settings,
                defaultCatalog,
                normalEggProbability,
                largeEggProbability,
                fertilizedEggProbability,
                seedCtx
            );

            var rooster = new RoosterData
            {
                RoosterType = roosterType,
                HarvestType = Config.General.HarvestType,
                BuildingRequired = settings.BuildingRequired,
                ProduceOption = settings.ProduceOption,
                DeluxeProduceOption = settings.DeluxeProduceOption,
                Female = femaleKey,
                FarmAnimalName = farmAnimalName,
                ProduceDays = settings.ProduceDays,
                MatureDays = settings.MatureDays,
                Price = settings.Price,
                Buyable = settings.Buyable,
                Eggs = eggs,
                AllProduces = BuildProduces(settings, seedCtx),
                Quantity = UpdateRoosterQuantity(farmAnimalName),
                Females = GetAdditionalFemaleKeys(roosterType),
            };

            RollChosenProduces(rooster, seedCtx);
            ApplyFertilizedBoost(rooster);

            roosterMap[roosterType] = rooster;
        }

        Roosters = roosterMap;
    }

        private static (ulong FarmId, int Day, string LocationName) GetWorldSeedInputs()
    {
        return RoosterSeedPolicy.GetWorldSeedInputs();
    }

    private static void EnsureRoosterDefaults(RoosterControlConfig cfg)
    {
        cfg.Roosters ??= new Dictionary<RoosterType, RoosterSettings>();
        foreach (var (key, def) in RoosterControlConfig.CreateDefaultRoosters())
            cfg.Roosters.TryAdd(key, def);
    }

    private Dictionary<EggTypeEntries, Dictionary<RoosterItemDataEntries, string>> BuildEggsForRooster(
        RoosterType roosterType,
        RoosterSettings settings,
        RoosterCatalog defaultCatalog,
        int normalEggProbability,
        int largeEggProbability,
        int fertilizedEggProbability,
        in RoosterSeedKey.Context seedCtx
    )
    {
        var enableHatching = settings.EnableRoosterHatching;

        var probs = new[] { normalEggProbability, largeEggProbability, fertilizedEggProbability };
        if (Config.General.HatchingMode == HatchingMode.OnlyFertilizedEggs)
        {
            probs[0] = 0;
            probs[1] = 0;
        }

        var eggCatalog = GetEggCatalog(roosterType, defaultCatalog);

        return new Dictionary<EggTypeEntries, Dictionary<RoosterItemDataEntries, string>>
        {
            [EggTypeEntries.Normal] = new Dictionary<RoosterItemDataEntries, string>
            {
                [RoosterItemDataEntries.Name] = ResolveChoiceDeterministic(
                    eggCatalog.Normal,
                    seedCtx,
                    channel: "egg:normal",
                    slotOrdinal: 1
                ),
                [RoosterItemDataEntries.Probability] = enableHatching ? probs[0].ToString(CultureInfo.InvariantCulture) : "0",
            },
            [EggTypeEntries.Large] = new Dictionary<RoosterItemDataEntries, string>
            {
                [RoosterItemDataEntries.Name] = ResolveChoiceDeterministic(
                    eggCatalog.Large,
                    seedCtx,
                    channel: "egg:large",
                    slotOrdinal: 2
                ),
                [RoosterItemDataEntries.Probability] = enableHatching ? probs[1].ToString(CultureInfo.InvariantCulture) : "0",
            },
            [EggTypeEntries.Fertilized] = new Dictionary<RoosterItemDataEntries, string>
            {
                [RoosterItemDataEntries.Name] = ResolveChoiceDeterministic(
                    eggCatalog.Fertilized,
                    seedCtx,
                    channel: "egg:fertilized",
                    slotOrdinal: 3
                ),
                [RoosterItemDataEntries.Probability] = enableHatching ? probs[2].ToString(CultureInfo.InvariantCulture) : "0",
            }
        };
    }

    private EggCatalog GetEggCatalog(RoosterType roosterType, RoosterCatalog defaults)
    {
        if (_roosterCatalog?.Roosters is not null
            && _roosterCatalog.Roosters.TryGetValue(roosterType.ToString(), out var entry)
            && entry.Eggs is not null
            && !string.IsNullOrWhiteSpace(entry.Eggs.Normal)
            && !string.IsNullOrWhiteSpace(entry.Eggs.Large)
            && !string.IsNullOrWhiteSpace(entry.Eggs.Fertilized))
        {
            return entry.Eggs;
        }

        return defaults.Roosters[roosterType.ToString()].Eggs;
    }

    private Dictionary<ProduceTypeEntries, Dictionary<ProduceEntries, Dictionary<RoosterItemDataEntries, string>>> BuildProduces(
        RoosterSettings settings,
        in RoosterSeedKey.Context seedCtx
    )
    {
        var produce = BuildProduceEntries(settings.ProduceEntries, seedCtx, ProduceTypeEntries.Produce);
        var deluxe = BuildProduceEntries(settings.DeluxeProduceEntries, seedCtx, ProduceTypeEntries.Deluxe);

        return new Dictionary<ProduceTypeEntries, Dictionary<ProduceEntries, Dictionary<RoosterItemDataEntries, string>>>
        {
            [ProduceTypeEntries.Produce] = produce,
            [ProduceTypeEntries.Deluxe] = deluxe,
        };
    }

    private Dictionary<ProduceEntries, Dictionary<RoosterItemDataEntries, string>> BuildProduceEntries(
        List<ProduceEntry> entries,
        in RoosterSeedKey.Context seedCtx,
        ProduceTypeEntries produceType
    )
    {
        entries ??= ProduceEntry.CreateDefaultFour();
        while (entries.Count < 4)
            entries.Add(new ProduceEntry());

        while (entries.Count > 4)
            entries.RemoveAt(entries.Count - 1);

        return new Dictionary<ProduceEntries, Dictionary<RoosterItemDataEntries, string>>
        {
            [ProduceEntries.First] = MapProduceEntry(entries[0], seedCtx, produceType, ProduceEntries.First, slotOrdinal: 1),
            [ProduceEntries.Second] = MapProduceEntry(entries[1], seedCtx, produceType, ProduceEntries.Second, slotOrdinal: 2),
            [ProduceEntries.Third] = MapProduceEntry(entries[2], seedCtx, produceType, ProduceEntries.Third, slotOrdinal: 3),
            [ProduceEntries.Fourth] = MapProduceEntry(entries[3], seedCtx, produceType, ProduceEntries.Fourth, slotOrdinal: 4),
            [ProduceEntries.Chosen] = new Dictionary<RoosterItemDataEntries, string>(),
        };
    }

    private Dictionary<RoosterItemDataEntries, string> MapProduceEntry(
        ProduceEntry entry,
        in RoosterSeedKey.Context seedCtx,
        ProduceTypeEntries produceType,
        ProduceEntries slot,
        int slotOrdinal
    )
    {
        entry ??= new ProduceEntry();

        var choice = entry.Choice ?? string.Empty;
        var keyToResolve = choice.Equals(ItemIds.CustomItems.Custom, StringComparison.OrdinalIgnoreCase)
            ? entry.CustomItemName ?? ItemIds.CustomItems.Custom
            : choice;

        var resolved = ResolveChoiceDeterministic(
            keyToResolve,
            seedCtx,
            channel: $"produce:{produceType}:{slot}",
            slotOrdinal: slotOrdinal
        );

        return new Dictionary<RoosterItemDataEntries, string>
        {
            [RoosterItemDataEntries.Name] = resolved,
            [RoosterItemDataEntries.Probability] = Math.Clamp(entry.Probability, 0, 100).ToString(CultureInfo.InvariantCulture),
        };
    }

    public string ResolveChoiceDeterministic(string choiceKey, RoosterData rooster, string channel, int slotOrdinal = 0)
    {
        if (rooster is null) throw new ArgumentNullException(nameof(rooster));

        var (farmId, day, locationName) = GetWorldSeedInputs();
        var ctx = new RoosterSeedKey.Context(
            FarmId: farmId,
            Day: day,
            LocationName: locationName,
            BuildingType: rooster.BuildingRequired.ToString(),
            FarmAnimalName: rooster.FarmAnimalName ?? string.Empty,
            RoosterType: rooster.RoosterType
        );

        return ResolveChoiceDeterministic(choiceKey, ctx, channel, slotOrdinal);
    }

    private string ResolveChoiceDeterministic(string choiceKey, in RoosterSeedKey.Context seedCtx, string channel, int slotOrdinal)
    {
        var baseSeed = RoosterSeedKey.ForChoiceResolution(seedCtx, channel, slotOrdinal);
        var resolved = _produceResolver.Resolve(choiceKey, baseSeed);

#if RCTRL_DEV
        WarnIfSuspiciousRandomGroup(choiceKey, resolved, seedCtx);
#endif

        return resolved;
    }

#if RCTRL_DEV
    private void WarnIfSuspiciousRandomGroup(string choiceKey, string resolved, in RoosterSeedKey.Context seedCtx)
    {
        if (_produceCatalog?.RandomGroups is null || string.IsNullOrWhiteSpace(choiceKey))
            return;

        var trimmed = choiceKey.Trim();

        var isKnownGroup = _produceCatalog.RandomGroups.TryGetValue(trimmed, out _);
        if (isKnownGroup && resolved.Equals(trimmed, StringComparison.OrdinalIgnoreCase))
        {
            LogOncePerDay(
                seedCtx.Day,
                $"cycle|{trimmed}",
                $"[determinism] Random group '{trimmed}' appears cyclic/unresolvable (resolved to itself).",
                LogLevel.Warn
            );
            return;
        }

        var looksRandomGroup = trimmed.StartsWith("Random", StringComparison.OrdinalIgnoreCase);
        var isTranslatedVar = ItemIds.VariableValues.ContainsKey(trimmed);

        if (looksRandomGroup && !isKnownGroup && !isTranslatedVar)
        {
            LogOncePerDay(
                seedCtx.Day,
                $"unknown|{trimmed}",
                $"[determinism] Unknown random-group key '{trimmed}'. Did you forget to define it in the produce catalog?",
                LogLevel.Warn
            );
        }
    }
#endif

    private void LogOncePerDay(int day, string key, string message, LogLevel level)
    {
        var fullKey = $"{day}|{key}";
        if (!_dailyOnceKeys.Add(fullKey))
            return;

        _monitor.Log(message, level);
    }

    private static int SumWeights(IReadOnlyDictionary<ProduceEntries, Dictionary<RoosterItemDataEntries, string>> entries)
    {
        var sum = 0;
        foreach (var (k, v) in entries)
        {
            if (k == ProduceEntries.Chosen || v is null)
                continue;

            _ = v.TryGetValue(RoosterItemDataEntries.Probability, out var raw);
            _ = int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var w);
            sum += Math.Max(0, w);
        }
        return sum;
    }

    private void MaybeLogDailyProduceSummary(RoosterData rooster, int day)
    {
        if (!Config.General.EnableDebug)
            return;

        if (!rooster.AllProduces.TryGetValue(ProduceTypeEntries.Produce, out var produce))
            return;

        if (!rooster.AllProduces.TryGetValue(ProduceTypeEntries.Deluxe, out var deluxe))
            return;

        var chosenProduce = produce[ProduceEntries.Chosen];
        var chosenDeluxe = deluxe[ProduceEntries.Chosen];

        _ = chosenProduce.TryGetValue(RoosterItemDataEntries.Name, out var cpName);
        _ = chosenProduce.TryGetValue(RoosterItemDataEntries.Probability, out var cpWeight);

        _ = chosenDeluxe.TryGetValue(RoosterItemDataEntries.Name, out var cdName);
        _ = chosenDeluxe.TryGetValue(RoosterItemDataEntries.Probability, out var cdWeight);

        var sumProduce = SumWeights(produce);
        var sumDeluxe = SumWeights(deluxe);

        LogOncePerDay(
            day,
            $"produceSummary|{rooster.RoosterType}",
            $"[produce] {rooster.RoosterType}: chosen={cpName} (w {cpWeight}), deluxe={cdName} (w {cdWeight}); tableSums produce={sumProduce}, deluxe={sumDeluxe}",
            LogLevel.Trace
        );
    }

    private void RollChosenProduces(RoosterData rooster, in RoosterSeedKey.Context seedCtx)
    {
        var produceSeed = RoosterSeedKey.ForChosenProduce(seedCtx, ProduceTypeEntries.Produce);
        var deluxeSeed = RoosterSeedKey.ForChosenProduce(seedCtx, ProduceTypeEntries.Deluxe);

        var rngProduce = new SeededRandomProvider(produceSeed);
        var rngDeluxe = new SeededRandomProvider(deluxeSeed);

        rooster.AllProduces[ProduceTypeEntries.Produce][ProduceEntries.Chosen] =
            _weighting.RollChosenProduce(rooster.AllProduces[ProduceTypeEntries.Produce], rngProduce);

        rooster.AllProduces[ProduceTypeEntries.Deluxe][ProduceEntries.Chosen] =
            _weighting.RollChosenProduce(rooster.AllProduces[ProduceTypeEntries.Deluxe], rngDeluxe);

        MaybeLogDailyProduceSummary(rooster, seedCtx.Day);
    }

#if RCTRL_DEV
    public string DebugDumpChosenProduce(
        RoosterType roosterType,
        int? dayOverride = null,
        string? locationNameOverride = null,
        string? buildingTypeOverride = null,
        int? seedOverride = null,
        bool full = false,
        bool json = false
    )
    {
        if (!Roosters.TryGetValue(roosterType, out var rooster))
            return $"[ra_debug_produce] Unknown rooster type: {roosterType}";

        if (!Config.Roosters.TryGetValue(roosterType, out var settings) || settings is null)
            return $"[ra_debug_produce] Missing config settings for rooster type: {roosterType}";

        var buildingType = !string.IsNullOrWhiteSpace(buildingTypeOverride)
            ? buildingTypeOverride!
            : rooster.BuildingRequired.ToString();

        var seedCtx = RoosterSeedPolicy.BuildContext(
            roosterType: rooster.RoosterType,
            farmAnimalName: rooster.FarmAnimalName ?? string.Empty,
            buildingType: buildingType,
            dayOverride: dayOverride,
            locationNameOverride: locationNameOverride,
            seedOverride: seedOverride
        );


        var reproHash = RoosterSeedPolicy.ComputeReproHash("produce", seedCtx);
        var anomalies = new List<DebugWeightAnomaly>();

        static void AddWeightAnomaly(
            List<DebugWeightAnomaly> list,
            string channel,
            DebugProduceSlotDump slotDump,
            string reason
        )
        {
            if (slotDump.RawWeight != slotDump.Weight)
            {
                list.Add(new DebugWeightAnomaly(
                    Channel: channel,
                    SlotOrdinal: slotDump.SlotOrdinal,
                    Slot: slotDump.Slot,
                    RawWeight: slotDump.RawWeight,
                    SanitizedWeight: slotDump.Weight,
                    Reason: reason
                ));
            }
        }

        var produceSeed = RoosterSeedKey.ForChosenProduce(seedCtx, ProduceTypeEntries.Produce);
        var deluxeSeed = RoosterSeedKey.ForChosenProduce(seedCtx, ProduceTypeEntries.Deluxe);

        var produceRoll = RollChosenSlot(settings.ProduceEntries, new SeededRandomProvider(produceSeed));
        var deluxeRoll = RollChosenSlot(settings.DeluxeProduceEntries, new SeededRandomProvider(deluxeSeed));

        var produceChosen = BuildSlotDump(seedCtx, ProduceTypeEntries.Produce, produceRoll);
        var deluxeChosen = BuildSlotDump(seedCtx, ProduceTypeEntries.Deluxe, deluxeRoll);
        var produceCandidates = full ? BuildCandidates(seedCtx, ProduceTypeEntries.Produce, settings.ProduceEntries) : null;
        var deluxeCandidates = full ? BuildCandidates(seedCtx, ProduceTypeEntries.Deluxe, settings.DeluxeProduceEntries) : null;

        // Audit chosen weights (always) and candidates (when --full).
        AddWeightAnomaly(anomalies, "produce:chosen", produceChosen, reason: "chosen slot weight clamped to [0..100]");
        AddWeightAnomaly(anomalies, "deluxe:chosen", deluxeChosen, reason: "chosen slot weight clamped to [0..100]");

        if (produceChosen.CycleDetected)
            anomalies.Add(new DebugWeightAnomaly("produce:chosen", produceChosen.SlotOrdinal, produceChosen.Slot, produceChosen.RawWeight, produceChosen.Weight, "cycle detected during random-group expansion"));
        if (deluxeChosen.CycleDetected)
            anomalies.Add(new DebugWeightAnomaly("deluxe:chosen", deluxeChosen.SlotOrdinal, deluxeChosen.Slot, deluxeChosen.RawWeight, deluxeChosen.Weight, "cycle detected during random-group expansion"));

        if (produceCandidates is not null)
            foreach (var c in produceCandidates)
                AddWeightAnomaly(anomalies, "produce:candidate", c, reason: "candidate weight clamped to [0..100]");
        if (deluxeCandidates is not null)
            foreach (var c in deluxeCandidates)
                AddWeightAnomaly(anomalies, "deluxe:candidate", c, reason: "candidate weight clamped to [0..100]");

        DebugProduceCacheStats? cacheStats = null;
        if (full && _produceResolver is RandomGroupProduceResolver rg)
        {
            var s = rg.GetCacheStats();
            cacheStats = new DebugProduceCacheStats(
                ResolveCacheEntries: s.ResolveCacheEntries,
                ResolveCacheCapacity: s.ResolveCacheCapacity,
                ResolveCacheHits: s.ResolveCacheHits,
                ResolveCacheMisses: s.ResolveCacheMisses,
                ResolveCacheEvictions: s.ResolveCacheEvictions,
                TraceCacheEntries: s.TraceCacheEntries,
                TraceCacheCapacity: s.TraceCacheCapacity,
                TraceCacheHits: s.TraceCacheHits,
                TraceCacheMisses: s.TraceCacheMisses,
                TraceCacheEvictions: s.TraceCacheEvictions
            );
        }

        var anomaliesOrNull = anomalies.Count == 0 ? null : anomalies;

        if (json)
        {
            var dump = new DebugProduceDump(
                RoosterType: roosterType.ToString(),
                FarmAnimalName: seedCtx.FarmAnimalName,
                FarmId: seedCtx.FarmId,
                Day: seedCtx.Day,
                LocationName: seedCtx.LocationName,
                BuildingType: seedCtx.BuildingType,
                SeedOverride: seedOverride,
                ReproHash: reproHash,
                ProduceSeed: produceSeed,
                DeluxeSeed: deluxeSeed,
                ChosenProduce: produceChosen,
                ChosenDeluxe: deluxeChosen,
                ProduceCandidates: produceCandidates,
                DeluxeCandidates: deluxeCandidates,
                WeightAnomalies: anomaliesOrNull,
                CacheStats: cacheStats
            );

            return JsonSerializer.Serialize(dump, new JsonSerializerOptions { WriteIndented = false });
        }

        var sb = new StringBuilder();
        sb.AppendLine("[ra_debug_produce] Inputs:");
        sb.AppendLine($"  roosterType: {roosterType}");
        sb.AppendLine($"  farmAnimalName: {seedCtx.FarmAnimalName}");
        sb.AppendLine($"  farmId: {seedCtx.FarmId}");
        sb.AppendLine($"  day: {seedCtx.Day}");
        sb.AppendLine($"  locationName: {seedCtx.LocationName}");
        sb.AppendLine($"  buildingType: {seedCtx.BuildingType}");
        sb.AppendLine($"  seedOverride: {(seedOverride is null ? "null" : seedOverride.Value.ToString(CultureInfo.InvariantCulture))}");
        sb.AppendLine($"  reproHash: {reproHash}");
        sb.AppendLine("[ra_debug_produce] Seeds:");
        sb.AppendLine($"  produceSeed: {produceSeed}");
        sb.AppendLine($"  deluxeSeed: {deluxeSeed}");
        sb.AppendLine("[ra_debug_produce] Results:");
        sb.AppendLine($"  chosenProduce: {produceChosen.ResolvedId} (slot {produceChosen.Slot} weight {produceChosen.Weight})");
        sb.AppendLine($"  chosenDeluxe: {deluxeChosen.ResolvedId} (slot {deluxeChosen.Slot} weight {deluxeChosen.Weight})");

                if (full)
        {
            var pList = produceCandidates ?? new List<DebugProduceSlotDump>();
            var dList = deluxeCandidates ?? new List<DebugProduceSlotDump>();

            var pTotal = pList.Sum(c => c.Weight);
            var dTotal = dList.Sum(c => c.Weight);

            sb.AppendLine("[ra_debug_produce] Sanity:");
            sb.AppendLine($"  produceTotalWeight: {pTotal}");
            sb.AppendLine($"  deluxeTotalWeight: {dTotal}");

            sb.AppendLine("[ra_debug_produce] ProduceCandidates:");
            foreach (var c in pList)
            {
                var norm = pTotal > 0 ? (c.Weight / (double)pTotal).ToString("0.####", CultureInfo.InvariantCulture) : "n/a";
                sb.AppendLine(
                    $"  {c.Slot}: key={c.ChoiceKey} resolved={c.ResolvedId} rawWeight={c.RawWeight} weight={c.Weight} norm={norm} " +
                    $"baseSeed={c.BaseSeed} traceSteps={c.TraceSteps?.Count ?? 0} cacheHit={c.TraceCacheHit} cycle={c.CycleDetected}"
                );
            }

            sb.AppendLine("[ra_debug_produce] DeluxeCandidates:");
            foreach (var c in dList)
            {
                var norm = dTotal > 0 ? (c.Weight / (double)dTotal).ToString("0.####", CultureInfo.InvariantCulture) : "n/a";
                sb.AppendLine(
                    $"  {c.Slot}: key={c.ChoiceKey} resolved={c.ResolvedId} rawWeight={c.RawWeight} weight={c.Weight} norm={norm} " +
                    $"baseSeed={c.BaseSeed} traceSteps={c.TraceSteps?.Count ?? 0} cacheHit={c.TraceCacheHit} cycle={c.CycleDetected}"
                );
            }

            if (cacheStats is not null)
            {
                sb.AppendLine("[ra_debug_produce] CacheStats:");
                sb.AppendLine($"  resolve: entries={cacheStats.ResolveCacheEntries}/{cacheStats.ResolveCacheCapacity} hits={cacheStats.ResolveCacheHits} misses={cacheStats.ResolveCacheMisses} evictions={cacheStats.ResolveCacheEvictions}");
                sb.AppendLine($"  trace: entries={cacheStats.TraceCacheEntries}/{cacheStats.TraceCacheCapacity} hits={cacheStats.TraceCacheHits} misses={cacheStats.TraceCacheMisses} evictions={cacheStats.TraceCacheEvictions}");
            }

            if (anomaliesOrNull is not null)
            {
                sb.AppendLine("[ra_debug_produce] Anomalies:");
                foreach (var a in anomaliesOrNull)
                    sb.AppendLine($"  {a.Channel} slot={a.Slot} raw={a.RawWeight} weight={a.SanitizedWeight} reason={a.Reason}");
            }
        }


        return sb.ToString().TrimEnd();
    }

    private int RollChosenSlot(IReadOnlyList<ProduceEntry> entries, IRandomProvider rng)
    {
        var table = new WeightedTable<int>(rng);

        for (var i = 0; i < entries.Count && i < 4; i++)
        {
            var e = entries[i] ?? new ProduceEntry();
            table.Add(i, Math.Clamp(e.Probability, 0, 100));
        }

        return table.RollOrDefault(0);
    }

    private DebugProduceSlotDump BuildSlotDump(in RoosterSeedKey.Context seedCtx, ProduceTypeEntries produceType, int slotIndex)
    {
        var slot = (ProduceEntries)slotIndex;
        var slotOrdinal = slotIndex + 1;
        var channel = $"produce:{produceType}:{slot}";

        var settings = Config.Roosters[seedCtx.RoosterType];
        var entry = (produceType == ProduceTypeEntries.Produce ? settings.ProduceEntries : settings.DeluxeProduceEntries)[slotIndex] ?? new ProduceEntry();

        var choice = (entry.Choice ?? string.Empty).Trim();
        var choiceKey = choice.Equals(ItemIds.CustomItems.Custom, StringComparison.OrdinalIgnoreCase)
            ? (entry.CustomItemName ?? ItemIds.CustomItems.Custom).Trim()
            : choice;

        var baseSeed = RoosterSeedKey.ForChoiceResolution(seedCtx, channel, slotOrdinal);

        var (resolved, traceSteps, traceCacheHit, cycleDetected) = ResolveWithOptionalTrace(choiceKey, baseSeed);

        return new DebugProduceSlotDump(slotOrdinal, slot.ToString(), choiceKey, resolved, Math.Clamp(entry.Probability, 0, 100), baseSeed, traceSteps, traceCacheHit, cycleDetected, entry.Probability);
    }

    private List<DebugProduceSlotDump> BuildCandidates(in RoosterSeedKey.Context seedCtx, ProduceTypeEntries produceType, IReadOnlyList<ProduceEntry> entries)
    {
        var list = new List<DebugProduceSlotDump>();

        for (var i = 0; i < entries.Count && i < 4; i++)
        {
            var slot = (ProduceEntries)i;
            var slotOrdinal = i + 1;
            var channel = $"produce:{produceType}:{slot}";

            var entry = entries[i] ?? new ProduceEntry();
            var choice = (entry.Choice ?? string.Empty).Trim();
            var choiceKey = choice.Equals(ItemIds.CustomItems.Custom, StringComparison.OrdinalIgnoreCase)
                ? (entry.CustomItemName ?? ItemIds.CustomItems.Custom).Trim()
                : choice;

            var baseSeed = RoosterSeedKey.ForChoiceResolution(seedCtx, channel, slotOrdinal);
            var (resolved, traceSteps, traceCacheHit, cycleDetected) = ResolveWithOptionalTrace(choiceKey, baseSeed);

            list.Add(new DebugProduceSlotDump(slotOrdinal, slot.ToString(), choiceKey, resolved, Math.Clamp(entry.Probability, 0, 100), baseSeed, traceSteps, traceCacheHit, cycleDetected, entry.Probability));
        }

        return list;
    }

        private (string Resolved, IReadOnlyList<DebugResolutionStep>? Steps, bool TraceCacheHit, bool CycleDetected) ResolveWithOptionalTrace(
        string choiceKey,
        int baseSeed
    )
    {
        if (_produceResolver is RandomGroupProduceResolver rg)
        {
            var tr = rg.ResolveWithTraceCached(choiceKey, baseSeed);
            var steps = tr.Trace.Steps.Select(s => new DebugResolutionStep(s.Depth, s.GroupKey, s.PickSeed, s.Index, s.Picked)).ToList();
            return (tr.Trace.ResolvedKey, steps, tr.CacheHit, tr.Trace.CycleDetected);
        }

        return (_produceResolver.Resolve(choiceKey, baseSeed), null, TraceCacheHit: false, CycleDetected: false);
    }
#endif


    private void ApplyFertilizedBoost(RoosterData rooster)
    {
        if (Config.General.HatchingMode != HatchingMode.OnlyFertilizedEggs)
        {
            rooster.Eggs[EggTypeEntries.Fertilized][RoosterItemDataEntries.Probability] = "0";
            return;
        }

        var raw = rooster.Eggs[EggTypeEntries.Fertilized][RoosterItemDataEntries.Probability];
        if (!float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var baseProb))
            baseProb = 0;

        rooster.Eggs[EggTypeEntries.Fertilized][RoosterItemDataEntries.Probability] =
            ApplyRoosterBoost(rooster.Quantity, baseProb).ToString(CultureInfo.InvariantCulture);
    }

    private float ApplyRoosterBoost(int quantity, float originalPercentage)
    {
        if (originalPercentage < 0)
            return originalPercentage;

        if (quantity <= 0)
            return 0;

        var adjusted = originalPercentage + quantity * Config.General.BoostByRooster;
        return Math.Clamp(adjusted, 0f, 100f);
    }


    private static HashSet<string>? _coopRoosterFarmAnimalKeys;

    private static readonly HashSet<string> DefaultCoopRoosterFarmAnimalKeys =
        new(StringComparer.OrdinalIgnoreCase)
        {
            RoosterKeys.White,
            RoosterKeys.Void,
            RoosterKeys.Blue,
            RoosterKeys.Gold,
            RoosterKeys.Brown,
        };

    /// <summary>Overrides which <see cref="FarmAnimal.type"/> keys are treated as roosters for coop scanning.</summary>
    public static void SetCoopRoosterFarmAnimalKeys(IEnumerable<string> keys)
    {
        if (keys is null)
        {
            _coopRoosterFarmAnimalKeys = null;
            return;
        }

        var set = keys
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => k.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        _coopRoosterFarmAnimalKeys = set.Count > 0 ? set : null;
    }

    private static HashSet<string> GetCoopRoosterFarmAnimalKeys()
        => _coopRoosterFarmAnimalKeys is { Count: > 0 }
            ? _coopRoosterFarmAnimalKeys
            : DefaultCoopRoosterFarmAnimalKeys;

    public static IEnumerable<FarmAnimal> GetRoostersFromCoops()
    {
        return Game1
            .getFarm()
            .buildings.Where(IsCoop)
            .SelectMany(GetRoostersFromCoop);
    }

    private static bool IsCoop(Building b)
        => b.buildingType.Value.Contains("coop", StringComparison.OrdinalIgnoreCase);

    private static IEnumerable<FarmAnimal> GetRoostersFromCoop(Building coop)
    {
        var animalHouse = (AnimalHouse)coop.indoors.Value;
        var cockTypes = GetCoopRoosterFarmAnimalKeys();
        return animalHouse.Animals.Values.Where(animal => cockTypes.Contains(animal.type.Value));
    }

    public void UpdateRoosterQuantity_All()
    {
        foreach (var rooster in Roosters.Values)
            rooster.Quantity = UpdateRoosterQuantity(rooster.FarmAnimalName);
    }

    private static int UpdateRoosterQuantity(string farmName)
    {
        var quantityInCoops = Game1
            .getFarm()
            .buildings.Where(IsCoop)
            .Sum(coop =>
                ((AnimalHouse)coop.indoors.Value).animals.Values.Count(animal =>
                    animal.type.Value == farmName && animal.isAdult()
                )
            );

        var quantityOutsideCoops = Game1
            .getFarm()
            .animals.Values.Count(animal =>
                IsCoop(animal.home) && animal.type.Value == farmName && animal.isAdult()
            );

        return quantityInCoops + quantityOutsideCoops;
    }

    public int ApplyLuckAdjustments(int originalPercentage)
    {
        if (originalPercentage == 0)
            return 0;

        var baseBonus = Math.Clamp(Config.General.ChosenPercentageDailyLuckBonus, 0, 100);

#if RCTRL_DEV
        var bonusMult = Math.Clamp(Config.DebugKnobs.BonusCalculationMultiplier, 0.0f, 10.0f);
        var bonus = Math.Clamp((int)Math.Round(baseBonus * bonusMult, MidpointRounding.AwayFromZero), 0, 100);
        var superUnluckyMult = Config.DebugKnobs.SuperUnluckyBonusMultiplier;
        var superLuckyMult = Config.DebugKnobs.SuperLuckyBonusMultiplier;
        var offset = Config.DebugKnobs.LuckAdjustmentOffset;
#else
        var bonus = baseBonus;
        const float superUnluckyMult = -2.0f;
        const float superLuckyMult = 1.5f;
        const float offset = 0.0f;
#endif

        var adjustment = DailyLuck switch
        {
            LuckyTypes.SuperUnlucky => (int)Math.Round(superUnluckyMult * bonus, MidpointRounding.AwayFromZero),
            LuckyTypes.Unlucky => -bonus,
            LuckyTypes.Neutral => 0,
            LuckyTypes.Lucky => bonus,
            LuckyTypes.SuperLucky => (int)Math.Round(superLuckyMult * bonus, MidpointRounding.AwayFromZero),
            _ => 0,
        };

        var adjusted = originalPercentage + adjustment;
        if (Math.Abs(offset) > float.Epsilon)
            adjusted = (int)Math.Round(adjusted + offset, MidpointRounding.AwayFromZero);

        return Math.Clamp(adjusted, 0, 100);
    }

    public void UpdateFemaleEggs(RoosterData rooster, IDictionary<string, FarmAnimalData> animalData)
    {
        var eggTypeMap = new Dictionary<string, EggTypeEntries>
        {
            ["Normal"] = EggTypeEntries.Normal,
            ["Large"] = EggTypeEntries.Large,
        };

        foreach (var female in rooster.Females)
        {
            if (!animalData.ContainsKey(female) || animalData[female].CustomFields is null)
                continue;

            FemaleEggs.TryAdd(female, new Dictionary<EggTypeEntries, string>());

            foreach (var (key, value) in animalData[female].CustomFields.Where(cf => eggTypeMap.ContainsKey(cf.Key)))
                FemaleEggs[female][eggTypeMap[key]] = value;
        }

        UpdateRoosterFemaleEggs(rooster);
    }

    private void UpdateRoosterFemaleEggs(RoosterData rooster)
    {
        FemaleEggs.TryAdd(rooster.Female, new Dictionary<EggTypeEntries, string>());

        FemaleEggs[rooster.Female][EggTypeEntries.Normal] = rooster.Eggs[EggTypeEntries.Normal][RoosterItemDataEntries.Name];
        FemaleEggs[rooster.Female][EggTypeEntries.Large] = rooster.Eggs[EggTypeEntries.Large][RoosterItemDataEntries.Name];
    }

    public static RoosterType GetRoosterTypeFromFarmName(string farmName)
    {
        return farmName switch
        {
            RoosterKeys.Blue => RoosterType.Blue,
            RoosterKeys.White => RoosterType.White,
            RoosterKeys.Gold => RoosterType.Gold,
            RoosterKeys.Void => RoosterType.Void,
            RoosterKeys.Brown => RoosterType.Brown,
            _ => RoosterType.White,
        };
    }

    private string GetFarmAnimalKey(RoosterType roosterType)
    {
        if (_roosterCatalog?.Roosters is not null
            && _roosterCatalog.Roosters.TryGetValue(roosterType.ToString(), out var entry)
            && !string.IsNullOrWhiteSpace(entry.FarmAnimalKey))
        {
            return entry.FarmAnimalKey.Trim();
        }

        return roosterType switch
        {
            RoosterType.White => RoosterKeys.White,
            RoosterType.Brown => RoosterKeys.Brown,
            RoosterType.Blue => RoosterKeys.Blue,
            RoosterType.Void => RoosterKeys.Void,
            RoosterType.Gold => RoosterKeys.Gold,
            _ => RoosterKeys.White,
        };
    }

    private string GetDefaultFemaleKey(RoosterType roosterType)
    {
        if (_roosterCatalog?.Roosters is not null
            && _roosterCatalog.Roosters.TryGetValue(roosterType.ToString(), out var entry)
            && !string.IsNullOrWhiteSpace(entry.DefaultFemaleKey))
        {
            return entry.DefaultFemaleKey.Trim();
        }

        return roosterType switch
        {
            RoosterType.White => "White Chicken",
            RoosterType.Brown => "Brown Chicken",
            RoosterType.Blue => "Blue Chicken",
            RoosterType.Void => "Void Chicken",
            RoosterType.Gold => "Golden Chicken",
            _ => "White Chicken",
        };
    }

    private List<string> GetAdditionalFemaleKeys(RoosterType roosterType)
    {
        if (_roosterCatalog?.Roosters is not null
            && _roosterCatalog.Roosters.TryGetValue(roosterType.ToString(), out var entry)
            && entry.AdditionalFemaleKeys is not null)
        {
            return entry.AdditionalFemaleKeys
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        return new List<string>();
    }
}
