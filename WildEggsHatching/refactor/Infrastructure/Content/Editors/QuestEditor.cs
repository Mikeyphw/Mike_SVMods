using System.Collections.Generic;
using StardewModdingAPI.Events;
using StardewValley.GameData.Quests;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.Infrastructure.Content;

namespace WildEggsHatching.Infrastructure.Content.Editors;

/// <summary>
/// Editor for Data/Quests asset - adds progression-related quests.
/// Refactored to use WildEggConstants.
/// </summary>
public sealed class QuestEditor : ProgressionAwareAssetEditor
{
    /// <inheritdoc />
    public override string AssetName => GameAssets.Quests;

    /// <inheritdoc />
    public QuestEditor() : base() { }

    /// <inheritdoc />
    public QuestEditor(
        PhaseProgressionService progressionService,
        IConfigProvider configProvider,
        IMonitor monitor) 
        : base(progressionService, configProvider, monitor) { }

    /// <inheritdoc />
    public override void Edit<T>(IAssetData<T> asset, string assetName)
    {
        if (assetName != GameAssets.Quests)
            return;

        var quests = asset.AsDictionary<string, QuestData>().Data;

        // Part I: Special Order I (available from start)
        if (HasReachedStep(ProgressionStep.PartI))
        {
            AddQuest(quests, WildEggConstants.QuestIds.SpecialOrderWildEggsI, CreateSpecialOrderI());
        }

        // Part III: Wizard Study
        if (HasReachedStep(ProgressionStep.PartIII))
        {
            AddQuest(quests, WildEggConstants.QuestIds.WizardStudy, CreateWizardStudy());
        }

        // Part IV: Special Order II
        if (HasReachedStep(ProgressionStep.PartIV))
        {
            AddQuest(quests, WildEggConstants.QuestIds.SpecialOrderWildEggsII, CreateSpecialOrderII());
        }

        // Part V: The Desert Scroll
        if (HasReachedStep(ProgressionStep.PartV))
        {
            AddQuest(quests, WildEggConstants.QuestIds.TheDesertScroll, CreateDesertScroll());
        }

        Log($"QuestEditor applied edits to {quests.Count} quests");
    }

    /// <summary>
    /// Safely adds a quest.
    /// </summary>
    private void AddQuest(IDictionary<string, QuestData> quests, string id, QuestData data)
    {
        if (!quests.ContainsKey(id))
        {
            quests[id] = data;
            Log($"Added quest: {id}", LogLevel.Trace);
        }
    }

    #region Quest Data Factory Methods

    private static QuestData CreateSpecialOrderI() => new()
    {
        Id = WildEggConstants.QuestIds.SpecialOrderWildEggsI,
        Type = WildEggConstants.QuestTypes.SpecialOrder,
        DisplayName = "Wild Egg Research: Phase I",
        Description = "Help the Wizard study wild eggs by collecting samples.",
        Objective = "Deliver 5 wild eggs to the Wizard",
        IsRepeatable = false,
        MoneyReward = 1000,
        GoldReward = 1000
    };

    private static QuestData CreateWizardStudy() => new()
    {
        Id = WildEggConstants.QuestIds.WizardStudy,
        Type = WildEggConstants.QuestTypes.Collect,
        DisplayName = "The Wizard's Study",
        Description = "The Wizard wants to study the wild eggs more closely.",
        Objective = "Bring samples to the Wizard's tower",
        IsRepeatable = false,
        MoneyReward = 500,
        GoldReward = 500
    };

    private static QuestData CreateSpecialOrderII() => new()
    {
        Id = WildEggConstants.QuestIds.SpecialOrderWildEggsII,
        Type = WildEggConstants.QuestTypes.SpecialOrder,
        DisplayName = "Wild Egg Research: Phase II",
        Description = "Continue your research into wild eggs with advanced analysis.",
        Objective = "Deliver rare materials for the Wizard's research",
        IsRepeatable = false,
        MoneyReward = 2000,
        GoldReward = 2000
    };

    private static QuestData CreateDesertScroll() => new()
    {
        Id = WildEggConstants.QuestIds.TheDesertScroll,
        Type = WildEggConstants.QuestTypes.Artifact,
        DisplayName = "The Desert Scroll",
        Description = "Seek the ancient scroll in the desert tomb.",
        Objective = "Find the mysterious scroll in the desert",
        IsRepeatable = false,
        MoneyReward = 3000,
        GoldReward = 3000
    };

    #endregion

    /// <inheritdoc />
    public override bool ShouldApply() => true;
}
