using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RoosterAddon.Enums;
using LuckyTypes = Mikew.BirdsOfAFeather.EnumsAndStrings.Enums.LuckyTypes;
using RoosterAddon.Framework;
using StardewModdingAPI;
using StardewValley.GameData.FarmAnimals;

namespace RoosterAddon.Helpers
{
    public static class Logger
    {
        public static void LogDailyLuck(
            LuckyTypes luckyType,
            IMonitor monitor,
            LogLevel logLevel = LogLevel.Trace
        )
        {
            monitor.Log($"Daily Luck is {ModConfig.GetEnumName(luckyType)}", logLevel);
        }

        public static void LogRoosterData(
            Dictionary<RoosterType, RoosterData> roosterData,
            IMonitor monitor,
            LogLevel logLevel = LogLevel.Trace
        )
        {
            var OpeningString = "{\"Roosters\": {";
            var RoosterStrings = new Dictionary<RoosterType, List<string>>
            {
                { RoosterType.White, new List<string>() },
                { RoosterType.Brown, new List<string>() },
                { RoosterType.Blue, new List<string>() },
                { RoosterType.Void, new List<string>() },
                { RoosterType.Gold, new List<string>() },
            };
            foreach (var rooster in roosterData)
            {
                RoosterStrings[rooster.Key].Add($"\"{ModConfig.GetEnumName(rooster.Key)}\":{{");
                RoosterStrings[rooster.Key]
                    .Add(
                        $"\"ProduceOption\": \"{rooster.Value.ProduceOption}\", \"DeluxeProduceOption\": \"{rooster.Value.DeluxeProduceOption}\","
                    );
                RoosterStrings[rooster.Key].Add("\"AllProduces\":{");

                foreach (var produce in rooster.Value.AllProduces)
                {
                    RoosterStrings[rooster.Key].Add($"\"{ModConfig.GetEnumName(produce.Key)}\":{{");
                    foreach (var entry in produce.Value)
                    {
                        RoosterStrings[rooster.Key].Add($"\"{ModConfig.GetEnumName(entry.Key)}\":{{");
                        foreach (var item in entry.Value)
                        {
                            RoosterStrings[rooster.Key]
                                .Add($"\"{ModConfig.GetEnumName(item.Key)}\": \"{item.Value}\",");
                        }
                        RoosterStrings[rooster.Key].Add("},");
                    }
                    RoosterStrings[rooster.Key].Add("},");
                }
                RoosterStrings[rooster.Key].Add("},");
                RoosterStrings[rooster.Key].Add($"\"Female\": \"{rooster.Value.Female}\",");
                var FemalesString = string.Join(",", rooster.Value.Females.Select(x=> $"\"{x}\""));
                RoosterStrings[rooster.Key].Add($"\"Females\":[{FemalesString}],");
                RoosterStrings[rooster.Key].Add("\"Eggs\":{");
                foreach (var egg in rooster.Value.Eggs)
                {
                    RoosterStrings[rooster.Key].Add($"\"{ModConfig.GetEnumName(egg.Key)}\":{{");
                    foreach (var entry in egg.Value)
                    {
                        RoosterStrings[rooster.Key]
                            .Add($"\"{ModConfig.GetEnumName(entry.Key)}\": \"{entry.Value}\",");
                    }
                    RoosterStrings[rooster.Key].Add("},");
                }
                RoosterStrings[rooster.Key].Add("},");
                RoosterStrings[rooster.Key]
                    .Add($"\"HarvestType\": \"{rooster.Value.HarvestType}\",");
                RoosterStrings[rooster.Key]
                    .Add($"\"ProduceDays\": \"{rooster.Value.ProduceDays}\",");
                RoosterStrings[rooster.Key].Add($"\"MatureDays\": \"{rooster.Value.MatureDays}\",");
                RoosterStrings[rooster.Key].Add($"\"Buyable\": \"{rooster.Value.Buyable}\",");
                RoosterStrings[rooster.Key]
                    .Add($"\"BuildingRequired\": \"{rooster.Value.BuildingRequired}\",");
                RoosterStrings[rooster.Key]
                    .Add($"\"FarmAnimalName\": \"{rooster.Value.FarmAnimalName}\",");
                RoosterStrings[rooster.Key].Add($"\"Price\": \"{rooster.Value.Price}\",");
                RoosterStrings[rooster.Key].Add($"\"Quantity\": \"{rooster.Value.Quantity}\",");
                RoosterStrings[rooster.Key].Add("},");
            }

            var flattenedString = string.Join("", RoosterStrings.SelectMany(kvp => kvp.Value));
            var ClosingString = "}}";
            var FinalString = OpeningString + flattenedString + ClosingString;
            monitor.Log(FinalString, logLevel);
        }

        public static void LogFarmAnimals(
            Dictionary<string, FarmAnimalData> farmAnimals,
            IMonitor monitor,
            LogLevel logLevel = LogLevel.Trace
        )
        {
            var OpeningString = @"{""FarmAnimals"": {";
            var AnimalStrings = new Dictionary<string, List<string>>();

            foreach (var entry in farmAnimals)
            {
                AnimalStrings[entry.Key] = new List<string>
                {
                    $"\"{entry.Key}\" : {{",
                    $"\"DisplayName\": \"{entry.Value.DisplayName}\",",
                    $"\"PurchasePrice\": \"{entry.Value.PurchasePrice}\",",
                    $"\"SellPrice\": \"{entry.Value.SellPrice}\",",
                    $"\"RequiredBuilding\": \"{entry.Value.RequiredBuilding}\",",
                    $"\"UnlockCondition\": \"{entry.Value.UnlockCondition}\","
                };

                if (entry.Value.AlternatePurchaseTypes != null)
                {
                    AnimalStrings[entry.Key].Add("\"AlternatePurchaseTypes\": [");
                    foreach (var alternatePurchaseType in entry.Value.AlternatePurchaseTypes)
                    {
                        AnimalStrings[entry.Key]
                            .Add(
                                $"{{ \"Id\": \"{alternatePurchaseType.Id}\", \"Condition\": \"{alternatePurchaseType.Condition}\", \"AnimalIds\": [ \"{string.Join(", ", alternatePurchaseType.AnimalIds)}\" ] }},"
                            );
                    }
                    AnimalStrings[entry.Key].Add("],");
                }
                else
                {
                    AnimalStrings[entry.Key].Add("\"AlternatePurchaseTypes\": null,");
                }

                if (entry.Value.EggItemIds != null)
                {
                    AnimalStrings[entry.Key]
                        .Add(
                            $"\"EggItemIds\": [ \"{string.Join(", ", entry.Value.EggItemIds)}\" ],"
                        );
                }
                else
                {
                    AnimalStrings[entry.Key].Add("\"EggItemIds\": null,");
                }

                AnimalStrings[entry.Key].Add($"\"DaysToMature\": \"{entry.Value.DaysToMature}\",");
                AnimalStrings[entry.Key]
                    .Add($"\"DaysToProduce\": \"{entry.Value.DaysToProduce}\",");
                AnimalStrings[entry.Key].Add($"\"HarvestType\": \"{entry.Value.HarvestType}\",");

                if (entry.Value.ProduceItemIds != null)
                {
                    AnimalStrings[entry.Key].Add("\"ProduceItemIds\":[");
                    foreach (var produceItemId in entry.Value.ProduceItemIds)
                    {
                        AnimalStrings[entry.Key].Add($"{{\"ItemId\":\"{produceItemId.ItemId}\"}},");
                    }
                    AnimalStrings[entry.Key].Add("],");
                }
                else
                {
                    AnimalStrings[entry.Key].Add("\"ProduceItemIds\": null,");
                }

                if (entry.Value.DeluxeProduceItemIds != null)
                {
                    AnimalStrings[entry.Key].Add("\"DeluxeProduceItemIds\":[");
                    foreach (var deluxeProduceItemId in entry.Value.DeluxeProduceItemIds)
                    {
                        AnimalStrings[entry.Key]
                            .Add($"{{\"ItemId\":\"{deluxeProduceItemId.ItemId}\"}},");
                    }
                    AnimalStrings[entry.Key].Add("],");
                }
                else
                {
                    AnimalStrings[entry.Key].Add("\"DeluxeProduceItemIds\": null,");
                }

                AnimalStrings[entry.Key]
                    .Add(
                        $"\"DeluxeProduceMinimumFriendship\": \"{entry.Value.DeluxeProduceMinimumFriendship}\","
                    );
                AnimalStrings[entry.Key]
                    .Add(
                        $"\"DeluxeProduceCareDivisor\": \"{entry.Value.DeluxeProduceCareDivisor}\","
                    );
                AnimalStrings[entry.Key]
                    .Add(
                        $"\"DeluxeProduceLuckMultiplier\": \"{entry.Value.DeluxeProduceLuckMultiplier}\","
                    );

                AnimalStrings[entry.Key].Add("},");
            }

            var flattenedString = string.Join("", AnimalStrings.SelectMany(kvp => kvp.Value));
            var ClosingString = "}}";
            var FinalString = OpeningString + flattenedString + ClosingString;
            monitor.Log(FinalString, logLevel);
        }
    }
}