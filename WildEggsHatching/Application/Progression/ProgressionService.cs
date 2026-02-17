using System;
using System.Collections.Generic;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.Core.Ids;
using WildEggsHatching.Infrastructure.Config;
using WildEggsHatching.Infrastructure.Game;
using WildEggsHatching.Infrastructure.Logging;
using WildEggsHatching.Infrastructure.SaveData;

namespace WildEggsHatching.Application.Progression;

public sealed class ProgressionService
{
    private const int SaveDataVersion = 1;

    private readonly ISaveDataStore _store;
    private readonly IConfigProvider _config;
    private readonly IUnlockFactsProvider _facts;
    private readonly IGameQuestService _quests;
    private readonly IGameMailService _mail;
    private readonly IGameRecipeService _recipes;
    private readonly ILog _log;
    private readonly Action _invalidateAssets;

    private ProgressionSaveData _data = new();
    private bool _initialized;
    private bool _schemaCompatible = true;
    private string? _schemaError;

    public event Action<ProgressionStep>? StepChanged;

    public ProgressionService(
        ISaveDataStore store,
        IConfigProvider config,
        IUnlockFactsProvider facts,
        IGameQuestService quests,
        IGameMailService mail,
        IGameRecipeService recipes,
        ILog log,
        Action invalidateAssets
    )
    {
        _store = store;
        _config = config;
        _facts = facts;
        _quests = quests;
        _mail = mail;
        _recipes = recipes;
        _log = log;
        _invalidateAssets = invalidateAssets;
    }

    public ProgressionStep GetCurrentStep()
    {
        var config = _config.Config;

        if (_initialized && !_schemaCompatible)
            return config.ProgressionMode ? ProgressionStep.PartI : ProgressionRules.MaxStep;

        if (!_initialized)
            return config.ProgressionMode ? ProgressionStep.PartI : ProgressionRules.MaxStep;

        if (!config.ProgressionMode)
            return ProgressionRules.MaxStep;

        Evaluate(ProgressionTrigger.OnDemand);
        return _data.Step;
    }

    

    public IReadOnlyDictionary<string, int> GetQuestCompletionDays()
    {
        EnsureInitialized();
        return new Dictionary<string, int>(_data.QuestCompletedDay);
    }

    /// <summary>
    /// Record completion day for an arbitrary quest ID (including dynamically generated note quests).
    /// This does not affect progression steps.
    /// </summary>
    public void RecordQuestCompletionDayIfCompleted(string questId)
    {
        if (string.IsNullOrWhiteSpace(questId))
            return;

        EnsureInitialized();

        var facts = _facts.GetFacts(_data);
        if (!facts.IsMainPlayer)
            return;

        if (_data.QuestCompletedDay.ContainsKey(questId))
            return;

        if (_quests.HasCompletedQuest(questId))
        {
            _data.QuestCompletedDay[questId] = facts.DayNumber;
            Save();
        }
    }

    public bool TryGetQuestCompletionDay(string questId, out int dayNumber)
    {
        EnsureInitialized();
        return _data.QuestCompletedDay.TryGetValue(questId, out dayNumber);
    }


    /// <summary>Force progression to a specific step. Intended for host-only console commands.</summary>
    public bool TryForceSetStep(
        ProgressionStep step,
        bool applySideEffects,
        string reason,
        out string message)
    {
        EnsureInitialized();

        var config = _config.Config;
        var facts = _facts.GetFacts(_data);

        if (!facts.IsMainPlayer)
        {
            message = "Host-only: clients cannot mutate progression.";
            return false;
        }

        if (!config.ProgressionMode)
        {
            message = "ProgressionMode is disabled; step is fixed to MaxStep.";
            return false;
        }

        NormalizeStateForConfig(config);

        if (!config.DesertTombModuleEnabled && step == ProgressionStep.PartV)
            step = ProgressionStep.PartVI;

        if (step < ProgressionStep.PartI || step > ProgressionRules.MaxStep)
        {
            message = $"Invalid step '{step}'.";
            return false;
        }

        if (step == _data.Step)
        {
            message = $"No-op: already at {step}.";
            return true;
        }

        var from = _data.Step;
        _data.Step = step;
        _data.LastStepAdvanceDay = facts.DayNumber;

        if (applySideEffects)
            ProgressionRules.ApplyStepUnlockSideEffects(step, _mail, config);

        _invalidateAssets();
        StepChanged?.Invoke(step);

        _log.Log($"Progression forced {from} -> {step}: {reason}", StardewModdingAPI.LogLevel.Info);

        message = $"Progression {from} -> {step}. (rollback: wegg step set {from})";
        return true;
    }

    public void SaveNow()
    {
        if (!_initialized)
            return;

        Save();
    }

public void OnSaveLoaded()
    {
        EnsureInitialized();

        if (!_config.Config.ProgressionMode)
        {
            _recipes.EnsureCraftingRecipeLearned("Elixir Of Revitalized Essence");
            _recipes.EnsureCraftingRecipeLearned("Wild Egg Incubator");
            return;
        }

        Evaluate(ProgressionTrigger.SaveLoaded);
    }

    public void OnDayStarted()
    {
        EnsureInitialized();

        if (!_config.Config.ProgressionMode)
            return;

        SyncQuestCompletionDays();
        Evaluate(ProgressionTrigger.DayStarted);
    }

    public void OnUpdateTicked(bool isMultipleOf60Ticks)
    {
        if (!isMultipleOf60Ticks)
            return;

        EnsureInitialized();

        if (!_config.Config.ProgressionMode)
            return;

        // "QuestCompleted" trigger via polling, since SMAPI doesn't expose a quest-completion event.
        if (SyncQuestCompletionDays())
            Evaluate(ProgressionTrigger.QuestCompleted);
    }

    public void OnDayEnding()
    {
        if (!_initialized)
            return;

        Save();
    }

    private void EnsureInitialized()
    {
        if (_initialized)
            return;

        var envelope = _store.ReadEnvelope<ProgressionSaveData>(SaveKeys.Progression);
        if (envelope is null)
        {
            _data = new ProgressionSaveData();
        }
        else if (envelope.Version != SaveDataVersion)
        {
            _schemaCompatible = false;
            _schemaError = $"Unsupported progression save schema version {envelope.Version} (expected {SaveDataVersion}).";
            _log.Log($"{_schemaError} NOT resetting. Progression will default to config but will not persist until the save data is cleared or updated.", StardewModdingAPI.LogLevel.Error);
            _data = envelope.Data ?? new ProgressionSaveData();
        }
        else
        {
            _data = envelope.Data ?? new ProgressionSaveData();
        }

        if (_schemaCompatible)
            NormalizeStateForConfig(_config.Config);

        _initialized = true;
    }

    private void Save()
    {
        if (!_schemaCompatible)
            return;

        var config = _config.Config;
        var facts = _facts.GetFacts(_data);

        if (!facts.IsMainPlayer)
            return;

        NormalizeStateForConfig(config);

        _store.WriteEnvelope(SaveKeys.Progression, new SaveDataEnvelope<ProgressionSaveData>(SaveDataVersion, _data));
    }

    private bool SyncQuestCompletionDays()
    {
        var config = _config.Config;
        var facts = _facts.GetFacts(_data);

        if (!facts.IsMainPlayer)
            return false;

        if (!config.ProgressionMode)
            return false;

        var changed = false;

        foreach (var id in ProgressionIds.Quests.Tracked)
        {
            if (_data.QuestCompletedDay.ContainsKey(id))
                continue;

            if (id == ProgressionIds.Quests.SpecialOrderWildEggsI)
            {
                // Parity with orig: completion day is recorded when the special-order "done" mail is received.
                if (!_mail.HasReceivedMail(ProgressionIds.Mail.SpecialOrderDoneI))
                    continue;

                _data.QuestCompletedDay[id] = facts.DayNumber;
                changed = true;
                continue;
            }

            if (id == ProgressionIds.Quests.SpecialOrderWildEggsII)
            {
                if (!_mail.HasReceivedMail(ProgressionIds.Mail.SpecialOrderDoneII))
                    continue;

                _data.QuestCompletedDay[id] = facts.DayNumber;
                changed = true;
                continue;
            }

            if (_quests.HasCompletedQuest(id))
            {
                _data.QuestCompletedDay[id] = facts.DayNumber;
                changed = true;
            }
        }

        return changed;
    }

    private void Evaluate(ProgressionTrigger trigger)
    {
        if (!_schemaCompatible)
            return;

        var config = _config.Config;
        var facts = _facts.GetFacts(_data);

        if (!facts.IsMainPlayer)
            return;
        if (!config.ProgressionMode)
            return;

        NormalizeStateForConfig(config);

        // Avoid multiple advances in the same day across triggers (DayStarted + polling + on-demand).
        if (_data.LastStepAdvanceDay == facts.DayNumber)
            return;

        var next = GetNextStep(_data.Step, config);
        if (next is null)
            return;

        if (!ProgressionRules.IsStepUnlocked(next.Value, facts, config))
            return;

        SetStep(next.Value, trigger, "Unlock conditions met");
        _data.LastStepAdvanceDay = facts.DayNumber;
    }

    private ProgressionStep? GetNextStep(ProgressionStep current, ModConfig config)
    {
        if (current >= ProgressionRules.MaxStep)
            return null;

        var next = (ProgressionStep)((int)current + 1);

        // Auto-skip DesertTomb-dependent step(s) while preserving parity IDs.
        if (!config.DesertTombModuleEnabled && next == ProgressionStep.PartV)
            next = ProgressionStep.PartVI;

        return next > ProgressionRules.MaxStep ? null : next;
    }

    private void SetStep(ProgressionStep step, ProgressionTrigger trigger, string reason)
    {
        if (step == _data.Step)
            return;

        var from = _data.Step;
        _data.Step = step;

        ProgressionRules.ApplyStepUnlockSideEffects(step, _mail, _config.Config);

        _invalidateAssets();
        StepChanged?.Invoke(step);

        _log.Log($"Progression {from} -> {step} ({trigger}): {reason}");
    }

    private void NormalizeStateForConfig(ModConfig config)
    {
        if (!config.ProgressionMode)
            return;

        if (!config.DesertTombModuleEnabled && _data.Step == ProgressionStep.PartV)
            _data.Step = ProgressionStep.PartVI;
    }

    private enum ProgressionTrigger
    {
        SaveLoaded,
        DayStarted,
        QuestCompleted,
        OnDemand,
    }
}
