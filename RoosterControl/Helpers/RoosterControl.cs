using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
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
public sealed class RoosterHelper
{
    private readonly FarmAnimalRatioWeightingService _weighting = new();
    private readonly IModHelper _helper;
    private readonly IMonitor _monitor;

    private readonly IDeterministicProduceResolver _produceResolver;
    private readonly ProduceCatalog? _produceCatalog;
    private readonly RoosterCatalog? _roosterCatalog;

#if RCTRL_DEV
    private readonly HashSet<string> _determinismWarnings = new(StringComparer.OrdinalIgnoreCase);
#endif

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
        => Config = config ?? throw new ArgumentNullException(nameof(config));

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
        if (!Context.IsWorldReady)
            return (0UL, 0, "not-ready");

        return (
            Game1.uniqueIDForThisGame,
            Game1.Date.TotalDays,
            Game1.getFarm()?.NameOrUniqueName ?? "Farm"
        );
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
            WarnOncePerDay(
                seedCtx.Day,
                $"cycle|{trimmed}",
                $"[determinism] Random group '{trimmed}' appears cyclic/unresolvable (resolved to itself)."
            );
            return;
        }

        var looksRandomGroup = trimmed.StartsWith("Random", StringComparison.OrdinalIgnoreCase);
        var isTranslatedVar = ItemIds.VariableValues.ContainsKey(trimmed);

        if (looksRandomGroup && !isKnownGroup && !isTranslatedVar)
        {
            WarnOncePerDay(
                seedCtx.Day,
                $"unknown|{trimmed}",
                $"[determinism] Unknown random-group key '{trimmed}'. Did you forget to define it in the produce catalog?"
            );
        }
    }

    private void WarnOncePerDay(int day, string key, string message)
    {
        var fullKey = $"{day}|{key}";
        if (!_determinismWarnings.Add(fullKey))
            return;

        _monitor.Log(message, LogLevel.Warn);
    }
#endif

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
    }

#if RCTRL_DEV
    internal string DebugDumpChosenProduce(
        RoosterType roosterType,
        int? dayOverride = null,
        string? locationNameOverride = null,
        string? buildingTypeOverride = null
    )
    {
        if (!Roosters.TryGetValue(roosterType, out var rooster))
            return $"[ra_debug_produce] Unknown rooster type: {roosterType}";

        var farmId = 0UL;
        var day = 0;
        var locationName = "not-ready";

        if (Context.IsWorldReady)
        {
            farmId = Game1.uniqueIDForThisGame;
            day = Game1.Date.TotalDays;
            locationName = Game1.getFarm()?.NameOrUniqueName ?? "Farm";
        }

        if (dayOverride is not null)
            day = dayOverride.Value;

        if (!string.IsNullOrWhiteSpace(locationNameOverride))
            locationName = locationNameOverride!;

        var buildingType = !string.IsNullOrWhiteSpace(buildingTypeOverride)
            ? buildingTypeOverride!
            : rooster.BuildingRequired.ToString();

        var seedCtx = new RoosterSeedKey.Context(
            FarmId: farmId,
            Day: day,
            LocationName: locationName,
            BuildingType: buildingType,
            FarmAnimalName: rooster.FarmAnimalName ?? string.Empty,
            RoosterType: rooster.RoosterType
        );

        var produceSeed = RoosterSeedKey.ForChosenProduce(seedCtx, ProduceTypeEntries.Produce);
        var deluxeSeed = RoosterSeedKey.ForChosenProduce(seedCtx, ProduceTypeEntries.Deluxe);

        var rngProduce = new SeededRandomProvider(produceSeed);
        var rngDeluxe = new SeededRandomProvider(deluxeSeed);

        var chosenProduce = _weighting.RollChosenProduce(rooster.AllProduces[ProduceTypeEntries.Produce], rngProduce);
        var chosenDeluxe = _weighting.RollChosenProduce(rooster.AllProduces[ProduceTypeEntries.Deluxe], rngDeluxe);

        chosenProduce.TryGetValue(RoosterItemDataEntries.Name, out var chosenProduceName);
        chosenProduce.TryGetValue(RoosterItemDataEntries.Probability, out var chosenProduceWeight);

        chosenDeluxe.TryGetValue(RoosterItemDataEntries.Name, out var chosenDeluxeName);
        chosenDeluxe.TryGetValue(RoosterItemDataEntries.Probability, out var chosenDeluxeWeight);

        var sb = new StringBuilder();
        sb.AppendLine("[ra_debug_produce] Inputs:");
        sb.AppendLine($"  roosterType: {roosterType}");
        sb.AppendLine($"  farmAnimalName: {seedCtx.FarmAnimalName}");
        sb.AppendLine($"  farmId: {seedCtx.FarmId}");
        sb.AppendLine($"  day: {seedCtx.Day}");
        sb.AppendLine($"  locationName: {seedCtx.LocationName}");
        sb.AppendLine($"  buildingType: {seedCtx.BuildingType}");
        sb.AppendLine("[ra_debug_produce] Seeds:");
        sb.AppendLine($"  produceSeed: {produceSeed}");
        sb.AppendLine($"  deluxeSeed: {deluxeSeed}");
        sb.AppendLine("[ra_debug_produce] Results:");
        sb.AppendLine($"  chosenProduce: {chosenProduceName} (weight {chosenProduceWeight})");
        sb.AppendLine($"  chosenDeluxe: {chosenDeluxeName} (weight {chosenDeluxeWeight})");
        return sb.ToString().TrimEnd();
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
        var cockTypes = new[]
        {
            RoosterKeys.White,
            RoosterKeys.Void,
            RoosterKeys.Blue,
            RoosterKeys.Gold,
            RoosterKeys.Brown,
        };
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
