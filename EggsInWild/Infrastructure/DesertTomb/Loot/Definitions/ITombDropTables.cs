using EggsInWild.DesertTomb.Core.Model.Loot;

namespace EggsInWild.Infrastructure.DesertTomb.Loot.Definitions;

public interface ITombDropTables
{
    bool TryGet(string dropTableId, out TombDropTableDefinition table);
}
