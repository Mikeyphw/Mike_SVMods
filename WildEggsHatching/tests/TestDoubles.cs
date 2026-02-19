using System;
using System.Collections.Generic;
using System.Linq;
using WildEggsHatching.Application.Progression;
using WildEggsHatching.Application.Spawning;
using WildEggsHatching.Infrastructure.Config;
using Mikew.BirdsOfAFeather.Helpers.Logging;
using WildEggsHatching.Infrastructure.ModData.Models;
using WildEggsHatching.Infrastructure.Mods;
using Mikew.BirdsOfAFeather.Helpers.SaveData;
using Mikew.BirdsOfAFeather.Helpers.Config;

namespace WildEggsHatching.Refactor.Tests;

internal sealed class FakeLog : ILog
{
    public List<string> Messages { get; } = new();

    public void Log(string message, StardewModdingAPI.LogLevel level = StardewModdingAPI.LogLevel.Debug)
        => Messages.Add($"{level}:{message}");
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

    public void Delete(string key)
        => _data.Remove(key);

    public SaveDataEnvelope<T>? ReadEnvelope<T>(string key) where T : class
        => _data.TryGetValue(key, out var value) ? value as SaveDataEnvelope<T> : null;

    public void WriteEnvelope<T>(string key, SaveDataEnvelope<T> envelope) where T : class
        => _data[key] = envelope;

    public bool ContainsKey(string key) => _data.ContainsKey(key);
}

internal sealed class FakeConfigProvider : SmapiConfigProvider<ModConfig>
{
    public FakeConfigProvider(ModConfig config) => Config = config;

    public ModConfig Config { get; private set; }

    public event EventHandler<ConfigChangedEventArgs>? ConfigChanged;

    public void Reload() { }

    public void Save() { }

    public void Update(Action<ModConfig> mutator, string reason)
    {
        var oldConfig = Clone(Config);
        mutator(Config);
        ConfigChanged?.Invoke(this, new ConfigChangedEventArgs(oldConfig, Config, reason));
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
}


internal sealed class FakeRecipeService : IGameRecipeService
{
    public readonly List<string> Learned = new();

    public bool EnsureCraftingRecipeLearned(string recipeName)
    {
        if (!Learned.Contains(recipeName))
            Learned.Add(recipeName);

        return true;
    }
}

internal sealed class FakeQuestService : IGameQuestService
{
    private readonly HashSet<string> _quests = new(StringComparer.Ordinal);
    private readonly HashSet<string> _specialOrders = new(StringComparer.Ordinal);

    public void SetCompletedQuest(string questId, bool completed)
    {
        if (completed)
            _quests.Add(questId);
        else
            _quests.Remove(questId);
    }

    public void SetCompletedSpecialOrder(string orderId, bool completed)
    {
        if (completed)
            _specialOrders.Add(orderId);
        else
            _specialOrders.Remove(orderId);
    }

    public bool HasCompletedQuest(string questId) => _quests.Contains(questId);

    public bool HasCompletedSpecialOrder(string orderId) => _specialOrders.Contains(orderId);
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