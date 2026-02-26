using System.Collections.Generic;
using EggsInWild.Core.Enums;
using EggsInWild.Core.Ids;

namespace EggsInWild.Application.Progression;

public sealed class ProgressionStepEffects
{
    public List<(string MailId, bool NoLetter)> MailReceived { get; } = new();
    public List<(string MailId, bool NoLetter)> MailForTomorrow { get; } = new();
    public List<string> Quests { get; } = new();
    public List<string> SpecialOrders { get; } = new();
    public List<string> CraftingRecipes { get; } = new();
    public List<string> CookingRecipes { get; } = new();

    public static IReadOnlyDictionary<ProgressionStep, ProgressionStepEffects> BuildCatalog()
    {
        // This catalog stays aligned with the original progression side effects.
        // Console 'step set --effects' applies these cumulatively and can reconcile on rollback.
        var dict = new Dictionary<ProgressionStep, ProgressionStepEffects>();

        var part2 = new ProgressionStepEffects();
        part2.MailReceived.Add((ProgressionIds.Mail.SpecialOrderDoneI, true));
        part2.MailForTomorrow.Add((ProgressionIds.Mail.MarnieThanks, false));
        dict[ProgressionStep.PartII] = part2;

        var part3 = new ProgressionStepEffects();
        part3.MailReceived.Add((ProgressionIds.Mail.WizardDone, true));
        dict[ProgressionStep.PartIII] = part3;

        var part4 = new ProgressionStepEffects();
        part4.MailReceived.Add((ProgressionIds.Mail.SpecialOrderDoneII, true));
        part4.MailReceived.Add((ProgressionIds.Mail.WizardThanks, false));
        dict[ProgressionStep.PartIV] = part4;

        var part5 = new ProgressionStepEffects();
        part5.MailReceived.Add((ProgressionIds.Mail.WizardFindings, false));
        dict[ProgressionStep.PartV] = part5;

        var part6 = new ProgressionStepEffects();
        part6.MailReceived.Add((ProgressionIds.Mail.MarnieGotIncubator, true));
        dict[ProgressionStep.PartVI] = part6;

        return dict;
    }
}
