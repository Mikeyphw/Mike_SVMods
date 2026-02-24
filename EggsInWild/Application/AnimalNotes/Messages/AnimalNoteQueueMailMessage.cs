namespace EggsInWild.Application.AnimalNotes.Messages;

public sealed class AnimalNoteQueueMailMessage
{
    public string MailId { get; set; } = string.Empty;
    public bool NoLetter { get; set; }
}
