using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using LetMeHatch.Core.Cloning;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace LetMeHatch.Integrations.StardewUi.Colors;

public sealed class LmhColorsEditorViewModel : INotifyPropertyChanged
{
    private readonly LmhColorsEditorService _service;
    private readonly Func<LmhColorsEditorCatalog> _loadCatalog;
    private readonly Action<LmhColorsEditorDraft> _saveDraft;
    private readonly Func<string, Color, Tuple<Texture2D, Rectangle>?>? _loadPreviewSprite;
    private readonly Func<string, Tuple<Texture2D, Rectangle>?>? _loadTilePreviewSprite;
    private static readonly Dictionary<uint, Tuple<Texture2D, Rectangle>> SwatchCache = new();

    private LmhColorsEditorCatalog _catalog = new(Array.Empty<LmhColorPaletteGroupRecord>(), Array.Empty<LmhColorOverrideRecord>(), Array.Empty<LmhColorOverrideRecord>());
    private LmhColorsEditorDraft _draft = new(
        new Dictionary<string, Dictionary<string, bool>>(StringComparer.OrdinalIgnoreCase),
        new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase));
    private string? _selectedPaletteGroupKey;
    private string? _selectedSourceEggId;
    private string? _selectedClonedEggId;
    private string _selectedOverrideColorText = string.Empty;
    private Color _selectedOverrideColor = Color.Transparent;
    private bool _syncingSelectedColor;
    private Tuple<Texture2D, Rectangle>? _previewSprite;
    private bool _isBrowsing = true;
    private bool _isSourceEggMode = true;
    private IReadOnlyList<LmhColorOverrideItemViewModel> _overrideRows = Array.Empty<LmhColorOverrideItemViewModel>();

    public event PropertyChangedEventHandler? PropertyChanged;

    public LmhColorsEditorViewModel(
        LmhColorsEditorService service,
        Func<LmhColorsEditorCatalog> loadCatalog,
        Action<LmhColorsEditorDraft> saveDraft,
        Func<string, Color, Tuple<Texture2D, Rectangle>?>? loadPreviewSprite = null,
        Func<string, Tuple<Texture2D, Rectangle>?>? loadTilePreviewSprite = null)
    {
        _service = service;
        _loadCatalog = loadCatalog;
        _saveDraft = saveDraft;
        _loadPreviewSprite = loadPreviewSprite;
        _loadTilePreviewSprite = loadTilePreviewSprite;
        Reload();
    }

    public string Title => "Colors Editor";
    public string SourceEggsTabText => "Source Eggs";
    public string ClonedEggsTabText => "Cloned Eggs";
    public bool IsSourceEggMode => _isSourceEggMode;
    public bool IsClonedEggMode => !_isSourceEggMode;
    public string SelectedPaletteGroup => SelectedPaletteGroupRecord?.DisplayName ?? string.Empty;
    public string SelectedEggId => SelectedOverrideRecord?.EggId ?? string.Empty;
    public bool IsBrowsing => _isBrowsing;
    public bool IsShowingDetail => !_isBrowsing && SelectedOverrideRecord is not null;
    public string PreviewLabel => SelectedOverrideRecord?.Label ?? "Select an egg";
    public string PreviewStatusText
    {
        get
        {
            if (SelectedOverrideRecord is null)
                return $"Eggs: {OverrideRows.Count}";

            if (IsSourceEggMode)
            {
                var selectedGroups = GetPaletteMap(SelectedOverrideRecord.EggId)
                    .Where(x => x.Value)
                    .Select(x => x.Key)
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase);
                var groupsText = string.Join(", ", selectedGroups);
                return $"Automatic palette groups: {groupsText}";
            }

            var colorText = _selectedOverrideColor == Color.Transparent ? "none" : LmhColorsEditorService.FormatColor(_selectedOverrideColor);
            return $"Manual override color: {colorText}";
        }
    }

    public string SelectedEggSummaryText
        => SelectedOverrideRecord is null
            ? (IsSourceEggMode
                ? "Select a source egg to edit the palette groups used for automatic clone coloring."
                : "Select a cloned egg to edit its explicit manual color override.")
            : $"Egg: {PreviewLabel} | Egg Id: {SelectedEggId}";

    public string SelectedPaletteGroupSummaryText => $"Selected group: {SelectedPaletteGroup}";

    public string SelectedColorPreviewText
        => _selectedOverrideColor == Color.Transparent
            ? "Preview color: none"
            : $"Preview color: {LmhColorsEditorService.FormatColor(_selectedOverrideColor)}";

    public Tuple<Texture2D, Rectangle>? PreviewSprite
    {
        get => _previewSprite;
        private set => SetPreviewSprite(value);
    }

    public bool HasPreviewSprite => PreviewSprite is not null;
    public bool MissingPreviewSprite => !HasPreviewSprite;

    public string EnabledPaletteGroupsText
    {
        get
        {
            if (SelectedOverrideRecord is null || IsClonedEggMode)
                return string.Empty;

            var groups = GetPaletteMap(SelectedOverrideRecord.EggId)
                .Where(x => x.Value)
                .Select(x => x.Key)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();
            var text = groups.Count == 0 ? "(none)" : string.Join(", ", groups);
            return $"Enabled groups: {text}";
        }
    }

    public IReadOnlyList<LmhColorPaletteGroupItemViewModel> PaletteGroups { get; private set; } = Array.Empty<LmhColorPaletteGroupItemViewModel>();
    public IReadOnlyList<LmhColorOverrideItemViewModel> OverrideRows
    {
        get => _overrideRows;
        private set
        {
            if (ReferenceEquals(_overrideRows, value))
                return;

            _overrideRows = value;
            OnPropertyChanged(nameof(OverrideRows));
        }
    }
    public IReadOnlyList<Color> PresetColors => _catalog.PaletteGroups.Select(x => x.RepresentativeColor).Where(x => x != Color.Transparent).ToList();
    public bool CanSave => SelectedOverrideRecord is not null && !HasValidationErrors && HasChanges;
    public bool CanDiscard => SelectedOverrideRecord is not null && HasChanges;
    public bool CanOpenPreviousEgg => GetSelectedOverrideIndex() > 0;
    public bool CanOpenNextEgg => GetSelectedOverrideIndex() >= 0 && GetSelectedOverrideIndex() < OverrideRows.Count - 1;

    public Color SelectedOverrideColor
    {
        get => _selectedOverrideColor;
        set
        {
            if (_selectedOverrideColor == value)
                return;

            _selectedOverrideColor = value;
            OnPropertyChanged(nameof(SelectedOverrideColor));
            OnPropertyChanged(nameof(SelectedColorPreviewText));
            OnPropertyChanged(nameof(CanSave));
            OnPropertyChanged(nameof(PreviewStatusText));

            if (_syncingSelectedColor)
                return;

            _syncingSelectedColor = true;
            _selectedOverrideColorText = LmhColorsEditorService.FormatColor(value);
            OnPropertyChanged(nameof(SelectedOverrideColorText));
            _syncingSelectedColor = false;

            RefreshPreviewSprite();
        }
    }

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

            if (_syncingSelectedColor)
                return;

            if (LmhColorsEditorService.TryParseColor(value, out var color))
            {
                _syncingSelectedColor = true;
                _selectedOverrideColor = color;
                OnPropertyChanged(nameof(SelectedOverrideColor));
                OnPropertyChanged(nameof(SelectedColorPreviewText));
                _syncingSelectedColor = false;
                RefreshPreviewSprite();
            }
        }
    }

    private LmhColorPaletteGroupRecord? SelectedPaletteGroupRecord => _catalog.PaletteGroups.FirstOrDefault(x => x.Key == _selectedPaletteGroupKey);
    private IReadOnlyList<LmhColorOverrideRecord> ActiveRecords => IsSourceEggMode ? _catalog.SourceEggs : _catalog.ClonedEggs;
    private string? SelectedRecordEggId => IsSourceEggMode ? _selectedSourceEggId : _selectedClonedEggId;
    private LmhColorOverrideRecord? SelectedOverrideRecord => ActiveRecords.FirstOrDefault(x => x.EggId == SelectedRecordEggId);

    private bool HasValidationErrors => SelectedOverrideRecord is not null && !LmhColorsEditorService.TryParseColor(SelectedOverrideColorText, out _);

    private bool HasChanges
    {
        get
        {
            foreach (var record in _catalog.SourceEggs)
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
            }

            foreach (var record in _catalog.ClonedEggs)
            {
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

        if (IsClonedEggMode)
        {
            var colors = new Dictionary<string, Color>(_draft.OverrideColors, StringComparer.OrdinalIgnoreCase)
            {
                [SelectedOverrideRecord.EggId] = SelectedOverrideColor
            };
            _draft = _draft with { OverrideColors = colors };
        }

        _saveDraft(_draft);
        Reload();
    }

    public void Discard() => Reload();

    public void BackToBrowser()
    {
        if (_isBrowsing)
            return;

        _isBrowsing = true;
        NotifyAll();
    }

    public void OpenPreviousEgg() => OpenAdjacentEgg(-1);
    public void OpenNextEgg() => OpenAdjacentEgg(1);

    public void Reload()
    {
        _catalog = _loadCatalog();
        _draft = _service.CreateDraft(_catalog);
        _selectedPaletteGroupKey ??= _catalog.PaletteGroups.FirstOrDefault()?.Key;
        _selectedSourceEggId = null;
        _selectedClonedEggId = null;
        _isBrowsing = true;
        SyncSelectedOverrideColorText();
        RebuildLists();
        NotifyAll();
    }

    public void ShowSourceEggs()
    {
        if (_isSourceEggMode)
            return;

        _isSourceEggMode = true;
        _isBrowsing = true;
        SyncSelectedOverrideColorText();
        RebuildLists();
        NotifyAll();
    }

    public void ShowClonedEggs()
    {
        if (!_isSourceEggMode)
            return;

        _isSourceEggMode = false;
        _isBrowsing = true;
        SyncSelectedOverrideColorText();
        RebuildLists();
        NotifyAll();
    }

    public void ToggleSelectedPaletteGroup()
    {
        if (SelectedOverrideRecord is null || SelectedPaletteGroupRecord is null || IsClonedEggMode)
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

        var selection = new Dictionary<string, Dictionary<string, bool>>(_draft.PaletteSelections, StringComparer.OrdinalIgnoreCase);
        if (!SelectedOverrideRecord.IsClonedEgg)
        {
            selection[SelectedOverrideRecord.EggId] = new Dictionary<string, bool>(SelectedOverrideRecord.EnabledPaletteGroups, StringComparer.OrdinalIgnoreCase);
        }

        var colors = new Dictionary<string, Color>(_draft.OverrideColors, StringComparer.OrdinalIgnoreCase);
        if (SelectedOverrideRecord.IsClonedEgg)
            colors[SelectedOverrideRecord.EggId] = SelectedOverrideRecord.OverrideColor;

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
            SelectedOverrideColor = Color.Transparent;
            PreviewSprite = null;
            return;
        }

        var color = SelectedOverrideRecord.IsClonedEgg
            && _draft.OverrideColors.TryGetValue(SelectedOverrideRecord.EggId, out var selectedColor)
                ? selectedColor
                : Color.Transparent;

        SelectedOverrideColor = color;
        RefreshPreviewSprite();
    }

    private void RefreshPreviewSprite()
    {
        if (_loadPreviewSprite is null || SelectedOverrideRecord is null)
        {
            PreviewSprite = null;
            return;
        }

        PreviewSprite = _loadPreviewSprite(SelectedOverrideRecord.EggId, SelectedOverrideColor);
    }

    private void RebuildLists()
    {
        PaletteGroups = _catalog.PaletteGroups
            .Select(group => new LmhColorPaletteGroupItemViewModel(
                group.Key,
                group.DisplayName,
                $"{_catalog.SourceEggs.Count(x => GetPaletteMap(x.EggId).TryGetValue(group.Key, out var enabled) && enabled)} eggs enabled",
                CreateSwatchSprite(group.RepresentativeColor),
                () =>
                {
                    _selectedPaletteGroupKey = group.Key;
                    NotifyAll();
                }))
            .ToList();

        OverrideRows = ActiveRecords
            .Select(row => new LmhColorOverrideItemViewModel(
                row.EggId,
                row.Label,
                BuildRowDescription(row),
                _loadTilePreviewSprite?.Invoke(row.EggId),
                () =>
                {
                    if (row.IsClonedEgg)
                        _selectedClonedEggId = row.EggId;
                    else
                        _selectedSourceEggId = row.EggId;
                    _isBrowsing = false;
                    SyncSelectedOverrideColorText();
                    NotifyAll();
                },
                ResetOverride))
            .ToList();
    }

    private string BuildRowDescription(LmhColorOverrideRecord row)
    {
        if (row.IsClonedEgg)
        {
            var color = _draft.OverrideColors.TryGetValue(row.EggId, out var overrideColor) ? LmhColorsEditorService.FormatColor(overrideColor) : string.Empty;
            return string.IsNullOrWhiteSpace(color)
                ? "Manual override: none"
                : $"Manual override: {color}";
        }

        var groupCount = GetPaletteMap(row.EggId).Count(x => x.Value);
        return $"{groupCount} palette groups enabled";
    }

    private void NotifyAll()
    {
        OnPropertyChanged(nameof(SourceEggsTabText));
        OnPropertyChanged(nameof(ClonedEggsTabText));
        OnPropertyChanged(nameof(IsSourceEggMode));
        OnPropertyChanged(nameof(IsClonedEggMode));
        OnPropertyChanged(nameof(SelectedPaletteGroup));
        OnPropertyChanged(nameof(SelectedEggId));
        OnPropertyChanged(nameof(IsBrowsing));
        OnPropertyChanged(nameof(IsShowingDetail));
        OnPropertyChanged(nameof(SelectedEggSummaryText));
        OnPropertyChanged(nameof(SelectedPaletteGroupSummaryText));
        OnPropertyChanged(nameof(SelectedColorPreviewText));
        OnPropertyChanged(nameof(HasPreviewSprite));
        OnPropertyChanged(nameof(MissingPreviewSprite));
        OnPropertyChanged(nameof(EnabledPaletteGroupsText));
        OnPropertyChanged(nameof(PreviewLabel));
        OnPropertyChanged(nameof(PreviewStatusText));
        OnPropertyChanged(nameof(PaletteGroups));
        OnPropertyChanged(nameof(OverrideRows));
        OnPropertyChanged(nameof(PresetColors));
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(CanDiscard));
        OnPropertyChanged(nameof(CanOpenPreviousEgg));
        OnPropertyChanged(nameof(CanOpenNextEgg));
        OnPropertyChanged(nameof(SelectedOverrideColor));
        OnPropertyChanged(nameof(SelectedOverrideColorText));
    }

    private int GetSelectedOverrideIndex()
    {
        if (SelectedOverrideRecord is null)
            return -1;

        for (var i = 0; i < OverrideRows.Count; i++)
        {
            if (string.Equals(OverrideRows[i].EggId, SelectedOverrideRecord.EggId, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    private void OpenAdjacentEgg(int delta)
    {
        var currentIndex = GetSelectedOverrideIndex();
        if (currentIndex < 0 || OverrideRows.Count == 0)
            return;

        var nextIndex = currentIndex + delta;
        if (nextIndex < 0)
            nextIndex = OverrideRows.Count - 1;
        else if (nextIndex >= OverrideRows.Count)
            nextIndex = 0;

        if (IsClonedEggMode)
            _selectedClonedEggId = OverrideRows[nextIndex].EggId;
        else
            _selectedSourceEggId = OverrideRows[nextIndex].EggId;

        _isBrowsing = false;
        SyncSelectedOverrideColorText();
        NotifyAll();
    }

    private bool SetPreviewSprite(Tuple<Texture2D, Rectangle>? value)
    {
        if (ReferenceEquals(_previewSprite, value))
            return false;

        _previewSprite = value;
        OnPropertyChanged(nameof(PreviewSprite));
        OnPropertyChanged(nameof(HasPreviewSprite));
        OnPropertyChanged(nameof(MissingPreviewSprite));
        return true;
    }

    private static Tuple<Texture2D, Rectangle>? CreateSwatchSprite(Color color)
    {
        if (color == Color.Transparent)
            return null;

        if (Game1.graphics?.GraphicsDevice is null)
            return null;

        var key = color.PackedValue;
        if (SwatchCache.TryGetValue(key, out var cached))
            return cached;

        var texture = new Texture2D(Game1.graphics.GraphicsDevice, 16, 16);
        var pixels = new Color[16 * 16];

        for (var i = 0; i < pixels.Length; i++)
            pixels[i] = color;

        texture.SetData(pixels);
        var sprite = Tuple.Create(texture, new Rectangle(0, 0, 16, 16));
        SwatchCache[key] = sprite;
        return sprite;
    }

    private void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed class LmhColorPaletteGroupItemViewModel
{
    private readonly Action _open;

    public LmhColorPaletteGroupItemViewModel(string key, string buttonText, string metaText, Tuple<Texture2D, Rectangle>? swatchSprite, Action open)
    {
        Key = key;
        ButtonText = buttonText;
        MetaText = metaText;
        SwatchSprite = swatchSprite;
        _open = open;
    }

    public string Key { get; }
    public string ButtonText { get; }
    public string MetaText { get; }
    public Tuple<Texture2D, Rectangle>? SwatchSprite { get; }
    public bool HasSwatchSprite => SwatchSprite is not null;
    public bool MissingSwatchSprite => !HasSwatchSprite;

    public void OpenPaletteGroup() => _open();
}

public sealed class LmhColorOverrideItemViewModel
{
    private readonly Action _select;
    private readonly Action _reset;

    public LmhColorOverrideItemViewModel(
        string eggId,
        string buttonText,
        string descriptionText,
        Tuple<Texture2D, Rectangle>? previewSprite,
        Action select,
        Action reset)
    {
        EggId = eggId;
        ButtonText = buttonText;
        DescriptionText = descriptionText;
        PreviewSprite = previewSprite;
        _select = select;
        _reset = reset;
    }

    public string EggId { get; }
    public string ButtonText { get; }
    public string DescriptionText { get; }
    public Tuple<Texture2D, Rectangle>? PreviewSprite { get; }
    public bool HasPreviewSprite => PreviewSprite is not null;
    public bool MissingPreviewSprite => !HasPreviewSprite;
    public string TooltipText => $"{ButtonText}\n{DescriptionText}";

    public void SelectOverrideRow() => _select();
    public void ResetOverride() => _reset();
}
