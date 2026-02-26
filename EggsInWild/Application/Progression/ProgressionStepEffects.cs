using System.Collections.Generic;
using EggsInWild.Core.Enums;
using EggsInWild.Core.Ids;

namespace EggsInWild.Application.Progression;

public sealed class ProgressionStepEffects
{
    public List<(string MailId, bool NoLetter)> MailReceived { get; } = new();
    public List<(string MailId, bool NoLetter)> MailForTomorrow { get; } = new();

    /// <summary>Quests that should exist in the player's quest log (active/accepted).</summary>
    public List<string> ActiveQuests { get; } = new();

    /// <summary>Quests that should be marked complete for this step (and earlier).</summary>
    public List<string> CompletedQuests { get; } = new();

    /// <summary>Special orders that should be marked complete for this step (and earlier).</summary>
    public List<string> CompletedSpecialOrders { get; } = new();

    public List<string> CraftingRecipes { get; } = new();
    public List<string> CookingRecipes { get; } = new();

    public static IReadOnlyDictionary<ProgressionStep, ProgressionStepEffects> BuildCatalog()
    {
        // This catalog mirrors the original progression chain. Console 'step set --effects' applies it cumulatively.
        var dict = new Dictionary<ProgressionStep, ProgressionStepEffects>();

        // PartII: player has completed special order I and is sent to the Wizard.
        var part2 = new ProgressionStepEffects();
        part2.CompletedSpecialOrders.Add(ProgressionIds.Quests.SpecialOrderWildEggsI);
        part2.MailReceived.Add((ProgressionIds.Mail.SpecialOrderDoneI, true));
        part2.MailForTomorrow.Add((ProgressionIds.Mail.MarnieThanks, false));
        part2.ActiveQuests.Add(ProgressionIds.Quests.WizardStudy);
        dict[ProgressionStep.PartII] = part2;

        // PartIII: WizardStudy completed (after a wait) and unlocks order II.
        var part3 = new ProgressionStepEffects();
        part3.MailReceived.Add((ProgressionIds.Mail.WizardDone, true));
        part3.CompletedQuests.Add(ProgressionIds.Quests.WizardStudy);
        dict[ProgressionStep.PartIII] = part3;

        // PartIV: special order II completed (after a wait), Wizard assigns Desert Scroll.
        var part4 = new ProgressionStepEffects();
        part4.CompletedSpecialOrders.Add(ProgressionIds.Quests.SpecialOrderWildEggsII);
        part4.MailReceived.Add((ProgressionIds.Mail.SpecialOrderDoneII, true));
        part4.MailReceived.Add((ProgressionIds.Mail.WizardThanks, false));
        part4.ActiveQuests.Add(ProgressionIds.Quests.TheDesertScroll);
        dict[ProgressionStep.PartIV] = part4;

        // PartV: Desert Scroll completed (after a wait), Wizard gives Elixir recipe via mail.
        var part5 = new ProgressionStepEffects();
        part5.MailReceived.Add((ProgressionIds.Mail.WizardFindings, false));
        part5.CompletedQuests.Add(ProgressionIds.Quests.TheDesertScroll);
        part5.CraftingRecipes.Add("Elixir Of Revitalized Essence");
        dict[ProgressionStep.PartV] = part5;

        // PartVI: Marnie gets incubator; we also ensure the incubator recipe is learned for progression-mode players.
        var part6 = new ProgressionStepEffects();
        part6.MailReceived.Add((ProgressionIds.Mail.MarnieGotIncubator, true));
        part6.CraftingRecipes.Add("Wild Egg Incubator");
        dict[ProgressionStep.PartVI] = part6;

        return dict;
    }
}
