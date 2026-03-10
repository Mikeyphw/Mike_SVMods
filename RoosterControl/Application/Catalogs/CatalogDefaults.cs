using System;
using System.Collections.Generic;
using RoosterControl.Constants;
using RoosterControl.Framework.Config;

namespace RoosterControl.Application.Catalogs;

public static class CatalogDefaults
{
    public static CatalogBundle CreateDefaults()
        => new()
        {
            Roosters = CreateRoosterCatalogDefaults(),
            Produce = CreateProduceCatalogDefaults(),
            Hatching = CreateHatchingCatalogDefaults(),
        };

    public static RoosterCatalog CreateRoosterCatalogDefaults()
        => new()
        {
            SchemaVersion = 1,
            Roosters = new Dictionary<string, RoosterCatalogEntry>(StringComparer.OrdinalIgnoreCase)
            {
                ["White"] = new RoosterCatalogEntry
                {
                    FarmAnimalKey = RoosterKeys.White,
                    Eggs = new EggCatalog
                    {
                        Normal = ItemIds.AnimalProducts.WhiteEgg,
                        Large = ItemIds.AnimalProducts.LargeWhiteEgg,
                        Fertilized = ItemIds.CustomItems.FertilizedWhiteEgg
                    }
                },
                ["Brown"] = new RoosterCatalogEntry
                {
                    FarmAnimalKey = RoosterKeys.Brown,
                    Eggs = new EggCatalog
                    {
                        Normal = ItemIds.AnimalProducts.BrownEgg,
                        Large = ItemIds.AnimalProducts.LargeBrownEgg,
                        Fertilized = ItemIds.CustomItems.FertilizedBrownEgg
                    }
                },
                ["Blue"] = new RoosterCatalogEntry
                {
                    FarmAnimalKey = RoosterKeys.Blue,
                    Eggs = new EggCatalog
                    {
                        Normal = ItemIds.CustomItems.MearaBlueEgg,
                        Large = ItemIds.CustomItems.MearaLargeBlueEgg,
                        Fertilized = ItemIds.CustomItems.FertilizedBlueEgg
                    }
                },
                ["Void"] = new RoosterCatalogEntry
                {
                    FarmAnimalKey = RoosterKeys.Void,
                    Eggs = new EggCatalog
                    {
                        Normal = ItemIds.AnimalProducts.VoidEgg,
                        Large = ItemIds.CustomItems.MearaLargeVoidEgg,
                        Fertilized = ItemIds.CustomItems.FertilizedVoidEgg
                    }
                },
                ["Gold"] = new RoosterCatalogEntry
                {
                    FarmAnimalKey = RoosterKeys.Gold,
                    Eggs = new EggCatalog
                    {
                        Normal = ItemIds.AnimalProducts.GoldenEgg,
                        Large = ItemIds.CustomItems.MearaLargeGoldEgg,
                        Fertilized = ItemIds.CustomItems.FertilizedGoldEgg
                    }
                }
            }
        };

    public static ProduceCatalog CreateProduceCatalogDefaults()
    {
        static ProduceCatalogEntry Make(string produce, string deluxe)
            => new()
            {
                Produce = new List<ProduceEntryDto>
                {
                    new() { Choice = produce, CustomItemName = ItemIds.CustomItems.Custom, Probability = 100 }
                },
                Deluxe = new List<ProduceEntryDto>
                {
                    new() { Choice = deluxe, CustomItemName = ItemIds.CustomItems.Custom, Probability = 100 }
                }
            };

        return new ProduceCatalog
        {
            SchemaVersion = 1,
            RandomGroups = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                [ItemIds.RandomProduces.RandomColorEgg] = new()
                {
                    ItemIds.AnimalProducts.BrownEgg,
                    ItemIds.AnimalProducts.WhiteEgg
                },
                [ItemIds.RandomProduces.RandomColorLargeEgg] = new()
                {
                    ItemIds.AnimalProducts.LargeBrownEgg,
                    ItemIds.AnimalProducts.LargeWhiteEgg
                },
                [ItemIds.RandomProduces.RandomSizeWhiteEgg] = new()
                {
                    ItemIds.AnimalProducts.LargeWhiteEgg,
                    ItemIds.AnimalProducts.WhiteEgg
                },
                [ItemIds.RandomProduces.RandomSizeBrownEgg] = new()
                {
                    ItemIds.AnimalProducts.BrownEgg,
                    ItemIds.AnimalProducts.LargeBrownEgg
                },
                [ItemIds.RandomProduces.RandomColorSizeEgg] = new()
                {
                    ItemIds.AnimalProducts.BrownEgg,
                    ItemIds.AnimalProducts.WhiteEgg,
                    ItemIds.AnimalProducts.LargeBrownEgg,
                    ItemIds.AnimalProducts.LargeWhiteEgg
                },
                [ItemIds.RandomProduces.RandomSizeBlueEgg] = new()
                {
                    ItemIds.CustomItems.MearaBlueEgg,
                    ItemIds.CustomItems.MearaLargeBlueEgg
                },
                [ItemIds.RandomProduces.RandomSizeVoidEgg] = new()
                {
                    ItemIds.CustomItems.MearaLargeVoidEgg,
                    ItemIds.AnimalProducts.VoidEgg
                },
                [ItemIds.RandomProduces.RandomSizeGoldenEgg] = new()
                {
                    ItemIds.CustomItems.MearaLargeGoldEgg,
                    ItemIds.AnimalProducts.GoldenEgg
                },
                [ItemIds.RandomProduces.RandomGems] = new()
                {
                    ItemIds.Gemstones.Amethyst,
                    ItemIds.Gemstones.Aquamarine,
                    ItemIds.Gemstones.Emerald,
                    ItemIds.Gemstones.Jade,
                    ItemIds.Gemstones.Ruby,
                    ItemIds.Gemstones.Topaz
                },
                [ItemIds.RandomProduces.RandomMinerals] = new()
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
            },
            Roosters = new Dictionary<string, ProduceCatalogEntry>(StringComparer.OrdinalIgnoreCase)
            {
                ["White"] = Make(ItemIds.AnimalProducts.WhiteEgg, ItemIds.AnimalProducts.LargeWhiteEgg),
                ["Brown"] = Make(ItemIds.AnimalProducts.BrownEgg, ItemIds.AnimalProducts.LargeBrownEgg),
                ["Blue"] = new ProduceCatalogEntry
                {
                    BlueEggChoice = ItemIds.CustomItems.MearaBlueEgg,
                    Produce = new List<ProduceEntryDto>
                    {
                        new() { Choice = ItemIds.CustomItems.MearaBlueEgg, CustomItemName = ItemIds.CustomItems.Custom, Probability = 100 }
                    },
                    Deluxe = new List<ProduceEntryDto>
                    {
                        new() { Choice = ItemIds.CustomItems.MearaLargeBlueEgg, CustomItemName = ItemIds.CustomItems.Custom, Probability = 100 }
                    }
                },
                ["Void"] = Make(ItemIds.AnimalProducts.VoidEgg, ItemIds.CustomItems.MearaLargeVoidEgg),
                ["Gold"] = Make(ItemIds.AnimalProducts.GoldenEgg, ItemIds.CustomItems.MearaLargeGoldEgg),
            }
        };
    }

    public static HatchingCatalog CreateHatchingCatalogDefaults()
    {
        var cfg = RoosterControlConfig.CreateDefault();

        var roosters = new Dictionary<string, HatchingRoosterCatalog>(StringComparer.OrdinalIgnoreCase);
        foreach (var (rt, settings) in cfg.Roosters)
        {
            roosters[rt.ToString()] = new HatchingRoosterCatalog
            {
                BuildingRequired = settings.BuildingRequired.ToString(),
                Buyable = settings.Buyable,
                Price = settings.Price,
                EnableRoosterHatching = settings.EnableRoosterHatching,
                EditAllFemales = settings.EditAllFemales,
                MatureDays = settings.MatureDays,
                ProduceDays = settings.ProduceDays,
                ProduceOption = settings.ProduceOption.ToString(),
                DeluxeProduceOption = settings.DeluxeProduceOption.ToString(),
                BypassBlueConditions = settings.BypassBlueConditions,
            };
        }

        return new HatchingCatalog
        {
            SchemaVersion = 1,
            General = new HatchingGeneralCatalog
            {
                UpdateRate = cfg.General.UpdateRate.ToString(),
                ChosenPercentageDailyLuckBonus = cfg.General.ChosenPercentageDailyLuckBonus,
                HarvestType = cfg.General.HarvestType.ToString(),
                HatchingMode = cfg.General.HatchingMode.ToString(),
                BetterTrufflesCompat = cfg.General.BetterTrufflesCompat,
                EnableStuckInside = cfg.General.EnableStuckInside,
                EnableEggProbability = cfg.General.EnableEggProbability,
                EnableLargeEggProbability = cfg.General.EnableLargeEggProbability,
                EnableDebug = cfg.General.EnableDebug,
                EggProbability = cfg.General.EggProbability,
                LargeEggProbability = cfg.General.LargeEggProbability,
                FertilizedEggProbability = cfg.General.FertilizedEggProbability,
                RoosterToChickenRatio = cfg.General.RoosterToChickenRatio,
                BoostByRooster = cfg.General.BoostByRooster,
            },
            Roosters = roosters
        };
    }
}
