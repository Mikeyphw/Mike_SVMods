using System.Collections.Generic;
using System;
using System.Linq;
using StardewValley;
using StardewValley.GameData.FarmAnimals;
using EggsInWild.Core.Enums;
using EggsInWild.Core.Ids;
using Mikew.NestCore.Helpers.Content;
using Mikew.NestCore;

namespace EggsInWild.Infrastructure.Content.Editors;

public sealed class QuestEditor : AssetEditorBase<ContentContext>
{
    public override string AssetName => Strings.AssetNames.Quests;

    public override void Edit(IAssetData asset, ContentContext context)
    {
        // The original mod uses the prior Data/Quests string schema.
        // Keeping this format maintains validation and avoids binding to SDV internal quest-data models.
        var data = asset.AsDictionary<string, string>().Data;

        foreach (var (id, value) in BuildQuests(context))
            data[id] = value;
    }

    private static IReadOnlyDictionary<string, string> BuildQuests(ContentContext context)
    {
        var quests = new Dictionary<string, string>();

        if (context.Step >= ProgressionStep.PartII)
        {
            quests[ProgressionIds.Quests.WizardStudy] = CreateQuestString(
                type: "ItemDelivery",
                title: TranslateOrFallback(context, "quest.WizardStudy.title", "Wizard Study"),
                details: TranslateOrFallback(context, "quest.WizardStudy.questDetails", "Bring the requested item to the Wizard."),
                hint: TranslateOrFallback(context, "quest.WizardStudy.hint", "The Wizard wants to study a strange egg."),
                solution: $"Wizard {ModItemIds.FarmLargeWildEgg}",
                gold: 0,
                cancellable: false,
                reaction: TranslateOrFallback(context, "quest.WizardStudy.reactionText", "Interesting... very interesting.")
            );
        }

        if (context.Step >= ProgressionStep.PartIV)
        {
            quests[ProgressionIds.Quests.TheDesertScroll] = CreateQuestString(
                type: "ItemDelivery",
                title: TranslateOrFallback(context, "quest.DesertScroll.title", "The Desert Scroll"),
                details: TranslateOrFallback(context, "quest.DesertScroll.questDetails", "Deliver the scroll to the Wizard."),
                hint: TranslateOrFallback(context, "quest.DesertScroll.hint", "A scroll from the desert might help."),
                solution: $"Wizard {ModItemIds.OsirisScroll}",
                gold: 500,
                cancellable: false,
                reaction: TranslateOrFallback(context, "quest.DesertScroll.reactionText", "This will do nicely.")
            );
        }

        if (context.Step >= ProgressionStep.PartVI)
        {
            foreach (var animalKey in GetAnimalKeys(context))
            {
                var token = NormalizeToken(animalKey);
                var questId = EggsInWild.Application.AnimalNotes.AnimalNoteIds.NoteQuestId(animalKey);
                var noteId = $"{EggsInWild.Constants.ModId}_junimoNote_{token}";

                var baseTitle = TranslateOrFallback(context, "quest.NoteDelivery.title", "Note Delivery");
                var baseDetails = TranslateOrFallback(context, "quest.NoteDelivery.questDetails", "Deliver the note to Marnie.");
                var baseHint = TranslateOrFallback(context, "quest.NoteDelivery.hint", "Deliver the note.");

                var reactionKey = $"quest.NoteDelivery.reactionText.{StardewValley.Game1.random.Next(4)}";
                var reaction = TranslateOrFallback(context, reactionKey, "Thanks for bringing this to me.");

                var goldReward = GetNoteQuestGoldReward(context, animalKey);
                var shopDescription = TryGetAnimalShopDescription(animalKey);
                if (!string.IsNullOrWhiteSpace(shopDescription))
                    reaction = $"{reaction}^^{shopDescription}";

                quests[questId] = CreateQuestString(
                    type: "ItemDelivery",
                    title: T(context, "quest.NoteDelivery.titleWithAnimal", "{0}: {1}", baseTitle, ResolveFarmAnimalDisplayName(animalKey)),
                    details: T(context, "quest.NoteDelivery.detailsWithAnimal", "{0}: {1}.", baseDetails, ResolveFarmAnimalDisplayName(animalKey)),
                    hint: T(context, "quest.NoteDelivery.hintWithAnimal", "{0}: {1}.", baseHint, ResolveFarmAnimalDisplayName(animalKey)),
                    solution: $"Marnie {noteId}",
                    gold: 0,
                    cancellable: false,
                    reaction: reaction
                );
}
        }

        return quests;
    }
    private static IReadOnlyList<string> GetAnimalKeys(ContentContext context)
    {
        return context.ModData.WildAnimals.Keys
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }


    private static string NormalizeToken(string s)
        => (s ?? string.Empty).Replace(" ", string.Empty, StringComparison.Ordinal);

private static string CreateQuestString(
        string type,
        string title,
        string details,
        string hint,
        string solution,
        int gold,
        bool cancellable,
        string reaction)
    {
        // Format: Type/Title/Details/Hint/Solution/NextQuest/Gold/RewardDescription/Cancellable/ReactionText
        return string.Join(
            '/',
            type,
            title,
            details,
            hint,
            solution,
            "-1",
            gold.ToString(),
            string.Empty,
            cancellable.ToString(),
            reaction
        );
    }

    private static int GetNoteQuestGoldReward(ContentContext context, string animalKey)
    {
        if (Game1.farmAnimalData is not null && Game1.farmAnimalData.TryGetValue(animalKey, out var data))
        {
            if (data.PurchasePrice > 0)
                return data.PurchasePrice;
        }

        if (context.ModData.WildAnimals.TryGetValue(animalKey, out var wild) && wild.Price > 0)
            return wild.Price;

        return 0;
    }

    private static string? TryGetAnimalShopDescription(string animalKey)
    {
        if (Game1.farmAnimalData is null || !Game1.farmAnimalData.TryGetValue(animalKey, out var data))
            return null;

        return data.ShopDescription;
    }

    private static string ResolveFarmAnimalDisplayName(string animalId)
    {
        if (Game1.farmAnimalData is not null && Game1.farmAnimalData.TryGetValue(animalId, out var data))
        {
            if (!string.IsNullOrWhiteSpace(data.DisplayName))
                return data.DisplayName;

            if (!string.IsNullOrWhiteSpace(data.ShopDisplayName))
                return data.ShopDisplayName;
        }

        try
        {
            var dict = Game1.content.Load<Dictionary<string, FarmAnimalData>>(Strings.AssetNames.FarmAnimals);
            if (dict.TryGetValue(animalId, out var d) && !string.IsNullOrWhiteSpace(d.DisplayName))
                return d.DisplayName;
        }
        catch
        {
            // Best-effort.
        }

        return animalId;
    }

    private static string T(ContentContext context, string key, string fallback, params object[] tokens)
    {
        var translated = tokens.Length == 0
            ? context.I18n.GetString(key)
            : context.I18n.GetString(key, tokens);

        if (!string.IsNullOrWhiteSpace(translated) && !string.Equals(translated, key, StringComparison.OrdinalIgnoreCase))
            return translated;

        return tokens.Length == 0 ? fallback : string.Format(fallback, tokens);
    }

    private static string TranslateOrFallback(ContentContext context, string key, string fallback)
        => T(context, key, fallback);
}
