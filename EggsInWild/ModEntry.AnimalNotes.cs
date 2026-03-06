using System;
using EggsInWild.Application.AnimalNotes;
using EggsInWild.Application.AnimalNotes.Messages;
using EggsInWild.Application.Progression;
using EggsInWild.Infrastructure.Game;
using EggsInWild.Infrastructure.Diagnostics;
using EggsInWild.Infrastructure.ModData;
using Mikew.NestCore.Framework;
using Mikew.NestCore.Helpers;
using Mikew.NestCore.Helpers.Config;
using Mikew.NestCore.Helpers.Logging;
using Mikew.NestCore.Helpers.SaveData;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace EggsInWild;

public sealed partial class ModEntry
{
    private AnimalNotesOrchestrator? _animalNotes;
    private AnimalNotesStartupValidator? _animalNotesValidator;
    private AnimalNotesClientObserver? _animalNotesClientObserver;

    private void InitializeAnimalNotesRuntime(ILog log)
    {
        if (_config is null || _contentDataLoader is null || _contentProgression is null)
            return;

        var saveData = new SmapiSaveDataStore(Helper, Monitor);
        var game = new GameStateReader();
        var i18n = new I18TranslationHelper(Helper);
        var mail = new GameMailService();
        var quests = new GameQuestService();
        var placer = new GameObjectPlacer();

        _animalNotes ??= new AnimalNotesOrchestrator(
            store: saveData,
            dataLoader: _contentDataLoader,
            game: game,
            i18n: i18n,
            debugMode: _config.Get().AnimalNotesDebugMode,
            warnAfterDays: _config.Get().AnimalNotesWarnAfterDays,
            placementRadius: _config.Get().AnimalNotesPlacementRadius,
            mail: mail,
            quests: quests,
            placer: placer,
            progression: _contentProgression,
            log: log,
            getStep: _contentProgression.GetCurrentStep,
            isMainPlayer: () => Context.IsMainPlayer,
            sendQueueMailMessage: SendAnimalNoteQueueMailMessage);

        _animalNotesValidator ??= new AnimalNotesStartupValidator(
            _contentDataLoader,
            log,
            _contentProgression.GetCurrentStep);

        _animalNotesClientObserver ??= new AnimalNotesClientObserver(
            helper: Helper,
            log: log,
            dataLoader: _contentDataLoader,
            modUniqueId: ModManifest.UniqueID,
            getStep: _contentProgression.GetCurrentStep,
            allowClientDiscoveries: () => _config.Get().AnimalNotesAllowClientDiscoveries);
    }

    private void OnDayEnding(object? sender, DayEndingEventArgs e)
    {
        _animalNotes?.OnDayEnding();
    }

    private void OnSaving(object? sender, SavingEventArgs e)
    {
        _animalNotes?.OnSaving();
    }

    private void OnWarped(object? sender, WarpedEventArgs e)
    {
        _animalNotes?.OnWarped(e.NewLocation);
        _animalNotesClientObserver?.OnWarped(e.NewLocation);
    }

    private void OnModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
    {
        if (!string.Equals(e.FromModID, ModManifest.UniqueID, StringComparison.OrdinalIgnoreCase))
            return;

        switch (e.Type)
        {
            case "WeggAnimalNoteFound":
                if (!Context.IsMainPlayer || _animalNotes is null)
                    return;

                var noteFound = e.ReadAs<AnimalNoteFoundMessage>();
                if (_animalNotes.TryHandleClientNoteFound(noteFound, e.FromPlayerID, out var questId, out var shouldGrantQuest) && shouldGrantQuest)
                {
                    SendAnimalNoteGrantQuestMessage(e.FromPlayerID, new AnimalNoteGrantQuestMessage
                    {
                        QuestId = questId,
                        NoteItemId = noteFound.NoteItemId
                    });
                }
                return;

            case "WeggAnimalNoteQuestCompleted":
                if (!Context.IsMainPlayer || _animalNotes is null)
                    return;

                _animalNotes.TryHandleClientQuestCompleted(e.ReadAs<AnimalNoteQuestCompletedMessage>(), e.FromPlayerID);
                return;

            case "WeggAnimalNoteGrantQuest":
                _animalNotesClientObserver?.OnHostGrantQuest(e.ReadAs<AnimalNoteGrantQuestMessage>());
                return;

            case "WeggAnimalNoteQueueMail":
                _animalNotesClientObserver?.OnHostQueueMail(e.ReadAs<AnimalNoteQueueMailMessage>());
                return;
        }
    }

    private void SendAnimalNoteGrantQuestMessage(long farmerId, AnimalNoteGrantQuestMessage message)
    {
        try
        {
            Helper.Multiplayer.SendMessage(
                message,
                "WeggAnimalNoteGrantQuest",
                modIDs: new[] { ModManifest.UniqueID },
                playerIDs: new[] { farmerId });
        }
        catch
        {
        }
    }

    private void SendAnimalNoteQueueMailMessage(long farmerId, AnimalNoteQueueMailMessage message)
    {
        try
        {
            Helper.Multiplayer.SendMessage(
                message,
                "WeggAnimalNoteQueueMail",
                modIDs: new[] { ModManifest.UniqueID },
                playerIDs: new[] { farmerId });
        }
        catch
        {
        }
    }

    private void UpdateDiagnosticsState()
    {
        WeggDiagnosticsState.GmcmRegistered = _gmcmHost is not null;
        WeggDiagnosticsState.NestCoreApiVersion =
            Helper.ModRegistry.GetApi<INestCoreApi>("Mikew.nestcore")?.ApiVersion;
    }
}
