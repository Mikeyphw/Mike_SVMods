using System.Collections.Generic;

namespace WildEggsHatching.Core.Ids;

/// <summary>IDs used by the original WildEggsHatching progression system.</summary>
public static class ProgressionIds
{
    public static class Quests
    {
        public const string SpecialOrderWildEggsI = "wildEggs_specialOrderI";
        public const string SpecialOrderWildEggsII = "wildEggs_specialOrderII";
        public const string WizardStudy = "wildEggs_WizardStudy";
        public const string TheDesertScroll = "wildEggs_theDesertScroll";

        public static readonly IReadOnlyList<string> Tracked =
            new[]
            {
                SpecialOrderWildEggsI,
                WizardStudy,
                TheDesertScroll,
                SpecialOrderWildEggsII,
            };

        public static readonly ISet<string> SpecialOrders =
            new HashSet<string> { SpecialOrderWildEggsI, SpecialOrderWildEggsII };
    }

    public static class Mail
    {
        public const string SpecialOrderDoneI = "wildEggs_specialOrderDoneI";
        public const string SpecialOrderDoneII = "wildEggs_specialOrderDoneII";
        public const string MarnieThanks = "wildEggs_marnieThanks";
        public const string WizardThanks = "wildEggs_wizardThanks";
        public const string WizardDone = "wildEggs_wizardStudyDone";
        public const string WizardFindings = "wildEggs_wizardFindings";
        public const string MarnieGotIncubator = "wildEggs_marnieIncubator";

        public static readonly IReadOnlyList<string> Tracked =
            new[]
            {
                SpecialOrderDoneI,
                SpecialOrderDoneII,
                MarnieThanks,
                WizardThanks,
                WizardDone,
                WizardFindings,
                MarnieGotIncubator,
            };
    }
}
