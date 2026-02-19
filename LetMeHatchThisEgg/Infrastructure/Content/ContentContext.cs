using System;
using LetMeHatchThisEgg.Application;
using LetMeHatchThisEgg.Config;
using LetMeHatchThisEgg.GameAssetManagement;
using Mikew.BirdsOfAFeather.Framework;
using Mikew.BirdsOfAFeather.Helpers;
using Mikew.BirdsOfAFeather.Helpers.Config;
using Mikew.BirdsOfAFeather.Helpers.Logging;
using StardewModdingAPI;

namespace LetMeHatchThisEgg.Infrastructure.Content;

public sealed class ContentContext
{
    public ContentContext(
        CoreContext core,
        SmapiConfigProvider<ModConfig> configProvider,
        ILog log,
        ModAssetHandler assetHandler,
        GameAssetManager assets,
        LmhApplicationServices app)
    {
        Core = core ?? throw new ArgumentNullException(nameof(core));
        ConfigProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
        Log = log ?? throw new ArgumentNullException(nameof(log));
        AssetHandler = assetHandler ?? throw new ArgumentNullException(nameof(assetHandler));
        Assets = assets ?? throw new ArgumentNullException(nameof(assets));
        App = app ?? throw new ArgumentNullException(nameof(app));
    }

    public CoreContext Core { get; }
    public SmapiConfigProvider<ModConfig> ConfigProvider { get; }
    public ModConfig Config => ConfigProvider.Config;
    public ILog Log { get; }
    public ModAssetHandler AssetHandler { get; }
    public GameAssetManager Assets { get; }
    public LmhApplicationServices App { get; }

    public IModHelper Helper => Core.Helper;
    public IMonitor Monitor => Core.Monitor;
    public IManifest Manifest => Core.Manifest;
    public I18TranslationHelper I18n => Core.Translation;
}
