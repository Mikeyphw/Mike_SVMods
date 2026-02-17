using WildEggsHatching.DesertTomb.Core.Model.Loot;

namespace WildEggsHatching.Infrastructure.DesertTomb.Loot.Definitions;

public interface ITombDropTables
{
    bool TryGet(string dropTableId, out TombDropTableDefinition table);
}
