using System.Collections.Generic;
using System.Linq;
using StardewValley;
using WildEggsHatching.Core.Models;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.Infrastructure.Game;

namespace WildEggsHatching.Progression.Rules;

/// <summary>
/// Abstraction for providing unlock context data.
/// This isolates game state access from the domain rules.
/// </summary>
public interface IUnlockContextProvider
{
    /// <summary>Creates an unlock context for the current game state.</summary>
    UnlockContext CreateContext(PlayerWildState playerState, ProgressionStep currentStep);

    /// <summary>Gets the completion day for a specific quest.</summary>
    int? GetQuestCompletionDay(string questId);

    /// <summary>Checks if a specific mail has been received.</summary>
    bool HasMail(string mailId);

    /// <summary>Checks if a specific quest has been completed.</summary>
    bool HasCompletedQuest(string questId);
}

/// <summary>
/// Default implementation using infrastructure services.
/// This is the preferred implementation that avoids direct Game1 access.
/// </summary>
public sealed class UnlockContextProvider : IUnlockContextProvider
{
    private readonly IGameStateReader _gameStateReader;
    private readonly IGameMailService _mailService;
    private readonly PlayerWildState _playerState;

    public UnlockContextProvider(
        IGameStateReader gameStateReader,
        IGameMailService mailService,
        PlayerWildState playerState)
    {
        _gameStateReader = gameStateReader ?? throw new System.ArgumentNullException(nameof(gameStateReader));
        _mailService = mailService ?? throw new System.ArgumentNullException(nameof(mailService));
        _playerState = playerState;
    }

    /// <inheritdoc />
    public UnlockContext CreateContext(PlayerWildState playerState, ProgressionStep currentStep)
    {
        return new UnlockContext
        {
            CurrentDay = _gameStateReader.GetCurrentDay(),
            CurrentStep = currentStep,
            PlayerState = playerState ?? _playerState,
            Location = _gameStateReader.GetPlayerLocation(),
            QuestCompletionDayProvider = this,
            MailChecker = this,
            QuestChecker = this
        };
    }

    /// <inheritdoc />
    public int? GetQuestCompletionDay(string questId)
    {
        // Get from PlayerWildState (persisted save data)
        return _playerState?.GetQuestCompletionDay(questId);
    }

    /// <inheritdoc />
    public bool HasMail(string mailId)
    {
        return _mailService.HasMail(mailId);
    }

    /// <inheritdoc />
    public bool HasCompletedQuest(string questId)
    {
        // Check both game state and our tracked completion
        return _mailService.IsQuestCompleted(questId) 
            || (_playerState?.IsQuestCompleted(questId) ?? false);
    }
}

/// <summary>
/// Legacy provider for backward compatibility during transition.
/// Uses ModEntry for game state access.
/// NOTE: Should be removed after full refactor is complete.
/// </summary>
[System.Obsolete("Use UnlockContextProvider instead")]
public sealed class LegacyUnlockContextProvider : IUnlockContextProvider
{
    private readonly IGameStateReader? _gameStateReader;
    private readonly IGameMailService? _mailService;

    public LegacyUnlockContextProvider() : this(null, null)
    {
    }

    public LegacyUnlockContextProvider(IGameStateReader? gameStateReader, IGameMailService? mailService)
    {
        _gameStateReader = gameStateReader;
        _mailService = mailService;
    }

    public UnlockContext CreateContext(PlayerWildState playerState, ProgressionStep currentStep)
    {
        int currentDay = _gameStateReader?.GetCurrentDay() ?? Game1.Date.TotalDays;
        
        return new UnlockContext
        {
            CurrentDay = currentDay,
            CurrentStep = currentStep,
            PlayerState = playerState,
            Location = _gameStateReader?.GetPlayerLocation() ?? Game1.player?.currentLocation,
            QuestCompletionDayProvider = this,
            MailChecker = this,
            QuestChecker = this
        };
    }

    public int? GetQuestCompletionDay(string questId)
    {
        // Try to get from save data first
        if (_mailService != null)
        {
            // Use mail service to check quest completion day
            // This would need to be implemented in IGameMailService
            return null;
        }

        // Fallback to ModEntry for legacy access
        var modData = ModEntry.HelperInstance.Data.ReadSaveData<ModData>(ModData.AnimalDataKey);
        if (modData?.QuestCompletedDay != null &&
            modData.QuestCompletedDay.TryGetValue(questId, out var day))
        {
            return day;
        }
        return null;
    }

    public bool HasMail(string mailId)
    {
        return _mailService?.HasMail(mailId) ?? Game1.player?.mailReceived.Contains(mailId) ?? false;
    }

    public bool HasCompletedQuest(string questId)
    {
        return _mailService?.IsQuestCompleted(questId) ?? Game1.player?.questLog.Any(q => q.id.Value == questId) ?? false;
    }
}
