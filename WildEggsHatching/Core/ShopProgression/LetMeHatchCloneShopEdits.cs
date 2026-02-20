using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley.GameData.FarmAnimals;
using WildEggsHatching.Application.AnimalNotes;

namespace WildEggsHatching.Core.ShopProgression;

/// <summary>
/// Integrates LetMeHatchThisEgg clone pairs into shop gating:
/// male clone variants inherit the female's AnimalNotes mail unlock.
/// </summary>
public static class LetMeHatchCloneShopEdits
{
    private static readonly StringComparer KeyComparer = StringComparer.OrdinalIgnoreCase;

    public static void ApplyInheritUnlock(
        IDictionary<string, FarmAnimalData> farmAnimals,
        IReadOnlyList<(string FemaleKey, string MaleKey, bool AddAsAlternatePurchase)> pairs)
    {
        if (farmAnimals is null)
            throw new ArgumentNullException(nameof(farmAnimals));
        if (pairs is null || pairs.Count == 0)
            return;

        var maleToFemaleUnlock = BuildMaleToFemaleUnlockCondition(pairs);

        ApplyDirectListingUnlockConditions(farmAnimals, maleToFemaleUnlock);
        ApplyAlternatePurchaseTypeUnlockConditions(farmAnimals, maleToFemaleUnlock);
    }

    private static Dictionary<string, string> BuildMaleToFemaleUnlockCondition(
        IReadOnlyList<(string FemaleKey, string MaleKey, bool AddAsAlternatePurchase)> pairs)
    {
        var map = new Dictionary<string, string>(KeyComparer);

        foreach (var (female, male, _) in pairs)
        {
            if (string.IsNullOrWhiteSpace(female) || string.IsNullOrWhiteSpace(male))
                continue;
            if (female.Equals(male, StringComparison.OrdinalIgnoreCase))
                continue;

            var mailId = AnimalNoteIds.NoteMailId(female);
            var cond = ConditionJoiner.MailReceivedByHost(mailId);
            map[male] = cond;
        }

        return map;
    }

    private static void ApplyDirectListingUnlockConditions(
        IDictionary<string, FarmAnimalData> farmAnimals,
        IReadOnlyDictionary<string, string> maleToFemaleUnlock)
    {
        foreach (var (maleId, femaleCond) in maleToFemaleUnlock)
        {
            if (!farmAnimals.TryGetValue(maleId, out var male) || male is null)
                continue;

            var existing = ShopFieldsAccessor.GetUnlockCondition(male);
            var cleaned = RemoveToken(existing, ConditionJoiner.MailReceivedByHost(AnimalNoteIds.NoteMailId(maleId)));
            var merged = ConditionJoiner.And(cleaned, femaleCond);

            ShopFieldsAccessor.SetUnlockCondition(male, merged);
        }
    }

    private static void ApplyAlternatePurchaseTypeUnlockConditions(
        IDictionary<string, FarmAnimalData> farmAnimals,
        IReadOnlyDictionary<string, string> maleToFemaleUnlock)
    {
        if (maleToFemaleUnlock.Count == 0)
            return;

        var maleIds = new HashSet<string>(maleToFemaleUnlock.Keys, KeyComparer);

        foreach (var parent in farmAnimals.Values.Where(v => v is not null))
        {
            var live = AlternatePurchaseTypesAccessor.Read(parent!);
            if (live.Count == 0)
                continue;

            // Snapshot to avoid issues if we insert while iterating.
            var entries = live.ToList();

            foreach (var entry in entries)
            {
                if (entry.AnimalIds.Count == 0)
                    continue;

                // Find which male clone IDs are in this entry.
                var present = entry.AnimalIds
                    .Where(id => id is not null && maleIds.Contains(id))
                    .Distinct(KeyComparer)
                    .ToList();

                if (present.Count == 0)
                    continue;

                foreach (var maleId in present)
                {
                    if (!maleToFemaleUnlock.TryGetValue(maleId, out var femaleCond))
                        continue;

                    // If we already have a male-only entry somewhere under this parent with the correct condition, don't add duplicates.
                    var hasDedicated = entries.Any(e =>
                        e.AnimalIds.Count == 1
                        && e.AnimalIds[0].Equals(maleId, StringComparison.OrdinalIgnoreCase)
                        && ContainsToken(e.Condition, femaleCond));

                    if (entry.AnimalIds.Count == 1)
                    {
                        // Male-only: just AND its Condition.
                        var newCond = ConditionJoiner.And(entry.Condition, femaleCond);
                        AlternatePurchaseTypesAccessor.SetCondition(entry, newCond);
                        continue;
                    }

                    // Mixed: remove male from the random pool and add a dedicated male-only entry gated by female.
                    for (var i = entry.AnimalIds.Count - 1; i >= 0; i--)
                    {
                        if (entry.AnimalIds[i].Equals(maleId, StringComparison.OrdinalIgnoreCase))
                            entry.AnimalIds.RemoveAt(i);
                    }

                    if (entry.AnimalIds.Count == 0)
                        AlternatePurchaseTypesAccessor.TryRemoveAlternate(parent!, entry);

                    if (hasDedicated)
                        continue;

                    var newId = (entry.Id ?? "wegg_alt") + $"__wegg_clone__{maleId}";
                    var newCond = ConditionJoiner.And(entry.Condition, femaleCond);

                    AlternatePurchaseTypesAccessor.TryInsertAfter(
                        parent!,
                        existing: entry,
                        animalIds: new[] { maleId },
                        id: newId,
                        condition: newCond);
                }
            }
        }
    }

    private static string? RemoveToken(string? condition, string token)
    {
        var parts = SplitTokens(condition);
        parts.RemoveAll(p => p.Equals(token, StringComparison.OrdinalIgnoreCase));
        return parts.Count == 0 ? null : string.Join(", ", parts);
    }

    private static bool ContainsToken(string? condition, string token)
        => SplitTokens(condition).Any(p => p.Equals(token, StringComparison.OrdinalIgnoreCase));

    private static List<string> SplitTokens(string? condition)
    {
        var parts = new List<string>();
        foreach (var raw in (condition ?? string.Empty).Split(','))
        {
            var t = raw.Trim();
            if (!string.IsNullOrWhiteSpace(t))
                parts.Add(t);
        }

        return parts;
    }
}
