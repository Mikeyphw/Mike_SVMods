using StardewModdingAPI;
using WildEggsHatching.Infrastructure.Config;

namespace WildEggsHatching.Infrastructure.Smapi;

public sealed class ModContext
{
    public ModContext(
        IModHelper helper,
        IMonitor monitor,
        IManifest manifest,
        ITranslationHelper translation,
        IConfigProvider configProvider)
    {
        Helper = helper;
        Monitor = monitor;
        Manifest = manifest;
        Translation = translation;
        ConfigProvider = configProvider;
    }

    public IModHelper Helper { get; }
    public IMonitor Monitor { get; }
    public IManifest Manifest { get; }
    public ITranslationHelper Translation { get; }
    public IConfigProvider ConfigProvider { get; }

    public void Log(string message, LogLevel level = LogLevel.Debug)
        => Monitor.Log(message, level);
}
