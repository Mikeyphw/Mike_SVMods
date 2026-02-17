using System;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using WildEggsHatching.Application.AnimalNotes.Messages;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.Infrastructure.Logging;

namespace WildEggsHatching.Application.AnimalNotes;

public sealed class AnimalNotesClientObserver
{
    private readonly IModHelper _helper;
    private readonly ILog _log;

    private readonly string _modUniqueId;
    private readonly Func<ProgressionStep> _getStep;
    private readonly Func<bool> _allowClientDiscoveries;

    private int _lastDay = -1;
    private readonly HashSet<string> _sentToday = new(StringComparer.OrdinalIgnoreCase);

    public AnimalNotesClientObserver(
        IModHelper helper,
        ILog log,
        string modUniqueId,
        Func<ProgressionStep> getStep,
        Func<bool> allowClientDiscoveries)
    {
        _helper = helper;
        _log = log;
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
            _sentToday.Clear();
        }
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

        if (newLocation is not AnimalHouse)
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
            if (_sentToday.Contains(noteId))
                continue;

            _sentToday.Add(noteId);

            var payload = new AnimalNoteFoundMessage
            {
                NoteItemId = noteId,
                HomeNameOrUniqueName = locName,
                FoundByFarmerName = Game1.player?.Name ?? string.Empty
            };

            try
            {
                _helper.Multiplayer.SendMessage(payload, "WeggAnimalNoteFound", new[] { _modUniqueId });
            }
            catch
            {
                // ignored (best-effort)
            }

            _log.Log($"AnimalNotes: client reported note '{noteId}' in '{locName}'.", LogLevel.Trace);
        }
    }

    private static bool IsJunimoNoteId(string itemId)
        => !string.IsNullOrWhiteSpace(itemId)
           && itemId.StartsWith($"{Constants.ModId}_junimoNote_", StringComparison.OrdinalIgnoreCase);
}
