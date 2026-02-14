using Mikew.BirdsOfAFeather.Framework;
using Mikew.BirdsOfAFeather.Helpers.GameStateQueries;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.BigCraftables;
using StardewValley.GameData.FarmAnimals;
using StardewValley.GameData.Machines;
using StardewValley.GameData.Objects;
using StardewValley.GameData.SpecialOrders;
using WildEggsHatching.Framework;
using WildEggsHatching.Management;
using WildEggsHatching.Models;
using static WildEggsHatching.Management.ProgressionManager;

namespace WildEggsHatching.Managers.GameAssetManagement
{
    internal class GameAssetManager
    {
        private readonly BigCraftableEditor BigCraftableEditor;
        private readonly CraftingRecipeEditor CraftingRecipeEditor;
        private readonly FarmAnimalEditor FarmAnimalEditor;
        private readonly MachineEditor MachineEditor;
        private readonly MailEditor MailEditor;
        private readonly ObjectEditor ObjectEditor;
        private readonly QuestEditor QuestEditor;
        private readonly SpecialOrderEditor SpecialOrderEditor;
        private readonly ProgressionManager _progressionManager;
        internal readonly HashSet<WildEgg> WildEggs;
        internal List<string> AllMail => MailEditor.AllMail;
        internal List<string> AllQuests => QuestEditor.AllQuests;

        internal GameAssetManager(
            ILetMeHatchApi? letMeHatchApi,
            WildListManager wildListManager,
            ProgressionManager progressionManager
        )
        {
            WildEggs = new();
            _progressionManager = progressionManager;
            BigCraftableEditor = new();
            CraftingRecipeEditor = new();
            ObjectEditor = new(WildEggs, wildListManager, progressionManager.ModSaveData);
            FarmAnimalEditor = new(
                wildListManager,
                WildEggs,
                letMeHatchApi,
                progressionManager.ModSaveData
            );
            MachineEditor = new(WildEggs);
            MailEditor = new(progressionManager.ModSaveData);
            QuestEditor = new(progressionManager.ModSaveData);
            SpecialOrderEditor = new();
        }

        internal void OnDayEnding()
        {
            BigCraftableEditor.BigCraftablesToAdd.Clear();
            CraftingRecipeEditor.CraftingRecipesToAdd.Clear();
            MachineEditor.MachineRulesToAdd.Clear();
            MailEditor.MailToAdd.Clear();
            ObjectEditor.ItemsToAdd.Clear();
            QuestEditor.QuestsToAdd.Clear();
            SpecialOrderEditor.SpecialOrdersToAdd.Clear();
        }

        internal void ResetAllEggs()
        {
            foreach (var egg in WildEggs)
            {
                egg.Manager.Reset();
            }
        }

        internal void ProcessWildEggs(Action<WildEgg> operation)
        {
            foreach (var egg in WildEggs)
            {
                operation(egg);
            }
        }

        internal void ChooseAnimalsForAllEggs()
        {
            foreach (var egg in WildEggs)
            {
                egg.Manager.SelectRandomAnimals();
            }
        }

        internal void AddBigCraftables(AssetRequestedEventArgs e, AssetEditPriority editPriority)
        {
            /// <summary>
            /// Adds big craftables to the game assets.
            /// </summary>
            /// <param name="e">The asset requested event arguments.</param>
            /// <param name="editPriority">The edit priority for the asset.</param>
            e.Edit(
                asset =>
                {
                    var data = asset.AsDictionary<string, BigCraftableData>().Data;
                    BigCraftableEditor.AddBigCraftables(data);
                },
                editPriority
            );
        }

        internal void EditWildEggIncubatorAsset(
            AssetRequestedEventArgs e,
            AssetEditPriority editPriority
        )
        {
            e.Edit(
                asset =>
                {
                    var editor = asset.AsImage();
                    BigCraftableEditor.EditWildEggIncubatorAsset(editor);
                },
                editPriority
            );
        }

        internal void AddCraftingRecipes(AssetRequestedEventArgs e, AssetEditPriority editPriority)
        {
            /// <summary>
            /// Adds crafting recipes to the game assets.
            /// </summary>
            /// <param name="e">The asset requested event arguments.</param>
            /// <param name="editPriority">The edit priority for the asset.</param>
            e.Edit(
                asset =>
                {
                    var data = asset.AsDictionary<string, string>().Data;
                    CraftingRecipeEditor.AddCraftingRecipes(data);
                },
                editPriority
            );
        }

        internal void AddVanillaAnimalPrices(
            AssetRequestedEventArgs e,
            AssetEditPriority editPriority
        )
        {
            /// <summary>
            /// Adds price to some vanilla animals.
            /// </summary>
            /// <param name="e">The asset requested event arguments.</param>
            /// <param name="editPriority">The edit priority for the asset.</param>
            e.Edit(
                asset =>
                {
                    var data = asset.AsDictionary<string, FarmAnimalData>().Data;
                    FarmAnimalEditor.AddVanillaAnimalPrices(data);
                },
                editPriority
            );
        }

        internal void AddWildEggToChosenAnimals(
            AssetRequestedEventArgs e,
            AssetEditPriority editPriority
        )
        {
            /// <summary>
            /// Adds the wild eggs to the chosen animals.
            /// </summary>
            /// <param name="e">The asset requested event arguments.</param>
            /// <param name="editPriority">The edit priority for the asset.</param>
            e.Edit(
                asset =>
                {
                    var data = asset.AsDictionary<string, FarmAnimalData>().Data;
                    _progressionManager.ExecuteIfProgressionAllows(
                        ProgressionStep.PartVI,
                        () => FarmAnimalEditor.AddWildEggsToChosenAnimals(data)
                    );
                },
                editPriority
            );
        }

        internal void ProgressionModeEdits(
            AssetRequestedEventArgs e,
            AssetEditPriority editPriority
        )
        {
            /// <summary>
            /// Adds price to some vanilla animals.
            /// </summary>
            /// <param name="e">The asset requested event arguments.</param>
            /// <param name="editPriority">The edit priority for the asset.</param>
            e.Edit(
                asset =>
                {
                    var data = asset.AsDictionary<string, FarmAnimalData>().Data;

                    FarmAnimalEditor.ProgressionModeEdits(data);
                },
                editPriority
            );
        }

        internal void AddMachineOutputRules(
            AssetRequestedEventArgs e,
            AssetEditPriority editPriority
        )
        {
            /// <summary>
            /// Adds machine output rules to the game assets.
            /// </summary>
            /// <param name="e">The asset requested event arguments.</param>
            /// <param name="editPriority">The edit priority for the asset.</param>
            e.Edit(
                asset =>
                {
                    var data = asset.AsDictionary<string, MachineData>().Data;
                    MachineEditor.AddMachineOutputRules(data);
                },
                editPriority
            );
        }

        internal void AddMachineOutputRulesForEggs(
            AssetRequestedEventArgs e,
            AssetEditPriority editPriority
        )
        {
            /// <summary>
            /// Adds machine output rules to the game assets.
            /// </summary>
            /// <param name="e">The asset requested event arguments.</param>
            /// <param name="editPriority">The edit priority for the asset.</param>
            e.Edit(
                asset =>
                {
                    var data = asset.AsDictionary<string, MachineData>().Data;
                    MachineEditor.AddMachineOutputRulesForEggs(data);
                },
                editPriority
            );
        }

        internal void EditOtherIncubators(AssetRequestedEventArgs e, AssetEditPriority editPriority)
        {
            /// <summary>
            /// Edits other incubators in the game assets.
            /// </summary>
            /// <param name="e">The asset requested event arguments.</param>
            /// <param name="editPriority">The edit priority for the asset.</param>
            e.Edit(
                asset =>
                {
                    var data = asset.AsDictionary<string, MachineData>().Data;
                    MachineEditor.EditOtherIncubators(data);
                },
                editPriority
            );
        }

        internal void AddItems(AssetRequestedEventArgs e, AssetEditPriority editPriority)
        {
            /// <summary>
            /// Adds items to the game assets.
            /// </summary>
            /// <param name="e">The asset requested event arguments.</param>
            /// <param name="editPriority">The edit priority for the asset.</param>
            e.Edit(
                asset =>
                {
                    var data = asset.AsDictionary<string, ObjectData>().Data;
                    ObjectEditor.AddItems(data);
                },
                editPriority
            );
        }

        internal void EditWildEgg(AssetRequestedEventArgs e, AssetEditPriority editPriority)
        {
            /// <summary>
            /// Adds items to the game assets.
            /// </summary>
            /// <param name="e">The asset requested event arguments.</param>
            /// <param name="editPriority">The edit priority for the asset.</param>
            e.Edit(
                asset =>
                {
                    var data = asset.AsDictionary<string, ObjectData>().Data;
                    ObjectEditor.EditWildEgg(data);
                },
                editPriority
            );
        }

        internal void AddMail(AssetRequestedEventArgs e, AssetEditPriority editPriority)
        {
            /// <summary>
            /// Adds mail to the game assets.
            /// </summary>
            /// <param name="e">The asset requested event arguments.</param>
            /// <param name="editPriority">The edit priority for the asset.</param>
            e.Edit(
                asset =>
                {
                    var data = asset.AsDictionary<string, string>().Data;
                    MailEditor.AddMail(data);
                },
                editPriority
            );
        }

        internal void AddQuests(AssetRequestedEventArgs e, AssetEditPriority editPriority)
        {
            /// <summary>
            /// Adds quests to the game assets.
            /// </summary>
            /// <param name="e">The asset requested event arguments.</param>
            /// <param name="editPriority">The edit priority for the asset.</param>
            e.Edit(
                asset =>
                {
                    var data = asset.AsDictionary<string, string>().Data;
                    QuestEditor.AddQuests(data);
                },
                editPriority
            );
        }

        internal void AddSpecialOrders(AssetRequestedEventArgs e, AssetEditPriority editPriority)
        {
            /// <summary>
            /// Adds special orders to the game assets.
            /// </summary>
            /// <param name="e">The asset requested event arguments.</param>
            /// <param name="editPriority">The edit priority for the asset.</param>
            e.Edit(
                asset =>
                {
                    var data = asset.AsDictionary<string, SpecialOrderData>().Data;
                    SpecialOrderEditor.AddSpecialOrders(data);
                },
                editPriority
            );
        }

        internal void ConfigEdits()
        {
            /// <summary>
            /// Configures edits for various game assets.
            /// </summary>
            CraftingRecipeEditor.EditCraftingRecipesAsset();
            MachineEditor.EditWildEggIncubatorRules();
            SpecialOrderEditor.ChangeAncientItemRequired();
        }

        internal static void PopulateIfEmpty<T>(Dictionary<string, T> dic, Action populateMethod)
        {
            if (dic.Count == 0)
            {
                populateMethod();
            }
        }

        // internal void TryToAdd<T>(Dictionary<string, T> dic, IDictionary<string, T> data)
        // {
        //     foreach (var (key, value) in dic)
        //     {
        //         var requiredStep = _progressionManager.GetProgressionStep(key);
        //         _progressionManager.ExecuteIfProgressionAllows(
        //             requiredStep,
        //             () => data[key] = value
        //         );
        //     }
        // }
        internal static void TryToAdd<T>(
            Dictionary<string, T> dic,
            IDictionary<string, T> data,
            ProgressionManager progressionManager
        )
        {
            foreach (var (key, value) in dic)
            {
                // Log the action of adding the value to the data dictionary
                ModEntry.LoggerHelper.VerboseLog($"Adding key '{key}' to data.");
                data[key] = value;
            }
        }

        internal static void SetQuestCompletionDay(
            string questID,
            ModData modSaveData,
            string mailID = "",
            bool noLetter = false,
            int? dayToSet = null
        )
        {
            /// <summary>
            /// Sets the completion day for a quest.
            /// </summary>
            /// <param name="questID">The ID of the quest.</param>
            /// <param name="mailID">The ID of the mail associated with the quest.</param>
            /// <param name="noLetter">Whether to send a letter or not.</param>
            QuestEditor.SetQuestCompletionDay(questID, modSaveData, mailID, noLetter, dayToSet);
        }

        internal static bool HasMail(string mailID, MailType mailType = MailType.Received)
        {
            /// <summary>
            /// Checks if the player has a specific mail.
            /// </summary>
            /// <param name="mailID">The ID of the mail.</param>
            /// <param name="mailType">The type of mail.</param>
            /// <returns>True if the player has the mail, false otherwise.</returns>
            return MailEditor.HasMail(mailID, mailType);
        }

        internal static bool AddMailToPlayer(
            string mailID,
            MailType mailType,
            bool isFlag = false,
            bool condition = true
        )
        {
            /// <summary>
            /// Adds mail to the player.
            /// </summary>
            /// <param name="mailID">The ID of the mail.</param>
            /// <param name="mailType">The type of mail.</param>
            /// <param name="isFlag">Whether the mail is a flag or not.</param>
            /// <param name="condition">The condition for adding the mail.</param>
            /// <returns>True if the mail was added, false otherwise.</returns>
            return MailEditor.AddMailToPlayer(mailID, mailType, isFlag, condition);
        }

        internal static void RemoveMailFromPlayer(string mailID)
        {
            /// <summary>
            /// Removes mail from the player.
            /// </summary>
            /// <param name="mailID">The ID of the mail.</param>
            MailEditor.RemoveMailFromPlayer(mailID);
        }

        internal static string GetDisplayName(string id, bool forShop = false)
        {
            /// <summary>
            /// Gets the display name for a farm animal.
            /// </summary>
            /// <param name="id">The ID of the farm animal.</param>
            /// <param name="forShop">Whether the display name is for the shop or not.</param>
            /// <returns>The display name of the farm animal.</returns>
            return FarmAnimalEditor.GetDisplayName(id, forShop);
        }

        internal static string GetShopDescription(string id)
        {
            /// <summary>
            /// Gets the shop description for a farm animal.
            /// </summary>
            /// <param name="id">The ID of the farm animal.</param>
            /// <returns>The shop description of the farm animal.</returns>
            return FarmAnimalEditor.GetShopDescription(id);
        }

        internal static bool HasItem(string itemId)
        {
            /// <summary>
            /// Checks if the player has a specific item.
            /// </summary>
            /// <param name="itemId">The ID of the item.</param>
            /// <returns>True if the player has the item, false otherwise.</returns>
            return ObjectEditor.HasItem(itemId);
        }

        internal static bool HasItemOnThisLocation(GameLocation location, string itemId)
        {
            /// <summary>
            /// Checks if the player has a specific item on a specific location.
            /// </summary>
            /// <param name="location">The location to check.</param>
            /// <param name="itemId">The ID of the item.</param>
            /// <returns>True if the player has the item on the location, false otherwise.</returns>
            return ObjectEditor.HasItemOnThisLocation(location, itemId);
        }

        internal static bool HasCompletedQuest(string questID)
        {
            /// <summary>
            /// Checks if the player has completed a specific quest.
            /// </summary>
            /// <param name="questID">The ID of the quest.</param>
            /// <returns>True if the player has completed the quest, false otherwise.</returns>
            return QuestEditor.HasCompletedQuest(questID);
        }

        internal static void SetCompletionDayBasedOnCompletionStatus(
            string questID,
            Dictionary<string, int?> questCompletedDay,
            int? dayToSet
        )
        {
            /// <summary>
            /// Checks if the player has completed a specific quest.
            /// /// </summary>
            /// <param name="questID">The ID of the quest.</param>
            /// <returns>True if the player has completed the quest, false otherwise.</returns>
            QuestEditor.SetCompletionDayBasedOnCompletionStatus(
                questID,
                questCompletedDay,
                dayToSet
            );
        }

        internal static bool HasEnoughDaysPassed(int? initialDay, int daysToPass)
        {
            /// <summary>
            /// Checks if enough days have passed since a specific initial day.
            /// </summary>
            /// <param name="initialDay">The initial day.</param>
            /// <param name="daysToPass">The number of days to pass.</param>
            /// <returns>True if enough days have passed, false otherwise.</returns>
            return initialDay != null
                && QuestEditor.HasEnoughDaysPassed(initialDay.Value, daysToPass);
        }

        internal static bool HasCompletedSpecialOrder(string specialOrderID)
        {
            /// <summary>
            /// Checks if the player has completed a specific special order.
            /// </summary>
            /// <param name="specialOrderID">The ID of the special order.</param>
            /// <returns>True if the player has completed the special order, false otherwise.</returns>
            return SpecialOrderEditor.HasCompletedSpecialOrder(specialOrderID);
        }
    }
}
