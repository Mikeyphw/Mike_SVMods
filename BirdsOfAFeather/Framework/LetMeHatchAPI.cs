namespace Mikew.BirdsOfAFeather.Framework;

public interface ILetMeHatchApi
{
    List<string> AnimalsWithClonedEggs { get; }
    bool FinishedEarlyStuff { get; }
    int EDIT_BIRTH_TEXT_PRIORITY { get; }
    int EDIT_EGG_LIST_PRIORITY { get; }
    int CLONED_EGGS_PRIORITY { get; }
    int NEW_EGGS_ADD_PRIORITY { get; }
    public List<(
        string FemaleKey,
        string MaleKey,
        bool AddAsAlternatePurchase
    )> GetClonedAnimalsInfo();
}
