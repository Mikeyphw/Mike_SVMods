using System.Collections.Generic;
using StardewModdingAPI.Events;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.Core.Ids;

namespace WildEggsHatching.Infrastructure.Content.Editors;

public sealed class MailEditor : AssetEditorBase
{
    public override string AssetName => GameAssets.Mail;

    public override void Edit(IAssetData asset, ContentContext context)
    {
        var data = asset.AsDictionary<string, string>().Data;

        foreach (var (id, value) in BuildMail(context))
            data[id] = value;
    }

    private static IReadOnlyDictionary<string, string> BuildMail(ContentContext context)
    {
        var mail = new Dictionary<string, string>();

        // Only letters (noLetter=false) need content in Data/mail. Flag-only mail doesn't.
        if (context.Step >= ProgressionStep.PartII)
        {
            mail[ProgressionIds.Mail.MarnieThanks] = CreateMailWithItem(
                context,
                bodyKey: "mail.MarnieThanks",
                bodyFallback: "Thanks for helping with those wild eggs. The Wizard asked you to stop by his tower.",
                itemType: "quest",
                itemId: ProgressionIds.Quests.WizardStudy,
                autoAccept: true,
                displayName: "Wild Egg Thanks"
            );
        }

        if (context.Step >= ProgressionStep.PartIV)
        {
            mail[ProgressionIds.Mail.WizardThanks] = CreateMailWithItem(
                context,
                bodyKey: "mail.WizardThanks",
                bodyFallback: "Your research has proven useful. I have another task for you.",
                itemType: "quest",
                itemId: ProgressionIds.Quests.TheDesertScroll,
                autoAccept: false,
                displayName: "The Desert Scroll"
            );
        }

        if (context.Step >= ProgressionStep.PartV)
        {
            mail[ProgressionIds.Mail.WizardFindings] = CreateMailWithItem(
                context,
                bodyKey: "mail.WizardFindings",
                bodyFallback: "These findings may help you craft a potent elixir.",
                itemType: "craftingRecipe",
                itemId: "WildEggs_ElixirCrafting",
                autoAccept: false,
                displayName: "Wizard Findings"
            );
        }

        return mail;
    }

    private static string CreateMailWithItem(
        ContentContext context,
        string bodyKey,
        string bodyFallback,
        string itemType,
        string itemId,
        bool autoAccept,
        string displayName)
    {
        var body = TranslateOrFallback(context, bodyKey, bodyFallback);

        // Format aligns with the legacy Data/mail schema used by the original mod.
        // Item IDs here are quest IDs or crafting recipe IDs depending on itemType.
        return $"{body} %item {itemType} {itemId}{(autoAccept ? " true" : "")} %% [#]{displayName}";
    }

    private static string TranslateOrFallback(ContentContext context, string key, string fallback)
    {
        var translated = context.I18n.Get(key).ToString();
        return string.Equals(translated, key, System.StringComparison.OrdinalIgnoreCase) ? fallback : translated;
    }
}
