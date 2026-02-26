using System.Collections.Generic;
using LetMeHatch.Core.ClonePipeline;
using LetMeHatch.Core.Cloning;
using static LetMeHatch.EnumsAndStrings.Enums;
using Xunit;

namespace LetMeHatch.Core.Tests;

public sealed class ClonePlanBuilderTests
{
    [Fact]
    public void Build_IsDeterministic_AndUsesCloneIdService()
    {
        var builder = new ClonePlanBuilder();

        var animals = new Dictionary<string, FarmAnimalProduceLists>
        {
            ["Chicken"] = new FarmAnimalProduceLists(
                ProduceItemIds: new[] { "176" },
                DeluxeProduceItemIds: new[] { "174" }
            )
        };

        var plan = builder.Build(new ClonePlanBuildRequest("Mikew.letmehatch", animals));

        Assert.Equal(2, plan.Count);

        var expectedNormal = CloneIdService.GetClonedItemId("Mikew.letmehatch", "Chicken", "176", CloneItemType.Egg, ItemType.Normal, false);
        var expectedLarge = CloneIdService.GetClonedItemId("Mikew.letmehatch", "Chicken", "174", CloneItemType.Egg, ItemType.Large, false);

        Assert.Contains(plan, s => s.CloneItemId == expectedNormal);
        Assert.Contains(plan, s => s.CloneItemId == expectedLarge);
    }
}
