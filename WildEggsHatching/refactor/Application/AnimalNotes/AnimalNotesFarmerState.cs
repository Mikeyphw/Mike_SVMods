using System.Collections.Generic;

namespace WildEggsHatching.Application.AnimalNotes;

public sealed class AnimalNotesFarmerState
{
    /// <summary>True once the delivery quest has been issued to this farmer.</summary>
    public bool QuestGiven { get; set; }

    public int? DayQuestGiven { get; set; }

    /// <summary>Day number (absolute) when the farmer completed the delivery quest.</summary>
    public int? DayQuestCompleted { get; set; }

    /// <summary>True once unlock mail was queued/sent for this farmer.</summary>
    public bool MailQueued { get; set; }

    public int? DayMailQueued { get; set; }
}
