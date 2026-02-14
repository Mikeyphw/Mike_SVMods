using Microsoft.Xna.Framework;
using Mikew.BirdsOfAFeather.Framework;
using static LetMeHatchThisEgg.EnumsAndStrings.Enums;

using static LetMeHatchThisEgg.ModEntry; namespace LetMeHatchThisEgg.Config;

public class ModConfig : CoreModConfig
{
    public bool EditEggListByName { get; set; }
    public bool EditBirthText { get; set; }
    public bool ShowEdits { get; set; }
    public bool AlternateBirthText { get; set; }
    public bool ShowAnimalKeys { get; set; }
    public bool CloneVanillaEggs { get; set; }
    public bool DinoEggAsArtifact { get; set; }
    public bool CloneMachineRules { get; set; }
    public bool ChangeEggColors { get; set; }
    public bool OnlyFertilizedEggs { get; set; }
    public bool ColorClonedMilk { get; set; }
    public bool BreedingSystemEnabled { get; set; }
    public Dictionary<
        string,
        Dictionary<CloneItemType, Dictionary<ItemType, string>>
    > ItemToClone {get; set;} = new();
    public bool LinkAnimalColorToEgg { get; set; }
    public float DailyLuckBonus { get; set; }
    public Dictionary<string, Dictionary<string, bool>> EggColors { get; set; } = new();
    public Dictionary<string, Color> EggColors_ColorObject { get; set; } = new();
    public Dictionary<string, bool> EditedEggListsOptions { get; set; } = new();
    public Dictionary<string, bool> EditedBirthTextOptions { get; set; } = new();
    public Dictionary<string, bool> ClonedVanillaEggsOptions { get; set; } = new();
    public Dictionary<ItemType, float> EggChances { get; set; } = new();
    internal Dictionary<EditType, Dictionary<string, bool>> EditedAnimals { get; set; } = new();
    public bool ExclusiveRabbitWool { get; internal set; }

    public ModConfig()
    {
        Reset();
    }

    public void Reset()
    {
        EditEggListByName = true;
        EditBirthText = true;
        ShowEdits = false;
        AlternateBirthText = false;
        ShowAnimalKeys = false;
        CloneVanillaEggs = true;
        DinoEggAsArtifact = false;
        CloneMachineRules = false;
        ChangeEggColors = false;
        LinkAnimalColorToEgg = true;
        OnlyFertilizedEggs = true;
        ColorClonedMilk = false;
        ExclusiveRabbitWool = true;
        EditedBirthTextOptions = new();
        EditedEggListsOptions = new();
        ClonedVanillaEggsOptions = new();
        BreedingSystemEnabled = true;
        EggChances = new Dictionary<ItemType, float>()
        {
            { ItemType.Normal, 0.2f },
            { ItemType.Large, 0.4f },
            { ItemType.FertilizedEgg, 0.6f },
        };
        EditedAnimals = new()
        {
            { EditType.BirthText, new() },
            { EditType.EggList, new() },
            { EditType.CloneEggs, new() },
        };
        EggColors = new();
        EggColors_ColorObject = new();
        DailyLuckBonus = 0.05f;
    }
}

public sealed class EditedOptions
{
    public Dictionary<string, bool> EditedEggListsOptions { get; set; } = new();
    public Dictionary<string, bool> EditedBirthTextOptions { get; set; } = new();
    public Dictionary<string, bool> ClonedVanillaEggsOptions { get; set; } = new();

    public EditedOptions() { }

    public Dictionary<string, bool> GetEditedOptions(EditType editType)
    {
        return editType switch
        {
            EditType.EggList => EditedEggListsOptions,
            EditType.BirthText => EditedBirthTextOptions,
            EditType.CloneEggs => ClonedVanillaEggsOptions,
            _ => throw new ArgumentException("Invalid editType", nameof(editType)),
        };
    }

    public void SetEditedOption(EditType editType, string key, bool value)
    {
        switch (editType)
        {
            case EditType.EggList:

                EditedEggListsOptions[key] = value;

                break;

            case EditType.BirthText:

                EditedBirthTextOptions[key] = value;

                break;

            case EditType.CloneEggs:

                ClonedVanillaEggsOptions[key] = value;

                break;

            default:

                throw new ArgumentException("Invalid editType", nameof(editType));
        }
    }
}
