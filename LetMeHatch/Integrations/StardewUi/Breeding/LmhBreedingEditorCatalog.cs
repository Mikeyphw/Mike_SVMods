using LetMeHatch.EnumsAndStrings;

namespace LetMeHatch.Integrations.StardewUi.Breeding;

public sealed record LmhBreedingPackRecord(
    string ContentPackId,
    string DisplayName,
    IReadOnlyList<string> FemaleKeys);

public sealed record LmhBreedingMaleRecord(
    string MaleKey,
    string DisplayName,
    bool IsCloned,
    bool AdjustableMaleChances,
    IReadOnlyDictionary<Enums.ItemType, IReadOnlyList<string>> EggCandidatePools,
    IReadOnlyDictionary<Enums.ItemType, string?> EggIds,
    bool Enabled,
    float MaleChance,
    float BoostByMale,
    bool AddAsAlternatePurchase);

public sealed record LmhBreedingFemaleRecord(
    string ContentPackId,
    string FemaleKey,
    bool ApplyDailyLuckBoost,
    bool AdjustableEggChances,
    IReadOnlyDictionary<Enums.ItemType, float> EggChances,
    IReadOnlyList<LmhBreedingMaleRecord> Males);

public sealed record LmhBreedingDraft(
    bool ApplyDailyLuckBoost,
    IReadOnlyDictionary<Enums.ItemType, float> EggChances,
    IReadOnlyDictionary<string, bool> Enabled,
    IReadOnlyDictionary<string, float> MaleChance,
    IReadOnlyDictionary<string, float> BoostByMale,
    bool AddAsAlternatePurchase);
