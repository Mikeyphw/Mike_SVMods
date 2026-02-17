using Mikew.BirdsOfAFeather.Framework;
using Mikew.BirdsOfAFeather.Helpers;
using Mikew.BirdsOfAFeather.Integrations.Gmcm;
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

    private GmcmIntegration? _gmcm;

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
        _gmcm = GmcmIntegration.TryCreate(
            helper: HelperInstance,
            manifest: ModManifest,
            translation: Context.Translation
        );

        if (_gmcm is null)
            return;

        _gmcm.RebuildLayout(
            getConfig: () => ModCfg,
            reset: () => ModCfg = new CoreModConfig(),
            save: () => HelperInstance.WriteConfig(ModCfg),
            define: layout =>
            {
                layout.Root(root =>
                {
                    // Keep option ids stable even if config property names change later.
                    root.Bool(cfg => cfg.VerboseLogging, optionId: "ExtraDebugInfo", tooltip: true);
                    root.Bool(cfg => cfg.TestingLogging, optionId: "Testing", tooltip: true);
                });
            }
        );
    }
}
