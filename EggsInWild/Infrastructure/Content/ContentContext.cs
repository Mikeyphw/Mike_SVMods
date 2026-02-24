using StardewModdingAPI;
using Mikew.NestCore.Helpers;
using Mikew.NestCore.Helpers.Logging;
using EggsInWild.Core.Enums;
using EggsInWild.Infrastructure.Config;
using EggsInWild.Infrastructure.ModData;

namespace EggsInWild.Infrastructure.Content;

public sealed class ContentContext
{
    public ContentContext(
        IModHelper helper,
        ILog log,
        I18TranslationHelper i18n,
        ModConfig config,
        ProgressionStep step,
        ModDataSnapshot modData)
    {
        Helper = helper;
        Logger = log;
        I18n = i18n;
        Config = config;
        Step = step;
        ModData = modData;
    }

    public IModHelper Helper { get; }
    public ILog Logger { get; }
    public I18TranslationHelper I18n { get; }
    public ModConfig Config { get; }
    public ProgressionStep Step { get; }
    public ModDataSnapshot ModData { get; }

    public void Log(string message, LogLevel level = LogLevel.Trace)
        => Logger.Log(message, level);
}
