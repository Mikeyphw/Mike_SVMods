using System.Collections.Generic;
using System;
using System.Linq;
using StardewValley;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.Core.Ids;
using Mikew.BirdsOfAFeather.Helpers.Content;
using Mikew.BirdsOfAFeather;

namespace WildEggsHatching.Infrastructure.Content.Editors;

public sealed class QuestEditor : AssetEditorBase<ContentContext>
{
    private static readonly object LockObj = new();
    private static IReadOnlyList<string>? _animalKeys;

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
                gold: goldReward,
                cancellable: false,
                reaction: TranslateOrFallback(context, "quest.WizardStudy.reactionText", "Interesting... very interesting.")
            );
        }

        if (context.Step >= ProgressionStep.PartIV)
        {
            quests[ProgressionIds.Quests.TheDesertScroll] = CreateQuestString(
                type: "ItemDelivery",
                title: TranslateOrFallback(context, "quest.theDesertScroll.title", "The Desert Scroll"),
                details: TranslateOrFallback(context, "quest.theDesertScroll.questDetails", "Deliver the scroll to the Wizard."),
                hint: TranslateOrFallback(context, "quest.theDesertScroll.hint", "A scroll from the desert might help."),
                solution: $"Wizard {ModItemIds.OsirisScroll}",
                gold: 500,
                cancellable: false,
                reaction: TranslateOrFallback(context, "quest.theDesertScroll.reactionText", "This will do nicely.")
            );
        }

        if (context.Step >= ProgressionStep.PartVI)
        {
            foreach (var animalKey in GetAnimalKeys(context))
            {
                var token = NormalizeToken(animalKey);
                var questId = WildEggsHatching.Application.AnimalNotes.AnimalNoteIds.NoteQuestId(animalKey);
                var noteId = $"{WildEggsHatching.Constants.ModId}_junimoNote_{token}";

                var baseTitle = TranslateOrFallback(context, "quest.NoteDelivery.title", "Note Delivery");
                var baseDetails = TranslateOrFallback(context, "quest.NoteDelivery.questDetails", "Deliver the note to Marnie.");
                var baseHint = TranslateOrFallback(context, "quest.NoteDelivery.hint", "Deliver the note.");

                var reactionKey = $"quest.NoteDelivery.reactionText{StardewValley.Game1.random.Next(4)}";
                var reaction = TranslateOrFallback(context, reactionKey, "Thanks for bringing this to me.");

                var goldReward = GetNoteQuestGoldReward(context, animalKey);
                var shopDescription = TryGetAnimalShopDescription(animalKey);
                if (!string.IsNullOrWhiteSpace(shopDescription))
                    reaction = $"{reaction}^^{shopDescription}";

                quests[questId] = CreateQuestString(
                    type: "ItemDelivery",
                    title: $"{baseTitle}: {animalKey}",
                    details: $"{baseDetails}: {animalKey}.",
                    hint: $"{baseHint}: {animalKey}.",
                    solution: $"Marnie {noteId}",
                    gold: goldReward,
                    cancellable: false,
                    reaction: reaction
                );
}
        }

        return quests;
    }

    
    private static IReadOnlyList<string> GetAnimalKeys(ContentContext context)
    {
        if (_animalKeys is not null)
            return _animalKeys;

        lock (LockObj)
        {
            if (_animalKeys is not null)
                return _animalKeys;

            var keys = context.ModData.WildAnimals.Keys
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .ToList();

            _animalKeys = keys;
            return _animalKeys;
        }
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

    private static string TranslateOrFallback(ContentContext context, string key, string fallback)
    {
        var translated = context.I18n.Get(key).ToString();
        return string.Equals(translated, key, System.StringComparison.OrdinalIgnoreCase) ? fallback : translated;
    }
}