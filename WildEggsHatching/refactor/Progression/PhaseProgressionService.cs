using System;
using System.Collections.Generic;
using StardewValley;
using StardewModdingAPI;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.Core.Events;
using WildEggsHatching.Core.Models;
using WildEggsHatching.Infrastructure.Content;
using WildEggsHatching.Infrastructure.Game;

namespace WildEggsHatching.Progression;

/// <summary>
/// Manages the player's progression through the 6-step wild egg story.
/// Evaluates conditions and advances the player through stages.
/// </summary>
public sealed class PhaseProgressionService
{
    /// <summary>The current progression state.</summary>
    private readonly ProgressionState _state;

    /// <summary>The player's wild animal state.</summary>
    private readonly PlayerWildState _playerState;

    /// <summary>The event bus for publishing progression events.</summary>
    private readonly EventBus _eventBus;

    /// <summary>Interface for reading game state.</summary>
    private readonly IGameStateReader _gameStateReader;

    /// <summary>Interface for sending mail/quests.</summary>
    private readonly IGameMailService _mailService;

    /// <summary>The current game day.</summary>
    private int _currentDay;

    /// <summary>
    /// Days to wait after quest completion for each step.
    /// </summary>
    private static readonly Dictionary<string, int> DaysToWait = new()
    {
        { WildEggConstants.QuestIds.WizardStudy, 2 },
        { WildEggConstants.QuestIds.SpecialOrderWildEggsII, 3 },
        { WildEggConstants.QuestIds.TheDesertScroll, 3 },
    };

    /// <summary>
    /// Creates a new phase progression service.
    /// </summary>
    /// <param name="state">The progression state to manage.</param>
    /// <param name="playerState">The player's animal state.</param>
    /// <param name="eventBus">The event bus for publishing events.</param>
    /// <param name="gameStateReader">The game state reader.</param>
    /// <param name="mailService">The mail service for quest/mail checks.</param>
    public PhaseProgressionService(
        ProgressionState state,
        PlayerWildState playerState,
        EventBus eventBus,
        IGameStateReader gameStateReader,
        IGameMailService mailService)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _playerState = playerState ?? throw new ArgumentNullException(nameof(playerState));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _gameStateReader = gameStateReader ?? throw new ArgumentNullException(nameof(gameStateReader));
        _mailService = mailService ?? throw new ArgumentNullException(nameof(mailService));
    }

    /// <summary>
    /// Creates a new phase progression service with default infrastructure.
    /// </summary>
    public PhaseProgressionService(
        ProgressionState state,
        PlayerWildState playerState,
        EventBus eventBus)
        : this(state, playerState, eventBus, new GameStateReader(), new GameMailService())
    {
    }

    /// <summary>
    /// Evaluates whether the player can advance to the next step.
    /// Should be called at day start.
    /// </summary>
    public void EvaluateProgression()
    {
        _currentDay = _gameStateReader.GetCurrentDay();

        // Update quest completion days from game state
        UpdateQuestCompletionDays();

        var nextStep = _state.CurrentStep + 1;

        if (nextStep > ProgressionStep.PartVI)
        {
            return; // Already at final step
        }

        if (IsStepUnlocked(nextStep))
        {
            AdvanceToStep(nextStep);
        }
    }

    /// <summary>
    /// Updates quest completion days from the game's quest log.
    /// This ensures we track when quests were actually completed.
    /// </summary>
    private void UpdateQuestCompletionDays()
    {
        // Check each known quest to see if it was completed today
        var knownQuests = new[] 
        { 
            WildEggConstants.QuestIds.SpecialOrderWildEggsI,
            WildEggConstants.QuestIds.SpecialOrderWildEggsII,
            WildEggConstants.QuestIds.WizardStudy,
            WildEggConstants.QuestIds.TheDesertScroll
        };

        foreach (var questId in knownQuests)
        {
            // If we don't have a completion day recorded yet
            if (!_playerState.GetQuestCompletionDay(questId).HasValue)
            {
                // Check if quest is completed in game
                if (_mailService.IsQuestCompleted(questId))
                {
                    // Record the completion day (we track from when it was found)
                    // For simplicity, we use the current day
                    _playerState.SetQuestCompletionDay(questId, _currentDay);
                }
            }
        }
    }

    /// <summary>
    /// Advances the player to the specified step.
    /// </summary>
    private void AdvanceToStep(ProgressionStep step)
    {
        _state.AdvanceTo(step, _currentDay);

        // Send appropriate mail for the new step
        SendStepMail(step);

        // Publish event for other services to react to
        _eventBus.Publish(new PhaseUnlockedEvent(step, _currentDay));

        ModEntry.LoggerHelper.Log(
            $"Advanced to progression step: {step}",
            LogLevel.Info
        );
    }

    /// <summary>
    /// Sends the appropriate mail when advancing to a step.
    /// </summary>
    private void SendStepMail(ProgressionStep step)
    {
        switch (step)
        {
            case ProgressionStep.PartII:
                _mailService.AddMailReceived(WildEggConstants.MailIds.MarnieThanks);
                break;
            case ProgressionStep.PartIII:
                _mailService.AddMailReceived(WildEggConstants.MailIds.WizardDone);
                break;
            case ProgressionStep.PartIV:
                _mailService.AddMailReceived(WildEggConstants.MailIds.WizardThanks);
                break;
            case ProgressionStep.PartV:
                _mailService.AddMailReceived(WildEggConstants.MailIds.WizardFindings);
                break;
            case ProgressionStep.PartVI:
                _mailService.AddMailReceived(WildEggConstants.MailIds.MarnieGotIncubator);
                break;
        }
    }

    /// <summary>
    /// Checks if a progression step is unlocked based on conditions.
    /// Uses infrastructure for all game state checks.
    /// </summary>
    private bool IsStepUnlocked(ProgressionStep step)
    {
        return step switch
        {
            // Part I: Always unlocked
            ProgressionStep.PartI => true,

            // Part II: After completing Special Order I (mail-based)
            ProgressionStep.PartII => _mailService.HasMail(WildEggConstants.MailIds.SpecialOrderDoneI),

            // Part III: After Wizard's study quest + 2 days
            ProgressionStep.PartIII => HasQuestWithDays(WildEggConstants.QuestIds.WizardStudy, 2),

            // Part IV: After Special Order II + 3 days
            ProgressionStep.PartIV => HasQuestWithDays(WildEggConstants.QuestIds.SpecialOrderWildEggsII, 3),

            // Part V: After receiving Desert Scroll + 3 days
            ProgressionStep.PartV => HasQuestWithDays(WildEggConstants.QuestIds.TheDesertScroll, 3),

            // Part VI: After Desert Scroll + 5 days (final step)
            // Note: This is actually TheDesertScroll + 5 days (3 base + 2 extra)
            ProgressionStep.PartVI => HasQuestWithDays(WildEggConstants.QuestIds.TheDesertScroll, 5),

            _ => false
        };
    }

    /// <summary>
    /// Checks if a quest was completed a certain number of days ago.
    /// </summary>
    private bool HasQuestWithDays(string questId, int daysRequired)
    {
        // Check if quest is completed (either in our tracking or in game state)
        var completedInGame = _mailService.IsQuestCompleted(questId);
        
        // Also check our tracked completion
        var trackedCompletionDay = _playerState.GetQuestCompletionDay(questId);

        if (!completedInGame && !trackedCompletionDay.HasValue)
        {
            return false;
        }

        // Use tracked completion day if available, otherwise use current day
        var completedDay = trackedCompletionDay ?? _currentDay;

        // Check if enough days have passed
        return _currentDay >= completedDay + daysRequired;
    }

    /// <summary>
    /// Gets the current progression step.
    /// </summary>
    public ProgressionStep GetCurrentStep() => _state.CurrentStep;

    /// <summary>
    /// Checks if the player has reached a specific step.
    /// </summary>
    public bool HasReached(ProgressionStep step) => _state.HasReached(step);

    /// <summary>
    /// Gets the days required for a specific quest.
    /// </summary>
    public static int GetDaysToWait(string questId)
    {
        return DaysToWait.TryGetValue(questId, out var days) ? days : 0;
    }
}

