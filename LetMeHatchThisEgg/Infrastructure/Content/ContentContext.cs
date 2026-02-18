using System;
using LetMeHatchThisEgg.Config;
using LetMeHatchThisEgg.Infrastructure.Logging;
using Mikew.BirdsOfAFeather.Framework;
using Mikew.BirdsOfAFeather.Helpers;
using StardewModdingAPI;

namespace LetMeHatchThisEgg.Infrastructure.Content;

public sealed class ContentContext
{
    public ContentContext(CoreContext core, ModConfig config, ILog log)
    {
        Core = core ?? throw new ArgumentNullException(nameof(core));
        Config = config ?? throw new ArgumentNullException(nameof(config));
        Log = log ?? throw new ArgumentNullException(nameof(log));
    }

    public CoreContext Core { get; }
    public ModConfig Config { get; }
    public ILog Log { get; }

    public IModHelper Helper => Core.Helper;
    public IMonitor Monitor => Core.Monitor;
    public IManifest Manifest => Core.Manifest;
    public I18TranslationHelper I18n => Core.Translation;
}
