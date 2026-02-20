namespace Mikew.BirdsOfAFeather.Framework;

public interface IWildEggsApi
{
    int ApiVersion { get; }

    /// <summary>Farm animal IDs considered valid for WildEggs-related birth text behavior.</summary>
    HashSet<string> ValidAnimals { get; }

    /// <summary>Object IDs for eggs treated as "wild eggs" by WildEggsHatching.</summary>
    HashSet<string> WildEggsIDs { get; }

    /// <summary>Birth text mode string used by downstream mods (e.g. "Mysterious").</summary>
    string BirthTextMode { get; }

    string? ChosenAnimal_Coop { get; }
    string? ChosenAnimal_Barn { get; }
    string? ChosenAnimal_MagicCoop { get; }
    string? ChosenAnimal_MagicBarn { get; }

    void SetValidAnimals(HashSet<string> animals);

    void SetWildEggsIDs(HashSet<string> ids);

    void SetBirthTextMode(string mode);

    void SetChosenAnimal(string chosenAnimal, string home, bool isMagic);
}
