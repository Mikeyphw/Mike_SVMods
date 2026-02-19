using System.Collections.Generic;
using LetMeHatchThisEgg.Core.Placeholders;

namespace LetMeHatchThisEgg.Core.Cloning;

/// <summary>
/// Central policy for placeholder-aware <c>FarmAnimalData</c> text fields.
/// This policy is pure (no SMAPI/ContentPatcher): it only decides <em>what to do</em>.
/// Translation/token evaluation stays in Infrastructure.
/// </summary>
public static class FarmAnimalCloneTextFieldPolicy
{
    /// <summary>Evaluate placeholder decisions for all supported text fields.</summary>
    public static IReadOnlyList<FarmAnimalTextFieldDecision> Evaluate(
        FarmAnimalTextFields current,
        FarmAnimalTextFields placeholder
    )
    {
        return new[]
        {
            new FarmAnimalTextFieldDecision(
                FarmAnimalTextField.DisplayName,
                PlaceholderStringPolicy.Decide(current.DisplayName, placeholder.DisplayName)
            ),
            new FarmAnimalTextFieldDecision(
                FarmAnimalTextField.ShopDisplayName,
                PlaceholderStringPolicy.Decide(current.ShopDisplayName, placeholder.ShopDisplayName)
            ),
            new FarmAnimalTextFieldDecision(
                FarmAnimalTextField.ShopDescription,
                PlaceholderStringPolicy.Decide(current.ShopDescription, placeholder.ShopDescription)
            ),
            new FarmAnimalTextFieldDecision(
                FarmAnimalTextField.ShopMissingBuildingDescription,
                PlaceholderStringPolicy.Decide(
                    current.ShopMissingBuildingDescription,
                    placeholder.ShopMissingBuildingDescription
                )
            ),
            new FarmAnimalTextFieldDecision(
                FarmAnimalTextField.BirthText,
                PlaceholderStringPolicy.Decide(current.BirthText, placeholder.BirthText)
            )
        };
    }
}

/// <summary>Farm animal text fields that participate in placeholder/i18n rules.</summary>
public enum FarmAnimalTextField
{
    DisplayName,
    ShopDisplayName,
    ShopDescription,
    ShopMissingBuildingDescription,
    BirthText
}

/// <summary>Decision for a single text field.</summary>
public readonly record struct FarmAnimalTextFieldDecision(
    FarmAnimalTextField Field,
    PlaceholderStringDecision Decision
);

/// <summary>Values for all policy-managed text fields.</summary>
public readonly record struct FarmAnimalTextFields(
    string? DisplayName,
    string? ShopDisplayName,
    string? ShopDescription,
    string? ShopMissingBuildingDescription,
    string? BirthText
);
