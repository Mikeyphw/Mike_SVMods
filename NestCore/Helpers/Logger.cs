using System;
using System.Collections.Generic;
using System.Linq;
using Mikew.NestCore.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.FarmAnimals;
using static Mikew.NestCore.EnumsAndStrings.Enums;
using Mikew.NestCore.Helpers.Logging;

namespace Mikew.NestCore.Helpers
{
    public class Logger : ILog
    {
        private readonly IMonitor _monitor;
        private readonly CoreModConfig _config;
        private readonly string _defaultMod;
        private readonly string _defaultCategory;

        // Constructor to initialize the logger
        public Logger(
            IMonitor monitor,
            CoreModConfig config,
            string defaultMod = ScopedLogCatalog.ModNestCore,
            string defaultCategory = ScopedLogCatalog.CategoryGeneral)
        {
            _monitor = monitor;
            _config = config; // Assign the passed config to the instance field
            _defaultMod = ScopedLogCatalog.NormalizeModTag(defaultMod);
            _defaultCategory = ScopedLogCatalog.NormalizeCategory(defaultCategory);
        }

        public void Log(string text, LogLevel logLevel = LogLevel.Trace, bool enabled = true)
        {
            Emit(_defaultMod, _defaultCategory, text, logLevel, enabled);
        }

        public void VerboseLog(string text, LogLevel logLevel = LogLevel.Debug, bool enabled = true)
        {
            var invalidLogging = !enabled || !_config.VerboseLogging;
            if (!invalidLogging)
            {
                Emit(_defaultMod, _defaultCategory, text, logLevel, enabled: true);
            }
        }

        public void TestingLog(string text, LogLevel logLevel = LogLevel.Debug, bool enabled = true)
{
    var invalidLogging = !enabled || !_config.TestingLogging;
    if (!invalidLogging)
    {
        Emit(_defaultMod, _defaultCategory, text, logLevel, enabled: true);
    }
}

public void LogException(Exception exception, string message, LogLevel logLevel = LogLevel.Error, bool enabled = true)
{
    Emit(_defaultMod, _defaultCategory, $"{message}\n{exception}", logLevel, enabled);
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
            Emit(_defaultMod, "farmanimals", json, logLevel, enabled: true);
        }

        public void LogScoped(
            string mod,
            string category,
            string message,
            LogLevel logLevel = LogLevel.Trace,
            bool enabled = true)
        {
            Emit(mod, category, message, logLevel, enabled);
        }

        private void Emit(string mod, string category, string message, LogLevel logLevel, bool enabled)
        {
            if (!enabled)
                return;

            _config.EnsureScopedLoggingDefaults();

            var modTag = ScopedLogCatalog.NormalizeModTag(mod);
            var categoryTag = ScopedLogCatalog.NormalizeCategory(category);
            var modCfg = _config.ScopedLogging.GetByMod(modTag);
            var effectiveLevel = modCfg.ResolveEffectiveLevel(categoryTag);
            if (!ScopedLogCatalog.ShouldEmit(effectiveLevel, logLevel))
                return;

            var levelTag = ScopedLogCatalog.ToLevelToken(logLevel);
            _monitor.Log($"[{modTag}][{categoryTag}][{levelTag}] {message}", logLevel);
        }
    }
}
