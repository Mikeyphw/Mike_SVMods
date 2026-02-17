using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Extensions;
using WildEggsHatching.Application.Progression;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.Core.Ids;
using WildEggsHatching.Infrastructure.Game;
using WildEggsHatching.Infrastructure.Logging;
using WildEggsHatching.Infrastructure.ModData;
using WildEggsHatching.Infrastructure.SaveData;

namespace WildEggsHatching.Application.AnimalNotes;

public sealed class AnimalNotesOrchestrator
{
    private const int SaveDataVersion = 1;

    private readonly ISaveDataStore _store;
    private readonly IModDataLoader _dataLoader;
    private readonly IGameStateReader _game;
    private readonly ITranslationHelper _i18n;
    private readonly IGameMailService _mail;
    private readonly IGameQuestService _quests;
    private readonly IGameObjectPlacer _placer;
    private readonly ProgressionService _progression;
    private readonly ILog _log;
    private readonly Func<ProgressionStep> _getStep;
    private readonly Func<bool> _isMainPlayer;

    private AnimalNotesSaveData _data = new();
    private bool _initialized;

    public AnimalNotesOrchestrator(
        ISaveDataStore store,
        IModDataLoader dataLoader,
        IGameStateReader game,
        ITranslationHelper i18n,
        IGameMailService mail,
        IGameQuestService quests,
        IGameObjectPlacer placer,
        ProgressionService progression,
        ILog log,
        Func<ProgressionStep> getStep,
        Func<bool> isMainPlayer)
    {
        _store = store;
        _dataLoader = dataLoader;
        _game = game;
        _i18n = i18n;
        _mail = mail;
        _quests = quests;
        _placer = placer;
        _progression = progression;
        _log = log;
        _getStep = getStep;
        _isMainPlayer = isMainPlayer;
    }

    public void OnSaveLoaded()
    {
        EnsureInitialized();
        Save(); // normalize envelope/version + ensure file exists for debugging.
    }

    public void OnSaving()
    {
        if (!_initialized)
            return;

        Save();
    }

    public void OnDayStarted()
    {
        if (!Context.IsWorldReady)
            return;

        if (!ShouldRun())
            return;

        EnsureInitialized();

        RefreshAnimalsOnFarm();
        FindNewHatchedAnimals();
        ProcessPending();
    }

    public void OnDayEnding()
    {
        if (!Context.IsWorldReady)
            return;

        if (!ShouldRun())
            return;

        EnsureInitialized();
        ProcessPending();
    }

    public void OnWarped(GameLocation newLocation)
    {
        if (!Context.IsWorldReady)
            return;

        if (!ShouldRun())
            return;

        EnsureInitialized();

        HandleLocation(newLocation);
        ProcessPending();
    }

    private bool ShouldRun()
    {
        if (!_isMainPlayer())
            return false;

        return _getStep() >= ProgressionStep.PartVI;
    }

    private void EnsureInitialized()
    {
        if (_initialized)
            return;

        var envelope = _store.ReadEnvelope<AnimalNotesSaveData>(SaveKeys.AnimalNotes);
        if (envelope is null)
        {
            _data = new AnimalNotesSaveData();
        }
        else if (envelope.Version != SaveDataVersion)
        {
            _log.Log($"AnimalNotes: unsupported save data version {envelope.Version}; defaulting.", StardewModdingAPI.LogLevel.Warn);
            _data = new AnimalNotesSaveData();
        }
        else
        {
            _data = envelope.Data ?? new AnimalNotesSaveData();
        }

        _initialized = true;
    }

    private void Save()
        => _store.WriteEnvelope(SaveKeys.AnimalNotes, new SaveDataEnvelope<AnimalNotesSaveData>(SaveDataVersion, _data));

    private HashSet<string> GetEligibleAnimalTypes()
    {
        var snapshot = _dataLoader.Load();

        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var k in snapshot.WildAnimals.Keys)
        {
            if (Game1.farmAnimalData.ContainsKey(k))
                keys.Add(k);
        }

        return keys;
    }

    private void RefreshAnimalsOnFarm()
    {
        var eligible = GetEligibleAnimalTypes();

        if (_data.AnimalsOnTheFarm.Count > 0)
            _data.AnimalsOnTheFarmYesterday = new Dictionary<string, List<long>>(_data.AnimalsOnTheFarm, StringComparer.OrdinalIgnoreCase);

        _data.AnimalsOnTheFarm = GetAnimalsOnFarm(eligible);
        Save();
    }

    private static Dictionary<string, List<long>> GetAnimalsOnFarm(HashSet<string> eligible)
    {
        var dict = new Dictionary<string, HashSet<long>>(StringComparer.OrdinalIgnoreCase);

        foreach (var b in Game1.getFarm().buildings.Where(IsCoopOrBarn))
        {
            if (b.indoors.Value is not AnimalHouse ah)
                continue;

            foreach (var a in ah.animals.Values)
            {
                var type = a.type.Value;
                if (!eligible.Contains(type))
                    continue;

                if (!dict.TryGetValue(type, out var set))
                    dict[type] = set = new HashSet<long>();

                set.Add(a.myID.Value);
            }
        }

        foreach (var a in Game1.getFarm().animals.Values)
        {
            var type = a.type.Value;
            if (!eligible.Contains(type))
                continue;

            if (!dict.TryGetValue(type, out var set))
                dict[type] = set = new HashSet<long>();

            set.Add(a.myID.Value);
        }

        return dict.ToDictionary(p => p.Key, p => p.Value.ToList(), StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsCoopOrBarn(Building building)
        => building.buildingType.Value.ContainsIgnoreCase("coop")
           || building.buildingType.Value.ContainsIgnoreCase("barn");

    private void FindNewHatchedAnimals()
    {
        var today = ToHashSetDict(_data.AnimalsOnTheFarm);
        var yesterday = ToHashSetDict(_data.AnimalsOnTheFarmYesterday);

        foreach (var (type, idsToday) in today)
        {
            if (_data.RegisteredAnimalTypes.Contains(type))
                continue;

            if (_data.Pending.Any(p => string.Equals(p.FarmAnimalName, type, StringComparison.OrdinalIgnoreCase)))
                continue;

            var added = !yesterday.TryGetValue(type, out var idsYesterday)
                ? idsToday
                : idsToday.Except(idsYesterday).ToHashSet();

            if (added.Count == 0)
                continue;

            var animalId = added.First();

            FarmAnimal? animal = null;
            try { animal = Utility.getAnimal(animalId); }
            catch { /* ignored */ }

            if (animal is null)
                continue;

            var home = animal.home?.GetIndoors();
            var homeName = home?.NameOrUniqueName ?? string.Empty;

            var rec = new HatchedAnimalRecord
            {
                FarmAnimalName = type,
                AnimalId = animalId,
                HomeNameOrUniqueName = homeName,
                HouseType = animal.buildingTypeILiveIn?.Name ?? string.Empty,
                NoteItemId = AnimalNoteIds.NoteItemId(type),
                QuestId = AnimalNoteIds.NoteQuestId(type),
                MailId = AnimalNoteIds.NoteMailId(type),
                NotePlaced = false,
                QuestGiven = false,
                DayNotePlaced = null,
                DayQuestGiven = null,
            };

            _data.Pending.Add(rec);
            _log.Log($"AnimalNotes: detected new '{type}' (id={animalId}) home='{homeName}'.", StardewModdingAPI.LogLevel.Trace);
        }

        Save();
    }

    private static Dictionary<string, HashSet<long>> ToHashSetDict(Dictionary<string, List<long>> raw)
    {
        var dict = new Dictionary<string, HashSet<long>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (k, v) in raw)
            dict[k] = new HashSet<long>(v ?? new List<long>());
        return dict;
    }

    private void ProcessPending()
    {
        if (_data.Pending.Count == 0)
            return;

        var day = _game.GetDayNumber();

        foreach (var p in _data.Pending)
            _progression.RecordQuestCompletionDayIfCompleted(p.QuestId);

        TrySpawnNotes(day);
        TryIssueUnlockMail(day);
        CleanupCompleted();
    }

    private void TrySpawnNotes(int dayNumber)
    {
        foreach (var p in _data.Pending.Where(x => !x.NotePlaced).ToList())
        {
            if (Utility.doesItemExistAnywhere(p.NoteItemId))
            {
                p.NotePlaced = true; // already exists somewhere, avoid duplicating.
                continue;
            }

            var home = ResolveAnimalHome(p.HomeNameOrUniqueName);
            if (home is null)
                continue;

            var tile = GetAdjacentOpenTileNearIncubator(home);
            if (tile is null)
                continue;

            if (_placer.TryPlaceObject(home, tile.Value, p.NoteItemId, stack: 1, destroyOvernight: false))
            {
                p.NotePlaced = true;
                p.DayNotePlaced = dayNumber;
                _log.Log($"AnimalNotes: placed note '{p.NoteItemId}' for '{p.FarmAnimalName}' at {tile.Value}.", StardewModdingAPI.LogLevel.Trace);
            }
        }

        Save();
    }

    private static Vector2? GetAdjacentOpenTileNearIncubator(GameLocation location)
    {
        var incubator = location.objects.Values.FirstOrDefault(o => string.Equals(o.ItemId, ModItemIds.WildEggIncubator, StringComparison.OrdinalIgnoreCase));
        var reference = incubator?.TileLocation ?? location.getRandomTile(Game1.random);

        Vector2 tile;
        try { tile = Utility.getRandomAdjacentOpenTile(reference, location); }
        catch { return null; }

        return tile;
    }

    private GameLocation? ResolveAnimalHome(string homeNameOrUniqueName)
    {
        if (string.IsNullOrWhiteSpace(homeNameOrUniqueName))
            return null;

        foreach (var b in Game1.getFarm().buildings)
        {
            if (b.indoors.Value is not AnimalHouse ah)
                continue;

            if (string.Equals(ah.NameOrUniqueName, homeNameOrUniqueName, StringComparison.OrdinalIgnoreCase))
                return ah;
        }

        return null;
    }

    private void HandleLocation(GameLocation location)
    {
        if (_data.Pending.Count == 0)
            return;

        var locName = location.NameOrUniqueName;

        foreach (var p in _data.Pending.Where(x => !x.QuestGiven && string.Equals(x.HomeNameOrUniqueName, locName, StringComparison.OrdinalIgnoreCase)).ToList())
        {
            if (!Utility.ForEachItemIn(location, item => item is not null && item.ItemId.EqualsIgnoreCase(p.NoteItemId)))
                continue;

            try
            {
                var hud = new HUDMessage(_i18n.Get("hudmessage").ToString())
                {
                    timeLeft = 5000,
                    messageSubject = ItemRegistry.Create(p.NoteItemId),
                };
                Game1.addHUDMessage(hud);
            }
            catch { /* HUD is best-effort */ }

            try
            {
                _quests.AddQuest(p.QuestId);
            }
            catch { /* ignored */ }

            p.QuestGiven = true;
            p.DayQuestGiven = _game.GetDayNumber();
            _log.Log($"AnimalNotes: quest '{p.QuestId}' issued for '{p.FarmAnimalName}'.", StardewModdingAPI.LogLevel.Trace);
        }

        Save();
    }

    private void TryIssueUnlockMail(int dayNumber)
    {
        foreach (var p in _data.Pending)
        {
            if (_mail.HasReceivedMail(p.MailId) || _mail.HasMailForTomorrow(p.MailId))
                continue;

            if (!_mail.HasReceivedMail(ProgressionIds.Mail.SpecialOrderDoneII))
                continue;

            if (!_quests.HasCompletedQuest(p.QuestId))
                continue;

            if (!TryGetDaysToMature(p.FarmAnimalName, out var daysToMature))
                continue;

            if (!_progression.TryGetQuestCompletionDay(p.QuestId, out var questDay))
                continue;

            if (dayNumber - questDay < daysToMature + 2)
                continue;

            _mail.AddMailForTomorrow(p.MailId, noLetter: false);
            _log.Log($"AnimalNotes: queued unlock mail '{p.MailId}' for '{p.FarmAnimalName}'.", StardewModdingAPI.LogLevel.Trace);
        }
    }

    private static bool TryGetDaysToMature(string farmAnimalName, out int daysToMature)
    {
        daysToMature = 0;

        try
        {
            if (!Game1.farmAnimalData.TryGetValue(farmAnimalName, out var data))
                return false;

            daysToMature = data.DaysToMature;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void CleanupCompleted()
    {
        var removedAny = false;

        for (var i = _data.Pending.Count - 1; i >= 0; i--)
        {
            var p = _data.Pending[i];
            if (!_mail.HasReceivedMail(p.MailId))
                continue;

            _data.Pending.RemoveAt(i);
            _data.RegisteredAnimalTypes.Add(p.FarmAnimalName);
            removedAny = true;
        }

        if (removedAny)
            Save();
    }
}
