using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Extensions;
using WildEggsHatching.Application.AnimalNotes.Messages;
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
    private readonly bool _debugMode;
    private readonly int _warnAfterDays;
    private readonly int _placementRadius;
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
        bool debugMode,
        int warnAfterDays,
        int placementRadius,
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
        _debugMode = debugMode;
        _warnAfterDays = Math.Max(1, warnAfterDays);
        _placementRadius = Math.Max(1, placementRadius);
        _mail = mail;
        _quests = quests;
        _placer = placer;
        _progression = progression;
        _log = log;
        _getStep = getStep;
        _isMainPlayer = isMainPlayer;
    }
    public string DumpState()
    {
        if (!Context.IsWorldReady)
            return "World not ready.";

        try { EnsureInitialized(); }
        catch (Exception ex) { return $"AnimalNotes init failed: {ex.Message}"; }

        var lines = new List<string>
        {
            $"AnimalNotes: step={_getStep()} pending={_data.Pending.Count} registeredTypes={_data.RegisteredAnimalTypes.Count}",
        };

        foreach (var p in _data.Pending.OrderBy(x => x.FarmAnimalName).ThenBy(x => x.AnimalId))
        {
            var noteExists = Utility.doesItemExistAnywhere(p.NoteItemId);
            var questActive = Game1.player?.questLog?.Any(q => string.Equals(q.id.Value, p.QuestId, StringComparison.OrdinalIgnoreCase)) ?? false;
            var questCompleted = _quests.HasCompletedQuest(p.QuestId);

            var mailReceived = _mail.HasReceivedMail(p.MailId);
            var mailTomorrow = Game1.player?.mailForTomorrow?.Contains(p.MailId) ?? false;

            lines.Add(
                $" - {p.FarmAnimalName} id={p.AnimalId} home='{p.HomeNameOrUniqueName}' " +
                $"note='{p.NoteItemId}' notePlaced={p.NotePlaced} noteExistsNow={noteExists} " +
                $"quest='{p.QuestId}' questGiven={p.QuestGiven} questActive={questActive} questCompleted={questCompleted} " +
                $"mail='{p.MailId}' mailTomorrow={mailTomorrow} mailReceived={mailReceived} " +
                $"failDays={p.ConsecutivePlacementFailureDays} lastFail='{p.LastPlacementFailureReason}'");
        }

        return string.Join(Environment.NewLine, lines);
    }

    public void OnClientNoteFound(AnimalNoteFoundMessage message)
    {
        if (!Context.IsWorldReady)
            return;

        if (message is null)
            return;

        EnsureInitialized();
        ReconcileState();

        if (!IsJunimoNoteId(message.NoteItemId))
            return;

        if (!Utility.doesItemExistAnywhere(message.NoteItemId))
        {
            _log.Log($"AnimalNotes: client '{message.FoundByFarmerName}' reported note '{message.NoteItemId}', but it no longer exists.", StardewModdingAPI.LogLevel.Trace);
            return;
        }

        var token = ExtractAnimalTokenFromNoteId(message.NoteItemId);
        if (string.IsNullOrWhiteSpace(token))
            return;

        var rec = _data.Pending.FirstOrDefault(p => string.Equals(p.NoteItemId, message.NoteItemId, StringComparison.OrdinalIgnoreCase));
        if (rec is null)
        {
            rec = new HatchedAnimalRecord
            {
                FarmAnimalName = token,
                AnimalId = 0,
                HomeNameOrUniqueName = message.HomeNameOrUniqueName ?? string.Empty,
                HouseType = string.Empty,
                UniqueToken = CreateUniqueToken(0, message.NoteItemId),
                NoteItemId = message.NoteItemId,
                QuestId = AnimalNoteIds.NoteQuestId(token),
                MailId = AnimalNoteIds.NoteMailId(token),
                NotePlaced = true,
                QuestGiven = false,
            };

            _data.Pending.Add(rec);
        }

        if (rec.QuestGiven)
            return;

        try
        {
            _quests.AddQuest(rec.QuestId);
            rec.QuestGiven = true;
            rec.DayQuestGiven ??= _game.GetDayNumber();
            _log.Log($"AnimalNotes: host issued quest '{rec.QuestId}' from client discovery (player='{message.FoundByFarmerName}').", StardewModdingAPI.LogLevel.Info);
        }
        catch (Exception ex)
        {
            _log.Log($"AnimalNotes: failed to issue quest '{rec.QuestId}' from client discovery: {ex}", StardewModdingAPI.LogLevel.Warn);
        }

        Save();
    }



    public void OnSaveLoaded()
    {
        EnsureInitialized();
        ReconcileState();
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
        ReconcileState();
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

        var addedAny = false;

        foreach (var (type, idsToday) in today)
        {
            if (_data.RegisteredAnimalTypes.Contains(type))
                continue;

            var added = !yesterday.TryGetValue(type, out var idsYesterday)
                ? idsToday
                : idsToday.Except(idsYesterday).ToHashSet();

            if (added.Count == 0)
                continue;

            var noteItemId = AnimalNoteIds.NoteItemId(type);

            foreach (var animalId in added)
            {
                if (_data.Pending.Any(p => p.AnimalId == animalId))
                    continue;

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
                    UniqueToken = CreateUniqueToken(animalId, noteItemId),
                    NoteItemId = noteItemId,
                    QuestId = AnimalNoteIds.NoteQuestId(type),
                    MailId = AnimalNoteIds.NoteMailId(type),
                    NotePlaced = false,
                    QuestGiven = false,
                    DayNotePlaced = null,
                    DayQuestGiven = null,
                };

                _data.Pending.Add(rec);
                addedAny = true;

                _log.Log($"AnimalNotes: detected new '{type}' (id={animalId}) home='{homeName}'.", StardewModdingAPI.LogLevel.Trace);
            }
        }

        if (addedAny)
        {
            DeduplicatePending();
            Save();
        }
    }

    private static Dictionary<string, HashSet<long>> ToHashSetDict(Dictionary<string, List<long>> raw)
    {
        var dict = new Dictionary<string, HashSet<long>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (k, v) in raw)
            dict[k] = new HashSet<long>(v ?? new List<long>());
        return dict;
    }

    
    private void ReconcileState()
    {
        EnsureUniqueTokens();
        UpdateHomeReferencesFromCurrentAnimals();
        ReconcileExistingNotesInAnimalHouses();
        NormalizeFlagsFromWorldState();
        DeduplicatePending();
    }

    private static string CreateUniqueToken(long animalId, string noteItemId)
        => $"{animalId}:{noteItemId}";

    private void EnsureUniqueTokens()
    {
        foreach (var p in _data.Pending)
        {
            if (string.IsNullOrWhiteSpace(p.UniqueToken))
                p.UniqueToken = CreateUniqueToken(p.AnimalId, p.NoteItemId);
        }
    }

    private void UpdateHomeReferencesFromCurrentAnimals()
    {
        foreach (var p in _data.Pending)
        {
            if (p.AnimalId <= 0)
                continue;

            FarmAnimal? animal = null;
            try { animal = Utility.getAnimal(p.AnimalId); }
            catch { /* ignored */ }

            if (animal is null)
                continue;

            var home = animal.home?.GetIndoors();
            if (home is not null)
                p.HomeNameOrUniqueName = home.NameOrUniqueName;
        }
    }

    private void ReconcileExistingNotesInAnimalHouses()
    {
        // If a note exists in-world but there is no record, recreate the record so quest/mail still function.
        foreach (var b in Game1.getFarm().buildings.Where(IsCoopOrBarn))
        {
            if (b.indoors.Value is not AnimalHouse ah)
                continue;

            foreach (var obj in ah.objects.Values)
            {
                if (obj is null)
                    continue;

                var noteId = obj.ItemId;
                if (!IsJunimoNoteId(noteId))
                    continue;

                var token = ExtractAnimalTokenFromNoteId(noteId);
                if (string.IsNullOrWhiteSpace(token))
                    continue;

                var animalId = TryFindAnimalIdInHouse(ah, token);

                var exists = _data.Pending.Any(p =>
                    string.Equals(p.NoteItemId, noteId, StringComparison.OrdinalIgnoreCase)
                    && (animalId <= 0 || p.AnimalId == animalId));

                if (exists)
                    continue;

                var rec = new HatchedAnimalRecord
                {
                    FarmAnimalName = token,
                    AnimalId = animalId,
                    HomeNameOrUniqueName = ah.NameOrUniqueName,
                    HouseType = b.buildingType.Value,
                    UniqueToken = CreateUniqueToken(animalId, noteId),
                    NoteItemId = noteId,
                    QuestId = AnimalNoteIds.NoteQuestId(token),
                    MailId = AnimalNoteIds.NoteMailId(token),
                    NotePlaced = true,
                    QuestGiven = false,
                };

                _data.Pending.Add(rec);
            }
        }
    }

    private static long TryFindAnimalIdInHouse(AnimalHouse house, string animalType)
    {
        try
        {
            foreach (var a in house.animals.Values)
            {
                if (string.Equals(a.type.Value, animalType, StringComparison.OrdinalIgnoreCase))
                    return a.myID.Value;
            }
        }
        catch
        {
            // ignored
        }

        return 0;
    }

    private static bool IsJunimoNoteId(string itemId)
        => !string.IsNullOrWhiteSpace(itemId) && itemId.StartsWith($"{Constants.ModId}_junimoNote_", StringComparison.OrdinalIgnoreCase);

    private static string ExtractAnimalTokenFromNoteId(string noteId)
        => noteId.Replace($"{Constants.ModId}_junimoNote_", string.Empty, StringComparison.OrdinalIgnoreCase);

    private void NormalizeFlagsFromWorldState()
    {
        foreach (var p in _data.Pending)
        {
            p.NotePlaced = Utility.doesItemExistAnywhere(p.NoteItemId);

            try
            {
                if (_quests.HasCompletedQuest(p.QuestId) || Game1.player.questLog.Any(q => string.Equals(q.id.Value, p.QuestId, StringComparison.OrdinalIgnoreCase)))
                    p.QuestGiven = true;
            }
            catch
            {
                // ignored
            }
        }
    }

    private void DeduplicatePending()
    {
        if (_data.Pending.Count <= 1)
            return;

        var map = new Dictionary<long, HatchedAnimalRecord>();
        var keep = new List<HatchedAnimalRecord>();

        foreach (var p in _data.Pending)
        {
            if (p.AnimalId <= 0)
            {
                keep.Add(p);
                continue;
            }

            if (!map.TryGetValue(p.AnimalId, out var existing))
            {
                map[p.AnimalId] = p;
                keep.Add(p);
                continue;
            }

            existing.NotePlaced |= p.NotePlaced;
            existing.QuestGiven |= p.QuestGiven;

            existing.DayNotePlaced ??= p.DayNotePlaced;
            existing.DayQuestGiven ??= p.DayQuestGiven;

            if (string.IsNullOrWhiteSpace(existing.HomeNameOrUniqueName))
                existing.HomeNameOrUniqueName = p.HomeNameOrUniqueName;

            if (string.IsNullOrWhiteSpace(existing.UniqueToken))
                existing.UniqueToken = p.UniqueToken;
        }

        _data.Pending = keep;
    }


    private string TranslateOrFallback(string key, string fallback)
    {
        var translated = _i18n.Get(key).ToString();
        return string.Equals(translated, key, StringComparison.OrdinalIgnoreCase) ? fallback : translated;
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
                p.NotePlaced = true;
                ResetPlacementFailures(p);
                continue;
            }

            var home = ResolveAnimalHome(p.HomeNameOrUniqueName);
            if (home is null)
            {
                RecordPlacementFailure(p, dayNumber, "Animal home location not found.");
                continue;
            }

            if (!TryFindPlacementTile(home, out var tile, out var reason))
            {
                RecordPlacementFailure(p, dayNumber, reason);
                continue;
            }

            if (_placer.TryPlaceObject(home, tile, p.NoteItemId, stack: 1, destroyOvernight: false, isSpawnedObject: true))
            {
                p.NotePlaced = true;
                p.DayNotePlaced ??= dayNumber;
                ResetPlacementFailures(p);
                _log.Log($"AnimalNotes: placed note '{p.NoteItemId}' for '{p.FarmAnimalName}' at {tile}.", StardewModdingAPI.LogLevel.Trace);
            }
            else
            {
                RecordPlacementFailure(p, dayNumber, "Failed to place object at chosen tile.");
            }
        }

        Save();
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

        foreach (var p in _data.Pending
                     .Where(x => !x.QuestGiven && string.Equals(x.HomeNameOrUniqueName, locName, StringComparison.OrdinalIgnoreCase))
                     .ToList())
        {
            if (!Utility.doesItemExistAnywhere(p.NoteItemId))
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
            catch
            {
                // HUD is best-effort.
            }

            try { _quests.AddQuest(p.QuestId); }
            catch { /* ignored */ }

            p.QuestGiven = true;
            p.DayQuestGiven ??= _game.GetDayNumber();
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
