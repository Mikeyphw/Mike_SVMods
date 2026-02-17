using WildEggsHatching.DesertTomb.Core.Model.Monsters;

namespace WildEggsHatching.Infrastructure.DesertTomb.Monsters.Definitions;

/// <summary>
/// JSON schema for <see cref="TombMonsterDefinition"/> entries, keyed by TombMonsterType string.
/// </summary>
public sealed class TombMonsterDefinitionJson
{
    public string? BaseMonster { get; set; }
    public string? BaseName { get; set; }
    public string? Texture { get; set; }

    public TombMonsterStatProfileJson? Stats { get; set; }
    public TombMonsterBehaviorFlagsJson? Flags { get; set; }

    public string? DropTableId { get; set; }
}

public sealed class TombMonsterStatProfileJson
{
    public int? DifficultyAdd { get; set; }
    public int? SpeedAdd { get; set; }
    public float? MaxHealthMultiplier { get; set; }
    public int? MaxHealthAdd { get; set; }
    public int? DamageToFarmer { get; set; }
    public int? ResilienceAdd { get; set; }

    public TombMonsterStatProfile ToModel()
        => new(
            DifficultyAdd: DifficultyAdd ?? 0,
            SpeedAdd: SpeedAdd ?? 0,
            MaxHealthMultiplier: MaxHealthMultiplier,
            MaxHealthAdd: MaxHealthAdd,
            DamageToFarmer: DamageToFarmer,
            ResilienceAdd: ResilienceAdd
        );
}

public sealed class TombMonsterBehaviorFlagsJson
{
    public bool? IsBoss { get; set; }
    public bool? HostAuthoritative { get; set; }

    public TombMonsterBehaviorFlags ToModel()
        => new(
            IsBoss: IsBoss ?? false,
            HostAuthoritative: HostAuthoritative ?? true
        );
}
