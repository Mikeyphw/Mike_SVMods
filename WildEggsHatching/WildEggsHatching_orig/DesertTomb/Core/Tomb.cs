using static WildEggsHatching.EnumsAndStrings.Enums.TextOptionValues;

namespace WildEggsHatching.DesertTomb.Core
{
    internal class Tomb
    {
        internal TombArea[] LevelAreas { get; set; }

        internal Tomb()
        {
            LevelAreas = Array.Empty<TombArea>();
        }
    }
}
