using System.Collections.Generic;
using EggsInWild.Infrastructure.Config;
using EggsInWild.Infrastructure.ModData.Models;

namespace EggsInWild.Infrastructure.Integrations.StardewUi.WildAnimals;

public sealed record WildAnimalsEditorCatalog(
    IReadOnlyDictionary<string, WildAnimalJson> Definitions,
    IReadOnlyList<WildAnimalsEditorAnimalRecord> Animals,
    IReadOnlyList<string> LocationTypeKeys);

public sealed record WildAnimalsEditorAnimalRecord(
    string Key,
    WildAnimalJson Definition,
    WildAnimalOverrideConfig? Override,
    bool EffectiveEnabled,
    bool EffectiveLargeEgg,
    int EffectivePrice,
    bool EffectiveIsMagic,
    IReadOnlyList<WildAnimalsEditorLocationTypeRecord> LocationTypes,
    IReadOnlyList<WildAnimalsEditorModGroupRecord> ModGroups);

public sealed record WildAnimalsEditorLocationTypeRecord(
    string Key,
    bool BaseAllowed,
    bool? OverrideAllowed,
    bool EffectiveAllowed);

public sealed record WildAnimalsEditorModGroupRecord(
    string ModId,
    IReadOnlyList<string> BaseKeys,
    IReadOnlyList<string>? OverrideKeys,
    IReadOnlyList<string> EffectiveKeys);

public sealed record WildAnimalDraft(
    string Key,
    bool BaseEnabled,
    bool Enabled,
    bool BaseLargeEgg,
    bool LargeEgg,
    int BasePrice,
    int Price,
    bool BaseIsMagic,
    bool IsMagic,
    IReadOnlyList<WildAnimalDraftLocationType> LocationTypes,
    IReadOnlyList<WildAnimalDraftModGroup> ModGroups);

public sealed record WildAnimalDraftLocationType(
    string Key,
    bool BaseAllowed,
    bool Allowed);

public sealed record WildAnimalDraftModGroup(
    string ModId,
    IReadOnlyList<string> BaseKeys,
    IReadOnlyList<string> Keys);
