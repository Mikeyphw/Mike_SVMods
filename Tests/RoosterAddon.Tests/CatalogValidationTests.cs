using System.Collections.Generic;
using System.Linq;
using RoosterAddon.Application.Catalogs;
using RoosterAddon.Constants;
using Xunit;

namespace RoosterAddon.Tests;

public sealed class CatalogValidationTests
{
    [Fact]
    public void Validate_ReportsMissingRequiredFields()
    {
        var bundle = new CatalogBundle
        {
            Roosters = new RoosterCatalog { Roosters = new Dictionary<string, RoosterCatalogEntry>() },
            Produce = new ProduceCatalog { Roosters = new Dictionary<string, ProduceCatalogEntry>() },
            Hatching = new HatchingCatalog
            {
                General = new HatchingGeneralCatalog
                {
                    UpdateRate = null,
                    ChosenPercentageDailyLuckBonus = null,
                    EggProbability = null,
                    LargeEggProbability = null,
                    FertilizedEggProbability = null,
                    RoosterToChickenRatio = null,
                    BoostByRooster = null
                },
                Roosters = new Dictionary<string, HatchingRoosterCatalog>()
            }
        };

        var issues = CatalogValidator.Validate(bundle);

        Assert.Contains(issues, i => i.Path == "Roosters" && i.Catalog == "roosters");
        Assert.Contains(issues, i => i.Path == "Roosters" && i.Catalog == "produce");
        Assert.Contains(issues, i => i.Path == "General.UpdateRate" && i.Catalog == "hatching");
    }

    [Fact]
    public void Sanitize_ClampsProduceProbabilities()
    {
        var bundle = new CatalogBundle
        {
            Roosters = new RoosterCatalog
            {
                Roosters = new Dictionary<string, RoosterCatalogEntry>
                {
                    ["White"] = new RoosterCatalogEntry
                    {
                        FarmAnimalKey = "Chicken",
                        DefaultFemaleKey = "Chicken",
                        Eggs = new EggCatalog { Normal = "176", Large = "174", Fertilized = "174" }
                    }
                }
            },
            Produce = new ProduceCatalog
            {
                RandomGroups = new Dictionary<string, List<string>> { ["Random Gems"] = new() { ItemIds.Gemstones.Jade } },
                Roosters = new Dictionary<string, ProduceCatalogEntry>
                {
                    ["White"] = new ProduceCatalogEntry
                    {
                        Produce = new()
                        {
                            new ProduceEntryDto { Choice = ItemIds.Gemstones.Jade, Probability = -5 },
                            new ProduceEntryDto { Choice = ItemIds.Gemstones.Jade, Probability = 150 }
                        },
                        Deluxe = new()
                    }
                }
            },
            Hatching = new HatchingCatalog
            {
                General = new HatchingGeneralCatalog
                {
                    UpdateRate = "OnDayStart",
                    ChosenPercentageDailyLuckBonus = 0,
                    EggProbability = 0,
                    LargeEggProbability = 0,
                    FertilizedEggProbability = 0,
                    RoosterToChickenRatio = 0,
                    BoostByRooster = 0f
                },
                Roosters = new Dictionary<string, HatchingRoosterCatalog>()
            }
        };

        CatalogSanitizer.Sanitize(bundle);

        var probs = bundle.Produce.Roosters["White"].Produce.Select(p => p.Probability).ToArray();
        Assert.Equal(new[] { 0, 100 }, probs);
    }
}
