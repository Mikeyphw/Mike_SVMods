using StardewModdingAPI;

namespace EggsInWild.Infrastructure.ModData;

public static class JsonFileReaderFactory
{
    public static IJsonFileReader Create(IModHelper helper)
    {
#if WEGG_DEV
        return new SmapiJsonFileReader(helper);
#else
        return new EmbeddedJsonFileReader();
#endif
    }
}
