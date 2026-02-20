using System;
using LetMeHatchThisEgg.Config;
using Mikew.BirdsOfAFeather.Framework;
using Mikew.BirdsOfAFeather.Helpers.Config;
using Mikew.BirdsOfAFeather.Integrations.Gmcm;
using static LetMeHatchThisEgg.EnumsAndStrings.Enums;

namespace LetMeHatchThisEgg.Integrations.Gmcm;

public static class LmhGmcmMenu
{
    public static void TryRegister(
        CoreContext core,
        SmapiConfigProvider<ModConfig> configProvider,
        Action? onSaved = null)
    {
        if (core is null)
            throw new ArgumentNullException(nameof(core));
        if (configProvider is null)
            throw new ArgumentNullException(nameof(configProvider));

        var host = GmcmConfigHost<ModConfig>.TryCreate(
            helper: core.Helper,
            manifest: core.Manifest,
            getConfig: configProvider.Get,
            reset: () => configProvider.Update(cfg => cfg.Reset(), save: true),
            save: configProvider.Save,
            translation: core.Translation,
            onSaved: onSaved
        );

        if (host is null)
            return;

        host.RebuildLayout(layout =>
        {
            layout.Root(root =>
            {
                root.Section("General");
                root.Bool(cfg => cfg.EditEggListByName, optionId: "EditEggListByName", tooltip: true);
                root.Bool(cfg => cfg.EditBirthText, optionId: "EditBirthText", tooltip: true);
                root.Bool(cfg => cfg.AlternateBirthText, optionId: "AlternateBirthText", tooltip: true);

                root.Separator(simple: true);

                root.Link("cloning");
                root.Link("colors");
                root.Link("breeding");
                root.Link("advanced");
                root.Link("debug");
            });

            layout.Page(
                "cloning",
                title: () => core.Translation.GetString("config.page.cloning.name"),
                build: page =>
                {
                    page.Bool(cfg => cfg.CloneVanillaEggs, optionId: "CloneVanillaEggs", tooltip: true);
                    page.Bool(cfg => cfg.CloneMachineRules, optionId: "CloneMachineRules", tooltip: true);
                    page.Bool("DinoEggsAsArtifact",
                        getValue: () => configProvider.Get().DinoEggAsArtifact,
                        setValue: v => configProvider.Get().DinoEggAsArtifact = v,
                        tooltip: true);
                    page.Bool(cfg => cfg.OnlyFertilizedEggs, optionId: "OnlyFertilizedEggs", tooltip: true);
                    page.Bool("ColorMilk",
                        getValue: () => configProvider.Get().ColorClonedMilk,
                        setValue: v => configProvider.Get().ColorClonedMilk = v,
                        tooltip: true);
                    page.Bool("ExclusiveRabbitWool",
                        getValue: () => configProvider.Get().ExclusiveRabbitWool,
                        setValue: v => configProvider.Get().ExclusiveRabbitWool = v,
                        tooltip: true);
                }
            );

            layout.Page(
                "colors",
                title: () => core.Translation.GetString("config.page.colors.name"),
                build: page =>
                {
                    page.Bool(cfg => cfg.ChangeEggColors, optionId: "ChangeEggColors", tooltip: true);
                    page.Bool("LinkAnimalColorToEggs",
                        getValue: () => configProvider.Get().LinkAnimalColorToEgg,
                        setValue: v => configProvider.Get().LinkAnimalColorToEgg = v,
                        tooltip: true);
                }
            );

            layout.Page(
                "breeding",
                title: () => core.Translation.GetString("config.page.breeding.name"),
                build: page =>
                {
                    page.Bool(cfg => cfg.BreedingSystemEnabled, optionId: "BreedingSystemEnabled", tooltip: true);
                    page.Bool(cfg => cfg.DeterministicBreedingRng, optionId: "DeterministicBreedingRng", tooltip: true);
                    page.Float(cfg => cfg.DailyLuckBonus, optionId: "DailyLuckBonus", tooltip: true, min: 0f, max: 1f, interval: 0.01f);

                    page.Separator(simple: true);

                    page.Float(
                        "NormalEggChance",
                        getValue: () => GetEggChance(configProvider.Get(), ItemType.Normal),
                        setValue: v => SetEggChance(configProvider.Get(), ItemType.Normal, v),
                        tooltip: true,
                        min: 0f,
                        max: 1f,
                        interval: 0.1f);

                    page.Float(
                        "LargeEggChance",
                        getValue: () => GetEggChance(configProvider.Get(), ItemType.Large),
                        setValue: v => SetEggChance(configProvider.Get(), ItemType.Large, v),
                        tooltip: true,
                        min: 0f,
                        max: 1f,
                        interval: 0.1f);

                    page.Float(
                        "FertilizedEggChance",
                        getValue: () => GetEggChance(configProvider.Get(), ItemType.FertilizedEgg),
                        setValue: v => SetEggChance(configProvider.Get(), ItemType.FertilizedEgg, v),
                        tooltip: true,
                        min: 0f,
                        max: 1f,
                        interval: 0.1f);
                }
            );

            layout.Page(
                "advanced",
                title: () => core.Translation.GetString("config.page.advanced.name"),
                build: page =>
                {
                    page.Bool(cfg => cfg.StabilizeFarmAnimalsAssetOrder, optionId: "StabilizeFarmAnimalsAssetOrder", tooltip: true);
                }
            );

            layout.Page(
                "debug",
                title: () => core.Translation.GetString("config.page.debug.name"),
                build: page =>
                {
                    page.Bool(cfg => cfg.ShowEdits, optionId: "ShowEdits", tooltip: true);
                    page.Bool(cfg => cfg.ShowAnimalKeys, optionId: "ShowAnimalKeys", tooltip: true);

                    page.Separator(simple: true);

                    page.Bool(cfg => cfg.VerboseLogging, optionId: "VerboseLogging", tooltip: true);
                    page.Bool(cfg => cfg.TestingLogging, optionId: "TestingLogging", tooltip: true);
                }
            );
        });
    }

    private static float GetEggChance(ModConfig cfg, ItemType type)
        => cfg.EggChances.TryGetValue(type, out var v) ? v : 0f;

    private static void SetEggChance(ModConfig cfg, ItemType type, float value)
        => cfg.EggChances[type] = value;
}
