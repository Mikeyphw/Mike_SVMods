using System;
using System.Collections.Generic;
using System.Linq;
using EggsInWild.Application.Progression;
using EggsInWild.Application.Spawning;
using EggsInWild.Infrastructure.Config;
using EggsInWild.Infrastructure.Game;
using EggsInWild.Infrastructure.SaveData;
using Mikew.NestCore.Helpers.Logging;
using EggsInWild.Infrastructure.ModData.Models;
using EggsInWild.Infrastructure.Mods;
using Mikew.NestCore.Helpers.SaveData;
using Mikew.NestCore.Helpers.Config;
using StardewModdingAPI;
using System.Reflection;
using System.Runtime.Serialization;

namespace EggsInWild.Tests;

internal sealed class FakeLog : ILog
{
    public List<string> Messages { get; } = new();

    public void Log(string message, LogLevel level = LogLevel.Debug, bool enabled = true)
        => Messages.Add($"{level}:{message}");

    public void VerboseLog(string message, LogLevel level = LogLevel.Debug, bool enabled = true)
        => Messages.Add($"{level}:{message}");

    public void TestingLog(string message, LogLevel level = LogLevel.Debug, bool enabled = true)
        => Messages.Add($"{level}:{message}");

    public void LogException(Exception exception, string message, LogLevel level = LogLevel.Error, bool enabled = true)
        => Messages.Add($"{level}:{message} :: {exception.Message}");
}

internal sealed class FakeMods : IModAvailability
{
    private readonly HashSet<string> _loaded;

    public FakeMods(IEnumerable<string> loaded)
        => _loaded = new HashSet<string>(loaded, StringComparer.OrdinalIgnoreCase);

    public bool IsLoaded(string uniqueId) => _loaded.Contains(uniqueId);
}

internal sealed class FakeWorld : IWorldLocations
{
    private readonly HashSet<string> _names;

    public FakeWorld(IEnumerable<string> names)
        => _names = new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<StardewValley.GameLocation> GetAll() => Array.Empty<StardewValley.GameLocation>();

    public StardewValley.GameLocation? TryGetByName(string name) => null;

    public bool Exists(string name) => _names.Contains(name);
}

internal sealed class AlwaysTrueConditions : IAreaConditionsEvaluator
{
    public bool AreMet(ExtraConditionsJson conditions) => true;
}

internal sealed class InMemorySaveDataStore : ISaveDataStore
{
    private readonly Dictionary<string, object?> _data = new(StringComparer.Ordinal);

    public T? Read<T>(string key) where T : class
        => _data.TryGetValue(key, out var value) ? value as T : null;

    public void Write<T>(string key, T value) where T : class
        => _data[key] = value;

    public T ReadOrCreate<T>(string key, Func<T> factory) where T : class
    {
        if (_data.TryGetValue(key, out var value) && value is T typed)
            return typed;

        var created = factory();
        _data[key] = created;
        return created;
    }

    public void Delete(string key)
        => _data.Remove(key);

    public SaveDataEnvelope<T>? ReadEnvelope<T>(string key) where T : class
        => _data.TryGetValue(key, out var value) ? value as SaveDataEnvelope<T> : null;

    public void WriteEnvelope<T>(string key, SaveDataEnvelope<T> envelope) where T : class
        => _data[key] = envelope;

    public bool ContainsKey(string key) => _data.ContainsKey(key);
}

internal sealed class FakeConfigProvider
{
    private readonly ModConfig _config;
    private readonly SmapiConfigProvider<ModConfig> _provider;

    public FakeConfigProvider(ModConfig config)
    {
        _config = Clone(config);
        _provider = CreateRawProvider(_config);
    }

    public ModConfig Config => _config;

    public ModConfig Get() => _config;

    public void Reload()
    {
    }

    public void Save()
    {
    }

    public void Update(Action<ModConfig> mutator, string reason)
    {
        mutator(_config);
    }

    public static implicit operator SmapiConfigProvider<ModConfig>(FakeConfigProvider provider)
        => provider._provider;

    private static SmapiConfigProvider<ModConfig> CreateRawProvider(ModConfig config)
    {
        var provider = (SmapiConfigProvider<ModConfig>)FormatterServices.GetUninitializedObject(typeof(SmapiConfigProvider<ModConfig>));
        var field = typeof(SmapiConfigProvider<ModConfig>).GetField("_config", BindingFlags.Instance | BindingFlags.NonPublic);
        field!.SetValue(provider, config);
        return provider;
    }

    private static ModConfig Clone(ModConfig config)
        => new()
        {
            ProgressionMode = config.ProgressionMode,
            DesertTombModuleEnabled = config.DesertTombModuleEnabled,
            AlternatePotionSprite = config.AlternatePotionSprite,
            NeedsFern = config.NeedsFern,
            HatchDinoEggs = config.HatchDinoEggs,
            DinoEggsExclusiveHatch = config.DinoEggsExclusiveHatch,
            DinoPrice = config.DinoPrice,
            VoidChickenPrice = config.VoidChickenPrice,
            OstrichPrice = config.OstrichPrice,
        };
}

internal sealed class FakeMailService : IGameMailService
{
    public readonly List<(string Id, bool NoLetter)> Tomorrow = new();
    public readonly List<(string Id, bool NoLetter)> Received = new();

    public bool HasReceivedMail(string mailId)
        => Received.Any(x => x.Id == mailId);

    public bool HasMailForTomorrow(string mailId)
        => Tomorrow.Any(x => x.Id == mailId);

    public void AddMailForTomorrow(string mailId, bool noLetter = false)
    {
        if (HasReceivedMail(mailId) || HasMailForTomorrow(mailId))
            return;

        Tomorrow.Add((mailId, noLetter));
    }

    public void AddReceivedMail(string mailId, bool noLetter = false)
    {
        if (HasReceivedMail(mailId))
            return;

        Received.Add((mailId, noLetter));
    }

    public bool RemoveReceivedMail(string mailId)
    {
        var removed = Received.RemoveAll(x => x.Id == mailId);
        return removed > 0;
    }

    public bool RemoveMailForTomorrow(string mailId)
    {
        var removed = Tomorrow.RemoveAll(x => x.Id == mailId);
        return removed > 0;
    }
}


internal sealed class FakeRecipeService : IGameRecipeService
{
    public readonly HashSet<string> Crafting = new(StringComparer.Ordinal);
    public readonly HashSet<string> Cooking = new(StringComparer.Ordinal);
    public IReadOnlyCollection<string> Learned => Crafting.Union(Cooking).ToArray();

    public bool HasCraftingRecipe(string recipeName) => Crafting.Contains(recipeName);
    public bool HasCookingRecipe(string recipeName) => Cooking.Contains(recipeName);

    public bool EnsureCraftingRecipeLearned(string recipeName)
        => Crafting.Add(recipeName);

    public bool EnsureCookingRecipeLearned(string recipeName)
        => Cooking.Add(recipeName);

    public bool ForgetCraftingRecipe(string recipeName)
        => Crafting.Remove(recipeName);

    public bool ForgetCookingRecipe(string recipeName)
        => Cooking.Remove(recipeName);
}

internal sealed class FakeQuestService : IGameQuestService
{
    private readonly HashSet<string> _completedQuests = new(StringComparer.Ordinal);
    private readonly HashSet<string> _completedSpecialOrders = new(StringComparer.Ordinal);
    private readonly HashSet<string> _questLog = new(StringComparer.Ordinal);

    public void SetCompletedQuest(string questId, bool completed)
    {
        if (completed)
            _completedQuests.Add(questId);
        else
            _completedQuests.Remove(questId);
    }

    public void SetCompletedSpecialOrder(string orderId, bool completed)
    {
        if (completed)
            _completedSpecialOrders.Add(orderId);
        else
            _completedSpecialOrders.Remove(orderId);
    }

    public bool HasCompletedQuest(string questId) => _completedQuests.Contains(questId);

    public bool HasCompletedSpecialOrder(string orderId) => _completedSpecialOrders.Contains(orderId);

    public bool HasQuestInLog(string questId) => _questLog.Contains(questId);

    public bool CompleteQuest(string questId) => _completedQuests.Add(questId);

    public bool UncompleteQuest(string questId) => _completedQuests.Remove(questId);

    public bool MarkSpecialOrderCompleted(string orderId) => _completedSpecialOrders.Add(orderId);

    public bool UnmarkSpecialOrderCompleted(string orderId) => _completedSpecialOrders.Remove(orderId);

    public void AddQuest(string questId) => _questLog.Add(questId);

    public bool RemoveQuest(string questId) => _questLog.Remove(questId);

    public bool AddSpecialOrder(string orderId) => true;

    public bool RemoveSpecialOrder(string orderId) => true;
}


internal sealed class FakeUnlockFactsProvider : IUnlockFactsProvider
{
    public int DayNumber { get; set; } = 1;
    public bool IsMainPlayer { get; set; } = true;
    public string LocationName { get; set; } = "Farm";
    public string PlayerName { get; set; } = "Player";

    public readonly Dictionary<string, bool> ReceivedMail = new(StringComparer.Ordinal);
    public readonly Dictionary<string, bool> CompletedQuests = new(StringComparer.Ordinal);
    public readonly Dictionary<string, bool> CompletedSpecialOrders = new(StringComparer.Ordinal);

    public UnlockFacts GetFacts(ProgressionSaveData saveData)
        => new(
            dayNumber: DayNumber,
            isMainPlayer: IsMainPlayer,
            locationName: LocationName,
            playerName: PlayerName,
            receivedMail: new Dictionary<string, bool>(ReceivedMail),
            completedQuests: new Dictionary<string, bool>(CompletedQuests),
            completedSpecialOrders: new Dictionary<string, bool>(CompletedSpecialOrders),
            questCompletedDay: saveData.QuestCompletedDay
        );
}
