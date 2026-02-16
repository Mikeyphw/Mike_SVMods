using System;
using System.Collections.Generic;
using System.Linq;
using LetMeHatchThisEgg.GameAssetManagement;
using LetMeHatchThisEgg.Models;
using Mikew.BirdsOfAFeather.Framework;

public class LetMeHatchApi : ILetMeHatchApi
{
    public List<string> AnimalsWithClonedEggs { get; private set; } = new();
    public bool FinishedEarlyStuff { get; private set; } = false;

    public int EDIT_BIRTH_TEXT_PRIORITY { get; private set; }
    public int EDIT_EGG_LIST_PRIORITY { get; private set; }
    public int CLONED_EGGS_PRIORITY { get; private set; }
    public int NEW_EGGS_ADD_PRIORITY { get; private set; }

    private GameAssetManager? _gameAssetManager;

    public LetMeHatchApi()
    {
    }

    public void SetGameAssetManager(GameAssetManager gameAssetManager)
    {
        _gameAssetManager = gameAssetManager;
    }

    public void SetFinishedEarlyStuff(bool finished)
    {
        FinishedEarlyStuff = finished;
    }

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

        return _gameAssetManager.ClonedAnimalsInfoList
            .Select(info => (info.FemaleKey, info.MaleKey, info.AddAsAlternatePurchase))
            .ToList();
    }

    internal void SetAnimalsWithClonedEggs(IEnumerable<string> animalKeys)
    {
        // Used by internal managers; external mods should prefer GetClonedAnimalsInfo().
        AnimalsWithClonedEggs = animalKeys.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }
}