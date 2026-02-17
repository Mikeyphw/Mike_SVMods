namespace WildEggsHatching.Application.AnimalNotes;

public sealed class HatchedAnimalRecord
{
    public string FarmAnimalName { get; set; } = string.Empty;
    public long AnimalId { get; set; }

    /// <summary>AnimalHouse.NameOrUniqueName for the building indoors where this animal lives.</summary>
    public string HomeNameOrUniqueName { get; set; } = string.Empty;

    public string HouseType { get; set; } = string.Empty;

    /// <summary>Stable identity token used for deduping across moves/loads.</summary>
    public string UniqueToken { get; set; } = string.Empty;

    public string NoteItemId { get; set; } = string.Empty;
    public string QuestId { get; set; } = string.Empty;
    public string MailId { get; set; } = string.Empty;

    public string LastPlacementFailureReason { get; set; } = string.Empty;
    public int ConsecutivePlacementFailureDays { get; set; }
    public int? LastPlacementAttemptDay { get; set; }
    public int? LastDevWarningDay { get; set; }

    /// <summary>True once the note object has been placed into the home location.</summary>
    public bool NotePlaced { get; set; }

    /// <summary>True once the player has been notified and the delivery quest has been added.</summary>
    public bool QuestGiven { get; set; }

    public int? DayNotePlaced { get; set; }
    public int? DayQuestGiven { get; set; }
}
