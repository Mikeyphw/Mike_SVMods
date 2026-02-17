using System;
using System.Collections.Generic;
using StardewModdingAPI;
using System.Linq;
using StardewValley;
using StardewValley.GameData.Objects;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.Infrastructure.Content;
using WildEggsHatching.Infrastructure.Logging;
using WildEggsHatching.Infrastructure.ModData;

namespace WildEggsHatching.Application.AnimalNotes;

public sealed class AnimalNotesStartupValidator
{
    private readonly IModDataLoader _dataLoader;
    private readonly ILog _log;
    private readonly Func<ProgressionStep> _getStep;

    public AnimalNotesStartupValidator(IModDataLoader dataLoader, ILog log, Func<ProgressionStep> getStep)
    {
        _dataLoader = dataLoader;
        _log = log;
        _getStep = getStep;
    }

    public void ValidateAndLog()
    {
        if (!Context.IsWorldReady)
            return;

        if (_getStep() < ProgressionStep.PartVI)
            return;

        var snapshot = _dataLoader.Load();
        var expectedTokens = snapshot.WildAnimals.Keys
            .Where(k => Game1.farmAnimalData.ContainsKey(k))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var expectedNoteIds = expectedTokens.Select(AnimalNoteIds.NoteItemId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var expectedQuestIds = expectedTokens.Select(AnimalNoteIds.NoteQuestId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var expectedMailIds = expectedTokens.Select(AnimalNoteIds.NoteMailId).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var objects = SafeLoad<Dictionary<string, ObjectData>>(GameAssets.Objects) ?? new Dictionary<string, ObjectData>();
        var quests = SafeLoad<Dictionary<string, string>>(GameAssets.Quests) ?? new Dictionary<string, string>();
        var mail = SafeLoad<Dictionary<string, string>>(GameAssets.Mail) ?? new Dictionary<string, string>();

        var presentNoteIds = objects.Keys.Where(IsJunimoNoteId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var presentQuestIds = quests.Keys.Where(IsNoteQuestId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var presentMailIds = mail.Keys.Where(IsNoteMailId).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missingNotes = expectedNoteIds.Except(presentNoteIds).ToList();
        var missingQuests = expectedQuestIds.Except(presentQuestIds).ToList();
        var missingMail = expectedMailIds.Except(presentMailIds).ToList();

        var extraNotes = presentNoteIds.Except(expectedNoteIds).ToList();
        var extraQuests = presentQuestIds.Except(expectedQuestIds).ToList();
        var extraMail = presentMailIds.Except(expectedMailIds).ToList();

        if (missingNotes.Count == 0 && missingQuests.Count == 0 && missingMail.Count == 0 && extraNotes.Count == 0 && extraQuests.Count == 0 && extraMail.Count == 0)
        {
            _log.Log($"AnimalNotes validation: OK (animals={expectedTokens.Count}).", StardewModdingAPI.LogLevel.Info);
            return;
        }

        _log.Log($"AnimalNotes validation: animals={expectedTokens.Count} missing(notes={missingNotes.Count}, quests={missingQuests.Count}, mail={missingMail.Count}) extra(notes={extraNotes.Count}, quests={extraQuests.Count}, mail={extraMail.Count}).", StardewModdingAPI.LogLevel.Warn);

        LogSample("Missing note IDs", missingNotes);
        LogSample("Missing quest IDs", missingQuests);
        LogSample("Missing mail IDs", missingMail);

        LogSample("Extra note IDs", extraNotes);
        LogSample("Extra quest IDs", extraQuests);
        LogSample("Extra mail IDs", extraMail);
    }

    private void LogSample(string label, List<string> ids)
    {
        if (ids.Count == 0)
            return;

        var sample = ids.Take(8);
        _log.Log($"{label}: {string.Join(", ", sample)}{(ids.Count > 8 ? ", ..." : string.Empty)}", StardewModdingAPI.LogLevel.Warn);
    }

    private static T? SafeLoad<T>(string assetName) where T : class
    {
        try { return Game1.content.Load<T>(assetName); }
        catch { return null; }
    }

    private static bool IsJunimoNoteId(string key)
        => key.StartsWith($"{Constants.ModId}_junimoNote_", StringComparison.OrdinalIgnoreCase);

    private static bool IsNoteQuestId(string key)
        => key.StartsWith("wildEggs_", StringComparison.OrdinalIgnoreCase) && key.EndsWith("Note", StringComparison.OrdinalIgnoreCase);

    private static bool IsNoteMailId(string key)
        => key.StartsWith("wildEggs_NoteMail_", StringComparison.OrdinalIgnoreCase);
}
