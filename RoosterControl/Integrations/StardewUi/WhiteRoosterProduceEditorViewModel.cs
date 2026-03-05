using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using RoosterControl.Constants;
using RoosterControl.Framework.Config;

namespace RoosterControl.Integrations.StardewUi;

/// <summary>
/// View model for editing White rooster produce entries using StardewUI.
/// </summary>
public sealed class WhiteRoosterProduceEditorViewModel
{
    private static readonly string[] AllowedChoices = BuildProduceChoiceAllowedValues();

    public WhiteRoosterProduceEditorViewModel(RoosterSettings settings)
    {
        if (settings is null)
            throw new ArgumentNullException(nameof(settings));

        EnsureProduceEntriesNormalized(settings.ProduceEntries);

        Title = "White Rooster Produce Editor";
        Choices = AllowedChoices;
        FormatProbability = value => ((int)MathF.Round(value)).ToString();

        var slots = new List<ProduceSlotViewModel>(settings.ProduceEntries.Count);
        for (var i = 0; i < settings.ProduceEntries.Count; i++)
            slots.Add(new ProduceSlotViewModel(slot: i + 1, entry: settings.ProduceEntries[i]));
        Slots = slots;
    }

    public string Title { get; }

    public IReadOnlyList<string> Choices { get; }

    public Func<float, string> FormatProbability { get; }

    public IReadOnlyList<ProduceSlotViewModel> Slots { get; }

    private static string[] BuildProduceChoiceAllowedValues()
    {
        var values = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

        static void AddConstStrings(Type t, ISet<string> sink)
        {
            foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (f.FieldType != typeof(string) || !f.IsLiteral || f.IsInitOnly)
                    continue;

                if (f.GetRawConstantValue() is string s && !string.IsNullOrWhiteSpace(s))
                    sink.Add(s.Trim());
            }
        }

        AddConstStrings(typeof(ItemIds.CustomItems), values);
        AddConstStrings(typeof(ItemIds.AnimalProducts), values);

        return new List<string>(values).ToArray();
    }

    private static void EnsureProduceEntriesNormalized(IList<ProduceEntry> entries)
    {
        if (entries is null)
            return;

        while (entries.Count < 4)
            entries.Add(new ProduceEntry());

        while (entries.Count > 4)
            entries.RemoveAt(entries.Count - 1);

        for (var i = 0; i < entries.Count; i++)
        {
            entries[i] ??= new ProduceEntry();

            entries[i].Choice = (entries[i].Choice ?? string.Empty).Trim();
            entries[i].CustomItemName = (entries[i].CustomItemName ?? string.Empty).Trim();
            entries[i].Probability = Math.Clamp(entries[i].Probability, 0, 100);
        }
    }
}

public sealed class ProduceSlotViewModel : INotifyPropertyChanged
{
    private readonly ProduceEntry _entry;

    public ProduceSlotViewModel(int slot, ProduceEntry entry)
    {
        _entry = entry ?? throw new ArgumentNullException(nameof(entry));
        SlotTitle = $"Slot {slot}";
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string SlotTitle { get; }

    public string Choice
    {
        get => _entry.Choice;
        set
        {
            var next = (value ?? string.Empty).Trim();
            if (string.Equals(_entry.Choice, next, StringComparison.Ordinal))
                return;

            _entry.Choice = next;
            OnPropertyChanged(nameof(Choice));
            OnPropertyChanged(nameof(IsCustom));
        }
    }

    public bool IsCustom => string.Equals(Choice, ItemIds.CustomItems.Custom, StringComparison.OrdinalIgnoreCase);

    public string CustomItemName
    {
        get => _entry.CustomItemName;
        set
        {
            var next = (value ?? string.Empty).Trim();
            if (string.Equals(_entry.CustomItemName, next, StringComparison.Ordinal))
                return;

            _entry.CustomItemName = next;
            OnPropertyChanged(nameof(CustomItemName));
        }
    }

    public float Probability
    {
        get => _entry.Probability;
        set
        {
            var next = Math.Clamp((int)MathF.Round(value), 0, 100);
            if (_entry.Probability == next)
                return;

            _entry.Probability = next;
            OnPropertyChanged(nameof(Probability));
        }
    }

    private void OnPropertyChanged(string name)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
