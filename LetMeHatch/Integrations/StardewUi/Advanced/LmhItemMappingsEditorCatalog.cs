using System.Collections.Generic;
using static LetMeHatch.EnumsAndStrings.Enums;

namespace LetMeHatch.Integrations.StardewUi.Advanced;

public sealed record LmhItemMappingsEditorCatalog(
    IReadOnlyList<LmhItemMappingsGroupRecord> Groups);

public sealed record LmhItemMappingsGroupRecord(
    CloneItemType CloneType,
    string DisplayName,
    IReadOnlyList<LmhItemMappingRecord> Mappings);

public sealed record LmhItemMappingRecord(
    string AnimalKey,
    string DisplayName,
    string BaseNormalItemId,
    string BaseLargeItemId,
    string ConfiguredNormalItemId,
    string ConfiguredLargeItemId,
    string EffectiveNormalItemId,
    string EffectiveLargeItemId);

public sealed record LmhItemMappingsEditorDraft(
    IReadOnlyDictionary<CloneItemType, Dictionary<string, LmhItemMappingDraftRow>> Mappings);

public sealed record LmhItemMappingDraftRow(
    string NormalItemId,
    string LargeItemId);
