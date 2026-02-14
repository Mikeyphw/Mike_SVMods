using BirdsOfAFeather.Helpers;
using Mikew.BirdsOfAFeather.Framework;
using Mikew.BirdsOfAFeather.Helpers;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace Mikew.BirdsOfAFeather
{
    /// <summary>The mod entry point.</summary>
    ///
    internal sealed partial class ModEntry : Mod
    {
        internal static IModHelper HelperInstance;
        internal IGMCMOptionsHelper configMenuExt = null!;
        internal static CoreModConfig ModCfg { get; private set; } = new();
        public static IMonitor? MonitorInstance { get; private set; }
        public static IManifest? ModManifestInstance { get; private set; }

        public override void Entry(IModHelper helper)
        {
            SetStaticFields(Monitor, Helper, ModManifest, Helper.ReadConfig<CoreModConfig>());
            Helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            Helper.Events.GameLoop.DayStarted += OnDayStart;
            Helper.Events.Player.Warped += OnWarped;
            Helper.Events.Content.AssetRequested += OnAssetRequested;
        }

        private static void SetStaticFields(
            IMonitor monitor,
            IModHelper helper,
            IManifest manifest,
            CoreModConfig config
        )
        {
            ModManifestInstance = manifest;
            ModCfg = config;
            HelperInstance = helper;
            MonitorInstance = monitor;
        }

        private void OnWarped(object? sender, WarpedEventArgs e) { }

        /// <summary>The method called after a new day starts.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnDayStart(object? sender, DayStartedEventArgs e) { }

        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e) { }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e) { }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (e.Ticks <= 4)
            {
                return;
            }
            HelperInstance.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
            var configMenuAPI = HelperInstance.ModRegistry.GetApi<IGenericModConfigMenuAPI>(
                "spacechase0.GenericModConfigMenu"
            );

            if (configMenuAPI == null)
            {
                return;
            }
            var translationHelper = new I18TranslationHelper(Helper);
            configMenuExt = new(null, configMenuAPI,translationHelper, ModManifest);

            // register mod
            configMenuExt.Register(
                reset: () => ModCfg = new CoreModConfig(),
                save: () => HelperInstance.WriteConfig(ModCfg)
            );
            GMCMSetup();
        }

        private void GMCMSetup()
        {
            configMenuExt.AddBoolOption(
                optionName: "ExtraDebugInfo",
                tooltip: true,
                getValue: () => ModCfg.VerboseLogging,
                setValue: value => ModCfg.VerboseLogging = value
            );
            configMenuExt.AddBoolOption(
                optionName: "Testing",
                tooltip: true,
                getValue: () => ModCfg.TestingLogging,
                setValue: value => ModCfg.TestingLogging = value
            );
        }
    }
}
