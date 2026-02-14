using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.Core.Events;
using WildEggsHatching.Core.Models;
using WildEggsHatching.Infrastructure.Game;

namespace WildEggsHatching.Notes;

/// <summary>
/// Handles spawning and managing notes that unlock new animals.
/// Listens for progression events and spawns appropriate notes.
/// </summary>
public sealed class NoteService
{
    /// <summary>The event bus for subscribing to events.</summary>
    private readonly EventBus _eventBus;

    /// <summary>The player's animal state.</summary>
    private readonly PlayerWildState _playerState;

    /// <summary>Interface for placing objects in the world.</summary>
    private readonly IGameObjectPlacer _objectPlacer;

    /// <summary>Interface for sending mail.</summary>
    private readonly IGameMailService _mailService;

    /// <summary>Tracks notes that have been sent to avoid duplicates.</summary>
    private readonly HashSet<string> _sentNotes;

    /// <summary>
    /// Creates a new note service.
    /// </summary>
    /// <param name="eventBus">The event bus for subscribing to events.</param>
    /// <param name="playerState">The player's animal state.</param>
    /// <param name="objectPlacer">The object placer for spawning notes.</param>
    /// <param name="mailService">The mail service for mail checks.</param>
    public NoteService(
        EventBus eventBus,
        PlayerWildState playerState,
        IGameObjectPlacer objectPlacer,
        IGameMailService mailService)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _playerState = playerState ?? throw new ArgumentNullException(nameof(playerState));
        _objectPlacer = objectPlacer ?? throw new ArgumentNullException(nameof(objectPlacer));
        _mailService = mailService ?? throw new ArgumentNullException(nameof(mailService));
        _sentNotes = new HashSet<string>();

        // Subscribe to relevant events
        _eventBus.Subscribe<AnimalUnlockedEvent>(OnAnimalUnlocked);
        _eventBus.Subscribe<PhaseUnlockedEvent>(OnPhaseUnlocked);
    }

    /// <summary>
    /// Called when an animal is unlocked.
    /// May spawn a note for the next animal in progression.
    /// </summary>
    private void OnAnimalUnlocked(AnimalUnlockedEvent @event)
    {
        // When an animal is unlocked, check if a note should spawn
        // for the next animal in the progression chain
        MaybeSpawnNextNote(@event.AnimalId);
    }

    /// <summary>
    /// Called when a progression phase is unlocked.
    /// May spawn notes appropriate for the new phase.
    /// </summary>
    private void OnPhaseUnlocked(PhaseUnlockedEvent @event)
    {
        // When a new phase unlocks, spawn notes for that phase
        SpawnNotesForPhase(@event.Step);
    }

    /// <summary>
    /// Attempts to spawn a note for the next animal in sequence.
    /// </summary>
    /// <param name="currentAnimalId">The animal that was just unlocked.</param>
    private void MaybeSpawnNextNote(string currentAnimalId)
    {
        // Get the next animal in the progression
        var nextAnimal = GetNextAnimalInProgression(currentAnimalId);

        if (nextAnimal == null)
        {
            return; // No more animals to unlock
        }

        // Check if note was already sent
        if (_sentNotes.Contains(nextAnimal))
        {
            return;
        }

        // Check if player already has the note
        if (_playerState.HasCollectedNote(nextAnimal))
        {
            return;
        }

        // Spawn the note
        SpawnNote(nextAnimal);
    }

    /// <summary>
    /// Spawns notes appropriate for a specific progression phase.
    /// </summary>
    private void SpawnNotesForPhase(ProgressionStep step)
    {
        var notesForPhase = GetNotesForPhase(step);

        foreach (var noteId in notesForPhase)
        {
            if (_sentNotes.Contains(noteId))
            {
                continue;
            }

            if (_playerState.HasCollectedNote(noteId))
            {
                continue;
            }

            SpawnNote(noteId);
        }
    }

    /// <summary>
    /// Spawns a note into the game world.
    /// </summary>
    /// <param name="animalId">The animal this note unlocks.</param>
    private void SpawnNote(string animalId)
    {
        // Find a valid location for the note
        var location = _objectPlacer.GetType()
            .GetMethod("GetLocation")
            ?.Invoke(null, new object[] { "Forest" }) as GameLocation;

        // Fallback to game state reader if object placer doesn't have GetLocation
        if (location == null)
        {
            location = Game1.getLocationFromName("Forest");
        }

        if (location == null)
        {
            ModEntry.LoggerHelper.Log(
                $"Could not find Forest location for note: {animalId}",
                StardewModdingAPI.LogLevel.Warn
            );
            return;
        }

        // Find a valid tile for the note
        var tile = FindNoteTile(location);

        if (tile == null)
        {
            ModEntry.LoggerHelper.Log(
                $"Could not find valid tile for note: {animalId}",
                StardewModdingAPI.LogLevel.Warn
            );
            return;
        }

        // Create the note object (junimo note style)
        var note = new Chest(0)
        {
            TileLocation = tile.Value,
            Location = location,
        };

        // Add the note to the location using object placer
        if (_objectPlacer.PlaceObject(location, tile.Value, note))
        {
            _sentNotes.Add(animalId);

            ModEntry.LoggerHelper.Log(
                $"Spawned note for animal: {animalId} at {tile.Value} in {location.Name}",
                StardewModdingAPI.LogLevel.Info
            );
        }
    }

    /// <summary>
    /// Finds a valid tile for spawning a note.
    /// </summary>
    private Vector2? FindNoteTile(GameLocation location)
    {
        if (location == null)
            return null;

        var mapWidth = location.Map.Layers[0].LayerWidth;
        var mapHeight = location.Map.Layers[0].LayerHeight;

        int attempts = 0;
        const int maxAttempts = 50;

        while (attempts < maxAttempts)
        {
            int x = Game1.random.Next(mapWidth);
            int y = Game1.random.Next(mapHeight);
            var tile = new Vector2(x, y);

            if (IsValidNoteTile(location, tile))
            {
                return tile;
            }

            attempts++;
        }

        return null;
    }

    /// <summary>
    /// Checks if a tile is valid for spawning a note.
    /// </summary>
    private bool IsValidNoteTile(GameLocation location, Vector2 tile)
    {
        if (location == null)
            return false;

        // Must be on map
        if (!location.Map.IsTileOnMap(tile))
        {
            return false;
        }

        // Must not be occupied
        if (location.Objects.ContainsKey(tile))
        {
            return false;
        }

        // Must be passable
        if (!location.isTilePassable(tile, Game1.viewport))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Gets the next animal in the progression chain.
    /// </summary>
    private string? GetNextAnimalInProgression(string currentAnimalId)
    {
        // Simplified: Define progression order
        var progressionOrder = new[]
        {
            "Chicken",
            "Duck",
            "Dinosaur",
            "Void Chicken",
        };

        var currentIndex = Array.IndexOf(progressionOrder, currentAnimalId);

        if (currentIndex < 0 || currentIndex >= progressionOrder.Length - 1)
        {
            return null; // Current animal not found or already at end
        }

        return progressionOrder[currentIndex + 1];
    }

    /// <summary>
    /// Gets the notes that should spawn for a specific phase.
    /// </summary>
    private IEnumerable<string> GetNotesForPhase(ProgressionStep step)
    {
        return step switch
        {
            // Phase II notes
            ProgressionStep.PartII => new[] { "Duck" },

            // Phase III notes
            ProgressionStep.PartIII => new[] { "Dinosaur" },

            // Phase IV notes
            ProgressionStep.PartIV => new[] { "Void Chicken" },

            // No notes in other phases
            _ => Enumerable.Empty<string>()
        };
    }

    /// <summary>
    /// Marks a note as collected by the player.
    /// Called when the player reads a note.
    /// </summary>
    public void CollectNote(string noteId)
    {
        _playerState.CollectNote(noteId);
        _sentNotes.Remove(noteId);

        ModEntry.LoggerHelper.Log(
            $"Note collected: {noteId}",
            StardewModdingAPI.LogLevel.Info
        );
    }
}

