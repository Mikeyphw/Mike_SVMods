using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.Infrastructure.Content;

namespace WildEggsHatching.Infrastructure.Content.Editors;

/// <summary>
/// Editor for Data/Mail asset - adds progression-related mail letters.
/// Refactored to use i18n and support dynamic note mail for hatched animals.
/// </summary>
public sealed class MailEditor : ProgressionAwareAssetEditor
{
    /// <inheritdoc />
    public override string AssetName => GameAssets.Mail;

    /// <summary>
    /// Mail item type constants.
    /// </summary>
    private const string ItemTypeQuest = "quest";
    private const string ItemTypeCraftingRecipe = "craftingRecipe";

    /// <summary>
    /// Creates a new mail editor.
    /// </summary>
    public MailEditor() : base() { }

    /// <summary>
    /// Creates a new mail editor with dependencies.
    /// </summary>
    public MailEditor(
        PhaseProgressionService progressionService,
        IConfigProvider configProvider,
        IMonitor monitor,
        IModHelper modHelper) 
        : base(progressionService, configProvider, monitor, modHelper) { }

    /// <inheritdoc />
    public override void Edit<T>(IAssetData<T> asset, string assetName)
    {
        if (assetName != GameAssets.Mail)
            return;

        var mail = asset.AsDictionary<string, string>().Data;

        // Part II: Special Order Done I + Marnie Thanks
        if (HasReachedStep(ProgressionStep.PartII))
        {
            AddMail(mail, WildEggConstants.MailIds.SpecialOrderDoneI, GetSpecialOrderDoneIText());
            AddMail(mail, WildEggConstants.MailIds.MarnieThanks, GetMarnieThanksText());
        }

        // Part III: Wizard Done
        if (HasReachedStep(ProgressionStep.PartIII))
        {
            AddMail(mail, WildEggConstants.MailIds.WizardDone, GetWizardDoneText());
        }

        // Part IV: Special Order Done II + Wizard Thanks
        if (HasReachedStep(ProgressionStep.PartIV))
        {
            AddMail(mail, WildEggConstants.MailIds.SpecialOrderDoneII, GetSpecialOrderDoneIIText());
            AddMail(mail, WildEggConstants.MailIds.WizardThanks, GetWizardThanksText());
        }

        // Part V: Wizard Findings
        if (HasReachedStep(ProgressionStep.PartV))
        {
            AddMail(mail, WildEggConstants.MailIds.WizardFindings, GetWizardFindingsText());
        }

        // Part VI: Marnie Got Incubator
        if (HasReachedStep(ProgressionStep.PartVI))
        {
            AddMail(mail, WildEggConstants.MailIds.MarnieGotIncubator, GetMarnieGotIncubatorText());
        }

        // Note: Dynamic note mail for hatched animals would be added here
        // This requires access to the player state data which would be passed in

        Log($"MailEditor applied edits to {mail.Count} mail entries");
    }

    /// <summary>
    /// Adds a mail entry using i18n.
    /// </summary>
    private void AddMail(IDictionary<string, string> mail, string id, string text)
    {
        if (!mail.ContainsKey(id))
        {
            mail[id] = text;
            Log($"Added mail: {id}", LogLevel.Trace);
        }
    }

    /// <summary>
    /// Creates mail with an attached quest or crafting recipe.
    /// Uses i18n for the text content.
    /// </summary>
    private string CreateMailWithItem(string textKey, string itemType, string itemId, bool autoAccept = true, string? mailName = null)
    {
        string text = Translate(textKey, GetDefaultMailText(textKey));
        return CreateMailWithItem(text, itemType, itemId, autoAccept, mailName);
    }

    /// <summary>
    /// Creates note mail for a hatched animal.
    /// This is called when an animal hatches to notify the player.
    /// </summary>
    /// <param name="animalKey">The farm animal type key (e.g., "Chicken", "Duck").</param>
    /// <param name="displayName">The display name of the hatched animal.</param>
    /// <param name="mailId">The unique mail ID for this notification.</param>
    public string CreateNoteMail(string animalKey, string displayName, string mailId)
    {
        // Get random variant text key
        int randomVariant = new Random().Next(3);
        string textKey = $"mail.animalAvaliable{randomVariant}";
        
        // Get translated text with fallback
        string textTemplate = Translate(
            textKey, 
            "Your new animal is ready! Come visit them at the farm."
        );
        
        string animalText = Translate(
            "mail.animalAvaliable.newanimal",
            "new animal"
        );
        
        string text = $"{textTemplate} {animalText} {displayName}**";
        
        // Create mail with the standard format
        return CreateMailWithItem(
            text,
            ItemTypeQuest,
            mailId,
            autoAccept: false,
            mailName: $"{displayName} Available"
        );
    }

    /// <summary>
    /// Adds dynamic note mail entries for hatched animals.
    /// Call this with the list of hatched animals from player state.
    /// </summary>
    /// <param name="mail">The mail dictionary to add to.</param>
    /// <param name="hatchedAnimals">List of hatched animal data (animal key and display name).</param>
    public void AddNoteMailForHatchedAnimals(
        IDictionary<string, string> mail, 
        IEnumerable<(string AnimalKey, string DisplayName, string MailId)> hatchedAnimals)
    {
        foreach (var (animalKey, displayName, mailId) in hatchedAnimals)
        {
            // Skip if mail already exists
            if (mail.ContainsKey(mailId))
                continue;

            mail[mailId] = CreateNoteMail(animalKey, displayName, mailId);
            Log($"Added note mail for hatched animal: {displayName}", LogLevel.Trace);
        }
    }

    /// <summary>
    /// Adds a crafting recipe mail for a hatched animal's unlock.
    /// </summary>
    public void AddAnimalRecipeMail(
        IDictionary<string, string> mail,
        string mailId,
        string recipeId,
        string requesterName)
    {
        if (mail.ContainsKey(mailId))
            return;

        string text = Translate(
            $"mail.{requesterName.ToLower()}Recipe",
            "Dear @,\n\nThank you for helping with research! Here's a new recipe for you.\n\n^Marnie"
        );

        mail[mailId] = CreateMailWithItem(
            text,
            ItemTypeCraftingRecipe,
            recipeId,
            autoAccept: true,
            mailName: $"{requesterName} Recipe"
        );
    }

    #region Mail Text Helpers

    private static string GetDefaultMailText(string key)
    {
        return key switch
        {
            "mail.MarnieThanks" => "Thank you for helping with the special order!",
            "mail.WizardDone" => "Your dedication has impressed me.",
            "mail.SpecialOrderDoneI" => "I've finished analyzing the egg samples.",
            "mail.SpecialOrderDoneII" => "Another successful study completed!",
            "mail.WizardThanks" => "Thank you for completing the research.",
            "mail.WizardFindings" => "I've made a breakthrough!",
            "mail.MarnieIncubator" => "Thanks to your research, we've cracked the code!",
            _ => ""
        };
    }

    private static string GetSpecialOrderDoneIText() =>
        "Dear @,\n\n" +
        "I've finished analyzing the egg samples you brought me. " +
        "The results are fascinating! There's definitely something special about these eggs.%fork\r\n" +
        "I believe we're on the verge of a breakthrough in our understanding of these creatures.%fork\r\n" +
        "Come see me at the tower when you have a chance - I have something important to discuss with you.%fork\r\n" +
        "^Wizard";

    private static string GetMarnieThanksText() =>
        "Dear @,\n\n" +
        "Thank you so much for helping with the special order! " +
        "The samples you provided have been invaluable for our research.%fork\r\n" +
        "We've learned so much about these mysterious eggs.%fork\r\n" +
        "Keep up the great work!\r\n" +
        "^Marnie";

    private static string GetWizardDoneText() =>
        "Dear @,\n\n" +
        "Your dedication to studying the wild eggs has impressed me.%fork\r\n" +
        "I've completed my initial analysis and have assigned you to a new research task.%fork\r\n" +
        "There's still much to discover about these remarkable creatures.%fork\r\n" +
        "^Wizard";

    private static string GetSpecialOrderDoneIIText() =>
        "Dear @,\n\n" +
        "Another successful study completed! Your contributions to this research are invaluable.%fork\r\n" +
        "We've made significant progress in understanding these eggs.%fork\r\n" +
        "I look forward to our continued collaboration.%fork\r\n" +
        "^Wizard";

    private static string GetWizardThanksText() =>
        "Dear @,\n\n" +
        "Thank you for completing the second phase of research.%fork\r\n" +
        "Your findings have shed new light on these ancient creatures.%fork\r\n" +
        "I believe we're close to a major discovery...%fork\r\n" +
        "^Wizard";

    private static string GetWizardFindingsText() =>
        "Dear @,\n\n" +
        "I've made a breakthrough! The ancient texts speak of a legendary scroll " +
        "that contains the secrets of these magical creatures.%fork\r\n" +
        "This could be the key to understanding everything about these eggs.%fork\r\n" +
        "Seek out the desert tomb - the scroll awaits those who are worthy.%fork\r\n" +
        "^Wizard";

    private static string GetMarnieGotIncubatorText() =>
        "Dear @,\n\n" +
        "Thanks to your research, we've finally cracked the code on hatching these eggs!%fork\r\n" +
        "I've developed a special incubator that can help these creatures hatch safely.%fork\r\n" +
        "You can now craft it at home. Good luck with your new friends!%fork\r\n" +
        "^Marnie";

    #endregion

    /// <inheritdoc />
    public override bool ShouldApply() => true;
}

