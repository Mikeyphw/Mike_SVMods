using Mikew.BirdsOfAFeather.Helpers.GameStateQueries;
using StardewValley;
using WildEggsHatching.Framework;
using WildEggsHatching.Managers;
using WildEggsHatching.Managers.GameAssetManagement;
using WildEggsHatching.Models;
using static WildEggsHatching.EnumsAndStrings.Strings;
using static WildEggsHatching.ModEntry;

namespace WildEggsHatching.Services
{
    internal class NewAnimalMonitoring
    {
        private readonly ModData _modSaveData;
        private readonly WildListManager _wildAnimalManager;
        private readonly Dictionary<string, bool> _animalProgressionMode;

        internal NewAnimalMonitoring(
            ModData modSaveData,
            WildListManager wildAnimalManager,
            Dictionary<string, bool> animalProgressionMode
        )
        {
            _wildAnimalManager = wildAnimalManager;
            _animalProgressionMode = animalProgressionMode;
            _modSaveData = modSaveData;
            LoggerHelper.VerboseLog("NewAnimalMonitoring initialized.");
        }

        internal List<ChosenAnimal> GetAnimalsNeedingNotes()
        {
            LoggerHelper.VerboseLog("Getting animals needing notes.");
            var animalsNeedingNotes = new List<ChosenAnimal>();

            foreach (var animal in _modSaveData.HatchedAnimals)
            {
                LoggerHelper.VerboseLog($"Processing animal: {animal.FarmAnimalName}");
                GameAssetManager.SetQuestCompletionDay(
                    animal.QuestID,
                    _modSaveData,
                    noLetter: true
                );

                if (ShouldSpawnNoteForAnimal(animal))
                {
                    LoggerHelper.VerboseLog($"Animal {animal.FarmAnimalName} needs a note.");
                    animalsNeedingNotes.Add(animal);
                }

                if (ShouldRemoveAnimal(animal))
                {
                    LoggerHelper.VerboseLog($"Animal {animal.FarmAnimalName} should be removed.");
                    _modSaveData.NewAddedAnimals.Add(animal);
                }
            }

            // Remove processed animals from HatchedAnimals
            foreach (var animal in _modSaveData.NewAddedAnimals)
            {
                _modSaveData.HatchedAnimals.Remove(animal);
                LoggerHelper.VerboseLog($"Removed animal: {animal.FarmAnimalName} from HatchedAnimals.");
            }

            LoggerHelper.VerboseLog($"Total animals needing notes: {animalsNeedingNotes.Count}");
            return animalsNeedingNotes;
        }

        private bool ShouldSpawnNoteForAnimal(ChosenAnimal animal)
        {
            bool shouldSpawn = !animal.NoteSpawned && !GameAssetManager.HasItem(animal.NoteID);
            LoggerHelper.VerboseLog($"Should spawn note for {animal.FarmAnimalName}: {shouldSpawn}");
            return shouldSpawn;
        }

        private bool ShouldRemoveAnimal(ChosenAnimal animal)
        {
            LoggerHelper.VerboseLog($"Checking if animal {animal.FarmAnimalName} should be removed.");
            int daysToMature = Game1.farmAnimalData[animal.FarmAnimalName].DaysToMature;
            bool hasSpecialOrderDoneII = GameAssetManager.HasMail(IDs.Mail.SpecialOrderDoneII);
            bool hasCompletedQuest = GameAssetManager.HasCompletedQuest(animal.QuestID);
            bool hasEnoughDaysPassed = GameAssetManager.HasEnoughDaysPassed(
                _modSaveData.QuestCompletedDay[animal.QuestID],
                daysToMature + 2
            );
            bool animalMailCheck =
                hasSpecialOrderDoneII && hasCompletedQuest && hasEnoughDaysPassed;

            LoggerHelper.VerboseLog($"Checking mail for {animal.FarmAnimalName}...");
            LoggerHelper.VerboseLog($"HasMail(IDs.Mail.SpecialOrderDoneII): {hasSpecialOrderDoneII}");
            LoggerHelper.VerboseLog($"HasCompletedQuest({animal.QuestID}): {hasCompletedQuest}");
            LoggerHelper.VerboseLog($"animalMailCheck: {animalMailCheck}");

            bool mailAdded = GameAssetManager.AddMailToPlayer(
                animal.MailID,
                MailType.Mailbox,
                false,
                animalMailCheck
            );
            LoggerHelper.VerboseLog(
                $"Mail{(mailAdded ? " " : " not ")}was added for {animal.FarmAnimalName}."
            );

            return GameAssetManager.HasMail(animal.MailID);
        }

        internal void FindChangesInAnimalFarm()
        {
            if (_modSaveData == null)
            {
                LoggerHelper.VerboseLog("ModSaveData is null, exiting FindChangesInAnimalFarm.");
                return;
            }

            LoggerHelper.VerboseLog("Finding changes in the animal farm.");
            var AnimalsOnTheFarm = _modSaveData.AnimalsOnTheFarm;
            var AnimalsOnTheFarmYesterday = _modSaveData.AnimalsOnTheFarmYesterday;
            var AlreadyAddedAnimals = _modSaveData
                .NewAddedAnimals.Select(a => a.FarmAnimalName)
                .ToList();
            var ExcludedAnimals = _animalProgressionMode
                .Where(a => !a.Value)
                .Select(a => a.Key)
                .ToList();

            // Check for new keys in AnimalsOnTheFarm
            foreach (string key in AnimalsOnTheFarm.Keys)
            {
                if (!AnimalsOnTheFarmYesterday.ContainsKey(key))
                {
                    if (
                        _wildAnimalManager.LoadedAnimalsKeys.Contains(key)
                        && AnimalsOnTheFarm[key].Count > 0
                        && !AlreadyAddedAnimals.Contains(key)
                        && !ExcludedAnimals.Contains(key)
                    )
                    {
                        LoggerHelper.VerboseLog("New animal KEY!");
                        var animal = Utility.getAnimal(AnimalsOnTheFarm[key].First());
                        var chosenAnimal = new ChosenAnimal
                        {
                            WasBorn = true,
                            FarmAnimalName = key,
                            ID = AnimalsOnTheFarm[key].First(),
                            Home = animal.home.GetIndoors(),
                            HouseType = animal.buildingTypeILiveIn.Name
                        };

                        chosenAnimal.SetIDs();
                        _modSaveData.HatchedAnimals.Add(chosenAnimal);
                        LoggerHelper.VerboseLog(
                            $"Added new animal: {chosenAnimal.FarmAnimalName} to HatchedAnimals."
                        );
                    }
                }
                else
                {
                    // Check if the list has changed for the same key
                    var yesterdayList = new HashSet<long>(AnimalsOnTheFarmYesterday[key]);
                    var todayList = new HashSet<long>(AnimalsOnTheFarm[key]);

                    var addedValues = todayList.Except(yesterdayList).ToList();

                    foreach (var addedValue in addedValues)
                    {
                        if (
                            _wildAnimalManager.LoadedAnimalsKeys.Contains(key)
                            && !AlreadyAddedAnimals.Contains(key)
                            && !ExcludedAnimals.Contains(key)
                        )
                        {
                            LoggerHelper.VerboseLog("New animal!");
                            var animal = Utility.getAnimal(addedValue);
                            var chosenAnimal = new ChosenAnimal
                            {
                                WasBorn = true,
                                FarmAnimalName = key,
                                ID = addedValue,
                                Home = animal.home.GetIndoors(),
                                HouseType = animal.buildingTypeILiveIn.Name
                            };
                            chosenAnimal.SetIDs();
                            _modSaveData.HatchedAnimals.Add(chosenAnimal);
                            LoggerHelper.VerboseLog(
                                $"Added new animal: {chosenAnimal.FarmAnimalName} to HatchedAnimals."
                            );
                        }
                    }
                }
            }
        }
    }
}
