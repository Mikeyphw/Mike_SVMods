using Mikew.BirdsOfAFeather.Framework;
using Mikew.BirdsOfAFeather.Helpers;
using Mikew.BirdsOfAFeather.Helpers.Config;
using Mikew.BirdsOfAFeather.Helpers.Logging;
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

    private SmapiConfigProvider<CoreModConfig>? _configProvider;
    private ILog? _log;
    private GmcmIntegration? _gmcm;

    public override object GetApi() => new BirdsOfAFeatherApi();

    public override void Entry(IModHelper helper)
    {
        HelperInstance = helper;
        MonitorInstance = Monitor;
        ModManifestInstance = ModManifest;

        _configProvider = new SmapiConfigProvider<CoreModConfig>(helper, Monitor);
        ModCfg = _configProvider.Get();

        _log = new SmapiLog(Monitor, verboseEnabled: () => ModCfg.VerboseLogging, testingEnabled: () => ModCfg.TestingLogging);

        var translation = new I18TranslationHelper(helper);
        Context = new CoreContext(helper, Monitor, ModManifest, translation, _log);

        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        if (_configProvider is null)
            return;

        _gmcm = GmcmIntegration.TryCreate(
            helper: HelperInstance,
            manifest: ModManifest,
            translation: Context.Translation
        );

        if (_gmcm is null)
            return;

        _gmcm.RebuildLayout(
            getConfig: () => _configProvider.Get(),
            reset: () => _configProvider.Update(cfg =>
            {
                cfg.VerboseLogging = false;
                cfg.TestingLogging = false;
            }),
            save: _configProvider.Save,
            define: layout =>
            {
                layout.Root(root =>
                {
                    root.Bool(cfg => cfg.VerboseLogging, tooltip: true);
                    root.Bool(cfg => cfg.TestingLogging, tooltip: true);
                });
            }
        );
    }
}
