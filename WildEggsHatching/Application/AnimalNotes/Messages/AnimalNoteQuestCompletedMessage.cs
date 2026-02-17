namespace WildEggsHatching.Application.AnimalNotes.Messages;

public sealed class AnimalNoteQuestCompletedMessage
{
    public string QuestId { get; set; } = string.Empty;
}
