using LetMeHatchThisEgg.Core.Cloning;
using static LetMeHatchThisEgg.EnumsAndStrings.Enums;

namespace LetMeHatchThisEgg.Application.UseCases;

/// <summary>
/// Application-level façade for clone-id operations.
/// Keeps call sites consistent as Core/Infrastructure boundaries evolve.
/// </summary>
public sealed class CloneIdUseCase
{
    public bool IsClonedEgg(string itemId) => CloneIdService.IsClonedEgg(itemId);

    public bool IsClonedItem(string itemId, CloneItemType? type = null, bool fertilized = false)
        => CloneIdService.IsClonedItem(itemId, type, fertilized);

    public string GetClonedItemId(
        string modUniqueId,
        string animalKey,
        string originalItemId,
        CloneItemType type,
        ItemType itemType = ItemType.Normal,
        bool fertilized = false
    ) => CloneIdService.GetClonedItemId(
        modUniqueId: modUniqueId,
        animalKey: animalKey,
        originalItemId: originalItemId,
        cloneItemType: type,
        itemType: itemType,
        isFertilized: fertilized
    );

    public bool TryParseCloneId(string itemId, out CloneIdParts parts)
        => CloneIdService.TryParseCloneId(itemId, out parts);
}
