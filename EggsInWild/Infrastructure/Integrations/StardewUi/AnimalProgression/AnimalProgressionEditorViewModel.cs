using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace EggsInWild.Infrastructure.Integrations.StardewUi.AnimalProgression;

public sealed class AnimalProgressionEditorViewModel : INotifyPropertyChanged
{
    private readonly AnimalProgressionEditorService _service;
    private readonly Func<IReadOnlyList<AnimalProgressionAnimalRecord>> _loadCatalog;
    private readonly Action<AnimalProgressionDraft, IReadOnlyList<AnimalProgressionAnimalRecord>> _saveDraft;

    private string _searchText = string.Empty;
    private IReadOnlyList<AnimalProgressionAnimalRecord> _catalog = Array.Empty<AnimalProgressionAnimalRecord>();
    private AnimalProgressionDraft _draft = new(new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase));
    private string? _selectedAnimalId;
    private bool _isBrowsing = true;

    public event PropertyChangedEventHandler? PropertyChanged;

    public AnimalProgressionEditorViewModel(
        AnimalProgressionEditorService service,
        Func<IReadOnlyList<AnimalProgressionAnimalRecord>> loadCatalog,
        Func<AnimalProgressionDraft> loadDraft,
        Action<AnimalProgressionDraft, IReadOnlyList<AnimalProgressionAnimalRecord>> saveDraft)
    {
        _service = service;
        _loadCatalog = loadCatalog;
        _saveDraft = saveDraft;
        _draft = loadDraft();
        Reload();
    }

    public string Title => "Animal Progression Editor";
    public string SummaryText => $"{Animals.Count} visible / {_catalog.Count} total";
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText == value)
                return;
            _searchText = value;
            RebuildAnimals();
            NotifyAll();
        }
    }

    public IReadOnlyList<AnimalProgressionAnimalItemViewModel> Animals { get; private set; } = Array.Empty<AnimalProgressionAnimalItemViewModel>();
    public string SelectedAnimal => SelectedAnimalRecord?.DisplayName ?? string.Empty;
    public bool HasSelectedAnimal => SelectedAnimalRecord is not null;
    public bool IsBrowsing => _isBrowsing;
    public bool IsShowingDetail => !_isBrowsing && SelectedAnimalRecord is not null;
    public string CurrentModeText => GetEffectiveModeText(SelectedAnimalRecord);
    public string DefaultModeText => SelectedAnimalRecord is null ? string.Empty : FormatMode(SelectedAnimalRecord.DefaultEnabled);
    public string EffectiveModeText => GetEffectiveModeText(SelectedAnimalRecord);
    public IReadOnlyList<AnimalProgressionModeOptionViewModel> ModeOptions { get; private set; } = Array.Empty<AnimalProgressionModeOptionViewModel>();
    public bool CanSave => HasChanges;
    public bool CanDiscard => HasChanges;
    public bool CanOpenPreviousAnimal => GetSelectedAnimalIndex() > 0;
    public bool CanOpenNextAnimal => GetSelectedAnimalIndex() >= 0 && GetSelectedAnimalIndex() < Animals.Count - 1;

    private AnimalProgressionAnimalRecord? SelectedAnimalRecord => _catalog.FirstOrDefault(x => x.AnimalId == _selectedAnimalId);

    private bool HasChanges
    {
        get
        {
            foreach (var animal in _catalog)
            {
                var current = _draft.ConfiguredModes.TryGetValue(animal.AnimalId, out var configured)
                    ? configured
                    : animal.DefaultEnabled;
                if (current != animal.ConfiguredEnabled)
                    return true;
            }

            return false;
        }
    }

    public void Save()
    {
        _saveDraft(_draft, _catalog);
        Reload();
    }

    public void Discard()
        => Reload();

    public void Reload()
    {
        _catalog = _loadCatalog();
        if (_selectedAnimalId is not null && !_catalog.Any(x => x.AnimalId == _selectedAnimalId))
            _selectedAnimalId = null;
        RebuildAnimals();
        RebuildModeOptions();
        NotifyAll();
    }

    public void BackToBrowser()
    {
        if (_isBrowsing)
            return;

        _isBrowsing = true;
        NotifyAll();
    }

    public void OpenPreviousAnimal() => OpenAdjacentAnimal(-1);

    public void OpenNextAnimal() => OpenAdjacentAnimal(1);

    private void RebuildAnimals()
    {
        var query = _searchText?.Trim() ?? string.Empty;
        var filtered = _catalog.Where(record =>
            string.IsNullOrWhiteSpace(query)
            || record.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase)
            || record.AnimalId.Contains(query, StringComparison.OrdinalIgnoreCase));

        Animals = filtered
            .Select(record => new AnimalProgressionAnimalItemViewModel(
                record.AnimalId,
                record.DisplayName,
                $"Default: {FormatMode(record.DefaultEnabled)} | Current: {GetConfiguredValue(record)}",
                () =>
                {
                    _selectedAnimalId = record.AnimalId;
                    _isBrowsing = false;
                    RebuildModeOptions();
                    NotifyAll();
                }))
            .ToList();
    }

    private void RebuildModeOptions()
    {
        if (SelectedAnimalRecord is null)
        {
            ModeOptions = Array.Empty<AnimalProgressionModeOptionViewModel>();
            return;
        }

        ModeOptions = new[]
        {
            new AnimalProgressionModeOptionViewModel(
                "Use default",
                $"Uses the default state: {FormatMode(SelectedAnimalRecord.DefaultEnabled)}",
                () => SetSelectedMode(SelectedAnimalRecord.DefaultEnabled, useDefault: true)),
            new AnimalProgressionModeOptionViewModel(
                "Force enabled",
                "Always keep this animal enabled for progression.",
                () => SetSelectedMode(true, useDefault: false)),
            new AnimalProgressionModeOptionViewModel(
                "Force disabled",
                "Always gate this animal behind progression.",
                () => SetSelectedMode(false, useDefault: false)),
        };
    }

    private void SetSelectedMode(bool value, bool useDefault)
    {
        if (SelectedAnimalRecord is null)
            return;

        var updated = new Dictionary<string, bool>(_draft.ConfiguredModes, StringComparer.OrdinalIgnoreCase);
        if (useDefault)
            updated.Remove(SelectedAnimalRecord.AnimalId);
        else
            updated[SelectedAnimalRecord.AnimalId] = value;

        _draft = new AnimalProgressionDraft(updated);
        RebuildAnimals();
        NotifyAll();
    }

    private string GetConfiguredValue(AnimalProgressionAnimalRecord record)
    {
        if (_draft.ConfiguredModes.TryGetValue(record.AnimalId, out var configured))
            return FormatMode(configured);

        return $"Default ({FormatMode(record.DefaultEnabled)})";
    }

    private static string GetEffectiveModeText(AnimalProgressionAnimalRecord? record)
        => record is null ? string.Empty : FormatMode(record.EffectiveEnabled);

    private static string FormatMode(bool enabled)
        => enabled ? "Enabled" : "Disabled";

    private void NotifyAll()
    {
        OnPropertyChanged(nameof(SummaryText));
        OnPropertyChanged(nameof(Animals));
        OnPropertyChanged(nameof(SelectedAnimal));
        OnPropertyChanged(nameof(HasSelectedAnimal));
        OnPropertyChanged(nameof(IsBrowsing));
        OnPropertyChanged(nameof(IsShowingDetail));
        OnPropertyChanged(nameof(CurrentModeText));
        OnPropertyChanged(nameof(DefaultModeText));
        OnPropertyChanged(nameof(EffectiveModeText));
        OnPropertyChanged(nameof(ModeOptions));
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(CanDiscard));
        OnPropertyChanged(nameof(CanOpenPreviousAnimal));
        OnPropertyChanged(nameof(CanOpenNextAnimal));
    }

    private int GetSelectedAnimalIndex()
    {
        if (SelectedAnimalRecord is null)
            return -1;

        for (var i = 0; i < Animals.Count; i++)
        {
            if (string.Equals(Animals[i].AnimalId, SelectedAnimalRecord.AnimalId, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    private void OpenAdjacentAnimal(int delta)
    {
        var currentIndex = GetSelectedAnimalIndex();
        if (currentIndex < 0)
            return;

        var nextIndex = currentIndex + delta;
        if (nextIndex < 0 || nextIndex >= Animals.Count)
            return;

        _selectedAnimalId = Animals[nextIndex].AnimalId;
        _isBrowsing = false;
        RebuildModeOptions();
        NotifyAll();
    }

    private void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed class AnimalProgressionAnimalItemViewModel
{
    private readonly Action _open;

    public AnimalProgressionAnimalItemViewModel(string animalId, string buttonText, string metaText, Action open)
    {
        AnimalId = animalId;
        ButtonText = buttonText;
        MetaText = metaText;
        _open = open;
    }

    public string AnimalId { get; }
    public string ButtonText { get; }
    public string MetaText { get; }

    public void OpenAnimal() => _open();
}

public sealed class AnimalProgressionModeOptionViewModel
{
    private readonly Action _select;

    public AnimalProgressionModeOptionViewModel(string buttonText, string descriptionText, Action select)
    {
        ButtonText = buttonText;
        DescriptionText = descriptionText;
        _select = select;
    }

    public string ButtonText { get; }
    public string DescriptionText { get; }

    public void SelectMode() => _select();
}
