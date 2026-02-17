using StardewModdingAPI;
using Mikew.BirdsOfAFeather.Helpers;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.Infrastructure.Config;

namespace WildEggsHatching.Infrastructure.Content;

public sealed class ContentContext
{
    public ContentContext(
        IModHelper helper,
        IMonitor monitor,
        I18TranslationHelper i18n,
        ModConfig config,
        ProgressionStep step)
    {
        Helper = helper;
        Monitor = monitor;
        I18n = i18n;
        Config = config;
        Step = step;
    }

    public IModHelper Helper { get; }
    public IMonitor Monitor { get; }
    public I18TranslationHelper I18n { get; }
    public ModConfig Config { get; }
    public ProgressionStep Step { get; }

    public void Log(string message, LogLevel level = LogLevel.Trace)
        => Monitor.Log(message, level);
}
