using System.Collections.Generic;
using StardewModdingAPI.Events;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.Core.Ids;

namespace WildEggsHatching.Infrastructure.Content.Editors;

public sealed class QuestEditor : AssetEditorBase
{
    public override string AssetName => GameAssets.Quests;

    public override void Edit(IAssetData asset, ContentContext context)
    {
        // The original mod uses the legacy Data/Quests string schema.
        // Keeping this format maintains parity and avoids binding to SDV internal quest-data models.
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
                title: TranslateOrFallback(context, "quest.theDesertScroll.title", "The Desert Scroll"),
                details: TranslateOrFallback(context, "quest.theDesertScroll.questDetails", "Deliver the scroll to the Wizard."),
                hint: TranslateOrFallback(context, "quest.theDesertScroll.hint", "A scroll from the desert might help."),
                solution: $"Wizard {ModItemIds.OsirisScroll}",
                gold: 500,
                cancellable: false,
                reaction: TranslateOrFallback(context, "quest.theDesertScroll.reactionText", "This will do nicely.")
            );
        }

        return quests;
    }

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

    private static string TranslateOrFallback(ContentContext context, string key, string fallback)
    {
        var translated = context.I18n.Get(key).ToString();
        return string.Equals(translated, key, System.StringComparison.OrdinalIgnoreCase) ? fallback : translated;
    }
}
