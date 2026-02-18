using System.Collections.Generic;

namespace WildEggsHatching.Core.Ids;

/// <summary>IDs used by the WildEggsHatching progression system.</summary>
public static class ProgressionIds
{
    public static class Quests
    {
        public const string SpecialOrderWildEggsI = "wegg_specialOrderI";
        public const string SpecialOrderWildEggsII = "wegg_specialOrderII";
        public const string WizardStudy = "wegg_WizardStudy";
        public const string TheDesertScroll = "wegg_theDesertScroll";

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
        public const string SpecialOrderDoneI = "wegg_specialOrderDoneI";
        public const string SpecialOrderDoneII = "wegg_specialOrderDoneII";
        public const string MarnieThanks = "wegg_marnieThanks";
        public const string WizardThanks = "wegg_wizardThanks";
        public const string WizardDone = "wegg_wizardStudyDone";
        public const string WizardFindings = "wegg_wizardFindings";
        public const string MarnieGotIncubator = "wegg_marnieIncubator";

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
