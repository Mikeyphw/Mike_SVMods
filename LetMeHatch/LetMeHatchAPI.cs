using System.Collections.Generic;
using System.Linq;
using LetMeHatch.GameAssetManagement;
using Mikew.NestCore.Framework;

namespace LetMeHatch;

/// <summary>
/// Public API object returned by SMAPI <c>GetApi()</c>.
/// Must implement NestCore's <see cref="ILetMeHatchApi"/> so other NestCore-first mods can consume it safely.
/// </summary>
public sealed class LetMeHatchApi : ILetMeHatchApi
{
    public int ApiVersion => 1;

    public List<string> AnimalsWithClonedEggs { get; private set; } = new();

    public bool FinishedEarlyStuff { get; private set; }

    public int EDIT_BIRTH_TEXT_PRIORITY { get; private set; }
    public int EDIT_EGG_LIST_PRIORITY { get; private set; }
    public int CLONED_EGGS_PRIORITY { get; private set; }
    public int NEW_EGGS_ADD_PRIORITY { get; private set; }

    private GameAssetManager? _gameAssetManager;

    public LetMeHatchApi() { }

    public void SetGameAssetManager(GameAssetManager gameAssetManager) => _gameAssetManager = gameAssetManager;

    public void SetFinishedEarlyStuff(bool finished) => FinishedEarlyStuff = finished;

    public void SetPriorities(int birthText, int eggList, int clonedEggs, int newEggsAdd)
    {
        EDIT_BIRTH_TEXT_PRIORITY = birthText;
        EDIT_EGG_LIST_PRIORITY = eggList;
        CLONED_EGGS_PRIORITY = clonedEggs;
        NEW_EGGS_ADD_PRIORITY = newEggsAdd;
    }

    public List<(string FemaleKey, string MaleKey, bool AddAsAlternatePurchase)> GetClonedAnimalsInfo()
    {
        if (_gameAssetManager is null)
            return new();

        var list = _gameAssetManager.ClonedAnimalsInfoList
            .Select(info => (info.FemaleKey, info.MaleKey, info.AddAsAlternatePurchase))
            .ToList();

        AnimalsWithClonedEggs = list.Select(x => x.FemaleKey).Distinct().ToList();
        return list;
    }
}
