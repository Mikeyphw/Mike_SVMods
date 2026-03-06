using System;
using System.Collections.Generic;
using EggsInWild.Api;
using EggsInWild.Core.Enums;
using EggsInWild.Infrastructure.Config;

namespace EggsInWild.Infrastructure.Api;

public sealed class WildEggsApi : IWildEggsApi
{
    private readonly Func<ModConfig> _getConfig;
    private readonly Func<ProgressionStep>? _getStep;
    private readonly Func<IReadOnlyDictionary<string, int>>? _getQuestCompletionDays;

    private HashSet<string> _wildEggIds = new(StringComparer.OrdinalIgnoreCase);
    private HashSet<string> _validAnimals = new(StringComparer.OrdinalIgnoreCase);

    private string _birthTextMode = "Default";
    private string? _chosenCoop;
    private string? _chosenBarn;
    private string? _chosenMagicCoop;
    private string? _chosenMagicBarn;

    public WildEggsApi(
        Func<ModConfig> getConfig,
        Func<ProgressionStep>? getStep = null,
        Func<IReadOnlyDictionary<string, int>>? getQuestCompletionDays = null)
    {
        _getConfig = getConfig;
        _getStep = getStep;
        _getQuestCompletionDays = getQuestCompletionDays;
    }

    public int ApiVersion => 1;

    public HashSet<string> ValidAnimals => new(_validAnimals, StringComparer.OrdinalIgnoreCase);

    public void SetValidAnimals(HashSet<string> animals)
    {
        _validAnimals = new HashSet<string>(animals ?? new HashSet<string>(), StringComparer.OrdinalIgnoreCase);
    }

    public HashSet<string> WildEggsIDs => new(_wildEggIds, StringComparer.OrdinalIgnoreCase);

    public void SetWildEggsIDs(HashSet<string> ids)
    {
        _wildEggIds = new HashSet<string>(ids ?? new HashSet<string>(), StringComparer.OrdinalIgnoreCase);
    }

    public string BirthTextMode => _birthTextMode;

    public void SetBirthTextMode(string mode)
    {
        _birthTextMode = string.IsNullOrWhiteSpace(mode) ? "Default" : mode;
    }

    public string? ChosenAnimal_Coop => _chosenCoop;
    public string? ChosenAnimal_Barn => _chosenBarn;
    public string? ChosenAnimal_MagicCoop => _chosenMagicCoop;
    public string? ChosenAnimal_MagicBarn => _chosenMagicBarn;

    public void SetChosenAnimal(string chosenAnimal, string home, bool isMagic)
    {
        if (string.IsNullOrWhiteSpace(chosenAnimal) || string.IsNullOrWhiteSpace(home))
            return;

        var isCoop = home.Equals("Coop", StringComparison.OrdinalIgnoreCase);

        if (isMagic)
        {
            if (isCoop)
                _chosenMagicCoop = chosenAnimal;
            else
                _chosenMagicBarn = chosenAnimal;

            return;
        }

        if (isCoop)
            _chosenCoop = chosenAnimal;
        else
            _chosenBarn = chosenAnimal;
    }

    public void ClearChosenAnimals()
    {
        _chosenCoop = null;
        _chosenBarn = null;
        _chosenMagicCoop = null;
        _chosenMagicBarn = null;
    }

    public ProgressionStep GetProgressionStep()
    {
        if (_getStep is not null)
            return _getStep();

        return _getConfig().ProgressionMode ? ProgressionStep.PartI : ProgressionStep.None;
    }

    public bool IsProgressionModeEnabled()
    {
        return _getConfig().ProgressionMode;
    }

    public bool IsDesertTombModuleEnabled()
    {
        return _getConfig().DesertTombModuleEnabled;
    }

    public IReadOnlyDictionary<string, int> GetQuestCompletionDays()
    {
        return _getQuestCompletionDays is null
            ? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, int>(_getQuestCompletionDays(), StringComparer.OrdinalIgnoreCase);
    }
}
