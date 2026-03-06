using System;
using System.Collections.Generic;
using System.Linq;
using LetMeHatch.Config;
using LetMeHatch.GameAssetManagement;
using LetMeHatch.Integrations.StardewUi.Breeding;
using LetMeHatch.Models;
using Mikew.NestCore.Framework;
using Mikew.NestCore.Helpers.Config;
using Mikew.NestCore.Integrations.Gmcm;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.GameData.FarmAnimals;
using StardewValley.GameData.Objects;
using static LetMeHatch.EnumsAndStrings.Enums;
using Mikew.NestCore.Helpers;

namespace LetMeHatch.Integrations.Gmcm;

public static class LmhGmcmMenu
{
    private const float MinChance = 0f;
    private const float MaxChance = 1f;
    private const float ChanceInterval = 0.1f;
    private static readonly LmhBreedingEditorService BreedingEditorService = new();

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
        void Define(GmcmMenuLayout<ModConfig> layout)
        {
            BuildRoot(layout, core);
            BuildEditsPages(layout, core, configProvider);
            BuildCloningPage(layout, core, configProvider);
            BuildColorsPages(layout, core, configProvider);
            BuildBreedingPages(layout, core, configProvider);
            BuildAdvancedPages(layout, core, configProvider);
            BuildDebugPage(layout, core, configProvider);
        }

        host.RebuildLayoutSaveAware(
            define: Define,
            force: true,
            rebuildOnSaveLoaded: true,
            rebuildOnReturnedToTitle: false,
            saveLoadedReady: () => ModEntry.CurrentAssetManager is not null,
            saveLoadedMaxTicks: 120
        );
    }

    private static void BuildRoot(GmcmMenuLayout<ModConfig> layout, CoreContext core)
    {
        layout.Root(root =>
        {
            root.Section("general");
            root.Bool(cfg => cfg.EditEggListByName, optionId: "general.editEggListByName", tooltip: true);
            root.Bool(cfg => cfg.EditBirthText, optionId: "general.editBirthText", tooltip: true);
            root.Bool(cfg => cfg.AlternateBirthText, optionId: "general.alternateBirthText", tooltip: true);

            root.Separator(simple: true);

            root.Link("edits");
            root.Link("cloning");
            root.Link("colors");
            root.Link("breeding");
            root.Link("advanced");
            root.Link("debug");
        });
    }

    private static void BuildEditsPages(
        GmcmMenuLayout<ModConfig> layout,
        CoreContext core,
        SmapiConfigProvider<ModConfig> configProvider)
    {
        layout.Page("edits", page =>
        {
            page.Section(core.Translation.GetString("config.page.edits.name"), tooltip: false);
            page.ParagraphDynamic(() => core.Translation.GetString("config.edits.paragraph"), id: "edits_paragraph");
            page.Separator(simple: true);
            page.Link("edits.eggList");
            page.Link("edits.birthText");
            page.Link("edits.cloneEggs");
        });

        layout.Pages(new (string PageId, Action<GmcmPageBuilder<ModConfig>> Build)[]
        {
            ("edits.eggList", page => BuildPerAnimalOverridesPage(page, core, configProvider, EditType.EggList)),
            ("edits.birthText", page => BuildPerAnimalOverridesPage(page, core, configProvider, EditType.BirthText)),
            ("edits.cloneEggs", page => BuildPerAnimalOverridesPage(page, core, configProvider, EditType.CloneEggs)),
        });
    }

    private static void BuildCloningPage(
        GmcmMenuLayout<ModConfig> layout,
        CoreContext core,
        SmapiConfigProvider<ModConfig> configProvider)
    {
        layout.Page(
            "cloning",
            title: () => core.Translation.GetString("config.page.cloning.name"),
            build: page =>
            {
                page.Bool(cfg => cfg.CloneVanillaEggs, optionId: "cloning.cloneVanillaEggs", tooltip: true);
                page.Bool(cfg => cfg.CloneMachineRules, optionId: "cloning.cloneMachineRules", tooltip: true);
                page.Bool(cfg => cfg.UseDynamicCloneTextures, optionId: "cloning.useDynamicCloneTextures", tooltip: true);
                page.Bool(cfg => cfg.DinoEggAsArtifact, optionId: "cloning.dinoEggAsArtifact", tooltip: true);
                page.Bool(cfg => cfg.OnlyFertilizedEggs, optionId: "cloning.onlyFertilizedEggs", tooltip: true);
                page.Bool(cfg => cfg.ColorClonedMilk, optionId: "cloning.colorClonedMilk", tooltip: true);
                page.Bool(cfg => cfg.ExclusiveRabbitWool, optionId: "cloning.exclusiveRabbitWool", tooltip: true);
            }
        );
    }

    private static void BuildColorsPages(
        GmcmMenuLayout<ModConfig> layout,
        CoreContext core,
        SmapiConfigProvider<ModConfig> configProvider)
    {
        layout.Page(
            "colors",
            title: () => core.Translation.GetString("config.page.colors.name"),
            build: page =>
            {
                page.Bool(cfg => cfg.ChangeEggColors, optionId: "colors.changeEggColors", tooltip: true);
                page.Enum(cfg => cfg.DynamicCloneTintMode, optionId: "colors.dynamicCloneTintMode", tooltip: true);
                page.Bool(cfg => cfg.LinkAnimalColorToEgg, optionId: "colors.linkAnimalColorToEgg", tooltip: true);

                page.Separator(simple: true);
                page.ParagraphDynamic(() => core.Translation.GetString("config.colors.paragraph"), id: "colors_intro");
                page.Separator(simple: true);

                page.Link("colors.eggPalette");
                page.Link("colors.eggColorOverrides");
            }
        );

        layout.Pages(new (string PageId, Action<GmcmPageBuilder<ModConfig>> Build)[]
        {
            ("colors.eggPalette", page => BuildEggPalettePage(page, core, configProvider)),
            ("colors.eggColorOverrides", page => BuildEggColorOverridesPage(page, core, configProvider)),
        });
    }

    private static void BuildBreedingPages(
        GmcmMenuLayout<ModConfig> layout,
        CoreContext core,
        SmapiConfigProvider<ModConfig> configProvider)
    {
        layout.Page(
            "breeding",
            title: () => core.Translation.GetString("config.page.breeding.name"),
            build: page =>
            {
                page.Bool(cfg => cfg.BreedingSystemEnabled, optionId: "breeding.systemEnabled", tooltip: true);
                page.Bool(cfg => cfg.DeterministicBreedingRng, optionId: "breeding.deterministicRng", tooltip: true);
                page.Float(cfg => cfg.DailyLuckBonus, optionId: "breeding.dailyLuckBonus", tooltip: true, min: MinChance, max: MaxChance, interval: 0.01f);

                page.Separator(simple: true);

                page.Float(
                    "NormalEggChance",
                    getValue: () => GetEggChance(configProvider.Get(), ItemType.Normal),
                    setValue: v => SetEggChance(configProvider.Get(), ItemType.Normal, v),
                    tooltip: true,
                    min: MinChance,
                    max: MaxChance,
                    interval: ChanceInterval);

                page.Float(
                    "LargeEggChance",
                    getValue: () => GetEggChance(configProvider.Get(), ItemType.Large),
                    setValue: v => SetEggChance(configProvider.Get(), ItemType.Large, v),
                    tooltip: true,
                    min: MinChance,
                    max: MaxChance,
                    interval: ChanceInterval);

                page.Float(
                    "FertilizedEggChance",
                    getValue: () => GetEggChance(configProvider.Get(), ItemType.FertilizedEgg),
                    setValue: v => SetEggChance(configProvider.Get(), ItemType.FertilizedEgg, v),
                    tooltip: true,
                    min: MinChance,
                    max: MaxChance,
                    interval: ChanceInterval);

                page.Separator(simple: true);
                page.Link("breeding.packs");
            }
        );

        // Strict parity restore: old GMCM's per-content-pack -> per-female -> per-male tuning flow.
        layout.Page("breeding.packs", page => BuildBreedingPackIndexPage(page, core));

        var packPages = new List<(string PageId, Func<string> Title, Action<GmcmPageBuilder<ModConfig>> Build)>();
        var femalePages = new List<(string PageId, Func<string> Title, Action<GmcmPageBuilder<ModConfig>> Build)>();

        var manager = ModEntry.CurrentAssetManager;
        var packs = manager is null
            ? Array.Empty<LmhBreedingPackRecord>()
            : BreedingEditorService.LoadPacksSnapshot(manager.LoadedFemaleBreedingData, manager.LoadedContentPacks);

        foreach (var pack in packs)
        {
            var packPageId = BuildPackPageId(pack.ContentPackId);
            var packTitle = pack.DisplayName;
            var capturedPackId = pack.ContentPackId;

            packPages.Add((
                packPageId,
                () => packTitle,
                page =>
                {
                    page.SectionLabel(() => core.Translation.GetString("config.section.breeding.females.text"));

                    foreach (var female in pack.FemaleKeys)
                        page.Link(BuildFemalePageId(capturedPackId, female));
                }));

            foreach (var female in pack.FemaleKeys)
            {
                var femalePageId = BuildFemalePageId(pack.ContentPackId, female);
                var femaleTitle = female;
                var capturedFemale = female;

                femalePages.Add((
                    femalePageId,
                    () => femaleTitle,
                    page => BuildFemaleBreedingPage(page, core, capturedPackId, capturedFemale)));
            }
        }

        layout.Pages(packPages);
        layout.Pages(femalePages);
    }

    private static void BuildBreedingPackIndexPage(GmcmPageBuilder<ModConfig> page, CoreContext core)
    {
        page.Section("breeding.loadedSystems");
        var manager = ModEntry.CurrentAssetManager;
        var packs = manager is null
            ? Array.Empty<LmhBreedingPackRecord>()
            : BreedingEditorService.LoadPacksSnapshot(manager.LoadedFemaleBreedingData, manager.LoadedContentPacks);
        if (packs.Count == 0)
        {
            page.Separator(simple: true);
            page.ParagraphDynamic(() => core.Translation.GetString("config.notavailable"), id: "notavailable");
            return;
        }

        page.Separator(simple: true);
        foreach (var pack in packs)
            page.Link(BuildPackPageId(pack.ContentPackId));
    }

    private static void BuildFemaleBreedingPage(
        GmcmPageBuilder<ModConfig> page,
        CoreContext core,
        string contentPackId,
        string femaleKey)
    {
        var manager = ModEntry.CurrentAssetManager;
        if (manager is null)
        {
            page.ParagraphDynamic(() => core.Translation.GetString("config.notavailable"), id: "notavailable");
            return;
        }

        if (!TryGetFemaleBreedingContext(manager, contentPackId, femaleKey, out var femaleData, out var femaleConfig))
        {
            page.ParagraphDynamic(
                () => core.Translation.GetString("config.breeding.missingData", new { femaleKey, contentPackId }),
                id: "breeding_missing"
            );
            return;
        }

        if (!BreedingEditorService.TryLoadFemaleRecord(
                manager.LoadedFemaleBreedingData,
                manager.BreedingConfigsByPack,
                contentPackId,
                femaleKey,
                out var femaleRecord))
        {
            page.ParagraphDynamic(
                () => core.Translation.GetString("config.breeding.missingData", new { femaleKey, contentPackId }),
                id: "breeding_missing_snapshot"
            );
            return;
        }

        page.BoolLabel(
            elementId: "ApplyDailyBoost",
            name: () => core.Translation.GetString("config.breeding.applyDailyBoost.name"),
            tooltip: () => core.Translation.GetString("config.breeding.applyDailyBoost.description"),
            getValue: () => femaleData.ApplyDailyLuckBoost,
            setValue: v => BreedingEditorService.SetApplyDailyLuckBoost(femaleData, femaleConfig, v)
        );

        if (femaleRecord.AdjustableEggChances)
        {
            page.Separator(simple: true);
            page.SectionLabel(() => core.Translation.GetString("config.section.breeding.normalHatching.text"));

            page.FloatLabel(
                elementId: "NormalEggChance",
                name: () => core.Translation.GetString("config.breeding.normalEggChance.name"),
                tooltip: () => core.Translation.GetString("config.breeding.normalEggChance.description"),
                getValue: () => GetBreedingEggChance(femaleConfig, ItemType.Normal),
                setValue: v => BreedingEditorService.SetEggChance(femaleData, femaleConfig, ItemType.Normal, v),
                min: MinChance,
                max: MaxChance,
                interval: ChanceInterval
            );

            page.FloatLabel(
                elementId: "LargeEggChance",
                name: () => core.Translation.GetString("config.breeding.largeEggChance.name"),
                tooltip: () => core.Translation.GetString("config.breeding.largeEggChance.description"),
                getValue: () => GetBreedingEggChance(femaleConfig, ItemType.Large),
                setValue: v => BreedingEditorService.SetEggChance(femaleData, femaleConfig, ItemType.Large, v),
                min: MinChance,
                max: MaxChance,
                interval: ChanceInterval
            );

            page.SectionLabel(() => core.Translation.GetString("config.section.breeding.fertilizedHatching.text"));
            page.FloatLabel(
                elementId: "FertilizedEggChance",
                name: () => core.Translation.GetString("config.breeding.fertilizedEggChance.name"),
                tooltip: () => core.Translation.GetString("config.breeding.fertilizedEggChance.description"),
                getValue: () => GetBreedingEggChance(femaleConfig, ItemType.FertilizedEgg),
                setValue: v => BreedingEditorService.SetEggChance(femaleData, femaleConfig, ItemType.FertilizedEgg, v),
                min: MinChance,
                max: MaxChance,
                interval: ChanceInterval
            );
        }

        page.Separator(simple: true);
        page.SectionLabel(() => core.Translation.GetString("config.section.breeding.males.text"));

        foreach (var maleRecord in femaleRecord.Males)
        {
            var maleKey = maleRecord.MaleKey;
            var maleData = femaleData.Males[maleKey];
            var maleLabel = maleRecord.IsCloned
                ? core.Translation.GetString("config.breeding.males.cloned.label")
                : maleKey;

            page.Separator(simple: true);
            page.SectionLabel(() => maleLabel);

            page.BoolLabel(
                elementId: $"EnableMale_{maleKey}",
                name: () => core.Translation.GetString("config.breeding.enableMale.name"),
                tooltip: () => core.Translation.GetString("config.breeding.enableMale.description"),
                getValue: () => BreedingEditorService.GetMaleEnabled(femaleConfig, maleKey),
                setValue: v => BreedingEditorService.SetMaleEnabled(femaleData, femaleConfig, maleKey, v)
            );

            if (maleRecord.IsCloned)
            {
                page.BoolLabel(
                    elementId: $"AddAsAlternatePurchase_{maleKey}",
                    name: () => core.Translation.GetString("config.breeding.addAsAlternatePurchase.name"),
                    tooltip: () => core.Translation.GetString("config.breeding.addAsAlternatePurchase.description"),
                    getValue: () => femaleConfig.MaleConfig.AddAsAlternatePurchase,
                    setValue: v => BreedingEditorService.SetAddAsAlternatePurchase(femaleData, femaleConfig, v)
                );
            }

            page.ParagraphDynamic(() => BuildMaleEggsParagraph(core.Translation, maleData), id: $"maleEggs_{contentPackId}_{femaleKey}_{maleKey}");

            if (maleRecord.AdjustableMaleChances)
            {
                page.FloatLabel(
                    elementId: $"BoostByMale_{maleKey}",
                    name: () => core.Translation.GetString("config.breeding.boostByMale.name"),
                    tooltip: () => core.Translation.GetString("config.breeding.boostByMale.description"),
                    getValue: () => BreedingEditorService.GetBoostByMale(femaleConfig, maleKey),
                    setValue: v => BreedingEditorService.SetBoostByMale(femaleData, femaleConfig, maleKey, v),
                    min: MinChance,
                    max: MaxChance,
                    interval: ChanceInterval
                );

                page.FloatLabel(
                    elementId: $"MaleChance_{maleKey}",
                    name: () => core.Translation.GetString("config.breeding.maleChance.name"),
                    tooltip: () => core.Translation.GetString("config.breeding.maleChance.description"),
                    getValue: () => BreedingEditorService.GetMaleChance(femaleConfig, maleKey),
                    setValue: v => BreedingEditorService.SetMaleChance(femaleData, femaleConfig, maleKey, v),
                    min: MinChance,
                    max: MaxChance,
                    interval: ChanceInterval
                );
            }
        }
    }

    private static void BuildAdvancedPages(
        GmcmMenuLayout<ModConfig> layout,
        CoreContext core,
        SmapiConfigProvider<ModConfig> configProvider)
    {
        layout.Page(
            "advanced",
            title: () => core.Translation.GetString("config.page.advanced.name"),
            build: page =>
            {
                page.Bool(cfg => cfg.StabilizeFarmAnimalsAssetOrder, optionId: "advanced.stabilizeFarmAnimalsAssetOrder", tooltip: true);

                page.Separator(simple: true);
                page.Link("advanced.itemMappings");
            }
        );

        layout.Page(
            "advanced.itemMappings",
            title: () => core.Translation.GetString("config.page.advanced.itemMappings.name"),
            build: page => BuildItemMappingsIndexPage(page, core)
        );

        layout.Page(
            "advanced.itemMappings.egg",
            title: () => core.Translation.GetString("config.page.advanced.itemMappings.egg.name"),
            build: page => BuildItemMappingsCloneTypePage(page, core, configProvider, CloneItemType.Egg)
        );

        layout.Page(
            "advanced.itemMappings.milk",
            title: () => core.Translation.GetString("config.page.advanced.itemMappings.milk.name"),
            build: page => BuildItemMappingsCloneTypePage(page, core, configProvider, CloneItemType.Milk)
        );

        layout.Page(
            "advanced.itemMappings.misc",
            title: () => core.Translation.GetString("config.page.advanced.itemMappings.misc.name"),
            build: page => BuildItemMappingsCloneTypePage(page, core, configProvider, CloneItemType.Misc)
        );
    }

    private static void BuildItemMappingsIndexPage(GmcmPageBuilder<ModConfig> page, CoreContext core)
    {
        page.ParagraphDynamic(() => core.Translation.GetString("config.itemMappings.paragraph"), id: "itemmappings_paragraph");
        page.Separator(simple: true);
        page.Link("advanced.itemMappings.egg");
        page.Link("advanced.itemMappings.milk");
        page.Link("advanced.itemMappings.misc");
    }

    private static void BuildItemMappingsCloneTypePage(
        GmcmPageBuilder<ModConfig> page,
        CoreContext core,
        SmapiConfigProvider<ModConfig> configProvider,
        CloneItemType cloneType)
    {
        var manager = ModEntry.CurrentAssetManager;
        if (manager is null)
        {
            page.ParagraphDynamic(() => core.Translation.GetString("config.itemMappings.noSave"), id: "itemmappings_nosave");
            return;
        }

        var cfg = configProvider.Get();
        var found = manager.SavedItemsData.FoundItems.TryGetValue(cloneType, out var list)
            ? list
            : new List<string>();

        var allowed = found.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        var rows = manager.SavedItemsData.AnimalItemRelation
            .Where(kvp => kvp.Value.TryGetValue(cloneType, out var map) && map is not null)
            .OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (rows.Count == 0)
        {
            page.ParagraphDynamic(() => core.Translation.GetString("config.itemMappings.noAnimals"), id: "itemmappings_noanimals");
            return;
        }

        foreach (var (animalKey, byType) in rows)
        {
            page.Separator(simple: true);
            page.SectionLabel(() => animalKey);

            foreach (var itemType in new[] { ItemType.Normal, ItemType.Large })
            {
                var elementId = $"{cloneType}_{animalKey}_{itemType}";
                page.TextLabel(
                    elementId: elementId,
                    name: () => itemType == ItemType.Normal
                        ? core.Translation.GetString("config.itemMappings.itemType.normal.name")
                        : core.Translation.GetString("config.itemMappings.itemType.large.name"),
                    tooltip: () => core.Translation.GetString("config.itemMappings.custom.tooltip"),
                    getValue: () => GetItemMapping(cfg, byType, animalKey, cloneType, itemType),
                    setValue: v => SetItemMapping(cfg, byType, animalKey, cloneType, itemType, v),
                    allowedValues: allowed,
                    formatValue: v => FormatMappedItemLabel(v)
                );
            }
        }
    }

    private static string GetItemMapping(
        ModConfig cfg,
        Dictionary<CloneItemType, Dictionary<ItemType, string>?> byType,
        string animalKey,
        CloneItemType cloneType,
        ItemType itemType)
    {
        var configByAnimal = EnsureConfigMapping(cfg, animalKey, cloneType);

        if (!configByAnimal.TryGetValue(itemType, out var value) || value is null)
        {
            value = byType.TryGetValue(cloneType, out var map) && map is not null && map.TryGetValue(itemType, out var existing)
                ? existing
                : string.Empty;

            configByAnimal[itemType] = value;
        }

        return value;
    }

    private static void SetItemMapping(
        ModConfig cfg,
        Dictionary<CloneItemType, Dictionary<ItemType, string>?> byType,
        string animalKey,
        CloneItemType cloneType,
        ItemType itemType,
        string value)
    {
        value ??= string.Empty;

        var configByAnimal = EnsureConfigMapping(cfg, animalKey, cloneType);
        configByAnimal[itemType] = value;

        byType[cloneType] ??= new Dictionary<ItemType, string>();
        byType[cloneType]![itemType] = value;
    }

    private static Dictionary<ItemType, string> EnsureConfigMapping(ModConfig cfg, string animalKey, CloneItemType cloneType)
    {
        if (!cfg.ItemToClone.TryGetValue(animalKey, out var byCloneType) || byCloneType is null)
        {
            byCloneType = new Dictionary<CloneItemType, Dictionary<ItemType, string>>();
            cfg.ItemToClone[animalKey] = byCloneType;
        }

        if (!byCloneType.TryGetValue(cloneType, out var byItemType) || byItemType is null)
        {
            byItemType = new Dictionary<ItemType, string>
            {
                [ItemType.Normal] = string.Empty,
                [ItemType.Large] = string.Empty,
                [ItemType.FertilizedEgg] = string.Empty,
            };
            byCloneType[cloneType] = byItemType;
        }

        return byItemType;
    }

    private static string FormatMappedItemLabel(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return "Off";

        if (Game1.objectData.TryGetValue(itemId, out var data))
            return data.DisplayName ?? data.Name ?? itemId;

        return itemId;
    }

    private static void BuildDebugPage(
        GmcmMenuLayout<ModConfig> layout,
        CoreContext core,
        SmapiConfigProvider<ModConfig> configProvider)
    {
        layout.Page(
            "debug",
            title: () => core.Translation.GetString("config.page.debug.name"),
            build: page =>
            {
                page.ParagraphGmcmOptionsStatus();
                page.Separator(simple: true);

                page.Bool(cfg => cfg.ShowEdits, optionId: "debug.showEdits", tooltip: true);
                page.Bool(cfg => cfg.ShowAnimalKeys, optionId: "debug.showAnimalKeys", tooltip: true);

                page.Separator(simple: true);

                page.Bool(cfg => cfg.VerboseLogging, optionId: "debug.verboseLogging", tooltip: true);
                page.Bool(cfg => cfg.TestingLogging, optionId: "debug.testingLogging", tooltip: true);
            }
        );
    }

    private static void BuildPerAnimalOverridesPage(
        GmcmPageBuilder<ModConfig> page,
        CoreContext core,
        SmapiConfigProvider<ModConfig> configProvider,
        EditType editType)
    {
        var cfg = configProvider.Get();
        var animals = GetAnimalOptions(core, cfg);

        page.ParagraphDynamic(() => core.Translation.GetString("config.edits.perAnimal.paragraph"), id: "edits_peranimal_paragraph");

        if (animals.Count <= 0)
        {
            page.Separator(simple: true);
            page.ParagraphDynamic(() => core.Translation.GetString("config.edits.noAnimals"), id: "edits_noanimals");
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
                tooltip: () => core.Translation.GetString("config.edits.animal.tooltip", new { animalKey }),
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
            page.ParagraphDynamic(() => core.Translation.GetString("config.eggPalette.noEggs"), id: "eggpalette_noeggs");
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
            name: () => core.Translation.TryGet($"config.color.{colorKey}.name", out var t) ? t : colorKey,
                    tooltip: () => core.Translation.GetString("config.eggPalette.allowColor.description"),
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
            page.ParagraphDynamic(() => core.Translation.GetString("config.eggPalette.noEggs"), id: "eggpalette_noeggs");
            return;
        }

        foreach (var egg in eggs)
        {
            page.Separator(simple: true);
            page.SectionLabel(() => egg.Label);

            page.BoolLabel(
                elementId: GmcmDynamicIds.ElementId("eggOverrideEnabled", egg.Id),
                name: () => core.Translation.GetString("config.eggOverride.enabled.name"),
                tooltip: () => core.Translation.GetString("config.eggOverride.enabled.description"),
                getValue: () => IsEggColorOverrideEnabled(configProvider.Get(), egg.Id),
                setValue: v => SetEggColorOverrideEnabled(configProvider.Get(), egg.Id, v)
            );

            page.ColorSwatchLabel(
                elementId: GmcmDynamicIds.ElementId("eggOverrideColor", egg.Id),
                name: () => core.Translation.GetString("config.eggOverride.color.name"),
                tooltip: () => core.Translation.GetString("config.eggOverride.color.description"),
                getValue: () => GetEggColorOverride(configProvider.Get(), egg.Id),
                setValue: v => SetEggColorOverride(configProvider.Get(), egg.Id, v),
                showAlpha: false
            );
        }
    }

    private static IReadOnlyList<string> GetPaletteColorGroups()
    {
        var saved = new LetMeHatch.Helpers.SavedColors();
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

        static void Add(Dictionary<string, string> dst, string id, string label)
        {
            if (string.IsNullOrWhiteSpace(id))
                return;

            dst.TryAdd(id, string.IsNullOrWhiteSpace(label) ? id : label);
        }

        try
        {
            var animals = core.Helper.GameContent.Load<Dictionary<string, FarmAnimalData>>(
                Mikew.NestCore.EnumsAndStrings.Strings.AssetNames.FarmAnimals
            );

            var objects = core.Helper.GameContent.Load<Dictionary<string, ObjectData>>(
                Mikew.NestCore.EnumsAndStrings.Strings.AssetNames.Objects
            );

            foreach (var animal in animals.Values)
            {
                if (animal?.ProduceItemIds is not null)
                {
                    foreach (var produce in animal.ProduceItemIds)
                    {
                        var id = produce.ItemId;
                        if (string.IsNullOrWhiteSpace(id))
                            continue;

                        if (objects.TryGetValue(id, out var obj))
                        {
                            var name = obj?.DisplayName ?? obj?.Name ?? id;
                            if (name.Contains("Egg", StringComparison.OrdinalIgnoreCase))
                                Add(eggs, id, name);
                        }
                    }
                }

                if (animal?.DeluxeProduceItemIds is not null)
                {
                    foreach (var produce in animal.DeluxeProduceItemIds)
                    {
                        var id = produce.ItemId;
                        if (string.IsNullOrWhiteSpace(id))
                            continue;

                        if (objects.TryGetValue(id, out var obj))
                        {
                            var name = obj?.DisplayName ?? obj?.Name ?? id;
                            if (name.Contains("Egg", StringComparison.OrdinalIgnoreCase))
                                Add(eggs, id, name);
                        }
                    }
                }
            }
        }
        catch
        {
            // best-effort
        }

        foreach (var id in cfg.EggColors.Keys)
            Add(eggs, id, id);
        foreach (var id in cfg.EggColors_ColorObject.Keys)
            Add(eggs, id, id);

        return eggs
            .Select(kvp => new EggOption(kvp.Key, kvp.Value))
            .OrderBy(e => e.Label, StringComparer.OrdinalIgnoreCase)
            .ThenBy(e => e.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();
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
                Mikew.NestCore.EnumsAndStrings.Strings.AssetNames.FarmAnimals
            );

            foreach (var (key, animal) in data)
                Add(key, animal?.DisplayName ?? key);
        }
        catch
        {
            // best-effort
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
        => OverrideDictionaries.SetOverride(overrides, key, enabled, defaultValue: true);

    private static float GetEggChance(ModConfig cfg, ItemType type)
        => cfg.EggChances.TryGetValue(type, out var v) ? v : 0f;

    private static void SetEggChance(ModConfig cfg, ItemType type, float value)
        => cfg.EggChances[type] = value;

    private static float GetBreedingEggChance(BreedingConfig cfg, ItemType type)
        => cfg.EggChances.TryGetValue(type, out var v) ? v : 0f;

    private static bool TryGetFemaleBreedingContext(
        GameAssetManager manager,
        string contentPackId,
        string femaleKey,
        out BreedingDataFemale femaleData,
        out BreedingConfig femaleConfig)
    {
        femaleData = null!;
        femaleConfig = null!;

        if (!manager.LoadedFemaleBreedingData.TryGetValue(contentPackId, out var byFemaleData))
            return false;
        if (!byFemaleData.TryGetValue(femaleKey, out femaleData!))
            return false;

        if (!manager.BreedingConfigsByPack.TryGetValue(contentPackId, out var byFemaleConfig))
            return false;
        if (!byFemaleConfig.TryGetValue(femaleKey, out femaleConfig!))
            return false;

        return true;
    }

    
    private static string BuildMaleEggsParagraph(I18TranslationHelper i18n, BreedingDataMale maleData)
{
    var header = i18n.GetString("config.breeding.maleEggs.header");

    var lines = maleData.GetEggIDs()
        .OrderBy(x => x.Key)
        .Select(x => string.Format(i18n.GetString("config.breeding.maleEggs.entry"), x.Key, x.Value));

    return header + Environment.NewLine + string.Join(Environment.NewLine, lines);
}

    private static string BuildPackPageId(string contentPackId)
        => "breeding.pack." + Slug(contentPackId);

    private static string BuildFemalePageId(string contentPackId, string femaleKey)
        => "breeding.female." + Slug(contentPackId) + "." + Slug(femaleKey);

    private static string Slug(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "empty";

        var chars = value
            .Select(c => char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : '_')
            .ToArray();

        return new string(chars).Trim('_');
    }

    private sealed record EggOption(string Id, string Label);
    private sealed record AnimalOption(string Key, string Label);
}
