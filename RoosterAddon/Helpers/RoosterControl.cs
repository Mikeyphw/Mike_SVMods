using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Mikew.BirdsOfAFeather.Framework.Random;
using RoosterAddon.Constants;
using RoosterAddon.Enums;
using RoosterAddon.Framework.Config;
using RoosterAddon.Application.FarmAnimals;
using RoosterAddon.Application.Catalogs;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.GameData.FarmAnimals;

using LuckyTypes = Mikew.BirdsOfAFeather.EnumsAndStrings.Enums.LuckyTypes;

namespace RoosterAddon.Helpers;

/// <summary>
/// Runtime rooster model builder and utilities.
/// </summary>
public sealed class RoosterControl
{
    private static readonly Random Rnd = new();
    private readonly FarmAnimalRatioWeightingService _weighting = new();
    private readonly IModHelper _helper;
    private readonly IMonitor _monitor;

    public RoosterAddonConfig Config { get; private set; }
    public LuckyTypes DailyLuck { private get; set; } = LuckyTypes.SuperUnlucky;

    public Dictionary<RoosterType, RoosterData> Roosters { get; private set; } = new();
    public Dictionary<string, Dictionary<EggTypeEntries, string>> FemaleEggs { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    public RoosterControl(RoosterAddonConfig config, IModHelper helper, IMonitor monitor, RoosterCatalog? roosterCatalog = null)
    {
        Config = config ?? throw new ArgumentNullException(nameof(config));
        _helper = helper ?? throw new ArgumentNullException(nameof(helper));
        _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        _roosterCatalog = roosterCatalog;
    }

    public void UpdateConfig(RoosterAddonConfig config)
        => Config = config ?? throw new ArgumentNullException(nameof(config));

    public static bool UpdatePercentage(int chance)
    {
        if (chance <= 0)
            return false;
        if (chance >= 100)
            return true;

        return Rnd.Next(100) < chance;
    }

    public static bool UpdatePercentage(float chance)
    {
        if (chance <= 0)
            return false;
        if (chance >= 100)
            return true;

        return Rnd.NextDouble() * 100.0 < chance;
    }

    public void SetUpRoosters()
    {
        EnsureRoosterDefaults(Config);

        var normalEggProbability = ApplyLuckAdjustments(Config.General.EggProbability);
        var largeEggProbability = ApplyLuckAdjustments(Config.General.LargeEggProbability);
        var fertilizedEggProbability = Config.General.HatchingMode == HatchingMode.OnlyFertilizedEggs
            ? ApplyLuckAdjustments(Config.General.FertilizedEggProbability)
            : 0;

        var eggsByType = BuildEggs(normalEggProbability, largeEggProbability, fertilizedEggProbability);

        var roosterMap = new Dictionary<RoosterType, RoosterData>();
        foreach (var roosterType in RoosterAddonConfig.CreateDefaultRoosters().Keys)
        {
            var settings = Config.Roosters[roosterType];

            var farmAnimalName = GetFarmAnimalKey(roosterType);
            var femaleKey = GetDefaultFemaleKey(roosterType);

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
                Eggs = eggsByType[roosterType],
                AllProduces = BuildProduces(settings),
                Quantity = UpdateRoosterQuantity(farmAnimalName),
                Females = GetAdditionalFemaleKeys(roosterType),
            };

            RollChosenProduces(rooster);
            ApplyFertilizedBoost(rooster);

            roosterMap[roosterType] = rooster;
        }

        Roosters = roosterMap;
    }

    private static void EnsureRoosterDefaults(RoosterAddonConfig cfg)
    {
        cfg.Roosters ??= new Dictionary<RoosterType, RoosterSettings>();
        foreach (var (key, def) in RoosterAddonConfig.CreateDefaultRoosters())
            cfg.Roosters.TryAdd(key, def);
    }

    private Dictionary<RoosterType, Dictionary<EggTypeEntries, Dictionary<RoosterItemDataEntries, string>>> BuildEggs(
        int normalEggProbability,
        int largeEggProbability,
        int fertilizedEggProbability
    )
    {
        var eggProbabilities = new[] { normalEggProbability, largeEggProbability, fertilizedEggProbability };

        var eggIds = new Dictionary<RoosterType, string[]>
        {
            [RoosterType.White] = new[]
            {
                ItemIds.AnimalProducts.WhiteEgg,
                ItemIds.AnimalProducts.LargeWhiteEgg,
                ItemIds.CustomItems.FertilizedWhiteEgg,
            },
            [RoosterType.Brown] = new[]
            {
                ItemIds.AnimalProducts.BrownEgg,
                ItemIds.AnimalProducts.LargeBrownEgg,
                ItemIds.CustomItems.FertilizedBrownEgg,
            },
            [RoosterType.Blue] = new[]
            {
                ItemIds.CustomItems.MearaBlueEgg,
                ItemIds.CustomItems.MearaLargeBlueEgg,
                ItemIds.CustomItems.FertilizedBlueEgg,
            },
            [RoosterType.Void] = new[]
            {
                ItemIds.AnimalProducts.VoidEgg,
                ItemIds.CustomItems.MearaLargeVoidEgg,
                ItemIds.CustomItems.FertilizedVoidEgg,
            },
            [RoosterType.Gold] = new[]
            {
                ItemIds.AnimalProducts.GoldenEgg,
                ItemIds.CustomItems.MearaLargeGoldEgg,
                ItemIds.CustomItems.FertilizedGoldEgg,
            },
        };
if (_roosterCatalog?.Roosters is not null)
{
    foreach (var (key, entry) in _roosterCatalog.Roosters)
    {
        if (!Enum.TryParse<RoosterType>(key, ignoreCase: true, out var rt))
            continue;

        if (entry?.Eggs is null)
            continue;

        var normal = string.IsNullOrWhiteSpace(entry.Eggs.Normal) ? null : entry.Eggs.Normal.Trim();
        var large = string.IsNullOrWhiteSpace(entry.Eggs.Large) ? null : entry.Eggs.Large.Trim();
        var fert = string.IsNullOrWhiteSpace(entry.Eggs.Fertilized) ? null : entry.Eggs.Fertilized.Trim();

        if (normal is not null && large is not null && fert is not null)
            eggIds[rt] = new[] { normal, large, fert };
    }
}


        static string TranslateEggId(string eggKey)
    => ItemIds.VariableValues.TryGetValue(eggKey, out var translated) ? translated : eggKey;

var result = new Dictionary<RoosterType, Dictionary<EggTypeEntries, Dictionary<RoosterItemDataEntries, string>>>();
        foreach (var roosterType in eggIds.Keys)
        {
            var settings = Config.Roosters[roosterType];
            var enableHatching = settings.EnableRoosterHatching;

            var probs = (int[])eggProbabilities.Clone();
            if (Config.General.HatchingMode == HatchingMode.OnlyFertilizedEggs)
            {
                probs[0] = 0;
                probs[1] = 0;
            }

            var eggs = eggIds[roosterType];
            var eggsDict = new Dictionary<EggTypeEntries, Dictionary<RoosterItemDataEntries, string>>();
            for (var i = 0; i < eggs.Length; i++)
            {
                eggsDict[(EggTypeEntries)i] = new Dictionary<RoosterItemDataEntries, string>
                {
                    [RoosterItemDataEntries.Name] = eggs[i],
                    [RoosterItemDataEntries.Probability] = enableHatching ? probs[i].ToString(CultureInfo.InvariantCulture) : "0",
                };
            }

            result[roosterType] = eggsDict;
        }

        return result;
    }

    private static Dictionary<ProduceTypeEntries, Dictionary<ProduceEntries, Dictionary<RoosterItemDataEntries, string>>> BuildProduces(RoosterSettings settings)
    {
        var produce = BuildProduceEntries(settings.ProduceEntries);
        var deluxe = BuildProduceEntries(settings.DeluxeProduceEntries);

        return new Dictionary<ProduceTypeEntries, Dictionary<ProduceEntries, Dictionary<RoosterItemDataEntries, string>>>
        {
            [ProduceTypeEntries.Produce] = produce,
            [ProduceTypeEntries.Deluxe] = deluxe,
        };
    }

    private static Dictionary<ProduceEntries, Dictionary<RoosterItemDataEntries, string>> BuildProduceEntries(List<ProduceEntry> entries)
    {
        entries ??= ProduceEntry.CreateDefaultFour();
        while (entries.Count < 4)
            entries.Add(new ProduceEntry());

        var mapped = new Dictionary<ProduceEntries, Dictionary<RoosterItemDataEntries, string>>
        {
            [ProduceEntries.First] = MapProduceEntry(entries[0]),
            [ProduceEntries.Second] = MapProduceEntry(entries[1]),
            [ProduceEntries.Third] = MapProduceEntry(entries[2]),
            [ProduceEntries.Fourth] = MapProduceEntry(entries[3]),
            [ProduceEntries.Chosen] = new Dictionary<RoosterItemDataEntries, string>(),
        };

        return mapped;
    }

    private static Dictionary<RoosterItemDataEntries, string> MapProduceEntry(ProduceEntry entry)
    {
        entry ??= new ProduceEntry();

        var choice = entry.Choice ?? string.Empty;
        var name = choice.Equals(ItemIds.CustomItems.Custom, StringComparison.OrdinalIgnoreCase)
            ? (entry.CustomItemName ?? ItemIds.CustomItems.Custom)
            : ProduceTranslation(choice);

        return new Dictionary<RoosterItemDataEntries, string>
        {
            [RoosterItemDataEntries.Name] = name,
            [RoosterItemDataEntries.Probability] = Math.Clamp(entry.Probability, 0, 100).ToString(CultureInfo.InvariantCulture),
        };
    }

    private void RollChosenProduces(RoosterData rooster)
    {
        var rng = new SystemRandomProvider(Rnd);

        rooster.AllProduces[ProduceTypeEntries.Produce][ProduceEntries.Chosen] =
            _weighting.RollChosenProduce(rooster.AllProduces[ProduceTypeEntries.Produce], rng);

        rooster.AllProduces[ProduceTypeEntries.Deluxe][ProduceEntries.Chosen] =
            _weighting.RollChosenProduce(rooster.AllProduces[ProduceTypeEntries.Deluxe], rng);
    }

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

        var bonus = Math.Clamp(Config.General.ChosenPercentageDailyLuckBonus, 0, 100);

        var adjustment = DailyLuck switch
        {
            LuckyTypes.SuperUnlucky => -2 * bonus,
            LuckyTypes.Unlucky => -bonus,
            LuckyTypes.Neutral => 0,
            LuckyTypes.Lucky => bonus,
            LuckyTypes.SuperLucky => (int)Math.Round(1.5 * bonus, MidpointRounding.AwayFromZero),
            _ => 0,
        };

        return Math.Clamp(originalPercentage + adjustment, 0, 100);
    }
    internal static string ProduceTranslation(string produceName)
        {
            string GetNewName(string[] namesList)
            {
                int index = Rnd.Next(namesList.Length);

                return ProduceTranslation(namesList[index]);
            }
            if (produceName.Contains("Random"))
            {
                return produceName switch
                {
                    ItemIds.RandomProduces.RandomColorEgg
                        => GetNewName(
                            new string[]
                            {
                                ItemIds.AnimalProducts.BrownEgg,
                                ItemIds.AnimalProducts.WhiteEgg
                            }
                        ),
                    ItemIds.RandomProduces.RandomColorLargeEgg
                        => GetNewName(
                            new string[]
                            {
                                ItemIds.AnimalProducts.LargeBrownEgg,
                                ItemIds.AnimalProducts.LargeWhiteEgg
                            }
                        ),
                    ItemIds.RandomProduces.RandomSizeWhiteEgg
                        => GetNewName(
                            new string[]
                            {
                                ItemIds.AnimalProducts.LargeWhiteEgg,
                                ItemIds.AnimalProducts.WhiteEgg
                            }
                        ),
                    ItemIds.RandomProduces.RandomSizeBrownEgg
                        => GetNewName(
                            new string[]
                            {
                                ItemIds.AnimalProducts.BrownEgg,
                                ItemIds.AnimalProducts.LargeBrownEgg
                            }
                        ),
                    ItemIds.RandomProduces.RandomColorSizeEgg
                        => GetNewName(
                            new string[]
                            {
                                ItemIds.AnimalProducts.BrownEgg,
                                ItemIds.AnimalProducts.WhiteEgg,
                                ItemIds.AnimalProducts.LargeBrownEgg,
                                ItemIds.AnimalProducts.LargeWhiteEgg
                            }
                        ),
                    ItemIds.RandomProduces.RandomSizeBlueEgg
                        => GetNewName(
                            new string[]
                            {
                                ItemIds.CustomItems.MearaBlueEgg,
                                ItemIds.CustomItems.MearaLargeBlueEgg,
                            }
                        ),
                    ItemIds.RandomProduces.RandomSizeVoidEgg
                        => GetNewName(
                            new string[]
                            {
                                ItemIds.CustomItems.MearaLargeVoidEgg,
                                ItemIds.AnimalProducts.VoidEgg,
                            }
                        ),
                    ItemIds.RandomProduces.RandomSizeGoldenEgg
                        => GetNewName(
                            new string[]
                            {
                                ItemIds.CustomItems.MearaLargeGoldEgg,
                                ItemIds.AnimalProducts.GoldenEgg,
                            }
                        ),
                    ItemIds.RandomProduces.RandomGems
                        => GetNewName(
                            new string[]
                            {
                                ItemIds.Gemstones.Amethyst,
                                ItemIds.Gemstones.Aquamarine,
                                ItemIds.Gemstones.Emerald,
                                ItemIds.Gemstones.Jade,
                                ItemIds.Gemstones.Ruby,
                                ItemIds.Gemstones.Topaz
                            }
                        ),
                    ItemIds.RandomProduces.RandomMinerals
                        => GetNewName(
                            new string[]
                            {
                                ItemIds.Minerals.Aerinite,
                                ItemIds.Minerals.Alamite,
                                ItemIds.Minerals.Baryte,
                                ItemIds.Minerals.Basalt,
                                ItemIds.Minerals.Bixite,
                                ItemIds.Minerals.Calcite,
                                ItemIds.Minerals.Celestine,
                                ItemIds.Minerals.Dolomite,
                                ItemIds.Minerals.EarthCrystal,
                                ItemIds.Minerals.Esperite,
                                ItemIds.Minerals.FairyStone,
                                ItemIds.Minerals.FireOpal,
                                ItemIds.Minerals.FireQuartz,
                                ItemIds.Minerals.FrozenTear,
                                ItemIds.Minerals.Geminite,
                                ItemIds.Minerals.GhostCrystal,
                                ItemIds.Minerals.Granite,
                                ItemIds.Minerals.Helvite,
                                ItemIds.Minerals.Hematite,
                                ItemIds.Minerals.Jagoite,
                                ItemIds.Minerals.Jamborite,
                                ItemIds.Minerals.Kyanite,
                                ItemIds.Minerals.LemonStone,
                                ItemIds.Minerals.Limestone,
                                ItemIds.Minerals.Lunarite,
                                ItemIds.Minerals.Malachite,
                                ItemIds.Minerals.Marble,
                                ItemIds.Minerals.Mudstone,
                                ItemIds.Minerals.Nekoite,
                                ItemIds.Minerals.Neptunite,
                                ItemIds.Minerals.Obsidian,
                                ItemIds.Minerals.OceanStone,
                                ItemIds.Minerals.Opal,
                                ItemIds.Minerals.Orpiment,
                                ItemIds.Minerals.PetrifiedSlime,
                                ItemIds.Minerals.Pyrite,
                                ItemIds.Minerals.Sandstone,
                                ItemIds.Minerals.Slate,
                                ItemIds.Minerals.Soapstone,
                                ItemIds.Minerals.StarShards,
                                ItemIds.Minerals.ThunderEgg,
                                ItemIds.Minerals.TigersEye
                            }
                        ),
                    _ => "",
                    //TODO ADD MISSING RANDOMS
                };
            }
            return ItemIds.VariableValues.TryGetValue(produceName, out var translated) ? translated : produceName;
        }

        if (produceName.Contains("Random", StringComparison.Ordinal))
        {
            return produceName switch
            {
                ItemIds.RandomProduces.RandomColorEgg
                    => PickOne(new[] { ItemIds.AnimalProducts.BrownEgg, ItemIds.AnimalProducts.WhiteEgg }),
                ItemIds.RandomProduces.RandomColorLargeEgg
                    => PickOne(new[] { ItemIds.AnimalProducts.LargeBrownEgg, ItemIds.AnimalProducts.LargeWhiteEgg }),
                ItemIds.RandomProduces.RandomSizeWhiteEgg
                    => PickOne(new[] { ItemIds.AnimalProducts.LargeWhiteEgg, ItemIds.AnimalProducts.WhiteEgg }),
                ItemIds.RandomProduces.RandomSizeBrownEgg
                    => PickOne(new[] { ItemIds.AnimalProducts.BrownEgg, ItemIds.AnimalProducts.LargeBrownEgg }),
                ItemIds.RandomProduces.RandomColorSizeEgg
                    => PickOne(new[] { ItemIds.AnimalProducts.BrownEgg, ItemIds.AnimalProducts.WhiteEgg, ItemIds.AnimalProducts.LargeBrownEgg, ItemIds.AnimalProducts.LargeWhiteEgg }),
                ItemIds.RandomProduces.RandomSizeBlueEgg
                    => PickOne(new[] { ItemIds.CustomItems.MearaBlueEgg, ItemIds.CustomItems.MearaLargeBlueEgg }),
                ItemIds.RandomProduces.RandomSizeVoidEgg
                    => PickOne(new[] { ItemIds.CustomItems.MearaLargeVoidEgg, ItemIds.AnimalProducts.VoidEgg }),
                ItemIds.RandomProduces.RandomSizeGoldenEgg
                    => PickOne(new[] { ItemIds.CustomItems.MearaLargeGoldEgg, ItemIds.AnimalProducts.GoldenEgg }),
                ItemIds.RandomProduces.RandomGems
                    => PickOne(new[] { ItemIds.Gemstones.Amethyst, ItemIds.Gemstones.Aquamarine, ItemIds.Gemstones.Emerald, ItemIds.Gemstones.Jade, ItemIds.Gemstones.Ruby, ItemIds.Gemstones.Topaz }),
                ItemIds.RandomProduces.RandomMinerals
                    => PickOne(new[]
                    {
                        ItemIds.Minerals.Aerinite, ItemIds.Minerals.Alamite, ItemIds.Minerals.Baryte, ItemIds.Minerals.Basalt, ItemIds.Minerals.Bixite, ItemIds.Minerals.Calcite,
                        ItemIds.Minerals.Celestine, ItemIds.Minerals.Dolomite, ItemIds.Minerals.EarthCrystal, ItemIds.Minerals.Esperite, ItemIds.Minerals.FireOpal, ItemIds.Minerals.FireQuartz,
                        ItemIds.Minerals.FrozenTear, ItemIds.Minerals.Geminite, ItemIds.Minerals.GhostCrystal, ItemIds.Minerals.Granite, ItemIds.Minerals.Helvite, ItemIds.Minerals.Hematite,
                        ItemIds.Minerals.Jagoite, ItemIds.Minerals.Jamborite, ItemIds.Minerals.Kyanite, ItemIds.Minerals.LemonStone, ItemIds.Minerals.Limestone, ItemIds.Minerals.Lunarite,
                        ItemIds.Minerals.Malachite,
                    }),
                _ => produceName,
            };
        }

        return ItemIds.VariableValues.TryGetValue(produceName, out var translated) ? translated : produceName;
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

