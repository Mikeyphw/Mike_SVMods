using System;
using System.Collections.Generic;
using System.Linq;
using LetMeHatch.Config;
using LetMeHatch.GameAssetManagement;
using LetMeHatch.Models;
using Mikew.NestCore.Framework;
using Mikew.NestCore.Helpers.Config;
using Mikew.NestCore.Integrations.Gmcm;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.FarmAnimals;
using StardewValley.GameData.Objects;
using static LetMeHatch.EnumsAndStrings.Enums;

namespace LetMeHatch.Integrations.Gmcm;

public static class LmhGmcmMenu
{
    private const float MinChance = 0f;
    private const float MaxChance = 1f;
    private const float ChanceInterval = 0.1f;

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

        void Rebuild(bool force)
        {
            host.RebuildLayout(layout =>
            {
                BuildRoot(layout, core);
                BuildEditsPages(layout, core, configProvider);
                BuildCloningPage(layout, core, configProvider);
                BuildColorsPages(layout, core, configProvider);
                BuildBreedingPages(layout, core, configProvider);
                BuildAdvancedPages(layout, core, configProvider);
                BuildDebugPage(layout, core, configProvider);
            }, force: force);
        }

        Rebuild(force: true);

        core.Helper.Events.GameLoop.SaveLoaded += (_, _) =>
        {
            var tries = 0;
            EventHandler<UpdateTickedEventArgs>? onTick = null;
            onTick = (_, __) =>
            {
                tries++;
                if (ModEntry.CurrentAssetManager is null && tries < 120)
                    return;

                core.Helper.Events.GameLoop.UpdateTicked -= onTick;
                Rebuild(force: true);
            };

            core.Helper.Events.GameLoop.UpdateTicked += onTick;
        };
    }

    private static void BuildRoot(GmcmMenuLayout<ModConfig> layout, CoreContext core)
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
    }

    private static void BuildEditsPages(
        GmcmMenuLayout<ModConfig> layout,
        CoreContext core,
        SmapiConfigProvider<ModConfig> configProvider)
    {
        layout.Page("edits", page =>
        {
            page.Section(SafeT(core, "config.page.edits.name", "Edits"), tooltip: false);
            page.Paragraph(SafeT(core, "config.edits.paragraph", "Per-animal edit toggles."));
            page.Separator(simple: true);
            page.Link("editsEggList");
            page.Link("editsBirthText");
            page.Link("editsCloneEggs");
        });

        layout.Page("editsEggList", page => BuildPerAnimalOverridesPage(page, core, configProvider, EditType.EggList));
        layout.Page("editsBirthText", page => BuildPerAnimalOverridesPage(page, core, configProvider, EditType.BirthText));
        layout.Page("editsCloneEggs", page => BuildPerAnimalOverridesPage(page, core, configProvider, EditType.CloneEggs));
    }

    private static void BuildCloningPage(
        GmcmMenuLayout<ModConfig> layout,
        CoreContext core,
        SmapiConfigProvider<ModConfig> configProvider)
    {
        layout.Page(
            "cloning",
            title: () => SafeT(core, "config.page.cloning.name", "Cloning"),
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
    }

    private static void BuildColorsPages(
        GmcmMenuLayout<ModConfig> layout,
        CoreContext core,
        SmapiConfigProvider<ModConfig> configProvider)
    {
        layout.Page(
            "colors",
            title: () => SafeT(core, "config.page.colors.name", "Colors"),
            build: page =>
            {
                page.Bool(cfg => cfg.ChangeEggColors, optionId: "ChangeEggColors", tooltip: true);
                page.Bool("LinkAnimalColorToEggs",
                    getValue: () => configProvider.Get().LinkAnimalColorToEgg,
                    setValue: v => configProvider.Get().LinkAnimalColorToEgg = v,
                    tooltip: true);

                page.Separator(simple: true);
                page.ParagraphDynamic(() => SafeT(core, "config.colors.paragraph", "Egg color behavior."), id: "colors_intro");
                page.Separator(simple: true);

                page.Link("eggPalette");
                page.Link("eggColorOverrides");
            }
        );

        layout.Page("eggPalette", page => BuildEggPalettePage(page, core, configProvider));
        layout.Page("eggColorOverrides", page => BuildEggColorOverridesPage(page, core, configProvider));
    }

    private static void BuildBreedingPages(
        GmcmMenuLayout<ModConfig> layout,
        CoreContext core,
        SmapiConfigProvider<ModConfig> configProvider)
    {
        layout.Page(
            "breeding",
            title: () => SafeT(core, "config.page.breeding.name", "Breeding"),
            build: page =>
            {
                page.Bool(cfg => cfg.BreedingSystemEnabled, optionId: "BreedingSystemEnabled", tooltip: true);
                page.Bool(cfg => cfg.DeterministicBreedingRng, optionId: "DeterministicBreedingRng", tooltip: true);
                page.Float(cfg => cfg.DailyLuckBonus, optionId: "DailyLuckBonus", tooltip: true, min: MinChance, max: MaxChance, interval: 0.01f);

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
                page.Link("breedingPacks");
            }
        );

        // Strict parity restore: old GMCM's per-content-pack -> per-female -> per-male tuning flow.
        layout.Page("breedingPacks", page => BuildBreedingPackIndexPage(page, core));

        foreach (var pack in GetBreedingPacksSnapshot())
        {
            var packPageId = BuildPackPageId(pack.ContentPackId);
            var packTitle = pack.DisplayName;

            layout.Page(
                packPageId,
                title: () => packTitle,
                build: page =>
                {
                    page.SectionLabel(() => SafeT(core, "config.section.Females.text", "Females"));

                    foreach (var female in pack.FemaleKeys)
                        page.Link(BuildFemalePageId(pack.ContentPackId, female));
                }
            );

            foreach (var female in pack.FemaleKeys)
            {
                var femalePageId = BuildFemalePageId(pack.ContentPackId, female);
                var femaleTitle = female;
                var capturedPack = pack.ContentPackId;
                var capturedFemale = female;

                layout.Page(
                    femalePageId,
                    title: () => femaleTitle,
                    build: page => BuildFemaleBreedingPage(page, core, capturedPack, capturedFemale)
                );
            }
        }
    }

    private static void BuildBreedingPackIndexPage(GmcmPageBuilder<ModConfig> page, CoreContext core)
    {
        page.Paragraph(SafeT(core, "config.section.LoadedBreedingSystems.text", "Loaded breeding systems"));

        var packs = GetBreedingPacksSnapshot();
        if (packs.Count == 0)
        {
            page.Separator(simple: true);
            page.Paragraph(SafeT(core, "config.notavaliable", "Not available yet. Load a save first."));
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
            page.Paragraph(SafeT(core, "config.notavaliable", "Not available yet. Load a save first."));
            return;
        }

        if (!TryGetFemaleBreedingContext(manager, contentPackId, femaleKey, out var femaleData, out var femaleConfig))
        {
            page.Paragraph($"Breeding data missing for '{femaleKey}' in '{contentPackId}'.");
            return;
        }

        page.BoolLabel(
            elementId: "ApplyDailyBoost",
            name: () => SafeT(core, "config.ApplyDailyBoost.name", "Apply daily luck boost"),
            tooltip: () => SafeT(core, "config.ApplyDailyBoost.description", "Enable daily luck adjustments for this female."),
            getValue: () => femaleData.ApplyDailyLuckBoost,
            setValue: v =>
            {
                femaleData.ApplyDailyLuckBoost = v;
                femaleConfig.ApplyDailyLuckBoost = v;
            }
        );

        if (femaleData.AdjustableEggChances)
        {
            page.Separator(simple: true);
            page.SectionLabel(() => SafeT(core, "config.section.NormalHatching.text", "Normal hatching"));

            page.FloatLabel(
                elementId: "NormalEggChance",
                name: () => SafeT(core, "config.NormalEggChance.name", "Normal egg chance"),
                tooltip: () => SafeT(core, "config.NormalEggChance.description", "Chance for normal eggs."),
                getValue: () => GetBreedingEggChance(femaleConfig, ItemType.Normal),
                setValue: v => SetBreedingEggChance(femaleConfig, ItemType.Normal, v),
                min: MinChance,
                max: MaxChance,
                interval: ChanceInterval
            );

            page.FloatLabel(
                elementId: "LargeEggChance",
                name: () => SafeT(core, "config.LargeEggChance.name", "Large egg chance"),
                tooltip: () => SafeT(core, "config.LargeEggChance.description", "Chance for large eggs."),
                getValue: () => GetBreedingEggChance(femaleConfig, ItemType.Large),
                setValue: v => SetBreedingEggChance(femaleConfig, ItemType.Large, v),
                min: MinChance,
                max: MaxChance,
                interval: ChanceInterval
            );

            page.SectionLabel(() => SafeT(core, "config.section.FertilizedHatching.text", "Fertilized hatching"));
            page.FloatLabel(
                elementId: "FertilizedEggChance",
                name: () => SafeT(core, "config.FertilizedEggChance.name", "Fertilized egg chance"),
                tooltip: () => SafeT(core, "config.FertilizedEggChance.description", "Chance for fertilized eggs."),
                getValue: () => GetBreedingEggChance(femaleConfig, ItemType.FertilizedEgg),
                setValue: v => SetBreedingEggChance(femaleConfig, ItemType.FertilizedEgg, v),
                min: MinChance,
                max: MaxChance,
                interval: ChanceInterval
            );
        }

        page.Separator(simple: true);
        page.SectionLabel(() => SafeT(core, "config.section.Males.text", "Males"));

        foreach (var (maleKey, maleData) in femaleData.Males.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
        {
            var maleLabel = maleKey.EndsWith("_Male", StringComparison.OrdinalIgnoreCase)
                ? "Cloned Male"
                : maleKey;

            page.Separator(simple: true);
            page.SectionLabel(() => maleLabel);

            page.BoolLabel(
                elementId: $"EnableMale_{maleKey}",
                name: () => SafeT(core, "config.EnableMale.name", "Enable male"),
                tooltip: () => SafeT(core, "config.EnableMale.description", "Enable this male option for breeding."),
                getValue: () => GetMaleEnabled(femaleConfig, maleKey),
                setValue: v => SetMaleEnabled(femaleConfig, maleKey, v)
            );

            if (manager.CloningHelper.IsAClonedAnimal(maleKey))
            {
                page.BoolLabel(
                    elementId: $"AddAsAlternatePurchase_{maleKey}",
                    name: () => SafeT(core, "config.AddAsAlternatePurchase.name", "Add as alternate purchase"),
                    tooltip: () => SafeT(core, "config.AddAsAlternatePurchase.description", "Show this clone as alternate purchase in shops."),
                    getValue: () => femaleConfig.MaleConfig.AddAsAlternatePurchase,
                    setValue: v => femaleConfig.MaleConfig.AddAsAlternatePurchase = v
                );
            }

            page.ParagraphDynamic(() => BuildMaleEggsParagraph(maleData), id: $"maleEggs_{contentPackId}_{femaleKey}_{maleKey}");

            if (maleData.AdjustableMaleChances)
            {
                page.FloatLabel(
                    elementId: $"BoostByMale_{maleKey}",
                    name: () => SafeT(core, "config.BoostByMale.name", "Boost by male"),
                    tooltip: () => SafeT(core, "config.BoostByMale.description", "Extra chance boost when this male is present."),
                    getValue: () => GetBoostByMale(femaleConfig, maleKey),
                    setValue: v => SetBoostByMale(femaleConfig, maleKey, v),
                    min: MinChance,
                    max: MaxChance,
                    interval: ChanceInterval
                );

                page.FloatLabel(
                    elementId: $"MaleChance_{maleKey}",
                    name: () => SafeT(core, "config.MaleChance.name", "Male chance"),
                    tooltip: () => SafeT(core, "config.MaleChance.description", "Chance to select this male."),
                    getValue: () => GetMaleChance(femaleConfig, maleKey),
                    setValue: v => SetMaleChance(femaleConfig, maleKey, v),
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
            title: () => SafeT(core, "config.page.advanced.name", "Advanced"),
            build: page =>
            {
                page.Bool(cfg => cfg.StabilizeFarmAnimalsAssetOrder, optionId: "StabilizeFarmAnimalsAssetOrder", tooltip: true);

                page.Separator(simple: true);
                page.Link("itemMappings");
            }
        );

        layout.Page(
            "itemMappings",
            title: () => SafeT(core, "config.page.itemMappings.name", "Item mappings"),
            build: page => BuildItemMappingsIndexPage(page, core)
        );

        layout.Page(
            "itemMappingsEgg",
            title: () => SafeT(core, "config.page.itemMappingsEgg.name", "Egg mappings"),
            build: page => BuildItemMappingsCloneTypePage(page, core, configProvider, CloneItemType.Egg)
        );

        layout.Page(
            "itemMappingsMilk",
            title: () => SafeT(core, "config.page.itemMappingsMilk.name", "Milk mappings"),
            build: page => BuildItemMappingsCloneTypePage(page, core, configProvider, CloneItemType.Milk)
        );

        layout.Page(
            "itemMappingsMisc",
            title: () => SafeT(core, "config.page.itemMappingsMisc.name", "Misc mappings"),
            build: page => BuildItemMappingsCloneTypePage(page, core, configProvider, CloneItemType.Misc)
        );
    }

    private static void BuildItemMappingsIndexPage(GmcmPageBuilder<ModConfig> page, CoreContext core)
    {
        page.Paragraph(SafeT(core, "config.itemMappings.paragraph", "Choose replacement outputs per animal and clone type."));
        page.Separator(simple: true);
        page.Link("itemMappingsEgg");
        page.Link("itemMappingsMilk");
        page.Link("itemMappingsMisc");
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
            page.Paragraph(SafeT(core, "config.itemMappings.noSave", "Load a save to edit mappings."));
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
            page.Paragraph(SafeT(core, "config.itemMappings.noAnimals", "No animals available for this mapping type."));
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
                        ? SafeT(core, "config.itemMappings.itemType.normal.name", "Normal")
                        : SafeT(core, "config.itemMappings.itemType.large.name", "Large"),
                    tooltip: () => SafeT(core, "config.itemMappings.custom.tooltip", "Set custom mapped item ID."),
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
            title: () => SafeT(core, "config.page.debug.name", "Debug"),
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
    }

    private static void BuildPerAnimalOverridesPage(
        GmcmPageBuilder<ModConfig> page,
        CoreContext core,
        SmapiConfigProvider<ModConfig> configProvider,
        EditType editType)
    {
        var cfg = configProvider.Get();
        var animals = GetAnimalOptions(core, cfg);

        page.Paragraph(SafeT(core, "config.edits.perAnimal.paragraph", "Configure per-animal overrides."));

        if (animals.Count <= 0)
        {
            page.Separator(simple: true);
            page.Paragraph(SafeT(core, "config.edits.noAnimals", "No animals available."));
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
                tooltip: () => SafeT(core, "config.edits.animal.tooltip", $"Toggle override for {animalKey}.").Replace("{0}", animalKey),
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
            text: () => SafeT(core, "config.eggPalette.paragraph", "Choose which color groups each egg can use."),
            id: "eggPalette_intro"
        );

        var cfg = configProvider.Get();
        var eggs = GetEggOptions(core, cfg);
        var colorGroups = GetPaletteColorGroups();

        if (eggs.Count <= 0)
        {
            page.Separator(simple: true);
            page.Paragraph(SafeT(core, "config.eggPalette.noEggs", "No eggs found."));
            return;
        }

        foreach (var egg in eggs)
        {
            page.Separator(simple: true);
            page.SectionLabel(() => egg.Label);

            if (egg.Id == "305")
                page.ParagraphDynamic(() => SafeT(core, "config.eggPalette.voidEggNote", "Void egg uses custom handling."), id: $"voidEgg_{egg.Id}");

            foreach (var colorKey in colorGroups)
            {
                var elementId = GmcmDynamicIds.ElementId(elementPrefix: $"egg_{egg.Id}", key: colorKey);

                page.BoolLabel(
                    elementId: elementId,
                    name: () => SafeT(core, $"config.color.{colorKey}.name", colorKey),
                    tooltip: () => SafeT(core, "config.eggPalette.allowColor.description", "Allow this color for this egg."),
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
            text: () => SafeT(core, "config.eggOverride.paragraph", "Force a fixed color per egg item."),
            id: "eggOverride_intro"
        );

        var cfg = configProvider.Get();
        var eggs = GetEggOptions(core, cfg);

        if (eggs.Count <= 0)
        {
            page.Separator(simple: true);
            page.Paragraph(SafeT(core, "config.eggPalette.noEggs", "No eggs found."));
            return;
        }

        foreach (var egg in eggs)
        {
            page.Separator(simple: true);
            page.SectionLabel(() => egg.Label);

            page.BoolLabel(
                elementId: GmcmDynamicIds.ElementId("eggOverrideEnabled", egg.Id),
                name: () => SafeT(core, "config.eggOverride.enabled.name", "Enable override"),
                tooltip: () => SafeT(core, "config.eggOverride.enabled.description", "Enable fixed color for this egg."),
                getValue: () => IsEggColorOverrideEnabled(configProvider.Get(), egg.Id),
                setValue: v => SetEggColorOverrideEnabled(configProvider.Get(), egg.Id, v)
            );

            page.ColorSwatchLabel(
                elementId: GmcmDynamicIds.ElementId("eggOverrideColor", egg.Id),
                name: () => SafeT(core, "config.eggOverride.color.name", "Override color"),
                tooltip: () => SafeT(core, "config.eggOverride.color.description", "Pick fixed color for this egg."),
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
    {
        if (enabled)
            overrides.Remove(key);
        else
            overrides[key] = false;
    }

    private static float GetEggChance(ModConfig cfg, ItemType type)
        => cfg.EggChances.TryGetValue(type, out var v) ? v : 0f;

    private static void SetEggChance(ModConfig cfg, ItemType type, float value)
        => cfg.EggChances[type] = value;

    private static float GetBreedingEggChance(BreedingConfig cfg, ItemType type)
        => cfg.EggChances.TryGetValue(type, out var v) ? v : 0f;

    private static void SetBreedingEggChance(BreedingConfig cfg, ItemType type, float value)
        => cfg.EggChances[type] = value;

    private static bool GetMaleEnabled(BreedingConfig cfg, string maleKey)
    {
        if (!cfg.Enabled.TryGetValue(maleKey, out var enabled))
        {
            cfg.Enabled[maleKey] = true;
            return true;
        }

        return enabled;
    }

    private static void SetMaleEnabled(BreedingConfig cfg, string maleKey, bool value)
        => cfg.Enabled[maleKey] = value;

    private static float GetBoostByMale(BreedingConfig cfg, string maleKey)
    {
        if (!cfg.BoostByMale.TryGetValue(maleKey, out var value))
        {
            cfg.BoostByMale[maleKey] = 0.5f;
            return 0.5f;
        }

        return value;
    }

    private static void SetBoostByMale(BreedingConfig cfg, string maleKey, float value)
        => cfg.BoostByMale[maleKey] = value;

    private static float GetMaleChance(BreedingConfig cfg, string maleKey)
    {
        if (!cfg.MaleChance.TryGetValue(maleKey, out var value))
        {
            cfg.MaleChance[maleKey] = 0.5f;
            return 0.5f;
        }

        return value;
    }

    private static void SetMaleChance(BreedingConfig cfg, string maleKey, float value)
        => cfg.MaleChance[maleKey] = value;

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

    private static string BuildMaleEggsParagraph(BreedingDataMale maleData)
    {
        var lines = maleData.GetEggIDs()
            .OrderBy(x => x.Key)
            .Select(x => $"Type: {x.Key}, ID: {x.Value}");

        return "Eggs:\n" + string.Join(Environment.NewLine, lines);
    }

    private static List<BreedingPackView> GetBreedingPacksSnapshot()
    {
        var manager = ModEntry.CurrentAssetManager;
        if (manager is null)
            return new List<BreedingPackView>();

        return manager.LoadedFemaleBreedingData
            .Select(kvp => new BreedingPackView(
                ContentPackId: kvp.Key,
                DisplayName: ResolveContentPackDisplayName(manager, kvp.Key),
                FemaleKeys: kvp.Value.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList()
            ))
            .OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.ContentPackId, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string ResolveContentPackDisplayName(GameAssetManager manager, string contentPackId)
    {
        var cp = manager.LoadedContentPacks.FirstOrDefault(x =>
            string.Equals(x.Manifest.UniqueID, contentPackId, StringComparison.OrdinalIgnoreCase));

        return cp?.Manifest.Name ?? contentPackId;
    }

    private static string BuildPackPageId(string contentPackId)
        => "breeding_pack_" + Slug(contentPackId);

    private static string BuildFemalePageId(string contentPackId, string femaleKey)
        => "breeding_female_" + Slug(contentPackId) + "_" + Slug(femaleKey);

    private static string Slug(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "empty";

        var chars = value
            .Select(c => char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : '_')
            .ToArray();

        return new string(chars).Trim('_');
    }

    private static string SafeT(CoreContext core, string key, string fallback)
    {
        try
        {
            var text = core.Translation.GetString(key);
            return string.IsNullOrWhiteSpace(text) ? fallback : text;
        }
        catch
        {
            return fallback;
        }
    }

    private sealed record EggOption(string Id, string Label);
    private sealed record AnimalOption(string Key, string Label);
    private sealed record BreedingPackView(string ContentPackId, string DisplayName, IReadOnlyList<string> FemaleKeys);
}
