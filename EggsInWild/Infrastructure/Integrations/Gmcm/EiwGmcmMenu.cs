using System;
using System.Collections.Generic;
using System.Linq;
using Mikew.NestCore.Helpers;
using Mikew.NestCore.Helpers.Logging;
using Mikew.NestCore.Integrations.Gmcm;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.FarmAnimals;
using EggsInWild.Core.ShopProgression;
using EggsInWild.Infrastructure.Config;
using EggsInWild.Infrastructure.Diagnostics;
using EggsInWild.Infrastructure.Integrations.Gmcm.Pages.WildAnimals;
#if WEGG_DEV
using EggsInWild.Infrastructure.Integrations.Gmcm.Pages.Debug;
#endif

namespace EggsInWild.Infrastructure.Integrations.Gmcm;

/// <summary>
/// GMCM menu orchestrator that coordinates the GMCM layout building.
/// </summary>
public sealed class EiwGmcmMenu
{
    private readonly EiwGmcmContext _ctx;

    public EiwGmcmMenu(EiwGmcmContext ctx)
    {
        _ctx = ctx ?? throw new System.ArgumentNullException(nameof(ctx));
    }

    /// <summary>
    /// Rebuild the entire GMCM layout.
    /// </summary>
    public void RebuildLayout(GmcmMenuLayout<ModConfig> layout, I18TranslationHelper i18n, bool force)
    {
        // Root page
        DefineRoot(layout, i18n);

        // Desert Tomb page
        DefineDesertTombPage(layout, i18n);

        // Hatching page
        DefineHatchingPage(layout, i18n);

        // Items page
        DefineItemsPage(layout, i18n);

        // Prices page
        DefinePricesPage(layout, i18n);

        // Animal Notes page
        DefineAnimalNotesPage(layout, i18n);

        // Animal Progression page
        DefineAnimalProgressionPage(layout, i18n);

        // Wild Animals pages
        WildAnimalsPages.Define(_ctx, layout, i18n);

#if WEGG_DEV
        DefineDebugPages(layout, i18n);
#endif
    }

    private void DefineRoot(GmcmMenuLayout<ModConfig> layout, I18TranslationHelper i18n)
    {
        layout.Root(root =>
        {
            root.Bool(cfg => cfg.ProgressionMode, optionId: "general.progressionMode", tooltip: true);
            root.Separator(simple: true);

            root.Link("desertTomb", tooltip: true);
            root.Link("hatching", tooltip: true);
            root.Link("items", tooltip: true);
            root.Link("prices", tooltip: true);
            root.Link("animalNotes", tooltip: true);
            root.Link("animalProgression", tooltip: true);
            root.Link("wildAnimals", tooltip: true);
            root.Action(
                optionId: "wildAnimals.openEditor",
                onAction: () => _ctx.OpenWildAnimalsEditor?.Invoke(),
                tooltip: true);
            root.ParagraphDynamic(
                () => _ctx.IsWildAnimalsEditorAvailable
                    ? ModEntry.Translate(i18n, "config.paragraph.wildAnimals.stardewUiStatus.enabled.text", "StardewUI editor available.")
                    : ModEntry.Translate(i18n, "config.paragraph.wildAnimals.stardewUiStatus.disabled.text", "StardewUI not installed. WildAnimals editor launcher disabled."),
                id: "wildAnimals_stardewUi_status");
#if WEGG_DEV
            root.Link("debug", tooltip: true);
#endif
        });
    }

    private static void DefineDesertTombPage(GmcmMenuLayout<ModConfig> layout, I18TranslationHelper i18n)
    {
        layout.Page("desertTomb", page =>
        {
            page.Bool(cfg => cfg.DesertTombModuleEnabled, optionId: "desertTomb.enabled", tooltip: true);
            page.Bool(cfg => cfg.KeepMineLevel, optionId: "desertTomb.keepMineLevel", tooltip: true);
            page.Enum(cfg => cfg.TombChance, optionId: "desertTomb.tombChance", tooltip: true);
            page.Enum(cfg => cfg.TombDifficulty, optionId: "desertTomb.tombDifficulty", tooltip: true);
        });
    }

    private static void DefineHatchingPage(GmcmMenuLayout<ModConfig> layout, I18TranslationHelper i18n)
    {
        layout.Page("hatching", page =>
        {
            page.Bool(cfg => cfg.HatchDinoEggs, optionId: "hatching.hatchDinoEggs", tooltip: true);
            page.Bool(cfg => cfg.DinoEggsExclusiveHatch, optionId: "hatching.dinoExclusiveHatch", tooltip: true);
            page.Text(cfg => cfg.BirthText, optionId: "general.birthText", tooltip: true);
        });
    }

    private static void DefineItemsPage(GmcmMenuLayout<ModConfig> layout, I18TranslationHelper i18n)
    {
        layout.Page("items", page =>
        {
            page.Bool(cfg => cfg.AlternatePotionSprite, optionId: "items.alternatePotionSprite", tooltip: true);
            page.Bool(cfg => cfg.NeedsFern, optionId: "items.incubatorNeedsFern", tooltip: true);
            page.Bool(cfg => cfg.FruitInsteadOfSeed, optionId: "items.fruitInsteadOfSeed", tooltip: true);
        });
    }

    private static void DefinePricesPage(GmcmMenuLayout<ModConfig> layout, I18TranslationHelper i18n)
    {
        layout.Page("prices", page =>
        {
            page.Text(cfg => cfg.DinoPrice, optionId: "prices.dino", tooltip: true);
            page.Text(cfg => cfg.VoidChickenPrice, optionId: "prices.voidChicken", tooltip: true);
            page.Text(cfg => cfg.OstrichPrice, optionId: "prices.ostrich", tooltip: true);
        });
    }

    private static void DefineAnimalNotesPage(GmcmMenuLayout<ModConfig> layout, I18TranslationHelper i18n)
    {
        layout.Page("animalNotes", page =>
        {
            page.Int(cfg => cfg.AnimalNotesWarnAfterDays, optionId: "animalNotes.warnAfterDays", tooltip: true, min: 1, max: 30, interval: 1);
            page.Int(cfg => cfg.AnimalNotesPlacementRadius, optionId: "animalNotes.placementRadius", tooltip: true, min: 1, max: 30, interval: 1);
            page.Bool(cfg => cfg.AnimalNotesQuestMailHostOnly, optionId: "animalNotes.hostOnlyQuestMail", tooltip: true);
            page.Bool(cfg => cfg.AnimalNotesAllowClientDiscoveries, optionId: "animalNotes.allowClientDiscoveries", tooltip: true);
            page.ParagraphDynamic(
                () => ModEntry.Translate(i18n, "config.paragraph.animalNotes.progressionOverrides.text", "Per-animal progression overrides are managed on the Animal Progression page."),
                id: "animalNotes_progression_hint");
            page.Link("animalProgression", tooltip: true);
        });
    }

    private void DefineAnimalProgressionPage(GmcmMenuLayout<ModConfig> layout, I18TranslationHelper i18n)
    {
        layout.Page("animalProgression", page =>
        {
            page.ParagraphDynamic(
                () => ModEntry.Translate(i18n, "config.paragraph.animalProgression.description.text", "Configure per-animal progression gating."),
                id: "animalProgression_intro");
            BuildAnimalProgressionPage(page, i18n);
        });
    }

    private void BuildAnimalProgressionPage(GmcmPageBuilder<ModConfig> page, I18TranslationHelper i18n)
    {
        if (!Context.IsWorldReady)
        {
            page.ParagraphDynamic(
                () => ModEntry.Translate(i18n, "config.paragraph.animalProgression.loadSave.text", "Load a save to edit per-animal progression settings."),
                id: "animalProgression_loadSave");
            return;
        }

        Dictionary<string, FarmAnimalData> farmAnimals;
        try
        {
            farmAnimals = _ctx.Helper.GameContent.Load<Dictionary<string, FarmAnimalData>>("Data/FarmAnimals");
        }
        catch (Exception ex)
        {
            var smapiLog = new SmapiLog(_ctx.Monitor);
            smapiLog.LogScoped(ScopedLogCatalog.ModWeCategory.Content, $"Failed to load Data/FarmAnimals for GMCM Animal Progression page: {ex}", LogLevel.Error);
            page.ParagraphDynamic(
                () => ModEntry.Translate(i18n, "config.paragraph.animalProgression.loadFailed.text", "Couldn't load Data/FarmAnimals. Try again after loading a save, or check your SMAPI log."),
                id: "animalProgression_loadFailed");
            return;
        }

        if (farmAnimals.Count == 0)
        {
            page.ParagraphDynamic(
                () => ModEntry.Translate(i18n, "config.paragraph.animalProgression.empty.text", "No farm animals were found in Data/FarmAnimals."),
                id: "animalProgression_empty");
            return;
        }

        var config = _ctx.GetConfig();
        config.AnimalProgressionMode ??= new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        var animals = farmAnimals
            .OrderBy(pair => pair.Value?.DisplayName?.ToString() ?? pair.Key, StringComparer.OrdinalIgnoreCase)
            .ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase);

        foreach (var (animalId, farmAnimal) in animals)
        {
            var defaultEnabled = AnimalShopProgressionPolicy.DefaultEnabled(animalId);
            var elementId = $"animalProgression.{animalId}";

            page.BoolLabel(
                elementId: elementId,
                name: () => BuildAnimalOptionName(animalId, farmAnimal),
                tooltip: () => BuildAnimalOptionTooltip(i18n, animalId, defaultEnabled),
                getValue: () => config.AnimalProgressionMode.TryGetValue(animalId, out var configured) ? configured : defaultEnabled,
                setValue: value =>
                {
                    if (value == defaultEnabled)
                        config.AnimalProgressionMode.Remove(animalId);
                    else
                        config.AnimalProgressionMode[animalId] = value;
                });
        }
    }

    private static string BuildAnimalOptionName(string animalId, FarmAnimalData? farmAnimal)
    {
        var displayName = farmAnimal?.DisplayName?.ToString();
        if (string.IsNullOrWhiteSpace(displayName) || string.Equals(displayName, animalId, StringComparison.OrdinalIgnoreCase))
            return animalId;

        return $"{displayName} ({animalId})";
    }

    private static string BuildAnimalOptionTooltip(I18TranslationHelper i18n, string animalId, bool defaultEnabled)
    {
        var defaultState = defaultEnabled
            ? ModEntry.Translate(i18n, "config.animalProgression.state.enabled", "Enabled")
            : ModEntry.Translate(i18n, "config.animalProgression.state.disabled", "Disabled");

        return ModEntry.Translate(
            i18n,
            "config.animalProgression.entry.tooltip",
            $"Gate this animal behind Animal Notes progression. Default: {defaultState}. Key: {animalId}.",
            defaultState,
            animalId);
    }

#if WEGG_DEV
    private void DefineDebugPages(GmcmMenuLayout<ModConfig> layout, I18TranslationHelper i18n)
    {
        DebugPages.Define(layout, i18n, _ctx.DevJsonTuning, _ctx.DevMonsterDefinitions, _ctx.MutateDevMonsterDefinitions, _ctx.NormalizeDevJsonKey);
    }
#endif
}
