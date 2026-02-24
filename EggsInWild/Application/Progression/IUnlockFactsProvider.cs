namespace EggsInWild.Application.Progression;

public interface IUnlockFactsProvider
{
    UnlockFacts GetFacts(ProgressionSaveData saveData);
}
