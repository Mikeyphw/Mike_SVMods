using WildEggsHatching.DesertTomb.Core.Model.Monsters;
using static WildEggsHatching.DesertTomb.Core.Enums.DesertTombEnums;

namespace WildEggsHatching.Infrastructure.DesertTomb.Monsters.Definitions;

public interface ITombMonsterDefinitions
{
    bool TryGet(TombMonsterType type, out TombMonsterDefinition definition);
}
