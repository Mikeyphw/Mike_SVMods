using System;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI;
using StardewValley;
using WildEggsHatching.Application.AnimalNotes.Messages;
using WildEggsHatching.Core.Enums;
using Mikew.BirdsOfAFeather.Helpers.Logging;
using WildEggsHatching.Infrastructure.ModData;
using SmapiLog = Mikew.BirdsOfAFeather.Helpers.Logging.SmapiLog;

namespace WildEggsHatching.Application.AnimalNotes;

public sealed class AnimalNotesClientObserver
{
    private readonly IModHelper _helper;
    private readonly ILog _log;
    private readonly IModDataLoader _dataLoader;

    private readonly string _modUniqueId;
    private readonly Func<ProgressionStep> _getStep;
    private readonly Func<bool> _allowClientDiscoveries;

    private int _lastDay = -1;
    private readonly HashSet<string> _sentNotesToday = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _sentQuestCompletionsToday = new(StringComparer.OrdinalIgnoreCase);

    private List<string>? _noteQuestIds;

    /// <summary>Helper to emit scoped log messages, falling back to basic log if not a SmapiLog instance.</summary>
    private void LogScoped(string category, string message, LogLevel level)
    {
        if (_log is SmapiLog slog)
            slog.LogScoped(ScopedLogCatalog.ModWe, category, message, level);
        else
            _log.Log(message, level);
    }

    public AnimalNotesClientObserver(
        IModHelper helper,
        ILog log,
        IModDataLoader dataLoader,
        string modUniqueId,
        Func<ProgressionStep> getStep,
        Func<bool> allowClientDiscoveries)
    {
        _helper = helper;
        _log = log;
        _dataLoader = dataLoader;
        _modUniqueId = modUniqueId;
        _getStep = getStep;
        _allowClientDiscoveries = allowClientDiscoveries;
    }

    public void OnDayStarted()
    {
        if (!Context.IsWorldReady)
            return;

        if (_lastDay != Game1.Date.TotalDays)
        {
            _lastDay = Game1.Date.TotalDays;
            _sentNotesToday.Clear();
            _sentQuestCompletionsToday.Clear();
        }

        ReportCompletedNoteQuests();
    }

    public void OnWarped(GameLocation newLocation)
    {
        if (!Context.IsWorldReady)
            return;

        // Host doesn't need to send messages to itself.
        if (Context.IsMainPlayer)
            return;

        if (_getStep() < ProgressionStep.PartVI)
            return;

        if (!_allowClientDiscoveries())
            return;

        if (!string.Equals(newLocation.GetType().Name, "AnimalHouse", StringComparison.OrdinalIgnoreCase))
            return;

        var locName = newLocation.NameOrUniqueName;
        var notes = newLocation.objects.Values
            .Where(o => o is not null && IsJunimoNoteId(o.ItemId))
            .Select(o => o!.ItemId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (notes.Count == 0)
            return;

        foreach (var noteId in notes)
        {
            if (_sentNotesToday.Contains(noteId))
                continue;

            _sentNotesToday.Add(noteId);

            var payload = new AnimalNoteFoundMessage
            {
                NoteItemId = noteId,
                HomeNameOrUniqueName = locName,
                FoundByFarmerName = Game1.player?.Name ?? string.Empty
            };

            TrySend(payload, "WeggAnimalNoteFound");
            LogScoped("animal-notes", $"AnimalNotes: client reported note '{noteId}' in '{locName}'.", LogLevel.Trace);
        }

        // Also opportunistically report quest completions when moving around.
        ReportCompletedNoteQuests();
    }

    public void OnHostGrantQuest(AnimalNoteGrantQuestMessage message)
    {
        if (!Context.IsWorldReady || Context.IsMainPlayer)
            return;

        if (message is null || string.IsNullOrWhiteSpace(message.QuestId))
            return;

        try
        {
            if (Game1.player.questLog.Any(q => string.Equals(q.id.Value, message.QuestId, StringComparison.OrdinalIgnoreCase)))
                return;

            Game1.player.addQuest(message.QuestId);
            LogScoped("animal-notes", $"AnimalNotes: client accepted quest '{message.QuestId}' from host.", LogLevel.Info);
        }
        catch (Exception ex)
        {
            LogScoped("animal-notes", $"AnimalNotes: client failed to add quest '{message.QuestId}': {ex}", LogLevel.Warn);
        }
    }

    public void OnHostQueueMail(AnimalNoteQueueMailMessage message)
    {
        if (!Context.IsWorldReady || Context.IsMainPlayer)
            return;

        if (message is null || string.IsNullOrWhiteSpace(message.MailId))
            return;

        try
        {
            if (HasReceivedMail(Game1.player, message.MailId) || HasMailForTomorrow(Game1.player, message.MailId))
                return;

            Game1.player.mailForTomorrow.Add(message.NoLetter ? message.MailId + "%&NL&%" : message.MailId);
            LogScoped("animal-notes", $"AnimalNotes: client queued mail '{message.MailId}' from host.", LogLevel.Trace);
        }
        catch (Exception ex)
        {
            LogScoped("animal-notes", $"AnimalNotes: client failed to queue mail '{message.MailId}': {ex}", LogLevel.Warn);
        }
    }

    private void ReportCompletedNoteQuests()
    {
        if (!Context.IsWorldReady || Context.IsMainPlayer)
            return;

        if (_getStep() < ProgressionStep.PartVI)
            return;

        if (!_allowClientDiscoveries())
            return;

        EnsureQuestIds();

        if (_noteQuestIds is null || _noteQuestIds.Count == 0)
            return;

        var hasCompleted = Game1.player?.GetType().GetMethod("hasCompletedQuest", new[] { typeof(string) });
        if (hasCompleted is null)
            return;

        foreach (var questId in _noteQuestIds)
        {
            if (_sentQuestCompletionsToday.Contains(questId))
                continue;

            bool completed;
            try { completed = (bool)(hasCompleted.Invoke(Game1.player, new object[] { questId }) ?? false); }
            catch { continue; }

            if (!completed)
                continue;

            _sentQuestCompletionsToday.Add(questId);

            var payload = new AnimalNoteQuestCompletedMessage { QuestId = questId };
            TrySend(payload, "WeggAnimalNoteQuestCompleted");

            LogScoped("animal-notes", $"AnimalNotes: client reported quest completion '{questId}'.", LogLevel.Trace);
        }
    }

    private void EnsureQuestIds()
    {
        if (_noteQuestIds is not null)
            return;

        try
        {
            var snapshot = _dataLoader.Load();
            var tokens = snapshot.WildAnimals.Keys
                .Where(k => Game1.farmAnimalData.ContainsKey(k))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            _noteQuestIds = tokens.Select(AnimalNoteIds.NoteQuestId).ToList();
        }
        catch
        {
            _noteQuestIds = new List<string>();
        }
    }

    private void TrySend<T>(T payload, string type) where T : class
    {
        try { _helper.Multiplayer.SendMessage(payload, type, new[] { _modUniqueId }); }
        catch { /* best-effort */ }
    }

    private static bool IsJunimoNoteId(string itemId)
        => !string.IsNullOrWhiteSpace(itemId)
           && itemId.StartsWith($"{Constants.ModId}_junimoNote_", StringComparison.OrdinalIgnoreCase);

    private static bool HasReceivedMail(Farmer farmer, string mailId)
        => farmer.mailReceived.Contains(mailId);

    private static bool HasMailForTomorrow(Farmer farmer, string mailId)
        => farmer.mailForTomorrow.Any(entry => string.Equals(entry, mailId, StringComparison.Ordinal) || entry.StartsWith(mailId + "%&", StringComparison.Ordinal));
}
