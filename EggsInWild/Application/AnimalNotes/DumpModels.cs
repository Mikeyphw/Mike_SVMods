using System.Collections.Generic;

namespace EggsInWild.Application.AnimalNotes;

internal sealed class DumpModel
{
    public string Step { get; set; } = string.Empty;
    public int Day { get; set; }
    public int PendingCount { get; set; }
    public int RegisteredTypesCount { get; set; }
    public DumpConfigModel Config { get; set; } = new();
    public List<DumpFarmerModel> Farmers { get; set; } = new();
    public List<DumpRecordModel> Records { get; set; } = new();
}

internal sealed class DumpConfigModel
{
    public bool DebugMode { get; set; }
    public int WarnAfterDays { get; set; }
    public int PlacementRadius { get; set; }
}

internal sealed class DumpFarmerModel
{
    public string Name { get; set; } = string.Empty;
    public long FarmerId { get; set; }
}

internal sealed class DumpRecordModel
{
    public string AnimalToken { get; set; } = string.Empty;
    public long AnimalId { get; set; }
    public string HomeNameOrUniqueName { get; set; } = string.Empty;
    public string UniqueToken { get; set; } = string.Empty;

    public string NoteItemId { get; set; } = string.Empty;
    public bool NotePlaced { get; set; }
    public bool NoteExistsNow { get; set; }
    public string QuestId { get; set; } = string.Empty;
    public string MailId { get; set; } = string.Empty;

    public string? NoteBlocked { get; set; }
    public int PlacementFailureDays { get; set; }
    public string? LastPlacementFailureReason { get; set; }

    public bool CleanupReadyBalanced { get; set; }
    public List<string> CleanupBlockedBy { get; set; } = new();

    public List<DumpRecordFarmerModel> Farmers { get; set; } = new();
}

internal sealed class DumpRecordFarmerModel
{
    public string Name { get; set; } = string.Empty;
    public long FarmerId { get; set; }

    public bool QuestGiven { get; set; }
    public bool QuestActive { get; set; }
    public bool QuestCompleted { get; set; }
    public int? QuestCompletedDay { get; set; }

    public bool MailQueued { get; set; }
    public bool MailTomorrow { get; set; }
    public bool MailReceived { get; set; }

    public bool IsInProgress { get; set; }
    public int? UnlockEligibleInDays { get; set; }

    public string? QuestBlocked { get; set; }
    public string? MailBlocked { get; set; }
}
