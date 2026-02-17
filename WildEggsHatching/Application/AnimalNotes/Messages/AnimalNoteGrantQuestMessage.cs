namespace WildEggsHatching.Application.AnimalNotes.Messages;

public sealed class AnimalNoteGrantQuestMessage
{
    public string QuestId { get; set; } = string.Empty;
    public string NoteItemId { get; set; } = string.Empty;
}
