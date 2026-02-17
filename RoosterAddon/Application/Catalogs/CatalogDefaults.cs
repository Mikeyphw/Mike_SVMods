using System;
using System.Collections.Generic;
using RoosterAddon.Constants;
using RoosterAddon.Enums;
using RoosterAddon.Framework.Config;

namespace RoosterAddon.Application.Catalogs;

public static class CatalogDefaults
{
    public static CatalogBundle CreateDefaults()
    {
        var bundle = new CatalogBundle
        {
            Roosters = CreateRoosterCatalogDefaults(),
            Produce = CreateProduceCatalogDefaults(),
            Hatching = CreateHatchingCatalogDefaults(),
        };

        return bundle;
    }

    public static RoosterCatalog CreateRoosterCatalogDefaults()
    {
        return new RoosterCatalog
        {
            SchemaVersion = 1,
            Roosters = new Dictionary<string, RoosterCatalogEntry>(StringComparer.OrdinalIgnoreCase)
            {
                ["White"] = new RoosterCatalogEntry
                {
                    FarmAnimalKey = RoosterKeys.White,
                    DefaultFemaleKey = "White Chicken",
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
                    DefaultFemaleKey = "Brown Chicken",
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
                    DefaultFemaleKey = "Blue Chicken",
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
                    DefaultFemaleKey = "Void Chicken",
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
                    DefaultFemaleKey = "Golden Chicken",
                    Eggs = new EggCatalog
                    {
                        Normal = ItemIds.AnimalProducts.GoldenEgg,
                        Large = ItemIds.CustomItems.MearaLargeGoldEgg,
                        Fertilized = ItemIds.CustomItems.FertilizedGoldEgg
                    }
                }
            }
        };
    }

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
        var cfg = RoosterAddonConfig.CreateDefault();

        var roosters = new Dictionary<string, HatchingRoosterCatalog>(StringComparer.OrdinalIgnoreCase);
        foreach (var (rt, settings) in cfg.Roosters)
        {
            roosters[rt.ToString()] = new HatchingRoosterCatalog
            {
                BuildingRequired = settings.BuildingRequired.ToString(),
                Buyable = settings.Buyable,
                Price = settings.Price,
                EnableRoosterHatching = settings.EnableRoosterHatching,
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
                HarvestType = cfg.General.HarvestType.ToString(),
                HatchingMode = cfg.General.HatchingMode.ToString()
            },
            Roosters = roosters
        };
    }
}
