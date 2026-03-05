using System;
using System.Collections.Generic;
using System.Linq;
using EggsInWild.Infrastructure.Config;
using Mikew.NestCore.Helpers.UI;

namespace EggsInWild.Infrastructure.Integrations.StardewUi.WildAnimals;

public sealed class WildAnimalsEditorViewModel : ObservableObject
{
    private readonly WildAnimalsEditorService _service;
    private readonly Func<ModConfig> _getConfig;
    private readonly Action<ModConfig, WildAnimalDraft> _saveDraft;

    private string? _selectedKey;
    private string _statusText = "Select a wild animal to edit.";
    private WildAnimalDraftViewModel? _selectedAnimal;
    private IReadOnlyList<WildAnimalGroupViewModel> _animalGroups = Array.Empty<WildAnimalGroupViewModel>();

    public WildAnimalsEditorViewModel(
        WildAnimalsEditorService service,
        Func<ModConfig> getConfig,
        Action<ModConfig, WildAnimalDraft> saveDraft)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _getConfig = getConfig ?? throw new ArgumentNullException(nameof(getConfig));
        _saveDraft = saveDraft ?? throw new ArgumentNullException(nameof(saveDraft));

        SaveCommand = new RelayCommand(SaveSelectedAnimal, () => SelectedAnimal is not null);
        DiscardCommand = new RelayCommand(DiscardSelectedAnimal, () => SelectedAnimal is not null);
        ReloadCommand = new RelayCommand(ReloadCatalog);

        ReloadCatalog();
    }

    public IReadOnlyList<WildAnimalGroupViewModel> AnimalGroups
    {
        get => _animalGroups;
        private set => SetProperty(ref _animalGroups, value);
    }

    public WildAnimalDraftViewModel? SelectedAnimal
    {
        get => _selectedAnimal;
        private set
        {
            if (!SetProperty(ref _selectedAnimal, value))
                return;

            OnPropertyChanged(nameof(HasSelectedAnimal));
            ((RelayCommand)SaveCommand).NotifyCanExecuteChanged();
            ((RelayCommand)DiscardCommand).NotifyCanExecuteChanged();
        }
    }

    public bool HasSelectedAnimal => SelectedAnimal is not null;

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public RelayCommand SaveCommand { get; }
    public RelayCommand DiscardCommand { get; }
    public RelayCommand ReloadCommand { get; }

    private void ReloadCatalog()
    {
        var catalog = _service.LoadCatalog();
        var groups = catalog.Animals
            .GroupBy(animal => GetGroupKey(animal.Key))
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => new WildAnimalGroupViewModel(
                group.Key,
                group.Select(animal => new WildAnimalListItemViewModel(
                    animal.Key,
                    animal.EffectiveEnabled,
                    new RelayCommand(() => SelectAnimal(animal.Key))))
                .ToList()))
            .ToList();

        AnimalGroups = groups;

        if (_selectedKey is not null && catalog.Animals.Any(animal => string.Equals(animal.Key, _selectedKey, StringComparison.OrdinalIgnoreCase)))
        {
            SelectedAnimal = new WildAnimalDraftViewModel(_service.CreateDraft(_selectedKey));
            StatusText = $"Reloaded {_selectedKey}.";
            return;
        }

        var first = catalog.Animals.FirstOrDefault();
        if (first is null)
        {
            SelectedAnimal = null;
            StatusText = "No wild animals were found in WildAnimalsList.";
            return;
        }

        SelectAnimal(first.Key);
    }

    private void SelectAnimal(string key)
    {
        _selectedKey = key;
        SelectedAnimal = new WildAnimalDraftViewModel(_service.CreateDraft(key));
        StatusText = $"Editing {key}.";
    }

    private void SaveSelectedAnimal()
    {
        if (SelectedAnimal is null)
            return;

        if (!SelectedAnimal.TryBuildDraft(out var draft, out var error))
        {
            StatusText = error;
            return;
        }

        var config = _getConfig();
        _saveDraft(config, draft);
        _selectedKey = draft.Key;
        SelectedAnimal = new WildAnimalDraftViewModel(_service.CreateDraft(draft.Key));
        StatusText = $"Saved {draft.Key}.";
    }

    private void DiscardSelectedAnimal()
    {
        if (SelectedAnimal is null)
            return;

        var key = SelectedAnimal.Key;
        SelectedAnimal = new WildAnimalDraftViewModel(_service.DiscardDraft(key));
        StatusText = $"Discarded unsaved changes for {key}.";
    }

    private static string GetGroupKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return "Other";

        var first = char.ToUpperInvariant(key.Trim()[0]);
        if (first is >= 'A' and <= 'F') return "A-F";
        if (first is >= 'G' and <= 'L') return "G-L";
        if (first is >= 'M' and <= 'R') return "M-R";
        if (first is >= 'S' and <= 'Z') return "S-Z";
        return "Other";
    }
}

public sealed record WildAnimalGroupViewModel(string Title, IReadOnlyList<WildAnimalListItemViewModel> Animals);

public sealed record WildAnimalListItemViewModel(string Key, bool Enabled, RelayCommand Open)
{
    public string Label => Enabled ? Key : $"{Key} (disabled)";
}
