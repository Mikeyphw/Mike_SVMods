using System;
using System.Collections.Generic;
using WildEggsHatching.Api;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.Infrastructure.Config;

namespace WildEggsHatching.Infrastructure.Api;

public sealed class WildEggsApi : IWildEggsApi
{
    private readonly Func<ModConfig> _getConfig;

    private HashSet<string> _wildEggIds = new(StringComparer.OrdinalIgnoreCase);
    private HashSet<string> _validAnimals = new(StringComparer.OrdinalIgnoreCase);

    private string _birthTextMode = "Default";
    private string? _chosenCoop;
    private string? _chosenBarn;
    private string? _chosenMagicCoop;
    private string? _chosenMagicBarn;

    public WildEggsApi(Func<ModConfig> getConfig)
    {
        _getConfig = getConfig;
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

    public ProgressionStep GetProgressionStep()
    {
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
        return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    }
}
