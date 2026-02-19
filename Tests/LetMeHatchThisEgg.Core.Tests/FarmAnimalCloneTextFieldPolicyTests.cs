using LetMeHatchThisEgg.Core.Cloning;
using Xunit;

namespace LetMeHatchThisEgg.Core.Tests;

public class FarmAnimalCloneTextFieldPolicyTests
{
    [Fact]
    public void Evaluate_ReturnsAllFieldsInStableOrder()
    {
        var decisions = FarmAnimalCloneTextFieldPolicy.Evaluate(
            current: new FarmAnimalTextFields("a", "b", "c", "d", "e"),
            placeholder: new FarmAnimalTextFields("pa", "pb", "pc", "pd", "pe")
        );

        Assert.Collection(
            decisions,
            d => Assert.Equal(FarmAnimalTextField.DisplayName, d.Field),
            d => Assert.Equal(FarmAnimalTextField.ShopDisplayName, d.Field),
            d => Assert.Equal(FarmAnimalTextField.ShopDescription, d.Field),
            d => Assert.Equal(FarmAnimalTextField.ShopMissingBuildingDescription, d.Field),
            d => Assert.Equal(FarmAnimalTextField.BirthText, d.Field)
        );
    }

    [Fact]
    public void Evaluate_WhenCurrentEqualsPlaceholder_UsesOriginal()
    {
        var decisions = FarmAnimalCloneTextFieldPolicy.Evaluate(
            current: new FarmAnimalTextFields("X", "X", "X", "X", "X"),
            placeholder: new FarmAnimalTextFields("X", "X", "X", "X", "X")
        );

        foreach (var d in decisions)
        {
            Assert.True(d.Decision.UseOriginal);
            Assert.Null(d.Decision.I18nKey);
        }
    }

    [Fact]
    public void Evaluate_WhenI18nTagPresent_ExtractsKeyAndTrimsSpaces()
    {
        var current = new FarmAnimalTextFields(
            DisplayName: "{{i18n: animals.cow.display_name }}",
            ShopDisplayName: "{{i18n:animals.cow.shop_name}}",
            ShopDescription: "{{i18n: animals.cow.desc}}",
            ShopMissingBuildingDescription: "{{i18n: animals.cow.missing_building}}",
            BirthText: "{{i18n: animals.cow.birth_text}}"
        );

        var placeholder = new FarmAnimalTextFields("P", "P", "P", "P", "P");
        var decisions = FarmAnimalCloneTextFieldPolicy.Evaluate(current, placeholder);

        Assert.Equal("animals.cow.display_name", decisions[0].Decision.I18nKey);
        Assert.Equal("animals.cow.shop_name", decisions[1].Decision.I18nKey);
        Assert.Equal("animals.cow.desc", decisions[2].Decision.I18nKey);
        Assert.Equal("animals.cow.missing_building", decisions[3].Decision.I18nKey);
        Assert.Equal("animals.cow.birth_text", decisions[4].Decision.I18nKey);

        foreach (var d in decisions)
        {
            Assert.False(d.Decision.UseOriginal);
        }
    }

    [Fact]
    public void Evaluate_WhenNoI18nTagAndNotPlaceholder_DoesNotUseOriginal()
    {
        var decisions = FarmAnimalCloneTextFieldPolicy.Evaluate(
            current: new FarmAnimalTextFields("Real", "Real", "Real", "Real", "Real"),
            placeholder: new FarmAnimalTextFields("P", "P", "P", "P", "P")
        );

        foreach (var d in decisions)
        {
            Assert.False(d.Decision.UseOriginal);
            Assert.Null(d.Decision.I18nKey);
        }
    }
}
