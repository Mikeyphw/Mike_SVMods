using System.Text.RegularExpressions;
using Mikew.BirdsOfAFeather.Helpers.GameStateQueries;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using WildEggsHatching.Framework;
using WildEggsHatching.Management;
using WildEggsHatching.Managers;
using WildEggsHatching.Managers.GameAssetManagement;
using WildEggsHatching.Models;
using WildEggsHatching.Services;
using WildEggsHatching.UI;
using static WildEggsHatching.EnumsAndStrings.Strings;
using static WildEggsHatching.Management.ProgressionManager;
using static WildEggsHatching.ModEntry;

namespace WildEggsHatching.Commands
{
    internal class CustomCommands
    {
        private readonly WildListManager _wildAnimalManager;
        private readonly ProgressionManager _progressionManager;
        private readonly GMCMenu _gMCMenu;
        private readonly ModData _modSaveData;
        private readonly GameAssetManager _gameAssetManager;
        private EggSpawning? EggSpawningService;

        internal CustomCommands(
            WildListManager wildAnimalManager,
            GMCMenu gMCMenu,
            ProgressionManager progressionManager,
            GameAssetManager gameAssetManager
        )
        {
            _gMCMenu = gMCMenu;
            _progressionManager = progressionManager;
            _modSaveData = progressionManager.ModSaveData;
            _wildAnimalManager = wildAnimalManager;
            _gameAssetManager = gameAssetManager;
        }

        internal void SetEggSpawning(EggSpawning eggSpawning)
        {
            EggSpawningService = eggSpawning;
        }

        internal void AddCustomCommands()
        {
            ModEntry.HelperInstance.ConsoleCommands.Add(
                "wildeggs_lists",
                "Displays a list of wild animals in the game.\n\nUsage: wildeggs_lists <type>\n- type: 'all' to show all wild animals, 'valid' to show only valid wild animals.",
                PrintLists
            );
            ModEntry.HelperInstance.ConsoleCommands.Add(
                "wildeggs_printEggs",
                "Displays info about the wild eggs.\n\nUsage: wildeggs_printEggs.",
                PrintEggsInfo
            );
            ModEntry.HelperInstance.ConsoleCommands.Add(
                "wildeggs_reloadList",
                "Reloads the list of wild animals and refreshes the animal selection.\n\nUsage: wildeggs_reloadList",
                ReloadWildAnimalsList
            );
            ModEntry.HelperInstance.ConsoleCommands.Add(
                "wildeggs_clear",
                "Clears specific game data related to wild eggs.\n\nUsage: wildeggs_clear <type>\n- type: 'mail', 'quest', 'hatched', 'added', 'notes', or 'all' to clear all.",
                ClearCommands
            );
            ModEntry.HelperInstance.ConsoleCommands.Add(
                "wildeggs_si",
                "Adds specific mail related to wild eggs to the player's mailbox.\n\nUsage: wildeggs_addmail",
                PrintSpawnInfo
            );
            ModEntry.HelperInstance.ConsoleCommands.Add(
                "wildeggs_addmail",
                "Adds specific mail related to wild eggs to the player's mailbox.\n\nUsage: wildeggs_addmail",
                AddMail
            );
            ModEntry.HelperInstance.ConsoleCommands.Add(
                "wildeggs_clearanimaldata",
                "Clears the data for a specified animal, including its quest and any associated mail.\n\nUsage: wildeggs_clearanimaldata <animalKey>\n- animalKey: the name of the animal to clear data for.",
                ClearAnimalData
            );
            ModEntry.HelperInstance.ConsoleCommands.Add(
                "wildeggs_goToStep",
                "Sets the player to the desired progression step.\n\nUsage: wildeggs_goToStep",
                GoToProgressionStep
            );
            ModEntry.HelperInstance.ConsoleCommands.Add(
                "wildeggs_getCurrentStep",
                "Gets the player current progression step.\n\nUsage: wildeggs_getCurrentStep",
                GetCurrentProgressionStep
            );
            ModEntry.HelperInstance.ConsoleCommands.Add(
                "wildeggs_resetparam",
                "Reset spawn parameters.",
                ResetSpawnParameters
            );
            //debug

            /* ModEntry.HelperInstance.ConsoleCommands.Add(
                 "wildeggs_setspawnp",
                 "Sets the min eggs to spawn per pack.\n\nUsage: wildeggs_setspawnp  min eggs per area  min egg per pack  max egg per pack  baserandom chance.",
                 SetSpawnParameters
             );*/
        }

        /*
                private void SetSpawnParameters(string command, string[] args)
                {
                    if (EggSpawningService == null)
                    {
                        ModEntry.LoggerHelper.Log("Egg Spawning Service is null!", LogLevel.Warn);
                        return;
                    }
                    var validConfirmations = new string[] { "yes", "y", "yep", "true", "t" };
                    var validNegations = new string[] { "no", "n", "not", "nope", "false", "f" };
                    var validOptions = validConfirmations.Union(validNegations).ToArray();
                    // Ensure we have the correct number of arguments
                    if (args.Length != 7)
                    {
                        ModEntry.LoggerHelper.Log(
                            "Invalid number of parameters! Expected 7 parameters.",
                            LogLevel.Warn
                        );
                        return;
                    }
        
                    // Parse the parameters
                    if (
                        TryParseInt(args[0], out var minEggPerArea)
                        && TryParsePositiveInt(args[1], out var maxEggsPerArea)
                        && TryParseInt(args[2], out var minEggsPerPack)
                        && TryParsePositiveInt(args[3], out var maxEggsPerPack)
                        && TryParsePositiveInt(args[4], out var maxEggsPerMap)
                        && TryParsePositiveFloat(args[5], out var baseRandomChance)
                        && validOptions.Contains(args[6], StringComparer.OrdinalIgnoreCase)
                    )
                    {
                        EggSpawningService.SetPersistenceMode(
                            validConfirmations.Contains(args[6], StringComparer.OrdinalIgnoreCase)
                        );
        
                        // Save the parameters to the EggSpawningService
                        EggSpawningService.SaveMinEggArea(minEggPerArea);
                        EggSpawningService.SaveMaxEggArea(maxEggsPerArea);
                        EggSpawningService.SaveMaxEggMap(maxEggsPerMap);
                        EggSpawningService.SaveMinEggPack(minEggsPerPack);
                        EggSpawningService.SaveMaxEggPack(maxEggsPerPack);
        
                        // Clamp base random chance to a maximum of 1
                        baseRandomChance = Math.Min(baseRandomChance, 1);
                        EggSpawningService.SetRandomBaseChance(baseRandomChance);
                        ModEntry.LoggerHelper.Log("Egg parameters set successfully!", LogLevel.Info);
                    }
                    else
                    {
                        LogInvalidNumber();
                    }
                }
        
                private bool TryParseFloat(string input, out float result)
                {
                    return float.TryParse(input, out result);
                }
        
                private bool TryParsePositiveFloat(string input, out float result)
                {
                    return float.TryParse(input, out result) && result > 0;
                }
        
                private bool TryParsePositiveInt(string input, out int result)
                {
                    return Int32.TryParse(input, out result) && result > 0;
                }
        
                private bool TryParseInt(string input, out int result)
                {
                    return Int32.TryParse(input, out result);
                }
        
                private void LogInvalidNumber()
                {
                    ModEntry.LoggerHelper.Log("Invalid number.", LogLevel.Error);
                }*/

        private void PrintSpawnInfo(string command, string[] args)
        {
            var option = args[0].ToLower() switch
            {
                "type" => 1,
                "map" => 2,
                "mod" => 3,
                "all" => 4,
                _ => 0,
            };
            EggSpawningService?.PrintSpawnedEggsLocations(option, LogLevel.Info);
        }

        private void ResetSpawnParameters(string command, string[] args)
        {
            EggSpawningService?.ResetSpawnParameters();
        }

        private void ReloadWildAnimalsList(string command, string[] args)
        {
            _wildAnimalManager.Reset();
            _wildAnimalManager.SetUpAnimalLists();
            _gameAssetManager.ProcessWildEggs(e => e.Manager.Reset());
            //_gameAssetManager.ResetAllEggs();
            _gameAssetManager.ChooseAnimalsForAllEggs();
            // _wildAnimalManager.LogChosenAnimals();
            _gMCMenu.RefreshMenu();
        }

        private void GetCurrentProgressionStep(string command, string[] args)
        {
            LoggerHelper.Log(
                $"Currrent Progression Step: {_progressionManager.GetCurrentStep()}",
                LogLevel.Info
            );
        }

        private void GoToProgressionStep(string command, string[] args)
        {
            if (!int.TryParse(args[0], out var step))
            {
                ModEntry.MonitorInstance?.Log($"Invalid Option!", LogLevel.Error);
                return;
            }

            var currentStep = (int)_progressionManager.GetCurrentStep();
            if (step < 0 || step > 5 || step == currentStep)
            {
                ModEntry.MonitorInstance?.Log($"Invalid Option!", LogLevel.Error);
                return;
            }
            _progressionManager.GoToPart((ProgressionStep)step);
            ModEntry.MonitorInstance?.Log(
                $"Set step to: {_progressionManager.GetCurrentStep()}",
                LogLevel.Info
            );
        }

        private void PrintLists(string command, string[] args)
        {
            switch (args[0].ToLower())
            {
                case "all":
                    LoggerHelper.Log(
                        $"All Wild Animals:\n{GetAnimalListsToDisplay(false)}",
                        LogLevel.Info
                    );
                    return;
                case "valid":
                    LoggerHelper.Log(
                        $"Valid Wild Animals:\n{GetAnimalListsToDisplay(true)}",
                        LogLevel.Info
                    );
                    return;
                default:
                    LoggerHelper.Log("Invalid Option.", LogLevel.Warn);
                    return;
            }
        }

        private void PrintEggsInfo(string command, string[] args)
        {
            var all = args[0].ToLower() == "all";
            var eggsToExport = new HashSet<WildEggExporter>();
            foreach (var egg in _gameAssetManager.WildEggs.Where(e => e.IsValid || all))
            {
                eggsToExport.Add(new WildEggExporter(egg));
                LoggerHelper.Log(egg.ToString(), LogLevel.Info);
            }
            ModEntry.HelperInstance.Data.WriteJsonFile(
                $"ModData/{(all ? "all" : "valid")}eggs.json",
                eggsToExport
            );
        }

        private string GetAnimalListsToDisplay(bool onlyValid)
        {
            var list = onlyValid
                ? _wildAnimalManager.ValidWildAnimals
                : _wildAnimalManager.WildAnimals;
            var name = onlyValid ? "Valid Wild Animals" : "Wild Animals";
            return GetFormatedAnimalList(list, name);
        }

        private string GetFormatedAnimalList(IEnumerable<WildAnimalData> list, string name)
        {
            return $"{name}:\n\n  {string.Join("\n   ", list.Select(wa => $"{wa.Name}:\n     {string.Join(",\n     ", wa.FarmAnimalKeys.Select(a => a.Id))}\n"))}";
        }

        private void ClearAnimalData(string command, string[] args)
        {
            if (_modSaveData == null)
            {
                LoggerHelper.Log("ModSaveData is null", LogLevel.Warn);
                return;
            }
            var animalKey = args[0];
            if (!_modSaveData.FarmAnimalNames.Contains(animalKey))
            {
                LoggerHelper.Log("Invalid Animal", LogLevel.Warn);
                return;
            }
            string animalKeyWSpace = animalKey.Replace(" ", "");
            var questID = $"wildEggs_{animalKeyWSpace}Note";
            var mailID = $"wildEggs_NoteMail_{animalKeyWSpace}";

            _modSaveData.AnimalsOnTheFarm[animalKey] = new();
            _modSaveData.AnimalsOnTheFarmYesterday[animalKey] = new();
            _modSaveData.QuestCompletedDay[animalKey] = null;
            LoggerHelper.Log(
                $"Reseted AnimalsOnTheFarm, AnimalsOnTheFarmYesterday, and QuestCompletedDay entries for {animalKey}",
                LogLevel.Info
            );
            var removed = _modSaveData.HatchedAnimals.RemoveWhere(a =>
                a.FarmAnimalName == animalKey
            );
            if (removed > 0)
            {
                LoggerHelper.Log($"Removed {removed} animals from HatchedAnimals", LogLevel.Info);
            }
            else
            {
                LoggerHelper.Log($"Didn't find animals to remove.", LogLevel.Info);
            }
            removed = _modSaveData.NewAddedAnimals.RemoveWhere(a => a.FarmAnimalName == animalKey);
            if (removed > 0)
            {
                LoggerHelper.Log($"Removed {removed} animals from NewAddedAnimals", LogLevel.Info);
            }
            else
            {
                LoggerHelper.Log($"Didn't find animals to remove.", LogLevel.Info);
            }
            Game1.player.removeQuest(questID);
            if (Game1.player.activeDialogueEvents.Count() > 0)
            {
                removed = Game1.player.activeDialogueEvents.RemoveWhere(ad =>
                    Regex.IsMatch(ad.Key, questID, RegexOptions.IgnoreCase)
                );
                if (removed > 0)
                {
                    LoggerHelper.Log(
                        $"Removed {removed} events from ActiveDialogueEvents",
                        LogLevel.Info
                    );
                }
                else
                {
                    LoggerHelper.Log($"Didn't find entries to remove.", LogLevel.Info);
                }
            }

            if (Game1.player.previousActiveDialogueEvents.Count() > 0)
            {
                removed = Game1.player.previousActiveDialogueEvents.RemoveWhere(ad =>
                    Regex.IsMatch(ad.Key, questID, RegexOptions.IgnoreCase)
                );
                if (removed > 0)
                {
                    LoggerHelper.Log(
                        $"Removed {removed} events from PreviousActiveDialogueEvents",
                        LogLevel.Info
                    );
                }
                else
                {
                    LoggerHelper.Log($"Didn't find entries to remove.", LogLevel.Info);
                }
            }
            if (Game1.player.mailbox.Remove(mailID))
            {
                LoggerHelper.Log($"Removed mail from mailbox.", LogLevel.Info);
            }
            else
            {
                LoggerHelper.Log($"Didn't find any mail in the mailbox to remove.", LogLevel.Info);
            }
            if (Game1.player.mailForTomorrow.Remove(mailID))
            {
                LoggerHelper.Log($"Removed future mail.", LogLevel.Info);
            }
            else
            {
                LoggerHelper.Log($"Didn't find any future mail to remove.", LogLevel.Info);
            }
            if (Game1.player.mailReceived.Remove(mailID))
            {
                LoggerHelper.Log($"Removed received mail.", LogLevel.Info);
            }
            else
            {
                LoggerHelper.Log($"Didn't find any received mail remove.", LogLevel.Info);
            }
        }

        private void AddMail(string command, string[] args)
        {
            var mail = new string[]
            {
                IDs.Mail.WizardFindings,
                IDs.Mail.WizardThanks,
                IDs.Mail.MarnieThanks,
            };
            var flags = new string[]
            {
                IDs.Mail.SpecialOrderDoneI,
                IDs.Mail.WizardDone,
                IDs.Mail.SpecialOrderDoneII,
            };
            foreach (var flag in flags)
            {
                GameAssetManager.AddMailToPlayer(flag, MailType.Received, true);
            }
            foreach (var mailID in mail)
            {
                GameAssetManager.AddMailToPlayer(mailID, MailType.Tomorrow, true);
            }
        }

        private void ClearCommands(string command, string[] args)
        {
            switch (args[0].ToLower())
            {
                case "mail":
                    ClearMailCommand();
                    return;
                case "quest":
                    ClearQuestCommand();
                    return;
                case "hatched":
                    ClearHatchedAnimalsCommand();
                    return;
                case "added":
                    ClearNewlyAddedAnimalsCommand();
                    return;
                case "notes":
                    ClearNotesCommand();
                    return;
                case "all":
                    _modSaveData.Reset();
                    ClearMailCommand();
                    ClearQuestCommand();
                    ClearHatchedAnimalsCommand();
                    ClearNewlyAddedAnimalsCommand();
                    ClearNotesCommand();
                    return;
                default:
                    LoggerHelper.Log("Invalid Option.", LogLevel.Warn);
                    return;
            }
        }

        private void ClearMailCommand()
        {
            var removedReceivedMail = Game1
                .player.mailReceived.Where(m =>
                    _gameAssetManager.AllMail.Contains(m) || m.StartsWith(IDs.ModIDPrefix)
                )
                .ToList();
            foreach (var mail in removedReceivedMail)
            {
                Game1.player.mailReceived.Remove(mail);
                LoggerHelper.Log($"Removed {mail} from received mail.");
            }
            var removedTomorrowMail = Game1
                .player.mailForTomorrow.Where(m =>
                    _gameAssetManager.AllMail.Contains(m) || m.StartsWith(IDs.ModIDPrefix)
                )
                .ToList();
            foreach (var mail in removedTomorrowMail)
            {
                Game1.player.mailReceived.Remove(mail);
                LoggerHelper.Log($"Removed {mail} from tomorrow mail.");
            }
            var removedMailboxMail = Game1
                .player.mailbox.Where(m =>
                    _gameAssetManager.AllMail.Contains(m) || m.StartsWith(IDs.ModIDPrefix)
                )
                .ToList();
            foreach (var mail in removedMailboxMail)
            {
                Game1.player.mailReceived.Remove(mail);
                LoggerHelper.Log($"Removed {mail} from mailbox mail.");
            }
            var removedBroadcastMail = Game1.player.team.broadcastedMail.Where(m =>
                Regex.IsMatch(m, "wildEggs", RegexOptions.IgnoreCase)
            );
            foreach (var mail in removedBroadcastMail)
            {
                Game1.player.team.broadcastedMail.Remove(mail);
                LoggerHelper.Log($"Removed {mail} from broadcast.");
            }
            LoggerHelper.Log("Cleared mail.", LogLevel.Info);
        }

        private void ClearQuestCommand()
        {
            if (Game1.player.questLog.Count > 0)
            {
                var removedQuests = Game1
                    .player.questLog.Where(q =>
                        Regex.IsMatch(q.id.Value, "wildEggs", RegexOptions.IgnoreCase)
                    )
                    .ToList();
                foreach (var quest in removedQuests)
                {
                    LoggerHelper.Log($"Removing quest: {quest.id.Value} from quest log.");
                    Game1.player.questLog.Remove(quest);
                }
            }
            foreach (var quest in _gameAssetManager.AllQuests)
            {
                Game1.player.removeQuest(quest);
            }

            var specialOrderList = new string[]
            {
                IDs.Quests.SpecialOrderWildEggsI,
                IDs.Quests.SpecialOrderWildEggsII,
            };
            var removedSpecialOrders = Game1
                .player.team.completedSpecialOrders.Where(so => specialOrderList.Contains(so))
                .ToList();
            foreach (var specialOrder in removedSpecialOrders)
            {
                LoggerHelper.Log($"Removing completed special order: {specialOrder}.");
                Game1.player.team.completedSpecialOrders.Remove(specialOrder);
            }

            var removedActiveSpecialOrders = Game1
                .player.team.specialOrders.Where(so => specialOrderList.Contains(so.questKey.Value))
                .ToList();
            foreach (var specialOrder in removedActiveSpecialOrders)
            {
                LoggerHelper.Log($"Removing active special order: {specialOrder.questKey.Value}.");
                Game1.player.team.specialOrders.Remove(specialOrder);
            }
            if (Game1.player.activeDialogueEvents.Count() > 0)
            {
                var activeDialogueEventsToRemove = Game1
                    .player.activeDialogueEvents.Keys.Where(key =>
                        Regex.IsMatch(key, "wildEggs", RegexOptions.IgnoreCase)
                    )
                    .ToList();
                foreach (var key in activeDialogueEventsToRemove)
                {
                    LoggerHelper.Log($"Removing activeDialogue: {key}.");
                    Game1.player.activeDialogueEvents.Remove(key);
                }
            }

            if (Game1.player.previousActiveDialogueEvents.Count() > 0)
            {
                var previousActiveDialogueEventsToRemove = Game1
                    .player.previousActiveDialogueEvents.Keys.Where(key =>
                        Regex.IsMatch(key, "wildEggs", RegexOptions.IgnoreCase)
                    )
                    .ToList();
                foreach (var key in previousActiveDialogueEventsToRemove)
                {
                    LoggerHelper.Log($"Removing previousActiveDialogue: {key}.");
                    Game1.player.previousActiveDialogueEvents.Remove(key);
                }
            }
            foreach (var item in _modSaveData.QuestCompletedDay.Keys)
            {
                _modSaveData.QuestCompletedDay[item] = null;
            }
            LoggerHelper.Log("Cleared quests.", LogLevel.Info);
        }

        private void ClearHatchedAnimalsCommand()
        {
            _modSaveData.HatchedAnimals.Clear();

            LoggerHelper.Log("Cleared Hatched Animals.", LogLevel.Info);
        }

        private void ClearNewlyAddedAnimalsCommand()
        {
            _modSaveData.NewAddedAnimals.Clear();

            LoggerHelper.Log("Cleared Newly Added Animals.", LogLevel.Info);
        }

        internal static void ClearNotesCommand()
        {
            LoggerHelper.Log("ClearNotesCommand started.", LogLevel.Info);
            foreach (var location in Game1.locations)
            {
                ClearOnThisLocation(location);
            }
            foreach (
                var location in Game1
                    .getFarm()
                    .buildings.Select(b => Game1.getLocationFromName(b.GetIndoorsName()))
            )
            {
                ClearOnThisLocation(location);
            }

            foreach (
                var item in Game1.player.Items.Where(i => i != null && IsAObjectToRemove(i.ItemId))
            )
            {
                LoggerHelper.Log($"Removing {item.ItemId} from inventory.");
                Game1.player.removeItemFromInventory(item);
            }

            LoggerHelper.Log("Cleared Junimo Notes.", LogLevel.Info);
            LoggerHelper.Log("ClearNotesCommand finished.", LogLevel.Info);
        }

        private static void ClearOnThisLocation(GameLocation location)
        {
            if (location == null)
            {
                return;
            }
            foreach (var obj in location.Objects)
            {
                foreach (var item in obj.Values)
                {
                    if (item is Chest chest)
                    {
                        for (int index = chest.Items.Count - 1; index >= 0; index--)
                        {
                            Item i = chest.Items[index];
                            if (IsAObjectToRemove(i.ItemId))
                            {
                                LoggerHelper.Log(
                                    $"Removing {i.ItemId} from a {chest.DisplayName} in {location.Name}."
                                );

                                chest.Items.RemoveAt(index);
                            }
                        }
                    }
                    else if (IsAObjectToRemove(item.ItemId))
                    {
                        var key = obj.Single(o => o.Value.ItemId == item.ItemId).Key;
                        LoggerHelper.Log($"Removing {item.ItemId} from {location.Name}.");
                        location.Objects.Remove(key);
                    }
                }
            }
        }

        private static bool IsAObjectToRemove(string itemID)
        {
            bool result =
                itemID.StartsWith("Mikew.wildeggshatching_junimoNote_")
                || itemID == IDs.Objects.OsirisScroll;
            if (result)
            {
                LoggerHelper.Log($"Item {itemID} is to be removed.");
            }

            return result;
        }
    }
}
