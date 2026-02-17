using System;
using static WildEggsHatching.DesertTomb.Core.Enums.DesertTombEnums;

namespace WildEggsHatching.DesertTomb.Core.Model.Monsters;

/// <summary>
/// Data-driven monster template for DesertTomb.
/// This is applied by <c>DesertTombMonsterFactory</c> after instantiating a base monster.
/// </summary>
public sealed record TombMonsterDefinition(
    TombMonsterType MonsterType,
    string DefinitionId,
    string BaseMonsterKey,
    string BaseName,
    string TextureName,
    TombMonsterStatProfile Stats,
    TombMonsterBehaviorFlags Flags,
    string? DropTableId
)
{
    public static TombMonsterDefinition CreateDefaults(
        TombMonsterType type,
        string textureName,
        string? baseName = null,
        string? baseMonsterKey = null)
        => new(
            MonsterType: type,
            DefinitionId: type.ToString(),
            BaseMonsterKey: baseMonsterKey ?? type.ToString(),
            BaseName: baseName ?? type.ToString(),
            TextureName: textureName,
            Stats: TombMonsterStatProfile.Default,
            Flags: TombMonsterBehaviorFlags.Default,
            DropTableId: null);
}

/// <summary>
/// Optional stat overrides applied on top of the base monster + multipliers.
/// </summary>
public sealed record TombMonsterStatProfile(
    int DifficultyAdd,
    int SpeedAdd,
    float? MaxHealthMultiplier,
    int? MaxHealthAdd,
    int? DamageToFarmer,
    int? ResilienceAdd
)
{
    public static TombMonsterStatProfile Default => new(
        DifficultyAdd: 0,
        SpeedAdd: 0,
        MaxHealthMultiplier: null,
        MaxHealthAdd: null,
        DamageToFarmer: null,
        ResilienceAdd: null
    );
}

/// <summary>
/// Optional behavior hints (kept for future phases; persisted to modData for host enforcement).
/// </summary>
public sealed record TombMonsterBehaviorFlags(
    bool IsBoss,
    bool HostAuthoritative
)
{
    public static TombMonsterBehaviorFlags Default => new(
        IsBoss: false,
        HostAuthoritative: true
    );
}
