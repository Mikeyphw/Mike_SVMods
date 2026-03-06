using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace LetMeHatch.Integrations.StardewUi.Colors;

public sealed record LmhColorsEditorCatalog(
    IReadOnlyList<LmhColorPaletteGroupRecord> PaletteGroups,
    IReadOnlyList<LmhColorOverrideRecord> Overrides);

public sealed record LmhColorPaletteGroupRecord(
    string Key,
    string DisplayName);

public sealed record LmhColorOverrideRecord(
    string EggId,
    string Label,
    IReadOnlyDictionary<string, bool> EnabledPaletteGroups,
    Color OverrideColor);

public sealed record LmhColorsEditorDraft(
    IReadOnlyDictionary<string, Dictionary<string, bool>> PaletteSelections,
    IReadOnlyDictionary<string, Color> OverrideColors);
