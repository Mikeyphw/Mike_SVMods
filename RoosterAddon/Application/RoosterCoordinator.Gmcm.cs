using System;
using System.Linq;
using Mikew.BirdsOfAFeather.Framework;
using RoosterAddon.Framework.Config;
using RoosterAddon.Integrations;
using StardewModdingAPI;
using StardewModdingAPI.Events;

using ILetMeHatchApi = Mikew.BirdsOfAFeather.Framework.ILetMeHatchApi;

namespace RoosterAddon.Application;

internal sealed partial class RoosterCoordinator
{
    public void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        ModEntry.ContentPatcher = Helper.ModRegistry.GetApi<IContentPatcherAPI>("Pathoschild.ContentPatcher");

        HatchApi = Helper.ModRegistry.GetApi<ILetMeHatchApi>("Mikew.lmh")
            ?? LetMeHatchApiAdapter.Wrap(Helper.ModRegistry.GetApi<object>("Mikew.lmh"));

        ConfigMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuAPI>("spacechase0.GenericModConfigMenu");

        TryRegisterGmcm(force: true);
    }

    public void OnLocaleChanged(object? sender, LocaleChangedEventArgs e)
    {
        TryRegisterGmcm(force: true);
        RequestInvalidate(FarmAnimalsAssetName);
        RequestInvalidate(ObjectsAssetName);
    }

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

                RoosterHelper?.UpdateConfig(Config);
                ConfigManager.Save(Helper, Config);

                InvalidateDailyPlanCache("config reset");
                _needsRoosterSetup = true;
                RequestInvalidate(FarmAnimalsAssetName);
                RequestInvalidate(ObjectsAssetName);
            },
            save: () =>
            {
                RoosterHelper?.UpdateConfig(Config);
                ConfigManager.Save(Helper, Config);

                InvalidateDailyPlanCache("config saved");
                _needsRoosterSetup = true;
                RequestInvalidate(FarmAnimalsAssetName);
                RequestInvalidate(ObjectsAssetName);
            }
        );

        RegisterGmcmOptions(ConfigMenu);

        _gmcmRegistered = true;
    }

    private void RegisterGmcmOptions(IGenericModConfigMenuAPI gmcm)
    {
        gmcm.AddSectionTitle(ModManifest, () => "General");

        gmcm.AddBoolOption(
            ModManifest,
            getValue: () => Config.General.EnableDebug,
            setValue: v => Config.General.EnableDebug = v,
            name: () => "Enable debug logging"
        );

        gmcm.AddTextOption(
            ModManifest,
            getValue: () => Config.General.UpdateRate.ToString(),
            setValue: v => Config.General.UpdateRate = ParseEnum(v, UpdateRateMode.OnDayStart),
            name: () => "Update rate",
            allowedValues: Enum.GetNames(typeof(UpdateRateMode))
        );

        gmcm.AddNumberOption(
            ModManifest,
            getValue: () => Config.General.ChosenPercentageDailyLuckBonus,
            setValue: v => Config.General.ChosenPercentageDailyLuckBonus = ClampInt(v, 0, 100),
            name: () => "Daily luck bonus (%)",
            min: 0,
            max: 100,
            interval: 1
        );

        gmcm.AddTextOption(
            ModManifest,
            getValue: () => Config.General.HarvestType.ToString(),
            setValue: v => Config.General.HarvestType = ParseEnum(v, HarvestType.DigUp),
            name: () => "Harvest type",
            allowedValues: Enum.GetNames(typeof(HarvestType))
        );

        gmcm.AddBoolOption(
            ModManifest,
            getValue: () => Config.General.EnableStuckInside,
            setValue: v => Config.General.EnableStuckInside = v,
            name: () => "Enable stuck-inside logic"
        );

        gmcm.AddBoolOption(
            ModManifest,
            getValue: () => Config.General.BetterTrufflesCompat,
            setValue: v => Config.General.BetterTrufflesCompat = v,
            name: () => "Better Truffles compatibility"
        );

        gmcm.AddTextOption(
            ModManifest,
            getValue: () => Config.General.HatchingMode.ToString(),
            setValue: v => Config.General.HatchingMode = ParseEnum(v, HatchingMode.Original),
            name: () => "Hatching mode",
            allowedValues: Enum.GetNames(typeof(HatchingMode))
        );

        gmcm.AddBoolOption(
            ModManifest,
            getValue: () => Config.General.EnableEggProbability,
            setValue: v => Config.General.EnableEggProbability = v,
            name: () => "Enable egg probability"
        );

        gmcm.AddNumberOption(
            ModManifest,
            getValue: () => Config.General.EggProbability,
            setValue: v => Config.General.EggProbability = ClampInt(v, 0, 100),
            name: () => "Egg probability (%)",
            min: 0,
            max: 100,
            interval: 1
        );

        gmcm.AddBoolOption(
            ModManifest,
            getValue: () => Config.General.EnableLargeEggProbability,
            setValue: v => Config.General.EnableLargeEggProbability = v,
            name: () => "Enable large egg probability"
        );

        gmcm.AddNumberOption(
            ModManifest,
            getValue: () => Config.General.LargeEggProbability,
            setValue: v => Config.General.LargeEggProbability = ClampInt(v, 0, 100),
            name: () => "Large egg probability (%)",
            min: 0,
            max: 100,
            interval: 1
        );

        gmcm.AddNumberOption(
            ModManifest,
            getValue: () => Config.General.FertilizedEggProbability,
            setValue: v => Config.General.FertilizedEggProbability = ClampInt(v, 0, 100),
            name: () => "Fertilized egg probability (%)",
            min: 0,
            max: 100,
            interval: 1
        );

        gmcm.AddNumberOption(
            ModManifest,
            getValue: () => Config.General.RoosterToChickenRatio,
            setValue: v => Config.General.RoosterToChickenRatio = ClampInt(v, 0, 100),
            name: () => "Rooster to chicken ratio (%)",
            min: 0,
            max: 100,
            interval: 1
        );

        gmcm.AddNumberOption(
            ModManifest,
            getValue: () => Config.General.BoostByRooster,
            setValue: v => Config.General.BoostByRooster = ClampFloat(v, 0f, 10f),
            name: () => "Boost by rooster",
            min: 0f,
            max: 10f,
            interval: 0.01f
        );
        gmcm.AddSectionTitle(ModManifest, () => "Roosters");

        gmcm.AddPageLink(ModManifest, "roosters_white", () => "White rooster");
        gmcm.AddPageLink(ModManifest, "roosters_brown", () => "Brown rooster");
        gmcm.AddPageLink(ModManifest, "roosters_blue", () => "Blue rooster");
        gmcm.AddPageLink(ModManifest, "roosters_void", () => "Void rooster");
        gmcm.AddPageLink(ModManifest, "roosters_gold", () => "Gold rooster");

        RegisterRoosterPage(gmcm, "roosters_white", RoosterAddon.Enums.RoosterType.White, "White rooster");
        RegisterRoosterPage(gmcm, "roosters_brown", RoosterAddon.Enums.RoosterType.Brown, "Brown rooster");
        RegisterRoosterPage(gmcm, "roosters_blue", RoosterAddon.Enums.RoosterType.Blue, "Blue rooster");
        RegisterRoosterPage(gmcm, "roosters_void", RoosterAddon.Enums.RoosterType.Void, "Void rooster");
        RegisterRoosterPage(gmcm, "roosters_gold", RoosterAddon.Enums.RoosterType.Gold, "Gold rooster");
    }

    private void RegisterRoosterPage(IGenericModConfigMenuAPI gmcm, string pageId, RoosterAddon.Enums.RoosterType type, string title)
    {
        gmcm.AddPage(ModManifest, pageId, () => title);

        if (!Config.Roosters.TryGetValue(type, out var s))
        {
            s = new RoosterSettings();
            Config.Roosters[type] = s;
        }

        gmcm.AddSectionTitle(ModManifest, () => "Availability");

        gmcm.AddBoolOption(
            ModManifest,
            getValue: () => s.Buyable,
            setValue: v => s.Buyable = v,
            name: () => "Buyable"
        );

        gmcm.AddNumberOption(
            ModManifest,
            getValue: () => s.Price,
            setValue: v => s.Price = ClampInt(v, 0, 100000),
            name: () => "Price",
            min: 0,
            max: 100000,
            interval: 100
        );

        gmcm.AddTextOption(
            ModManifest,
            getValue: () => s.BuildingRequired.ToString(),
            setValue: v => s.BuildingRequired = ParseEnum(v, RequiredBuilding.BigCoop),
            name: () => "Required building",
            allowedValues: Enum.GetNames(typeof(RequiredBuilding))
        );

        gmcm.AddSectionTitle(ModManifest, () => "Behavior");

        gmcm.AddBoolOption(
            ModManifest,
            getValue: () => s.EnableRoosterHatching,
            setValue: v => s.EnableRoosterHatching = v,
            name: () => "Enable rooster hatching"
        );

        gmcm.AddBoolOption(
            ModManifest,
            getValue: () => s.EditAllFemales,
            setValue: v => s.EditAllFemales = v,
            name: () => "Edit all females"
        );

        gmcm.AddNumberOption(
            ModManifest,
            getValue: () => s.MatureDays,
            setValue: v => s.MatureDays = ClampInt(v, 0, 999),
            name: () => "Mature days",
            min: 0,
            max: 999,
            interval: 1
        );

        gmcm.AddNumberOption(
            ModManifest,
            getValue: () => s.ProduceDays,
            setValue: v => s.ProduceDays = ClampInt(v, 1, 999),
            name: () => "Produce days",
            min: 1,
            max: 999,
            interval: 1
        );

        gmcm.AddSectionTitle(ModManifest, () => "Produce");

        gmcm.AddTextOption(
            ModManifest,
            getValue: () => s.ProduceOption.ToString(),
            setValue: v => s.ProduceOption = ParseEnum(v, ProduceMode.CustomizeBelow),
            name: () => "Produce mode",
            allowedValues: Enum.GetNames(typeof(ProduceMode))
        );

        for (var i = 0; i < s.ProduceEntries.Count; i++)
        {
            var idx = i;

            gmcm.AddTextOption(
                ModManifest,
                getValue: () => s.ProduceEntries[idx].Choice ?? string.Empty,
                setValue: v => s.ProduceEntries[idx].Choice = v ?? string.Empty,
                name: () => $"Produce #{idx + 1} item key"
            );

            gmcm.AddTextOption(
                ModManifest,
                getValue: () => s.ProduceEntries[idx].CustomItemName ?? "Custom",
                setValue: v => s.ProduceEntries[idx].CustomItemName = v ?? "Custom",
                name: () => $"Produce #{idx + 1} custom name"
            );

            gmcm.AddNumberOption(
                ModManifest,
                getValue: () => s.ProduceEntries[idx].Probability,
                setValue: v => s.ProduceEntries[idx].Probability = ClampInt(v, 0, 100),
                name: () => $"Produce #{idx + 1} probability (%)",
                min: 0,
                max: 100,
                interval: 1
            );
        }

        gmcm.AddSectionTitle(ModManifest, () => "Deluxe produce");

        gmcm.AddTextOption(
            ModManifest,
            getValue: () => s.DeluxeProduceOption.ToString(),
            setValue: v => s.DeluxeProduceOption = ParseEnum(v, DeluxeProduceMode.CustomizeBelow),
            name: () => "Deluxe produce mode",
            allowedValues: Enum.GetNames(typeof(DeluxeProduceMode))
        );

        for (var i = 0; i < s.DeluxeProduceEntries.Count; i++)
        {
            var idx = i;

            gmcm.AddTextOption(
                ModManifest,
                getValue: () => s.DeluxeProduceEntries[idx].Choice ?? string.Empty,
                setValue: v => s.DeluxeProduceEntries[idx].Choice = v ?? string.Empty,
                name: () => $"Deluxe produce #{idx + 1} item key"
            );

            gmcm.AddTextOption(
                ModManifest,
                getValue: () => s.DeluxeProduceEntries[idx].CustomItemName ?? "Custom",
                setValue: v => s.DeluxeProduceEntries[idx].CustomItemName = v ?? "Custom",
                name: () => $"Deluxe produce #{idx + 1} custom name"
            );

            gmcm.AddNumberOption(
                ModManifest,
                getValue: () => s.DeluxeProduceEntries[idx].Probability,
                setValue: v => s.DeluxeProduceEntries[idx].Probability = ClampInt(v, 0, 100),
                name: () => $"Deluxe produce #{idx + 1} probability (%)",
                min: 0,
                max: 100,
                interval: 1
            );
        }

        gmcm.AddSectionTitle(ModManifest, () => "Blue-only");

        gmcm.AddBoolOption(
            ModManifest,
            getValue: () => s.BypassBlueConditions,
            setValue: v => s.BypassBlueConditions = v,
            name: () => "Bypass blue conditions"
        );

        gmcm.AddTextOption(
            ModManifest,
            getValue: () => s.BlueEggChoice ?? string.Empty,
            setValue: v => s.BlueEggChoice = v ?? string.Empty,
            name: () => "Blue egg choice item key"
        );
    }

    private static int ClampInt(int value, int min, int max) => Math.Min(max, Math.Max(min, value));
    private static float ClampFloat(float value, float min, float max) => Math.Min(max, Math.Max(min, value));

    private static T ParseEnum<T>(string value, T fallback) where T : struct, Enum
        => Enum.TryParse<T>(value, ignoreCase: true, out var parsed) ? parsed : fallback;
}
