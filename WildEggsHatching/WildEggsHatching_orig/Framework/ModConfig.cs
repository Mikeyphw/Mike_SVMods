using Mikew.BirdsOfAFeather.Framework;
using WildEggsHatching.Models;
using static WildEggsHatching.EnumsAndStrings.Enums;
namespace WildEggsHatching.Framework;

public class ModConfig : CoreModConfig
{
    public TextOptionValues.UpdateRate UpdateRate { get; set; }
    public bool WildEggEdit { get; set; }
    public Dictionary<string, float> ChosenWeights { get; set; }
    public bool ShowCoopAnimal { get; set; }
    public bool ShowBarnAnimal { get; set; }
    public bool ShowMagicCoopAnimal { get; set; }
    public bool ShowMagicBarnAnimal { get; set; }
    public bool ProgressionMode { get; set; }
    public Dictionary<string, bool> AnimalProgressionMode { get; set; }
    public TextOptionValues.BirthText BirthText { get; set; } = TextOptionValues.BirthText.Default;
    public bool SetRandom { get; set; }
    public bool SetZero { get; set; }
    public bool KeepMineLevel { get; set; }
    public bool FruitInsteadOfSeed { get; set; }
    public bool HatchDinoEggs { get; set; }
    public bool DinoEggsExclusiveHatch { get; set; }
    public bool NeedsFern { get; set; }
    public bool EnableSpiders { get; set; }
    public bool AlternatePotionSprite { get; set; }
    public bool ExtraDebugInfo { get; set; }
    public bool Testing { get; set; }

    public string DinoPrice { get; set; }
    public TextOptionValues.TombChance TombChance { get; set; }
    public string OstrichPrice { get; set; }
    public string VoidChickenPrice { get; set; }
    public TextOptionValues.TombDifficulty TombDifficulty { get; set; }

    public ModConfig()
    {
        AnimalProgressionMode = new();
        UpdateRate = TextOptionValues.UpdateRate.DayStart;
        /*ChosenWeights = EnumExtensions.GetValues<AnimalName>()
            .ToDictionary(x => x, x => 0.0f);*/
        ChosenWeights = new() { ["Chicken"] = 0.6f, ["Duck"] = 0.4f };
        ShowBarnAnimal = false;
        ShowCoopAnimal = false;
        ShowMagicBarnAnimal = false;
        ShowMagicCoopAnimal = false;
        WildEggEdit = false;
        SetRandom = false;
        SetZero = false;
        ProgressionMode = true;
        KeepMineLevel = false;
        FruitInsteadOfSeed = false;
        HatchDinoEggs = true;
        DinoEggsExclusiveHatch = true;
        AlternatePotionSprite = false;
        NeedsFern = true;
        EnableSpiders = true;
        ExtraDebugInfo = false;
        Testing = true;
        DinoPrice = "-1";
        OstrichPrice = "-1";
        VoidChickenPrice = "default";
        TombDifficulty = TextOptionValues.TombDifficulty.Moderate;
        TombChance = TextOptionValues.TombChance.Medium;
    }

    public void Reset()
    {
        UpdateRate = TextOptionValues.UpdateRate.DayStart;
        WildEggEdit = false;
        ChosenWeights = new() { ["Chicken"] = 0.6f, ["Duck"] = 0.4f };
        ShowBarnAnimal = false;
        ShowCoopAnimal = false;
        ShowMagicBarnAnimal = false;
        ShowMagicCoopAnimal = false;
        ProgressionMode = true;
        SetRandom = false;
        SetZero = false;
        KeepMineLevel = false;
        FruitInsteadOfSeed = false;
        HatchDinoEggs = true;
        DinoEggsExclusiveHatch = true;
        AlternatePotionSprite = false;
        NeedsFern = true;
        EnableSpiders = true;
        ExtraDebugInfo = false;
        Testing = true;
        DinoPrice = "-1";
        OstrichPrice = "-1";
        VoidChickenPrice = "default";
        TombDifficulty = TextOptionValues.TombDifficulty.Moderate;
        TombChance = TextOptionValues.TombChance.Medium;
        AnimalProgressionMode.Clear(); // Clear any existing entries
    }

    private int GetPurchasePrice(string priceString)
    {
        if (string.IsNullOrEmpty(priceString) || priceString == "-1")
        {
            return -1;
        }
        if (int.TryParse(priceString, out int price))
        {
            return price / 2;
        }
        else
        {
            ModEntry.MonitorInstance!.Log(
                $"Invalid value for price: {priceString}",
                StardewModdingAPI.LogLevel.Error
            );
            return -1;
        }
    }

    public int GetOstrichPurchasePrice() => GetPurchasePrice(OstrichPrice);

    public int GetVoidChickenPurchasePrice() => GetPurchasePrice(VoidChickenPrice);

    public int GetDinoPurchasePrice() => GetPurchasePrice(DinoPrice);

    internal void PopulateAnimalProgressionMode(HashSet<WildAnimalData> animals)
    {
        foreach (var animal in animals)
        {
            foreach (var key in animal.FarmAnimalKeys)
            {
                AnimalProgressionMode.TryAdd(key.Id, true);
            }
        }
        foreach (var entry in AnimalProgressionMode.Keys)
        {
            if (!animals.SelectMany(a => a.FarmAnimalKeys.Select(k => k.Id)).Contains(entry))
            {
                AnimalProgressionMode.Remove(entry);
            }
        }
    }
}
