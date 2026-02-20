using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using LetMeHatchThisEgg.Config;
using Mikew.BirdsOfAFeather.Framework;
using Mikew.BirdsOfAFeather.Helpers.Config;
using Mikew.BirdsOfAFeather.Integrations.Gmcm;
using StardewValley.GameData.FarmAnimals;
using StardewValley.GameData.Objects;
using static LetMeHatchThisEgg.EnumsAndStrings.Enums;
using StardewModdingAPI;
using StardewValley;
using LetMeHatchThisEgg.Framework;
using StardewModdingAPI.Events;

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

        Action<GmcmMenuLayout<ModConfig>> define = layout =>
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
        };

        host.RebuildLayout(define);

        
        core.Helper.Events.GameLoop.SaveLoaded += (_, _) =>
        {
            // Rebuild again once the coordinator finishes early init and the asset manager is available.
            var tries = 0;
            EventHandler<UpdateTickedEventArgs>? onTick = null;
            onTick = (_, __) =>
            {
                tries++;
                if (ModEntry.CurrentAssetManager is null && tries < 120)
                    return;

                core.Helper.Events.GameLoop.UpdateTicked -= onTick;
                host.RebuildLayout(define, force: true);
            };

            core.Helper.Events.GameLoop.UpdateTicked += onTick;
        };



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

                    page.Separator(simple: true);

                    page.ParagraphDynamic(
                        text: () => core.Translation.GetString("config.colors.paragraph"),
                        id: "colors_intro"
                    );

                    page.Separator(simple: true);

                    page.Link("eggPalette");
                    page.Link("eggColorOverrides");
                }
            );

            layout.Page("eggPalette", page => BuildEggPalettePage(page, core, configProvider));

            layout.Page("eggColorOverrides", page => BuildEggColorOverridesPage(page, core, configProvider));


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

                    page.Separator(simple: true);

                    page.Link("itemMappings");
                }
            );

            
            layout.Page(
                "itemMappings",
                title: () => core.Translation.GetString("config.page.itemMappings.name"),
                build: page => BuildItemMappingsIndexPage(page, core)
            );

            layout.Page(
                "itemMappingsEgg",
                title: () => core.Translation.GetString("config.page.itemMappingsEgg.name"),
                build: page => BuildItemMappingsCloneTypePage(page, core, configProvider, CloneItemType.Egg)
            );

            layout.Page(
                "itemMappingsMilk",
                title: () => core.Translation.GetString("config.page.itemMappingsMilk.name"),
                build: page => BuildItemMappingsCloneTypePage(page, core, configProvider, CloneItemType.Milk)
            );

            layout.Page(
                "itemMappingsMisc",
                title: () => core.Translation.GetString("config.page.itemMappingsMisc.name"),
                build: page => BuildItemMappingsCloneTypePage(page, core, configProvider, CloneItemType.Misc)
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


    private static void BuildEggPalettePage(
        GmcmPageBuilder<ModConfig> page,
        CoreContext core,
        SmapiConfigProvider<ModConfig> configProvider)
    {
        page.ParagraphGmcmOptionsStatus();
        page.Separator(simple: true);

        page.ParagraphDynamic(
            text: () => core.Translation.GetString("config.eggPalette.paragraph"),
            id: "eggPalette_intro"
        );

        var cfg = configProvider.Get();
        var eggs = GetEggOptions(core, cfg);
        var colorGroups = GetPaletteColorGroups();

        if (eggs.Count <= 0)
        {
            page.Separator(simple: true);
            page.Paragraph(core.Translation.GetString("config.eggPalette.noEggs"));
            return;
        }

        foreach (var egg in eggs)
        {
            page.Separator(simple: true);
            page.SectionLabel(() => egg.Label);

            if (egg.Id == "305")
                page.ParagraphDynamic(() => core.Translation.GetString("config.eggPalette.voidEggNote"), id: $"voidEgg_{egg.Id}");

            foreach (var colorKey in colorGroups)
            {
                var elementId = GmcmDynamicIds.ElementId(elementPrefix: $"egg_{egg.Id}", key: colorKey);

                page.BoolLabel(
                    elementId: elementId,
                    name: () => core.Translation.GetString($"config.color.{colorKey}.name"),
                    tooltip: () => core.Translation.GetString(
                        "config.eggPalette.allowColor.description",
                        core.Translation.GetString($"config.color.{colorKey}.name"),
                        egg.Label
                    ),
                    getValue: () => GetEggPaletteEnabled(configProvider.Get(), egg.Id, colorGroups, colorKey),
                    setValue: v => SetEggPaletteEnabled(configProvider.Get(), egg.Id, colorGroups, colorKey, v)
                );
            }
        }
    }

    private static void BuildEggColorOverridesPage(
        GmcmPageBuilder<ModConfig> page,
        CoreContext core,
        SmapiConfigProvider<ModConfig> configProvider)
    {
        page.ParagraphGmcmOptionsStatus();
        page.Separator(simple: true);

        page.ParagraphDynamic(
            text: () => core.Translation.GetString("config.eggOverride.paragraph"),
            id: "eggOverride_intro"
        );

        var cfg = configProvider.Get();
        var eggs = GetEggOptions(core, cfg);

        if (eggs.Count <= 0)
        {
            page.Separator(simple: true);
            page.Paragraph(core.Translation.GetString("config.eggPalette.noEggs"));
            return;
        }

        foreach (var egg in eggs)
        {
            page.Separator(simple: true);
            page.SectionLabel(() => egg.Label);

            page.BoolLabel(
                elementId: GmcmDynamicIds.ElementId("eggOverrideEnabled", egg.Id),
                name: () => core.Translation.GetString("config.eggOverride.enabled.name"),
                tooltip: () => core.Translation.GetString("config.eggOverride.enabled.description", egg.Label),
                getValue: () => IsEggColorOverrideEnabled(configProvider.Get(), egg.Id),
                setValue: v => SetEggColorOverrideEnabled(configProvider.Get(), egg.Id, v)
            );

            page.ColorSwatchLabel(
                elementId: GmcmDynamicIds.ElementId("eggOverrideColor", egg.Id),
                name: () => core.Translation.GetString("config.eggOverride.color.name"),
                tooltip: () => core.Translation.GetString("config.eggOverride.color.description", egg.Label),
                getValue: () => GetEggColorOverride(configProvider.Get(), egg.Id),
                setValue: v => SetEggColorOverride(configProvider.Get(), egg.Id, v),
                showAlpha: false
            );
        }
    }

    private static IReadOnlyList<string> GetPaletteColorGroups()
    {
        // "gold" is intentionally excluded because LMH never randomizes "gold" from palettes.
        var saved = new LetMeHatchThisEgg.Helpers.SavedColors();
        return saved._colorDictionary.Keys
            .Where(k => !string.Equals(k, "gold", StringComparison.OrdinalIgnoreCase))
            .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool GetEggPaletteEnabled(ModConfig cfg, string eggId, IReadOnlyList<string> colorGroups, string colorKey)
    {
        var map = EnsureEggPalette(cfg, eggId, colorGroups);
        return map.TryGetValue(colorKey, out var enabled) && enabled;
    }

    private static void SetEggPaletteEnabled(ModConfig cfg, string eggId, IReadOnlyList<string> colorGroups, string colorKey, bool enabled)
    {
        var map = EnsureEggPalette(cfg, eggId, colorGroups);
        map[colorKey] = enabled;

        // keep config clean: "all true" means default behavior, so remove the override map.
        if (colorGroups.All(k => map.TryGetValue(k, out var v) && v))
            cfg.EggColors.Remove(eggId);
    }

    private static Dictionary<string, bool> EnsureEggPalette(ModConfig cfg, string eggId, IReadOnlyList<string> colorGroups)
    {
        if (!cfg.EggColors.TryGetValue(eggId, out var map) || map is null)
        {
            map = colorGroups.ToDictionary(k => k, _ => true, StringComparer.OrdinalIgnoreCase);
            cfg.EggColors[eggId] = map;
            return map;
        }

        foreach (var key in colorGroups)
        {
            if (!map.ContainsKey(key))
                map[key] = true;
        }

        return map;
    }

    private static bool IsEggColorOverrideEnabled(ModConfig cfg, string eggId)
        => cfg.EggColors_ColorObject.TryGetValue(eggId, out var c) && c != Color.Transparent;

    private static void SetEggColorOverrideEnabled(ModConfig cfg, string eggId, bool enabled)
    {
        if (!enabled)
        {
            cfg.EggColors_ColorObject.Remove(eggId);
            return;
        }

        if (!cfg.EggColors_ColorObject.ContainsKey(eggId))
            cfg.EggColors_ColorObject[eggId] = Color.White;
    }

    private static Color GetEggColorOverride(ModConfig cfg, string eggId)
        => cfg.EggColors_ColorObject.TryGetValue(eggId, out var c) ? c : Color.White;

    private static void SetEggColorOverride(ModConfig cfg, string eggId, Color value)
    {
        if (value == Color.Transparent)
        {
            cfg.EggColors_ColorObject.Remove(eggId);
            return;
        }

        cfg.EggColors_ColorObject[eggId] = value;
    }

    private static IReadOnlyList<EggOption> GetEggOptions(CoreContext core, ModConfig cfg)
    {
        var eggs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        void Add(string id, string label)
        {
            if (string.IsNullOrWhiteSpace(id))
                return;

            eggs.TryAdd(id, string.IsNullOrWhiteSpace(label) ? id : label);
        }

        try
        {
            var animals = core.Helper.GameContent.Load<Dictionary<string, FarmAnimalData>>(
                Mikew.BirdsOfAFeather.EnumsAndStrings.Strings.AssetNames.FarmAnimals
            );

            var objects = core.Helper.GameContent.Load<Dictionary<string, ObjectData>>(
                Mikew.BirdsOfAFeather.EnumsAndStrings.Strings.AssetNames.Objects
            );

            foreach (var animal in animals.Values)
            {
                if (animal?.ProduceItemIds is not null)
                    foreach (var id in animal.ProduceItemIds)
                        AddEggIfApplicable(id);

                if (animal?.DeluxeProduceItemIds is not null)
                    foreach (var id in animal.DeluxeProduceItemIds)
                        AddEggIfApplicable(id);
            }

            void AddEggIfApplicable(string id)
            {
                if (!objects.TryGetValue(id, out var obj))
                    return;

                var name = obj?.DisplayName ?? obj?.Name ?? id;
                if (name.Contains("Egg", StringComparison.OrdinalIgnoreCase))
                    Add(id, name);
            }
        }
        catch
        {
            // no-op: fall back to config-derived eggs.
        }

        foreach (var id in cfg.EggColors.Keys)
            Add(id, id);
        foreach (var id in cfg.EggColors_ColorObject.Keys)
            Add(id, id);

        return eggs
            .Select(kvp => new EggOption(kvp.Key, kvp.Value))
            .OrderBy(e => e.Label, StringComparer.OrdinalIgnoreCase)
            .ThenBy(e => e.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private sealed record EggOption(string Id, string Label);


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