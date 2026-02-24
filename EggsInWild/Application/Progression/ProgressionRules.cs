using System.Collections.Generic;
using EggsInWild.Core.Enums;
using EggsInWild.Core.Ids;
using EggsInWild.Infrastructure.Config;
using EggsInWild.Infrastructure.Game;

namespace EggsInWild.Application.Progression;

public static class ProgressionRules
{
    public static ProgressionStep MaxStep => ProgressionStep.PartVI;

    private static readonly IReadOnlyDictionary<string, int> DaysToWait =
        new Dictionary<string, int>
        {
            { ProgressionIds.Quests.SpecialOrderWildEggsI, 2 },
            { ProgressionIds.Quests.WizardStudy, 2 },
            { ProgressionIds.Quests.SpecialOrderWildEggsII, 7 },
            { ProgressionIds.Quests.TheDesertScroll, 5 },
        };

    public static int GetDaysToWait(string questOrOrderId)
        => DaysToWait.TryGetValue(questOrOrderId, out var days) ? days : 0;

    public static bool IsStepUnlocked(
        ProgressionStep step,
        UnlockFacts facts,
        ModConfig config
    )
    {
        if (!config.ProgressionMode)
            return true;

        if (!config.DesertTombModuleEnabled && step == ProgressionStep.PartV)
            return true;

        if (!config.DesertTombModuleEnabled && step == ProgressionStep.PartVI)
            return true;

        return step switch
        {
            ProgressionStep.PartI => true,
            ProgressionStep.PartII => facts.HasReceivedMail(ProgressionIds.Mail.SpecialOrderDoneI),
            ProgressionStep.PartIII => HasEnoughDaysPassed(
                facts,
                ProgressionIds.Quests.WizardStudy,
                isSpecialOrder: false,
                config
            ),
            ProgressionStep.PartIV => HasEnoughDaysPassed(
                facts,
                ProgressionIds.Quests.SpecialOrderWildEggsII,
                isSpecialOrder: true,
                config
            ),
            ProgressionStep.PartV => HasEnoughDaysPassed(
                facts,
                ProgressionIds.Quests.TheDesertScroll,
                isSpecialOrder: false,
                config
            ),
            ProgressionStep.PartVI => HasEnoughDaysPassed(
                facts,
                ProgressionIds.Quests.TheDesertScroll,
                isSpecialOrder: false,
                daysToWaitOverride: GetDaysToWait(ProgressionIds.Quests.TheDesertScroll) + 2,
                config: config
            ),
            _ => false,
        };
    }

    public static void ApplyStepUnlockSideEffects(
        ProgressionStep step,
        IGameMailService mail,
        ModConfig config
    )
    {
        if (!config.ProgressionMode)
            return;

        if (!config.DesertTombModuleEnabled && step == ProgressionStep.PartV)
            return;

        // Keep behavior aligned with the original: mail changes are triggered on step unlock.
        switch (step)
        {
            case ProgressionStep.PartII:
                mail.AddMailForTomorrow(ProgressionIds.Mail.MarnieThanks, noLetter: false);
                break;

            case ProgressionStep.PartIII:
                mail.AddReceivedMail(ProgressionIds.Mail.WizardDone, noLetter: true);
                break;

            case ProgressionStep.PartIV:
                mail.AddReceivedMail(ProgressionIds.Mail.WizardThanks, noLetter: false);
                break;

            case ProgressionStep.PartV:
                mail.AddReceivedMail(ProgressionIds.Mail.WizardFindings, noLetter: false);
                break;

            case ProgressionStep.PartVI:
                mail.AddReceivedMail(ProgressionIds.Mail.MarnieGotIncubator, noLetter: true);
                break;
        }
    }

    private static bool HasEnoughDaysPassed(
        UnlockFacts facts,
        string questOrOrderId,
        bool isSpecialOrder,
        ModConfig config,
        int? daysToWaitOverride = null
    )
    {
        if (!config.ProgressionMode)
            return true;

        if (isSpecialOrder && !facts.HasCompletedSpecialOrder(questOrOrderId))
            return false;

        if (!isSpecialOrder && !facts.HasCompletedQuest(questOrOrderId))
            return false;

        if (!facts.QuestCompletedDay.TryGetValue(questOrOrderId, out var completionDay))
            return false;

        var daysToWait = daysToWaitOverride ?? GetDaysToWait(questOrOrderId);
        return facts.DayNumber - completionDay >= daysToWait;
    }
}
