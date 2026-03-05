using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mikew.NestCore.Integrations.Gmcm;
using RoosterControl.Constants;
using RoosterControl.Enums;
using RoosterControl.Framework.Config;
using StardewValley.ItemTypeDefinitions;
using RoosterControl.Application;

namespace RoosterControl.Integrations.Gmcm.Pages;

internal static class RoosterTypePages
{
    public static readonly string[] PageIds =
    {
        "rooster.white",
        "rooster.brown",
        "rooster.blue",
        "rooster.void",
        "rooster.gold",
    };

    public static void Define(GmcmMenuLayout<RoosterControlConfig> layout, RoosterCoordinator owner)
    {
        layout.Pages(new (string PageId, Action<GmcmPageBuilder<RoosterControlConfig>> Build)[]
        {
            ("rooster.white", b => BuildRoosterPage(b, owner, RoosterType.White, "rooster.white")),
            ("rooster.brown", b => BuildRoosterPage(b, owner, RoosterType.Brown, "rooster.brown")),
            ("rooster.blue", b => BuildRoosterPage(b, owner, RoosterType.Blue, "rooster.blue")),
            ("rooster.void", b => BuildRoosterPage(b, owner, RoosterType.Void, "rooster.void")),
            ("rooster.gold", b => BuildRoosterPage(b, owner, RoosterType.Gold, "rooster.gold")),
        });
    }

    private static void BuildRoosterPage(
        GmcmPageBuilder<RoosterControlConfig> b,
        RoosterCoordinator owner,
        RoosterType type,
        string pageId)
    {
        var s = GetOrCreateRoosterSettings(owner, type);

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
        BuildProduceEntriesEditor(b, owner, pageId, isDeluxe: false, entries: s.ProduceEntries);

        b.Separator(simple: true);

        b.Section("rooster.deluxeProduceEntries");
        BuildProduceEntriesEditor(b, owner, pageId, isDeluxe: true, entries: s.DeluxeProduceEntries);


        if (type == RoosterType.Blue)
        {
            b.Separator(simple: true);

            b.Section("rooster.blue");
            b.Bool($"{pageId}.bypassBlueConditions", () => s.BypassBlueConditions, v => s.BypassBlueConditions = v, tooltip: true);
            b.Text($"{pageId}.blueEggChoice", () => s.BlueEggChoice, v => s.BlueEggChoice = v, tooltip: true);
        }
    }

    private static RoosterSettings GetOrCreateRoosterSettings(RoosterCoordinator owner, RoosterType type)
    {
        var config = owner.Config;
        if (!config.Roosters.TryGetValue(type, out var s) || s is null)
        {
            s = new RoosterSettings();
            config.Roosters[type] = s;
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

            entries[i].Choice = (entries[i].Choice ?? string.Empty).Trim();
            entries[i].CustomItemName = (entries[i].CustomItemName ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(entries[i].Choice))
                entries[i].Choice = ItemIds.CustomItems.Custom;

            if (string.IsNullOrWhiteSpace(entries[i].CustomItemName))
                entries[i].CustomItemName = ItemIds.CustomItems.Custom;

            entries[i].Probability = Math.Clamp(entries[i].Probability, 0, 100);
        }
    }

    private static void BuildProduceEntriesEditor(
        GmcmPageBuilder<RoosterControlConfig> b,
        RoosterCoordinator owner,
        string pageId,
        bool isDeluxe,
        IList<ProduceEntry> entries)
    {
        EnsureProduceEntriesNormalized(entries);

        var slotSectionPrefix = isDeluxe ? "rooster.deluxeProduceEntries.slot" : "rooster.produceEntries.slot";
        var choiceLabelKey = isDeluxe ? "config.deluxeProduceEntry.choice.name" : "config.produceEntry.choice.name";
        var customLabelKey = isDeluxe ? "config.deluxeProduceEntry.customItemName.name" : "config.produceEntry.customItemName.name";
        var probLabelKey = isDeluxe ? "config.deluxeProduceEntry.probability.name" : "config.produceEntry.probability.name";

        var choiceDescKey = isDeluxe ? "config.deluxeProduceEntry.choice.description" : "config.produceEntry.choice.description";
        var customDescKey = isDeluxe ? "config.deluxeProduceEntry.customItemName.description" : "config.produceEntry.customItemName.description";
        var probDescKey = isDeluxe ? "config.deluxeProduceEntry.probability.description" : "config.produceEntry.probability.description";

        var i18n = owner.I18n;

        for (var i = 0; i < entries.Count; i++)
        {
            var slot = i + 1;
            var entry = entries[i];

            b.Section($"{slotSectionPrefix}{slot}");

            b.TextLabel(
                elementId: $"{pageId}.{(isDeluxe ? "deluxeProduceEntries" : "produceEntries")}.slot{slot}.choice",
                name: () => i18n.GetString("config.gmcm.slotNumber.format", new { label = i18n.GetString(choiceLabelKey), slot }), tooltip: () => i18n.GetString(choiceDescKey),
                getValue: () => entry.Choice,
                setValue: v => entry.Choice = v,
                allowedValues: ProduceChoiceAllowedValues,
                formatAllowedValue: false
            );

            b.TextLabel(
                elementId: $"{pageId}.{(isDeluxe ? "deluxeProduceEntries" : "produceEntries")}.slot{slot}.customItemName",
                name: () => i18n.GetString("config.gmcm.slotNumber.format", new { label = i18n.GetString(customLabelKey), slot }), tooltip: () => i18n.GetString(customDescKey),
                getValue: () => entry.CustomItemName,
                setValue: v => entry.CustomItemName = v
            );

            b.IntLabel(
                elementId: $"{pageId}.{(isDeluxe ? "deluxeProduceEntries" : "produceEntries")}.slot{slot}.probability",
                name: () => i18n.GetString("config.gmcm.slotNumber.format", new { label = i18n.GetString(probLabelKey), slot }), tooltip: () => i18n.GetString(probDescKey),
                getValue: () => entry.Probability,
                setValue: v => entry.Probability = v,
                min: 0,
                max: 100,
                interval: 1
            );

            b.Separator(simple: true);
        }
    }
}

