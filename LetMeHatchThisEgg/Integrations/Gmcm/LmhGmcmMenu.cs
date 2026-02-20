using System;
using System.Collections.Generic;
using System.Linq;
using LetMeHatchThisEgg.Config;
using Mikew.BirdsOfAFeather.Framework;
using Mikew.BirdsOfAFeather.Helpers.Config;
using Mikew.BirdsOfAFeather.Integrations.Gmcm;
using StardewValley.GameData.FarmAnimals;
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

                root.Link("edits");
                root.Link("cloning");
                root.Link("colors");
                root.Link("breeding");
                root.Link("advanced");
                root.Link("debug");
            });

            layout.Page("edits", page =>
            {
                page.Section(core.Translation.GetString("config.page.edits.name"), tooltip: false);
                page.Paragraph(core.Translation.GetString("config.edits.paragraph"));

                page.Separator(simple: true);

                page.Link("editsEggList");
                page.Link("editsBirthText");
                page.Link("editsCloneEggs");
            });

            layout.Page("editsEggList", page =>
                BuildPerAnimalOverridesPage(page, core, configProvider, EditType.EggList));

            layout.Page("editsBirthText", page =>
                BuildPerAnimalOverridesPage(page, core, configProvider, EditType.BirthText));

            layout.Page("editsCloneEggs", page =>
                BuildPerAnimalOverridesPage(page, core, configProvider, EditType.CloneEggs));

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
                    page.ParagraphGmcmOptionsStatus();
                    page.Separator(simple: true);

                    page.Bool(cfg => cfg.ShowEdits, optionId: "ShowEdits", tooltip: true);
                    page.Bool(cfg => cfg.ShowAnimalKeys, optionId: "ShowAnimalKeys", tooltip: true);

                    page.Separator(simple: true);

                    page.Bool(cfg => cfg.VerboseLogging, optionId: "VerboseLogging", tooltip: true);
                    page.Bool(cfg => cfg.TestingLogging, optionId: "TestingLogging", tooltip: true);
                }
            );
        });
    }

    private static void BuildPerAnimalOverridesPage(
        GmcmPageBuilder<ModConfig> page,
        CoreContext core,
        SmapiConfigProvider<ModConfig> configProvider,
        EditType editType)
    {
        var cfg = configProvider.Get();
        var animals = GetAnimalOptions(core, cfg);

        page.Paragraph(core.Translation.GetString("config.edits.perAnimal.paragraph"));

        if (animals.Count <= 0)
        {
            page.Separator(simple: true);
            page.Paragraph(core.Translation.GetString("config.edits.noAnimals"));
            return;
        }

        page.Separator(simple: true);

        foreach (var a in animals)
        {
            var animalKey = a.Key;
            var label = a.Label;

            var elementId = GmcmDynamicIds.ElementId(elementPrefix: editType.ToString(), key: animalKey);
            page.BoolLabel(
                elementId: elementId,
                name: () => label,
                tooltip: () => core.Translation.GetString("config.edits.animal.tooltip", animalKey),
                getValue: () => IsEnabled(GetOverrides(cfg, editType), animalKey),
                setValue: v => SetEnabled(GetOverrides(cfg, editType), animalKey, v)
            );
        }
    }

    private static IReadOnlyList<AnimalOption> GetAnimalOptions(CoreContext core, ModConfig cfg)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        void Add(string key, string label)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            if (!map.ContainsKey(key))
                map[key] = string.IsNullOrWhiteSpace(label) ? key : label;
        }

        try
        {
            var data = core.Helper.GameContent.Load<Dictionary<string, FarmAnimalData>>(
                Mikew.BirdsOfAFeather.EnumsAndStrings.Strings.AssetNames.FarmAnimals
            );

            foreach (var (key, animal) in data)
                Add(key, animal?.DisplayName ?? key);
        }
        catch
        {
            // no-op: GMCM still works even if the asset isn't available yet.
        }

        foreach (var key in cfg.EditedEggListsOptions.Keys)
            Add(key, key);
        foreach (var key in cfg.EditedBirthTextOptions.Keys)
            Add(key, key);
        foreach (var key in cfg.ClonedVanillaEggsOptions.Keys)
            Add(key, key);

        return map
            .Select(kvp => new AnimalOption(kvp.Key, kvp.Value))
            .OrderBy(a => a.Label, StringComparer.OrdinalIgnoreCase)
            .ThenBy(a => a.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static Dictionary<string, bool> GetOverrides(ModConfig cfg, EditType editType)
        => editType switch
        {
            EditType.EggList => cfg.EditedEggListsOptions,
            EditType.BirthText => cfg.EditedBirthTextOptions,
            EditType.CloneEggs => cfg.ClonedVanillaEggsOptions,
            _ => throw new ArgumentOutOfRangeException(nameof(editType), editType, null)
        };

    private static bool IsEnabled(Dictionary<string, bool> overrides, string key)
        => !overrides.TryGetValue(key, out var value) || value;

    private static void SetEnabled(Dictionary<string, bool> overrides, string key, bool enabled)
    {
        if (enabled)
            overrides.Remove(key);
        else
            overrides[key] = false;
    }

    private sealed record AnimalOption(string Key, string Label);

    private static float GetEggChance(ModConfig cfg, ItemType type)
        => cfg.EggChances.TryGetValue(type, out var v) ? v : 0f;

    private static void SetEggChance(ModConfig cfg, ItemType type, float value)
        => cfg.EggChances[type] = value;
}
