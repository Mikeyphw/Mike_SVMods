// using LetMeHatchThisEgg.Models;

// public class LetMeHatchApi
// {
//     private static LetMeHatchApi _instance;

//     public static LetMeHatchApi Instance
//     {
//         get { return _instance ??= new LetMeHatchApi(); }
//     }

//     public List<string> AnimalsWithClonedEggs { get; private set; } = new();
//     public bool CloneEggs { get; private set; } = false;
//     public int EDIT_BIRTH_TEXT_PRIORITY { get; private set; }
//     public int EDIT_EGG_LIST_PRIORITY { get; private set; }
//     public int CLONED_EGGS_PRIORITY { get; private set; }
//     public int NEW_EGGS_ADD_PRIORITY { get; private set; }
//     public List<(string FemaleKey, string MaleKey, bool AddAsAlternatePurchase)> ClonedAnimalsInfo {get; private set;} = new();

//     public void SetClonedAnimalsInfo(List<ClonedAnimalInfo> clonedAnimalInfos)
//     {
//         ClonedAnimalsInfo = clonedAnimalInfos
//             .Select(info => (info.FemaleKey, info.MaleKey, info.AddAsAlternatePurchase))
//             .ToList();
//     }

//     public void SetPriorities(int birthText, int eggList, int clonedEggs, int newEggsAdd)
//     {
//         EDIT_BIRTH_TEXT_PRIORITY = birthText;
//         EDIT_EGG_LIST_PRIORITY = eggList;
//         CLONED_EGGS_PRIORITY = clonedEggs;
//         NEW_EGGS_ADD_PRIORITY = newEggsAdd;
//     }

//     public void SetCloneEggs(bool cloneEggs)
//     {
//         CloneEggs = cloneEggs;
//     }

//     private LetMeHatchApi() { }
// }
using LetMeHatchThisEgg.GameAssetManagement;
using LetMeHatchThisEgg.Models;

public class LetMeHatchApi
{
    // Public properties
    public List<string> AnimalsWithClonedEggs { get; private set; } = new();
    public bool FinishedEarlyStuff { get; private set; } = false;
    public int EDIT_BIRTH_TEXT_PRIORITY { get; private set; }
    public int EDIT_EGG_LIST_PRIORITY { get; private set; }
    public int CLONED_EGGS_PRIORITY { get; private set; }
    public int NEW_EGGS_ADD_PRIORITY { get; private set; }
    private GameAssetManager? _gameAssetManager;

    // Public constructor to allow instance creation
    public LetMeHatchApi()
    {
    }
    public void SetGameAssetManager(GameAssetManager gameAssetManager){
        _gameAssetManager = gameAssetManager;
    }

    public List<(
        string FemaleKey,
        string MaleKey,
        bool AddAsAlternatePurchase
    )> GetClonedAnimalsInfo()
    {
        if (_gameAssetManager != null)
        {
            return _gameAssetManager
                .ClonedAnimalsInfoList.Select(info =>
                    (info.FemaleKey, info.MaleKey, info.AddAsAlternatePurchase)
                )
                .ToList();
        }
        else
        {
            return new();
        }
    }
    public void SetFinishedEarlyStuff(bool finished){
        FinishedEarlyStuff = finished;
    }

    public void SetPriorities(int birthText, int eggList, int clonedEggs, int newEggsAdd)
    {
        EDIT_BIRTH_TEXT_PRIORITY = birthText;
        EDIT_EGG_LIST_PRIORITY = eggList;
        CLONED_EGGS_PRIORITY = clonedEggs;
        NEW_EGGS_ADD_PRIORITY = newEggsAdd;
    }


}
