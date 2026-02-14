using StardewValley;
using Mikew.BirdsOfAFeather.Helpers;
using WildEggsHatching.Framework;
using WildEggsHatching.Helpers;
using WildEggsHatching.Models;
using static WildEggsHatching.EnumsAndStrings.Strings;
using static WildEggsHatching.ModEntry;

namespace WildEggsHatching.Managers.GameAssetManagement
{
    internal class QuestEditor
    {
        private readonly ModData _modSaveData;
        internal readonly Dictionary<string, string> QuestsToAdd;
        internal readonly List<string> AllQuests = new()
        {
            IDs.Quests.WizardStudy,
            IDs.Quests.SpecialOrderWildEggsI,
        };

        internal enum QuestType
        {
            Location,
            Basic,
            ItemHarvest,
            ItemDelivery,
            Building,
            Crafting,
            Social,
            Monster,
            Fishing,
            LostItem,
        }

        internal QuestEditor(ModData modData)
        {
            _modSaveData = modData;
            QuestsToAdd = new();
        }

        private void CreateQuests()
        {
            QuestsToAdd[IDs.Quests.WizardStudy] = CreateWizardStudyQuest();
            QuestsToAdd[IDs.Quests.TheDesertScroll] = CreateScrollQuest();
        }

        private QuestHelper CreateAnimalQuest(ChosenAnimal animal)
        {
            var inGameDisplayName = GameAssetManager.GetDisplayName(animal.FarmAnimalName);
            var inGameShopDescription = GameAssetManager.GetShopDescription(animal.FarmAnimalName);
            var inGamePrice = Game1.farmAnimalData[animal.FarmAnimalName].PurchasePrice;
            var quest = new QuestHelper
            {
                ID = animal.QuestID,
                Type = QuestType.ItemDelivery,
                Cancellable = false,
                Gold = inGamePrice,
                Hint = $"{TranslationHelper.Get("quest.NoteDelivery.hint")}: {inGameDisplayName}.",
                Title = $"{TranslationHelper.Get("quest.NoteDelivery.title")}: {inGameDisplayName}",
                Solution = $"Marnie {animal.NoteID}",
                QuestDetails =
                    $"{TranslationHelper.Get("quest.NoteDelivery.questDetails")}: {inGameDisplayName}.",
                ReactionText =
                    $"{TranslationHelper.Get($"quest.NoteDelivery.reactionText{Game1.random.Next(4)}")}",
            };
            if (!string.IsNullOrEmpty(inGameShopDescription))
            {
                quest.ReactionText +=
                    $"{TranslationHelper.Get("quest.NoteDelivery.reactionText_AnimalInfo")} {inGameShopDescription}";
            }
            _modSaveData.QuestCompletedDay.TryAdd(animal.QuestID, null);
            return quest;
        }

        private string CreateWizardStudyQuest()
        {
            var quest = new QuestHelper
            {
                ID = IDs.Quests.WizardStudy,
                Type = QuestType.ItemDelivery,
                Cancellable = false,
                Gold = 0,
                Hint = TranslationHelper.Get("quest.WizardStudy.hint"),
                Title = TranslationHelper.Get("quest.WizardStudy.title"),
                Solution = $"Wizard {IDs.Objects.FarmLWEgg}",
                QuestDetails = TranslationHelper.Get("quest.WizardStudy.questDetails"),
                ReactionText = TranslationHelper.Get("quest.WizardStudy.reactionText"),
            };
            string questString = quest.GetQuestString();
            LoggerHelper.VerboseLog($"Created Wizard Study Quest: {questString}");
            return questString;
        }

        private string CreateScrollQuest()
        {
            var quest = new QuestHelper
            {
                ID = IDs.Quests.TheDesertScroll,
                Type = QuestType.ItemDelivery,
                Cancellable = false,
                Gold = 500,
                Hint = TranslationHelper.Get("quest.theDesertScroll.hint"),
                Title = TranslationHelper.Get("quest.theDesertScroll.title"),
                Solution = $"Wizard {IDs.Objects.OsirisScroll}",
                QuestDetails = TranslationHelper.Get("quest.theDesertScroll.questDetails"),
                ReactionText = TranslationHelper.Get("quest.theDesertScroll.reactionText"),
            };

            string questString = quest.GetQuestString();
            LoggerHelper.VerboseLog($"Created Desert Scroll Quest: {questString}");
            return questString;
        }

        internal static void SetQuestCompletionDay(
            string questID,
            ModData modSaveData,
            string mailID = "",
            bool noLetter = false,
            int? dayToSet = null
        )
        {
            // Log the initial parameters received by the method
            LoggerHelper.VerboseLog(
                $"SetQuestCompletionDay called with parameters: questID={questID}, mailID={mailID}, noLetter={noLetter}, dayToSet={dayToSet}"
            );

            // Set the dayToSet to the current game day if it is null
            dayToSet ??= Game1.Date.TotalDays;
            LoggerHelper.VerboseLog($"Determined dayToSet: {dayToSet}");

            // Check if the questID exists in the modSaveData
            if (
                !modSaveData.QuestCompletedDay.ContainsKey(questID)
                || modSaveData.QuestCompletedDay[questID] == null
            )
            {
                LoggerHelper.VerboseLog(
                    $"Quest ID {questID} is either not completed or has not been set yet."
                );

                // Set the completion day based on the presence of mail or completion status
                if (noLetter)
                {
                    SetCompletionDayBasedOnCompletionStatus(
                        questID,
                        modSaveData.QuestCompletedDay,
                        dayToSet
                    );
                }
                else
                {
                    SetCompletionDayBasedOnMailPresence(
                        questID,
                        modSaveData.QuestCompletedDay,
                        mailID,
                        dayToSet
                    );
                }
            }
            else
            {
                LoggerHelper.VerboseLog(
                    $"Quest ID {questID} has already been completed on day {modSaveData.QuestCompletedDay[questID]}."
                );
            }

            LoggerHelper.VerboseLog(
                $"Final completion day for quest ID {questID} is {modSaveData.QuestCompletedDay[questID]}."
            );
        }

        internal static void SetCompletionDayBasedOnCompletionStatus(
            string questID,
            Dictionary<string, int?> questCompletedDay,
            int? dayToSet
        )
        {
            bool completed = HasCompletedQuest(questID);
            questCompletedDay[questID] = completed ? dayToSet : null;
            LoggerHelper.VerboseLog(
                $"Set completion day for quest ID {questID} to {questCompletedDay[questID]} based on completion status: {completed}"
            );
        }

        private static void SetCompletionDayBasedOnMailPresence(
            string questID,
            Dictionary<string, int?> questCompletedDay,
            string mailID,
            int? dayToSet
        )
        {
            bool hasMail = GameAssetManager.HasMail(mailID);
            questCompletedDay[questID] = hasMail ? dayToSet : null;
            LoggerHelper.VerboseLog(
                $"Set completion day for quest ID {questID} to {questCompletedDay[questID]} based on mail presence: {hasMail}"
            );
        }

        internal void AddQuests(IDictionary<string, string> data)
        {
            GameAssetManager.PopulateIfEmpty(QuestsToAdd, () => CreateQuests());
            CreateNoteQuests();
            data.TryAddMany(QuestsToAdd);
            LoggerHelper.VerboseLog("Quests added successfully.");
        }

        private void CreateNoteQuests()
        {
            if (_modSaveData.HatchedAnimals.Any())
            {
                foreach (
                    var animal in _modSaveData.HatchedAnimals.Where(x =>
                        !QuestsToAdd.ContainsKey(x.QuestID)
                    )
                )
                {
                    var NoteDelivery = CreateAnimalQuest(animal);
                    QuestsToAdd[NoteDelivery.ID] = NoteDelivery.GetQuestString();
                    if (!AllQuests.Contains(NoteDelivery.ID))
                    {
                        AllQuests.Add(NoteDelivery.ID);
                    }
                    LoggerHelper.VerboseLog(
                        $"Created Note Delivery Quest for {animal.FarmAnimalName}"
                    );
                }
            }
        }

        internal static bool HasCompletedQuest(string questID)
        {
            bool result =
                (
                    Game1.player.activeDialogueEvents != null
                    && Game1.player.activeDialogueEvents.ContainsKey($"questComplete_{questID}")
                )
                || (
                    Game1.player.previousActiveDialogueEvents != null
                    && Game1.player.previousActiveDialogueEvents.ContainsKey(
                        $"questComplete_{questID}"
                    )
                );
            LoggerHelper.VerboseLog($"Has completed quest: {questID} - {result}");
            return result;
        }

        internal static bool HasEnoughDaysPassed(int initialDay, int daysToPass)
        {
            int totalDays = Game1.Date.TotalDays;
            bool result = totalDays - initialDay >= daysToPass;
            LoggerHelper.VerboseLog(
                $"Has enough days passed: {initialDay} - {daysToPass} - {result}"
            );
            return result;
        }
    }
}
