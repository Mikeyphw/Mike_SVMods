namespace WildEggsHatching.Application.AnimalNotes;

public sealed record AnimalNotesDumpOptions(
    bool AsJson = false,
    long? FarmerId = null,
    string? AnimalToken = null);
