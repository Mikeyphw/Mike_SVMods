using System.Collections.Generic;
using System;
using System.Linq;
using StardewValley;
using StardewValley.GameData.FarmAnimals;
using EggsInWild.Core.Enums;
using EggsInWild.Core.Ids;
using EggsInWild.Application.AnimalNotes;
using Mikew.NestCore.Helpers.Content;
using Mikew.NestCore;

namespace EggsInWild.Infrastructure.Content.Editors;

public sealed class MailEditor : AssetEditorBase<ContentContext>
{
    public override string AssetName => Strings.AssetNames.Mail;

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
                displayName: T(context, "mail.MarnieThanks.title", "Wild Egg Thanks")
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
                displayName: T(context, "mail.WizardThanks.title", "The Desert Scroll")
            );
        }

        if (context.Step >= ProgressionStep.PartV)
        {
            mail[ProgressionIds.Mail.WizardFindings] = CreateMailWithItem(
                context,
                bodyKey: "mail.WizardFindings",
                bodyFallback: "These findings may help you craft a potent elixir.",
                itemType: "craftingRecipe",
                itemId: "Elixir Of Revitalized Essence",
                autoAccept: false,
                displayName: T(context, "mail.WizardFindings.title", "Wizard Findings")
            );
        }

        if (context.Step >= ProgressionStep.PartVI)
        {
            foreach (var animalKey in GetAnimalKeys(context))
            {
                var mailId = AnimalNoteIds.NoteMailId(animalKey);
                mail[mailId] = CreateNoteMail(context, animalKey);
            }
        }

        return mail;
    }

    private static string CreateNoteMail(ContentContext context, string animalKey)
    {
        var variant = Game1.random.Next(4);

        var animalName = ResolveFarmAnimalDisplayName(animalKey);

        var body = TAny(
            context,
            keys: new[]
            {
                $"mail.animalAvailable.body{variant}",
                $"mail.animalAvaliable{variant}",
            },
            fallback: "Dear @,^^Thanks for the note! The animals are doing great and are now available in my shop.");

        var banner = TAny(
            context,
            keys: new[]
            {
                "mail.animalAvailable.banner",
                "mail.animalAvaliable.newAnimal",
            },
            fallback: "**New Animal Available:**");

        var title = T(context, "mail.animalAvailable.title", "{0} Available", animalName);

        return $"{body}^^{banner} {animalName}**[#]{title}";
    }

    private static IReadOnlyList<string> GetAnimalKeys(ContentContext context)
    {
        return context.ModData.WildAnimals.Keys
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
            .ToList();
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

        // Format aligns with the prior Data/mail schema used by the original mod.
        // Item IDs here are quest IDs or crafting recipe IDs depending on itemType.
        return $"{body} %item {itemType} {itemId}{(autoAccept ? " true" : "")} %% [#]{displayName}";
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

    private static string TAny(ContentContext context, IEnumerable<string> keys, string fallback, params object[] tokens)
    {
        foreach (var k in keys)
        {
            var translated = tokens.Length == 0
                ? context.I18n.GetString(k)
                : context.I18n.GetString(k, tokens);

            if (!string.IsNullOrWhiteSpace(translated) && !string.Equals(translated, k, StringComparison.OrdinalIgnoreCase))
                return translated;
        }

        return tokens.Length == 0 ? fallback : string.Format(fallback, tokens);
    }


private static string TranslateOrFallback(ContentContext context, string key, string fallback)
    {
        var translated = context.I18n.Get(key).ToString();
        return string.Equals(translated, key, System.StringComparison.OrdinalIgnoreCase) ? fallback : translated;
    }
}
