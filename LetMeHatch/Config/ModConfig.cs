using Microsoft.Xna.Framework;
using Mikew.NestCore.Framework;
using Mikew.NestCore.Helpers.Logging;
using static LetMeHatch.EnumsAndStrings.Enums;

using static LetMeHatch.ModEntry;

namespace LetMeHatch.Config;

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
    public bool UseDynamicCloneTextures { get; set; }
    public bool ChangeEggColors { get; set; }
    public bool OnlyFertilizedEggs { get; set; }
    public bool ColorClonedMilk { get; set; }
    public bool BreedingSystemEnabled { get; set; }

    public bool DeterministicBreedingRng { get; set; }

    public bool StabilizeFarmAnimalsAssetOrder { get; set; }
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

    // Legacy GMCM toggles mapped to scoped logging levels.
    public bool VerboseLogging
    {
        get => ScopedLogging?.LTHTE?.AllLevel is ScopedLogCatalog.LevelAll or ScopedLogCatalog.LevelDebug or ScopedLogCatalog.LevelTrace;
        set => EnsureLthteLogging().AllLevel = value ? ScopedLogCatalog.LevelDebug : ScopedLogCatalog.LevelInfo;
    }

    public bool TestingLogging
    {
        get => string.Equals(ScopedLogging?.LTHTE?.AllLevel, ScopedLogCatalog.LevelTrace, StringComparison.OrdinalIgnoreCase);
        set
        {
            var logging = EnsureLthteLogging();
            if (value)
            {
                logging.AllLevel = ScopedLogCatalog.LevelTrace;
                return;
            }

            if (string.Equals(logging.AllLevel, ScopedLogCatalog.LevelTrace, StringComparison.OrdinalIgnoreCase))
                logging.AllLevel = ScopedLogCatalog.LevelInfo;
        }
    }

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
        DeterministicBreedingRng = false;
        StabilizeFarmAnimalsAssetOrder = false;
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

    private ModScopedLoggingConfig EnsureLthteLogging()
    {
        ScopedLogging ??= ScopedLoggingConfig.CreateDefault();
        ScopedLogging.LTHTE ??= ModScopedLoggingConfig.CreateDefault(ScopedLogCatalog.ModLthte);
        return ScopedLogging.LTHTE;
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
