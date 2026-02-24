namespace EggsInWild.Application.AnimalNotes.Messages;

public sealed class AnimalNoteFoundMessage
{
    public string NoteItemId { get; set; } = string.Empty;
    public string HomeNameOrUniqueName { get; set; } = string.Empty;
    public string FoundByFarmerName { get; set; } = string.Empty;
}
