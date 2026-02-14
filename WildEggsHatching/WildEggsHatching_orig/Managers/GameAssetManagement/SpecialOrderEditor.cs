using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Mikew.BirdsOfAFeather.Helpers.GameStateQueries;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.GameData.SpecialOrders;
using static WildEggsHatching.EnumsAndStrings.Strings;
using static WildEggsHatching.ModEntry;

namespace WildEggsHatching.Managers.GameAssetManagement
{
    /// <summary>The mod entry point.</summary>
    internal class SpecialOrderEditor
    {
        private const string Marnie = "Marnie";
        private const string Wizard = "Wizard";

        private const string MailReceived = "MailReceived";
        private const string Deliver = "Deliver";
        private const string Message = "Message";
        private const string TargetName = "TargetName";
        private const string AcceptedContextTags = "AcceptedContextTags";
        private const string Donate = "Donate";
        private const string Collect = "Collect";
        private const string MarnieDropBoxName = "ForestWildEgg";
        private const string MarnieDropBoxLocation = "Forest";
        internal readonly Dictionary<string, SpecialOrderData> SpecialOrdersToAdd;

        internal SpecialOrderEditor()
        {
            SpecialOrdersToAdd = new();
            LoggerHelper.VerboseLog("SpecialOrderEditor initialized.");
        }

        private void CreateSpecialOrders()
        {
            LoggerHelper.VerboseLog("Creating special orders...");
            SpecialOrdersToAdd[IDs.Quests.SpecialOrderWildEggsI] = CreateWildEggsOrderI();
            SpecialOrdersToAdd[IDs.Quests.SpecialOrderWildEggsII] = CreateWildEggsOrderII();
            LoggerHelper.VerboseLog("Special orders created.");
        }

        private void SetUpDropBox()
        {
            LoggerHelper.VerboseLog("Setting up drop box...");
            var Forest = Game1.locations.First(l => l.NameOrUniqueName == MarnieDropBoxLocation);
            Forest.setTileProperty(87, 16, "Buildings", "Action", $"DropBox {MarnieDropBoxName}");
            Forest.dropBoxIndicatorLocation = new Vector2(87, 14);
            LoggerHelper.VerboseLog($"Drop box set up at {MarnieDropBoxLocation}.");
        }

        internal void AddSpecialOrders(IDictionary<string, SpecialOrderData> data)
        {
            LoggerHelper.VerboseLog("Adding special orders...");
            GameAssetManager.PopulateIfEmpty(SpecialOrdersToAdd, () => CreateSpecialOrders());
            data.TryAddMany(SpecialOrdersToAdd);
            LoggerHelper.VerboseLog("Special orders added.");
        }

        internal static bool HasCompletedSpecialOrder(string specialOrderID)
        {
            bool completed = GameStateQuery.CheckConditions(
                PlayerCondition.SpecialOrderComplete(specialOrderID)
            );
            LoggerHelper.VerboseLog(
                $"Checked completion for special order {specialOrderID}: {completed}"
            );
            return completed;
        }

        private SpecialOrderData CreateWildEggsOrderI()
        {
            LoggerHelper.VerboseLog("Creating Wild Eggs Order I...");
            var orderData = CreateSpecialOrderData(
                condition: $"!{PlayerCondition.HasMail(IDs.Mail.SpecialOrderDoneI)}, !{TimeCondition.Season("winter")}",
                textKey: "specialOrder.WildEggIncubatorI.text",
                nameKey: "specialOrder.WildEggIncubatorI.name",
                requester: Marnie,
                objectives: new List<SpecialOrderObjectiveData>
                {
                    CreateObjective(
                        Collect,
                        "wildegg_item",
                        5,
                        "specialOrder.WildEggIncubatorI.objective.0"
                    ),
                    CreateObjective(
                        Donate,
                        new Dictionary<string, string>
                        {
                            { "DropBox", MarnieDropBoxName },
                            { "DropBoxGameLocation", MarnieDropBoxLocation },
                            { "DropBoxIndicatorLocation", "87 16" },
                            { AcceptedContextTags, "wildegg_item" },
                        },
                        5,
                        "specialOrder.WildEggIncubatorI.objective.1"
                    ),
                },
                rewards: new List<SpecialOrderRewardData>
                {
                    CreateReward(MailReceived, IDs.Mail.SpecialOrderDoneI),
                }
            );
            LoggerHelper.VerboseLog("Wild Eggs Order I created.");
            return orderData;
        }

        private SpecialOrderData CreateWildEggsOrderII()
        {
            LoggerHelper.VerboseLog("Creating Wild Eggs Order II...");
            var chosenAncient = ModEntry.Config.FruitInsteadOfSeed ? "fruit" : "seed";
            var orderData = CreateSpecialOrderData(
                condition: $"!{PlayerCondition.HasMail(IDs.Mail.SpecialOrderDoneII)}, {PlayerCondition.HasMail(IDs.Mail.WizardDone)}",
                textKey: "specialOrder.WildEggIncubatorII.text",
                nameKey: "specialOrder.WildEggIncubatorII.name",
                requester: Wizard,
                objectives: new List<SpecialOrderObjectiveData>
                {
                    CreateObjective(
                        Deliver,
                        new Dictionary<string, string>
                        {
                            {
                                Message,
                                TranslationHelper.Get(
                                    $"specialOrder.WildEggIncubatorII.objective.0.message_{chosenAncient}"
                                )
                            },
                            { TargetName, Wizard },
                            { AcceptedContextTags, $"item_ancient_{chosenAncient}" },
                        },
                        1,
                        $"specialOrder.WildEggIncubatorII.objective.0.text_{chosenAncient}"
                    ),
                    CreateObjective(
                        Deliver,
                        new Dictionary<string, string>
                        {
                            {
                                Message,
                                TranslationHelper.Get(
                                    "specialOrder.WildEggIncubatorII.objective.2.message"
                                )
                            },
                            { TargetName, Wizard },
                            { AcceptedContextTags, "item_dinosaur_egg" },
                        },
                        1,
                        "specialOrder.WildEggIncubatorII.objective.2.text"
                    ),
                    CreateObjective(
                        Deliver,
                        new Dictionary<string, string>
                        {
                            {
                                Message,
                                TranslationHelper.Get(
                                    "specialOrder.WildEggIncubatorII.objective.1.message"
                                )
                            },
                            { TargetName, Wizard },
                            { AcceptedContextTags, "preserve_sheet_index_734" },
                        },
                        1,
                        "specialOrder.WildEggIncubatorII.objective.1.text"
                    ),
                },
                rewards: new List<SpecialOrderRewardData>
                {
                    CreateReward(MailReceived, IDs.Mail.SpecialOrderDoneII),
                }
            );
            LoggerHelper.VerboseLog("Wild Eggs Order II created.");
            return orderData;
        }

        // Helper methods
        private SpecialOrderData CreateSpecialOrderData(
            string condition,
            string textKey,
            string nameKey,
            string requester,
            List<SpecialOrderObjectiveData> objectives,
            List<SpecialOrderRewardData> rewards
        )
        {
            LoggerHelper.VerboseLog("Creating special order data...");
            var orderData = new SpecialOrderData
            {
                Condition = condition,
                Duration = QuestDuration.Month,
                Text = TranslationHelper.Get(textKey),
                ItemToRemoveOnEnd = string.Empty,
                MailToRemoveOnEnd = string.Empty,
                Name = TranslationHelper.Get(nameKey),
                Objectives = objectives,
                OrderType = string.Empty,
                RandomizedElements = null,
                Repeatable = true,
                Requester = requester,
                RequiredTags = string.Empty,
                Rewards = rewards,
                CustomFields = new Dictionary<string, string> { { "Untimed", "true" } },
            };
            LoggerHelper.VerboseLog("Special order data created.");
            return orderData;
        }

        private SpecialOrderObjectiveData CreateObjective(
            string type,
            object data,
            int requiredCount,
            string textKey
        )
        {
            LoggerHelper.VerboseLog("Creating special order objective...");
            var objectiveData = new SpecialOrderObjectiveData
            {
                Type = type,
                Data = data is Dictionary<string, string> dictionary
                    ? dictionary
                    : new Dictionary<string, string>(),
                RequiredCount = requiredCount.ToString(),
                Text = TranslationHelper.Get(textKey),
            };
            LoggerHelper.VerboseLog("Special order objective created.");
            return objectiveData;
        }

        private SpecialOrderRewardData CreateReward(string type, string mailReceived)
        {
            LoggerHelper.VerboseLog("Creating special order reward...");
            var rewardData = new SpecialOrderRewardData
            {
                Type = type,
                Data = new Dictionary<string, string> { { MailReceived, mailReceived } },
            };
            LoggerHelper.VerboseLog("Special order reward created.");
            return rewardData;
        }

        internal void ChangeAncientItemRequired()
        {
            LoggerHelper.VerboseLog("Changing ancient item required...");
            if (
                SpecialOrdersToAdd.TryGetValue(
                    IDs.Quests.SpecialOrderWildEggsII,
                    out var specialOrderData
                )
            )
            {
                var objective = specialOrderData.Objectives.Find(o =>
                    Regex.IsMatch(o.Data[AcceptedContextTags], @"^item_ancient_.*")
                );
                var chosenAncient = ModEntry.Config.FruitInsteadOfSeed ? "fruit" : "seed";
                if (objective == null)
                {
                    return;
                }
                objective.Data[AcceptedContextTags] = $"item_ancient_{chosenAncient}";
                objective.Data[Message] = TranslationHelper.Get(
                    $"specialOrder.WildEggIncubatorII.objective.0.message_{chosenAncient}"
                );
                objective.Text =
                    $"specialOrder.WildEggIncubatorII.objective.0.text_{chosenAncient}";
            }
            LoggerHelper.VerboseLog("Ancient item required changed.");
        }
    }
}
