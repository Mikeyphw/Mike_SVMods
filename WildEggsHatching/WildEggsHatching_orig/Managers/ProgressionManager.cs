using System.Text.RegularExpressions;
using Mikew.BirdsOfAFeather.Helpers.GameStateQueries;
using StardewValley;
using WildEggsHatching.Commands;
using WildEggsHatching.Framework;
using WildEggsHatching.Managers;
using WildEggsHatching.Managers.GameAssetManagement;
using WildEggsHatching.Models;
using WildEggsHatching.Services;
using static WildEggsHatching.EnumsAndStrings.Strings.IDs;
using static WildEggsHatching.ModEntry;

namespace WildEggsHatching.Management
{
    internal class ProgressionManager
    {
        internal readonly ModData ModSaveData;
        internal const string AnimalDataKey = "wildEggs-animalData";

        private readonly NewAnimalMonitoring NewAnimalMonitoring;
        private readonly NoteSpawning NoteSpawning;
        private readonly WildListManager _wildAnimalManager;
        internal static bool HasScrollOrCompletedQuest =>
            GameAssetManager.HasCompletedQuest(Quests.TheDesertScroll)
            || GameAssetManager.HasItem(Objects.OsirisScroll);

        internal enum ProgressionStep
        {
            /// <summary>
            /// The initial step of the progression.
            /// </summary>
            PartI,

            /// <summary>
            /// It starts when player has completed the first special order.
            /// And consists of the player delivering a large egg to the wizard for him to help studying how to hatch it.
            /// </summary>
            PartII,

            /// <summary>
            /// It starts after a few days has passed after the player has delivered the large egg to the Wizard.
            /// And consists of the player completing the second special order.
            /// </summary>
            PartIII,

            /// <summary>
            /// It starts after a few days has passed after the player has completed the second special order.
            /// And consists of the player getting the osiris scroll.
            /// </summary>
            PartIV,

            /// <summary>
            /// It starts after a few days have passed since the player has delivered the osiris scroll.
            /// And consists of the wizard giving the recipe of the elixir.
            /// </summary>
            PartV,

            /// <summary>
            /// The final step of the progression. It starts the day after the player has received the Wizard thanks and the elixir recipe.
            /// It consists of Marnie giving the recipe of the incubator.
            /// </summary>
            PartVI,
        }

        internal ProgressionManager(WildListManager wildAnimalManager)
        {
            ModSaveData = new();
            _wildAnimalManager = wildAnimalManager;
            NoteSpawning = new(ModSaveData);
            NewAnimalMonitoring = new(
                ModSaveData,
                _wildAnimalManager,
                ModEntry.Config.AnimalProgressionMode
            );
        }

        internal void OnDayStart()
        {
            // Load Save Data
            LoadSaveData();

            // Check if there are any animals that need notes spawned
            ExecuteIfProgressionAllows(ProgressionStep.PartVI, () => ProcessHatchedAnimals());

            // Update quest completion days if necessary
            SetQuestCompletionDays();

            // Check and unlock the next progression step if applicable
            UnlockNextStep();

            // Log the current progression step
            LoggerHelper.VerboseLog(
                "New day has started. Current progression step: " + GetCurrentStep()
            );
        }

        internal void OnDayEnding()
        {
            SaveModData();
        }

        internal void SaveModData()
        {
            HelperInstance.Data.WriteSaveData(AnimalDataKey, ModSaveData);
        }

        internal void UnlockNextStep()
        {
            // Get the current progression step
            var currentStep = GetCurrentStep();

            // Determine the next step
            ProgressionStep nextStep = currentStep + 1;

            // Check if the next step is valid and unlocked
            if (nextStep <= ProgressionStep.PartVI && IsStepUnlocked(nextStep))
            {
                UnlockStep(nextStep);
                LoggerHelper.VerboseLog($"Unlocked step: {nextStep}");
            }
        }

        private void LoadSaveData()
        {
            ModSaveData.Update(_wildAnimalManager.LoadedAnimalsKeys);
        }

        internal bool IsStepUnlocked(ProgressionStep step)
        {
            LoggerHelper.VerboseLog($"Checking if step {step} is unlocked.");
            if (!ModEntry.Config.ProgressionMode)
                return true;

            bool result = step switch
            {
                ProgressionStep.PartI => true,
                ProgressionStep.PartII => GameAssetManager.HasMail(Mail.SpecialOrderDoneI),
                ProgressionStep.PartIII => CheckQuestsOrOrderAndDaysPassed(
                    Quests.WizardStudy,
                    false
                ),
                ProgressionStep.PartIV => CheckQuestsOrOrderAndDaysPassed(
                    Quests.SpecialOrderWildEggsII,
                    true
                ),
                ProgressionStep.PartV => CheckQuestsOrOrderAndDaysPassed(
                    Quests.TheDesertScroll,
                    false
                ),
                ProgressionStep.PartVI => CheckQuestsOrOrderAndDaysPassed(
                    Quests.TheDesertScroll,
                    false,
                    GetDaysToWait(Quests.TheDesertScroll) + 2
                ),
                _ => false,
            };

            LoggerHelper.VerboseLog($"IsStepUnlocked for {step}: {result}");
            return result;
        }

        private bool CheckQuestsOrOrderAndDaysPassed(
            string questOrOrderID,
            bool isSpecialOrder,
            int? daysToPass = null
        )
        {
            LoggerHelper.VerboseLog($"Checking quests or orders for ID: {questOrOrderID}");
            static bool specialOrderCheck(string id) =>
                GameAssetManager.HasCompletedSpecialOrder(id);
            static bool questCheck(string id) => GameAssetManager.HasCompletedQuest(id);

            var completedQuestOrOrder = isSpecialOrder
                ? specialOrderCheck(questOrOrderID)
                : questCheck(questOrOrderID);

            int daysToWait = daysToPass ?? GetDaysToWait(questOrOrderID);

            bool result =
                completedQuestOrOrder
                && GameAssetManager.HasEnoughDaysPassed(
                    ModSaveData.QuestCompletedDay[questOrOrderID],
                    daysToWait
                );

            LoggerHelper.VerboseLog(
                $"CheckQuestsOrOrderAndDaysPassed for {questOrOrderID}: {result}"
            );
            return result;
        }

        internal void GoToPart(ProgressionStep progressionStep)
        {
            LoggerHelper.Log($"Entering GoToPart with progressionStep: {progressionStep}");

            var (mailsToAdd, mailsToRemove, questsToAdd, questsToRemove) = GetMailAndQuestChanges(
                progressionStep
            );

            // LoggerHelper.Log($"Mails to add: {string.Join(", ", mailsToAdd)}");
            // LoggerHelper.Log($"Mails to remove: {string.Join(", ", mailsToRemove)}");
            // LoggerHelper.Log($"Quests to add: {string.Join(", ", questsToAdd)}");
            // LoggerHelper.Log($"Quests to remove: {string.Join(", ", questsToRemove)}");

            foreach (var mail in mailsToAdd)
            {
                LoggerHelper.Log($"Adding mail: {mail}");
                GameAssetManager.AddMailToPlayer(mail, MailType.Received, IsMailFlag(mail));
            }

            foreach (var mail in mailsToRemove)
            {
                LoggerHelper.Log($"Removing mail: {mail}");
                GameAssetManager.RemoveMailFromPlayer(mail);
            }

            foreach (var quest in questsToAdd)
            {
                LoggerHelper.Log($"Adding quest: {quest}");
                if (quest == Quests.SpecialOrderWildEggsI || quest == Quests.SpecialOrderWildEggsII)
                {
                    Game1.player.team.specialOrders.RemoveWhere(o => o.questKey.Value == quest);
                    Game1.player.team.completedSpecialOrders.Add(quest);
                    SetQuestSOCompletionDay(quest);
                }
                else
                {
                    Game1.player.completeQuest(quest);
                    SetQuestSOCompletionDay(quest);
                    var completionDay = ModSaveData.QuestCompletedDay[quest];
                    Game1.player.activeDialogueEvents.TryAdd(
                        $"questComplete_{quest}",
                        completionDay ?? 0
                    );
                }
            }

            foreach (var quest in questsToRemove)
            {
                LoggerHelper.Log($"Removing quest: {quest}");
                if (quest == Quests.SpecialOrderWildEggsI || quest == Quests.SpecialOrderWildEggsII)
                {
                    Game1.player.team.specialOrders.RemoveWhere(o => o.questKey.Value == quest);
                    Game1.player.team.completedSpecialOrders.Remove(quest);
                }
                else
                {
                    Game1.player.questLog.RemoveWhere(q => q.id.Value == quest);
                    Game1.player.activeDialogueEvents?.RemoveWhere(x =>
                        x.Key.StartsWith($"questComplete_{quest}")
                    );
                    Game1.player.previousActiveDialogueEvents?.RemoveWhere(x =>
                        x.Key.StartsWith($"questComplete_{quest}")
                    );
                }
            }

            FinalStepRevert();
            LoggerHelper.Log($"Exiting GoToPart for progressionStep: {progressionStep}");
        }

        private void SetQuestSOCompletionDay(string entryID)
        {
            var currentDay = Game1.Date.TotalDays;
            if (
                ModSaveData.QuestCompletedDay.ContainsKey(entryID)
                    && GameAssetManager.HasEnoughDaysPassed(
                        ModSaveData.QuestCompletedDay[entryID],
                        GetDaysToWait(entryID)
                    )
                || !ModSaveData.QuestCompletedDay.ContainsKey(entryID)
            )
            {
                ModSaveData.QuestCompletedDay[entryID] = currentDay - GetDaysToWait(entryID);
            }
        }

        private void FinalStepRevert()
        {
            string notesCombinedPattern =
                @"[^""]*junimoNote_[^""]+|wildEggs_[^""]+Note|wildEggs_NoteMail_[^""]+";
            Game1.player.questLog.RemoveWhere(q => Regex.IsMatch(q.id.Value, notesCombinedPattern));
            Game1.player.activeDialogueEvents?.RemoveWhere(x =>
                Regex.IsMatch(x.Key, notesCombinedPattern)
            );
            Game1.player.previousActiveDialogueEvents?.RemoveWhere(x =>
                Regex.IsMatch(x.Key, notesCombinedPattern)
            );

            Game1.player.team.specialOrders.RemoveWhere(o =>
                Regex.IsMatch(o.questKey.Value, notesCombinedPattern)
            );
            Game1.player.team.completedSpecialOrders.RemoveWhere(x =>
                Regex.IsMatch(x, notesCombinedPattern)
            );
            Game1.player.mailForTomorrow.RemoveWhere(x => Regex.IsMatch(x, notesCombinedPattern));
            Game1.player.mailbox.RemoveWhere(x => Regex.IsMatch(x, notesCombinedPattern));
            Game1.player.mailReceived.RemoveWhere(x => Regex.IsMatch(x, notesCombinedPattern));
            ModSaveData.HatchedAnimals.Clear();
            ModSaveData.NewAddedAnimals.Clear();
            CustomCommands.ClearNotesCommand();
        }

        internal void UnlockStep(ProgressionStep step)
        {
            LoggerHelper.VerboseLog($"Unlocking step: {step}");
            switch (step)
            {
                case ProgressionStep.PartII:
                    GameAssetManager.AddMailToPlayer(Mail.MarnieThanks, MailType.Mailbox, false);
                    LoggerHelper.VerboseLog("Unlocked Part II.");
                    break;
                case ProgressionStep.PartIII:
                    GameAssetManager.AddMailToPlayer(Mail.WizardDone, MailType.Received, true);
                    LoggerHelper.VerboseLog("Unlocked Part III.");
                    break;
                case ProgressionStep.PartIV:
                    GameAssetManager.AddMailToPlayer(Mail.WizardThanks, MailType.Received, false);
                    LoggerHelper.VerboseLog("Unlocked Part IV.");
                    break;
                case ProgressionStep.PartV:
                    GameAssetManager.AddMailToPlayer(Mail.WizardFindings, MailType.Received, false);
                    LoggerHelper.VerboseLog("Unlocked Part V.");
                    break;
                case ProgressionStep.PartVI:
                    GameAssetManager.AddMailToPlayer(
                        Mail.MarnieGotIncubator,
                        MailType.Received,
                        true
                    );
                    LoggerHelper.VerboseLog("Unlocked Part VI.");
                    break;
            }
        }

        internal ProgressionStep GetCurrentStep()
        {
            LoggerHelper.VerboseLog("Getting the current progression step.");
            if (IsStepUnlocked(ProgressionStep.PartVI))
            {
                LoggerHelper.VerboseLog("Current step is VI.");
                return ProgressionStep.PartVI;
            }
            if (IsStepUnlocked(ProgressionStep.PartIV))
            {
                LoggerHelper.VerboseLog("Current step is Part IV.");
                return ProgressionStep.PartIV;
            }
            if (IsStepUnlocked(ProgressionStep.PartIII))
            {
                LoggerHelper.VerboseLog("Current step is Part III.");
                return ProgressionStep.PartIII;
            }
            if (IsStepUnlocked(ProgressionStep.PartII))
            {
                LoggerHelper.VerboseLog("Current step is Part II.");
                return ProgressionStep.PartII;
            }
            LoggerHelper.VerboseLog("Current step is Part I.");
            return ProgressionStep.PartI;
        }

        /* internal ProgressionStep GetProgressionStep(string id)
         {
             return ProgressionStep.PartI;
             var progressionStepNeeded = new Dictionary<string, ProgressionStep>()
             {
                 { BigCraftables.WildEggIncubator, ProgressionStep.PartV },
                 { BigCraftables.WEIncubatorCrafting, ProgressionStep.PartV },
                 { Objects.ElixirCrafting, ProgressionStep.PartIV },
                 { $"(BC){BigCraftables.WildEggIncubator}", ProgressionStep.PartV },
                 { Mail.MarnieThanks, ProgressionStep.PartI },
                 { Mail.WizardThanks, ProgressionStep.PartI },
                 { Mail.WizardFindings, ProgressionStep.PartIV },
                 { Mail.MarnieGotIncubator, ProgressionStep.PartV },
                 { Objects.FarmLWEgg, ProgressionStep.PartI },
                 { Objects.Elixir, ProgressionStep.PartIV },
                 { Objects.FarmMWEgg, ProgressionStep.PartVI },
                 { Objects.FarmLMWEgg, ProgressionStep.PartVI },
                 { Objects.OsirisScroll, ProgressionStep.PartIII },
                 { Quests.WizardStudy, ProgressionStep.PartI },
                 { Quests.TheDesertScroll, ProgressionStep.PartIII },
                 { Quests.SpecialOrderWildEggsI, ProgressionStep.PartI },
                 { Quests.SpecialOrderWildEggsII, ProgressionStep.PartIII },
             };
             string notesCombinedPattern =
                 @"[^""]*junimoNote_[^""]+|wildEggs_[^""]+Note|wildEggs_NoteMail_[^""]+";
 
             if (progressionStepNeeded.TryGetValue(id, out var progressionStep))
             {
                 return progressionStep;
             }
             if (Regex.IsMatch(id, notesCombinedPattern))
             {
                 return ProgressionStep.PartVI;
             }
             return ProgressionStep.PartI;
         }*/

        private readonly Dictionary<string, bool> mailFlags = new()
        {
            { Mail.SpecialOrderDoneI, true },
            { Mail.MarnieThanks, false },
            { Mail.WizardDone, true },
            { Mail.SpecialOrderDoneII, true },
            { Mail.WizardThanks, false },
            { Mail.WizardFindings, false },
            { Mail.MarnieGotIncubator, false },
        };

        private bool IsMailFlag(string mail)
        {
            if (mailFlags.TryGetValue(mail, out bool isFlag))
            {
                return isFlag;
            }
            return true;
        }

        private readonly Dictionary<string, int> DaysToWait = new()
        {
            { Quests.WizardStudy, 2 },
            { Quests.SpecialOrderWildEggsII, 3 },
            { Quests.TheDesertScroll, 3 },
        };

        private int GetDaysToWait(string mail)
        {
            if (DaysToWait.TryGetValue(mail, out int days))
            {
                return days;
            }
            return 0;
        }

        internal (
            List<string> mailsToAdd,
            List<string> mailsToRemove,
            List<string> questsToAdd,
            List<string> questsToRemove
        ) GetMailAndQuestChanges(ProgressionStep desiredStep)
        {
            var mailsToAdd = new List<string>();
            var mailsToRemove = new List<string>();
            var questsToAdd = new List<string>();
            var questsToRemove = new List<string>();

            // Define the mails and quests that should be present at each step
            var progressionMails = new Dictionary<ProgressionStep, List<string>>()
            {
                { ProgressionStep.PartI, new List<string>() }, // No mails in Part I
                {
                    ProgressionStep.PartII,
                    new List<string> { Mail.SpecialOrderDoneI, Mail.MarnieThanks }
                },
                {
                    ProgressionStep.PartIII,
                    new List<string> { Mail.WizardDone, Mail.WizardFindings }
                },
                {
                    ProgressionStep.PartIV,
                    new List<string> { Mail.SpecialOrderDoneII, Mail.WizardThanks }
                },
                {
                    ProgressionStep.PartV,
                    new List<string> { Mail.WizardFindings }
                },
                {
                    ProgressionStep.PartVI,
                    new List<string> { Mail.MarnieGotIncubator }
                },
            };

            var progressionQuests = new Dictionary<ProgressionStep, List<string>>()
            {
                { ProgressionStep.PartI, new List<string>() }, // No quests in Part I
                {
                    ProgressionStep.PartII,
                    new List<string> { Quests.SpecialOrderWildEggsI }
                },
                {
                    ProgressionStep.PartIII,
                    new List<string> { Quests.WizardStudy }
                },
                {
                    ProgressionStep.PartIV,
                    new List<string> { Quests.SpecialOrderWildEggsII }
                },
                {
                    ProgressionStep.PartV,
                    new List<string> { Quests.TheDesertScroll }
                },
            };

            // Add mails from all steps up to and including the desired step
            for (var step = ProgressionStep.PartI; step <= desiredStep; step++)
            {
                if (progressionMails.ContainsKey(step))
                {
                    mailsToAdd.AddRange(progressionMails[step]);
                }
            }

            // Add quests from all steps up to and including the desired step
            for (var step = ProgressionStep.PartI; step <= desiredStep; step++)
            {
                if (progressionQuests.ContainsKey(step))
                {
                    questsToAdd.AddRange(progressionQuests[step]);
                }
            }

            // Remove mails from all steps beyond the desired step
            for (var step = desiredStep + 1; step <= ProgressionStep.PartVI; step++)
            {
                if (progressionMails.ContainsKey(step))
                {
                    mailsToRemove.AddRange(progressionMails[step]);
                }
            }

            // Remove quests from all steps beyond the desired step
            for (var step = desiredStep + 1; step <= ProgressionStep.PartVI; step++)
            {
                if (progressionQuests.ContainsKey(step))
                {
                    questsToRemove.AddRange(progressionQuests[step]);
                }
            }

            // Return the lists of mails and quests to add and remove
            return (
                mailsToAdd.Distinct().ToList(),
                mailsToRemove.Distinct().ToList(),
                questsToAdd.Distinct().ToList(),
                questsToRemove.Distinct().ToList()
            );
        }

        internal void ExecuteIfProgressionAllows(ProgressionStep requiredStep, Action action)
        {
            LoggerHelper.VerboseLog(
                $"Executing action if progression allows for step: {requiredStep}"
            );
            if (IsStepUnlocked(requiredStep))
            {
                LoggerHelper.VerboseLog($"Step {requiredStep} is unlocked. Executing action.");
                action();
            }
            else
            {
                LoggerHelper.VerboseLog(
                    $"Step {requiredStep} is not unlocked. Action will not be executed."
                );
            }
        }

        internal void ProcessHatchedAnimals()
        {
            LoggerHelper.VerboseLog("Processing hatched animals.");
            var animalsNeedingNotes = NewAnimalMonitoring.GetAnimalsNeedingNotes();
            LoggerHelper.VerboseLog($"Found {animalsNeedingNotes.Count} animals needing notes.");

            foreach (var animal in animalsNeedingNotes)
            {
                SpawnNoteForAnimal(animal);
            }
        }

        internal void SpawnNoteForAnimal(ChosenAnimal animal)
        {
            LoggerHelper.VerboseLog($"Spawning note for animal: {animal.FarmAnimalName}");
            NoteSpawning.Spawn(animal);
            animal.NoteSpawned = true;
            LoggerHelper.VerboseLog(
                $"Note spawned for animal: {animal.FarmAnimalName}. NoteSpawned set to true."
            );
        }

        internal void SetQuestCompletionDays()
        {
            LoggerHelper.VerboseLog("Setting quest completion days.");
            foreach (var pair in QuestMailPairs)
            {
                LoggerHelper.VerboseLog(
                    $"Setting completion day for quest: {pair.Key}, mail ID: {pair.Value}"
                );
                SetQuestCompletionDay(pair.Key, pair.Value, string.IsNullOrEmpty(pair.Value));
            }
        }

        private Dictionary<string, string> QuestMailPairs =>
            new()
            {
                { Quests.SpecialOrderWildEggsI, Mail.SpecialOrderDoneI },
                { Quests.WizardStudy, "" },
                { Quests.TheDesertScroll, "" },
                { Quests.SpecialOrderWildEggsII, Mail.SpecialOrderDoneII },
            };

        private void SetQuestCompletionDay(
            string questID,
            string mailID,
            bool noLetter,
            int? dayToSet = null
        )
        {
            LoggerHelper.VerboseLog(
                $"Setting quest completion day for quest ID: {questID}, mail ID: {mailID}, noLetter: {noLetter}, dayToSet: {dayToSet}"
            );
            if (
                !ModSaveData.QuestCompletedDay.ContainsKey(questID)
                || ModSaveData.QuestCompletedDay[questID] == null
            )
            {
                GameAssetManager.SetQuestCompletionDay(
                    questID,
                    ModSaveData,
                    mailID,
                    noLetter,
                    dayToSet
                );
                LoggerHelper.VerboseLog($"Quest completion day set for quest ID: {questID}");
            }
            else
            {
                LoggerHelper.VerboseLog(
                    $"Quest ID {questID} already has a completion day set. No action taken."
                );
            }
        }
    }
}
