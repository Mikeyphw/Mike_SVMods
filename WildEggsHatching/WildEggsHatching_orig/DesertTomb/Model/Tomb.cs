using static WildEggsHatching.EnumsAndStrings.Enums.TextOptionValues;

namespace WildEggsHatching.DesertTomb.Model
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
