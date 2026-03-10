using System;
using System.Linq;
using Mikew.NestCore.Diagnostics;
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
            "nest",
            "NestCore commands. Use: nest help",
            (_, args) => ExecuteNestCommand(args)
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

    private void ExecuteNestCommand(string[] args)
    {
        if (_log is null)
            return;

        if (args.Length == 0 || IsHelp(args[0]))
        {
            LogScopedHelper.Log(_log, ModNestCoreCategory.Runtime, BuildNestHelpText(), LogLevel.Info);
            return;
        }

        var verb = args[0].ToLowerInvariant();
        switch (verb)
        {
            case "help":
                LogScopedHelper.Log(_log, ModNestCoreCategory.Runtime, BuildNestHelpText(), LogLevel.Info);
                return;
            case "status":
                PrintStatus();
                return;
            case "verify":
            {
                var json = args.Any(x => string.Equals(x, "--json", StringComparison.OrdinalIgnoreCase));
                var report = BuildVerifyReport();
                var message = json
                    ? DiagnosticReportFormatter.FormatJson(report, "nest verify")
                    : DiagnosticReportFormatter.FormatText(report, "nest verify");
                LogScopedHelper.Log(_log, ModNestCoreCategory.Runtime, message, report.FailCount > 0 ? LogLevel.Warn : LogLevel.Info);
                return;
            }
            case "save":
                HandleSaveCommand(args.Skip(1).ToArray());
                return;
            case "debug":
#if NEST_DEV
                HandleDebugCommand(args.Skip(1).ToArray());
#else
                LogScopedHelper.Log(_log, ModNestCoreCategory.Runtime, "Dev-only commands are unavailable in this build.", LogLevel.Warn);
#endif
                return;
            default:
                LogScopedHelper.Log(_log, ModNestCoreCategory.Runtime, $"Unknown subcommand '{args[0]}'.\n\n{BuildNestHelpText()}", LogLevel.Warn);
                return;
        }
    }

    private string BuildNestHelpText()
    {
        return
            "nest help\n" +
            "nest status\n" +
            "nest verify [--json]\n" +
            "nest save clear --yes\n" +
#if NEST_DEV
            "nest debug i18n-missing-values\n" +
#endif
            "\nStatus prints a concise runtime snapshot.\n" +
            "Verify runs read-only NestCore runtime/config checks.";
    }

    private DiagnosticReport BuildVerifyReport()
    {
        var report = new DiagnosticReport();
        report.Add("GAME_LAUNCHED", StardewModdingAPI.Context.IsGameLaunched ? DiagnosticSeverity.Pass : DiagnosticSeverity.Warn, "runtime", $"GameLaunched={StardewModdingAPI.Context.IsGameLaunched}");
        report.Add("WORLD_READY", StardewModdingAPI.Context.IsWorldReady ? DiagnosticSeverity.Pass : DiagnosticSeverity.Warn, "runtime", $"WorldReady={StardewModdingAPI.Context.IsWorldReady}");
        report.Add("MAIN_PLAYER", StardewModdingAPI.Context.IsMainPlayer ? DiagnosticSeverity.Pass : DiagnosticSeverity.Warn, "runtime", $"MainPlayer={StardewModdingAPI.Context.IsMainPlayer}");
        report.Add("HELPER", HelperInstance is not null ? DiagnosticSeverity.Pass : DiagnosticSeverity.Fail, "runtime", $"HelperReady={HelperInstance is not null}");
        report.Add("CONFIG_PROVIDER", _configProvider is not null ? DiagnosticSeverity.Pass : DiagnosticSeverity.Fail, "config", $"ConfigProviderReady={_configProvider is not null}");
        report.Add("TRANSLATION", Context?.Translation is not null ? DiagnosticSeverity.Pass : DiagnosticSeverity.Fail, "runtime", $"TranslationReady={Context?.Translation is not null}");
        report.Add("GMCM", _gmcm is not null ? DiagnosticSeverity.Pass : DiagnosticSeverity.Warn, "integrations", $"GmcmRegistered={_gmcm is not null}", "Install/enable GMCM to use the NestCore config UI.");
        report.Add("SCOPED_LOGGING", ModCfg.EnsureScopedLoggingDefaults() ? DiagnosticSeverity.Warn : DiagnosticSeverity.Pass, "config", "Scoped logging defaults are initialized.");
        return report;
    }

    private void PrintStatus()
    {
        if (_log is not null)
        {
            LogScopedHelper.Log(_log, ModNestCoreCategory.Runtime, $"GameLaunched={StardewModdingAPI.Context.IsGameLaunched}", LogLevel.Info);
            LogScopedHelper.Log(_log, ModNestCoreCategory.Runtime, $"WorldReady={StardewModdingAPI.Context.IsWorldReady}", LogLevel.Info);
            LogScopedHelper.Log(_log, ModNestCoreCategory.Runtime, $"MainPlayer={StardewModdingAPI.Context.IsMainPlayer}", LogLevel.Info);
            LogScopedHelper.Log(_log, ModNestCoreCategory.Runtime, $"ConfigProviderReady={_configProvider is not null}", LogLevel.Info);
            LogScopedHelper.Log(_log, ModNestCoreCategory.Runtime, $"GmcmRegistered={_gmcm is not null}", LogLevel.Info);
        }
    }

    private void HandleSaveCommand(string[] args)
    {
        if (_log is null)
            return;

        if (args.Length == 0 || IsHelp(args[0]))
        {
            LogScopedHelper.Log(_log, ModNestCoreCategory.Runtime, "Usage: nest save clear --yes", LogLevel.Info);
            return;
        }

        if (!string.Equals(args[0], "clear", StringComparison.OrdinalIgnoreCase))
        {
            LogScopedHelper.Log(_log, ModNestCoreCategory.Runtime, $"Unknown save subcommand '{args[0]}'.\n\n{BuildNestHelpText()}", LogLevel.Warn);
            return;
        }

        LogScopedHelper.Log(_log, ModNestCoreCategory.Runtime, "NestCore does not currently persist per-save data, so there is nothing to clear.", LogLevel.Info);
    }

#if NEST_DEV
    private void HandleDebugCommand(string[] args)
    {
        if (_log is null)
            return;

        if (args.Length == 0 || IsHelp(args[0]))
        {
            LogScopedHelper.Log(_log, ModNestCoreCategory.Runtime, "nest debug i18n-missing-values", LogLevel.Info);
            return;
        }

        switch (args[0].ToLowerInvariant())
        {
            case "i18n-missing-values":
                PrintI18nMissingValues();
                return;
            default:
                LogScopedHelper.Log(_log, ModNestCoreCategory.Runtime, $"Unknown debug subcommand '{args[0]}'.", LogLevel.Warn);
                return;
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
#endif

    private static bool IsHelp(string arg)
        => string.Equals(arg, "help", StringComparison.OrdinalIgnoreCase)
           || string.Equals(arg, "-h", StringComparison.OrdinalIgnoreCase)
           || string.Equals(arg, "--help", StringComparison.OrdinalIgnoreCase);
}
