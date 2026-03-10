using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RoosterControl.Constants;
using RoosterControl.Enums;
using RoosterControl.Framework.Config;
using StardewValley;

namespace RoosterControl.Integrations.StardewUi.Produce;

internal sealed class RoosterProduceEditorService
{
    private static readonly string[] AllowedChoices = BuildAllowedChoices();
    private static readonly IReadOnlyList<RoosterProduceChoiceOptionRecord> ChoiceOptions = BuildChoiceOptions();

    public RoosterProduceEditorSnapshot LoadSnapshot(RoosterControlConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var roosters = Enum.GetValues(typeof(RoosterType))
            .Cast<RoosterType>()
            .Where(config.Roosters.ContainsKey)
            .Select(type => BuildRoosterRecord(type, config.Roosters[type]))
            .ToList();

        return new RoosterProduceEditorSnapshot(roosters, ChoiceOptions);
    }

    public RoosterProduceEditorDraft CreateDraft(RoosterProduceEditorSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var roosters = snapshot.Roosters.ToDictionary(
            record => record.Type,
            record => new RoosterProduceRoosterDraft(
                record.Type,
                record.DisplayName,
                record.ProduceMode,
                record.DeluxeProduceMode,
                record.Normal.Slots.Select(CloneSlot).ToList(),
                record.Deluxe.Slots.Select(CloneSlot).ToList()),
            EqualityComparer<RoosterType>.Default);

        return new RoosterProduceEditorDraft(roosters);
    }

    public void ApplyDraft(RoosterControlConfig config, RoosterProduceEditorDraft draft)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(draft);

        foreach (var (type, roosterDraft) in draft.Roosters)
        {
            if (!config.Roosters.TryGetValue(type, out var settings))
                continue;

            settings.ProduceOption = roosterDraft.ProduceMode;
            settings.DeluxeProduceOption = roosterDraft.DeluxeProduceMode;
            settings.ProduceEntries = roosterDraft.NormalSlots.Select(ToConfigEntry).ToList();
            settings.DeluxeProduceEntries = roosterDraft.DeluxeSlots.Select(ToConfigEntry).ToList();
        }
    }

    public string BuildSectionSummary(RoosterProduceRoosterDraft rooster, RoosterProduceSection section)
    {
        var mode = section == RoosterProduceSection.Normal
            ? rooster.ProduceMode.ToString()
            : rooster.DeluxeProduceMode.ToString();

        return section switch
        {
            RoosterProduceSection.Normal when mode == ProduceMode.KeepOriginalProduce.ToString()
                => "Normal produce uses the game's original produce table.",
            RoosterProduceSection.Deluxe when mode == DeluxeProduceMode.Off.ToString()
                => "Deluxe produce is disabled.",
            RoosterProduceSection.Deluxe when mode == DeluxeProduceMode.KeepOriginalProduce.ToString()
                => "Deluxe produce uses the game's original deluxe produce table.",
            _ => "Custom weighted slots are active for this section."
        };
    }

    public string BuildModeSummary(RoosterProduceRoosterDraft rooster)
        => $"Normal: {rooster.ProduceMode} | Deluxe: {rooster.DeluxeProduceMode}";

    public IReadOnlyList<string> GetProduceModeOptions()
        => Enum.GetNames(typeof(ProduceMode));

    public IReadOnlyList<string> GetDeluxeProduceModeOptions()
        => Enum.GetNames(typeof(DeluxeProduceMode));

    public IReadOnlyList<RoosterProduceExistingItemOptionRecord> GetExistingItemOptions()
    {
        if (Game1.objectData is null || Game1.objectData.Count == 0)
            return Array.Empty<RoosterProduceExistingItemOptionRecord>();

        return Game1.objectData
            .OrderBy(kvp => kvp.Value?.DisplayName ?? kvp.Key, StringComparer.OrdinalIgnoreCase)
            .Take(400)
            .Select(kvp => new RoosterProduceExistingItemOptionRecord(
                kvp.Key,
                $"{kvp.Value?.DisplayName ?? kvp.Key} ({kvp.Key})"))
            .ToList();
    }

    public bool TryValidateExistingItem(string? itemId, out string normalizedItemId, out string message)
    {
        normalizedItemId = string.IsNullOrWhiteSpace(itemId) ? string.Empty : itemId.Trim();
        if (string.IsNullOrWhiteSpace(normalizedItemId))
        {
            message = "Enter an item ID to validate.";
            return false;
        }

        if (ItemRegistry.GetData(normalizedItemId) is not null)
        {
            message = $"Valid item ID: {normalizedItemId}";
            return true;
        }

        message = $"Unknown item ID: {normalizedItemId}";
        return false;
    }

    public RoosterProduceItemValidationResult ValidateExistingItem(string? itemId)
    {
        var isValid = TryValidateExistingItem(itemId, out var normalizedItemId, out var message);
        return new RoosterProduceItemValidationResult(isValid, normalizedItemId, message);
    }

    private static RoosterProduceRoosterRecord BuildRoosterRecord(RoosterType type, RoosterSettings settings)
    {
        var normalSlots = NormalizeEntries(settings.ProduceEntries)
            .Select((entry, index) => BuildSlotRecord(index + 1, entry))
            .ToList();
        var deluxeSlots = NormalizeEntries(settings.DeluxeProduceEntries)
            .Select((entry, index) => BuildSlotRecord(index + 1, entry))
            .ToList();

        return new RoosterProduceRoosterRecord(
            type,
            type.ToString(),
            settings.ProduceOption,
            settings.DeluxeProduceOption,
            new RoosterProduceSectionRecord(RoosterProduceSection.Normal, normalSlots),
            new RoosterProduceSectionRecord(RoosterProduceSection.Deluxe, deluxeSlots));
    }

    private static RoosterProduceSlotRecord BuildSlotRecord(int slot, ProduceEntry entry)
    {
        var choice = NormalizeChoice(entry.Choice);
        var option = ChoiceOptions.FirstOrDefault(candidate => string.Equals(candidate.Key, choice, StringComparison.OrdinalIgnoreCase));

        return new RoosterProduceSlotRecord(
            slot,
            choice,
            NormalizeCustomItem(entry.CustomItemName),
            Math.Clamp(entry.Probability, 0, 100),
            option?.DisplayLabel ?? BuildDisplayLabel(choice),
            option?.Category ?? ClassifyChoice(choice),
            ItemIds.VariableValues.TryGetValue(choice, out var stableKey) ? stableKey : choice);
    }

    private static RoosterProduceSlotDraft CloneSlot(RoosterProduceSlotRecord slot)
        => new(
            slot.Slot,
            slot.Choice,
            slot.CustomItemName,
            slot.Weight,
            slot.DisplayLabel,
            slot.Category,
            slot.StableKey);

    private static ProduceEntry ToConfigEntry(RoosterProduceSlotDraft slot)
        => new()
        {
            Choice = NormalizeChoice(slot.Choice),
            CustomItemName = NormalizeCustomItem(slot.CustomItemName),
            Probability = Math.Clamp(slot.Weight, 0, 100),
        };

    private static List<ProduceEntry> NormalizeEntries(List<ProduceEntry>? entries)
    {
        entries ??= ProduceEntry.CreateDefaultFour();

        while (entries.Count < 4)
            entries.Add(new ProduceEntry());

        while (entries.Count > 4)
            entries.RemoveAt(entries.Count - 1);

        for (var i = 0; i < entries.Count; i++)
        {
            entries[i] ??= new ProduceEntry();
            entries[i].Choice = NormalizeChoice(entries[i].Choice);
            entries[i].CustomItemName = NormalizeCustomItem(entries[i].CustomItemName);
            entries[i].Probability = Math.Clamp(entries[i].Probability, 0, 100);
        }

        return entries;
    }

    private static string NormalizeChoice(string? choice)
        => string.IsNullOrWhiteSpace(choice)
            ? ItemIds.CustomItems.Custom
            : choice.Trim();

    private static string NormalizeCustomItem(string? customItem)
        => string.IsNullOrWhiteSpace(customItem)
            ? ItemIds.CustomItems.Custom
            : customItem.Trim();

    private static string[] BuildAllowedChoices()
    {
        var values = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        AddConstStrings(typeof(ItemIds.CustomItems), values);
        AddConstStrings(typeof(ItemIds.AnimalProducts), values);
        return values.ToArray();
    }

    private static IReadOnlyList<RoosterProduceChoiceOptionRecord> BuildChoiceOptions()
        => AllowedChoices
            .Select(key => new RoosterProduceChoiceOptionRecord(
                key,
                BuildDisplayLabel(key),
                ClassifyChoice(key),
                ItemIds.VariableValues.TryGetValue(key, out var stableKey) ? stableKey : key))
            .ToList();

    private static void AddConstStrings(Type type, ISet<string> sink)
    {
        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.FieldType != typeof(string) || !field.IsLiteral || field.IsInitOnly)
                continue;

            if (field.GetRawConstantValue() is string value && !string.IsNullOrWhiteSpace(value))
                sink.Add(value.Trim());
        }
    }

    private static string BuildDisplayLabel(string key)
    {
        if (key.Contains(' ', StringComparison.Ordinal))
            return key;

        var chars = new List<char>(key.Length + 8);
        for (var i = 0; i < key.Length; i++)
        {
            var current = key[i];
            if (i > 0 && char.IsUpper(current) && !char.IsUpper(key[i - 1]))
                chars.Add(' ');
            chars.Add(current);
        }

        return new string(chars.ToArray());
    }

    private static string ClassifyChoice(string key)
    {
        if (string.Equals(key, ItemIds.CustomItems.Custom, StringComparison.OrdinalIgnoreCase))
            return "Custom";

        foreach (var field in typeof(ItemIds.AnimalProducts).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.GetRawConstantValue() is string value && string.Equals(value, key, StringComparison.OrdinalIgnoreCase))
                return "Animal Product";
        }

        foreach (var field in typeof(ItemIds.RandomProduces).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.GetRawConstantValue() is string value && string.Equals(value, key, StringComparison.OrdinalIgnoreCase))
                return "Random Group";
        }

        foreach (var field in typeof(ItemIds.CustomItems).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.GetRawConstantValue() is string value && string.Equals(value, key, StringComparison.OrdinalIgnoreCase))
                return "Custom";
        }

        return "Custom";
    }
}

internal sealed record RoosterProduceEditorSnapshot(
    IReadOnlyList<RoosterProduceRoosterRecord> Roosters,
    IReadOnlyList<RoosterProduceChoiceOptionRecord> ChoiceOptions);

internal sealed record RoosterProduceExistingItemOptionRecord(
    string ItemId,
    string DisplayText);

internal sealed record RoosterProduceItemValidationResult(
    bool IsValid,
    string NormalizedItemId,
    string Message);

internal sealed record RoosterProduceRoosterRecord(
    RoosterType Type,
    string DisplayName,
    ProduceMode ProduceMode,
    DeluxeProduceMode DeluxeProduceMode,
    RoosterProduceSectionRecord Normal,
    RoosterProduceSectionRecord Deluxe);

internal sealed record RoosterProduceSectionRecord(
    RoosterProduceSection Section,
    IReadOnlyList<RoosterProduceSlotRecord> Slots);

internal sealed record RoosterProduceSlotRecord(
    int Slot,
    string Choice,
    string CustomItemName,
    int Weight,
    string DisplayLabel,
    string Category,
    string StableKey);

internal sealed record RoosterProduceChoiceOptionRecord(
    string Key,
    string DisplayLabel,
    string Category,
    string StableKey);

internal sealed class RoosterProduceEditorDraft
{
    public RoosterProduceEditorDraft(Dictionary<RoosterType, RoosterProduceRoosterDraft> roosters)
    {
        Roosters = roosters;
    }

    public Dictionary<RoosterType, RoosterProduceRoosterDraft> Roosters { get; }
}

internal sealed class RoosterProduceRoosterDraft
{
    public RoosterProduceRoosterDraft(
        RoosterType type,
        string displayName,
        ProduceMode produceMode,
        DeluxeProduceMode deluxeProduceMode,
        List<RoosterProduceSlotDraft> normalSlots,
        List<RoosterProduceSlotDraft> deluxeSlots)
    {
        Type = type;
        DisplayName = displayName;
        ProduceMode = produceMode;
        DeluxeProduceMode = deluxeProduceMode;
        NormalSlots = normalSlots;
        DeluxeSlots = deluxeSlots;
    }

    public RoosterType Type { get; }
    public string DisplayName { get; }
    public ProduceMode ProduceMode { get; set; }
    public DeluxeProduceMode DeluxeProduceMode { get; set; }
    public List<RoosterProduceSlotDraft> NormalSlots { get; }
    public List<RoosterProduceSlotDraft> DeluxeSlots { get; }
}

internal sealed class RoosterProduceSlotDraft
{
    public RoosterProduceSlotDraft(
        int slot,
        string choice,
        string customItemName,
        int weight,
        string displayLabel,
        string category,
        string stableKey)
    {
        Slot = slot;
        Choice = choice;
        CustomItemName = customItemName;
        Weight = weight;
        DisplayLabel = displayLabel;
        Category = category;
        StableKey = stableKey;
    }

    public int Slot { get; }
    public string Choice { get; set; }
    public string CustomItemName { get; set; }
    public int Weight { get; set; }
    public string DisplayLabel { get; set; }
    public string Category { get; set; }
    public string StableKey { get; set; }
}

internal enum RoosterProduceSection
{
    Normal,
    Deluxe
}
