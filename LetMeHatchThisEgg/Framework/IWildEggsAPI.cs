using static LetMeHatchThisEgg.ModEntry;

namespace LetMeHatchThisEgg.Framework
{
    public interface IWildEggsApi
    {
        HashSet<string> ValidAnimals { get; }
        HashSet<string> WildEggsIDs { get; }
        string BirthTextMode { get; }
    }
}
