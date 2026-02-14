using Microsoft.Xna.Framework;
using Mikew.BirdsOfAFeather.Helpers;
using StardewValley.Extensions;
using StardewValley.GameData;
using StardewValley.GameData.Machines;
using WildEggsHatching.Models;
using static Mikew.BirdsOfAFeather.EnumsAndStrings.Strings.ItemIds;
using static WildEggsHatching.EnumsAndStrings.Enums;
using static WildEggsHatching.EnumsAndStrings.Strings;
using static WildEggsHatching.ModEntry;

namespace WildEggsHatching.Managers.GameAssetManagement
{
    /// <summary>The mod entry point.</summary>
    internal class MachineEditor
    {
        internal readonly Dictionary<string, MachineData> MachineRulesToAdd;
        private const string IncubatorOutputMethod =
            "StardewValley.Object, Stardew Valley: OutputIncubator";
        private readonly string WildEggIncubatorKey = $"(BC){IDs.BigCraftables.WildEggIncubator}";
        private readonly HashSet<WildEgg> _wildEggs;

        internal MachineEditor(HashSet<WildEgg> wildEggs)
        {
            MachineRulesToAdd = new();
            _wildEggs = wildEggs;
            LoggerHelper.VerboseLog("MachineEditor initialized.");
        }

        private void CreateMachineRules()
        {
            LoggerHelper.VerboseLog("Creating machine rules...");
            MachineRulesToAdd[WildEggIncubatorKey] = CreateMachineData();
            LoggerHelper.VerboseLog("Machine rules created for WildEggIncubator.");
        }

        private MachineData CreateMachineData()
        {
            LoggerHelper.VerboseLog("Creating machine data for WildEggIncubator...");
            return new MachineData
            {
                HasInput = false,
                HasOutput = false,
                InteractMethod = null,
                OutputRules = CreateOutputRules(),
                AdditionalConsumedItems = CreateAdditionalConsumedItems(),
                ReadyTimeModifiers = CreateReadyTimeModifiers(),
                ReadyTimeModifierMode = QuantityModifier.QuantityModifierMode.Stack,
                InvalidItemMessage = "Only work with wild eggs.",
                InvalidItemMessageCondition = "ITEM_CATEGORY Input -5",
                InvalidCountMessage = null,
                LoadEffects = CreateLoadEffects(),
                WorkingEffects = CreateWorkingEffects(),
                WorkingEffectChance = 0.33f,
                WobbleWhileWorking = false,
                LightWhileWorking = null,
                ShowNextIndexWhileWorking = true,
                AllowFairyDust = true,
                IsIncubator = true,
            };
        }

        private List<MachineOutputRule> CreateOutputRules()
        {
            LoggerHelper.VerboseLog("Creating output rules for the incubator...");
            var rules = new List<MachineOutputRule>();
            foreach (var egg in _wildEggs.Where(e => e.ChosenAnimalKey != null))
            {
                rules.Add(CreateOutputRule(egg.ID, egg.IsLarge));
            }
            return rules;
        }

        private MachineOutputRule CreateOutputRule(string requiredItemId, bool incrementIndex)
        {
            LoggerHelper.VerboseLog(
                $"Creating output rule: {requiredItemId} for required item: {requiredItemId}..."
            );
            return new MachineOutputRule
            {
                Id = requiredItemId,
                Triggers = new List<MachineOutputTriggerRule>
                {
                    new()
                    {
                        Id = $"ItemPlacedInMachine_{requiredItemId}",
                        Trigger = MachineOutputTrigger.ItemPlacedInMachine,
                        RequiredItemId = requiredItemId,
                    },
                },
                UseFirstValidOutput = true,
                OutputItem = new List<MachineItemOutput>
                {
                    new()
                    {
                        OutputMethod = IncubatorOutputMethod,
                        IncrementMachineParentSheetIndex = incrementIndex ? 1 : 0,
                    },
                },
            };
        }

        private List<MachineItemAdditionalConsumedItems> CreateAdditionalConsumedItems()
        {
            LoggerHelper.VerboseLog("Creating additional consumed items for the incubator...");
            return new List<MachineItemAdditionalConsumedItems>
            {
                new()
                {
                    ItemId = "259",
                    RequiredCount = 5,
                    InvalidCountMessage = "It needs 5 ferns.",
                },
            };
        }

        private List<QuantityModifier> CreateReadyTimeModifiers()
        {
            LoggerHelper.VerboseLog("Creating ready time modifiers...");
            return new List<QuantityModifier>
            {
                new()
                {
                    Id = "Coopmaster",
                    Condition = "PLAYER_HAS_PROFESSION Target 2",
                    Modification = QuantityModifier.ModificationType.Multiply,
                    Amount = 0.5f,
                },
            };
        }

        private List<MachineEffects> CreateLoadEffects()
        {
            LoggerHelper.VerboseLog("Creating load effects...");
            return new List<MachineEffects>
            {
                new()
                {
                    Id = "Default",
                    Condition = null,
                    Sounds = new List<MachineSoundData>
                    {
                        new() { Id = "trashcan", Delay = 0 },
                    },
                    Interval = 100,
                    ShakeDuration = -1,
                },
            };
        }

        private List<MachineEffects> CreateWorkingEffects()
        {
            LoggerHelper.VerboseLog("Creating working effects...");
            return new List<MachineEffects>
            {
                new()
                {
                    Id = "Default",
                    Condition = null,
                    Sounds = new List<MachineSoundData>
                    {
                        new() { Id = "trashcan", Delay = 0 },
                    },
                    Interval = 100,
                    ShakeDuration = -1,
                    TemporarySprites = new List<TemporaryAnimatedSpriteDefinition>
                    {
                        new()
                        {
                            Id = "Default",
                            Texture = "TileSheets\\animations",
                            SourceRect = new Rectangle(0, 704, 64, 64),
                            Interval = 20f,
                            Frames = 6,
                            Loops = 999999,
                            PositionOffset = new Vector2(0, -32),
                            Flicker = false,
                            Scale = 0.25f,
                            AlphaFade = 0.005f,
                        },
                    },
                },
            };
        }

        internal void EditOtherIncubators(IDictionary<string, MachineData> data)
        {
            LoggerHelper.VerboseLog("Editing other incubators...");
            var incubators = data
                .Keys.Where(key =>
                    (
                        key.ContainsIgnoreCase("incubator")
                        || key == $"(BC){IDs.BigCraftables.Incubator}"
                        || key == $"(BC){IDs.BigCraftables.OstrichIncubator}"
                    ) && (key != WildEggIncubatorKey)
                )
                .ToList();
            incubators.ForEach(machine =>
            {
                LoggerHelper.VerboseLog($"Editing incubator: {machine}...");
                data[machine]
                    .OutputRules.ForEach(or =>
                        or.Triggers.ForEach(t =>
                        {
                            t.RequiredTags ??= new List<string>();
                            t.RequiredTags.Add("!wildegg_item");
                            if (ModEntry.Config.DinoEggsExclusiveHatch)
                            {
                                t.RequiredTags.Add("!item_dinosaur_egg");
                            }
                        })
                    );
                data[machine].InvalidItemMessageCondition = "ITEM_CONTEXT_TAG Input wildegg_item";
                data[machine].InvalidItemMessage = "You can't hatch wild eggs here.";
            });
            LoggerHelper.VerboseLog("Other incubators edited.");
        }

        internal void EditWildEggIncubatorRules()
        {
            LoggerHelper.VerboseLog("Editing WildEggIncubator rules...");
            if (ModEntry.Config.HatchDinoEggs)
            {
                try
                {
                    MachineRulesToAdd[WildEggIncubatorKey]
                        .OutputRules.Add(
                            new MachineOutputRule
                            {
                                Id = "DinoEgg",
                                Triggers = new List<MachineOutputTriggerRule>
                                {
                                    new()
                                    {
                                        Id = "ItemPlacedInMachineDino",
                                        Trigger = MachineOutputTrigger.ItemPlacedInMachine,
                                        RequiredItemId = AnimalProducts.DinoEgg,
                                    },
                                },
                                UseFirstValidOutput = true,
                                OutputItem = new List<MachineItemOutput>
                                {
                                    new()
                                    {
                                        OutputMethod = IncubatorOutputMethod,
                                        IncrementMachineParentSheetIndex = 2,
                                    },
                                },
                            }
                        );
                }
                catch (System.Exception)
                {
                    LoggerHelper.Log(
                        string.Join(',', MachineRulesToAdd.Keys),
                        StardewModdingAPI.LogLevel.Error
                    );
                    throw;
                }
            }
            if (!ModEntry.Config.NeedsFern)
            {
                MachineRulesToAdd[WildEggIncubatorKey].AdditionalConsumedItems = null;
            }
            LoggerHelper.VerboseLog("WildEggIncubator rules edited.");
        }

        internal void AddMachineOutputRules(IDictionary<string, MachineData> data)
        {
            LoggerHelper.VerboseLog("Adding machine output rules...");
            data["(BC)15"]
                .OutputRules?.RemoveWhere(x =>
                    x.OutputItem != null
                    && x.OutputItem.Any(o => o.ItemId == "yourdorkbrains.cp_PickledWildEggs")
                );
            data["(BC)24"]
                .OutputRules?.RemoveWhere(x =>
                    x.OutputItem != null
                    && x.OutputItem.Any(o => o.ItemId == "yourdorkbrains.cp_WildEggMayo")
                );
            GameAssetManager.PopulateIfEmpty(MachineRulesToAdd, () => CreateMachineRules());
            data.TryAddMany(MachineRulesToAdd);

            LoggerHelper.VerboseLog("Machine output rules added.");
        }

        internal void AddMachineOutputRulesForEggs(IDictionary<string, MachineData> data)
        {
            LoggerHelper.VerboseLog("Adding machine output rules for eggs...");
            // Define a helper method to create and add output rules
            void CreateAndAddOutputRule(
                string dataKey,
                string outputRuleId,
                string outpuItemId,
                string contextTag,
                int minStack,
                int maxStack,
                int minutesUntilReady
            )
            {
                LoggerHelper.VerboseLog(
                    $"Creating and adding output rule: {outputRuleId} for item: {outpuItemId}..."
                );
                var machineOutputTriggerRule = new MachineOutputTriggerRule
                {
                    Id = contextTag,
                    Trigger = MachineOutputTrigger.ItemPlacedInMachine,
                    //RequiredItemId = contextTag,
                    RequiredCount = 1,
                    RequiredTags = new() { contextTag },
                };

                var machineItemOutput = new MachineItemOutput
                {
                    CopyQuality = true,
                    Id = outpuItemId,
                    ItemId = outpuItemId,
                    MinStack = minStack,
                    MaxStack = maxStack,
                };

                var outputRule = new MachineOutputRule
                {
                    Id = outputRuleId,
                    OutputItem = new List<MachineItemOutput> { machineItemOutput },
                    MinutesUntilReady = minutesUntilReady,
                    Triggers = new List<MachineOutputTriggerRule> { machineOutputTriggerRule },
                };

                data[dataKey].OutputRules.Add(outputRule);
            }
            int baseTime = 200;
            int baseQuantity = 1;
            var eggTypeContexTags = EnumExtensions.GetValues<EggType>();

            foreach (var type in eggTypeContexTags)
            {
                int quantity = type switch
                {
                    EggType.wildEgg => baseQuantity,
                    EggType.largeWildEgg => baseQuantity * 2,
                    EggType.magicWildEgg => baseQuantity * 4,
                    EggType.largeMagicWildEgg => baseQuantity * 8,
                    _ => baseQuantity,
                };
                int time = type switch
                {
                    EggType.wildEgg => baseTime,
                    EggType.largeWildEgg => baseTime * 2,
                    EggType.magicWildEgg => baseTime * 4,
                    EggType.largeMagicWildEgg => baseTime * 8,
                    _ => baseTime,
                };
                string contextTag = "wildegg_" + type.ToString();
                string id = $"{ModManifestInstance.UniqueID}_{type}";
                CreateAndAddOutputRule(
                    "(BC)15",
                    id,
                    "yourdorkbrains.cp_PickledWildEggs",
                    contextTag,
                    quantity,
                    quantity,
                    time
                );
                CreateAndAddOutputRule(
                    "(BC)24",
                    id,
                    "yourdorkbrains.cp_WildEggMayo",
                    contextTag,
                    quantity,
                    quantity,
                    time
                );
            }

            LoggerHelper.VerboseLog("Machine output rules for eggs added.");
        }
    }
}
