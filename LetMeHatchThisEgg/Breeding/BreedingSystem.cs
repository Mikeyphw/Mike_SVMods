using LetMeHatchThisEgg.Config;
using LetMeHatchThisEgg.Framework;
using LetMeHatchThisEgg.Models;
using Mikew.BirdsOfAFeather.Helpers;
using Mikew.BirdsOfAFeather.Framework.Random;
using LetMeHatchThisEgg.Application.UseCases;
using LetMeHatchThisEgg.Core.Breeding;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.GameData.FarmAnimals;
using StardewValley.TokenizableStrings;
using static LetMeHatchThisEgg.EnumsAndStrings.Enums;

using static LetMeHatchThisEgg.ModEntry;

namespace LetMeHatchThisEgg.Breeding
{
    public class BreedingSystem
    {
        private readonly IDictionary<string, FarmAnimalData> _gameAnimalData;
        private readonly FarmAnimalData _gameDataFemale;
        private readonly FarmAnimalData _gameDataMale;
        private readonly Dictionary<ItemType, (string? ID, float Chance)> _eggs;
        private readonly List<FarmAnimalData> _malesNotChosenGameData;
        private readonly int _chosenMaleQuantity;
        private readonly bool _onlyFertilizedEggs;
        private readonly bool _applyDailyLuckBoost;
        private readonly float _maleChance;
        private readonly float _boostByMale;
        private readonly HashSet<string> WildEggsIDs;

        private readonly IRandomProvider _rng;
        private readonly EggSelectionUseCase _eggSelector;

        public BreedingSystem(
            HashSet<string> wildEggsIDs,
            Dictionary<string, Dictionary<string, BreedingConfig>> breedingConfigsByPack,
            bool onlyFertilizedEggs,
            string femaleKey,
            (string ContentPackID, BreedingDataFemale BreedingData) chosenFemaleData,
            KeyValuePair<string, BreedingDataMale> chosenMaleData,
            IDictionary<string, FarmAnimalData> farmAnimalsData,
            IRandomProvider? rng = null
        )
        {
            WildEggsIDs = wildEggsIDs;
            _gameAnimalData = farmAnimalsData;

            _rng = rng ?? new GameRandomProvider();
            _eggSelector = new EggSelectionUseCase(_rng);

            var generalConfig = breedingConfigsByPack[chosenFemaleData.ContentPackID][femaleKey];

            _gameDataFemale = _gameAnimalData[femaleKey];
            _gameDataMale = _gameAnimalData[chosenMaleData.Key];
            _eggs = GetEggs(generalConfig.EggChances, chosenMaleData.Value.GetEggIDs());
            _malesNotChosenGameData = GetOtherMalesFarmAnimalData(
                chosenFemaleData.BreedingData.Males,
                chosenMaleData.Key
            );
            _chosenMaleQuantity = chosenMaleData.Value.GetQuantity();
            _onlyFertilizedEggs = onlyFertilizedEggs;
            _applyDailyLuckBoost = generalConfig.ApplyDailyLuckBoost;
            _maleChance = generalConfig.MaleChance[chosenMaleData.Key];
            _boostByMale = generalConfig.BoostByMale[chosenMaleData.Key];
        }

        private KeyValuePair<string, BreedingDataMale> GetChosenMale(
            Dictionary<string, BreedingDataMale> males
        )
        {
            return males
                .Where(m => m.Value.Enabled)
                .GetRandomItem(male => GetMaleQuantity(male.Key), randomProvider: _rng);
        }

        private Dictionary<ItemType, (string? ID, float Chance)> GetEggs(
            Dictionary<ItemType, float> eggChances,
            Dictionary<ItemType, string?> eggIDs
        )
        {
            return new()
            {
                { ItemType.Normal, (eggIDs[ItemType.Normal], eggChances[ItemType.Normal]) },
                { ItemType.Large, (eggIDs[ItemType.Large], eggChances[ItemType.Large]) },
                {
                    ItemType.FertilizedEgg,
                    (eggIDs[ItemType.FertilizedEgg], eggChances[ItemType.FertilizedEgg])
                }
            };
        }

        private List<FarmAnimalData> GetOtherMalesFarmAnimalData(
            Dictionary<string, BreedingDataMale> males,
            string chosenMaleKey
        )
        {
            return males
                .Where(r => r.Key != chosenMaleKey)
                .Select(r => _gameAnimalData[r.Key])
                .ToList();
        }

        public static int GetMaleQuantity(string farmName)
        {
            try
            {
                return Game1
                        .getFarm()
                        .buildings.Where(b => IsCoop(b))
                        .Sum(coop =>
                            ((AnimalHouse)coop.indoors.Value).animals.Values.Count(animal =>
                                animal.type.Value == farmName && animal.isAdult()
                            )
                        )
                    + Game1
                        .getFarm()
                        .animals.Values.Count(animal =>
                            IsCoop(animal.home) && animal.type.Value == farmName && animal.isAdult()
                        );
            }
            catch (System.Exception)
            {
                LoggerHelper.VerboseLog("Farm not found!");
                return 0;
            }
        }

        private static bool IsCoop(Building b)
        {
            return b.buildingType.Value.ToLower().Contains("coop");
        }

        public void Run()
        {
            if (_onlyFertilizedEggs && string.IsNullOrEmpty(_eggs[ItemType.FertilizedEgg].ID))
            {
                LoggerHelper.VerboseLog(
                    $"Can't run hatching system because no fertilized eggs are available.  Couple: ({GetAnimalDisplayName(_gameDataFemale)},{GetAnimalDisplayName(_gameDataMale)}).",
                    LogLevel.Warn
                );
                return;
            }
            if (
                !_onlyFertilizedEggs
                && string.IsNullOrEmpty(_eggs[ItemType.Normal].ID)
                && string.IsNullOrEmpty(_eggs[ItemType.Large].ID)
            )
            {
                LoggerHelper.VerboseLog(
                    $"Can't run hatching system for couple ({GetAnimalDisplayName(_gameDataFemale)},{GetAnimalDisplayName(_gameDataMale)}), because there's no eggs available.",
                    LogLevel.Error
                );
                return;
            }
            if (_onlyFertilizedEggs && !string.IsNullOrEmpty(_eggs[ItemType.FertilizedEgg].ID))
            {
                FertilizedHatchingSystem();
            }
            else if (!_onlyFertilizedEggs)
            {
                NormalHatchingSystem();
            }
        }

        private void FertilizedHatchingSystem()
        {
            var fertilizedEgg = _eggs[ItemType.FertilizedEgg].ID!;
            var animalsThatShareEgg = GetAnimalsThatProduceSameEgg(fertilizedEgg);
            ClearEggLists(animalsThatShareEgg, fertilizedEgg);

            if (_chosenMaleQuantity <= 0)
            {
                LoggerHelper.VerboseLog(
                    $"Can't run hatching system because there are no males available. Couple: ({GetAnimalDisplayName(_gameDataFemale)},{GetAnimalDisplayName(_gameDataMale)}).",
                    LogLevel.Trace
                );
                return;
            }
            if (
                _eggSelector.RollChance(_eggs[ItemType.FertilizedEgg].Chance + _chosenMaleQuantity * _boostByMale)
            )
            {
                _gameDataFemale.DeluxeProduceItemIds ??= new();
                _gameDataFemale.DeluxeProduceItemIds.Clear();
                _gameDataFemale.DeluxeProduceItemIds.Add(CreateFarmAnimalProduce(fertilizedEgg));

                _gameDataFemale.ProduceItemIds ??= new();
                _gameDataFemale.ProduceItemIds.Clear();
                _gameDataFemale.ProduceItemIds.Add(CreateFarmAnimalProduce(fertilizedEgg));
            }
            if (_eggSelector.RollChanceInclusive(_maleChance))
            {
                _gameDataMale.EggItemIds.Add(fertilizedEgg);
            }
            else
            {
                _gameDataFemale.EggItemIds.Add(fertilizedEgg);
            }
        }

        private void ClearEggLists(List<FarmAnimalData>? animalsThatShareEgg, string eggID)
        {
            // Retain only the items in EggItemIds that are also in WildEggs

            _gameDataMale.EggItemIds ??= new();
            _gameDataMale.EggItemIds.RemoveAll(item => !WildEggsIDs.Contains(item));

            _gameDataFemale.EggItemIds ??= new();
            _gameDataFemale.EggItemIds.RemoveAll(item => !WildEggsIDs.Contains(item));

            animalsThatShareEgg?.ForEach(a =>
            {
                a.EggItemIds ??= new();
                a.EggItemIds.Remove(eggID);
            });

            _malesNotChosenGameData.ForEach(or =>
            {
                or.EggItemIds ??= new();
                or.EggItemIds.RemoveAll(item => !WildEggsIDs.Contains(item));
            });
        }

        private void NormalHatchingSystem()
        {
            foreach (var eggType in new[] { ItemType.Normal, ItemType.Large })
            {
                if (!string.IsNullOrEmpty(_eggs[eggType].ID))
                {
                    RunNormalHatchingSystem(eggType);
                }
                else
                {
                    LoggerHelper.VerboseLog(
                        $"Can't run hatching system for {eggType} eggs because none are available. Couple: ({GetAnimalDisplayName(_gameDataFemale)},{GetAnimalDisplayName(_gameDataMale)}).",
                        LogLevel.Warn
                    );
                }
            }
        }

        private void RunNormalHatchingSystem(ItemType eggType)
        {
            var (ID, Chance) = _eggs[eggType];
            var animalsThatShareEgg = GetAnimalsThatProduceSameEgg(ID!);

            ClearEggLists(animalsThatShareEgg, ID!);
            var adjustedChance = _applyDailyLuckBoost
                ? DailyLuckHelper.AdjustChanceByDailyLuck(
                    (double)Chance,
                    ModEntry.Config.DailyLuckBonus
                )
                : Chance;

            if (_eggSelector.ChooseRecipient(adjustedChance) == EggRecipient.Male)
            {
                _gameDataMale.EggItemIds.Add(ID);
            }
            else
            {
                _gameDataFemale.EggItemIds.Add(ID);
            }
        }

        private List<FarmAnimalData> GetAnimalsThatProduceSameEgg(string femaleEgg)
        {
            return _gameAnimalData
                .Where(a =>
                    a.Value.EggItemIds != null
                    && a.Value.EggItemIds.Exists(e => e.ToLower() == femaleEgg.ToLower())
                )
                .Select(a => a.Value)
                .ToList();
        }

        private string GetAnimalDisplayName(FarmAnimalData animalData)
        {
            if (animalData.DisplayName.StartsWith('['))
            {
                return TokenParser.ParseText(animalData.DisplayName);
            }
            else
            {
                return animalData.DisplayName;
            }
        }

        public static FarmAnimalProduce CreateFarmAnimalProduce(string? ItemID)
        {
            if (ItemID == null)
            {
                return GetErrorProduce();
            }
            else
            {
                return new FarmAnimalProduce() { ItemId = ItemID, Id = ItemID };
            }
        }

        public static FarmAnimalProduce GetErrorProduce()
        {
            return CreateFarmAnimalProduce("400");
        }
    }
}
