using System.Collections.Generic;

namespace WildEggsHatching.Application.AnimalNotes;

public sealed class AnimalNotesSaveData
{
    public Dictionary<string, List<long>> AnimalsOnTheFarm { get; set; } = new();
    public Dictionary<string, List<long>> AnimalsOnTheFarmYesterday { get; set; } = new();

    /// <summary>Animal types already fully processed (we won't track more hatches for these).</summary>
    public HashSet<string> RegisteredAnimalTypes { get; set; } = new();

    /// <summary>Pending hatched animals (one per animal type) that still need note/quest/mail handling.</summary>
    public List<HatchedAnimalRecord> Pending { get; set; } = new();
}
