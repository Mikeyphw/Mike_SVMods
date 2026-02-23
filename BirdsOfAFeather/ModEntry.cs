using System;
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

        var bootstrapLog = new SmapiLog(Monitor);
        _configProvider = new SmapiConfigProvider<CoreModConfig>(helper, bootstrapLog);
        ModCfg = _configProvider.Get();
        if (ModCfg.EnsureScopedLoggingDefaults())
            _configProvider.Save();

        _log = new SmapiLog(
            Monitor,
            verboseEnabled: () => ModCfg.VerboseLogging,
            testingEnabled: () => ModCfg.TestingLogging,
            configProvider: () => ModCfg,
            defaultMod: Helpers.Logging.ScopedLogCatalog.ModBoaf,
            defaultCategory: Helpers.Logging.ScopedLogCatalog.CategoryGeneral);

        var translation = new I18TranslationHelper(helper);
        Context = new CoreContext(helper, Monitor, ModManifest, translation, _log);

        helper.ConsoleCommands.Add(
            "boaf_status",
            "Print BirdsOfAFeather runtime status.",
            (_, _) => PrintStatus()
        );

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
                cfg.ScopedLogging = ScopedLoggingConfig.CreateDefault();
            }),
            save: () =>
            {
                var cfg = _configProvider.Get();
                cfg.EnsureScopedLoggingDefaults();
                _configProvider.Save();
            },
            define: layout =>
            {
                layout.Root(root =>
                {
                    root.Bool(cfg => cfg.VerboseLogging, tooltip: true);
                    root.Bool(cfg => cfg.TestingLogging, tooltip: true);
                    root.Separator(simple: true);
                    root.Link("logging.boaf", tooltip: true);
                    root.Link("logging.roosteraddon", tooltip: true);
                    root.Link("logging.we", tooltip: true);
                    root.Link("logging.lthte", tooltip: true);
                });

                layout.Page("logging.boaf", () => "BOAF", page => BuildLoggingPage(page, layout.GetConfigFunc(), ScopedLogCatalog.ModBoaf));
                layout.Page("logging.roosteraddon", () => "RoosterAddon", page => BuildLoggingPage(page, layout.GetConfigFunc(), ScopedLogCatalog.ModRoosterAddon));
                layout.Page("logging.we", () => "WildEggsHatching (WE)", page => BuildLoggingPage(page, layout.GetConfigFunc(), ScopedLogCatalog.ModWe));
                layout.Page("logging.lthte", () => "LetMeHatchThisEgg (LTHTE)", page => BuildLoggingPage(page, layout.GetConfigFunc(), ScopedLogCatalog.ModLthte));
            }
        );
    }

    private static void BuildLoggingPage(
        GmcmPageBuilder<CoreModConfig> page,
        Func<CoreModConfig> getConfig,
        string modTag)
    {
        page.ParagraphDynamic(
            () => "Log format: [MOD][CATEGORY][LEVEL] <message>. 'general' category and 'all' level are always available.",
            id: $"logging_{modTag}_format");
        page.Separator(simple: true);

        page.TextLabel(
            elementId: $"{modTag}.all",
            name: () => "all",
            tooltip: () => "Default level for every category in this mod.",
            getValue: () =>
            {
                var modCfg = getConfig().ScopedLogging.GetByMod(modTag);
                modCfg.EnsureDefaults(modTag);
                return modCfg.AllLevel;
            },
            setValue: value =>
            {
                var modCfg = getConfig().ScopedLogging.GetByMod(modTag);
                modCfg.AllLevel = ScopedLogCatalog.NormalizeLevel(value);
            },
            allowedValues: ScopedLogCatalog.AllowedLevels,
            formatValue: v => v
        );

        foreach (var category in ScopedLogCatalog.GetCategories(modTag))
        {
            page.TextLabel(
                elementId: $"{modTag}.cat.{category}",
                name: () => category,
                tooltip: () => $"Level override for category '{category}'.",
                getValue: () =>
                {
                    var modCfg = getConfig().ScopedLogging.GetByMod(modTag);
                    modCfg.EnsureDefaults(modTag);
                    return modCfg.GetCategoryLevel(category);
                },
                setValue: value =>
                {
                    var modCfg = getConfig().ScopedLogging.GetByMod(modTag);
                    modCfg.SetCategoryLevel(category, value);
                },
                allowedValues: ScopedLogCatalog.AllowedLevels,
                formatValue: v => v
            );
        }
    }

    private void PrintStatus()
    {
        var cfg = _configProvider?.Get() ?? ModCfg;
        Monitor.Log($"WorldReady={StardewModdingAPI.Context.IsWorldReady}", LogLevel.Info);
        Monitor.Log($"MainPlayer={StardewModdingAPI.Context.IsMainPlayer}", LogLevel.Info);
        Monitor.Log($"VerboseLogging={cfg.VerboseLogging}", LogLevel.Info);
        Monitor.Log($"TestingLogging={cfg.TestingLogging}", LogLevel.Info);
        Monitor.Log($"GmcmRegistered={_gmcm is not null}", LogLevel.Info);
    }
}
