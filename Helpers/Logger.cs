using System.Collections.Generic;
using System.Linq;
using Mikew.BirdsOfAFeather.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.FarmAnimals;
using static Mikew.BirdsOfAFeather.EnumsAndStrings.Enums;

namespace Mikew.BirdsOfAFeather.Helpers
{
    public class Logger
    {
        private readonly IMonitor _monitor;
        private readonly CoreModConfig _config;

        // Constructor to initialize the logger
        public Logger(IMonitor monitor, CoreModConfig config)
        {
            _monitor = monitor;
            _config = config; // Assign the passed config to the instance field
        }

        public void Log(string text, LogLevel logLevel = LogLevel.Trace, bool enabled = true)
        {
            var invalidLogging = !enabled;
            if (!invalidLogging)
            {
                _monitor.Log(text, logLevel);
            }
        }

        public void VerboseLog(string text, LogLevel logLevel = LogLevel.Debug, bool enabled = true)
        {
            var invalidLogging = !enabled || !_config.VerboseLogging;
            if (!invalidLogging)
            {
                _monitor.Log(text, logLevel);
            }
        }

        public void TestingLog(string text, LogLevel logLevel = LogLevel.Debug, bool enabled = true)
        {
            var invalidLogging = !enabled || !_config.TestingLogging;
            if (!invalidLogging)
            {
                _monitor.Log(text, logLevel);
            }
        }

        // WildEggs
        public void LogFarmAnimals(string chosenAnimal, LogLevel logLevel = LogLevel.Trace)
        {
            var animalData = Game1.farmAnimalData[chosenAnimal];
            var openingString = @"{""FarmAnimals"": {";
            var animalStrings = new Dictionary<string, List<string>>
            {
                [chosenAnimal] = new List<string>
                {
                    $"\"{chosenAnimal}\" : {{",
                    $"\"DisplayName\": \"{animalData.DisplayName}\",",
                    $"\"PurchasePrice\": \"{animalData.PurchasePrice}\",",
                    $"\"SellPrice\": \"{animalData.SellPrice}\",",
                    $"\"RequiredBuilding\": \"{animalData.RequiredBuilding}\",",
                    $"\"UnlockCondition\": \"{animalData.UnlockCondition}\","
                }
            };

            if (animalData.AlternatePurchaseTypes != null)
            {
                animalStrings[chosenAnimal].Add("\"AlternatePurchaseTypes\": [");
                foreach (var alternatePurchaseType in animalData.AlternatePurchaseTypes)
                {
                    animalStrings[chosenAnimal]
                        .Add(
                            $"{{ \"Id\": \"{alternatePurchaseType.Id}\", \"Condition\": \"{alternatePurchaseType.Condition}\", \"AnimalIds\": [ \"{string.Join(", ", alternatePurchaseType.AnimalIds)}\" ] }},"
                        );
                }
                animalStrings[chosenAnimal].Add("],");
            }
            else
            {
                animalStrings[chosenAnimal].Add("\"AlternatePurchaseTypes\": null,");
            }

            if (animalData.EggItemIds != null)
            {
                animalStrings[chosenAnimal]
                    .Add($"\"EggItemIds\": [ \"{string.Join(", ", animalData.EggItemIds)}\" ],");
            }
            else
            {
                animalStrings[chosenAnimal].Add("\"EggItemIds\": null,");
            }

            animalStrings[chosenAnimal].Add($"\"DaysToMature\": \"{animalData.DaysToMature}\",");
            animalStrings[chosenAnimal].Add($"\"DaysToProduce\": \"{animalData.DaysToProduce}\",");
            animalStrings[chosenAnimal].Add($"\"HarvestType\": \"{animalData.HarvestType}\",");

            if (animalData.ProduceItemIds != null)
            {
                animalStrings[chosenAnimal].Add("\"ProduceItemIds\":[");
                foreach (var produceItemId in animalData.ProduceItemIds)
                {
                    animalStrings[chosenAnimal].Add($"{{\"ItemId\":\"{produceItemId.ItemId}\"}},");
                }
                animalStrings[chosenAnimal].Add("],");
            }
            else
            {
                animalStrings[chosenAnimal].Add("\"ProduceItemIds\": null,");
            }

            if (animalData.DeluxeProduceItemIds != null)
            {
                animalStrings[chosenAnimal].Add("\"DeluxeProduceItemIds\":[");
                foreach (var deluxeProduceItemId in animalData.DeluxeProduceItemIds)
                {
                    animalStrings[chosenAnimal]
                        .Add($"{{\"ItemId\":\"{deluxeProduceItemId.ItemId}\"}},");
                }
                animalStrings[chosenAnimal].Add("],");
            }
            else
            {
                animalStrings[chosenAnimal].Add("\"DeluxeProduceItemIds\": null,");
            }

            animalStrings[chosenAnimal]
                .Add(
                    $"\"DeluxeProduceMinimumFriendship\": \"{animalData.DeluxeProduceMinimumFriendship}\","
                );
            animalStrings[chosenAnimal]
                .Add($"\"DeluxeProduceCareDivisor\": \"{animalData.DeluxeProduceCareDivisor}\",");
            animalStrings[chosenAnimal].Add("}}");

            var json = openingString + string.Join(", ", animalStrings[chosenAnimal]) + "}";
            _monitor.Log(json, logLevel);
        }
    }
}
