using static LetMeHatch.ModEntry;

namespace LetMeHatch.Framework
{
    public interface IRoosterHatchingAPI
    {
        int ApiVersion { get; }
        List<string> ChosenFemales { get; }
    }
}
