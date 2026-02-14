using static Mikew.BirdsOfAFeather.EnumsAndStrings.Strings;

public class WildEggsApi
{
    private static WildEggsApi _instance;

    public static WildEggsApi Instance
    {
        get { return _instance ??= new WildEggsApi(); }
    }

    public HashSet<string> ValidAnimals { get; private set; } = new();
    public string ChosenAnimal_Coop { get; private set; } = "";
    public string ChosenAnimal_Barn { get; private set; } = "";
    public string ChosenAnimal_CoopMagic { get; private set; } = "";
    public string ChosenAnimal_BarnMagic { get; private set; } = "";
    public string BirthTextMode { get; private set; } = "";
    public HashSet<string> WildEggsIDs { get; private set; } = new();

    public void SetValidAnimals(HashSet<string> validAnimals)
    {
        ValidAnimals = validAnimals;
    }

    public void SetWildEggsIDs(HashSet<string> wildEggsIDs)
    {
        WildEggsIDs = wildEggsIDs;
    }

    public void SetChosenAnimal(string chosenAnimal, string home, bool isMagic = false)
    {
        switch (home)
        {
            case VanillaBuildings.Coop:
                if (isMagic)
                {
                    ChosenAnimal_CoopMagic = chosenAnimal;
                }
                else
                {
                    ChosenAnimal_Coop = chosenAnimal;
                }
                return;
            case VanillaBuildings.Barn:
                if (isMagic)
                {
                    ChosenAnimal_BarnMagic = chosenAnimal;
                }
                else
                {
                    ChosenAnimal_Barn = chosenAnimal;
                }
                return;
            default:
                return;
        }
    }
    public void SetBirthTextMode(string chosenMode)
    {
        BirthTextMode = chosenMode;
    }

    private WildEggsApi() { }
}
