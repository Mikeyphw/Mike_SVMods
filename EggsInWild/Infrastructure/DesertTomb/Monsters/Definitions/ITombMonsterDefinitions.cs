using EggsInWild.DesertTomb.Core.Model.Monsters;
using static EggsInWild.DesertTomb.Core.Enums.DesertTombEnums;

namespace EggsInWild.Infrastructure.DesertTomb.Monsters.Definitions;

public interface ITombMonsterDefinitions
{
    bool TryGet(TombMonsterType type, out TombMonsterDefinition definition);
}
