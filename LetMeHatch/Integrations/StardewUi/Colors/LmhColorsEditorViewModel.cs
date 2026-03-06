using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.Xna.Framework;

namespace LetMeHatch.Integrations.StardewUi.Colors;

public sealed class LmhColorsEditorViewModel : INotifyPropertyChanged
{
    private readonly LmhColorsEditorService _service;
    private readonly Func<LmhColorsEditorCatalog> _loadCatalog;
    private readonly Action<LmhColorsEditorDraft> _saveDraft;

    private LmhColorsEditorCatalog _catalog = new(Array.Empty<LmhColorPaletteGroupRecord>(), Array.Empty<LmhColorOverrideRecord>());
    private LmhColorsEditorDraft _draft = new(
        new Dictionary<string, Dictionary<string, bool>>(StringComparer.OrdinalIgnoreCase),
        new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase));
    private string? _selectedPaletteGroupKey;
    private string? _selectedEggId;
    private string _selectedOverrideColorText = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public LmhColorsEditorViewModel(
        LmhColorsEditorService service,
        Func<LmhColorsEditorCatalog> loadCatalog,
        Action<LmhColorsEditorDraft> saveDraft)
    {
        _service = service;
        _loadCatalog = loadCatalog;
        _saveDraft = saveDraft;
        Reload();
    }

    public string Title => "Colors Editor";
    public string SelectedPaletteGroup => SelectedPaletteGroupRecord?.DisplayName ?? string.Empty;
    public string PreviewLabel => SelectedOverrideRecord?.Label ?? "Select an egg";
    public string PreviewStatusText
    {
        get
        {
            if (SelectedOverrideRecord is null)
                return $"Eggs: {OverrideRows.Count}";

            var selectedGroups = GetPaletteMap(SelectedOverrideRecord.EggId)
                .Where(x => x.Value)
                .Select(x => x.Key)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase);
            var groupsText = string.Join(", ", selectedGroups);
            var colorText = string.IsNullOrWhiteSpace(SelectedOverrideColorText) ? "none" : SelectedOverrideColorText;
            return $"Palette groups: {groupsText} | Override color: {colorText}";
        }
    }

    public IReadOnlyList<LmhColorPaletteGroupItemViewModel> PaletteGroups { get; private set; } = Array.Empty<LmhColorPaletteGroupItemViewModel>();
    public IReadOnlyList<LmhColorOverrideItemViewModel> OverrideRows { get; private set; } = Array.Empty<LmhColorOverrideItemViewModel>();
    public bool CanSave => SelectedOverrideRecord is not null && !HasValidationErrors && HasChanges;
    public bool CanDiscard => SelectedOverrideRecord is not null && HasChanges;
    public string SelectedOverrideColorText
    {
        get => _selectedOverrideColorText;
        set
        {
            if (_selectedOverrideColorText == value)
                return;

            _selectedOverrideColorText = value;
            OnPropertyChanged(nameof(SelectedOverrideColorText));
            OnPropertyChanged(nameof(CanSave));
            OnPropertyChanged(nameof(PreviewStatusText));
        }
    }

    private LmhColorPaletteGroupRecord? SelectedPaletteGroupRecord => _catalog.PaletteGroups.FirstOrDefault(x => x.Key == _selectedPaletteGroupKey);
    private LmhColorOverrideRecord? SelectedOverrideRecord => _catalog.Overrides.FirstOrDefault(x => x.EggId == _selectedEggId);

    private bool HasValidationErrors
        => SelectedOverrideRecord is not null && !LmhColorsEditorService.TryParseColor(SelectedOverrideColorText, out _);

    private bool HasChanges
    {
        get
        {
            foreach (var record in _catalog.Overrides)
            {
                var originalMap = record.EnabledPaletteGroups;
                var currentMap = GetPaletteMap(record.EggId);
                if (originalMap.Count != currentMap.Count)
                    return true;

                foreach (var entry in originalMap)
                {
                    if (!currentMap.TryGetValue(entry.Key, out var current) || current != entry.Value)
                        return true;
                }

                var originalColor = record.OverrideColor;
                var currentColor = _draft.OverrideColors.TryGetValue(record.EggId, out var configured) ? configured : Color.Transparent;
                if (originalColor != currentColor)
                    return true;
            }

            return false;
        }
    }

    public void Save()
    {
        if (SelectedOverrideRecord is null || HasValidationErrors)
            return;

        if (LmhColorsEditorService.TryParseColor(SelectedOverrideColorText, out var color))
        {
            var colors = new Dictionary<string, Color>(_draft.OverrideColors, StringComparer.OrdinalIgnoreCase)
            {
                [SelectedOverrideRecord.EggId] = color
            };
            _draft = _draft with { OverrideColors = colors };
        }

        _saveDraft(_draft);
        Reload();
    }

    public void Discard()
        => Reload();

    public void Reload()
    {
        _catalog = _loadCatalog();
        _draft = _service.CreateDraft(_catalog);
        _selectedPaletteGroupKey ??= _catalog.PaletteGroups.FirstOrDefault()?.Key;
        _selectedEggId ??= _catalog.Overrides.FirstOrDefault()?.EggId;
        SyncSelectedOverrideColorText();
        RebuildLists();
        NotifyAll();
    }

    public void ToggleSelectedPaletteGroup()
    {
        if (SelectedOverrideRecord is null || SelectedPaletteGroupRecord is null)
            return;

        var map = new Dictionary<string, bool>(GetPaletteMap(SelectedOverrideRecord.EggId), StringComparer.OrdinalIgnoreCase);
        map[SelectedPaletteGroupRecord.Key] = !map.TryGetValue(SelectedPaletteGroupRecord.Key, out var enabled) || !enabled;

        var selection = new Dictionary<string, Dictionary<string, bool>>(_draft.PaletteSelections, StringComparer.OrdinalIgnoreCase)
        {
            [SelectedOverrideRecord.EggId] = map
        };
        _draft = _draft with { PaletteSelections = selection };
        RebuildLists();
        NotifyAll();
    }

    public void ResetOverride()
    {
        if (SelectedOverrideRecord is null)
            return;

        var selection = new Dictionary<string, Dictionary<string, bool>>(_draft.PaletteSelections, StringComparer.OrdinalIgnoreCase)
        {
            [SelectedOverrideRecord.EggId] = new Dictionary<string, bool>(SelectedOverrideRecord.EnabledPaletteGroups, StringComparer.OrdinalIgnoreCase)
        };
        var colors = new Dictionary<string, Color>(_draft.OverrideColors, StringComparer.OrdinalIgnoreCase)
        {
            [SelectedOverrideRecord.EggId] = SelectedOverrideRecord.OverrideColor
        };
        _draft = _draft with { PaletteSelections = selection, OverrideColors = colors };
        SyncSelectedOverrideColorText();
        RebuildLists();
        NotifyAll();
    }

    private Dictionary<string, bool> GetPaletteMap(string eggId)
        => _draft.PaletteSelections.TryGetValue(eggId, out var map)
            ? map
            : new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

    private void SyncSelectedOverrideColorText()
    {
        if (SelectedOverrideRecord is null)
        {
            SelectedOverrideColorText = string.Empty;
            return;
        }

        var color = _draft.OverrideColors.TryGetValue(SelectedOverrideRecord.EggId, out var selectedColor)
            ? selectedColor
            : Color.Transparent;
        SelectedOverrideColorText = LmhColorsEditorService.FormatColor(color);
    }

    private void RebuildLists()
    {
        PaletteGroups = _catalog.PaletteGroups
            .Select(group => new LmhColorPaletteGroupItemViewModel(
                group.Key,
                group.DisplayName,
                $"{_catalog.Overrides.Count(x => GetPaletteMap(x.EggId).TryGetValue(group.Key, out var enabled) && enabled)} eggs enabled",
                () =>
                {
                    _selectedPaletteGroupKey = group.Key;
                    NotifyAll();
                }))
            .ToList();

        OverrideRows = _catalog.Overrides
            .Select(row => new LmhColorOverrideItemViewModel(
                row.EggId,
                row.Label,
                BuildRowDescription(row),
                () =>
                {
                    _selectedEggId = row.EggId;
                    SyncSelectedOverrideColorText();
                    NotifyAll();
                },
                ResetOverride))
            .ToList();
    }

    private string BuildRowDescription(LmhColorOverrideRecord row)
    {
        var groupCount = GetPaletteMap(row.EggId).Count(x => x.Value);
        var color = _draft.OverrideColors.TryGetValue(row.EggId, out var overrideColor) ? LmhColorsEditorService.FormatColor(overrideColor) : string.Empty;
        return string.IsNullOrWhiteSpace(color)
            ? $"{groupCount} palette groups enabled"
            : $"{groupCount} palette groups enabled | {color}";
    }

    private void NotifyAll()
    {
        OnPropertyChanged(nameof(SelectedPaletteGroup));
        OnPropertyChanged(nameof(PreviewLabel));
        OnPropertyChanged(nameof(PreviewStatusText));
        OnPropertyChanged(nameof(PaletteGroups));
        OnPropertyChanged(nameof(OverrideRows));
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(CanDiscard));
        OnPropertyChanged(nameof(SelectedOverrideColorText));
    }

    private void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed class LmhColorPaletteGroupItemViewModel
{
    private readonly Action _open;

    public LmhColorPaletteGroupItemViewModel(string key, string buttonText, string metaText, Action open)
    {
        Key = key;
        ButtonText = buttonText;
        MetaText = metaText;
        _open = open;
    }

    public string Key { get; }
    public string ButtonText { get; }
    public string MetaText { get; }

    public void OpenPaletteGroup() => _open();
}

public sealed class LmhColorOverrideItemViewModel
{
    private readonly Action _select;
    private readonly Action _reset;

    public LmhColorOverrideItemViewModel(string eggId, string buttonText, string descriptionText, Action select, Action reset)
    {
        EggId = eggId;
        ButtonText = buttonText;
        DescriptionText = descriptionText;
        _select = select;
        _reset = reset;
    }

    public string EggId { get; }
    public string ButtonText { get; }
    public string DescriptionText { get; }

    public void SelectOverrideRow() => _select();
    public void ResetOverride() => _reset();
}
