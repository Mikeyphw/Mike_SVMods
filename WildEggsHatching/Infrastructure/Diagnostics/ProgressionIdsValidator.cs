using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley.GameData.SpecialOrders;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.Core.Ids;

namespace WildEggsHatching.Infrastructure.Diagnostics;

internal static class ProgressionIdsValidator
{
    public static (WeggVerifyStatus Status, string Message) Validate(
        ProgressionStep step,
        IReadOnlyDictionary<string, string>? mail,
        IReadOnlyDictionary<string, string>? quests,
        IReadOnlyDictionary<string, SpecialOrderData>? specialOrders,
        bool full)
    {
        var missing = new List<string>();

        var expectedMail = ExpectedMailIds(step, full);
        var expectedQuests = ExpectedQuestIds(step, full);
        var expectedOrders = ExpectedSpecialOrderIds(step, full);

        if (expectedMail.Count > 0)
        {
            if (mail is null)
                missing.Add("Data/mail unavailable");
            else
                missing.AddRange(expectedMail.Where(id => !mail.ContainsKey(id)).Select(id => $"Data/mail missing: {id}"));
        }

        if (expectedQuests.Count > 0)
        {
            if (quests is null)
                missing.Add("Data/Quests unavailable");
            else
                missing.AddRange(expectedQuests.Where(id => !quests.ContainsKey(id)).Select(id => $"Data/Quests missing: {id}"));
        }

        if (expectedOrders.Count > 0)
        {
            if (specialOrders is null)
                missing.Add("Data/SpecialOrders unavailable");
            else
                missing.AddRange(expectedOrders.Where(id => !specialOrders.ContainsKey(id)).Select(id => $"Data/SpecialOrders missing: {id}"));
        }

        if (missing.Count == 0)
        {
            var msg = $"Progression IDs OK (mail={expectedMail.Count}, quests={expectedQuests.Count}, orders={expectedOrders.Count}).";
            return (WeggVerifyStatus.Pass, msg);
        }

        var sampleN = full ? missing.Count : Math.Min(10, missing.Count);
        var sample = missing.Take(sampleN).ToList();
        var suffix = missing.Count > sampleN ? $" (+{missing.Count - sampleN} more)" : string.Empty;

        return (WeggVerifyStatus.Fail,
            $"Progression ID mismatch (expected IDs missing): count={missing.Count}. MissingProgressionIds=[{string.Join("; ", sample)}]{suffix}");
    }

    private static IReadOnlyList<string> ExpectedMailIds(ProgressionStep step, bool full)
    {
        // Only letters (noLetter=false) need Data/mail content.
        // Flag-only mail (noLetter=true) is intentionally absent from Data/mail.
        if (full)
            return new[] { ProgressionIds.Mail.MarnieThanks, ProgressionIds.Mail.WizardThanks, ProgressionIds.Mail.WizardFindings };

        var ids = new List<string>();
        if (step >= ProgressionStep.PartII)
            ids.Add(ProgressionIds.Mail.MarnieThanks);
        if (step >= ProgressionStep.PartIV)
            ids.Add(ProgressionIds.Mail.WizardThanks);
        if (step >= ProgressionStep.PartV)
            ids.Add(ProgressionIds.Mail.WizardFindings);
        return ids;
    }

    private static IReadOnlyList<string> ExpectedQuestIds(ProgressionStep step, bool full)
    {
        if (full)
            return new[] { ProgressionIds.Quests.WizardStudy, ProgressionIds.Quests.TheDesertScroll };

        var ids = new List<string>();
        if (step >= ProgressionStep.PartII)
            ids.Add(ProgressionIds.Quests.WizardStudy);
        if (step >= ProgressionStep.PartIV)
            ids.Add(ProgressionIds.Quests.TheDesertScroll);
        return ids;
    }

    private static IReadOnlyList<string> ExpectedSpecialOrderIds(ProgressionStep step, bool full)
    {
        // IDs are stored in ProgressionIds.Quests.* but represent SpecialOrders asset keys.
        if (full)
            return new[] { ProgressionIds.Quests.SpecialOrderWildEggsI, ProgressionIds.Quests.SpecialOrderWildEggsII };

        var ids = new List<string>();
        if (step >= ProgressionStep.PartI)
            ids.Add(ProgressionIds.Quests.SpecialOrderWildEggsI);
        if (step >= ProgressionStep.PartIII)
            ids.Add(ProgressionIds.Quests.SpecialOrderWildEggsII);
        return ids;
    }
}
