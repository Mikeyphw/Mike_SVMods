using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mikew.NestCore.Integrations.Gmcm;
using RoosterControl.Application.Hatching;
using RoosterControl.Constants;
using RoosterControl.Enums;
using RoosterControl.Framework.Config;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.FarmAnimals;
using Mikew.NestCore.Helpers.Logging;

using ILetMeHatchApi = Mikew.NestCore.Framework.ILetMeHatchApi;
using static Mikew.NestCore.Helpers.Logging.ScopedLogCatalog;

namespace RoosterControl.Application;

internal sealed partial class RoosterCoordinator
{
    private GmcmConfigHost<RoosterControlConfig>? _gmcmHost;
    private bool _gmcmRebuildQueued;

    public void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        HatchApi =
            Helper.ModRegistry.GetApi<ILetMeHatchApi>("Mikew.lmh")
            ?? Helper.ModRegistry.GetApi<ILetMeHatchApi>("Mikew.letmehatch.smapi");

        if (!Helper.ModRegistry.IsLoaded("Mikew.nestcore"))
            throw new InvalidOperationException("NestCore is required but was not found.");

        _gmcmHost = GmcmConfigHost<RoosterControlConfig>.TryCreate(
            helper: Helper,
            manifest: ModManifest,
            getConfig: () => Config,
            reset: ResetConfig,
            save: SaveConfig
        );

        if (_gmcmHost is null)
            return;

        _gmcmHost.RebuildLayout(
            define: DefineGmcmLayout,
            fieldIdPrefix: ModManifest.UniqueID,
            titleScreenOnly: false,
            force: true,
            attachLocaleHandler: true
        );
    }

    public void OnLocaleChanged(object? sender, LocaleChangedEventArgs e)
    {
        // GMCM rebuild is handled by NestCore when attachLocaleHandler=true.
        RequestInvalidate(FarmAnimalsAssetName);
        RequestInvalidate(ObjectsAssetName);
    }

    private void ResetConfig()
    {
        Config = RoosterControlConfig.CreateDefault();

        // Treat a GMCM reset as "new config" for catalog seeding.
        _catalogs = _catalogManager.LoadOrCreateAll(Helper, Monitor);
        var changed = _catalogManager.ApplyToConfig(Config, _catalogs, isNewConfig: true, Config.General.CatalogApplyMode, Monitor);
        if (changed)
            ConfigManager.Save(Helper, Config);

        InvalidateDailyPlanCache("config reset");
        _needsRoosterSetup = true;
        RequestInvalidate(FarmAnimalsAssetName);
        RequestInvalidate(ObjectsAssetName);

        RequestGmcmRebuild();
    }

    private void SaveConfig()
    {
        ConfigManager.Save(Helper, Config);

        InvalidateDailyPlanCache("config saved");
        _needsRoosterSetup = true;
        RequestInvalidate(FarmAnimalsAssetName);
        RequestInvalidate(ObjectsAssetName);
    }

    private void DefineGmcmLayout(GmcmMenuLayout<RoosterControlConfig> layout)
    {
        layout.Root(b =>
        {
            b.Section("root.general");
            b.Enum(cfg => cfg.General.UpdateRate, optionId: "general.updateRate", tooltip: true);
            b.Enum(cfg => cfg.General.HatchingMode, optionId: "general.hatchingMode", tooltip: true);
            b.Enum(cfg => cfg.General.HarvestType, optionId: "general.harvestType", tooltip: true);

            b.Separator(simple: true);

            b.Section("root.pages");
            b.Link("compat", tooltip: true);
            b.Link("probabilities", tooltip: true);
            b.Link("females", tooltip: true);
            b.Link("debug", tooltip: true);

            b.Separator(simple: true);

            b.Section("root.roosters");
            b.Link("rooster.white", tooltip: true);
            b.Link("rooster.brown", tooltip: true);
            b.Link("rooster.blue", tooltip: true);
            b.Link("rooster.void", tooltip: true);
            b.Link("rooster.gold", tooltip: true);
        });

        layout.Page("compat", b =>
        {
            b.Section("compat.settings");
            b.Bool(cfg => cfg.General.BetterTrufflesCompat, optionId: "compat.betterTrufflesCompat", tooltip: true);
            b.Bool(cfg => cfg.General.EnableStuckInside, optionId: "compat.enableStuckInside", tooltip: true);
        });

        layout.Page("probabilities", b =>
        {
            b.Section("probabilities.eggs");
            b.Bool(cfg => cfg.General.EnableEggProbability, optionId: "prob.enableEggProbability", tooltip: true);
            b.Int(cfg => cfg.General.EggProbability, optionId: "prob.eggProbability", tooltip: true, min: 0, max: 100, interval: 1);

            b.Bool(cfg => cfg.General.EnableLargeEggProbability, optionId: "prob.enableLargeEggProbability", tooltip: true);
            b.Int(cfg => cfg.General.LargeEggProbability, optionId: "prob.largeEggProbability", tooltip: true, min: 0, max: 100, interval: 1);

            b.Int(cfg => cfg.General.FertilizedEggProbability, optionId: "prob.fertilizedEggProbability", tooltip: true, min: 0, max: 100, interval: 1);

            b.Separator(simple: true);

            b.Section("probabilities.breeding");
            b.Int(cfg => cfg.General.RoosterToChickenRatio, optionId: "prob.roosterToChickenRatio", tooltip: true, min: 0, max: 100, interval: 1);
            b.Float(cfg => cfg.General.BoostByRooster, optionId: "prob.boostByRooster", tooltip: true, min: 0, max: 10, interval: 0.05f);
            b.Int(cfg => cfg.General.ChosenPercentageDailyLuckBonus, optionId: "prob.chosenDailyLuckBonusPct", tooltip: true, min: 0, max: 100, interval: 1);
        });

        layout.Page("females", BuildFemalesPage);

        layout.Page("debug", b =>
        {
            b.ParagraphGmcmOptionsStatus();
            b.Separator(simple: true);

            b.Section("debug.flags");
            b.Bool(cfg => cfg.General.EnableDebug, optionId: "debug.enableDebug", tooltip: true);

            b.Separator(simple: true);

            b.Section("debug.logging");
            b.Bool(cfg => cfg.VerboseLogging, optionId: "debug.verboseLogging", tooltip: true);
            b.Bool(cfg => cfg.TestingLogging, optionId: "debug.testingLogging", tooltip: true);

            b.Paragraph("debug.note");

#if MIKE_DEV
            b.Separator(simple: true);
            b.Section("debug.tuning.luck");
            b.Float(cfg => cfg.DebugKnobs.BonusCalculationMultiplier, optionId: "debug.tuning.bonusCalculation", tooltip: true, min: 0, max: 10, interval: 0.05f);
            b.Float(cfg => cfg.DebugKnobs.SuperUnluckyBonusMultiplier, optionId: "debug.tuning.superUnluckyBonus", tooltip: true, min: -10, max: 0, interval: 0.25f);
            b.Float(cfg => cfg.DebugKnobs.SuperLuckyBonusMultiplier, optionId: "debug.tuning.superLuckyBonus", tooltip: true, min: 0, max: 10, interval: 0.25f);
            b.Float(cfg => cfg.DebugKnobs.LuckAdjustmentOffset, optionId: "debug.tuning.luckAdjustment", tooltip: true, min: -100, max: 100, interval: 1f);
#endif
        });

        AddRoosterPages(layout);
    }


    private void BuildFemalesPage(GmcmPageBuilder<RoosterControlConfig> b)
    {
        if (!Context.IsWorldReady)
        {
            b.Paragraph("females.placeholder");
            return;
        }

        EnsureRoostersSetup();

        Dictionary<string, FarmAnimalData> data;
        try
        {
            data = Helper.GameContent.Load<Dictionary<string, FarmAnimalData>>(FarmAnimalsAssetName);
        }
        catch (Exception ex)
        {
            LogScopedHelper.Log(_log, ModRoosterControlCategory.Gmcm, $"Failed to load {FarmAnimalsAssetName} for GMCM females page: {ex}", LogLevel.Error);
            b.Paragraph("females.error");
            return;
        }

        var plan = FemaleAssignmentService.ComputeFemaleAssignmentsPlan(
            config: Config,
            animalData: data,
            roosters: RoosterHelper.Roosters,
            animalExclusions: _animalExclusions,
            hatchApi: HatchApi
        );

        // Keep the config dictionary aligned with the game-derived list to avoid missing entries.
        if (plan.ConfigChanged || Config.FemalesOptions is null)
        {
            Config.FemalesOptions = new Dictionary<string, FemaleRoosterAssignment>(plan.FemalesOptions, StringComparer.OrdinalIgnoreCase);
            _femaleAssignmentsDirty = true;
        }

        b.Section("females.assignments");

        foreach (var femaleKey in plan.FemalesOptions.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            var elementId = GmcmDynamicIds.ElementId("females.assignment", femaleKey);

            b.EnumLabel<FemaleRoosterAssignment>(
                elementId: elementId,
                name: () => GetFemaleLabel(femaleKey, data),
                getValue: () => GetFemaleAssignment(femaleKey, plan),
                setValue: v => SetFemaleAssignment(femaleKey, v),
                formatAllowedValue: FormatFemaleAssignmentAllowedValue,
                tooltip: () => GetFemaleTooltip(femaleKey, plan)
            );
        }

        b.Paragraph("females.note");
    }

    private FemaleRoosterAssignment GetFemaleAssignment(string femaleKey, FemaleAssignmentsPlanResult plan)
    {
        if (Config.FemalesOptions is not null && Config.FemalesOptions.TryGetValue(femaleKey, out var current))
            return current;

        if (plan.FemalesOptions.TryGetValue(femaleKey, out var planned))
            return planned;

        return FemaleRoosterAssignment.Off;
    }

    private void SetFemaleAssignment(string femaleKey, FemaleRoosterAssignment value)
    {
        Config.FemalesOptions ??= new Dictionary<string, FemaleRoosterAssignment>(StringComparer.OrdinalIgnoreCase);
        Config.FemalesOptions[femaleKey] = value;
    }

    private string GetFemaleLabel(string femaleKey, IDictionary<string, FarmAnimalData> data)
    {
        if (!data.TryGetValue(femaleKey, out var entry) || entry is null)
            return femaleKey;

        var displayName = entry.DisplayName?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(displayName) || string.Equals(displayName, femaleKey, StringComparison.OrdinalIgnoreCase))
            return femaleKey;

        return $"{displayName} ({femaleKey})";
    }

    private string GetFemaleTooltip(string femaleKey, FemaleAssignmentsPlanResult plan)
    {
        var normal = "";
        var large = "";

        if (plan.FemaleEggs.TryGetValue(femaleKey, out var eggs))
        {
            if (eggs.TryGetValue(EggTypeEntries.Normal, out var n) && !string.IsNullOrWhiteSpace(n))
                normal = n;
            if (eggs.TryGetValue(EggTypeEntries.Large, out var l) && !string.IsNullOrWhiteSpace(l))
                large = l;
        }

        return Helper.Translation.Get(
            "config.females.dynamic.tooltip",
            new
            {
                femaleKey,
                normalEgg = string.IsNullOrWhiteSpace(normal) ? "-" : normal,
                largeEgg = string.IsNullOrWhiteSpace(large) ? "-" : large
            }
        ).ToString();
    }

    private string FormatFemaleAssignmentAllowedValue(string raw)
    {
        var key = $"config.females.assignment.allowed.{raw}";
        return Helper.Translation.Get(key).ToString();
    }


    private void AddRoosterPages(GmcmMenuLayout<RoosterControlConfig> layout)
    {
        foreach (var (type, pageId) in GetRoosterPages())
        {
            layout.Page(pageId, b => BuildRoosterPage(b, type, pageId));
        }
    }

    private static IEnumerable<(RoosterType Type, string PageId)> GetRoosterPages()
        => new[]
        {
            (RoosterType.White, "rooster.white"),
            (RoosterType.Brown, "rooster.brown"),
            (RoosterType.Blue, "rooster.blue"),
            (RoosterType.Void, "rooster.void"),
            (RoosterType.Gold, "rooster.gold"),
        };

    private void BuildRoosterPage(GmcmPageBuilder<RoosterControlConfig> b, RoosterType type, string pageId)
    {
        var s = GetOrCreateRoosterSettings(type);

        b.Section("rooster.settings");
        b.Text(
            optionId: $"{pageId}.buildingRequired",
            getValue: () => s.BuildingRequired.ToString(),
            setValue: v => s.BuildingRequired = Enum.Parse<RequiredBuilding>(v, ignoreCase: true),
            tooltip: true,
            allowedValues: Enum.GetNames(typeof(RequiredBuilding)),
            formatAllowedValue: true);

        b.Bool($"{pageId}.buyable", () => s.Buyable, v => s.Buyable = v, tooltip: true);
        b.Int($"{pageId}.price", () => s.Price, v => s.Price = v, tooltip: true, min: 0, max: 50000, interval: 100);

        b.Separator(simple: true);

        b.Section("rooster.hatching");
        b.Bool($"{pageId}.enableRoosterHatching", () => s.EnableRoosterHatching, v => s.EnableRoosterHatching = v, tooltip: true);
        b.Bool($"{pageId}.editAllFemales", () => s.EditAllFemales, v => s.EditAllFemales = v, tooltip: true);
        b.Int($"{pageId}.matureDays", () => s.MatureDays, v => s.MatureDays = v, tooltip: true, min: 0, max: 60, interval: 1);
        b.Int($"{pageId}.produceDays", () => s.ProduceDays, v => s.ProduceDays = v, tooltip: true, min: 1, max: 14, interval: 1);

        b.Separator(simple: true);

        b.Section("rooster.produce");
        b.Text(
            optionId: $"{pageId}.produceOption",
            getValue: () => s.ProduceOption.ToString(),
            setValue: v => s.ProduceOption = Enum.Parse<ProduceMode>(v, ignoreCase: true),
            tooltip: true,
            allowedValues: Enum.GetNames(typeof(ProduceMode)),
            formatAllowedValue: true);

        b.Text(
            optionId: $"{pageId}.deluxeProduceOption",
            getValue: () => s.DeluxeProduceOption.ToString(),
            setValue: v => s.DeluxeProduceOption = Enum.Parse<DeluxeProduceMode>(v, ignoreCase: true),
            tooltip: true,
            allowedValues: Enum.GetNames(typeof(DeluxeProduceMode)),
            formatAllowedValue: true);

        b.Paragraph("rooster.produceEntries.note");

        b.Separator(simple: true);

        b.Section("rooster.produceEntries");
        BuildProduceEntriesEditor(b, pageId, isDeluxe: false, entries: s.ProduceEntries);

        b.Separator(simple: true);

        b.Section("rooster.deluxeProduceEntries");
        BuildProduceEntriesEditor(b, pageId, isDeluxe: true, entries: s.DeluxeProduceEntries);


        if (type == RoosterType.Blue)
        {
            b.Separator(simple: true);

            b.Section("rooster.blue");
            b.Bool($"{pageId}.bypassBlueConditions", () => s.BypassBlueConditions, v => s.BypassBlueConditions = v, tooltip: true);
            b.Text($"{pageId}.blueEggChoice", () => s.BlueEggChoice, v => s.BlueEggChoice = v, tooltip: true);
        }
    }

    private RoosterSettings GetOrCreateRoosterSettings(RoosterType type)
    {
        if (!Config.Roosters.TryGetValue(type, out var s) || s is null)
        {
            s = new RoosterSettings();
            Config.Roosters[type] = s;
        }

        s.ProduceEntries ??= ProduceEntry.CreateDefaultFour();
        s.DeluxeProduceEntries ??= ProduceEntry.CreateDefaultFour();

        EnsureProduceEntriesNormalized(s.ProduceEntries);
        EnsureProduceEntriesNormalized(s.DeluxeProduceEntries);

        return s;
    }


    private static readonly string[] ProduceChoiceAllowedValues = BuildProduceChoiceAllowedValues();

    private static string[] BuildProduceChoiceAllowedValues()
    {
        var values = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

        static void AddConstStrings(Type t, ISet<string> sink)
        {
            foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (f.FieldType != typeof(string) || !f.IsLiteral || f.IsInitOnly)
                    continue;

                if (f.GetRawConstantValue() is string s && !string.IsNullOrWhiteSpace(s))
                    sink.Add(s.Trim());
            }
        }

        AddConstStrings(typeof(ItemIds.CustomItems), values);
        AddConstStrings(typeof(ItemIds.AnimalProducts), values);

        return values.ToArray();
    }

    private static void EnsureProduceEntriesNormalized(IList<ProduceEntry> entries)
    {
        if (entries is null)
            return;

        while (entries.Count < 4)
            entries.Add(new ProduceEntry());

        while (entries.Count > 4)
            entries.RemoveAt(entries.Count - 1);

        for (var i = 0; i < entries.Count; i++)
        {
            entries[i] ??= new ProduceEntry();

            if (string.IsNullOrWhiteSpace(entries[i].Choice))
                entries[i].Choice = ItemIds.CustomItems.Custom;

            if (string.IsNullOrWhiteSpace(entries[i].CustomItemName))
                entries[i].CustomItemName = ItemIds.CustomItems.Custom;

            entries[i].Probability = Math.Clamp(entries[i].Probability, 0, 100);
        }
    }

    private void BuildProduceEntriesEditor(GmcmPageBuilder<RoosterControlConfig> b, string pageId, bool isDeluxe, IList<ProduceEntry> entries)
    {
        EnsureProduceEntriesNormalized(entries);

        var slotSectionPrefix = isDeluxe ? "rooster.deluxeProduceEntries.slot" : "rooster.produceEntries.slot";
        var choiceLabelKey = isDeluxe ? "config.deluxeProduceEntry.choice.name" : "config.produceEntry.choice.name";
        var customLabelKey = isDeluxe ? "config.deluxeProduceEntry.customItemName.name" : "config.produceEntry.customItemName.name";
        var probLabelKey = isDeluxe ? "config.deluxeProduceEntry.probability.name" : "config.produceEntry.probability.name";

        var choiceDescKey = isDeluxe ? "config.deluxeProduceEntry.choice.description" : "config.produceEntry.choice.description";
        var customDescKey = isDeluxe ? "config.deluxeProduceEntry.customItemName.description" : "config.produceEntry.customItemName.description";
        var probDescKey = isDeluxe ? "config.deluxeProduceEntry.probability.description" : "config.produceEntry.probability.description";

        for (var i = 0; i < entries.Count; i++)
        {
            var slot = i + 1;
            var entry = entries[i];

            b.Section($"{slotSectionPrefix}{slot}");

            b.TextLabel(
                elementId: $"{pageId}.{(isDeluxe ? "deluxeProduceEntries" : "produceEntries")}.slot{slot}.choice",
                name: () => $"{Helper.Translation.Get(choiceLabelKey)} #{slot}",
                tooltip: () => Helper.Translation.Get(choiceDescKey).ToString(),
                getValue: () => entry.Choice,
                setValue: v => entry.Choice = v,
                allowedValues: ProduceChoiceAllowedValues,
                formatAllowedValue: false
            );

            b.TextLabel(
                elementId: $"{pageId}.{(isDeluxe ? "deluxeProduceEntries" : "produceEntries")}.slot{slot}.customItemName",
                name: () => $"{Helper.Translation.Get(customLabelKey)} #{slot}",
                tooltip: () => Helper.Translation.Get(customDescKey).ToString(),
                getValue: () => entry.CustomItemName,
                setValue: v => entry.CustomItemName = v
            );

            b.IntLabel(
                elementId: $"{pageId}.{(isDeluxe ? "deluxeProduceEntries" : "produceEntries")}.slot{slot}.probability",
                name: () => $"{Helper.Translation.Get(probLabelKey)} #{slot}",
                tooltip: () => Helper.Translation.Get(probDescKey).ToString(),
                getValue: () => entry.Probability,
                setValue: v => entry.Probability = v,
                min: 0,
                max: 100,
                interval: 1
            );

            b.Separator(simple: true);
        }
    }


    private void RequestGmcmRebuild()
    {
        if (_gmcmHost is null)
            return;

        if (_gmcmRebuildQueued)
            return;

        _gmcmRebuildQueued = true;
        QueueNextTickAction(() =>
        {
            _gmcmRebuildQueued = false;
            _gmcmHost?.RebuildLayout(
                define: DefineGmcmLayout,
                fieldIdPrefix: ModManifest.UniqueID,
                titleScreenOnly: false,
                force: true,
                attachLocaleHandler: true
            );
        });
    }
}
