using RoosterAddon.Framework.Config;
using Mikew.BirdsOfAFeather.Framework;
using RoosterAddon.Constants;
using RoosterAddon.Integrations;
using StardewModdingAPI.Events;

using ILetMeHatchApi = Mikew.BirdsOfAFeather.Framework.ILetMeHatchApi;

namespace RoosterAddon.Application;

internal sealed partial class RoosterCoordinator
{
    public void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        ModEntry.ContentPatcher = Helper.ModRegistry.GetApi<IContentPatcherAPI>("Pathoschild.ContentPatcher");

        HatchApi = Helper.ModRegistry.GetApi<ILetMeHatchApi>("Mikew.lmh");
        if (HatchApi is null)
            HatchApi = LetMeHatchApiAdapter.Wrap(Helper.ModRegistry.GetApi<object>("Mikew.lmh"));

        ConfigMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuAPI>("spacechase0.GenericModConfigMenu");

        TryRegisterGmcm(force: true);
    }

        public void OnLocaleChanged(object? sender, LocaleChangedEventArgs e)
        {
            TryRegisterGmcm(force: true);
            RequestInvalidate(FarmAnimalsAssetName);
            RequestInvalidate(ObjectsAssetName);
        }

        /// <summary>The method called after a new day starts.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        // Event handler for when a new day starts

        private void TryRegisterGmcm(bool force = false)
        {
            if (ConfigMenu is null)
                return;

            if (_gmcmRegistered && !force)
                return;

            if (_gmcmRegistered && force)
                ConfigMenu.Unregister(ModManifest);

            ConfigMenu.Register(
                mod: ModManifest,
                reset: () =>
                {
                    Config = RoosterAddonConfig.CreateDefault();
                    Config.FemalesOptions = new Dictionary<string, string>(_roosterFemales.Females);

                    ModCfg = LegacyConfigAdapter.ToLegacy(Config);
                    if (RoosterHelper is not null)
                        RoosterHelper.ModCfg = ModCfg;

                    ConfigManager.Save(Helper, Config);

                    _needsRoosterSetup = true;
                    RequestInvalidate(FarmAnimalsAssetName);
                    RequestInvalidate(ObjectsAssetName);
                },
                save: () =>
                {
                    ModCfg = LegacyConfigAdapter.ToLegacy(Config);
                    if (RoosterHelper is not null)
                        RoosterHelper.ModCfg = ModCfg;

                    ConfigManager.Save(Helper, Config);

                    _roosterFemales.Females = new Dictionary<string, string>(Config.FemalesOptions);
                    _roosterFemalesDirty = true;

                    _needsRoosterSetup = true;
                    RequestInvalidate(FarmAnimalsAssetName);
                    RequestInvalidate(ObjectsAssetName);
                }
            );

            Config.FemalesOptions = new Dictionary<string, string>(_roosterFemales.Females);
            ModCfg = LegacyConfigAdapter.ToLegacy(Config);
            if (RoosterHelper is not null)
                RoosterHelper.ModCfg = ModCfg;

            var gmcm = ConfigMenu;
            _host.GMCMSetup(ref gmcm);
            ConfigMenu = gmcm;

            _gmcmRegistered = true;
        }

}
