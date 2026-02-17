namespace WildEggsHatching.Application.Progression;

public interface IUnlockFactsProvider
{
    UnlockFacts GetFacts(ProgressionSaveData saveData);
}
