using System;
using Mikew.NestCore.Framework;
using Mikew.NestCore.Helpers;
using Mikew.NestCore.Helpers.Config;
using Mikew.NestCore.Helpers.Logging;
using Mikew.NestCore.Integrations.Gmcm;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using static Mikew.NestCore.Helpers.Logging.ScopedLogCatalog;

using LogScopedHelper = Mikew.NestCore.Helpers.Logging.LogScopedHelper;

namespace Mikew.NestCore;

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

    public override object GetApi() => new NestCoreApi();

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
            configProvider: () => ModCfg,
            defaultMod: Helpers.Logging.ScopedLogCatalog.ModNestCore,
            defaultCategory: Helpers.Logging.ScopedLogCatalog.CategoryGeneral);

        var translation = new I18TranslationHelper(helper);
        Context = new CoreContext(helper, Monitor, ModManifest, translation, _log);

        helper.ConsoleCommands.Add(
            "nestcore_status",
            "Print NestCore runtime status.",
            (_, _) => PrintStatus()
        );
        helper.ConsoleCommands.Add(
            "nestcore_i18n_missing_values",
            "Scan NestCore-dependent mods and print i18n keys with missing/empty values.",
            (_, _) => PrintI18nMissingValues()
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
                    root.Link("logging.nestcore", tooltip: true);
                    root.Link("logging.roostercontrol", tooltip: true);
                    root.Link("logging.we", tooltip: true);
                    root.Link("logging.lthte", tooltip: true);
                });

                layout.Page("logging.nestcore", () => "NestCore", page => BuildLoggingPage(page, layout.GetConfigFunc(), ScopedLogCatalog.ModNestCore));
                layout.Page("logging.roostercontrol", () => "RoosterControl", page => BuildLoggingPage(page, layout.GetConfigFunc(), ScopedLogCatalog.ModRoosterControl));
                layout.Page("logging.we", () => "EggsInWild (WE)", page => BuildLoggingPage(page, layout.GetConfigFunc(), ScopedLogCatalog.ModWe));
                layout.Page("logging.lthte", () => "LetMeHatch (LTHTE)", page => BuildLoggingPage(page, layout.GetConfigFunc(), ScopedLogCatalog.ModLthte));
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
        if (_log is not null)
        {
            LogScopedHelper.Log(_log, ModNestCoreCategory.Runtime, $"WorldReady={StardewModdingAPI.Context.IsWorldReady}", LogLevel.Info);
            LogScopedHelper.Log(_log, ModNestCoreCategory.Runtime, $"MainPlayer={StardewModdingAPI.Context.IsMainPlayer}", LogLevel.Info);
            LogScopedHelper.Log(_log, ModNestCoreCategory.Runtime, $"GmcmRegistered={_gmcm is not null}", LogLevel.Info);
        }
    }

    private void PrintI18nMissingValues()
    {
        if (_log is null)
            return;

        var reports = I18TranslationHelper.ScanNestCoreDependentModsForMissingValues(HelperInstance.DirectoryPath);
        var scanned = reports.Count;
        var withIssues = reports.Where(r => r.MissingValues.Count > 0).ToList();

        LogScopedHelper.Log(_log, ModNestCoreCategory.Runtime, $"Scanned {scanned} NestCore-dependent mod(s).", LogLevel.Info);

        if (withIssues.Count == 0)
        {
            LogScopedHelper.Log(_log, ModNestCoreCategory.Runtime, "No missing/empty i18n values found.", LogLevel.Info);
            return;
        }

        foreach (var report in withIssues)
        {
            LogScopedHelper.Log(
                _log,
                ModNestCoreCategory.Runtime,
                $"[{report.ModName}] ({report.UniqueId}) - {report.MissingValues.Count} i18n issue(s)",
                LogLevel.Warn);

            foreach (var issue in report.MissingValues)
                LogScopedHelper.Log(_log, ModNestCoreCategory.Runtime, $"  {issue.Key} -> {issue.Issue}", LogLevel.Warn);
        }
    }
}
