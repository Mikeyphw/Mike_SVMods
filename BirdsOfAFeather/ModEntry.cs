using Mikew.BirdsOfAFeather.Framework;
using Mikew.BirdsOfAFeather.Helpers;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace Mikew.BirdsOfAFeather;

/// <summary>The mod entry point.</summary>
internal sealed class ModEntry : Mod
{
    internal static IModHelper HelperInstance { get; private set; } = null!;
    public static IMonitor MonitorInstance { get; private set; } = null!;
    public static IManifest ModManifestInstance { get; private set; } = null!;

    internal static CoreModConfig ModCfg { get; private set; } = new();
    internal static CoreContext Context { get; private set; } = null!;

    private IGMCMOptionsHelper _gmcm = null!;

    public override void Entry(IModHelper helper)
    {
        HelperInstance = helper;
        MonitorInstance = Monitor;
        ModManifestInstance = ModManifest;

        ModCfg = helper.ReadConfig<CoreModConfig>();
        var translation = new I18TranslationHelper(helper);
        Context = new CoreContext(helper, Monitor, ModManifest, translation);

        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        var gmcmApi = HelperInstance.ModRegistry.GetApi<IGenericModConfigMenuAPI>("spacechase0.GenericModConfigMenu");
        if (gmcmApi is null)
        {
            return;
        }

        _gmcm = new IGMCMOptionsHelper(extraOptionsAPI: null, configMenuAPI: gmcmApi, translationHelper: Context.Translation, manifest: ModManifest);

        _gmcm.Register(
            reset: () => ModCfg = new CoreModConfig(),
            save: () => HelperInstance.WriteConfig(ModCfg)
        );

        SetupGmcm();
    }

    private void SetupGmcm()
    {
        _gmcm.AddBoolOption(
            optionName: "ExtraDebugInfo",
            tooltip: true,
            getValue: () => ModCfg.VerboseLogging,
            setValue: value => ModCfg.VerboseLogging = value
        );

        _gmcm.AddBoolOption(
            optionName: "Testing",
            tooltip: true,
            getValue: () => ModCfg.TestingLogging,
            setValue: value => ModCfg.TestingLogging = value
        );
    }
}
