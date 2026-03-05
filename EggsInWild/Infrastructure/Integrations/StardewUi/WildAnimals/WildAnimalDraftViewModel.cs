using System;
using System.Collections.Generic;
using System.Linq;
using Mikew.NestCore.Helpers.UI;

namespace EggsInWild.Infrastructure.Integrations.StardewUi.WildAnimals;

public sealed class WildAnimalDraftViewModel : ObservableObject
{
    private bool _enabled;
    private bool _largeEgg;
    private bool _isMagic;
    private string _priceText;
    private string _newModGroupId = "";

    public WildAnimalDraftViewModel(WildAnimalDraft draft)
    {
        Draft = draft ?? throw new ArgumentNullException(nameof(draft));
        _enabled = draft.Enabled;
        _largeEgg = draft.LargeEgg;
        _isMagic = draft.IsMagic;
        _priceText = draft.Price.ToString();

        LocationTypes = draft.LocationTypes
            .Select(location => new WildAnimalLocationTypeEditorViewModel(location.Key, location.BaseAllowed, location.Allowed))
            .ToList();

        ModGroups = draft.ModGroups
            .Select(group => new WildAnimalModGroupEditorViewModel(group.ModId, group.BaseKeys, group.Keys))
            .ToList();

        AddModGroupCommand = new RelayCommand(AddModGroup);
    }

    public WildAnimalDraft Draft { get; }
    public string Key => Draft.Key;
    public IReadOnlyList<WildAnimalLocationTypeEditorViewModel> LocationTypes { get; }
    public List<WildAnimalModGroupEditorViewModel> ModGroups { get; }
    public RelayCommand AddModGroupCommand { get; }

    public bool Enabled
    {
        get => _enabled;
        set => SetProperty(ref _enabled, value);
    }

    public bool LargeEgg
    {
        get => _largeEgg;
        set => SetProperty(ref _largeEgg, value);
    }

    public bool IsMagic
    {
        get => _isMagic;
        set => SetProperty(ref _isMagic, value);
    }

    public string PriceText
    {
        get => _priceText;
        set => SetProperty(ref _priceText, value);
    }

    public string NewModGroupId
    {
        get => _newModGroupId;
        set => SetProperty(ref _newModGroupId, value);
    }

    public string ModGroupSummary
        => ModGroups.Count == 0
            ? "(none)"
            : string.Join(Environment.NewLine, ModGroups.Select(group => $"{group.ModId} | {group.KeysText}"));

    public bool TryBuildDraft(out WildAnimalDraft draft, out string error)
    {
        if (!int.TryParse(PriceText, out var parsedPrice))
        {
            draft = Draft;
            error = $"Price for {Key} must be a whole number.";
            return false;
        }

        draft = new WildAnimalDraft(
            Draft.Key,
            Draft.BaseEnabled,
            Enabled,
            Draft.BaseLargeEgg,
            LargeEgg,
            Draft.BasePrice,
            parsedPrice,
            Draft.BaseIsMagic,
            IsMagic,
            LocationTypes.Select(location => new WildAnimalDraftLocationType(location.Key, location.BaseAllowed, location.Allowed)).ToList(),
            ModGroups
                .Where(group => !string.IsNullOrWhiteSpace(group.ModId))
                .Select(group => new WildAnimalDraftModGroup(
                    group.ModId,
                    group.BaseKeys.ToList(),
                    WildAnimalsEditorHelpers.ParseCsvKeyList(group.KeysText).ToList()))
                .ToList());

        error = "";
        return true;
    }

    private void AddModGroup()
    {
        var modId = NewModGroupId.Trim();
        if (modId.Length == 0 || ModGroups.Any(group => string.Equals(group.ModId, modId, StringComparison.OrdinalIgnoreCase)))
            return;

        ModGroups.Add(new WildAnimalModGroupEditorViewModel(modId, Array.Empty<string>(), Array.Empty<string>()));
        NewModGroupId = "";
        OnPropertyChanged(nameof(ModGroups));
        OnPropertyChanged(nameof(ModGroupSummary));
    }
}

public sealed class WildAnimalLocationTypeEditorViewModel : ObservableObject
{
    private bool _allowed;

    public WildAnimalLocationTypeEditorViewModel(string key, bool baseAllowed, bool allowed)
    {
        Key = key;
        BaseAllowed = baseAllowed;
        _allowed = allowed;
    }

    public string Key { get; }
    public bool BaseAllowed { get; }
    public string Label => BaseAllowed ? $"{Key} (default on)" : $"{Key} (default off)";

    public bool Allowed
    {
        get => _allowed;
        set => SetProperty(ref _allowed, value);
    }
}

public sealed class WildAnimalModGroupEditorViewModel : ObservableObject
{
    private string _keysText;

    public WildAnimalModGroupEditorViewModel(string modId, IReadOnlyList<string> baseKeys, IReadOnlyList<string> keys)
    {
        ModId = modId;
        BaseKeys = baseKeys.ToList();
        _keysText = string.Join(", ", keys);
    }

    public string ModId { get; }
    public IReadOnlyList<string> BaseKeys { get; }
    public string BaseKeysText => BaseKeys.Count == 0 ? "(none)" : string.Join(", ", BaseKeys);

    public string KeysText
    {
        get => _keysText;
        set => SetProperty(ref _keysText, value);
    }
}
