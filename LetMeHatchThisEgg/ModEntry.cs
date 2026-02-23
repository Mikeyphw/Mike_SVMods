using LetMeHatchThisEgg.Application.Coordination;
using LetMeHatchThisEgg.Config;
using LetMeHatchThisEgg.Infrastructure.Commands;
using LetMeHatchThisEgg.Infrastructure.Smapi;
using LetMeHatchThisEgg.GameAssetManagement;
using Mikew.BirdsOfAFeather.Framework;
using Mikew.BirdsOfAFeather.Helpers;
using Mikew.BirdsOfAFeather.Helpers.Config;
using Mikew.BirdsOfAFeather.Helpers.Content;
using Mikew.BirdsOfAFeather.Helpers.Logging;
using StardewModdingAPI;

namespace LetMeHatchThisEgg
{
    /// <summary>The mod entry point.</summary>
    internal sealed partial class ModEntry : Mod
    {
        public override object GetApi() => letMeHatchApi;

        private readonly LetMeHatchApi letMeHatchApi = new();

        internal static ModConfig Config { get; private set; } = new();

        public static IMonitor MonitorInstance { get; private set; } = null!;
        public static IModHelper HelperInstance { get; private set; } = null!;
        public static IManifest ModManifestInstance { get; private set; } = null!;
        public static Logger LoggerHelper { get; private set; } = null!;
        public static I18TranslationHelper TranslationHelper { get; private set; } = null!;
        public static ModAssetHandler AssetHandler { get; private set; } = null!;

        public static AssetInvalidationBatcher InvalidationBatcher { get; private set; } = null!;

        internal static GameAssetManager? CurrentAssetManager { get; set; }

        private CoreContext? _core;
        private SmapiConfigProvider<ModConfig>? _configProvider;
        private BoafLog? _log;
        private LmhCoordinator? _coordinator;

        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            DependencyGuards.AssertBirdsOfAFeatherPresent(helper.ModRegistry);
            DependencyGuards.AssertNoLocalBirdsOfAFeatherCopies(helper.DirectoryPath, Monitor);

            _configProvider = new SmapiConfigProvider<ModConfig>(helper, Monitor);

            InitializeStaticFields(Monitor, helper, ModManifest, _configProvider.Get());

            _core = new CoreContext(helper, Monitor, ModManifest, TranslationHelper);
            _log = new BoafLog(LoggerHelper);
            InvalidationBatcher = new AssetInvalidationBatcher(helper, _log);
            _coordinator = new LmhCoordinator(_core, _configProvider, _log, letMeHatchApi, InvalidationBatcher);

            BindEvents(helper, _coordinator);
            LmhConsoleCommands.Register(helper, _coordinator);
        }

        public static void InitializeStaticFields(
            IMonitor monitor,
            IModHelper helper,
            IManifest manifest,
            ModConfig config)
        {
            MonitorInstance = monitor;
            HelperInstance = helper;
            ModManifestInstance = manifest;
            Config = config;

            LoggerHelper = new Logger(monitor, Config);
            TranslationHelper = new I18TranslationHelper(helper);
            AssetHandler = new ModAssetHandler(manifest.UniqueID, monitor);
        }

        private static void BindEvents(IModHelper helper, LmhCoordinator coordinator)
        {
            helper.Events.GameLoop.DayStarted += coordinator.OnDayStarted;
            helper.Events.GameLoop.DayEnding += coordinator.OnDayEnding;
            helper.Events.GameLoop.GameLaunched += coordinator.OnGameLaunched;
            helper.Events.GameLoop.SaveLoaded += coordinator.OnSaveLoaded;
            helper.Events.Player.Warped += coordinator.OnWarped;
        }
    }
}
