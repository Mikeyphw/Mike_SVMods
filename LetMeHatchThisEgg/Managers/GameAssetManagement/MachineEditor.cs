// using LetMeHatchThisEgg.Models;
// using StardewModdingAPI;
// using Mikew.BirdsOfAFeather.Helpers;
// using StardewValley.GameData.Machines;
// using static LetMeHatchThisEgg.EnumsAndStrings.Enums;

// using static LetMeHatchThisEgg.ModEntry; namespace LetMeHatchThisEgg.AssetManagement
// {
//     public class MachineEditor
//     {
//         public readonly List<SavedMachineRuleInfo> MachineRules = new();
//         private readonly IMonitor Monitor;
//         private readonly List<OutputItemCloneInfo> OutpuItemsToClone;

//         private readonly Dictionary<
//             string,
//             Dictionary<TextureInfoType, ItemTextureInfo>
//         > ClonedItemsTextureInfo;

//         public MachineEditor(
//             IMonitor monitor,
//             Dictionary<string, Dictionary<TextureInfoType, ItemTextureInfo>> clonedItemsTextureInfo,
//             List<OutputItemCloneInfo> outpuItemsToClone
//         )
//         {
//             Monitor = monitor;
//             ClonedItemsTextureInfo = clonedItemsTextureInfo;
//             OutpuItemsToClone = outpuItemsToClone;
//         }

//         public void CloneMachineData(Dictionary<string, MachineData> data)
//         {
//             if (MachineRules.Count < 1)
//             {
//                 SetUpMachineRules(data);
//                LoggerHelper."Machine rules were set up as none were present.", LogLevel.Info);
//             }

//             var modifiedMachines = new Dictionary<string, MachineData>();
//            LoggerHelper.
//                 $"Starting to clone machine data. Total MachineRules: {MachineRules.Count}",
//                 LogLevel.Info
//             );

//             foreach (var machineRule in MachineRules)
//             {
//                LoggerHelper.
//                     $"Processing MachineKey: {machineRule.MachineKey}, OutputRuleId: {machineRule.OutputRuleId}, OutputItemRuleID: {machineRule.OutputItemRuleID}, OutputItemId: {machineRule.OutputItemId}",
//                     LogLevel.Debug
//                 );

//                LoggerHelper.
//                     $"OutputItemRuleID null or empty: {!string.IsNullOrEmpty(machineRule.OutputItemRuleID)}. OutpuItemsToClone contains {machineRule.OutputItemId ?? ""}: {(machineRule.OutputItemId != null ? OutpuItemsToClone.Select(x => x.OutputItemOriginalID).Contains(machineRule.OutputItemId) : "is null")}",
//                     LogLevel.Warn
//                 );

//                 var outputRule = data[machineRule.MachineKey]
//                     .OutputRules.Find(or => or.Id == machineRule.OutputRuleId);
//                 if (outputRule == null)
//                 {
//                    LoggerHelper.
//                         $"Output rule not found for MachineKey: {machineRule.MachineKey} and OutputRuleId: {machineRule.OutputRuleId}.",
//                         LogLevel.Warn
//                     );
//                     continue; // Skip to the next iteration if the output rule is not found
//                 }

//                 if (
//                     outputRule.OutputItem[0].ItemId != null
//                     && machineRule.OutputItemId != null
//                     && OutpuItemsToClone
//                         .Select(x => x.OutputItemOriginalID)
//                         .Contains(GetUnqualifiedItemID(outputRule.OutputItem[0].ItemId))
//                 )
//                 {
//                    LoggerHelper.
//                         $"Cloning output rule for MachineKey: {machineRule.MachineKey}.",
//                         LogLevel.Info
//                     );

//                     var clonedRules = new List<MachineOutputRule>();
//                     foreach (var outputItemCloneInfo in OutpuItemsToClone)
//                     {
//                         var clonedRule = outputRule.CloneOrDeepClone(true);
//                         if (clonedRule == null)
//                         {
//                            LoggerHelper.$"Rule couln't be cloned.", LogLevel.Warn);
//                             continue;
//                         }
//                         var baseTrigger = clonedRule.Triggers.Find(t =>
//                             t.RequiredItemId == null
//                             || GetUnqualifiedItemID(t.RequiredItemId)
//                                 == outputItemCloneInfo.InputItemID
//                         );
//                         if (baseTrigger == null)
//                         {
//                            LoggerHelper.$"Trigger couln't be cloned.", LogLevel.Warn);
//                             continue;
//                         }
//                         baseTrigger.RequiredItemId = outputItemCloneInfo.InputItemClonedID;
//                         clonedRule.Triggers = new() { baseTrigger };

//                         var outputItemRule = clonedRule.OutputItem.Find(i =>
//                             i.Id == machineRule.OutputItemRuleID
//                         );
//                         if (outputItemRule == null)
//                         {
//                            LoggerHelper.
//                                 $"Output item rule not found for OutputItemRuleID: {machineRule.OutputItemRuleID}.",
//                                 LogLevel.Warn
//                             );
//                             continue;
//                         }
//                         outputItemRule.ItemId = outputItemCloneInfo.OutputItemClonedID;
//                         clonedRule.OutputItem = new() { outputItemRule };
//                         clonedRules.Add(clonedRule);
//                     }

//                     data[machineRule.MachineKey].OutputRules.AddRange(clonedRules);
//                     modifiedMachines.Add(machineRule.MachineKey, data[machineRule.MachineKey]);
//                    LoggerHelper.
//                         $"Cloned rule added for MachineKey: {machineRule.MachineKey}.",
//                         LogLevel.Info
//                     );
//                 }
//                 else
//                 {
//                    LoggerHelper.
//                         $"Adding triggers to existing output rule for MachineKey: {machineRule.MachineKey}.",
//                         LogLevel.Info
//                     );
//                     outputRule?.Triggers.AddRange(machineRule.Triggers);
//                 }
//             }

//            LoggerHelper.
//                 $"Finished processing all machine rules. Modified Machines count: {modifiedMachines.Count}",
//                 LogLevel.Info
//             );
//             ModEntry.HelperInstance?.Data.WriteJsonFile(
//                 "data/modifiedMachines.json",
//                 modifiedMachines
//             );
//             ModEntry.HelperInstance?.Data.WriteJsonFile("data/machineRules.json", MachineRules);
//         }

//         private void SetUpMachineRules(Dictionary<string, MachineData> data)
//         {
//            LoggerHelper."Setting up machine rules...", LogLevel.Debug);

//             var originalItems = ClonedItemsTextureInfo
//                 .SelectMany(t =>
//                     t.Value.Where(x => x.Key == TextureInfoType.Original).Select(x => x.Value)
//                 )
//                 .Select(y => y.ID)
//                 .Distinct()
//                 .ToList();

//            LoggerHelper.$"Original items: {string.Join(", ", originalItems)}", LogLevel.Debug);

//             foreach (var (machineKey, machineData) in data.Where(x => x.Value.OutputRules != null))
//             {
//                LoggerHelper.$"Processing machine: {machineKey}", LogLevel.Debug);

//                 foreach (var rule in machineData.OutputRules.Where(r => r.Triggers != null))
//                 {
//                    LoggerHelper.$"Processing rule: {rule.Id}", LogLevel.Debug);
//                     List<MachineOutputTriggerRule> triggers = new();

//                     foreach (var trigger in rule.Triggers)
//                     {
//                        LoggerHelper.$"Checking trigger: {trigger.RequiredItemId}", LogLevel.Debug);

//                         if (
//                             trigger.RequiredItemId != null
//                             && originalItems.Contains(GetUnqualifiedItemID(trigger.RequiredItemId))
//                         )
//                         {
//                            LoggerHelper.
//                                 $"Trigger required item ID is valid: {trigger.RequiredItemId}",
//                                 LogLevel.Debug
//                             );
//                             var unqualifiedReqItemId = GetUnqualifiedItemID(trigger.RequiredItemId);
//                             foreach (
//                                 var clonedID in ClonedItemsTextureInfo
//                                     .Values.Where(x =>
//                                         x[TextureInfoType.Original].ID == unqualifiedReqItemId
//                                     )
//                                     .Select(y => y[TextureInfoType.Cloned].ID)
//                             )
//                             {
//                                 var clonedTrigger = trigger.CloneOrDeepClone(true);
//                                 if (clonedTrigger != null)
//                                 {
//                                     clonedTrigger.RequiredItemId = $"{clonedID}";
//                                     clonedTrigger.Id = $"{clonedID}";
//                                     triggers.Add(clonedTrigger);

//                                    LoggerHelper.
//                                         $"Cloned trigger added with ID: {clonedID}",
//                                         LogLevel.Debug
//                                     );
//                                 }
//                                 else
//                                 {
//                                    LoggerHelper."Cloned trigger is null.", LogLevel.Debug);
//                                 }
//                             }
//                         }
//                         else
//                         {
//                            LoggerHelper.
//                                 $"Trigger required item ID is invalid or null: {trigger.RequiredItemId}",
//                                 LogLevel.Debug
//                             );
//                         }
//                     }

//                     if (triggers.Count < 1)
//                     {
//                        LoggerHelper."No valid triggers found, skipping this rule.", LogLevel.Debug);
//                         continue;
//                     }

//                     var outputItemRule = rule.OutputItem[0];
//                     var outputItemRuleID = outputItemRule.Id;
//                     var outputItemID =
//                         outputItemRule.ItemId != null
//                             ? GetUnqualifiedItemID(outputItemRule.ItemId)
//                             : null;

//                    LoggerHelper.
//                         $"Adding machine rule for {machineKey}: Rule ID: {rule.Id}, Output Item Rule ID: {outputItemRuleID}, Output Item ID: {outputItemID}",
//                         LogLevel.Debug
//                     );

//                     MachineRules.Add(
//                         new SavedMachineRuleInfo(
//                             machineKey,
//                             rule.Id,
//                             triggers,
//                             outputItemRuleID,
//                             outputItemID
//                         )
//                     );
//                 }
//             }

//             //LoggerHelper."Writing machine rules to JSON file...", LogLevel.Debug);
//             ModEntry.HelperInstance?.Data.WriteJsonFile("machineRuless.json", MachineRules);
//            LoggerHelper."Machine rules setup complete.", LogLevel.Debug);
//         }

//         private string GetUnqualifiedItemID(string qualifiedItemID) =>
//             qualifiedItemID.Replace("(O)", "");
//     }
// }
using LetMeHatchThisEgg.Config;
using LetMeHatchThisEgg.Models;
using Mikew.BirdsOfAFeather.Helpers;
using StardewModdingAPI;
using Mikew.BirdsOfAFeather.Helpers;
using StardewValley.GameData.Machines;
using static LetMeHatchThisEgg.EnumsAndStrings.Enums;

using static LetMeHatchThisEgg.ModEntry;

namespace LetMeHatchThisEgg.GameAssetManagement
{
    public class MachineEditor
    {
        internal readonly List<SavedMachineRuleInfo> MachineRules = new();
        private readonly List<OutputItemCloneInfo> _outputItemsToClone;
        private readonly Dictionary<
            string,
            Dictionary<TextureInfoType, ItemTextureInfo>
        > _clonedItemsTextureInfo;

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
            if (!MachineRules.Any())
            {
                SetUpMachineRules(data);
                LoggerHelper.VerboseLog("Machine rules were set up as none were present.", LogLevel.Info);
            }

            var modifiedMachines = new Dictionary<string, MachineData>();
            LoggerHelper.VerboseLog(
                $"Starting to clone machine data. Total MachineRules: {MachineRules.Count}",
                LogLevel.Info
            );

            foreach (var machineRule in MachineRules)
            {
                ProcessMachineRule(machineRule, data, modifiedMachines);
            }

            LoggerHelper.VerboseLog(
                $"Finished processing all machine rules. Modified Machines count: {modifiedMachines.Count}",
                LogLevel.Info
            );
            SaveModifiedData(modifiedMachines);
        }

        private void ProcessMachineRule(
            SavedMachineRuleInfo machineRule,
            Dictionary<string, MachineData> data,
            Dictionary<string, MachineData> modifiedMachines
        )
        {
            LoggerHelper.VerboseLog(
                $"Processing MachineKey: {machineRule.MachineKey}, OutputRuleId: {machineRule.OutputRuleId}, OutputItemRuleID: {machineRule.OutputItemRuleID}, OutputItemId: {machineRule.OutputItemId}",
                LogLevel.Debug
            );

            var outputRule = data[machineRule.MachineKey]
                .OutputRules.Find(or => or.Id == machineRule.OutputRuleId);
            if (outputRule == null)
            {
                LoggerHelper.VerboseLog(
                    $"Output rule not found for MachineKey: {machineRule.MachineKey} and OutputRuleId: {machineRule.OutputRuleId}.",
                    LogLevel.Warn
                );
                return; // Skip to the next iteration if the output rule is not found
            }

            if (CanCloneOutputRule(machineRule, outputRule))
            {
                CloneOutputRule(machineRule, outputRule, data, modifiedMachines);
            }
            else
            {
                LoggerHelper.VerboseLog(
                    $"Adding triggers to existing output rule for MachineKey: {machineRule.MachineKey}.",
                    LogLevel.Info
                );
                outputRule.Triggers.AddRange(machineRule.Triggers);
            }
        }

        private bool CanCloneOutputRule(
            SavedMachineRuleInfo machineRule,
            MachineOutputRule outputRule
        )
        {
            return outputRule.OutputItem[0].ItemId != null
                && machineRule.OutputItemId != null
                && _outputItemsToClone
                    .Select(x => x.OutputItemOriginalID)
                    .Contains(GetUnqualifiedItemID(outputRule.OutputItem[0].ItemId));
        }

        private void CloneOutputRule(
            SavedMachineRuleInfo machineRule,
            MachineOutputRule outputRule,
            Dictionary<string, MachineData> data,
            Dictionary<string, MachineData> modifiedMachines
        )
        {
            LoggerHelper.VerboseLog(
                $"Cloning output rule for MachineKey: {machineRule.MachineKey}.",
                LogLevel.Info
            );
            var clonedRules = new List<MachineOutputRule>();

            foreach (var outputItemCloneInfo in _outputItemsToClone)
            {
                var clonedRule = outputRule.CloneOrDeepClone(true);
                if (clonedRule == null)
                {
                    LoggerHelper.VerboseLog("Rule couldn't be cloned.", LogLevel.Warn);
                    continue;
                }

                UpdateClonedRule(clonedRule, outputItemCloneInfo);
                clonedRules.Add(clonedRule);
            }

            data[machineRule.MachineKey].OutputRules.AddRange(clonedRules);
            modifiedMachines[machineRule.MachineKey] = data[machineRule.MachineKey];
            LoggerHelper.VerboseLog(
                $"Cloned rule added for MachineKey: {machineRule.MachineKey}.",
                LogLevel.Info
            );
        }

        private void UpdateClonedRule(
            MachineOutputRule clonedRule,
            OutputItemCloneInfo outputItemCloneInfo
        )
        {
            var baseTrigger = clonedRule.Triggers.Find(t =>
                t.RequiredItemId == null
                || GetUnqualifiedItemID(t.RequiredItemId) == outputItemCloneInfo.InputItemID
            );
            if (baseTrigger != null)
            {
                baseTrigger.RequiredItemId = outputItemCloneInfo.InputItemClonedID;
                clonedRule.Triggers = new() { baseTrigger };
            }

            var outputItemRule = clonedRule.OutputItem.Find(i =>
                i.Id == outputItemCloneInfo.OutputItemClonedID
            );
            if (outputItemRule != null)
            {
                outputItemRule.ItemId = outputItemCloneInfo.OutputItemClonedID;
                clonedRule.OutputItem = new() { outputItemRule };
            }
        }

        private void SaveModifiedData(Dictionary<string, MachineData> modifiedMachines)
        {
            ModEntry.HelperInstance?.Data.WriteJsonFile(
                "data/modifiedMachines.json",
                modifiedMachines
            );
            ModEntry.HelperInstance?.Data.WriteJsonFile("data/machineRules.json", MachineRules);
        }

        private void SetUpMachineRules(Dictionary<string, MachineData> data)
        {
            LoggerHelper.VerboseLog("Setting up machine rules...", LogLevel.Debug);

            var originalItems = GetOriginalItems();
            LoggerHelper.VerboseLog($"Original items: {string.Join(", ", originalItems)}", LogLevel.Debug);

            foreach (var (machineKey, machineData) in data.Where(x => x.Value.OutputRules != null))
            {
                LoggerHelper.VerboseLog($"Processing machine: {machineKey}", LogLevel.Debug);
                ProcessMachineRules(machineKey, machineData, originalItems);
            }

            ModEntry.HelperInstance?.Data.WriteJsonFile("machineRules.json", MachineRules);
            LoggerHelper.VerboseLog("Machine rules setup complete.", LogLevel.Debug);
        }

        private List<string> GetOriginalItems()
        {
            return _clonedItemsTextureInfo
                .SelectMany(t =>
                    t.Value.Where(x => x.Key == TextureInfoType.Original).Select(x => x.Value.ID)
                )
                .Distinct()
                .ToList();
        }

        private void ProcessMachineRules(
            string machineKey,
            MachineData machineData,
            List<string> originalItems
        )
        {
            foreach (var rule in machineData.OutputRules.Where(r => r.Triggers != null))
            {
                LoggerHelper.VerboseLog($"Processing rule: {rule.Id}", LogLevel.Debug);
                var triggers = GetValidTriggers(rule.Triggers, originalItems);

                if (!triggers.Any())
                {
                    LoggerHelper.VerboseLog("No valid triggers found, skipping this rule.", LogLevel.Debug);
                    continue;
                }

                AddMachineRule(machineKey, rule, triggers);
            }
        }

        private List<MachineOutputTriggerRule> GetValidTriggers(
            List<MachineOutputTriggerRule> triggers,
            List<string> originalItems
        )
        {
            var validTriggers = new List<MachineOutputTriggerRule>();

            foreach (var trigger in triggers)
            {
                LoggerHelper.VerboseLog($"Checking trigger: {trigger.RequiredItemId}", LogLevel.Debug);

                if (
                    trigger.RequiredItemId != null
                    && originalItems.Contains(GetUnqualifiedItemID(trigger.RequiredItemId))
                )
                {
                    LoggerHelper.VerboseLog(
                        $"Trigger required item ID is valid: {trigger.RequiredItemId}",
                        LogLevel.Debug
                    );
                    validTriggers.AddRange(CloneTriggers(trigger));
                }
                else
                {
                    LoggerHelper.VerboseLog(
                        $"Trigger required item ID is invalid or null: {trigger.RequiredItemId}",
                        LogLevel.Debug
                    );
                }
            }

            return validTriggers;
        }

        private IEnumerable<MachineOutputTriggerRule> CloneTriggers(
            MachineOutputTriggerRule trigger
        )
        {
            var clonedTriggers = new List<MachineOutputTriggerRule>();

            foreach (
                var clonedID in _clonedItemsTextureInfo
                    .Values.Where(x =>
                        x[TextureInfoType.Original].ID
                        == GetUnqualifiedItemID(trigger.RequiredItemId)
                    )
                    .Select(y => y[TextureInfoType.Cloned].ID)
            )
            {
                var clonedTrigger = trigger.CloneOrDeepClone(true);
                if (clonedTrigger != null)
                {
                    clonedTrigger.RequiredItemId = $"{clonedID}";
                    clonedTrigger.Id = $"{clonedID}";
                    clonedTriggers.Add(clonedTrigger);
                    LoggerHelper.VerboseLog($"Cloned trigger added with ID: {clonedID}", LogLevel.Debug);
                }
                else
                {
                    LoggerHelper.VerboseLog("Cloned trigger is null.", LogLevel.Debug);
                }
            }

            return clonedTriggers;
        }

        private void AddMachineRule(
            string machineKey,
            MachineOutputRule rule,
            List<MachineOutputTriggerRule> triggers
        )
        {
            var outputItemRule = rule.OutputItem[0];
            var outputItemRuleID = outputItemRule.Id;
            var outputItemID =
                outputItemRule.ItemId != null ? GetUnqualifiedItemID(outputItemRule.ItemId) : null;

            LoggerHelper.VerboseLog(
                $"Adding machine rule for {machineKey}: Rule ID: {rule.Id}, Output Item Rule ID: {outputItemRuleID}, Output Item ID: {outputItemID}",
                LogLevel.Debug
            );

            MachineRules.Add(
                new SavedMachineRuleInfo(
                    machineKey,
                    rule.Id,
                    triggers,
                    outputItemRuleID,
                    outputItemID
                )
            );
        }

        private string GetUnqualifiedItemID(string qualifiedItemID) =>
            qualifiedItemID.Replace("(O)", "");
    }
}