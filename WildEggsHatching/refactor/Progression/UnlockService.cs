using System;
using System.Collections.Generic;
using StardewValley;
using WildEggsHatching.Core.Events;
using WildEggsHatching.Core.Models;
using WildEggsHatching.Infrastructure.Game;

namespace WildEggsHatching.Progression;

/// <summary>
/// Handles unlocking new animals for the player.
/// Listens for hatch events and updates player state accordingly.
/// </summary>
public sealed class UnlockService
{
    /// <summary>The player's wild animal state.</summary>
    private readonly PlayerWildState _playerState;

    /// <summary>The event bus for publishing unlock events.</summary>
    private readonly EventBus _eventBus;

    /// <summary>Interface for reading game state.</summary>
    private readonly IGameStateReader _gameStateReader;

    /// <summary>Interface for sending mail notifications.</summary>
    private readonly IGameMailService _mailService;

    /// <summary>The current game day.</summary>
    private int _currentDay;

    /// <summary>
    /// Creates a new unlock service.
    /// </summary>
    /// <param name="playerState">The player's animal state.</param>
    /// <param name="eventBus">The event bus for publishing events.</param>
    /// <param name="gameStateReader">The game state reader.</param>
    /// <param name="mailService">The mail service for sending notifications.</param>
    public UnlockService(
        PlayerWildState playerState,
        EventBus eventBus,
        IGameStateReader gameStateReader,
        IGameMailService mailService)
    {
        _playerState = playerState ?? throw new ArgumentNullException(nameof(playerState));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _gameStateReader = gameStateReader ?? throw new ArgumentNullException(nameof(gameStateReader));
        _mailService = mailService ?? throw new ArgumentNullException(nameof(mailService));

        // Subscribe to hatch events
        _eventBus.Subscribe<AnimalHatchedEvent>(OnAnimalHatched);
    }

    /// <summary>
    /// Creates a new unlock service with default infrastructure.
    /// </summary>
    public UnlockService(PlayerWildState playerState, EventBus eventBus)
        : this(playerState, eventBus, new GameStateReader(), new GameMailService())
    {
    }

    /// <summary>
    /// Handles when an animal hatches from an egg.
    /// </summary>
    private void OnAnimalHatched(AnimalHatchedEvent @event)
    {
        _currentDay = _gameStateReader.GetCurrentDay();

        // Check if this animal is already unlocked
        if (_playerState.IsUnlocked(@event.AnimalId))
        {
            return; // Already unlocked
        }

        // Discover the animal (first time seeing it)
        _playerState.Discover(@event.AnimalId);

        // Unlock the animal for the player
        _playerState.Unlock(@event.AnimalId);

        // Publish event for other services to react to
        _eventBus.Publish(new AnimalUnlockedEvent(
            @event.AnimalId,
            "EggHatched",
            _currentDay
        ));

        ModEntry.LoggerHelper.Log(
            $"Unlocked animal: {@event.AnimalId}",
            StardewModdingAPI.LogLevel.Info
        );
    }

    /// <summary>
    /// Checks if a specific animal is unlocked.
    /// </summary>
    public bool IsUnlocked(string animalId) => _playerState.IsUnlocked(animalId);

    /// <summary>
    /// Gets all unlocked animal IDs.
    /// </summary>
    public HashSet<string> GetUnlockedAnimals() => _playerState.UnlockedAnimals;

    /// <summary>
    /// Manually unlocks an animal (for testing or special cases).
    /// </summary>
    public void UnlockAnimal(string animalId, string reason)
    {
        if (_playerState.IsUnlocked(animalId))
        {
            return; // Already unlocked
        }

        _currentDay = _gameStateReader.GetCurrentDay();
        _playerState.Unlock(animalId);

        _eventBus.Publish(new AnimalUnlockedEvent(
            animalId,
            reason,
            _currentDay
        ));
    }

    /// <summary>
    /// Sends a notification mail when an animal is unlocked.
    /// </summary>
    public void SendUnlockNotification(string animalId, string mailId)
    {
        _mailService.SendMail(mailId, daysDelay: 1);
    }
}

