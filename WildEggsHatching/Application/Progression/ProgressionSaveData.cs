using System.Collections.Generic;
using WildEggsHatching.Core.Enums;

namespace WildEggsHatching.Application.Progression;

public sealed class ProgressionSaveData
{
    public ProgressionStep Step { get; set; } = ProgressionStep.PartI;

    /// <summary>
    /// Completion day for each tracked quest/special order (Game1.Date.TotalDays), as recorded by this mod.
    /// </summary>
    public Dictionary<string, int> QuestCompletedDay { get; set; } = new();

    /// <summary>Prevents multiple step advances in a single in-game day.</summary>
    public int LastStepAdvanceDay { get; set; } = -1;
}
