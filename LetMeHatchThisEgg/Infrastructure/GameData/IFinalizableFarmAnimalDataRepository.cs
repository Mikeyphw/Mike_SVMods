namespace LetMeHatchThisEgg.Infrastructure.GameData;

/// <summary>Optional extension interface for repositories that can finalize edits (e.g., rebuild dictionary ordering).</summary>
public interface IFinalizableFarmAnimalDataRepository
{
    /// <summary>Finalize edits after all mutations have been applied.</summary>
    void FinalizeEdits();
}
