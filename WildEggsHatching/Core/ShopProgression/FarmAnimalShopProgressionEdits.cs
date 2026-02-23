using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using StardewValley.GameData.FarmAnimals;
using WildEggsHatching.Application.AnimalNotes;
using WildEggsHatching.Core.ShopAnchors;
using WildEggsHatching.Infrastructure.Config;

namespace WildEggsHatching.Core.ShopProgression;

/// <summary>
/// Applies AnimalNotes-driven shop gating to <c>Data/FarmAnimals</c>.
/// </summary>
public static class FarmAnimalShopProgressionEdits
{
    private static readonly StringComparer KeyComparer = StringComparer.OrdinalIgnoreCase;

    public sealed record AltEntrySnapshot(int Index, List<string> AnimalIds, string? Condition);

    public sealed class Snapshot
    {
        public Dictionary<string, ShopFields> FieldsByAnimalId { get; } = new(KeyComparer);
        public Dictionary<string, List<AltEntrySnapshot>> AlternatesByParentId { get; } = new(KeyComparer);
        public Dictionary<(string ParentId, string ChildId), string?> AlternateConditionByPair { get; } = new();

        public Dictionary<string, string> PreferredMasterByChildId { get; } = new(KeyComparer);
    }

    public sealed class ShopFields
    {
        public int PurchasePrice { get; init; }
        public int SellPrice { get; init; }
        public object? RequiredBuilding { get; init; }
        public string? ShopDescription { get; init; }
        public string? ShopDisplayName { get; init; }
        public string? ShopMissingBuildingDescription { get; init; }
        public object? ShopSourceRect { get; init; }
        public string? ShopTexture { get; init; }
        public string? UnlockCondition { get; init; }
    }

    public static Snapshot Capture(IDictionary<string, FarmAnimalData> farmAnimals)
    {
        if (farmAnimals is null)
            throw new ArgumentNullException(nameof(farmAnimals));

        var snap = new Snapshot();

        foreach (var (id, a) in farmAnimals)
        {
            if (a is null || string.IsNullOrWhiteSpace(id))
                continue;

            snap.FieldsByAnimalId[id] = ShopFieldsAccessor.Read(a);
        }

        foreach (var (parentId, parent) in farmAnimals)
        {
            if (parent is null || string.IsNullOrWhiteSpace(parentId))
                continue;

            var alt = AlternatePurchaseTypesAccessor.Read(parent);
            if (alt.Count == 0)
                continue;

            var list = new List<AltEntrySnapshot>(alt.Count);
            for (var i = 0; i < alt.Count; i++)
            {
                var entry = alt[i];

                var ids = entry.AnimalIds
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct(KeyComparer)
                    .ToList();

                list.Add(new AltEntrySnapshot(
                    Index: i,
                    AnimalIds: ids,
                    Condition: entry.Condition));

                foreach (var childId in ids)
                {
                    snap.AlternateConditionByPair[(parentId, childId)] = entry.Condition;
                    if (!snap.PreferredMasterByChildId.ContainsKey(childId))
                        snap.PreferredMasterByChildId[childId] = parentId;
                    else
                    {
                        // Prefer a master with a visible listing price in the original data.
                        var existing = snap.PreferredMasterByChildId[childId];
                        if (snap.FieldsByAnimalId.TryGetValue(existing, out var exf)
                            && snap.FieldsByAnimalId.TryGetValue(parentId, out var nf)
                            && exf.PurchasePrice <= 0
                            && nf.PurchasePrice > 0)
                        {
                            snap.PreferredMasterByChildId[childId] = parentId;
                        }
                    }
                }
            }

            snap.AlternatesByParentId[parentId] = list;
        }

        return snap;
    }

    /// <summary>
    /// Phase 6A: lock animals and remove them from alternate listings to avoid hinting.
    /// </summary>
    public static void ApplyLockedEdits(
        ModConfig config,
        IReadOnlyCollection<string> managedAnimalIds,
        IDictionary<string, FarmAnimalData> farmAnimals,
        ShopAnchorIndex anchors)
    {
        if (config is null)
            throw new ArgumentNullException(nameof(config));
        if (managedAnimalIds is null)
            throw new ArgumentNullException(nameof(managedAnimalIds));
        if (farmAnimals is null)
            throw new ArgumentNullException(nameof(farmAnimals));

        anchors ??= ShopAnchorIndex.Empty;

        var readOnlyFarmAnimals =
            farmAnimals as IReadOnlyDictionary<string, FarmAnimalData>
            ?? new Dictionary<string, FarmAnimalData>(farmAnimals, StringComparer.OrdinalIgnoreCase);
        var gated = ComputeGated(config, managedAnimalIds, readOnlyFarmAnimals, anchors);
        if (gated.Count == 0)
            return;

        foreach (var id in gated)
        {
            if (!farmAnimals.TryGetValue(id, out var a) || a is null)
                continue;

            a.PurchasePrice = -1;
            a.SellPrice = -1;
        }

        foreach (var parent in farmAnimals.Values.Where(v => v is not null))
            AlternatePurchaseTypesAccessor.RemoveAnimalIds(parent!, gated);
    }

    /// <summary>
    /// Phase 6B: re-enable unlocked visibility via GSQ/mail parity and repair direct listings when needed.
    /// </summary>
    public static void ApplyUnlockedEdits(
        ModConfig config,
        IReadOnlyCollection<string> managedAnimalIds,
        IDictionary<string, FarmAnimalData> farmAnimals,
        ShopAnchorIndex anchors,
        Snapshot original)
    {
        if (config is null)
            throw new ArgumentNullException(nameof(config));
        if (managedAnimalIds is null)
            throw new ArgumentNullException(nameof(managedAnimalIds));
        if (farmAnimals is null)
            throw new ArgumentNullException(nameof(farmAnimals));
        if (original is null)
            throw new ArgumentNullException(nameof(original));

        anchors ??= ShopAnchorIndex.Empty;

        var readOnlyFarmAnimals =
            farmAnimals as IReadOnlyDictionary<string, FarmAnimalData>
            ?? new Dictionary<string, FarmAnimalData>(farmAnimals, StringComparer.OrdinalIgnoreCase);
        var gated = ComputeGated(config, managedAnimalIds, readOnlyFarmAnimals, anchors);
        if (gated.Count == 0)
            return;

        // Restore original alternate IDs (unlocked gating handled via per-animal UnlockCondition).
        RestoreAlternatesFromSnapshot(farmAnimals, original, gated);

        foreach (var id in gated)
        {
            if (!farmAnimals.TryGetValue(id, out var animal) || animal is null)
                continue;

            var mailId = AnimalNoteIds.NoteMailId(id);
            var mailCond = ConditionJoiner.MailReceivedByHost(mailId);

            // Base: preserve any existing condition, and AND the mail condition.
            var cond = ShopFieldsAccessor.GetUnlockCondition(animal);

            // Merge original condition(s) for parity.
            if (original.FieldsByAnimalId.TryGetValue(id, out var selfOriginal)
                && !string.IsNullOrWhiteSpace(selfOriginal.UnlockCondition))
            {
                cond = ConditionJoiner.And(cond, selfOriginal.UnlockCondition);
            }

            // If the animal was previously only accessible as an alternate, merge the alternate condition.
            if (original.PreferredMasterByChildId.TryGetValue(id, out var masterId)
                && original.AlternateConditionByPair.TryGetValue((masterId, id), out var altCond)
                && !string.IsNullOrWhiteSpace(altCond))
            {
                cond = ConditionJoiner.And(cond, altCond);
            }

            cond = ConditionJoiner.And(cond, mailCond);
            ShopFieldsAccessor.SetUnlockCondition(animal, cond);

            // If this is (or must become) a direct listing: restore/copy shop fields + price.
            if (!original.FieldsByAnimalId.TryGetValue(id, out var originalFields))
                continue;

            if (originalFields.PurchasePrice > 0)
            {
                RestoreDirectListing(animal, originalFields);
                continue;
            }

            // Hidden variant: if its master listing is locked, convert to direct listing using master fields.
            if (original.PreferredMasterByChildId.TryGetValue(id, out masterId)
                && farmAnimals.TryGetValue(masterId, out var masterLive)
                && masterLive is not null
                && masterLive.PurchasePrice < 0
                && original.FieldsByAnimalId.TryGetValue(masterId, out var masterOriginal)
                && masterOriginal.PurchasePrice > 0)
            {
                RestoreDirectListing(animal, masterOriginal);
            }
        }
    }

    private static void RestoreAlternatesFromSnapshot(
        IDictionary<string, FarmAnimalData> farmAnimals,
        Snapshot original,
        ISet<string> gated)
    {
        foreach (var (parentId, entries) in original.AlternatesByParentId)
        {
            if (!farmAnimals.TryGetValue(parentId, out var parent) || parent is null)
                continue;

            var live = AlternatePurchaseTypesAccessor.Read(parent);
            if (live.Count == 0)
                continue;

            foreach (var e in entries)
            {
                if (e.Index < 0 || e.Index >= live.Count)
                    continue;

                var liveIds = live[e.Index].AnimalIds;

                foreach (var childId in e.AnimalIds)
                {
                    if (!gated.Contains(childId))
                        continue;

                    if (!liveIds.Contains(childId, KeyComparer))
                        liveIds.Add(childId);
                }
            }
        }
    }

    private static void RestoreDirectListing(FarmAnimalData target, ShopFields source)
    {
        if (target is null || source is null)
            return;

        if (target.PurchasePrice < 0)
        {
            target.PurchasePrice = source.PurchasePrice;
            target.SellPrice = source.SellPrice > 0 ? source.SellPrice : source.PurchasePrice;
        }

        ShopFieldsAccessor.CopyMissingShopFields(source, target);

        target.ShopDisplayName ??= target.DisplayName;
    }

    private static HashSet<string> ComputeGated(
        ModConfig config,
        IReadOnlyCollection<string> managedAnimalIds,
        IReadOnlyDictionary<string, FarmAnimalData> farmAnimals,
        ShopAnchorIndex anchors)
    {
        var gated = new HashSet<string>(KeyComparer);

        foreach (var id in managedAnimalIds)
        {
            if (string.IsNullOrWhiteSpace(id))
                continue;

            if (!farmAnimals.ContainsKey(id))
                continue;

            if (AnimalShopProgressionPolicy.ShouldGate(id, config, farmAnimals, anchors))
                gated.Add(id);
        }

        return gated;
    }

    internal static class ShopFieldsAccessor
    {
        private static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public;

        public static ShopFields Read(FarmAnimalData a)
        {
            return new ShopFields
            {
                PurchasePrice = a.PurchasePrice,
                SellPrice = a.SellPrice,
                RequiredBuilding = TryGet(a, "RequiredBuilding"),
                ShopDescription = TryGet(a, "ShopDescription") as string,
                ShopDisplayName = TryGet(a, "ShopDisplayName") as string,
                ShopMissingBuildingDescription = TryGet(a, "ShopMissingBuildingDescription") as string,
                ShopSourceRect = TryGet(a, "ShopSourceRect"),
                ShopTexture = TryGet(a, "ShopTexture") as string,
                UnlockCondition = TryGet(a, "UnlockCondition") as string,
            };
        }

        public static string? GetUnlockCondition(FarmAnimalData a)
            => TryGet(a, "UnlockCondition") as string;

        public static void SetUnlockCondition(FarmAnimalData a, string? value)
        {
            TrySet(a, "UnlockCondition", value);
        }

        public static void CopyMissingShopFields(ShopFields source, FarmAnimalData target)
        {
            CopyIfMissing(target, "RequiredBuilding", source.RequiredBuilding);
            CopyIfMissing(target, "ShopDescription", source.ShopDescription);
            CopyIfMissing(target, "ShopDisplayName", source.ShopDisplayName);
            CopyIfMissing(target, "ShopMissingBuildingDescription", source.ShopMissingBuildingDescription);
            CopyIfMissing(target, "ShopSourceRect", source.ShopSourceRect);
            CopyIfMissing(target, "ShopTexture", source.ShopTexture);
        }

        private static void CopyIfMissing(FarmAnimalData target, string propName, object? value)
        {
            if (value is null)
                return;

            var cur = TryGet(target, propName);
            if (cur is null)
            {
                TrySet(target, propName, value);
                return;
            }

            if (cur is string s && string.IsNullOrWhiteSpace(s))
                TrySet(target, propName, value);
        }

        private static object? TryGet(FarmAnimalData a, string propName)
            => a.GetType().GetProperty(propName, Flags)?.GetValue(a);

        private static void TrySet(FarmAnimalData a, string propName, object? value)
        {
            try
            {
                var p = a.GetType().GetProperty(propName, Flags);
                if (p is null || !p.CanWrite)
                    return;

                p.SetValue(a, value);
            }
            catch
            {
                // best-effort
            }
        }
    }
}
