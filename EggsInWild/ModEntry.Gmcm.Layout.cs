using Mikew.NestCore.Helpers;
using Mikew.NestCore.Integrations.Gmcm;
using EggsInWild.Infrastructure.Config;

namespace EggsInWild;

public sealed partial class ModEntry
{
    private void DefineGmcmLayout(GmcmMenuLayout<ModConfig> layout, I18TranslationHelper i18n)
    {
        DefineGmcmRoot(layout, i18n);
        DefineGmcmDesertTombPage(layout, i18n);
        DefineGmcmHatchingPage(layout, i18n);
        DefineGmcmItemsPage(layout, i18n);
        DefineGmcmPricesPage(layout, i18n);
        DefineGmcmAnimalNotesPage(layout, i18n);
        DefineGmcmAnimalProgressionPage(layout, i18n);

        DefineGmcmWildAnimalsPages(layout, i18n);

#if WEGG_DEV
        DefineGmcmDebugPages(layout, i18n);
#endif
    }

    private static void DefineGmcmRoot(GmcmMenuLayout<ModConfig> layout, I18TranslationHelper i18n)
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
#if WEGG_DEV
            root.Link("debug", tooltip: true);
#endif
        });
    }

    private static void DefineGmcmDesertTombPage(GmcmMenuLayout<ModConfig> layout, I18TranslationHelper i18n)
    {
        layout.Page("desertTomb", page =>
        {
            page.Bool(cfg => cfg.DesertTombModuleEnabled, optionId: "desertTomb.enabled", tooltip: true);
            page.Bool(cfg => cfg.KeepMineLevel, optionId: "desertTomb.keepMineLevel", tooltip: true);
            page.Enum(cfg => cfg.TombChance, optionId: "desertTomb.tombChance", tooltip: true);
            page.Enum(cfg => cfg.TombDifficulty, optionId: "desertTomb.tombDifficulty", tooltip: true);
        });
    }

    private static void DefineGmcmHatchingPage(GmcmMenuLayout<ModConfig> layout, I18TranslationHelper i18n)
    {
        layout.Page("hatching", page =>
        {
            page.Bool(cfg => cfg.HatchDinoEggs, optionId: "hatching.hatchDinoEggs", tooltip: true);
            page.Bool(cfg => cfg.DinoEggsExclusiveHatch, optionId: "hatching.dinoExclusiveHatch", tooltip: true);
            page.Text(cfg => cfg.BirthText, optionId: "general.birthText", tooltip: true);
        });
    }

    private static void DefineGmcmItemsPage(GmcmMenuLayout<ModConfig> layout, I18TranslationHelper i18n)
    {
        layout.Page("items", page =>
        {
            page.Bool(cfg => cfg.AlternatePotionSprite, optionId: "items.alternatePotionSprite", tooltip: true);
            page.Bool(cfg => cfg.NeedsFern, optionId: "items.incubatorNeedsFern", tooltip: true);
            page.Bool(cfg => cfg.FruitInsteadOfSeed, optionId: "items.fruitInsteadOfSeed", tooltip: true);
        });
    }

    private static void DefineGmcmPricesPage(GmcmMenuLayout<ModConfig> layout, I18TranslationHelper i18n)
    {
        layout.Page("prices", page =>
        {
            page.Text(cfg => cfg.DinoPrice, optionId: "prices.dino", tooltip: true);
            page.Text(cfg => cfg.VoidChickenPrice, optionId: "prices.voidChicken", tooltip: true);
            page.Text(cfg => cfg.OstrichPrice, optionId: "prices.ostrich", tooltip: true);
        });
    }

    private static void DefineGmcmAnimalNotesPage(GmcmMenuLayout<ModConfig> layout, I18TranslationHelper i18n)
    {
        layout.Page("animalNotes", page =>
        {
            page.Int(cfg => cfg.AnimalNotesWarnAfterDays, optionId: "animalNotes.warnAfterDays", tooltip: true, min: 1, max: 30, interval: 1);
            page.Int(cfg => cfg.AnimalNotesPlacementRadius, optionId: "animalNotes.placementRadius", tooltip: true, min: 1, max: 30, interval: 1);
            page.Bool(cfg => cfg.AnimalNotesQuestMailHostOnly, optionId: "animalNotes.hostOnlyQuestMail", tooltip: true);
            page.Bool(cfg => cfg.AnimalNotesAllowClientDiscoveries, optionId: "animalNotes.allowClientDiscoveries", tooltip: true);
            page.ParagraphDynamic(
                () => Translate(i18n, "config.paragraph.animalNotes.progressionOverrides.text", "Per-animal progression overrides are managed on the Animal Progression page."),
                id: "animalNotes_progression_hint");
            page.Link("animalProgression", tooltip: true);
        });
    }

    private void DefineGmcmAnimalProgressionPage(GmcmMenuLayout<ModConfig> layout, I18TranslationHelper i18n)
    {
        layout.Page("animalProgression", page =>
        {
            page.ParagraphDynamic(
                () => Translate(i18n, "config.paragraph.animalProgression.description.text", "Configure per-animal progression gating."),
                id: "animalProgression_intro");
            BuildAnimalProgressionPage(page, i18n);
        });
    }

    private partial void DefineGmcmWildAnimalsPages(GmcmMenuLayout<ModConfig> layout, I18TranslationHelper i18n);

#if WEGG_DEV
    private partial void DefineGmcmDebugPages(GmcmMenuLayout<ModConfig> layout, I18TranslationHelper i18n);
#endif
}
