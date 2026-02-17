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
    private const int SaveDataVersion = 2;

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
    private readonly Action<long, AnimalNoteQueueMailMessage>? _sendQueueMailMessage;

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
        Func<bool> isMainPlayer,
        Action<long, AnimalNoteQueueMailMessage>? sendQueueMailMessage)
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
        _sendQueueMailMessage = sendQueueMailMessage;
    }
    public string DumpState()
    {
        if (!Context.IsWorldReady)
            return "World not ready.";

        try { EnsureInitialized(); }
        catch (Exception ex) { return $"AnimalNotes init failed: {ex.Message}"; }

        var day = _game.GetDayNumber();
        var farmers = SafeGetAllFarmers();

        var lines = new List<string>
        {
            $"AnimalNotes: step={_getStep()} day={day} pending={_data.Pending.Count} registeredTypes={_data.RegisteredAnimalTypes.Count}",
            $"Config: debug={_debugMode} warnAfterDays={_warnAfterDays} placementRadius={_placementRadius}",
            $"Farmers: {string.Join(", ", farmers.Select(f => $"{f.Name}({f.UniqueMultiplayerID})"))}"
        };

        foreach (var p in _data.Pending.OrderBy(x => x.FarmAnimalName).ThenBy(x => x.AnimalId))
        {
            var noteExists = Utility.doesItemExistAnywhere(p.NoteItemId);
            var noteBlock = GetNoteBlockedReason(p, noteExists);

            lines.Add(
                $" - {p.FarmAnimalName} id={p.AnimalId} home='{p.HomeNameOrUniqueName}' token='{p.UniqueToken}' " +
                $"note='{p.NoteItemId}' notePlaced={p.NotePlaced} noteExistsNow={noteExists} quest='{p.QuestId}' mail='{p.MailId}'");

            if (!string.IsNullOrWhiteSpace(noteBlock))
                lines.Add($"    noteBlocked: {noteBlock}");

            foreach (var farmer in farmers)
            {
                var state = GetOrCreateFarmerState(p, farmer.UniqueMultiplayerID);
                var questActive = farmer.questLog.Any(q => string.Equals(q.id.Value, p.QuestId, StringComparison.OrdinalIgnoreCase));
                var questCompleted = HasCompletedQuest(farmer, p.QuestId);

                var mailReceived = HasReceivedMail(farmer, p.MailId);
                var mailTomorrow = HasMailForTomorrow(farmer, p.MailId);

                var questBlock = GetQuestBlockedReason(p, farmer, noteExists, questActive, questCompleted, state);
                var mailBlock = GetMailBlockedReason(p, day, farmer, mailTomorrow, mailReceived, state);

                lines.Add(
                    $"    farmer={farmer.Name}({farmer.UniqueMultiplayerID}) questGiven={state.QuestGiven} questActive={questActive} questCompleted={questCompleted} questDay={state.DayQuestCompleted?.ToString() ?? "-"} " +
                    $"mailQueued={state.MailQueued} mailTomorrow={mailTomorrow} mailReceived={mailReceived}");

                if (!string.IsNullOrWhiteSpace(questBlock))
                    lines.Add($"      questBlocked: {questBlock}");

                if (!string.IsNullOrWhiteSpace(mailBlock))
                    lines.Add($"      mailBlocked: {mailBlock}");
            }

            if (p.ConsecutivePlacementFailureDays > 0 || !string.IsNullOrWhiteSpace(p.LastPlacementFailureReason))
                lines.Add($"    placement: failDays={p.ConsecutivePlacementFailureDays} lastFail='{p.LastPlacementFailureReason}'");
        }

        return string.Join(Environment.NewLine, lines);
    }

    public bool TryHandleClientNoteFound(AnimalNoteFoundMessage message, long farmerId, out string questId)
    {
        questId = string.Empty;

        if (!Context.IsWorldReady)
            return false;

        if (message is null)
            return false;

        EnsureInitialized();
        ReconcileState();

        if (!IsJunimoNoteId(message.NoteItemId))
            return false;

        if (!Utility.doesItemExistAnywhere(message.NoteItemId))
        {
            _log.Log($"AnimalNotes: client '{message.FoundByFarmerName}' reported note '{message.NoteItemId}', but it no longer exists.", StardewModdingAPI.LogLevel.Trace);
            return false;
        }

        var token = ExtractAnimalTokenFromNoteId(message.NoteItemId);
        if (string.IsNullOrWhiteSpace(token))
            return false;

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
            };

            _data.Pending.Add(rec);
        }

        questId = rec.QuestId;

        var state = GetOrCreateFarmerState(rec, farmerId);
        if (state.QuestGiven)
            return true;

        state.QuestGiven = true;
        state.DayQuestGiven ??= _game.GetDayNumber();

        Save();
        return true;
    }

    public bool TryHandleClientQuestCompleted(AnimalNoteQuestCompletedMessage message, long farmerId)
    {
        if (!Context.IsWorldReady)
            return false;

        if (message is null || string.IsNullOrWhiteSpace(message.QuestId))
            return false;

        EnsureInitialized();
        ReconcileState();

        var rec = _data.Pending.FirstOrDefault(p => string.Equals(p.QuestId, message.QuestId, StringComparison.OrdinalIgnoreCase));
        if (rec is null)
            return false;

        var state = GetOrCreateFarmerState(rec, farmerId);
        if (state.DayQuestCompleted is not null)
            return true;

        state.DayQuestCompleted = _game.GetDayNumber();
        Save();
        return true;
    }

    // Back-compat wrapper.
    public void OnClientNoteFound(AnimalNoteFoundMessage message)
        => TryHandleClientNoteFound(message, Game1.player?.UniqueMultiplayerID ?? 0, out _);


    // Back-compat wrapper.
    public void OnClientNoteFound(AnimalNoteFoundMessage message)
        => TryHandleClientNoteFound(message, out _);




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

        TrackQuestCompletionsForAllFarmers(day);
        TrySpawnNotes(day);
        TryIssueUnlockMailForAllFarmers(day);
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

        var farmer = Game1.player;
        if (farmer is null)
            return;

        var farmerId = farmer.UniqueMultiplayerID;
        var day = _game.GetDayNumber();
        var locName = location.NameOrUniqueName;

        foreach (var p in _data.Pending
                     .Where(x => string.Equals(x.HomeNameOrUniqueName, locName, StringComparison.OrdinalIgnoreCase))
                     .ToList())
        {
            if (!Utility.doesItemExistAnywhere(p.NoteItemId))
                continue;

            var state = GetOrCreateFarmerState(p, farmerId);
            if (state.QuestGiven)
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

            TryAddQuestToFarmer(farmer, p.QuestId);

            state.QuestGiven = true;
            state.DayQuestGiven ??= day;
            _log.Log($"AnimalNotes: quest '{p.QuestId}' issued for '{p.FarmAnimalName}' (farmer={farmerId}).", StardewModdingAPI.LogLevel.Trace);
        }

        Save();
    }

    private void TryIssueUnlockMailForAllFarmers(int dayNumber)
    {
        var farmers = SafeGetAllFarmers();
        if (farmers.Count == 0)
            return;

        var online = new HashSet<long>(Game1.getOnlineFarmers().Select(f => f.UniqueMultiplayerID));

        foreach (var p in _data.Pending)
        {
            if (!TryGetDaysToMature(p.FarmAnimalName, out var daysToMature))
                continue;

            foreach (var farmer in farmers)
            {
                var farmerId = farmer.UniqueMultiplayerID;
                var state = GetOrCreateFarmerState(p, farmerId);

                if (HasReceivedMail(farmer, p.MailId) || HasMailForTomorrow(farmer, p.MailId))
                {
                    state.MailQueued = true;
                    continue;
                }

                if (!HasReceivedMail(farmer, ProgressionIds.Mail.SpecialOrderDoneII))
                    continue;

                if (state.DayQuestCompleted is null)
                    continue;

                if (dayNumber - state.DayQuestCompleted.Value < daysToMature + 2)
                    continue;

                AddMailForTomorrow(farmer, p.MailId, noLetter: false);
                state.MailQueued = true;
                state.DayMailQueued ??= dayNumber;

                // For online farmhands, also send a message so they queue it locally immediately.
                if (!_isMainPlayer() || farmerId == Game1.player.UniqueMultiplayerID)
                    continue;

                if (online.Contains(farmerId))
                {
                    try { _sendQueueMailMessage?.Invoke(farmerId, new AnimalNoteQueueMailMessage { MailId = p.MailId, NoLetter = false }); }
                    catch { /* best-effort */ }
                }

                _log.Log($"AnimalNotes: queued unlock mail '{p.MailId}' for '{p.FarmAnimalName}' (farmer={farmerId}).", StardewModdingAPI.LogLevel.Trace);
            }
        }

        Save();
    }

    private void TrackQuestCompletionsForAllFarmers(int dayNumber)
    {
        var farmers = SafeGetAllFarmers();
        if (farmers.Count == 0)
            return;

        foreach (var p in _data.Pending)
        {
            foreach (var farmer in farmers)
            {
                var state = GetOrCreateFarmerState(p, farmer.UniqueMultiplayerID);
                if (state.DayQuestCompleted is not null)
                    continue;

                if (HasCompletedQuest(farmer, p.QuestId))
                    state.DayQuestCompleted = dayNumber;
            }
        }

        Save();
    }

    private static List<Farmer> SafeGetAllFarmers()
    {
        try { return Game1.getAllFarmers().ToList(); }
        catch { return new List<Farmer>(); }
    }

    private static AnimalNotesFarmerState GetOrCreateFarmerState(HatchedAnimalRecord record, long farmerId)
    {
        if (!record.FarmerStates.TryGetValue(farmerId, out var state) || state is null)
        {
            state = new AnimalNotesFarmerState();
            record.FarmerStates[farmerId] = state;
        }

        return state;
    }

    private static void TryAddQuestToFarmer(Farmer farmer, string questId)
    {
        if (string.IsNullOrWhiteSpace(questId))
            return;

        try
        {
            if (farmer.questLog.Any(q => string.Equals(q.id.Value, questId, StringComparison.OrdinalIgnoreCase)))
                return;
        }
        catch
        {
            // ignored
        }

        try { farmer.addQuest(questId); }
        catch { /* ignored */ }
    }

    private static bool HasCompletedQuest(Farmer farmer, string questId)
    {
        var method = farmer.GetType().GetMethod("hasCompletedQuest", new[] { typeof(string) });
        if (method is null)
            return false;

        try { return (bool)(method.Invoke(farmer, new object[] { questId }) ?? false); }
        catch { return false; }
    }

    private static bool HasReceivedMail(Farmer farmer, string mailId)
        => farmer.mailReceived.Contains(mailId);

    private static bool HasMailForTomorrow(Farmer farmer, string mailId)
        => farmer.mailForTomorrow.Any(entry => string.Equals(entry, mailId, StringComparison.Ordinal) || entry.StartsWith(mailId + "%&", StringComparison.Ordinal));

    private static void AddMailForTomorrow(Farmer farmer, string mailId, bool noLetter)
    {
        if (HasReceivedMail(farmer, mailId) || HasMailForTomorrow(farmer, mailId))
            return;

        farmer.mailForTomorrow.Add(noLetter ? mailId + "%&NL&%" : mailId);
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
        var farmers = SafeGetAllFarmers();
        if (farmers.Count == 0)
            return;

        var removedAny = false;

        for (var i = _data.Pending.Count - 1; i >= 0; i--)
        {
            var p = _data.Pending[i];
            if (!farmers.All(f => HasReceivedMail(f, p.MailId)))
                continue;

            _data.Pending.RemoveAt(i);
            _data.RegisteredAnimalTypes.Add(p.FarmAnimalName);
            removedAny = true;
        }

        if (removedAny)
            Save();
    }
}
