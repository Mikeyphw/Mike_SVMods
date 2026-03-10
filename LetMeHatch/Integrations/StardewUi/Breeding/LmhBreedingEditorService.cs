using LetMeHatch.Config;
using LetMeHatch.Models;
using StardewModdingAPI;
using static LetMeHatch.EnumsAndStrings.Enums;

namespace LetMeHatch.Integrations.StardewUi.Breeding;

public sealed class LmhBreedingEditorService
{
    public IReadOnlyList<LmhBreedingPackRecord> LoadPacksSnapshot(
        IReadOnlyDictionary<string, Dictionary<string, BreedingDataFemale>> loadedFemaleBreedingData,
        IReadOnlyList<IContentPack> loadedContentPacks)
    {
        ArgumentNullException.ThrowIfNull(loadedFemaleBreedingData);
        ArgumentNullException.ThrowIfNull(loadedContentPacks);

        return loadedFemaleBreedingData
            .Select(kvp => new LmhBreedingPackRecord(
                kvp.Key,
                ResolveContentPackDisplayName(loadedContentPacks, kvp.Key),
                kvp.Value.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList()))
            .OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.ContentPackId, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public bool TryLoadFemaleRecord(
        IReadOnlyDictionary<string, Dictionary<string, BreedingDataFemale>> loadedFemaleBreedingData,
        IReadOnlyDictionary<string, Dictionary<string, BreedingConfig>> breedingConfigsByPack,
        string contentPackId,
        string femaleKey,
        out LmhBreedingFemaleRecord record)
    {
        ArgumentNullException.ThrowIfNull(loadedFemaleBreedingData);
        ArgumentNullException.ThrowIfNull(breedingConfigsByPack);

        record = null!;

        if (!loadedFemaleBreedingData.TryGetValue(contentPackId, out var byFemaleData))
            return false;
        if (!byFemaleData.TryGetValue(femaleKey, out var femaleData))
            return false;
        if (!breedingConfigsByPack.TryGetValue(contentPackId, out var byFemaleConfig))
            return false;
        if (!byFemaleConfig.TryGetValue(femaleKey, out var femaleConfig))
            return false;

        record = new LmhBreedingFemaleRecord(
            contentPackId,
            femaleKey,
            femaleData.ApplyDailyLuckBoost,
            femaleData.AdjustableEggChances,
            new Dictionary<ItemType, float>(femaleConfig.EggChances),
            femaleData.Males
                .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .Select(x => BuildMaleRecord(x.Key, x.Value, femaleConfig))
                .ToList());
        return true;
    }

    public LmhBreedingDraft CreateDraft(BreedingDataFemale femaleData, BreedingConfig femaleConfig)
    {
        ArgumentNullException.ThrowIfNull(femaleData);
        ArgumentNullException.ThrowIfNull(femaleConfig);

        return new LmhBreedingDraft(
            femaleData.ApplyDailyLuckBoost,
            new Dictionary<ItemType, float>(femaleConfig.EggChances),
            new Dictionary<string, bool>(femaleConfig.Enabled, StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, float>(femaleConfig.MaleChance, StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, float>(femaleConfig.BoostByMale, StringComparer.OrdinalIgnoreCase),
            femaleConfig.MaleConfig.AddAsAlternatePurchase);
    }

    public void ApplyDraft(BreedingDataFemale femaleData, BreedingConfig femaleConfig, LmhBreedingDraft draft)
    {
        ArgumentNullException.ThrowIfNull(femaleData);
        ArgumentNullException.ThrowIfNull(femaleConfig);
        ArgumentNullException.ThrowIfNull(draft);

        femaleData.ApplyDailyLuckBoost = draft.ApplyDailyLuckBoost;
        femaleConfig.ApplyDailyLuckBoost = draft.ApplyDailyLuckBoost;

        foreach (var eggType in draft.EggChances.Keys)
            femaleConfig.EggChances[eggType] = draft.EggChances[eggType];

        foreach (var maleKey in draft.Enabled.Keys)
            femaleConfig.Enabled[maleKey] = draft.Enabled[maleKey];

        foreach (var maleKey in draft.MaleChance.Keys)
            femaleConfig.MaleChance[maleKey] = draft.MaleChance[maleKey];

        foreach (var maleKey in draft.BoostByMale.Keys)
            femaleConfig.BoostByMale[maleKey] = draft.BoostByMale[maleKey];

        femaleConfig.MaleConfig.AddAsAlternatePurchase = draft.AddAsAlternatePurchase;
    }

    public void SetApplyDailyLuckBoost(BreedingDataFemale femaleData, BreedingConfig femaleConfig, bool value)
    {
        var draft = CreateDraft(femaleData, femaleConfig) with { ApplyDailyLuckBoost = value };
        ApplyDraft(femaleData, femaleConfig, draft);
    }

    public void SetEggChance(BreedingDataFemale femaleData, BreedingConfig femaleConfig, ItemType type, float value)
    {
        var eggChances = new Dictionary<ItemType, float>(CreateDraft(femaleData, femaleConfig).EggChances)
        {
            [type] = value
        };
        ApplyDraft(femaleData, femaleConfig, CreateDraft(femaleData, femaleConfig) with { EggChances = eggChances });
    }

    public bool GetMaleEnabled(BreedingConfig femaleConfig, string maleKey)
    {
        if (!femaleConfig.Enabled.TryGetValue(maleKey, out var enabled))
        {
            femaleConfig.Enabled[maleKey] = true;
            return true;
        }

        return enabled;
    }

    public void SetMaleEnabled(BreedingDataFemale femaleData, BreedingConfig femaleConfig, string maleKey, bool value)
    {
        var enabled = new Dictionary<string, bool>(CreateDraft(femaleData, femaleConfig).Enabled, StringComparer.OrdinalIgnoreCase)
        {
            [maleKey] = value
        };
        ApplyDraft(femaleData, femaleConfig, CreateDraft(femaleData, femaleConfig) with { Enabled = enabled });
    }

    public float GetBoostByMale(BreedingConfig femaleConfig, string maleKey)
    {
        if (!femaleConfig.BoostByMale.TryGetValue(maleKey, out var value))
        {
            femaleConfig.BoostByMale[maleKey] = 0.5f;
            return 0.5f;
        }

        return value;
    }

    public void SetBoostByMale(BreedingDataFemale femaleData, BreedingConfig femaleConfig, string maleKey, float value)
    {
        var boostByMale = new Dictionary<string, float>(CreateDraft(femaleData, femaleConfig).BoostByMale, StringComparer.OrdinalIgnoreCase)
        {
            [maleKey] = value
        };
        ApplyDraft(femaleData, femaleConfig, CreateDraft(femaleData, femaleConfig) with { BoostByMale = boostByMale });
    }

    public float GetMaleChance(BreedingConfig femaleConfig, string maleKey)
    {
        if (!femaleConfig.MaleChance.TryGetValue(maleKey, out var value))
        {
            femaleConfig.MaleChance[maleKey] = 0.5f;
            return 0.5f;
        }

        return value;
    }

    public void SetMaleChance(BreedingDataFemale femaleData, BreedingConfig femaleConfig, string maleKey, float value)
    {
        var maleChance = new Dictionary<string, float>(CreateDraft(femaleData, femaleConfig).MaleChance, StringComparer.OrdinalIgnoreCase)
        {
            [maleKey] = value
        };
        ApplyDraft(femaleData, femaleConfig, CreateDraft(femaleData, femaleConfig) with { MaleChance = maleChance });
    }

    public void SetAddAsAlternatePurchase(BreedingDataFemale femaleData, BreedingConfig femaleConfig, bool value)
    {
        ApplyDraft(femaleData, femaleConfig, CreateDraft(femaleData, femaleConfig) with { AddAsAlternatePurchase = value });
    }

    private static LmhBreedingMaleRecord BuildMaleRecord(string maleKey, BreedingDataMale maleData, BreedingConfig femaleConfig)
    {
        var enabled = femaleConfig.Enabled.TryGetValue(maleKey, out var currentEnabled) ? currentEnabled : maleData.Enabled;
        var maleChance = femaleConfig.MaleChance.TryGetValue(maleKey, out var currentChance) ? currentChance : maleData.MaleChance;
        var boostByMale = femaleConfig.BoostByMale.TryGetValue(maleKey, out var currentBoost) ? currentBoost : maleData.BoostByMale;

        return new LmhBreedingMaleRecord(
            maleKey,
            maleKey.EndsWith("_Male", StringComparison.OrdinalIgnoreCase) ? "config.breeding.males.cloned.label" : maleKey,
            maleKey.EndsWith("_Male", StringComparison.OrdinalIgnoreCase),
            maleData.AdjustableMaleChances,
            maleData.EggsIDs.ToDictionary(
                pair => pair.Key,
                pair => (IReadOnlyList<string>)pair.Value.Values
                    .Where(static value => !string.IsNullOrWhiteSpace(value))
                    .Select(static value => value.Trim())
                    .ToList(),
                EqualityComparer<ItemType>.Default),
            new Dictionary<ItemType, string?>(maleData.GetEggIDs()),
            enabled,
            maleChance,
            boostByMale,
            femaleConfig.MaleConfig.AddAsAlternatePurchase);
    }

    private static string ResolveContentPackDisplayName(IReadOnlyList<IContentPack> loadedContentPacks, string contentPackId)
    {
        var pack = loadedContentPacks.FirstOrDefault(x =>
            string.Equals(x.Manifest.UniqueID, contentPackId, StringComparison.OrdinalIgnoreCase));

        return pack?.Manifest.Name ?? contentPackId;
    }
}
