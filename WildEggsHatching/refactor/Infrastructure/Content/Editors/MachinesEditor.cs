using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.GameData.Machines;
using StardewValley;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.Infrastructure.Content;

namespace WildEggsHatching.Infrastructure.Content.Editors;

/// <summary>
/// Editor for Data/MachineOutputData asset - adds machine output rules for wild eggs.
/// Refactored to use factory methods and support all missing methods.
/// </summary>
public sealed class MachinesEditor : ProgressionAwareAssetEditor
{
    /// <inheritdoc />
    public override string AssetName => GameAssets.MachineOutput;

    /// <summary>
    /// The key for the wild egg incubator in machine data.
    /// </summary>
    private readonly string _wildEggIncubatorKey;

    /// <summary>
    /// Creates a new machines editor.
    /// </summary>
    public MachinesEditor() : base() 
    {
        _wildEggIncubatorKey = $"(BC){WildEggConstants.Items.WildEggIncubator}";
    }

    /// <summary>
    /// Creates a new machines editor with dependencies.
    /// </summary>
    public MachinesEditor(
        PhaseProgressionService progressionService,
        IConfigProvider configProvider,
        IMonitor monitor,
        IModHelper modHelper) 
        : base(progressionService, configProvider, monitor, modHelper) 
    {
        _wildEggIncubatorKey = $"(BC){WildEggConstants.Items.WildEggIncubator}";
    }

    /// <inheritdoc />
    public override void Edit<T>(IAssetData<T> asset, string assetName)
    {
        if (assetName != GameAssets.MachineOutput)
            return;

        var machines = asset.AsDictionary<string, MachineData>().Data;

        // Part I: Add wild egg incubator rules
        if (HasReachedStep(ProgressionStep.PartI))
        {
            AddWildEggIncubatorRules(machines);
        }

        // Modify existing incubators to work with wild eggs
        EditOtherIncubators(machines);

        Log($"MachinesEditor applied edits to {machines.Count} machines");
    }

    /// <summary>
    /// Adds the wild egg incubator machine rules.
    /// </summary>
    private void AddWildEggIncubatorRules(IDictionary<string, MachineData> machines)
    {
        if (machines.ContainsKey(_wildEggIncubatorKey))
            return;

        var config = GetConfig();

        // Build consumed items
        var consumedItems = new List<MachineItemAdditionalConsumedItems>();
        if (config.NeedsFern)
        {
            consumedItems.Add(CreateConsumedItem("259", 5, "It needs 5 ferns."));
        }

        // Build output rules (would need egg IDs - typically added at runtime)
        var outputRules = new List<MachineOutputRule>();

        // Create machine data using factory method
        var incubatorData = CreateMachineData(
            outputRules: outputRules,
            consumedItems: consumedItems.Count > 0 ? consumedItems : null,
            invalidItemMessage: "Only work with wild eggs.",
            invalidItemMessageCondition: "ITEM_CATEGORY Input -5",
            isIncubator: true,
            hasInput: false,
            hasOutput: false,
            workingEffectChance: 0.33f,
            loadEffects: CreateLoadEffects(),
            workingEffects: CreateWorkingEffects(
                animationTexture: "TileSheets/animations",
                animationSourceRect: new Rectangle(0, 704, 64, 64)
            )
        );

        machines[_wildEggIncubatorKey] = incubatorData;
        Log($"Added wild egg incubator machine rules", LogLevel.Trace);
    }

    /// <summary>
    /// Modifies existing incubators to reject wild eggs.
    /// </summary>
    public void EditOtherIncubators(IDictionary<string, MachineData> machines)
    {
        var config = GetConfig();

        // Incubator (ID 101)
        if (machines.TryGetValue("(BC)101", out var incubator))
        {
            ModifyIncubatorToRejectWildEggs(incubator, config.DinoEggsExclusiveHatch);
        }

        // Ostrich Incubator (ID 254)
        if (machines.TryGetValue("(BC)254", out var ostrichIncubator))
        {
            ModifyIncubatorToRejectWildEggs(ostrichIncubator, config.DinoEggsExclusiveHatch);
        }

        // Find any other incubators by name
        foreach (var (key, machineData) in machines)
        {
            if (key.ContainsIgnoreCase("incubator") && key != _wildEggIncubatorKey)
            {
                ModifyIncubatorToRejectWildEggs(machineData, config.DinoEggsExclusiveHatch);
            }
        }

        Log("Modified existing incubators to reject wild eggs", LogLevel.Trace);
    }

    /// <summary>
    /// Modifies an incubator to reject wild eggs.
    /// </summary>
    private void ModifyIncubatorToRejectWildEggs(MachineData machine, bool dinoEggsExclusive)
    {
        // Add required tags to reject wild eggs
        foreach (var outputRule in machine.OutputRules ?? Enumerable.Empty<MachineOutputRule>())
        {
            foreach (var trigger in outputRule.Triggers ?? Enumerable.Empty<MachineOutputTriggerRule>())
            {
                trigger.RequiredTags ??= new List<string>();
                trigger.RequiredTags.Add("!wildegg_item");
                
                if (dinoEggsExclusive)
                {
                    trigger.RequiredTags.Add("!item_dinosaur_egg");
                }
            }
        }

        // Set invalid item message
        machine.InvalidItemMessageCondition = "ITEM_CONTEXT_TAG Input wildegg_item";
        machine.InvalidItemMessage = "You can't hatch wild eggs here.";
    }

    /// <summary>
    /// Edits wild egg incubator rules based on runtime config.
    /// Call this when config changes.
    /// </summary>
    public void EditWildEggIncubatorRules(IDictionary<string, MachineData> machines)
    {
        var config = GetConfig();

        if (!machines.TryGetValue(_wildEggIncubatorKey, out var incubator))
            return;

        // Handle dino egg config
        if (config.HatchDinoEggs)
        {
            // Check if rule already exists
            bool hasDinoRule = incubator.OutputRules.Any(r => r.Id == "DinoEgg");
            
            if (!hasDinoRule)
            {
                incubator.OutputRules.Add(CreateDinoEggOutputRule());
                Log("Added dinosaur egg output rule to incubator", LogLevel.Trace);
            }
        }

        // Handle fern config
        if (!config.NeedsFern)
        {
            incubator.AdditionalConsumedItems = null;
            Log("Removed fern requirement from incubator", LogLevel.Trace);
        }
    }

    /// <summary>
    /// Creates a dinosaur egg output rule.
    /// </summary>
    private MachineOutputRule CreateDinoEggOutputRule()
    {
        return new MachineOutputRule
        {
            Id = "DinoEgg",
            Triggers = new List<MachineOutputTriggerRule>
            {
                new()
                {
                    Id = "ItemPlacedInMachineDino",
                    Trigger = MachineOutputTrigger.ItemPlacedInMachine,
                    RequiredItemId = "107" // Dinosaur Egg
                }
            },
            UseFirstValidOutput = true,
            OutputItem = new List<MachineItemOutput>
            {
                new()
                {
                    OutputMethod = "StardewValley.Object, Stardew Valley: OutputIncubator",
                    IncrementMachineParentSheetIndex = 2
                }
            }
        };
    }

    /// <summary>
    /// Adds machine output rules for eggs (for CP mod compatibility).
    /// Adds wild egg processing to Cheese Press and Mayonnaise Machine.
    /// </summary>
    public void AddMachineOutputRulesForEggs(IDictionary<string, MachineData> machines)
    {
        // Cheese Press (ID 15)
        if (machines.TryGetValue("(BC)15", out var cheesePress))
        {
            AddEggProcessingRule(cheesePress, "wildegg_item", "yourdorkbrains.cp_PickledWildEggs", 1, 200);
        }

        // Mayonnaise Machine (ID 24)
        if (machines.TryGetValue("(BC)24", out var mayonnaiseMachine))
        {
            AddEggProcessingRule(mayonnaiseMachine, "wildegg_item", "yourdorkbrains.cp_WildEggMayo", 1, 200);
        }

        Log("Added machine output rules for eggs", LogLevel.Trace);
    }

    /// <summary>
    /// Adds an egg processing rule to a machine.
    /// </summary>
    private void AddEggProcessingRule(
        MachineData machine,
        string contextTag,
        string outputItemId,
        int quantity,
        int minutesUntilReady)
    {
        var outputRule = new MachineOutputRule
        {
            Id = $"{WildEggConstants.ModId}_{contextTag}",
            OutputItem = new List<MachineItemOutput>
            {
                new()
                {
                    CopyQuality = true,
                    Id = outputItemId,
                    ItemId = outputItemId,
                    MinStack = quantity,
                    MaxStack = quantity
                }
            },
            MinutesUntilReady = minutesUntilReady,
            Triggers = new List<MachineOutputTriggerRule>
            {
                new()
                {
                    Id = contextTag,
                    Trigger = MachineOutputTrigger.ItemPlacedInMachine,
                    RequiredCount = 1,
                    RequiredTags = new List<string> { contextTag }
                }
            }
        };

        machine.OutputRules ??= new List<MachineOutputRule>();
        machine.OutputRules.Add(outputRule);
    }

    /// <summary>
    /// Adds output rules for wild eggs from machines.
    /// This is called to add specific egg output rules.
    /// </summary>
    public void AddWildEggOutputRule(IDictionary<string, MachineData> machines, string eggId, bool isLarge)
    {
        if (!machines.TryGetValue(_wildEggIncubatorKey, out var incubator))
            return;

        // Check if rule already exists
        if (incubator.OutputRules.Any(r => r.Id == eggId))
            return;

        incubator.OutputRules.Add(CreateIncubatorOutputRule(eggId, isLarge));
        Log($"Added output rule for {eggId}", LogLevel.Trace);
    }

    /// <inheritdoc />
    public override bool ShouldApply()
    {
        return true;
    }
}
