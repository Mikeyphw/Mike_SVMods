using System.Collections.Generic;
using System.Linq;
using LetMeHatch.Models;
using Mikew.NestCore.Helpers;
using StardewValley.Extensions;
using StardewValley.GameData.Machines;
using static LetMeHatch.EnumsAndStrings.Enums;
using static LetMeHatch.ModEntry;

namespace LetMeHatch.GameAssetManagement;

/// <summary>
/// Clones relevant machine rules/triggers for cloned items.
/// </summary>
public sealed class MachineEditor
{
    internal readonly List<SavedMachineRuleInfo> MachineRules = new();

    private readonly List<OutputItemCloneInfo> _outputItemsToClone;
    private readonly Dictionary<string, Dictionary<TextureInfoType, ItemTextureInfo>> _clonedItemsTextureInfo;

    public MachineEditor(
        Dictionary<string, Dictionary<TextureInfoType, ItemTextureInfo>> clonedItemsTextureInfo,
        List<OutputItemCloneInfo> outputItemsToClone
    )
    {
        _clonedItemsTextureInfo = clonedItemsTextureInfo;
        _outputItemsToClone = outputItemsToClone;
    }

    public void CloneMachineData(Dictionary<string, MachineData> data)
    {
        if (MachineRules.Count < 1)
        {
            SetUpMachineRules(data);
            LoggerHelper.VerboseLog("Machine rules were set up as none were present.");
        }

        var modifiedMachines = new Dictionary<string, MachineData>();
        LoggerHelper.VerboseLog($"Starting to clone machine data. Total MachineRules: {MachineRules.Count}");

        foreach (var machineRule in MachineRules)
        {
            if (!data.TryGetValue(machineRule.MachineKey, out var machineData))
            {
                continue;
            }

            var outputRule = machineData.OutputRules?.FirstOrDefault(r => r.Id == machineRule.OutputRuleId);
            if (outputRule == null)
            {
                continue;
            }

            if (outputRule.OutputItem == null || outputRule.OutputItem.Count < 1)
            {
                continue;
            }

            var outputItemRule = outputRule.OutputItem.FirstOrDefault(i => i.Id == machineRule.OutputItemRuleID);
            if (outputItemRule == null)
            {
                continue;
            }

            if (outputItemRule.ItemId == null)
            {
                continue;
            }

            var unqualifiedItemId = GetUnqualifiedItemId(outputItemRule.ItemId);

            foreach (var outputItemToClone in _outputItemsToClone.Where(o => o.InputItemID == unqualifiedItemId))
            {
                var clonedItemId = outputItemToClone.InputItemClonedID;
                var clonedOutputRule = outputRule.CloneOrDeepClone(true);
                if (clonedOutputRule == null)
                {
                    continue;
                }

                clonedOutputRule.Id = $"{outputRule.Id}_{clonedItemId}";
                clonedOutputRule.OutputItem[0].Id = $"{outputItemRule.Id}_{clonedItemId}";
                clonedOutputRule.OutputItem[0].ItemId = $"(O){clonedItemId}";

                if (machineRule.Triggers?.Count > 0)
                {
                    var triggers = CloneTriggers(machineRule.Triggers);
                    clonedOutputRule.Triggers ??= new List<MachineOutputTriggerRule>();
                    clonedOutputRule.Triggers.AddRange(triggers);
                }

                modifiedMachines[machineRule.MachineKey] = machineData;
                machineData.OutputRules ??= new List<MachineOutputRule>();
                machineData.OutputRules.Add(clonedOutputRule);
            }
        }

        LoggerHelper.VerboseLog($"Completed cloning machine data. Modified machines: {modifiedMachines.Count}");
    }

    private void SetUpMachineRules(Dictionary<string, MachineData> data)
    {
        MachineRules.Clear();

        foreach (var (machineKey, machineData) in data)
        {
            if (machineData.OutputRules == null)
            {
                continue;
            }

            foreach (var rule in machineData.OutputRules)
            {
                if (rule.OutputItem == null || rule.OutputItem.Count < 1)
                {
                    continue;
                }

                var triggers = rule.Triggers ?? new List<MachineOutputTriggerRule>();
                AddMachineRule(machineKey, rule, triggers);
            }
        }
    }

    private List<MachineOutputTriggerRule> CloneTriggers(List<MachineOutputTriggerRule> triggers)
    {
        var clonedTriggers = new List<MachineOutputTriggerRule>();

        foreach (var trigger in triggers)
        {
            if (trigger.RequiredItemId == null)
            {
                continue;
            }

            var requiredUnqualified = GetUnqualifiedItemId(trigger.RequiredItemId);
            foreach (var clonedId in _clonedItemsTextureInfo.Values
                         .Where(x => x[TextureInfoType.Original].ID == requiredUnqualified)
                         .Select(y => y[TextureInfoType.Cloned].ID))
            {
                var clonedTrigger = trigger.CloneOrDeepClone(true);
                if (clonedTrigger == null)
                {
                    continue;
                }

                clonedTrigger.RequiredItemId = $"{clonedId}";
                clonedTrigger.Id = $"{clonedId}";
                clonedTriggers.Add(clonedTrigger);
            }
        }

        return clonedTriggers;
    }

    private void AddMachineRule(string machineKey, MachineOutputRule rule, List<MachineOutputTriggerRule> triggers)
    {
        var outputItemRule = rule.OutputItem[0];
        var outputItemRuleId = outputItemRule.Id;
        var outputItemId = outputItemRule.ItemId != null ? GetUnqualifiedItemId(outputItemRule.ItemId) : null;

        MachineRules.Add(new SavedMachineRuleInfo(machineKey, rule.Id, triggers, outputItemRuleId, outputItemId));
    }

    private static string GetUnqualifiedItemId(string qualifiedItemId) => qualifiedItemId.Replace("(O)", "");
}
